#!/bin/bash
set -e

echo "==> Running EF Core migrations..."

if ./efbundle --connection "$ConnectionStrings__DefaultConnection"; then
    echo "==> Migrations applied successfully."
else
    EXIT_CODE=$?
    echo "==> Migration exited with code $EXIT_CODE. Proceeding anyway..."
fi

echo "==> Starting API..."
exec dotnet Devken.CBC.SchoolManagement.API.dll