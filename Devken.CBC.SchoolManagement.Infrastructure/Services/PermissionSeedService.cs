using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
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

        public PermissionSeedService(AppDbContext context, ILogger<PermissionSeedService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Seeds permissions, roles, role-permission assignments, and ensures
        /// the default school admin has the SchoolAdmin role.
        ///
        /// Each step is committed independently so that FK constraints are
        /// satisfied before dependent rows are inserted:
        ///   1. Permissions  (no FK dependencies)
        ///   2. Roles        (FK → Schools.Id via SchoolId)
        ///   3. RolePermissions (FK → Roles.Id AND Permissions.Id)
        ///   4. UserRoles    (FK → Users.Id AND Roles.Id)
        /// </summary>
        public async Task<Guid> SeedPermissionsAndRolesAsync(Guid tenantId)
        {
            _logger.LogInformation(
                "Seeding permissions and roles for tenant {TenantId}", tenantId);

            try
            {
                // ── Step 1: Permissions ───────────────────────────────────────
                // No FK dependencies — safe to save first.
                var permissionMap = await SeedPermissionsAsync();
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "✅ Saved {Count} permission(s) for tenant {TenantId}",
                    permissionMap.Count, tenantId);

                // ── Step 2: Roles ─────────────────────────────────────────────
                // FK: Roles.SchoolId → Schools.Id  (must set SchoolId = tenantId)
                // FK: Roles.TenantId is the app-level discriminator (also = tenantId)
                var roleMap = await SeedDefaultRolesAsync(tenantId);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "✅ Saved {Count} role(s) for tenant {TenantId}",
                    roleMap.Count, tenantId);

                // ── Step 3: RolePermissions ───────────────────────────────────
                // FK: RolePermissions.RoleId       → Roles.Id       (committed above)
                // FK: RolePermissions.PermissionId → Permissions.Id (committed above)
                await AssignPermissionsToRolesAsync(roleMap, permissionMap);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "✅ Saved role-permission assignments for tenant {TenantId}", tenantId);

                // ── Step 4: UserRoles ─────────────────────────────────────────
                // FK: UserRoles.UserId → Users.Id  (user must already exist)
                // FK: UserRoles.RoleId → Roles.Id  (committed above)
                await EnsureDefaultAdminRoleAssignmentAsync(tenantId, roleMap["SchoolAdmin"]);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "✅ Completed seeding for tenant {TenantId} — {PermCount} permissions, {RoleCount} roles",
                    tenantId, permissionMap.Count, roleMap.Count);

                return roleMap["SchoolAdmin"];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error seeding permissions and roles for tenant {TenantId}", tenantId);
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // STEP 1 — Permissions
        // ─────────────────────────────────────────────────────────────────────

        private async Task<Dictionary<string, Guid>> SeedPermissionsAsync()
        {
            var permissionMap = new Dictionary<string, Guid>();

            foreach (var (key, display, group, desc) in PermissionCatalogue.All)
            {
                var existing = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Key == key);

                if (existing != null)
                {
                    permissionMap[key] = existing.Id;
                    continue;
                }

                var permission = new Permission
                {
                    Id = Guid.NewGuid(),
                    Key = key,
                    DisplayName = display,
                    GroupName = group,
                    Description = desc,
                    CreatedOn = DateTime.UtcNow
                };

                _context.Permissions.Add(permission);
                permissionMap[key] = permission.Id;

                _logger.LogDebug("Queued permission: {Key}", key);
            }

            return permissionMap;
        }

        // ─────────────────────────────────────────────────────────────────────
        // STEP 2 — Roles
        // ─────────────────────────────────────────────────────────────────────

        private async Task<Dictionary<string, Guid>> SeedDefaultRolesAsync(Guid tenantId)
        {
            var roleMap = new Dictionary<string, Guid>();

            foreach (var (roleName, description, isSystem, _) in DefaultRoles.All)
            {
                var existing = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == roleName && r.TenantId == tenantId);

                if (existing != null)
                {
                    roleMap[roleName] = existing.Id;
                    _logger.LogDebug(
                        "Role '{RoleName}' already exists for tenant {TenantId} — skipping.",
                        roleName, tenantId);
                    continue;
                }

                var role = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    Description = description,
                    IsSystemRole = isSystem,
                    // ✅ BOTH must be set:
                    //    SchoolId → satisfies FK_Roles_Schools_SchoolId (DB constraint)
                    //    TenantId → application-level tenant discriminator
                    SchoolId = tenantId,
                    TenantId = tenantId,
                    CreatedOn = DateTime.UtcNow
                };

                _context.Roles.Add(role);
                roleMap[roleName] = role.Id;

                _logger.LogDebug(
                    "Queued role: '{RoleName}' for tenant {TenantId}", roleName, tenantId);
            }

            return roleMap;
        }

        // ─────────────────────────────────────────────────────────────────────
        // STEP 3 — RolePermissions
        // ─────────────────────────────────────────────────────────────────────

        private async Task AssignPermissionsToRolesAsync(
            Dictionary<string, Guid> roleMap,
            Dictionary<string, Guid> permissionMap)
        {
            foreach (var (roleName, _, _, permissions) in DefaultRoles.All)
            {
                if (!roleMap.TryGetValue(roleName, out var roleId))
                    continue;

                foreach (var key in permissions)
                {
                    if (!permissionMap.TryGetValue(key, out var permissionId))
                    {
                        _logger.LogWarning(
                            "Permission '{Key}' not found in catalogue — skipping for role '{RoleName}'.",
                            key, roleName);
                        continue;
                    }

                    var exists = await _context.RolePermissions
                        .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

                    if (exists)
                        continue;

                    _context.RolePermissions.Add(new RolePermission
                    {
                        Id = Guid.NewGuid(),
                        RoleId = roleId,
                        PermissionId = permissionId,
                        CreatedOn = DateTime.UtcNow
                    });
                }

                _logger.LogDebug(
                    "Queued {Count} permission(s) for role '{RoleName}'",
                    permissions.Length, roleName);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // STEP 4 — UserRoles
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Assigns the SchoolAdmin role to the default admin user if they exist.
        /// Skips silently if the user is not found (they may be seeded separately).
        /// </summary>
        private async Task EnsureDefaultAdminRoleAssignmentAsync(Guid tenantId, Guid schoolAdminRoleId)
        {
            var adminUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == DefaultAdminEmail && u.TenantId == tenantId);

            if (adminUser == null)
            {
                _logger.LogInformation(
                    "Default admin '{Email}' not found for tenant {TenantId} — role assignment skipped.",
                    DefaultAdminEmail, tenantId);
                return;
            }

            var alreadyAssigned = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == schoolAdminRoleId);

            if (alreadyAssigned)
            {
                _logger.LogInformation(
                    "Default admin '{Email}' already has SchoolAdmin role for tenant {TenantId}.",
                    DefaultAdminEmail, tenantId);
                return;
            }

            _context.UserRoles.Add(new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = adminUser.Id,
                RoleId = schoolAdminRoleId,
                TenantId = tenantId,
                CreatedOn = DateTime.UtcNow
            });

            _logger.LogInformation(
                "✅ Assigned SchoolAdmin role to '{Email}' for tenant {TenantId}.",
                DefaultAdminEmail, tenantId);
        }
    }
}