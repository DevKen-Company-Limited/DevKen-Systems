# ═══════════════════════════════════════════════════════════
# ReflectionTypeLoadException Quick Fix Script
# ═══════════════════════════════════════════════════════════

param(
    [switch]$FullClean,
    [switch]$CheckPackages,
    [switch]$FixEFCore
)

$SolutionRoot = "C:\Users\ngeti\source\repos\DevKen.School.System"

function Write-Step {
    param([string]$Message)
    Write-Host "`n═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host " $Message" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════`n" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor Blue
}

# ══════════════════════════════════════════════════════════
# MAIN EXECUTION
# ══════════════════════════════════════════════════════════

Write-Host "`n🔧 ReflectionTypeLoadException Quick Fix" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Yellow

Push-Location $SolutionRoot

# ── Step 1: Clean Solution ────────────────────────────────
if ($FullClean) {
    Write-Step "Step 1: Deep Clean (Removing bin/obj)"
    
    Write-Info "Finding bin and obj folders..."
    $foldersToDelete = Get-ChildItem -Path $SolutionRoot -Include bin,obj -Recurse -Directory -Force
    
    $count = ($foldersToDelete | Measure-Object).Count
    Write-Info "Found $count folders to delete"
    
    if ($count -gt 0) {
        $foldersToDelete | Remove-Item -Recurse -Force
        Write-Success "Deleted $count bin/obj folders"
    }
}

Write-Step "Step 2: Clean Solution"
dotnet clean
if ($LASTEXITCODE -eq 0) {
    Write-Success "Solution cleaned"
} else {
    Write-Error-Custom "Clean failed"
}

# ── Step 2: Check Package Issues ──────────────────────────
if ($CheckPackages) {
    Write-Step "Step 3: Checking Package Issues"
    
    Write-Info "Checking for outdated packages..."
    dotnet list package --outdated
    
    Write-Info "Checking for vulnerable packages..."
    dotnet list package --vulnerable
    
    Write-Info "Checking for deprecated packages..."
    dotnet list package --deprecated
}

# ── Step 3: Restore Packages ──────────────────────────────
Write-Step "Step 4: Restoring NuGet Packages"
dotnet restore
if ($LASTEXITCODE -eq 0) {
    Write-Success "Packages restored"
} else {
    Write-Error-Custom "Restore failed"
}

# ── Step 4: Fix EF Core Design Package ────────────────────
if ($FixEFCore) {
    Write-Step "Step 5: Adding Entity Framework Core Design Package"
    
    $infraProject = "$SolutionRoot\Devken.CBC.SchoolManagement.Infrastructure\Devken.CBC.SchoolManagement.Infrastructure.csproj"
    
    if (Test-Path $infraProject) {
        Write-Info "Adding Microsoft.EntityFrameworkCore.Design to Infrastructure project..."
        dotnet add $infraProject package Microsoft.EntityFrameworkCore.Design
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "EF Core Design package added"
        } else {
            Write-Error-Custom "Failed to add package"
        }
    } else {
        Write-Error-Custom "Infrastructure project not found at: $infraProject"
    }
}

# ── Step 5: Rebuild Solution ──────────────────────────────
Write-Step "Step 6: Rebuilding Solution"
dotnet build
if ($LASTEXITCODE -eq 0) {
    Write-Success "Build successful!"
} else {
    Write-Error-Custom "Build failed - check errors above"
}

# ── Summary ───────────────────────────────────────────────
Write-Step "Summary"

if ($LASTEXITCODE -eq 0) {
    Write-Success "All steps completed successfully!"
    Write-Info "Try running your application now."
    Write-Info "`nIf the error persists:"
    Write-Info "  1. Replace Program.cs with Program_ErrorCatcher.cs"
    Write-Info "  2. Add StartupErrorHandler.cs to your project"
    Write-Info "  3. Run the app to see detailed error information"
} else {
    Write-Error-Custom "Some steps failed. Review the output above."
}

Pop-Location

# ── Usage Instructions ────────────────────────────────────
Write-Host "`n" -NoNewline
Write-Host "💡 Usage Examples:" -ForegroundColor Yellow
Write-Host "  .\Fix-ReflectionError.ps1              # Basic clean and rebuild"
Write-Host "  .\Fix-ReflectionError.ps1 -FullClean   # Deep clean (delete bin/obj)"
Write-Host "  .\Fix-ReflectionError.ps1 -CheckPackages  # Check for package issues"
Write-Host "  .\Fix-ReflectionError.ps1 -FixEFCore   # Add EF Core Design package"
Write-Host "  .\Fix-ReflectionError.ps1 -FullClean -FixEFCore  # All fixes"
Write-Host "`n"

Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
