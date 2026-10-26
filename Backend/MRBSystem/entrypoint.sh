#!/bin/bash

# Wait for SQL Server to be ready
echo "Waiting for SQL Server..."
for i in {1..30}; do
    if sqlcmd -S sqlserver -U sa -P "$DB_SA_PASSWORD" -Q "SELECT 1" > /dev/null 2>&1; then
        echo "SQL Server is ready!"
        break
    fi
    echo "Attempt $i: SQL Server not ready, waiting..."
    sleep 2
done

# Create database if it doesn't exist
echo "Creating database..."
sqlcmd -S sqlserver -U sa -P "$DB_SA_PASSWORD" -Q "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'MRBSystemDb') CREATE DATABASE MRBSystemDb;"

# Run migrations
echo "Running database migrations..."
cd /src
dotnet ef database update --project infrasturucture/MRBSystem.Persistence --startup-project presentation/MRBSystem.WebAPI

echo "Migrations complete!"

# Start the application
echo "Starting application..."
cd /app
exec dotnet MRBSystem.WebAPI.dll
