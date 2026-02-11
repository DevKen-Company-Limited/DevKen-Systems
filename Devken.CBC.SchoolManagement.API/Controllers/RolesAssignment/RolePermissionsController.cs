using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.DTOs.RolePermission;

namespace Devken.CBC.SchoolManagement.Api.Controllers
{
    /// <summary>
    /// Controller for managing role permissions
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RolePermissionsController : BaseApiController
    {
        private readonly IRepositoryManager _repository;
        private readonly ILogger<RolePermissionsController> _logger;

        public RolePermissionsController(
            IRepositoryManager repository,
            ILogger<RolePermissionsController> logger) : base(logger: logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Query Endpoints

        /// <summary>
        /// Get all available permissions in the system with user counts
        /// </summary>
        /// <response code="200">Returns list of all permissions with user counts</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet("permissions")]
        [ProducesResponseType(typeof(List<PermissionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAllPermissions()
        {
            try
            {
                _logger.LogInformation("User {UserId} getting all permissions", CurrentUserId);

                var permissions = await _repository.Permission
                    .FindAll(false)
                    .OrderBy(p => p.GroupName)
                    .ThenBy(p => p.DisplayName)
                    .Select(p => new PermissionDto
                    {
                        Id = p.Id,
                        Key = p.Key ?? string.Empty,
                        DisplayName = p.DisplayName ?? string.Empty,
                        GroupName = p.GroupName,
                        Description = p.Description,
                        IsAssigned = false
                    })
                    .ToListAsync();

                // Calculate user count for each permission
                foreach (var permission in permissions)
                {
                    permission.UserCount = await GetPermissionUserCountAsync(permission.Id);
                }

                await LogUserActivityAsync("permissions.view_all", $"Retrieved {permissions.Count} permissions");

                return SuccessResponse(permissions, $"Retrieved {permissions.Count} permissions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all permissions");
                return ErrorResponse("An error occurred while fetching permissions", 500);
            }
        }

        /// <summary>
        /// Get permissions grouped by category with user counts
        /// </summary>
        /// <response code="200">Returns grouped permissions</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet("permissions/grouped")]
        [ProducesResponseType(typeof(Dictionary<string, List<PermissionDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPermissionsGrouped()
        {
            try
            {
                _logger.LogInformation("User {UserId} getting grouped permissions", CurrentUserId);

                var permissions = await _repository.Permission
                    .FindAll(false)
                    .OrderBy(p => p.GroupName)
                    .ThenBy(p => p.DisplayName)
                    .Select(p => new PermissionDto
                    {
                        Id = p.Id,
                        Key = p.Key ?? string.Empty,
                        DisplayName = p.DisplayName ?? string.Empty,
                        GroupName = p.GroupName,
                        Description = p.Description,
                        IsAssigned = false
                    })
                    .ToListAsync();

                // Calculate user count for each permission
                foreach (var permission in permissions)
                {
                    permission.UserCount = await GetPermissionUserCountAsync(permission.Id);
                }

                var grouped = permissions
                    .GroupBy(p => p.GroupName ?? "Other")
                    .ToDictionary(
                        g => g.Key,
                        g => g.ToList()
                    );

                await LogUserActivityAsync("permissions.view_grouped", $"Retrieved {grouped.Count} permission groups");

                return SuccessResponse(grouped, $"Retrieved {grouped.Count} permission groups");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting grouped permissions");
                return ErrorResponse("An error occurred while fetching grouped permissions", 500);
            }
        }

        /// <summary>
        /// Get user count for a specific permission
        /// </summary>
        /// <param name="permissionId">Permission ID</param>
        /// <response code="200">Returns number of users with this permission</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Permission not found</response>
        [HttpGet("permissions/{permissionId:guid}/user-count")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPermissionUserCount(Guid permissionId)
        {
            try
            {
                // Verify permission exists
                var permissionExists = await _repository.Permission
                    .FindByCondition(p => p.Id == permissionId, false)
                    .AnyAsync();

                if (!permissionExists)
                {
                    return NotFoundResponse("Permission not found");
                }

                var userCount = await GetPermissionUserCountAsync(permissionId);

                return SuccessResponse(userCount, $"Permission has {userCount} user(s)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user count for permission {PermissionId}", permissionId);
                return ErrorResponse("An error occurred while fetching user count", 500);
            }
        }

        /// <summary>
        /// Get users who have a specific permission (through their roles)
        /// </summary>
        /// <param name="permissionId">Permission ID</param>
        /// <response code="200">Returns list of users with this permission</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Permission not found</response>
        [HttpGet("permissions/{permissionId:guid}/users")]
        [ProducesResponseType(typeof(List<UserWithPermissionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUsersWithPermission(Guid permissionId)
        {
            try
            {
                _logger.LogInformation("User {UserId} getting users with permission {PermissionId}",
                    CurrentUserId, permissionId);

                // Verify permission exists
                var permission = await _repository.Permission
                    .FindByCondition(p => p.Id == permissionId, false)
                    .FirstOrDefaultAsync();

                if (permission == null)
                {
                    return NotFoundResponse("Permission not found");
                }

                // Get all users who have this permission through their roles
                var usersWithPermission = await _repository.UserRole
                    .FindAll(false)
                    .Include(ur => ur.User)
                    .Include(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                    .Where(ur => ur.Role!.RolePermissions!.Any(rp => rp.PermissionId == permissionId))
                    .Where(ur => ur.User!.IsActive)
                    .Select(ur => new
                    {
                        User = ur.User,
                        RoleName = ur.Role!.Name
                    })
                    .Distinct()
                    .ToListAsync();

                // Group by user to get all roles that grant this permission
                var userDtos = usersWithPermission
                    .GroupBy(x => x.User!.Id)
                    .Select(g => new UserWithPermissionDto
                    {
                        UserId = g.Key,
                        Email = g.First().User!.Email,
                        FullName = $"{g.First().User!.FirstName} {g.First().User!.LastName}".Trim(),
                        RoleNames = g.Select(x => x.RoleName ?? "Unknown").Distinct().ToList()
                    })
                    .OrderBy(u => u.FullName)
                    .ToList();

                await LogUserActivityAsync("permission.users.view",
                    $"Viewed {userDtos.Count} users with permission {permissionId}");

                return SuccessResponse(userDtos,
                    $"Retrieved {userDtos.Count} user(s) with this permission");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users with permission {PermissionId}", permissionId);
                return ErrorResponse("An error occurred while fetching users", 500);
            }
        }

        /// <summary>
        /// Get permissions for a specific role
        /// </summary>
        /// <param name="roleId">Role ID</param>
        /// <response code="200">Returns list of role permissions</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - insufficient permissions</response>
        /// <response code="404">Role not found</response>
        [HttpGet("roles/{roleId:guid}/permissions")]
        [ProducesResponseType(typeof(List<PermissionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRolePermissions(Guid roleId)
        {
            try
            {
                _logger.LogInformation("User {UserId} getting permissions for role {RoleId}", CurrentUserId, roleId);

                // ✅ FIX: Check for "Role.Read" instead of "Roles.View"
                if (!HasPermission(PermissionKeys.RoleRead) && !HasPermission(PermissionKeys.RoleWrite))
                {
                    LogUserAuthorization(PermissionKeys.RoleRead, roleId.ToString(), false, "Missing permission");
                    return ForbiddenResponse("You do not have permission to view role permissions");
                }

                // Get role with validation
                var role = await _repository.Role
                    .FindByCondition(r => r.Id == roleId, false)
                    .FirstOrDefaultAsync();

                if (role == null)
                {
                    return NotFoundResponse("Role not found");
                }

                // Validate tenant access for non-SuperAdmin
                if (!IsSuperAdmin)
                {
                    if (!role.IsSystemRole && role.TenantId != Guid.Empty && role.TenantId != CurrentTenantId)
                    {
                        LogUserAuthorization(PermissionKeys.RoleRead, roleId.ToString(), false, "Tenant mismatch");
                        return ForbiddenResponse("You do not have access to this role");
                    }
                }

                // Get role permissions
                var rolePermissions = await _repository.RolePermission
                    .FindByCondition(rp => rp.RoleId == roleId, false)
                    .Include(rp => rp.Permission)
                    .ToListAsync();

                var permissions = rolePermissions
                    .Where(rp => rp.Permission != null)
                    .Select(rp => new PermissionDto
                    {
                        Id = rp.PermissionId,
                        Key = rp.Permission!.Key ?? string.Empty,
                        DisplayName = rp.Permission.DisplayName ?? string.Empty,
                        GroupName = rp.Permission.GroupName,
                        Description = rp.Permission.Description,
                        IsAssigned = true
                    })
                    .OrderBy(p => p.GroupName)
                    .ThenBy(p => p.DisplayName)
                    .ToList();

                // Add user counts
                foreach (var permission in permissions)
                {
                    permission.UserCount = await GetPermissionUserCountAsync(permission.Id);
                }

                await LogUserActivityAsync("role.permissions.view", $"Viewed permissions for role {roleId}");

                return SuccessResponse(permissions, $"Retrieved {permissions.Count} permissions for role");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for role {RoleId}", roleId);
                return ErrorResponse("An error occurred while fetching role permissions", 500);
            }
        }

        /// <summary>
        /// Get detailed role information with permissions
        /// </summary>
        /// <param name="roleId">Role ID</param>
        /// <response code="200">Returns role with permissions</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - insufficient permissions</response>
        /// <response code="404">Role not found</response>
        [HttpGet("roles/{roleId:guid}")]
        [ProducesResponseType(typeof(RoleWithPermissionsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRoleWithPermissions(Guid roleId)
        {
            try
            {
                _logger.LogInformation("User {UserId} getting role {RoleId} with permissions", CurrentUserId, roleId);

                // ✅ FIX: Check for "Role.Read" instead of "Roles.View"
                if (!HasPermission(PermissionKeys.RoleRead) && !HasPermission(PermissionKeys.RoleWrite))
                {
                    LogUserAuthorization(PermissionKeys.RoleRead, roleId.ToString(), false, "Missing permission");
                    return ForbiddenResponse("You do not have permission to view roles");
                }

                // Get role with permissions
                var role = await _repository.Role
                    .FindByCondition(r => r.Id == roleId, false)
                    .Include(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync();

                if (role == null)
                {
                    return NotFoundResponse("Role not found");
                }

                // Validate tenant access
                if (!IsSuperAdmin)
                {
                    if (!role.IsSystemRole && role.TenantId != Guid.Empty && role.TenantId != CurrentTenantId)
                    {
                        LogUserAuthorization(PermissionKeys.RoleRead, roleId.ToString(), false, "Tenant mismatch");
                        return ForbiddenResponse("You do not have access to this role");
                    }
                }

                var permissions = role.RolePermissions?
                    .Where(rp => rp.Permission != null)
                    .Select(rp => new PermissionDto
                    {
                        Id = rp.PermissionId,
                        Key = rp.Permission!.Key ?? string.Empty,
                        DisplayName = rp.Permission.DisplayName ?? string.Empty,
                        GroupName = rp.Permission.GroupName,
                        Description = rp.Permission.Description,
                        IsAssigned = true
                    })
                    .OrderBy(p => p.GroupName)
                    .ThenBy(p => p.DisplayName)
                    .ToList() ?? new List<PermissionDto>();

                // Add user counts
                foreach (var permission in permissions)
                {
                    permission.UserCount = await GetPermissionUserCountAsync(permission.Id);
                }

                var result = new RoleWithPermissionsDto
                {
                    RoleId = role.Id,
                    RoleName = role.Name ?? string.Empty,
                    Description = role.Description,
                    IsSystemRole = role.IsSystemRole,
                    TenantId = role.TenantId,
                    Permissions = permissions,
                    TotalPermissions = permissions.Count
                };

                await LogUserActivityAsync("role.view_details", $"Viewed details for role {roleId}");

                return SuccessResponse(result, "Role details retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role {RoleId} with permissions", roleId);
                return ErrorResponse("An error occurred while fetching role details", 500);
            }
        }

        /// <summary>
        /// Check if a role has a specific permission
        /// </summary>
        /// <param name="roleId">Role ID</param>
        /// <param name="permissionId">Permission ID</param>
        /// <response code="200">Returns true if role has permission</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet("roles/{roleId:guid}/permissions/{permissionId:guid}/check")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CheckRoleHasPermission(Guid roleId, Guid permissionId)
        {
            try
            {
                var hasPermission = await _repository.RolePermission
                    .FindByCondition(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, false)
                    .AnyAsync();

                return SuccessResponse(hasPermission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission for role {RoleId}", roleId);
                return ErrorResponse("An error occurred while checking permission", 500);
            }
        }

        #endregion

        #region Command Endpoints

        /// <summary>
        /// Update all permissions for a role (replaces existing permissions)
        /// </summary>
        /// <param name="request">Update request containing role ID and permission IDs</param>
        /// <response code="200">Permissions updated successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - insufficient permissions</response>
        /// <response code="404">Role not found</response>
        [HttpPut("roles/permissions")]
        [ProducesResponseType(typeof(RolePermissionResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateRolePermissions([FromBody] UpdateRolePermissionsRequest request)
        {
            try
            {
                if (request == null || request.RoleId == Guid.Empty)
                    return ErrorResponse("Invalid request data");

                _logger.LogInformation(
                    "User {UserId} updating permissions for role {RoleId}. Incoming permissions: {Count}",
                    CurrentUserId, request.RoleId, request.PermissionIds?.Count ?? 0);

                // ✅ FIX: Check for "Role.Write" instead of "Roles.AssignPermissions"
                if (!HasPermission(PermissionKeys.RoleWrite))
                {
                    LogUserAuthorization(PermissionKeys.RoleWrite, request.RoleId.ToString(), false, "Missing permission");
                    return ForbiddenResponse("You do not have permission to assign permissions to roles");
                }

                request.PermissionIds = request.PermissionIds?.Distinct().ToList() ?? new List<Guid>();

                // 1. Validate role exists and tenant access
                var role = await _repository.Role
                    .FindByCondition(r => r.Id == request.RoleId, false)
                    .FirstOrDefaultAsync();

                if (role == null)
                    return NotFoundResponse("Role not found");

                // Validate tenant access for non-system roles
                if (!IsSuperAdmin)
                {
                    if (!role.IsSystemRole && role.TenantId != Guid.Empty && role.TenantId != CurrentTenantId)
                    {
                        LogUserAuthorization(PermissionKeys.RoleWrite, request.RoleId.ToString(), false, "Tenant mismatch");
                        return ForbiddenResponse("Role does not belong to your organization");
                    }
                }

                // 2. Validate all permissions exist
                if (request.PermissionIds.Any())
                {
                    var existingPermissions = await _repository.Permission
                        .FindByCondition(p => request.PermissionIds.Contains(p.Id), false)
                        .ToListAsync();

                    if (existingPermissions.Count != request.PermissionIds.Count)
                    {
                        return ErrorResponse("One or more permissions not found");
                    }
                }

                // 3. Get current role permissions
                var currentRolePermissions = await _repository.RolePermission
                    .FindByCondition(rp => rp.RoleId == request.RoleId, true)
                    .ToListAsync();

                var currentPermissionIds = currentRolePermissions.Select(rp => rp.PermissionId).ToHashSet();

                // 4. Calculate differences
                var permissionsToRemove = currentPermissionIds.Except(request.PermissionIds).ToList();
                var permissionsToAdd = request.PermissionIds.Except(currentPermissionIds).ToList();

                _logger.LogInformation(
                    "Role {RoleId}: Removing {RemoveCount}, Adding {AddCount}",
                    request.RoleId, permissionsToRemove.Count, permissionsToAdd.Count);

                // 5. Remove old permissions
                foreach (var rp in currentRolePermissions.Where(rp => permissionsToRemove.Contains(rp.PermissionId)))
                {
                    _repository.RolePermission.Delete(rp);
                }

                // 6. Add new permissions
                foreach (var permissionId in permissionsToAdd)
                {
                    _repository.RolePermission.Create(new RolePermission
                    {
                        Id = Guid.NewGuid(),
                        RoleId = request.RoleId,
                        PermissionId = permissionId,
                        Status = Domain.Enums.EntityStatus.Active,
                        CreatedOn = DateTime.UtcNow,
                        UpdatedOn = DateTime.UtcNow,
                        CreatedBy = CurrentUserId,
                        UpdatedBy = CurrentUserId
                    });
                }

                await _repository.SaveAsync();

                _logger.LogInformation("Successfully updated permissions for role {RoleId}", request.RoleId);
                await LogUserActivityAsync("role.permissions.update",
                    $"Updated permissions for role {request.RoleId}: +{permissionsToAdd.Count} -{permissionsToRemove.Count}");

                // Get updated role with permissions
                var updatedRole = await GetRoleWithPermissionsDto(request.RoleId);

                var result = RolePermissionResult.Successful(
                    "Role permissions updated successfully",
                    updatedRole);

                return SuccessResponse(result, "Role permissions updated successfully");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating permissions for role {RoleId}", request?.RoleId);
                return ErrorResponse("Failed to update permissions due to database error", 500);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permissions for role {RoleId}", request?.RoleId);
                return ErrorResponse("Failed to update role permissions", 500);
            }
        }

        /// <summary>
        /// Add permissions to a role
        /// </summary>
        /// <param name="request">Request containing role ID and permission IDs to add</param>
        /// <response code="200">Permissions added successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - insufficient permissions</response>
        /// <response code="404">Role or permissions not found</response>
        [HttpPost("roles/permissions/add")]
        [ProducesResponseType(typeof(RolePermissionResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddPermissionsToRole([FromBody] AddPermissionsRequest request)
        {
            try
            {
                if (request == null || request.RoleId == Guid.Empty || !request.PermissionIds.Any())
                    return ErrorResponse("Invalid request data");

                _logger.LogInformation("User {UserId} adding {Count} permissions to role {RoleId}",
                    CurrentUserId, request.PermissionIds.Count, request.RoleId);

                // ✅ FIX: Check for "Role.Write" instead of "Roles.AssignPermissions"
                if (!HasPermission(PermissionKeys.RoleWrite))
                {
                    LogUserAuthorization(PermissionKeys.RoleWrite, request.RoleId.ToString(), false, "Missing permission");
                    return ForbiddenResponse("You do not have permission to assign permissions to roles");
                }

                // Validate role
                var role = await _repository.Role
                    .FindByCondition(r => r.Id == request.RoleId, false)
                    .FirstOrDefaultAsync();

                if (role == null)
                    return NotFoundResponse("Role not found");

                // Validate tenant access
                if (!IsSuperAdmin)
                {
                    if (!role.IsSystemRole && role.TenantId != Guid.Empty && role.TenantId != CurrentTenantId)
                    {
                        LogUserAuthorization(PermissionKeys.RoleWrite, request.RoleId.ToString(), false, "Tenant mismatch");
                        return ForbiddenResponse("Role does not belong to your organization");
                    }
                }

                // Get existing permissions
                var existingPermissionIds = await _repository.RolePermission
                    .FindByCondition(rp => rp.RoleId == request.RoleId, false)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync();

                // Find new permissions to add
                var newPermissionIds = request.PermissionIds.Distinct().Except(existingPermissionIds).ToList();

                if (!newPermissionIds.Any())
                {
                    var existingRole = await GetRoleWithPermissionsDto(request.RoleId);
                    var existingResult = RolePermissionResult.Successful(
                        "Permissions already assigned to role",
                        existingRole);
                    return SuccessResponse(existingResult, "Permissions already assigned to role");
                }

                // Validate permissions exist
                var permissions = await _repository.Permission
                    .FindByCondition(p => newPermissionIds.Contains(p.Id), false)
                    .ToListAsync();

                if (permissions.Count != newPermissionIds.Count)
                    return ErrorResponse("One or more permissions not found");

                // Add new permissions
                foreach (var permissionId in newPermissionIds)
                {
                    _repository.RolePermission.Create(new RolePermission
                    {
                        Id = Guid.NewGuid(),
                        RoleId = request.RoleId,
                        PermissionId = permissionId,
                        Status = Domain.Enums.EntityStatus.Active,
                        CreatedOn = DateTime.UtcNow,
                        UpdatedOn = DateTime.UtcNow,
                        CreatedBy = CurrentUserId,
                        UpdatedBy = CurrentUserId
                    });
                }

                await _repository.SaveAsync();

                _logger.LogInformation("Successfully added {Count} permissions to role {RoleId}",
                    newPermissionIds.Count, request.RoleId);
                await LogUserActivityAsync("role.permissions.add",
                    $"Added {newPermissionIds.Count} permissions to role {request.RoleId}");

                var updatedRole = await GetRoleWithPermissionsDto(request.RoleId);
                var result = RolePermissionResult.Successful(
                    $"Added {newPermissionIds.Count} permission(s) to role",
                    updatedRole);

                return SuccessResponse(result, $"Added {newPermissionIds.Count} permission(s) to role");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding permissions to role {RoleId}", request?.RoleId);
                return ErrorResponse("An error occurred while adding permissions", 500);
            }
        }

        /// <summary>
        /// Get all roles with their permissions
        /// </summary>
        /// <response code="200">Returns all roles with permissions</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        [HttpGet("roles")]
        [ProducesResponseType(typeof(List<RoleWithPermissionsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllRolesWithPermissions()
        {
            try
            {
                _logger.LogInformation("User {UserId} getting all roles with permissions", CurrentUserId);

                // ✅ FIX: Check for "Role.Read" instead of "Roles.View"
                if (!HasPermission(PermissionKeys.RoleRead))
                {
                    LogUserAuthorization(PermissionKeys.RoleRead, "ALL_ROLES", false, "Missing permission");
                    return ForbiddenResponse("You do not have permission to view roles");
                }

                // Build the base query with includes
                IQueryable<Role> rolesQuery = _repository.Role
                    .FindAll(false)
                    .Include(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission);

                // Apply tenant filtering for non-superadmin
                if (!IsSuperAdmin)
                {
                    rolesQuery = rolesQuery.Where(r =>
                        r.IsSystemRole ||
                        r.TenantId == CurrentTenantId ||
                        r.TenantId == Guid.Empty);
                }

                var roles = await rolesQuery.ToListAsync();

                var result = roles.Select(role =>
                {
                    var permissions = role.RolePermissions?
                        .Where(rp => rp.Permission != null)
                        .Select(rp => new PermissionDto
                        {
                            Id = rp.PermissionId,
                            Key = rp.Permission!.Key ?? string.Empty,
                            DisplayName = rp.Permission.DisplayName ?? string.Empty,
                            GroupName = rp.Permission.GroupName,
                            Description = rp.Permission.Description,
                            IsAssigned = true
                        })
                        .OrderBy(p => p.GroupName)
                        .ThenBy(p => p.DisplayName)
                        .ToList() ?? new List<PermissionDto>();

                    return new RoleWithPermissionsDto
                    {
                        RoleId = role.Id,
                        RoleName = role.Name ?? string.Empty,
                        Description = role.Description,
                        IsSystemRole = role.IsSystemRole,
                        TenantId = role.TenantId,
                        Permissions = permissions,
                        TotalPermissions = permissions.Count
                    };
                }).ToList();

                await LogUserActivityAsync(
                    "roles.view_all",
                    $"Retrieved {result.Count} roles with permissions");

                return SuccessResponse(result, $"Retrieved {result.Count} roles");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all roles with permissions");
                return ErrorResponse("An error occurred while fetching roles", 500);
            }
        }

        /// <summary>
        /// Add a single permission to a role
        /// </summary>
        /// <param name="roleId">Role ID</param>
        /// <param name="permissionId">Permission ID</param>
        /// <response code="200">Permission added successfully</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - insufficient permissions</response>
        /// <response code="404">Role or permission not found</response>
        [HttpPost("roles/{roleId:guid}/permissions/{permissionId:guid}")]
        [ProducesResponseType(typeof(RolePermissionResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddPermissionToRole(Guid roleId, Guid permissionId)
        {
            var request = new AddPermissionsRequest
            {
                RoleId = roleId,
                PermissionIds = new List<Guid> { permissionId }
            };

            return await AddPermissionsToRole(request);
        }

        /// <summary>
        /// Remove a permission from a role
        /// </summary>
        /// <param name="roleId">Role ID</param>
        /// <param name="permissionId">Permission ID</param>
        /// <response code="200">Permission removed successfully</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - insufficient permissions</response>
        /// <response code="404">Role or permission not found</response>
        [HttpDelete("roles/{roleId:guid}/permissions/{permissionId:guid}")]
        [ProducesResponseType(typeof(RolePermissionResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemovePermissionFromRole(Guid roleId, Guid permissionId)
        {
            try
            {
                _logger.LogInformation("User {UserId} removing permission {PermissionId} from role {RoleId}",
                    CurrentUserId, permissionId, roleId);

                // ✅ FIX: Check for "Role.Write" instead of "Roles.AssignPermissions"
                if (!HasPermission(PermissionKeys.RoleWrite))
                {
                    LogUserAuthorization(PermissionKeys.RoleWrite, roleId.ToString(), false, "Missing permission");
                    return ForbiddenResponse("You do not have permission to modify role permissions");
                }

                // Validate role
                var role = await _repository.Role
                    .FindByCondition(r => r.Id == roleId, false)
                    .FirstOrDefaultAsync();

                if (role == null)
                    return NotFoundResponse("Role not found");

                // Validate tenant access
                if (!IsSuperAdmin)
                {
                    if (!role.IsSystemRole && role.TenantId != Guid.Empty && role.TenantId != CurrentTenantId)
                    {
                        LogUserAuthorization(PermissionKeys.RoleWrite, roleId.ToString(), false, "Tenant mismatch");
                        return ForbiddenResponse("Role does not belong to your organization");
                    }
                }

                var rolePermission = await _repository.RolePermission
                    .FindByCondition(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, true)
                    .FirstOrDefaultAsync();

                if (rolePermission == null)
                {
                    return NotFoundResponse("Permission not assigned to role");
                }

                _repository.RolePermission.Delete(rolePermission);
                await _repository.SaveAsync();

                _logger.LogInformation("Successfully removed permission {PermissionId} from role {RoleId}",
                    permissionId, roleId);
                await LogUserActivityAsync("role.permissions.remove",
                    $"Removed permission {permissionId} from role {roleId}");

                var updatedRole = await GetRoleWithPermissionsDto(roleId);
                var result = RolePermissionResult.Successful(
                    "Permission removed from role",
                    updatedRole);

                return SuccessResponse(result, "Permission removed from role successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing permission {PermissionId} from role {RoleId}",
                    permissionId, roleId);
                return ErrorResponse("An error occurred while removing permission", 500);
            }
        }

        /// <summary>
        /// Remove all permissions from a role
        /// </summary>
        /// <param name="roleId">Role ID</param>
        /// <response code="200">All permissions removed successfully</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - insufficient permissions</response>
        /// <response code="404">Role not found</response>
        [HttpDelete("roles/{roleId:guid}/permissions")]
        [ProducesResponseType(typeof(RolePermissionResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveAllPermissionsFromRole(Guid roleId)
        {
            try
            {
                _logger.LogInformation("User {UserId} removing all permissions from role {RoleId}",
                    CurrentUserId, roleId);

                // ✅ FIX: Check for "Role.Write" instead of "Roles.AssignPermissions"
                if (!HasPermission(PermissionKeys.RoleWrite))
                {
                    LogUserAuthorization(PermissionKeys.RoleWrite, roleId.ToString(), false, "Missing permission");
                    return ForbiddenResponse("You do not have permission to modify role permissions");
                }

                // Validate role
                var role = await _repository.Role
                    .FindByCondition(r => r.Id == roleId, false)
                    .FirstOrDefaultAsync();

                if (role == null)
                    return NotFoundResponse("Role not found");

                // Validate tenant access
                if (!IsSuperAdmin)
                {
                    if (!role.IsSystemRole && role.TenantId != Guid.Empty && role.TenantId != CurrentTenantId)
                    {
                        LogUserAuthorization(PermissionKeys.RoleWrite, roleId.ToString(), false, "Tenant mismatch");
                        return ForbiddenResponse("Role does not belong to your organization");
                    }
                }

                var rolePermissions = await _repository.RolePermission
                    .FindByCondition(rp => rp.RoleId == roleId, true)
                    .ToListAsync();

                foreach (var rp in rolePermissions)
                {
                    _repository.RolePermission.Delete(rp);
                }

                await _repository.SaveAsync();

                _logger.LogInformation("Successfully removed {Count} permissions from role {RoleId}",
                    rolePermissions.Count, roleId);
                await LogUserActivityAsync("role.permissions.remove_all",
                    $"Removed all {rolePermissions.Count} permissions from role {roleId}");

                var updatedRole = await GetRoleWithPermissionsDto(roleId);
                var result = RolePermissionResult.Successful(
                    "All permissions removed from role",
                    updatedRole);

                return SuccessResponse(result, "All permissions removed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing all permissions from role {RoleId}", roleId);
                return ErrorResponse("An error occurred while removing permissions", 500);
            }
        }

        /// <summary>
        /// Clone permissions from one role to another
        /// </summary>
        /// <param name="request">Request containing source and target role IDs</param>
        /// <response code="200">Permissions cloned successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - insufficient permissions</response>
        /// <response code="404">Source or target role not found</response>
        [HttpPost("roles/permissions/clone")]
        [ProducesResponseType(typeof(RolePermissionResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CloneRolePermissions([FromBody] ClonePermissionsRequest request)
        {
            try
            {
                if (request == null ||
                    request.SourceRoleId == Guid.Empty ||
                    request.TargetRoleId == Guid.Empty)
                {
                    return ErrorResponse("Invalid request data");
                }

                if (request.SourceRoleId == request.TargetRoleId)
                {
                    return ErrorResponse("Source and target roles cannot be the same");
                }

                _logger.LogInformation("User {UserId} cloning permissions from role {SourceId} to role {TargetId}",
                    CurrentUserId, request.SourceRoleId, request.TargetRoleId);

                // ✅ FIX: Check for "Role.Write" instead of "Roles.AssignPermissions"
                if (!HasPermission(PermissionKeys.RoleWrite))
                {
                    LogUserAuthorization(PermissionKeys.RoleWrite, request.TargetRoleId.ToString(), false, "Missing permission");
                    return ForbiddenResponse("You do not have permission to assign permissions to roles");
                }

                // Get source role permissions
                var sourcePermissions = await _repository.RolePermission
                    .FindByCondition(rp => rp.RoleId == request.SourceRoleId, false)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync();

                if (!sourcePermissions.Any())
                {
                    return ErrorResponse("Source role has no permissions to clone");
                }

                // Update target role with source permissions
                var updateRequest = new UpdateRolePermissionsRequest
                {
                    RoleId = request.TargetRoleId,
                    PermissionIds = sourcePermissions,
                    TenantId = request.TenantId
                };

                await LogUserActivityAsync("role.permissions.clone",
                    $"Cloned {sourcePermissions.Count} permissions from role {request.SourceRoleId} to {request.TargetRoleId}");

                return await UpdateRolePermissions(updateRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cloning permissions from role {SourceId} to {TargetId}",
                    request?.SourceRoleId, request?.TargetRoleId);
                return ErrorResponse("An error occurred while cloning permissions", 500);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Helper method to get role with permissions as DTO
        /// </summary>
        private async Task<RoleWithPermissionsDto?> GetRoleWithPermissionsDto(Guid roleId)
        {
            var role = await _repository.Role
                .FindByCondition(r => r.Id == roleId, false)
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync();

            if (role == null)
                return null;

            var permissions = role.RolePermissions?
                .Where(rp => rp.Permission != null)
                .Select(rp => new PermissionDto
                {
                    Id = rp.PermissionId,
                    Key = rp.Permission!.Key ?? string.Empty,
                    DisplayName = rp.Permission.DisplayName ?? string.Empty,
                    GroupName = rp.Permission.GroupName,
                    Description = rp.Permission.Description,
                    IsAssigned = true
                })
                .OrderBy(p => p.GroupName)
                .ThenBy(p => p.DisplayName)
                .ToList() ?? new List<PermissionDto>();

            // Add user counts
            foreach (var permission in permissions)
            {
                permission.UserCount = await GetPermissionUserCountAsync(permission.Id);
            }

            return new RoleWithPermissionsDto
            {
                RoleId = role.Id,
                RoleName = role.Name ?? string.Empty,
                Description = role.Description,
                IsSystemRole = role.IsSystemRole,
                TenantId = role.TenantId,
                Permissions = permissions,
                TotalPermissions = permissions.Count
            };
        }

        /// <summary>
        /// Get the number of users who have a specific permission
        /// </summary>
        private async Task<int> GetPermissionUserCountAsync(Guid permissionId)
        {
            // Get all users who have this permission through any of their roles
            var userCount = await _repository.UserRole
                .FindAll(false)
                .Include(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                .Where(ur => ur.Role!.RolePermissions!.Any(rp => rp.PermissionId == permissionId))
                .Where(ur => ur.User!.IsActive) // Only count active users
                .Select(ur => ur.UserId)
                .Distinct()
                .CountAsync();

            return userCount;
        }

        #endregion
    }
}