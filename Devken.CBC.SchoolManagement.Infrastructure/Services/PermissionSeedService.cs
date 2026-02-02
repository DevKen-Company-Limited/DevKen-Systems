using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services
{
    public class PermissionSeedService : IPermissionSeedService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PermissionSeedService> _logger;

        public PermissionSeedService(
            AppDbContext context,
            ILogger<PermissionSeedService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Guid> SeedPermissionsAndRolesAsync(Guid tenantId)
        {
            _logger.LogInformation("Seeding permissions and roles for tenant {TenantId}", tenantId);

            try
            {
                // Step 1: Seed all permissions (global, not tenant-specific)
                var permissionMap = await SeedPermissionsAsync();

                // Step 2: Seed default roles for this tenant
                var roleMap = await SeedDefaultRolesAsync(tenantId);

                // Step 3: Assign permissions to roles
                await AssignPermissionsToRolesAsync(roleMap, permissionMap);

                // Save all changes
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Successfully seeded {PermissionCount} permissions and {RoleCount} roles for tenant {TenantId}",
                    permissionMap.Count, roleMap.Count, tenantId);

                // Return the SchoolAdmin role ID
                return roleMap["SchoolAdmin"];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding permissions and roles for tenant {TenantId}", tenantId);
                throw;
            }
        }

        /// <summary>
        /// Seeds all permissions from PermissionCatalogue
        /// </summary>
        private async Task<Dictionary<string, Guid>> SeedPermissionsAsync()
        {
            var permissionMap = new Dictionary<string, Guid>();

            foreach (var (key, display, group, desc) in PermissionCatalogue.All)
            {
                // Check if permission already exists
                var existingPermission = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Key == key);

                if (existingPermission != null)
                {
                    permissionMap[key] = existingPermission.Id;
                    continue;
                }

                // Create new permission
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

        /// <summary>
        /// Seeds default roles for the tenant
        /// </summary>
        private async Task<Dictionary<string, Guid>> SeedDefaultRolesAsync(Guid tenantId)
        {
            var roleMap = new Dictionary<string, Guid>();

            foreach (var (roleName, description, isSystem, _) in DefaultRoles.All)
            {
                // Check if role already exists for this tenant
                var existingRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == roleName && r.TenantId == tenantId);

                if (existingRole != null)
                {
                    roleMap[roleName] = existingRole.Id;
                    continue;
                }

                // Create new role
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

        /// <summary>
        /// Assigns permissions to roles based on DefaultRoles configuration
        /// </summary>
        private async Task AssignPermissionsToRolesAsync(
            Dictionary<string, Guid> roleMap,
            Dictionary<string, Guid> permissionMap)
        {
            foreach (var (roleName, _, _, permissions) in DefaultRoles.All)
            {
                if (!roleMap.ContainsKey(roleName))
                    continue;

                var roleId = roleMap[roleName];

                foreach (var permissionKey in permissions)
                {
                    if (!permissionMap.ContainsKey(permissionKey))
                    {
                        _logger.LogWarning(
                            "Permission key {PermissionKey} not found in permission map for role {RoleName}",
                            permissionKey, roleName);
                        continue;
                    }

                    var permissionId = permissionMap[permissionKey];

                    // Check if assignment already exists
                    var exists = await _context.RolePermissions
                        .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

                    if (exists)
                        continue;

                    // Create role-permission assignment
                    var rolePermission = new RolePermission
                    {
                        Id = Guid.NewGuid(),
                        RoleId = roleId,
                        PermissionId = permissionId,
                        CreatedOn = DateTime.UtcNow,
                        UpdatedOn = DateTime.UtcNow
                    };

                    _context.RolePermissions.Add(rolePermission);
                }

                _logger.LogDebug(
                    "Assigned {PermissionCount} permissions to role {RoleName}",
                    permissions.Length, roleName);
            }
        }
    }
}
