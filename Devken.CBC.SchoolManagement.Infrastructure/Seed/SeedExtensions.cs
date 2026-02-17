using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Seed
{
    /// <summary>
    /// Extension methods for seeding application data.
    /// </summary>
    public static class SeedExtensions
    {
        // ─────────────────────────────────────────────────────────────────────
        // DEFAULT DATA SEED (called once at application startup)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Seeds all default data: SuperAdmin, default school, permissions, roles, and academic data.
        /// </summary>
        public static async Task SeedDefaultDataAsync(this IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;
            var logger = sp.GetService<ILogger<AppDbContext>>();
            var dbContext = sp.GetRequiredService<AppDbContext>();
            var permissionSeeder = sp.GetRequiredService<IPermissionSeedService>();

            try
            {
                logger?.LogInformation("=== Starting complete database seeding ===");

                // 1. SuperAdmin + default school + default school admin
                await dbContext.SeedDatabaseAsync(logger);

                // 2. Retrieve the default school
                var defaultSchool = await dbContext.Schools
                    .FirstOrDefaultAsync(s => s.SlugName == "default-school");

                if (defaultSchool == null)
                {
                    logger?.LogError("Default school not found after seeding — aborting.");
                    return;
                }

                // 3. Permissions & roles
                logger?.LogInformation(
                    "Seeding permissions and roles for school: {SchoolId} ({Name})",
                    defaultSchool.Id, defaultSchool.Name);

                await permissionSeeder.SeedPermissionsAndRolesAsync(defaultSchool.Id);

                // 4. Assign SchoolAdmin role to the default admin user
                await AssignSchoolAdminRoleAsync(dbContext, defaultSchool.Id, "admin@defaultschool.com", logger);

                // 5. Academic year & classes
                logger?.LogInformation(
                    "Seeding academic data for school: {SchoolId}", defaultSchool.Id);

                var academicSeeder = new AcademicSeeder(dbContext, logger);
                await academicSeeder.SeedAcademicDataAsync(defaultSchool.Id);

                logger?.LogInformation("=== Database seeding completed successfully ===");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error during database seeding.");
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // NEW SCHOOL SETUP (called on demand when provisioning a new school)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a fully configured school: entity, admin user, permissions,
        /// roles, role assignment, and academic seed data.
        /// Safe to call multiple times — skips any step that is already complete.
        /// </summary>
        /// <param name="services">Application service provider.</param>
        /// <param name="schoolName">Full school name.</param>
        /// <param name="schoolSlug">URL-friendly unique slug.</param>
        /// <param name="schoolEmail">School contact email.</param>
        /// <param name="schoolPhone">School contact phone number.</param>
        /// <param name="schoolAddress">Physical address.</param>
        /// <param name="adminEmail">Admin user email.</param>
        /// <param name="adminPassword">Admin user plain-text password (will be hashed).</param>
        /// <param name="adminFullName">Admin full name (split on first space into first/last).</param>
        /// <param name="schoolCounty">County (optional).</param>
        /// <param name="schoolSubCounty">Sub-county (optional).</param>
        /// <param name="registrationNumber">Government registration number (optional).</param>
        /// <param name="knecCenterCode">KNEC center code (optional).</param>
        /// <param name="kraPin">KRA PIN (optional).</param>
        /// <param name="schoolType">School type — defaults to <see cref="SchoolType.Public"/>.</param>
        /// <param name="category">School category — defaults to <see cref="SchoolCategory.Day"/>.</param>
        /// <param name="adminPhone">Admin phone number (optional).</param>
        public static async Task SeedNewSchoolWithSetupAsync(
            this IServiceProvider services,
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
            string? adminPhone = null)
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;
            var logger = sp.GetService<ILogger<AppDbContext>>();
            var dbContext = sp.GetRequiredService<AppDbContext>();
            var permissionSeeder = sp.GetRequiredService<IPermissionSeedService>();

            try
            {
                logger?.LogInformation("=== Provisioning new school: {SchoolName} ===", schoolName);

                // 1. School entity + admin user
                var school = await DatabaseSeeder.SeedNewSchoolAsync(
                    dbContext,
                    schoolName: schoolName,
                    schoolSlug: schoolSlug,
                    schoolEmail: schoolEmail,
                    schoolPhone: schoolPhone,
                    schoolAddress: schoolAddress,
                    adminEmail: adminEmail,
                    adminPassword: adminPassword,
                    adminFullName: adminFullName,
                    schoolCounty: schoolCounty,
                    schoolSubCounty: schoolSubCounty,
                    registrationNumber: registrationNumber,
                    knecCenterCode: knecCenterCode,
                    kraPin: kraPin,
                    schoolType: schoolType,
                    category: category,
                    adminPhone: adminPhone,
                    logger: logger);

                // 2. Permissions & roles
                logger?.LogInformation(
                    "Seeding permissions and roles for school: {SchoolId} ({Name})",
                    school.Id, school.Name);

                await permissionSeeder.SeedPermissionsAndRolesAsync(school.Id);

                // 3. Assign SchoolAdmin role to the new admin
                await AssignSchoolAdminRoleAsync(dbContext, school.Id, adminEmail, logger);

                // 4. Academic year & classes
                logger?.LogInformation(
                    "Seeding academic data for school: {SchoolId}", school.Id);

                var academicSeeder = new AcademicSeeder(dbContext, logger);
                await academicSeeder.SeedAcademicDataAsync(school.Id);

                logger?.LogInformation(
                    "=== School provisioning completed: {SchoolName} (ID: {SchoolId}, Type: {Type}, Category: {Category}) ===",
                    school.Name, school.Id, school.SchoolType, school.Category);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error during new school setup for '{SchoolName}'.", schoolName);
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Assigns the SchoolAdmin role to a given user within a school tenant.
        /// Skips silently if the user or role cannot be found, or if already assigned.
        /// </summary>
        private static async Task AssignSchoolAdminRoleAsync(
            AppDbContext dbContext,
            Guid schoolId,
            string adminEmail,
            ILogger? logger)
        {
            // Resolve user
            var adminUser = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == adminEmail && u.TenantId == schoolId);

            if (adminUser == null)
            {
                logger?.LogWarning(
                    "Admin user '{Email}' not found for school {SchoolId} — role assignment skipped.",
                    adminEmail, schoolId);
                return;
            }

            // Resolve role
            var schoolAdminRole = await dbContext.Roles
                .FirstOrDefaultAsync(r => r.Name == "SchoolAdmin" && r.TenantId == schoolId);

            if (schoolAdminRole == null)
            {
                logger?.LogWarning(
                    "SchoolAdmin role not found for school {SchoolId} — role assignment skipped.",
                    schoolId);
                return;
            }

            // Guard: already assigned?
            var alreadyAssigned = await dbContext.UserRoles
                .AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == schoolAdminRole.Id);

            if (alreadyAssigned)
            {
                logger?.LogInformation(
                    "ℹ️ User '{Email}' already has the SchoolAdmin role in school {SchoolId}.",
                    adminEmail, schoolId);
                return;
            }

            dbContext.UserRoles.Add(new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = adminUser.Id,
                RoleId = schoolAdminRole.Id,
                TenantId = schoolId,
                CreatedOn = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();

            logger?.LogInformation(
                "✅ Assigned SchoolAdmin role to '{Email}' in school {SchoolId}.",
                adminEmail, schoolId);
        }
    }
}