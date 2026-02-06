using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Seed
{
    /// <summary>
    /// Seeds the database with initial SuperAdmin and default school data
    /// </summary>
    public static class DatabaseSeeder
    {
        private const string DefaultSuperAdminEmail = "superadmin@devken.com";
        private const string DefaultSuperAdminPassword = "SuperAdmin@123";

        private const string DefaultSchoolSlug = "default-school";
        private const string DefaultSchoolName = "Default School";
        private const string DefaultSchoolEmail = "info@defaultschool.com";
        private const string DefaultSchoolPhone = "+254700000000";
        private const string DefaultSchoolAddress = "Default Address, Nairobi, Kenya";

        private const string DefaultSchoolAdminEmail = "admin@defaultschool.com";
        private const string DefaultSchoolAdminPassword = "Admin@123";
        private const string DefaultSchoolAdminFirstName = "School";
        private const string DefaultSchoolAdminLastName = "Administrator";

        /// <summary>
        /// Seeds the database with SuperAdmin and default school
        /// </summary>
        public static async Task SeedDatabaseAsync(this AppDbContext dbContext, ILogger? logger = null)
        {
            if (dbContext == null)
                throw new ArgumentNullException(nameof(dbContext));

            logger?.LogInformation("Starting database seeding...");

            // ── 1. SEED SUPERADMIN ────────────────────────────────
            await SeedSuperAdminAsync(dbContext, logger);

            // ── 2. SEED DEFAULT SCHOOL ────────────────────────────
            var defaultSchool = await SeedDefaultSchoolAsync(dbContext, logger);

            // ── 3. SEED DEFAULT SCHOOL ADMIN ──────────────────────
            await SeedDefaultSchoolAdminAsync(dbContext, defaultSchool, logger);

            logger?.LogInformation("Database seeding completed successfully.");
        }

        /// <summary>
        /// Seeds the SuperAdmin account
        /// </summary>
        private static async Task SeedSuperAdminAsync(AppDbContext dbContext, ILogger? logger)
        {
            var superAdminExists = await dbContext.SuperAdmins
                .AnyAsync(sa => sa.Email == DefaultSuperAdminEmail);

            if (!superAdminExists)
            {
                var superAdmin = new SuperAdmin
                {
                    Id = Guid.NewGuid(),
                    Email = DefaultSuperAdminEmail,
                    FirstName = "Super",
                    LastName = "Admin",
                    IsActive = true,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                };

                superAdmin.PasswordHash = new PasswordHasher<SuperAdmin>()
                    .HashPassword(superAdmin, DefaultSuperAdminPassword);

                dbContext.SuperAdmins.Add(superAdmin);
                await dbContext.SaveChangesAsync();

                logger?.LogInformation(
                    "✅ Seeded SuperAdmin with email: {Email}",
                    DefaultSuperAdminEmail);
            }
            else
            {
                logger?.LogInformation(
                    "ℹ️ SuperAdmin already exists with email: {Email}",
                    DefaultSuperAdminEmail);
            }
        }

        /// <summary>
        /// Seeds the default school
        /// </summary>
        private static async Task<School> SeedDefaultSchoolAsync(AppDbContext dbContext, ILogger? logger)
        {
            var defaultSchool = await dbContext.Schools
                .FirstOrDefaultAsync(s => s.SlugName == DefaultSchoolSlug);

            if (defaultSchool == null)
            {
                defaultSchool = new School
                {
                    Id = Guid.NewGuid(),
                    Name = DefaultSchoolName,
                    SlugName = DefaultSchoolSlug,
                    Email = DefaultSchoolEmail,
                    PhoneNumber = DefaultSchoolPhone,
                    Address = DefaultSchoolAddress,
                    IsActive = true,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                };

                dbContext.Schools.Add(defaultSchool);
                await dbContext.SaveChangesAsync();

                logger?.LogInformation(
                    "✅ Seeded default school: {Name} (ID: {Id})",
                    DefaultSchoolName,
                    defaultSchool.Id);
            }
            else
            {
                logger?.LogInformation(
                    "ℹ️ Default school already exists: {Name} (ID: {Id})",
                    defaultSchool.Name,
                    defaultSchool.Id);
            }

            return defaultSchool;
        }

        /// <summary>
        /// Seeds the default school administrator
        /// </summary>
        private static async Task SeedDefaultSchoolAdminAsync(
            AppDbContext dbContext,
            School defaultSchool,
            ILogger? logger)
        {
            var schoolAdminExists = await dbContext.Users
                .AnyAsync(u => u.Email == DefaultSchoolAdminEmail && u.TenantId == defaultSchool.Id);

            if (!schoolAdminExists)
            {
                // Create the user
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = DefaultSchoolAdminEmail,
                    FirstName = DefaultSchoolAdminFirstName,
                    LastName = DefaultSchoolAdminLastName,
                    TenantId = defaultSchool.Id,  // ✅ Set TenantId
                    IsActive = true,
                    IsEmailVerified = true,
                    RequirePasswordChange = false,  // ✅ Set to false for default admin
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                };

                user.PasswordHash = new PasswordHasher<User>()
                    .HashPassword(user, DefaultSchoolAdminPassword);

                dbContext.Users.Add(user);
                await dbContext.SaveChangesAsync();

                logger?.LogInformation(
                    "✅ Seeded default school admin: {Email} (ID: {Id})",
                    DefaultSchoolAdminEmail,
                    user.Id);

                // The SchoolAdmin role and permissions will be assigned by PermissionSeedService
                // which is called separately in SeedExtensions
            }
            else
            {
                logger?.LogInformation(
                    "ℹ️ Default school admin already exists: {Email}",
                    DefaultSchoolAdminEmail);
            }
        }

        /// <summary>
        /// Seeds a new school with an admin user
        /// </summary>
        /// <param name="dbContext">Database context</param>
        /// <param name="schoolName">Name of the school</param>
        /// <param name="schoolSlug">URL-friendly slug for the school</param>
        /// <param name="schoolEmail">School email</param>
        /// <param name="schoolPhone">School phone number</param>
        /// <param name="schoolAddress">School address</param>
        /// <param name="adminEmail">School admin email</param>
        /// <param name="adminPassword">School admin password</param>
        /// <param name="adminFullName">School admin full name</param>
        /// <param name="adminPhone">School admin phone</param>
        /// <param name="logger">Logger instance</param>
        /// <returns>The created school</returns>
        public static async Task<School> SeedNewSchoolAsync(
            AppDbContext dbContext,
            string schoolName,
            string schoolSlug,
            string schoolEmail,
            string schoolPhone,
            string schoolAddress,
            string adminEmail,
            string adminPassword,
            string adminFullName,
            string? adminPhone = null,
            ILogger? logger = null)
        {
            // Check if school already exists
            var existingSchool = await dbContext.Schools
                .FirstOrDefaultAsync(s => s.SlugName == schoolSlug);

            if (existingSchool != null)
            {
                logger?.LogWarning(
                    "School with slug '{Slug}' already exists",
                    schoolSlug);
                return existingSchool;
            }

            // Create the school
            var school = new School
            {
                Id = Guid.NewGuid(),
                Name = schoolName,
                SlugName = schoolSlug,
                Email = schoolEmail,
                PhoneNumber = schoolPhone,
                Address = schoolAddress,
                IsActive = true,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow
            };

            dbContext.Schools.Add(school);
            await dbContext.SaveChangesAsync();

            logger?.LogInformation(
                "✅ Created new school: {Name} (ID: {Id})",
                schoolName,
                school.Id);

            // Parse admin name
            var nameParts = adminFullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var firstName = nameParts.Length > 0 ? nameParts[0] : "Admin";
            var lastName = nameParts.Length > 1
                ? string.Join(" ", nameParts.Skip(1))
                : "User";

            // Check if admin user already exists for this school
            var existingAdmin = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == adminEmail && u.TenantId == school.Id);

            if (existingAdmin != null)
            {
                logger?.LogInformation(
                    "ℹ️ Admin user already exists: {Email} for school {SchoolName}",
                    adminEmail,
                    schoolName);
                return school;
            }

            // Create school admin user
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Email = adminEmail,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = adminPhone,
                TenantId = school.Id,  // ✅ Set TenantId
                IsActive = true,
                IsEmailVerified = true,
                RequirePasswordChange = false,  // ✅ Set to false for seeded admin
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow
            };

            adminUser.PasswordHash = new PasswordHasher<User>()
                .HashPassword(adminUser, adminPassword);

            dbContext.Users.Add(adminUser);
            await dbContext.SaveChangesAsync();

            logger?.LogInformation(
                "✅ Created school admin: {Email} for school {SchoolName}",
                adminEmail,
                schoolName);

            return school;
        }
    }
}