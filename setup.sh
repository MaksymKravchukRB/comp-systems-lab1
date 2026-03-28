#!/bin/bash
set -euo pipefail

# ============================================
# Lab1 Deployment Automation Script
# ============================================

# Configuration variables
PROJECT_ROOT="$(pwd)"                      # Where the script is run from
DEPLOY_PATH="/opt/mywebapp"                # Where the compiled app goes
CONFIG_PATH="/etc/mywebapp"                # Configuration directory
SERVICE_NAME="mywebapp"                    # Systemd service name
NGINX_SITE="mywebapp"                      # Nginx site name
DB_NAME="notesdb"                          # PostgreSQL database name
DB_USER="lab1"                             # PostgreSQL username
DB_PASSWORD="mysecretpassword"             # Change to a strong password!
GRADEBOOK_NUMBER="9"                       # Group number N

# Detect the default user (the one who invoked sudo)
DEFAULT_USER_TO_LOCK="${SUDO_USER:-ubuntu}"

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Helper functions
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1" >&2
}

check_root() {
    if [ "$EUID" -ne 0 ]; then
        log_error "Please run as root (use sudo)."
        exit 1
    fi
}

check_prerequisites() {
    if [ ! -f "comp_systems_lab1.csproj" ]; then
        log_error "No comp_systems_lab1.csproj found in current directory. Are you in the project root?"
        exit 1
    fi
    log_info "Prerequisites OK."
}

# ============================================
# 1. Install necessary packages
# ============================================
install_packages() {
    log_info "Updating package lists and installing required packages..."
    apt update
    apt install -y dotnet-sdk-10.0 postgresql nginx
}

# ============================================
# 2. Create users
# ============================================
create_users() {
    log_info "Creating system users..."

    # System user for the app
    if ! id "app" &>/dev/null; then
        useradd -r -s /usr/sbin/nologin app
        log_info "User 'app' created."
    else
        log_info "User 'app' already exists."
    fi

    # Student user (with sudo)
    if ! id "student" &>/dev/null; then
        useradd -m student
        usermod -aG sudo student
        log_info "User 'student' created with sudo rights."
    else
        log_info "User 'student' already exists."
    fi

    # Teacher user
    if ! id "teacher" &>/dev/null; then
        useradd -m teacher
        echo "teacher:12345678" | chpasswd
        chage -d 0 teacher   # Force password change on first login
        log_info "User 'teacher' created (password 12345678, must change)."
    else
        log_info "User 'teacher' already exists."
    fi

    # Operator user – special handling on Ubuntu 24.04 (group 'operator' exists)
    if ! id "operator" &>/dev/null; then
        # On Ubuntu 24.04, `adduser operator` may fail because group 'operator' exists.
        # We'll use `useradd` which doesn't check group name conflicts.
        useradd -m operator
        echo "operator:12345678" | chpasswd
        chage -d 0 operator
        log_info "User 'operator' created (password 12345678, must change)."
    else
        log_info "User 'operator' already exists."
    fi
}

# ============================================
# 3. Configure sudo for operator
# ============================================
configure_sudo_operator() {
    log_info "Configuring sudo for operator..."
    cat > /etc/sudoers.d/operator <<EOF
# Operator can manage the webapp service and reload nginx
operator ALL=(ALL) NOPASSWD: /usr/bin/systemctl start $SERVICE_NAME
operator ALL=(ALL) NOPASSWD: /usr/bin/systemctl stop $SERVICE_NAME
operator ALL=(ALL) NOPASSWD: /usr/bin/systemctl restart $SERVICE_NAME
operator ALL=(ALL) NOPASSWD: /usr/bin/systemctl status $SERVICE_NAME
operator ALL=(ALL) NOPASSWD: /usr/sbin/nginx -s reload
EOF
    chmod 440 /etc/sudoers.d/operator
    log_info "Sudo rules for operator applied."
}

# ============================================
# 4. Create database and user in PostgreSQL
# ============================================
setup_database() {
    log_info "Setting up PostgreSQL database and user..."

    # Ensure PostgreSQL is running
    systemctl start postgresql
    systemctl enable postgresql

    # Create user and database if they don't exist
    sudo -u postgres psql <<EOF
DO \$\$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '$DB_USER') THEN
        CREATE USER $DB_USER WITH PASSWORD '$DB_PASSWORD';
    END IF;
END
\$\$;
SELECT 'CREATE DATABASE $DB_NAME OWNER $DB_USER'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '$DB_NAME')\gexec
EOF
    log_info "Database '$DB_NAME' and user '$DB_USER' ready."
}

# ============================================
# 5. Create configuration file
# ============================================
create_config() {
    log_info "Creating configuration file at $CONFIG_PATH/config.json..."

    mkdir -p "$CONFIG_PATH"
    cat > "$CONFIG_PATH/config.json" <<EOF
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD"
  },
  "Port": 5000
}
EOF

    # Set proper ownership and permissions
    chown -R app:app "$CONFIG_PATH"
    chmod 750 "$CONFIG_PATH"
    chmod 640 "$CONFIG_PATH/config.json"
    log_info "Configuration file created."
}

# ============================================
# 6. Build and deploy the application
# ============================================
deploy_app() {
    log_info "Building and publishing the application..."

    # Clean previous publish (optional)
    rm -rf "$DEPLOY_PATH"

    # Publish the application (Release configuration)
    dotnet publish -c Release -o "$DEPLOY_PATH"

    # Set permissions
    chown -R app:app "$DEPLOY_PATH"
    chmod 750 "$DEPLOY_PATH"
    log_info "Application deployed to $DEPLOY_PATH"
}

# ============================================
# 7. Create systemd service unit
# ============================================
create_systemd_service() {
    log_info "Creating systemd service $SERVICE_NAME..."

    cat > "/etc/systemd/system/$SERVICE_NAME.service" <<EOF
[Unit]
Description=Lab1 Notes Service
After=network.target postgresql.service

[Service]
Type=simple
User=app
Group=app
WorkingDirectory=$DEPLOY_PATH
ExecStartPre=/usr/bin/dotnet $DEPLOY_PATH/mywebapp.dll --migrate
ExecStart=/usr/bin/dotnet $DEPLOY_PATH/mywebapp.dll
Restart=on-failure

[Install]
WantedBy=multi-user.target
EOF

    systemctl daemon-reload
    systemctl enable "$SERVICE_NAME"
    log_info "Systemd service created and enabled."
}

# ============================================
# 8. Configure nginx as reverse proxy
# ============================================
configure_nginx() {
    log_info "Configuring nginx..."

    # Remove default site if it exists (or disable)
    rm -f /etc/nginx/sites-enabled/default

    # Create site configuration
    cat > "/etc/nginx/sites-available/$NGINX_SITE" <<EOF
server {
    listen 80;
    server_name _;

    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
    }
}
EOF

    # Enable site
    ln -sf "/etc/nginx/sites-available/$NGINX_SITE" "/etc/nginx/sites-enabled/"

    # Test configuration and reload
    nginx -t
    systemctl reload nginx
    log_info "Nginx configured and reloaded."
}

# ============================================
# 9. Create gradebook file
# ============================================
create_gradebook() {
    log_info "Creating gradebook file..."
    echo "$GRADEBOOK_NUMBER" > "/home/student/gradebook"
    chown student:student "/home/student/gradebook"
    chmod 644 "/home/student/gradebook"
    log_info "Gradebook created with number $GRADEBOOK_NUMBER."
}

# ============================================
# 10. Block default user
# ============================================
lock_default_user() {
    # Check if the default user is one of the created users
    if [[ "$DEFAULT_USER_TO_LOCK" == "student" || "$DEFAULT_USER_TO_LOCK" == "teacher" || "$DEFAULT_USER_TO_LOCK" == "operator" || "$DEFAULT_USER_TO_LOCK" == "app" ]]; then
        log_info "Default user $DEFAULT_USER_TO_LOCK is one of the created users, skipping lock."
        return
    fi
    if id "$DEFAULT_USER_TO_LOCK" &>/dev/null; then
        log_info "Locking default user $DEFAULT_USER_TO_LOCK..."
        passwd -l "$DEFAULT_USER_TO_LOCK"
        log_info "User $DEFAULT_USER_TO_LOCK locked."
    else
        log_info "User $DEFAULT_USER_TO_LOCK not found, skipping."
    fi
}

# ============================================
# 11. Start the service
# ============================================
start_service() {
    log_info "Starting $SERVICE_NAME service..."
    systemctl start "$SERVICE_NAME"
    log_info "Service started."
}

# ============================================
# Main execution
# ============================================
main() {
    check_root
    check_prerequisites

    install_packages
    create_users
    configure_sudo_operator
    setup_database
    create_config
    deploy_app
    create_systemd_service
    configure_nginx
    create_gradebook
    lock_default_user
    start_service

    log_info "============================================"
    log_info "Deployment completed successfully!"
    log_info "You can now access the Notes Service at http://<VM_IP>"
    log_info "Health checks: /health/alive and /health/ready"
    log_info "============================================"
}

main "$@"