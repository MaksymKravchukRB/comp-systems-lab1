#!/bin/bash
set -euo pipefail

echo "Setting up target node for Notes Service..."

# Install Docker and Docker Compose
apt-get update
apt-get install -y docker.io docker-compose nginx

# Start Docker and enable on boot
systemctl start docker
systemctl enable docker

# Create app directory
mkdir -p /opt/notes-service
chmod 755 /opt/notes-service

# Create docker-compose.yml (will be overwritten during deployment)
cat > /opt/notes-service/docker-compose.yml <<EOF
version: '3.8'
services:
  web:
    image: \${IMAGE_TAG}
    environment:
      - DB_CONNECTION_STRING=Host=db;Database=notesdb;Username=lab1;Password=\${DB_PASSWORD}
    ports:
      - "5000:5000"
    depends_on:
      - db
  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: notesdb
      POSTGRES_USER: lab1
      POSTGRES_PASSWORD: \${DB_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
volumes:
  postgres_data:
EOF

# Create environment file template (secrets filled during CD)
touch /opt/notes-service/.env
chmod 600 /opt/notes-service/.env

# Create systemd unit for Docker Compose
cat > /etc/systemd/system/notes-service.service <<EOF
[Unit]
Description=Notes Service Docker Compose
Requires=docker.service
After=docker.service

[Service]
Type=oneshot
RemainAfterExit=yes
WorkingDirectory=/opt/notes-service
User=root
EnvironmentFile=/opt/notes-service/.env
ExecStart=/usr/bin/docker-compose up -d
ExecStop=/usr/bin/docker-compose down
ExecReload=/usr/bin/docker-compose restart

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable notes-service

# Configure Nginx as reverse proxy
cat > /etc/nginx/sites-available/notes-service <<EOF
server {
    listen 80;
    server_name _;
    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
    }
}
EOF
ln -sf /etc/nginx/sites-available/notes-service /etc/nginx/sites-enabled/
rm -f /etc/nginx/sites-enabled/default
nginx -t && systemctl reload nginx

echo "Target node setup complete."