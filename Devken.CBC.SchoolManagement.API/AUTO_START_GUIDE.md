# 🚀 Auto-Start System for DevKen School Management

This solution provides **automatic database migration** and **concurrent API + Angular startup**.

## 📦 What's Included

1. **DatabaseMigrationService.cs** - Automatic migration service
2. **Program.cs** - Updated startup with auto-migration
3. **Start-DevKenSystem.ps1** - PowerShell launcher script
4. **START-SYSTEM.bat** - Double-click launcher
5. **launchSettings.json** - Visual Studio integration

---

## 🎯 Features

✅ **Auto-apply database migrations** on API startup  
✅ **Start both API and Angular** with one command  
✅ **Check for pending migrations** before startup  
✅ **Colored console output** for better visibility  
✅ **Error handling** with detailed logging  
✅ **Visual Studio integration** for easy debugging

---

## 📁 File Placement

Place files in these locations:

```
C:\Users\ngeti\source\repos\DevKen.School.System\
├── START-SYSTEM.bat                          ← Root of solution
├── Start-DevKenSystem.ps1                    ← Root of solution
│
├── Devken.CBC.SchoolManagement.API\
│   ├── Program.cs                            ← Replace existing
│   ├── Services\
│   │   └── DatabaseMigrationService.cs       ← Create new folder & file
│   └── Properties\
│       └── launchSettings.json               ← Replace existing
│
└── Devken.CBC.SchoolManagement.Infrastructure\
    └── (your existing structure)
```

---

## 🚀 Usage Options

### Option 1: Double-Click Launcher (Easiest)
1. Place `START-SYSTEM.bat` in your solution root
2. Double-click `START-SYSTEM.bat`
3. Both API and Angular will start automatically!

### Option 2: PowerShell Script
```powershell
# Run everything (migrations + API + UI)
.\Start-DevKenSystem.ps1

# Run only API
.\Start-DevKenSystem.ps1 -ApiOnly

# Run only UI
.\Start-DevKenSystem.ps1 -UIOnly

# Skip migrations
.\Start-DevKenSystem.ps1 -SkipMigration
```

### Option 3: Visual Studio
1. Replace `launchSettings.json` in your API project's Properties folder
2. In Visual Studio, select **"API + Angular"** from the launch profile dropdown
3. Press F5 or click Run
4. Both projects start automatically!

### Option 4: Manual (Traditional)
```bash
# Terminal 1 - API
cd C:\Users\ngeti\source\repos\DevKen.School.System\Devken.CBC.SchoolManagement.API
dotnet run

# Terminal 2 - Angular
cd C:\Users\ngeti\source\repos\DevKen.School.System\Devken.CBC.SchoolManagement.API\Devken.CBC.SchoolManagment.UI\School-System-UI
npm start
```

---

## 🔧 How Auto-Migration Works

### On API Startup (Program.cs)
```csharp
using (var scope = app.Services.CreateScope())
{
    var migrationService = scope.ServiceProvider
        .GetRequiredService<DatabaseMigrationService>();
    await migrationService.MigrateAsync();
}
```

### What Happens:
1. ✅ Checks for pending migrations
2. ✅ Applies them automatically
3. ✅ Logs everything to console
4. ✅ Continues if migrations succeed
5. ⚠️ Logs error if migrations fail (app continues)

### To Stop App on Migration Failure
In `Program.cs`, uncomment this line:
```csharp
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "❌ Failed to apply database migrations on startup!");
    
    throw; // ← Uncomment this to stop the app if migrations fail
}
```

---

## 📝 Configuration

### Update Solution Path (if needed)
Edit `Start-DevKenSystem.ps1`:
```powershell
# Line 17-20: Update these paths if different
$SolutionRoot = "C:\Users\ngeti\source\repos\DevKen.School.System"
$ApiProject = "$SolutionRoot\Devken.CBC.SchoolManagement.API"
$InfraProject = "$SolutionRoot\Devken.CBC.SchoolManagement.Infrastructure"
$AngularProject = "$ApiProject\Devken.CBC.SchoolManagment.UI\School-System-UI"
```

### Update API/Angular Ports (if needed)
Edit `launchSettings.json`:
```json
"applicationUrl": "https://localhost:7001;http://localhost:5000"  // API ports
```

Edit Angular `angular.json` or `package.json`:
```json
"start": "ng serve --port 4200"  // Angular port
```

---

## 🎨 Console Output Examples

### Successful Startup
```
═══════════════════════════════════════════════════════════
 Step 1: Database Migrations
═══════════════════════════════════════════════════════════

ℹ️  Checking for pending migrations...
✅ Migration check completed
ℹ️  Applying database migrations...
✅ Database migrations applied successfully!

═══════════════════════════════════════════════════════════
 Step 2: Starting .NET API
═══════════════════════════════════════════════════════════

ℹ️  Starting API at C:\...\Devken.CBC.SchoolManagement.API
✅ API process started!

═══════════════════════════════════════════════════════════
 Step 3: Starting Angular UI
═══════════════════════════════════════════════════════════

ℹ️  Starting Angular at C:\...\School-System-UI
✅ Angular UI process started!
ℹ️  Angular dev server will be available at: http://localhost:4200
```

### With Pending Migrations
```
🔍 Checking for pending database migrations...
⚠️  Found 3 pending migration(s):
   - 20250202120000_InitialMigration
   - 20250202130000_AddUserRoles
   - 20250202140000_AddRefreshTokens
🚀 Applying migrations...
✅ Database migrations applied successfully!
✅ Database connection verified.
```

---

## 🛠️ Troubleshooting

### Issue: "npm not found" or Angular doesn't start
**Solution:** Install Node.js and npm, then run:
```bash
cd C:\Users\ngeti\source\repos\DevKen.School.System\Devken.CBC.SchoolManagement.API\Devken.CBC.SchoolManagment.UI\School-System-UI
npm install
```

### Issue: PowerShell script won't run (execution policy)
**Solution:** Run PowerShell as Administrator and execute:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Issue: Port already in use
**Solution:** Change ports in:
- `launchSettings.json` (API)
- `angular.json` or `package.json` (Angular)

### Issue: Database connection fails
**Solution:** Check `appsettings.json` connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=SchoolManagement;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### Issue: Migrations fail on startup
**Solution 1:** Check migration files in `Infrastructure/Persistence/Migrations`  
**Solution 2:** Manually run migrations:
```bash
dotnet ef database update --project Devken.CBC.SchoolManagement.Infrastructure --startup-project Devken.CBC.SchoolManagement.API
```

---

## 🔒 Production Considerations

### ⚠️ Auto-Migration in Production
**NOT RECOMMENDED** for production environments!

For production:
1. Disable auto-migration in `Program.cs`
2. Run migrations manually during deployment
3. Use CI/CD pipelines for controlled migrations

### Recommended Production Approach
```csharp
// Program.cs - Production Configuration
if (app.Environment.IsDevelopment())
{
    // Only auto-migrate in development
    using (var scope = app.Services.CreateScope())
    {
        var migrationService = scope.ServiceProvider
            .GetRequiredService<DatabaseMigrationService>();
        await migrationService.MigrateAsync();
    }
}
```

---

## 📚 Additional Commands

### Create New Migration
```bash
dotnet ef migrations add MigrationName --project Devken.CBC.SchoolManagement.Infrastructure --startup-project Devken.CBC.SchoolManagement.API --output-dir Persistence/Migrations
```

### Remove Last Migration
```bash
dotnet ef migrations remove --project Devken.CBC.SchoolManagement.Infrastructure --startup-project Devken.CBC.SchoolManagement.API
```

### View Migration History
```bash
dotnet ef migrations list --project Devken.CBC.SchoolManagement.Infrastructure --startup-project Devken.CBC.SchoolManagement.API
```

### Revert to Specific Migration
```bash
dotnet ef database update MigrationName --project Devken.CBC.SchoolManagement.Infrastructure --startup-project Devken.CBC.SchoolManagement.API
```

---

## ✨ Benefits Summary

| Feature | Before | After |
|---------|--------|-------|
| **Migration** | Manual command each time | Automatic on startup |
| **Start API** | Separate terminal | One-click launch |
| **Start Angular** | Separate terminal | One-click launch |
| **Visual Studio** | Manual process | Integrated launcher |
| **Error Handling** | Basic | Comprehensive logging |
| **Developer Experience** | 😐 Manual steps | 😊 Automated workflow |

---

## 🎉 You're All Set!

Now you can start your entire development environment with:
- **One double-click** on `START-SYSTEM.bat`
- **One F5 press** in Visual Studio
- **One PowerShell command**

Happy coding! 🚀

---

## 📞 Need Help?

If you encounter issues:
1. Check the console output for error messages
2. Verify file paths in `Start-DevKenSystem.ps1`
3. Ensure all dependencies are installed (dotnet SDK, Node.js, npm)
4. Check connection strings in `appsettings.json`
