using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services
{
    /// <summary>
    /// Seeds <see cref="Permission"/> records (global, one per key) and
    /// <see cref="Role"/> + <see cref="RolePermission"/> records (per school)
    /// from <see cref="PermissionCatalogue"/> and <see cref="DefaultRoles"/>.
    ///
    /// Designed to be idempotent: calling any method multiple times
    /// will never create duplicates.
    /// </summary>
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

        // ── Single school (called during school registration) ──────────────

        /// <inheritdoc/>
        public async Task<Guid> SeedPermissionsAndRolesAsync(Guid schoolId)
        {
            _logger.LogInformation(
                "Seeding permissions and roles for school {SchoolId}", schoolId);

            // 1. Ensure all global Permission rows exist
            var permissionMap = await EnsurePermissionsAsync();

            // 2. Seed roles for this school
            var schoolAdminRoleId = await SeedRolesForSchoolAsync(schoolId, permissionMap);

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Seeding complete for school {SchoolId}. SchoolAdmin role: {RoleId}",
                schoolId, schoolAdminRoleId);

            return schoolAdminRoleId;
        }

        // ── All schools (admin / migration tool) ──────────────────────────

        /// <inheritdoc/>
        public async Task SeedAllSchoolsAsync()
        {
            _logger.LogInformation("Starting permission/role seed for ALL schools");

            // 1. Ensure global permissions exist once
            var permissionMap = await EnsurePermissionsAsync();
            await _context.SaveChangesAsync();

            // 2. Load all active schools
            var schoolIds = await _context.Schools
                .Where(s => s.IsActive)
                .Select(s => s.Id)
                .ToListAsync();

            _logger.LogInformation(
                "Found {Count} school(s) to process", schoolIds.Count);

            int seeded = 0;
            int skipped = 0;

            foreach (var schoolId in schoolIds)
            {
                // Check if this school already has the full set of roles
                var existingRoleNames = await _context.Roles
                    .Where(r => r.TenantId == schoolId)
                    .Select(r => r.Name)
                    .ToListAsync();

                var allExpectedNames = DefaultRoles.All.Select(r => r.RoleName).ToHashSet();
                bool alreadyFull = allExpectedNames.All(n => existingRoleNames.Contains(n));

                if (alreadyFull)
                {
                    _logger.LogDebug(
                        "School {SchoolId} already has all roles — skipping", schoolId);
                    skipped++;
                    continue;
                }

                await SeedRolesForSchoolAsync(schoolId, permissionMap);
                await _context.SaveChangesAsync();   // save per school so partial failures don't roll back everything

                _logger.LogInformation(
                    "Seeded roles for school {SchoolId}", schoolId);
                seeded++;
            }

            _logger.LogInformation(
                "Seed complete. Seeded: {Seeded}, Skipped (already full): {Skipped}",
                seeded, skipped);
        }

        // ── Core helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Ensures every <see cref="PermissionCatalogue.All"/> entry exists as a
        /// global <see cref="Permission"/> row. Returns a map of Key → Permission.Id.
        /// Idempotent — never duplicates.
        /// </summary>
        private async Task<Dictionary<string, Guid>> EnsurePermissionsAsync()
        {
            // Load what already exists
            var existing = await _context.Permissions
                .ToDictionaryAsync(p => p.Key, p => p.Id);

            var toAdd = new List<Permission>();

            foreach (var (key, display, group, desc) in PermissionCatalogue.All)
            {
                if (existing.ContainsKey(key))
                    continue;

                var permission = new Permission
                {
                    Id = Guid.NewGuid(),
                    Key = key,
                    DisplayName = display,
                    GroupName = group,
                    Description = desc
                };

                toAdd.Add(permission);
                existing[key] = permission.Id;
            }

            if (toAdd.Count > 0)
            {
                _context.Permissions.AddRange(toAdd);
                _logger.LogInformation(
                    "Added {Count} new permission(s)", toAdd.Count);
            }

            return existing;
        }

        /// <summary>
        /// Creates any <see cref="DefaultRoles"/> entries that do not yet exist
        /// for <paramref name="schoolId"/> and wires up their
        /// <see cref="RolePermission"/> rows.
        ///
        /// Returns the ID of the <c>SchoolAdmin</c> role (existing or newly created).
        /// </summary>
        private async Task<Guid> SeedRolesForSchoolAsync(
            Guid schoolId,
            Dictionary<string, Guid> permissionMap)
        {
            // Load existing roles for this school in one query
            var existingRoles = await _context.Roles
                .Include(r => r.RolePermissions)
                .Where(r => r.TenantId == schoolId)
                .ToDictionaryAsync(r => r.Name, r => r);

            Guid schoolAdminRoleId = Guid.Empty;

            foreach (var (roleName, description, isSystem, permissions) in DefaultRoles.All)
            {
                Role role;

                if (existingRoles.TryGetValue(roleName, out var existingRole))
                {
                    // Role exists — ensure any missing RolePermission rows are added
                    role = existingRole;
                    var existingPermIds = role.RolePermissions
                        .Select(rp => rp.PermissionId)
                        .ToHashSet();

                    foreach (var permKey in permissions)
                    {
                        if (!permissionMap.TryGetValue(permKey, out var permId))
                        {
                            _logger.LogWarning(
                                "Permission key '{Key}' not found in catalogue — skipping", permKey);
                            continue;
                        }

                        if (!existingPermIds.Contains(permId))
                        {
                            _context.RolePermissions.Add(new RolePermission
                            {
                                Id = Guid.NewGuid(),
                                RoleId = role.Id,
                                PermissionId = permId
                            });
                        }
                    }
                }
                else
                {
                    // Role does not exist — create it
                    role = new Role
                    {
                        Id = Guid.NewGuid(),
                        Name = roleName,
                        Description = description,
                        IsSystemRole = isSystem,
                        TenantId = schoolId,
                        SchoolId = schoolId
                    };

                    _context.Roles.Add(role);

                    foreach (var permKey in permissions)
                    {
                        if (!permissionMap.TryGetValue(permKey, out var permId))
                        {
                            _logger.LogWarning(
                                "Permission key '{Key}' not found in catalogue — skipping", permKey);
                            continue;
                        }

                        _context.RolePermissions.Add(new RolePermission
                        {
                            Id = Guid.NewGuid(),
                            RoleId = role.Id,
                            PermissionId = permId
                        });
                    }
                }

                if (roleName == "SchoolAdmin")
                    schoolAdminRoleId = role.Id;
            }

            if (schoolAdminRoleId == Guid.Empty)
                throw new InvalidOperationException(
                    $"DefaultRoles does not contain a 'SchoolAdmin' entry. " +
                    $"Cannot determine admin role ID for school {schoolId}.");

            return schoolAdminRoleId;
        }
    }
}