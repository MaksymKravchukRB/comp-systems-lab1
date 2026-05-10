#!/bin/sh
set -e

# Run database migrations
dotnet mywebapp.dll --migrate

# Start the web server
exec dotnet mywebapp.dll
