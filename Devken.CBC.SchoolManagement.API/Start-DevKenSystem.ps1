# ═══════════════════════════════════════════════════════════
# DevKen School Management System - Auto Start Script
# ═══════════════════════════════════════════════════════════
# This script automatically:
# 1. Applies any pending database migrations
# 2. Starts the .NET API
# 3. Starts the Angular frontend
# ═══════════════════════════════════════════════════════════

param(
    [switch]$SkipMigration = $false,
    [switch]$ApiOnly = $false,
    [switch]$UIOnly = $false
)

# ── Configuration ─────────────────────────────────────────
$SolutionRoot = "C:\Users\ngeti\source\repos\DevKen.School.System"
$ApiProject = "$SolutionRoot\Devken.CBC.SchoolManagement.API"
$InfraProject = "$SolutionRoot\Devken.CBC.SchoolManagement.Infrastructure"
$AngularProject = "$ApiProject\Devken.CBC.SchoolManagment.UI\School-System-UI"

# ── Colors ────────────────────────────────────────────────
function Write-Header {
    param([string]$Message)
    Write-Host "`n═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host " $Message" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════`n" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor Blue
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor Yellow
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

# ══════════════════════════════════════════════════════════
# MAIN EXECUTION
# ══════════════════════════════════════════════════════════

Write-Header "DevKen School Management System - Auto Start"

# ── Step 1: Apply Database Migrations ─────────────────────
if (-not $SkipMigration -and -not $UIOnly) {
    Write-Header "Step 1: Database Migrations"
    
    Write-Info "Checking for pending migrations..."
    
    try {
        # Check if there are pending migrations
        $pendingMigrations = dotnet ef migrations list --project $InfraProject --startup-project $ApiProject --no-build 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Migration check completed"
            
            # Apply migrations
            Write-Info "Applying database migrations..."
            dotnet ef database update --project $InfraProject --startup-project $ApiProject
            
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Database migrations applied successfully!"
            } else {
                Write-Warning "Migrations may have failed, but continuing..."
            }
        } else {
            Write-Warning "Could not check migrations. Skipping migration step..."
        }
    }
    catch {
        Write-Error-Custom "Error during migration: $_"
        Write-Warning "Continuing despite migration errors..."
    }
}
elseif ($SkipMigration) {
    Write-Info "Skipping database migrations (--SkipMigration flag)"
}

# ── Step 2: Start .NET API ────────────────────────────────
if (-not $UIOnly) {
    Write-Header "Step 2: Starting .NET API"
    
    Write-Info "Starting API at $ApiProject"
    Write-Info "API will auto-apply migrations on startup if configured..."
    
    # Start API in a new window
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$ApiProject'; Write-Host 'Starting .NET API...' -ForegroundColor Green; dotnet run"
    
    Write-Success "API process started!"
    Write-Info "Waiting 5 seconds for API to initialize..."
    Start-Sleep -Seconds 5
}

# ── Step 3: Start Angular UI ──────────────────────────────
if (-not $ApiOnly) {
    Write-Header "Step 3: Starting Angular UI"
    
    # Check if Angular project exists
    if (Test-Path $AngularProject) {
        Write-Info "Starting Angular at $AngularProject"
        
        # Check if node_modules exists
        if (-not (Test-Path "$AngularProject\node_modules")) {
            Write-Warning "node_modules not found. Running npm install..."
            Push-Location $AngularProject
            npm install
            Pop-Location
            
            if ($LASTEXITCODE -eq 0) {
                Write-Success "npm install completed!"
            } else {
                Write-Error-Custom "npm install failed!"
                exit 1
            }
        }
        
        # Start Angular in a new window
        Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$AngularProject'; Write-Host 'Starting Angular Development Server...' -ForegroundColor Green; npm start"
        
        Write-Success "Angular UI process started!"
        Write-Info "Angular dev server will be available at: http://localhost:4200"
    }
    else {
        Write-Error-Custom "Angular project not found at: $AngularProject"
        Write-Info "Please verify the Angular project path."
    }
}

# ── Summary ───────────────────────────────────────────────
Write-Header "Summary"

if (-not $UIOnly) {
    Write-Success "✅ API Started"
    Write-Info "   API is running (check the new terminal window)"
}

if (-not $ApiOnly) {
    Write-Success "✅ Angular UI Started"
    Write-Info "   UI will be available at: http://localhost:4200"
}

Write-Host "`n"
Write-Info "Both applications are now running in separate windows."
Write-Info "Press Ctrl+C in each window to stop the respective application."
Write-Host "`n"

# Keep this window open
Write-Host "Press any key to exit this launcher..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
