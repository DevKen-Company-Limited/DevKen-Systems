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

//    {
//  "schoolName": "Nairobi International Academy",
//  "schoolSlug": "nairobi-international-academy",
//  "schoolEmail": "info@nairobiacademy.co.ke",
//  "schoolPhone": "+254712345678",
//  "schoolAddress": "123 Kenyatta Avenue, Nairobi, Kenya",
//  "adminEmail": "admin@nairobiacademy.co.ke",
//  "adminPassword": "StrongP@ssw0rd!",
//  "adminFullName": "James Mwangi",
//  "adminPhone": "+254798765432"
//}

public static class DatabaseSeeder
    {
        private const string DefaultSuperAdminEmail = "superadmin@devken.com";
        private const string DefaultSuperAdminPassword = "SuperAdmin@123";

        private const string DefaultSchoolSlug = "default-school";
        private const string DefaultSchoolName = "Default School";
        private const string DefaultSchoolEmail = "info@defaultschool.com";
        private const string DefaultSchoolAdminEmail = "admin@defaultschool.com";
        private const string DefaultSchoolAdminPassword = "Admin@123";

        public static async Task SeedDatabaseAsync(this AppDbContext dbContext, ILogger? logger = null)
        {
            if (dbContext == null)
                throw new ArgumentNullException(nameof(dbContext));

            // ── SEED SUPERADMIN ───────────────────────────────
            var superAdminExists = await dbContext.SuperAdmins.AnyAsync(sa => sa.Email == DefaultSuperAdminEmail);
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

                logger?.LogInformation("Seeded SuperAdmin with email {Email}", DefaultSuperAdminEmail);
            }
            else
            {
                logger?.LogInformation("SuperAdmin already exists with email {Email}", DefaultSuperAdminEmail);
            }

            // ── SEED DEFAULT SCHOOL ────────────────────────────
            var defaultSchool = await dbContext.Schools.FirstOrDefaultAsync(s => s.SlugName == DefaultSchoolSlug);
            if (defaultSchool == null)
            {
                defaultSchool = new School
                {
                    Id = Guid.NewGuid(),
                    Name = DefaultSchoolName,
                    SlugName = DefaultSchoolSlug,
                    Email = DefaultSchoolEmail,
                    PhoneNumber = "0000000000",
                    Address = "Default Address",
                    IsActive = true,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                };
                dbContext.Schools.Add(defaultSchool);
                await dbContext.SaveChangesAsync();

                logger?.LogInformation("Seeded default school {Name}", DefaultSchoolName);
            }

            // ── SEED DEFAULT SCHOOL ADMIN ──────────────────────
            var schoolAdminExists = await dbContext.Users.AnyAsync(u => u.Email == DefaultSchoolAdminEmail);
            if (!schoolAdminExists)
            {
                var names = DefaultSchoolAdminEmail.Split('@')[0].Split('.');
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = DefaultSchoolAdminEmail,
                    FirstName = names.Length > 0 ? names[0] : "Admin",
                    LastName = names.Length > 1 ? names[1] : "User",
                    Tenant = defaultSchool,
                    IsActive = true,
                    IsEmailVerified = true,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                };

                user.PasswordHash = new PasswordHasher<User>()
                    .HashPassword(user, DefaultSchoolAdminPassword);

                dbContext.Users.Add(user);
                await dbContext.SaveChangesAsync();

                // Seed role for school admin
                var role = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "SchoolAdmin" && r.TenantId == defaultSchool.Id);
                if (role == null)
                {
                    role = new Role
                    {
                        Id = Guid.NewGuid(),
                        Name = "SchoolAdmin",
                        TenantId = defaultSchool.Id,
                        CreatedOn = DateTime.UtcNow,
                        UpdatedOn = DateTime.UtcNow
                    };
                    dbContext.Roles.Add(role);
                    await dbContext.SaveChangesAsync();
                }

                // Assign role to user
                dbContext.UserRoles.Add(new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    RoleId = role.Id
                });

                await dbContext.SaveChangesAsync();
                logger?.LogInformation("Seeded default school admin {Email}", DefaultSchoolAdminEmail);
            }
            else
            {
                logger?.LogInformation("Default school admin already exists with email {Email}", DefaultSchoolAdminEmail);
            }
        }
    }
}
