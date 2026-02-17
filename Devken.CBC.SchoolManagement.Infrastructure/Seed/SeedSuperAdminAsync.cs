using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Enums;
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
    /// Seeds the database with initial SuperAdmin and default school data.
    /// </summary>
    public static class DatabaseSeeder
    {
        #region SuperAdmin Constants

        private const string DefaultSuperAdminEmail = "superadmin@devken.com";
        private const string DefaultSuperAdminPassword = "SuperAdmin@123";

        #endregion

        #region Default School Constants

        private const string DefaultSchoolSlug = "default-school";
        private const string DefaultSchoolName = "Default School";
        private const string DefaultSchoolEmail = "info@defaultschool.com";
        private const string DefaultSchoolPhone = "+254700000000";
        private const string DefaultSchoolAddress = "Default Address, Nairobi, Kenya";
        private const string DefaultSchoolCounty = "Nairobi";
        private const string DefaultSchoolSubCounty = "Westlands";
        private const string DefaultSchoolRegNumber = "REG/2024/001";
        private const SchoolType DefaultSchoolType = SchoolType.Public;
        private const SchoolCategory DefaultSchoolCategory = SchoolCategory.Day;

        #endregion

        #region Default School Admin Constants

        private const string DefaultSchoolAdminEmail = "admin@defaultschool.com";
        private const string DefaultSchoolAdminPassword = "Admin@123";
        private const string DefaultSchoolAdminFirstName = "School";
        private const string DefaultSchoolAdminLastName = "Administrator";

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        // ENTRY POINT
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Seeds the database with SuperAdmin, default school, and default school admin.
        /// </summary>
        public static async Task SeedDatabaseAsync(this AppDbContext dbContext, ILogger? logger = null)
        {
            if (dbContext == null)
                throw new ArgumentNullException(nameof(dbContext));

            logger?.LogInformation("Starting database seeding...");

            // 1. SuperAdmin
            await SeedSuperAdminAsync(dbContext, logger);

            // 2. Default school
            var defaultSchool = await SeedDefaultSchoolAsync(dbContext, logger);

            // 3. Default school admin
            //    ⚠️ School must be saved before the User is inserted (FK on SchoolId)
            await SeedDefaultSchoolAdminAsync(dbContext, defaultSchool, logger);

            logger?.LogInformation("Database seeding completed successfully.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE SEEDERS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Seeds the SuperAdmin account.
        /// </summary>
        private static async Task SeedSuperAdminAsync(AppDbContext dbContext, ILogger? logger)
        {
            var exists = await dbContext.SuperAdmins
                .AnyAsync(sa => sa.Email == DefaultSuperAdminEmail);

            if (!exists)
            {
                var superAdmin = new SuperAdmin
                {
                    Id = Guid.NewGuid(),
                    Email = DefaultSuperAdminEmail,
                    FirstName = "Super",
                    LastName = "Admin",
                    IsActive = true,
                    CreatedOn = DateTime.UtcNow
                };

                superAdmin.PasswordHash = new PasswordHasher<SuperAdmin>()
                    .HashPassword(superAdmin, DefaultSuperAdminPassword);

                dbContext.SuperAdmins.Add(superAdmin);
                await dbContext.SaveChangesAsync();

                logger?.LogInformation(
                    "✅ Seeded SuperAdmin: {Email}", DefaultSuperAdminEmail);
            }
            else
            {
                logger?.LogInformation(
                    "ℹ️ SuperAdmin already exists: {Email}", DefaultSuperAdminEmail);
            }
        }

        /// <summary>
        /// Seeds the default school. Must be committed before any Users are inserted.
        /// </summary>
        private static async Task<School> SeedDefaultSchoolAsync(AppDbContext dbContext, ILogger? logger)
        {
            var school = await dbContext.Schools
                .FirstOrDefaultAsync(s => s.SlugName == DefaultSchoolSlug);

            if (school == null)
            {
                school = new School
                {
                    Id = Guid.NewGuid(),
                    SlugName = DefaultSchoolSlug,
                    Name = DefaultSchoolName,
                    RegistrationNumber = DefaultSchoolRegNumber,
                    Email = DefaultSchoolEmail,
                    PhoneNumber = DefaultSchoolPhone,
                    Address = DefaultSchoolAddress,
                    County = DefaultSchoolCounty,
                    SubCounty = DefaultSchoolSubCounty,
                    SchoolType = DefaultSchoolType,
                    Category = DefaultSchoolCategory,
                    IsActive = true,
                    CreatedOn = DateTime.UtcNow
                };

                dbContext.Schools.Add(school);

                // ⚠️ CRITICAL: SaveChanges here so the School row exists in the DB
                // before any User with SchoolId FK is inserted.
                await dbContext.SaveChangesAsync();

                logger?.LogInformation(
                    "✅ Seeded default school: {Name} (ID: {Id}, Type: {Type}, Category: {Category})",
                    DefaultSchoolName, school.Id, DefaultSchoolType, DefaultSchoolCategory);
            }
            else
            {
                logger?.LogInformation(
                    "ℹ️ Default school already exists: {Name} (ID: {Id})",
                    school.Name, school.Id);
            }

            return school;
        }

        /// <summary>
        /// Seeds the default school administrator.
        /// Requires the school to already be persisted (FK: Users.SchoolId → Schools.Id).
        /// </summary>
        private static async Task SeedDefaultSchoolAdminAsync(
            AppDbContext dbContext,
            School defaultSchool,
            ILogger? logger)
        {
            var exists = await dbContext.Users
                .AnyAsync(u => u.Email == DefaultSchoolAdminEmail && u.TenantId == defaultSchool.Id);

            if (!exists)
            {
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = DefaultSchoolAdminEmail,
                    FirstName = DefaultSchoolAdminFirstName,
                    LastName = DefaultSchoolAdminLastName,
                    // ✅ Both SchoolId AND TenantId must point to the school's PK.
                    //    SchoolId is the FK enforced by the DB constraint.
                    //    TenantId is the application-level tenant discriminator.
                    SchoolId = defaultSchool.Id,
                    TenantId = defaultSchool.Id,
                    IsActive = true,
                    IsEmailVerified = true,
                    RequirePasswordChange = false,
                    CreatedOn = DateTime.UtcNow
                };

                user.PasswordHash = new PasswordHasher<User>()
                    .HashPassword(user, DefaultSchoolAdminPassword);

                dbContext.Users.Add(user);
                await dbContext.SaveChangesAsync();

                logger?.LogInformation(
                    "✅ Seeded default school admin: {Email} (ID: {Id})",
                    DefaultSchoolAdminEmail, user.Id);

                // Role and permission assignment is handled separately by PermissionSeedService
            }
            else
            {
                logger?.LogInformation(
                    "ℹ️ Default school admin already exists: {Email}", DefaultSchoolAdminEmail);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUBLIC UTILITY: SEED A NEW SCHOOL
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new school and its admin user programmatically.
        /// Safe to call multiple times — skips if the school slug already exists.
        /// </summary>
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
            string? schoolCounty = null,
            string? schoolSubCounty = null,
            string? registrationNumber = null,
            string? knecCenterCode = null,
            string? kraPin = null,
            SchoolType schoolType = SchoolType.Public,
            SchoolCategory category = SchoolCategory.Day,
            string? adminPhone = null,
            ILogger? logger = null)
        {
            // ── School ────────────────────────────────────────────────────────
            var school = await dbContext.Schools
                .FirstOrDefaultAsync(s => s.SlugName == schoolSlug);

            if (school != null)
            {
                logger?.LogWarning(
                    "School with slug '{Slug}' already exists — skipping creation.", schoolSlug);
                return school;
            }

            school = new School
            {
                Id = Guid.NewGuid(),
                SlugName = schoolSlug.Trim(),
                Name = schoolName.Trim(),
                RegistrationNumber = registrationNumber?.Trim(),
                KnecCenterCode = knecCenterCode?.Trim(),
                KraPin = kraPin?.Trim(),
                Email = schoolEmail.Trim(),
                PhoneNumber = schoolPhone.Trim(),
                Address = schoolAddress.Trim(),
                County = schoolCounty?.Trim(),
                SubCounty = schoolSubCounty?.Trim(),
                SchoolType = schoolType,
                Category = category,
                IsActive = true,
                CreatedOn = DateTime.UtcNow
            };

            dbContext.Schools.Add(school);

            // ⚠️ CRITICAL: Commit the school row BEFORE inserting the admin User
            // to satisfy the FK_Users_Schools_SchoolId constraint.
            await dbContext.SaveChangesAsync();

            logger?.LogInformation(
                "✅ Created school: {Name} (ID: {Id}, Type: {Type}, Category: {Category})",
                schoolName, school.Id, schoolType, category);

            // ── Admin User ────────────────────────────────────────────────────
            var existingAdmin = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == adminEmail && u.TenantId == school.Id);

            if (existingAdmin != null)
            {
                logger?.LogInformation(
                    "ℹ️ Admin user already exists: {Email} for school '{SchoolName}'",
                    adminEmail, schoolName);
                return school;
            }

            var nameParts = adminFullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var firstName = nameParts.Length > 0 ? nameParts[0] : "Admin";
            var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "User";

            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Email = adminEmail.Trim(),
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = adminPhone?.Trim(),
                // ✅ Both must be set — SchoolId satisfies the DB FK,
                //    TenantId is the application-level tenant discriminator.
                SchoolId = school.Id,
                TenantId = school.Id,
                IsActive = true,
                IsEmailVerified = true,
                RequirePasswordChange = false,
                CreatedOn = DateTime.UtcNow
            };

            adminUser.PasswordHash = new PasswordHasher<User>()
                .HashPassword(adminUser, adminPassword);

            dbContext.Users.Add(adminUser);
            await dbContext.SaveChangesAsync();

            logger?.LogInformation(
                "✅ Created school admin: {Email} for school '{SchoolName}'",
                adminEmail, schoolName);

            return school;
        }
    }
}