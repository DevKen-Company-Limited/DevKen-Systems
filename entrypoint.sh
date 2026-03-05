#!/bin/bash
set -e

echo "==> Running EF Core migrations..."
./efbundle --connection "$ConnectionStrings__DefaultConnection"

echo "==> Migrations complete. Starting API..."
exec dotnet Devken.CBC.SchoolManagement.API.dll
```

**How it works:**

1. `dotnet ef migrations bundle` compiles all pending migrations into a single self-contained binary (`efbundle`) during the Docker build — no SDK needed at runtime.
2. On every container startup, `entrypoint.sh` runs `efbundle` first which applies any pending migrations, then starts the API.
3. The `--connection` flag reads the connection string from the `ConnectionStrings__DefaultConnection` environment variable you set in Render's dashboard.

**In Render, set these environment variables:**
```
ConnectionStrings__DefaultConnection = Server=<host>;Database=<db>;User=<user>;Password=<pass>;
Cors__AllowedOrigins__0 = https://dev-ken-systems.vercel.app
Cors__AllowedOrigins__1 = http://localhost:4200