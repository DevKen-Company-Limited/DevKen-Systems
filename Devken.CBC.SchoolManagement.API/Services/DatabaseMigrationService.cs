using Microsoft.EntityFrameworkCore;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;

namespace Devken.CBC.SchoolManagement.API.Services
{
    /// <summary>
    /// Service to automatically apply database migrations on application startup
    /// </summary>
    public class DatabaseMigrationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseMigrationService> _logger;

        public DatabaseMigrationService(
            IServiceProvider serviceProvider,
            ILogger<DatabaseMigrationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Applies pending migrations and ensures database is up to date
        /// </summary>
        public async Task MigrateAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                _logger.LogInformation("🔍 Checking for pending database migrations...");

                // Get pending migrations
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                var pendingList = pendingMigrations.ToList();

                if (pendingList.Any())
                {
                    _logger.LogWarning("⚠️  Found {Count} pending migration(s):", pendingList.Count);
                    foreach (var migration in pendingList)
                    {
                        _logger.LogWarning("   - {Migration}", migration);
                    }

                    _logger.LogInformation("🚀 Applying migrations...");
                    await context.Database.MigrateAsync();
                    _logger.LogInformation("✅ Database migrations applied successfully!");
                }
                else
                {
                    _logger.LogInformation("✅ Database is up to date. No pending migrations.");
                }

                // Verify connection
                var canConnect = await context.Database.CanConnectAsync();
                if (canConnect)
                {
                    _logger.LogInformation("✅ Database connection verified.");
                }
                else
                {
                    _logger.LogError("❌ Unable to connect to database!");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error applying database migrations: {Message}", ex.Message);
                throw;
            }
        }
    }
}