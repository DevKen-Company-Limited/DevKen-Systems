using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
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
    /// Extension methods for seeding application data
    /// </summary>
    public static class SeedExtensions
    {
        /// <summary>
        /// Seeds all default data including SuperAdmin, schools, permissions, roles, and academic data
        /// </summary>
        public static async Task SeedDefaultDataAsync(this IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            var logger = serviceProvider.GetService<ILogger<AppDbContext>>();
            var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
            var permissionSeeder = serviceProvider.GetRequiredService<IPermissionSeedService>();

            try
            {
                logger?.LogInformation("=== Starting complete database seeding ===");

                // ── 1. SEED SUPERADMIN & DEFAULT SCHOOL ──────────
                await dbContext.SeedDatabaseAsync(logger);

                // ── 2. GET DEFAULT SCHOOL ─────────────────────────
                var defaultSchool = await dbContext.Schools
                    .FirstOrDefaultAsync(s => s.SlugName == "default-school");

                if (defaultSchool == null)
                {
                    logger?.LogError("Default school not found after seeding!");
                    return;
                }

                // ── 3. SEED PERMISSIONS & ROLES ───────────────────
                logger?.LogInformation("Seeding permissions and roles for school: {SchoolId}", defaultSchool.Id);
                await permissionSeeder.SeedPermissionsAndRolesAsync(defaultSchool.Id);

                // ── 4. ASSIGN SCHOOLADMIN ROLE TO DEFAULT ADMIN ───
                await AssignSchoolAdminRoleAsync(dbContext, defaultSchool.Id, logger);

                // ── 5. SEED ACADEMIC DATA (ACADEMIC YEAR & CLASSES) ───
                logger?.LogInformation("Seeding academic data for school: {SchoolId}", defaultSchool.Id);
                var academicSeeder = new AcademicSeeder(dbContext, logger);
                await academicSeeder.SeedAcademicDataAsync(defaultSchool.Id);

                logger?.LogInformation("=== Database seeding completed successfully ===");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error during database seeding");
                throw;
            }
        }

        /// <summary>
        /// Assigns the SchoolAdmin role to the default school administrator
        /// </summary>
        private static async Task AssignSchoolAdminRoleAsync(
            AppDbContext dbContext,
            Guid schoolId,
            ILogger? logger)
        {
            const string defaultAdminEmail = "admin@defaultschool.com";

            // Find the default admin user
            var adminUser = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == defaultAdminEmail && u.TenantId == schoolId);

            if (adminUser == null)
            {
                logger?.LogWarning("Default admin user not found for school {SchoolId}", schoolId);
                return;
            }

            // Find the SchoolAdmin role
            var schoolAdminRole = await dbContext.Roles
                .FirstOrDefaultAsync(r => r.Name == "SchoolAdmin" && r.TenantId == schoolId);

            if (schoolAdminRole == null)
            {
                logger?.LogWarning("SchoolAdmin role not found for school {SchoolId}", schoolId);
                return;
            }

            // Check if role is already assigned
            var roleAssigned = await dbContext.UserRoles
                .AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == schoolAdminRole.Id);

            if (!roleAssigned)
            {
                dbContext.UserRoles.Add(new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = adminUser.Id,
                    RoleId = schoolAdminRole.Id,
                    TenantId = schoolId,  // ✅ CRITICAL: Set TenantId
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                });

                await dbContext.SaveChangesAsync();

                logger?.LogInformation(
                    "✅ Assigned SchoolAdmin role to user {Email}",
                    defaultAdminEmail);
            }
            else
            {
                logger?.LogInformation(
                    "ℹ️ User {Email} already has SchoolAdmin role",
                    defaultAdminEmail);
            }
        }

        /// <summary>
        /// Seeds a new school with complete setup (school, admin, roles, permissions, and academic data)
        /// </summary>
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
            string? adminPhone = null)
        {
            using var scope = services.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            var logger = serviceProvider.GetService<ILogger<AppDbContext>>();
            var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
            var permissionSeeder = serviceProvider.GetRequiredService<IPermissionSeedService>();

            try
            {
                logger?.LogInformation("=== Creating new school: {SchoolName} ===", schoolName);

                // 1. Create the school and admin user
                var school = await DatabaseSeeder.SeedNewSchoolAsync(
                    dbContext,
                    schoolName,
                    schoolSlug,
                    schoolEmail,
                    schoolPhone,
                    schoolAddress,
                    adminEmail,
                    adminPassword,
                    adminFullName,
                    adminPhone,
                    logger);

                // 2. Seed permissions and roles for the new school
                logger?.LogInformation("Seeding permissions and roles for school: {SchoolId}", school.Id);
                await permissionSeeder.SeedPermissionsAndRolesAsync(school.Id);

                // 3. Assign SchoolAdmin role to the admin user
                var adminUser = await dbContext.Users
                    .FirstOrDefaultAsync(u => u.Email == adminEmail && u.TenantId == school.Id);

                if (adminUser != null)
                {
                    var schoolAdminRole = await dbContext.Roles
                        .FirstOrDefaultAsync(r => r.Name == "SchoolAdmin" && r.TenantId == school.Id);

                    if (schoolAdminRole != null)
                    {
                        var roleAssigned = await dbContext.UserRoles
                            .AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == schoolAdminRole.Id);

                        if (!roleAssigned)
                        {
                            dbContext.UserRoles.Add(new UserRole
                            {
                                Id = Guid.NewGuid(),
                                UserId = adminUser.Id,
                                RoleId = schoolAdminRole.Id,
                                TenantId = school.Id,  // ✅ CRITICAL: Set TenantId
                                CreatedOn = DateTime.UtcNow,
                                UpdatedOn = DateTime.UtcNow
                            });

                            await dbContext.SaveChangesAsync();

                            logger?.LogInformation(
                                "✅ Assigned SchoolAdmin role to {Email}",
                                adminEmail);
                        }
                    }
                }

                // 4. Seed academic data (academic year and classes)
                logger?.LogInformation("Seeding academic data for school: {SchoolId}", school.Id);
                var academicSeeder = new AcademicSeeder(dbContext, logger);
                await academicSeeder.SeedAcademicDataAsync(school.Id);

                logger?.LogInformation(
                    "=== School setup completed successfully: {SchoolName} ===",
                    schoolName);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error during new school setup");
                throw;
            }
        }
    }
}