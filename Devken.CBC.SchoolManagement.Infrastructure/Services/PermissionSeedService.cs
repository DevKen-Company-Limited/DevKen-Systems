using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services
{
    public class PermissionSeedService : IPermissionSeedService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PermissionSeedService> _logger;

        private const string DefaultAdminEmail = "admin@defaultschool.com";
        private const string DefaultAdminPassword = "Admin@123";

        public PermissionSeedService(AppDbContext context, ILogger<PermissionSeedService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Seeds permissions, roles, and default school admin for a tenant
        /// </summary>
        /// <param name="tenantId">The school tenant ID</param>
        /// <returns>The SchoolAdmin role ID</returns>
        public async Task<Guid> SeedPermissionsAndRolesAsync(Guid tenantId)
        {
            _logger.LogInformation("Seeding permissions and roles for tenant {TenantId}", tenantId);

            try
            {
                var permissionMap = await SeedPermissionsAsync();
                var roleMap = await SeedDefaultRolesAsync(tenantId);
                await AssignPermissionsToRolesAsync(roleMap, permissionMap);

                // Note: User seeding is now handled separately in DatabaseSeeder
                // We only ensure the role assignment happens here if user already exists
                await EnsureDefaultAdminRoleAssignmentAsync(tenantId, roleMap["SchoolAdmin"]);

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Successfully seeded {PermissionCount} permissions and {RoleCount} roles for tenant {TenantId}",
                    permissionMap.Count, roleMap.Count, tenantId);

                return roleMap["SchoolAdmin"];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding permissions and roles for tenant {TenantId}", tenantId);
                throw;
            }
        }

        private async Task<Dictionary<string, Guid>> SeedPermissionsAsync()
        {
            var permissionMap = new Dictionary<string, Guid>();

            foreach (var (key, display, group, desc) in PermissionCatalogue.All)
            {
                var existing = await _context.Permissions.FirstOrDefaultAsync(p => p.Key == key);
                if (existing != null)
                {
                    permissionMap[key] = existing.Id;
                    continue; // Skip duplicate
                }

                var permission = new Permission
                {
                    Id = Guid.NewGuid(),
                    Key = key,
                    DisplayName = display,
                    GroupName = group,
                    Description = desc,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                };

                _context.Permissions.Add(permission);
                permissionMap[key] = permission.Id;

                _logger.LogDebug("Created permission: {PermissionKey}", key);
            }

            return permissionMap;
        }

        private async Task<Dictionary<string, Guid>> SeedDefaultRolesAsync(Guid tenantId)
        {
            var roleMap = new Dictionary<string, Guid>();

            foreach (var (roleName, description, isSystem, _) in DefaultRoles.All)
            {
                var existing = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName && r.TenantId == tenantId);
                if (existing != null)
                {
                    roleMap[roleName] = existing.Id;
                    _logger.LogDebug("Role {RoleName} already exists for tenant {TenantId}, skipping.", roleName, tenantId);
                    continue; // Skip duplicate
                }

                var role = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    Description = description,
                    IsSystemRole = isSystem,
                    TenantId = tenantId,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                };

                _context.Roles.Add(role);
                roleMap[roleName] = role.Id;

                _logger.LogDebug("Created role: {RoleName} for tenant {TenantId}", roleName, tenantId);
            }

            return roleMap;
        }

        private async Task AssignPermissionsToRolesAsync(Dictionary<string, Guid> roleMap, Dictionary<string, Guid> permissionMap)
        {
            foreach (var (roleName, _, _, permissions) in DefaultRoles.All)
            {
                if (!roleMap.ContainsKey(roleName)) continue;

                var roleId = roleMap[roleName];

                foreach (var key in permissions)
                {
                    if (!permissionMap.ContainsKey(key))
                    {
                        _logger.LogWarning("Permission {PermissionKey} not found for role {RoleName}", key, roleName);
                        continue;
                    }

                    var exists = await _context.RolePermissions
                        .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionMap[key]);
                    if (exists) continue; // Skip duplicate

                    _context.RolePermissions.Add(new RolePermission
                    {
                        Id = Guid.NewGuid(),
                        RoleId = roleId,
                        PermissionId = permissionMap[key],
                        CreatedOn = DateTime.UtcNow,
                        UpdatedOn = DateTime.UtcNow
                    });
                }

                _logger.LogDebug("Assigned {PermissionCount} permissions to role {RoleName}", permissions.Length, roleName);
            }
        }

        /// <summary>
        /// Ensures the default admin user has the SchoolAdmin role assigned if they exist
        /// </summary>
        private async Task EnsureDefaultAdminRoleAssignmentAsync(Guid tenantId, Guid schoolAdminRoleId)
        {
            var existingAdmin = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == DefaultAdminEmail && u.TenantId == tenantId);

            if (existingAdmin == null)
            {
                _logger.LogInformation("Default admin user not found for tenant {TenantId}, skipping role assignment", tenantId);
                return;
            }

            // Check if role is already assigned
            var roleAssigned = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == existingAdmin.Id && ur.RoleId == schoolAdminRoleId);

            if (!roleAssigned)
            {
                _context.UserRoles.Add(new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = existingAdmin.Id,
                    RoleId = schoolAdminRoleId,
                    TenantId = tenantId,  // ✅ CRITICAL: Set TenantId
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                });

                _logger.LogInformation("Assigned SchoolAdmin role to default admin for tenant {TenantId}", tenantId);
            }
            else
            {
                _logger.LogInformation("Default admin already has SchoolAdmin role for tenant {TenantId}", tenantId);
            }
        }
    }
}