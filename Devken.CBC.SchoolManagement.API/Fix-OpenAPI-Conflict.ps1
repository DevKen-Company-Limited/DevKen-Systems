# ═══════════════════════════════════════════════════════════
# Fix OpenAPI/Swagger Version Conflict
# ═══════════════════════════════════════════════════════════

$SolutionRoot = "C:\Users\ngeti\source\repos\DevKen.School.System"
$ApiProject = "$SolutionRoot\Devken.CBC.SchoolManagement.API\Devken.CBC.SchoolManagement.API.csproj"

Write-Host "`n🔧 Fixing OpenAPI Version Conflict" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Yellow

# Step 1: Remove old Swashbuckle packages
Write-Host "📦 Removing old Swashbuckle packages..." -ForegroundColor Cyan
dotnet remove $ApiProject package Swashbuckle.AspNetCore 2>$null
dotnet remove $ApiProject package Swashbuckle.AspNetCore.SwaggerGen 2>$null
dotnet remove $ApiProject package Swashbuckle.AspNetCore.SwaggerUI 2>$null
dotnet remove $ApiProject package Microsoft.OpenApi 2>$null

Write-Host "✅ Old packages removed`n" -ForegroundColor Green

# Step 2: Add latest Swashbuckle (includes correct Microsoft.OpenApi)
Write-Host "📦 Installing latest Swashbuckle.AspNetCore..." -ForegroundColor Cyan
dotnet add $ApiProject package Swashbuckle.AspNetCore --version 7.2.0

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Swashbuckle.AspNetCore 7.2.0 installed`n" -ForegroundColor Green
} else {
    Write-Host "❌ Failed to install package`n" -ForegroundColor Red
    exit 1
}

# Step 3: Clean solution
Write-Host "🧹 Cleaning solution..." -ForegroundColor Cyan
Push-Location $SolutionRoot
dotnet clean

Write-Host "✅ Solution cleaned`n" -ForegroundColor Green

# Step 4: Delete bin/obj folders
Write-Host "🗑️  Deleting bin/obj folders..." -ForegroundColor Cyan
$foldersToDelete = Get-ChildItem -Path $SolutionRoot -Include bin,obj -Recurse -Directory -Force
$count = ($foldersToDelete | Measure-Object).Count
$foldersToDelete | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "✅ Deleted $count folders`n" -ForegroundColor Green

# Step 5: Restore packages
Write-Host "📦 Restoring packages..." -ForegroundColor Cyan
dotnet restore
Write-Host "✅ Packages restored`n" -ForegroundColor Green

# Step 6: Rebuild
Write-Host "🔨 Building solution..." -ForegroundColor Cyan
dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ BUILD SUCCESSFUL!" -ForegroundColor Green
    Write-Host "`n🎉 The OpenAPI version conflict is fixed!" -ForegroundColor Green
    Write-Host "You can now run your application without the ReflectionTypeLoadException.`n" -ForegroundColor White
} else {
    Write-Host "`n❌ Build failed. Check errors above." -ForegroundColor Red
}

Pop-Location

Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
