
//using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
//using Devken.CBC.SchoolManagement.Application.Service;
//using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace Devken.CBC.SchoolManagement.Infrastructure.Services
//{
//    public class TenantSeedService : ITenantSeedService
//    {
//        private readonly IRepositoryManager _repo;
//        private readonly TenantContext _tenantContext;
//        private readonly ILogger<TenantSeedService> _logger;

//        public TenantSeedService(
//            IRepositoryManager repo,
//            TenantContext tenantContext,
//            ILogger<TenantSeedService> logger)
//        {
//            _repo = repo;
//            _tenantContext = tenantContext;
//            _logger = logger;
//        }

//        public async Task SeedNewTenantAsync(
//            Guid tenantId,
//            string adminEmail,
//            string adminPasswordHash,
//            string? adminFirstName = null,
//            string? adminLastName = null,
//            Guid? actingUserId = null)
//        {
//            _logger.LogInformation("Seeding new tenant {TenantId}", tenantId);

//            // Stamp rows with acting user (null for initial bootstrap)
//            _tenantContext.ActingUserId = actingUserId;

//            // ── 1. Seed global permissions ─────────────────────
//            var permissionMap = new Dictionary<string, Guid>();
//            foreach (var (key, display, group, desc) in PermissionCatalogue.All)
//            {
//                var existing = await _repo.Permission.GetByKeyAsync(key);
//                if (existing != null)
//                {
//                    permissionMap[key] = existing.Id;
//                    continue;
//                }

//                var perm = new Permission
//                {
//                    Id = Guid.NewGuid(),
//                    Key = key,
//                    DisplayName = display,
//                    GroupName = group,
//                    Description = desc
//                };
//                _repo.Permission.Create(perm);
//                permissionMap[key] = perm.Id;
//            }

//            // ── 2. Create default roles ───────────────────────
//            var roleMap = new Dictionary<string, Guid>();
//            foreach (var (roleName, desc, isSystem, _) in DefaultRoles.All)
//            {
//                var role = new Role
//                {
//                    Id = Guid.NewGuid(),
//                    TenantId = tenantId,
//                    Name = roleName,
//                    Description = desc,
//                    IsSystemRole = isSystem
//                };
//                _repo.Role.Create(role);
//                roleMap[roleName] = role.Id;
//            }

//            // ── 3. Assign permissions to roles ───────────────
//            foreach (var (roleName, _, _, permissions) in DefaultRoles.All)
//            {
//                var roleId = roleMap[roleName];
//                foreach (var permKey in permissions)
//                {
//                    if (!permissionMap.ContainsKey(permKey))
//                    {
//                        _logger.LogWarning(
//                            "Permission key {Key} not found during role seeding", permKey);
//                        continue;
//                    }

//                    var rp = new RolePermission
//                    {
//                        Id = Guid.NewGuid(),
//                        RoleId = roleId,
//                        PermissionId = permissionMap[permKey]
//                    };
//                    _repo.RolePermission.Create(rp);
//                }
//            }

//            // ── 4. Create first admin user ────────────────────
//            var adminUser = new User
//            {
//                Id = Guid.NewGuid(),
//                TenantId = tenantId,
//                Email = adminEmail,
//                PasswordHash = adminPasswordHash,
//                FirstName = adminFirstName,
//                LastName = adminLastName,
//                IsActive = true,
//                IsEmailVerified = true,     // first admin is trusted
//                RequirePasswordChange = true // must change password on first login
//            };
//            _repo.User.Create(adminUser);

//            // Bootstrap self-reference for audit columns
//            adminUser.CreatedBy = adminUser.Id;
//            adminUser.UpdatedBy = adminUser.Id;

//            // Acting user now becomes the new admin
//            _tenantContext.ActingUserId = adminUser.Id;

//            // Assign SchoolAdmin role
//            var adminRole = new UserRole
//            {
//                Id = Guid.NewGuid(),
//                TenantId = tenantId,
//                UserId = adminUser.Id,
//                RoleId = roleMap["SchoolAdmin"]
//            };
//            _repo.UserRole.Create(adminRole);

//            await _repo.SaveAsync();

//            _logger.LogInformation(
//                "Tenant {TenantId} seeded successfully. Admin user: {Email}",
//                tenantId, adminEmail);
//        }
//    }
//}
