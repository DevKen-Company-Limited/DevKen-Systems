#!/bin/bash
set -e

echo "==> Running EF Core migrations..."

if ./efbundle --connection "$ConnectionStrings__DefaultConnection"; then
    echo "==> Migrations applied successfully."
else
    EXIT_CODE=$?
    if echo "$EXIT_CODE" | grep -q "already exists\|no pending\|up-to-date"; then
        echo "==> Database already up to date. Skipping."
    else
        echo "==> Migration failed with exit code $EXIT_CODE. Proceeding anyway..."
    fi
fi

echo "==> Starting API..."
exec dotnet Devken.CBC.SchoolManagement.API.dll