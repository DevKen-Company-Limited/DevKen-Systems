using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.SchoolConf;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF
{
    public class AppDbContext : DbContext
    {
        private readonly TenantContext _tenantContext;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            TenantContext tenantContext,
            IPasswordHasher<User> passwordHasher)
            : base(options)
        {
            _tenantContext = tenantContext;
            _passwordHasher = passwordHasher;
        }

        public DbSet<School> Schools => Set<School>();
        public DbSet<User> Users => Set<User>();
        public DbSet<SuperAdmin> SuperAdmins => Set<SuperAdmin>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            mb.ApplyConfiguration(new SchoolConfiguration());
            mb.ApplyConfiguration(new PermissionConfiguration());
            mb.ApplyConfiguration(new RoleConfiguration(_tenantContext));
            mb.ApplyConfiguration(new UserConfiguration(_tenantContext));
            mb.ApplyConfiguration(new RolePermissionConfiguration(_tenantContext));
            mb.ApplyConfiguration(new UserRoleConfiguration(_tenantContext));
            mb.ApplyConfiguration(new RefreshTokenConfiguration(_tenantContext));

            // ── SEED DEFAULT SCHOOL + ADMIN + ROLE ─────────────
            var defaultSchoolId = Guid.NewGuid();
            var defaultAdminId = Guid.NewGuid();
            var defaultRoleId = Guid.NewGuid();

            mb.Entity<School>().HasData(new School
            {
                Id = defaultSchoolId,
                Name = "Default School",
                SlugName = "default-school",
                Email = "info@defaultschool.com",
                PhoneNumber = "0000000000",
                Address = "Default Address",
                IsActive = true,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow
            });

            mb.Entity<Role>().HasData(new Role
            {
                Id = defaultRoleId,
                Name = "SchoolAdmin",
                TenantId = defaultSchoolId,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow
            });

            var adminUser = new User
            {
                Id = defaultAdminId,
                Email = "admin@defaultschool.com",
                FirstName = "Default",
                LastName = "Admin",
                PhoneNumber = "0000000000",
                TenantId = defaultSchoolId,
                IsActive = true,
                IsEmailVerified = true,
                RequirePasswordChange = false,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow
            };

            adminUser.PasswordHash = _passwordHasher.HashPassword(adminUser, "Admin@123");

            mb.Entity<User>().HasData(adminUser);

            mb.Entity<UserRole>().HasData(new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = defaultAdminId,
                RoleId = defaultRoleId,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow
            });

            // ── SEED DEFAULT SUPER ADMIN ─────────────
            var superAdminId = Guid.NewGuid();
            var superAdminPasswordHasher = new PasswordHasher<SuperAdmin>();
            var superAdmin = new SuperAdmin
            {
                Id = superAdminId,
                Email = "superadmin@devken.com",
                FirstName = "Super",
                LastName = "Admin",
                IsActive = true,            
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow,
                CreatedBy = null,
                UpdatedBy = null
            };

            // Hash password
            superAdmin.PasswordHash = superAdminPasswordHasher.HashPassword(superAdmin, "SuperAdmin@123");

            mb.Entity<SuperAdmin>().HasData(superAdmin);
        }
    }
}
