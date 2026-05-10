# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies (cached unless csproj changes)
COPY comp_systems_lab1.csproj .
RUN dotnet restore

# Copy the rest of the source code and publish
COPY . .
RUN dotnet publish comp_systems_lab1.csproj -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy published application from build stage
COPY --from=build /app/publish .

# Copy entrypoint script
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

# Expose the port (can be overridden by ASPNETCORE_URLS)
EXPOSE 5000

# Use entrypoint to run migrations then start the app
ENTRYPOINT ["/entrypoint.sh"]
