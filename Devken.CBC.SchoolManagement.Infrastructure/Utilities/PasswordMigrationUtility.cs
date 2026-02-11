using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Devken.CBC.SchoolManagement.Infrastructure.Utilities
{
    /// <summary>
    /// Utility to migrate existing passwords to BCrypt format
    /// This should be run once during deployment or as a background job
    /// </summary>
    public class PasswordMigrationUtility
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHashingService _passwordHashingService;
        private readonly ILogger<PasswordMigrationUtility> _logger;

        public PasswordMigrationUtility(
            AppDbContext context,
            IPasswordHashingService passwordHashingService,
            ILogger<PasswordMigrationUtility> logger)
        {
            _context = context;
            _passwordHashingService = passwordHashingService;
            _logger = logger;
        }

        /// <summary>
        /// Checks how many users need password migration
        /// </summary>
        public async Task<int> CountPasswordsNeedingMigrationAsync()
        {
            if (_passwordHashingService is not BCryptPasswordHashingService bcryptService)
                return 0;

            var users = await _context.Users.Where(u => u.IsActive).ToListAsync();
            return users.Count(u => bcryptService.NeedsRehash(u.PasswordHash));
        }

        /// <summary>
        /// Checks how many super admins need password migration
        /// </summary>
        public async Task<int> CountSuperAdminPasswordsNeedingMigrationAsync()
        {
            if (_passwordHashingService is not BCryptPasswordHashingService bcryptService)
                return 0;

            var admins = await _context.SuperAdmins.Where(a => a.IsActive).ToListAsync();
            return admins.Count(a => bcryptService.NeedsRehash(a.PasswordHash));
        }

        /// <summary>
        /// Note: This method cannot migrate passwords because we don't have the plain text passwords.
        /// Password migration happens automatically on login when users authenticate.
        /// This method is here for documentation purposes.
        /// </summary>
        public async Task<MigrationReport> GetMigrationStatusAsync()
        {
            var userCount = await CountPasswordsNeedingMigrationAsync();
            var superAdminCount = await CountSuperAdminPasswordsNeedingMigrationAsync();

            var report = new MigrationReport
            {
                UsersNeedingMigration = userCount,
                SuperAdminsNeedingMigration = superAdminCount,
                TotalNeedingMigration = userCount + superAdminCount,
                MigrationStrategy = "Automatic on Login",
                Notes = "Passwords will be automatically migrated to BCrypt when users successfully log in. " +
                       "Old password formats (SHA256, ASP.NET Identity) are still supported for verification."
            };

            _logger.LogInformation(
                "Password Migration Status: {UserCount} users and {SuperAdminCount} super admins need migration",
                userCount, superAdminCount);

            return report;
        }

        /// <summary>
        /// If you have a list of test/default passwords, you can use this to force-migrate specific accounts
        /// WARNING: Only use this for test/development or with known default passwords
        /// </summary>
        public async Task<int> MigrateKnownPasswordsAsync(Dictionary<string, string> emailPasswordPairs)
        {
            int migratedCount = 0;

            foreach (var (email, password) in emailPasswordPairs)
            {
                try
                {
                    // Try regular users first
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                    if (user != null)
                    {
                        if (_passwordHashingService.VerifyPassword(password, user.PasswordHash))
                        {
                            user.PasswordHash = _passwordHashingService.HashPassword(password);
                            migratedCount++;
                            _logger.LogInformation("Migrated password for user {Email}", email);
                        }
                        continue;
                    }

                    // Try super admins
                    var admin = await _context.SuperAdmins.FirstOrDefaultAsync(a => a.Email == email);
                    if (admin != null)
                    {
                        if (_passwordHashingService.VerifyPassword(password, admin.PasswordHash))
                        {
                            admin.PasswordHash = _passwordHashingService.HashPassword(password);
                            migratedCount++;
                            _logger.LogInformation("Migrated password for super admin {Email}", email);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error migrating password for {Email}", email);
                }
            }

            if (migratedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return migratedCount;
        }
    }

    public class MigrationReport
    {
        public int UsersNeedingMigration { get; set; }
        public int SuperAdminsNeedingMigration { get; set; }
        public int TotalNeedingMigration { get; set; }
        public string MigrationStrategy { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}