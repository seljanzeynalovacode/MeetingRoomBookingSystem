#!/bin/bash
set -e

echo "Waiting for database..."
sleep 10

echo "Applying migrations..."
# dotnet ef database update...

echo "Starting application..."
exec dotnet MRBSystem.WebAPI.dll
