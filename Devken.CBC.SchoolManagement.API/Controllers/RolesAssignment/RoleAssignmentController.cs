using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.RoleAssignment;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.IRolesAssignment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Api.Controllers.RolesAssignment
{
    [ApiController]
    [Route("api/role-assignments")]
    [Authorize]
    public class RoleAssignmentController : BaseApiController
    {
        private readonly IRoleAssignmentService _roleAssignmentService;
        private readonly ILogger<RoleAssignmentController> _logger;

        public RoleAssignmentController(
            IRoleAssignmentService roleAssignmentService,
            IUserActivityService activityService,
            ILogger<RoleAssignmentController> logger)
            : base(activityService, logger)
        {
            _roleAssignmentService = roleAssignmentService
                ?? throw new ArgumentNullException(nameof(roleAssignmentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Users

        [HttpGet("all-users")]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            LogUserAuthorization("GetAllUsers");

            // FIX: Changed from "RoleAssignment.Read" to "User.Read" to match JWT permissions
            if (!IsSuperAdmin && !HasPermission("User.Read"))
            {
                _logger.LogWarning("User {UserId} attempted to view users without permission", CurrentUserId);
                return ForbiddenResponse("You do not have permission to view users.");
            }

            pageNumber = Math.Max(1, pageNumber);
            pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

            // SuperAdmin can see users across all tenants
            var tenantId = IsSuperAdmin ? Guid.Empty : CurrentTenantId;

            _logger.LogInformation(
                "Retrieving all users - TenantId: {TenantId}, Page: {PageNumber}, PageSize: {PageSize}, RequestedBy: {UserId}",
                tenantId, pageNumber, pageSize, CurrentUserId);

            var users = await _roleAssignmentService.GetAllUsersWithRolesAsync(
                tenantId,
                pageNumber,
                pageSize);

            // _logger.LogInformation("Retrieved {Count} users", users?.Count ?? 0);

            return SuccessResponse(users, "Users retrieved successfully");
        }

        // In RoleAssignmentController or a new StatisticsController
        [HttpGet("role-user-counts")]
        public async Task<IActionResult> GetRoleUserCounts()
        {
            LogUserAuthorization("GetRoleUserCounts");

            if (!IsSuperAdmin && !HasPermission("Role.Read"))
            {
                _logger.LogWarning("User {UserId} attempted to view role user counts without permission", CurrentUserId);
                return ForbiddenResponse("You do not have permission to view role statistics.");
            }

            // FIX: Ensure tenantId is Guid, not Guid?
            var tenantId = IsSuperAdmin ? Guid.Empty : (CurrentTenantId ?? Guid.Empty);

            _logger.LogInformation(
                "Retrieving user counts for all roles in tenant {TenantId}, requested by {UserId}",
                tenantId, CurrentUserId);

            try
            {
                var roleUserCounts = await _roleAssignmentService.GetRoleUserCountsAsync(tenantId);

                _logger.LogInformation("Retrieved user counts for {Count} roles", roleUserCounts?.Count ?? 0);

                return SuccessResponse(roleUserCounts, "Role user counts retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve role user counts for tenant {TenantId}", tenantId);
                return ErrorResponse(
                    $"Failed to retrieve role user counts: {ex.Message}",
                    StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("search-users")]
        public async Task<IActionResult> SearchUsers([FromQuery] string searchTerm)
        {
            LogUserAuthorization("SearchUsers");

            // FIX: Changed from "RoleAssignment.Read" to "User.Read" to match JWT permissions
            if (!IsSuperAdmin && !HasPermission("User.Read"))
            {
                _logger.LogWarning("User {UserId} attempted to search users without permission", CurrentUserId);
                return ForbiddenResponse("You do not have permission to search users.");
            }

            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            {
                _logger.LogWarning("Invalid search term provided: {SearchTerm}", searchTerm);
                return ValidationErrorResponse(new Dictionary<string, string[]>
                {
                    { "searchTerm", new[] { "Search term must be at least 2 characters." } }
                });
            }

            // SuperAdmin can search users across all tenants
            var tenantId = IsSuperAdmin ? Guid.Empty : CurrentTenantId;

            _logger.LogInformation(
                "Searching users - SearchTerm: {SearchTerm}, TenantId: {TenantId}, RequestedBy: {UserId}",
                searchTerm, tenantId, CurrentUserId);

            var users = await _roleAssignmentService.SearchUsersAsync(searchTerm, tenantId);

            _logger.LogInformation("Found {Count} user(s) matching search term '{SearchTerm}'", users.Count, searchTerm);

            return SuccessResponse(users, $"Found {users.Count} user(s)");
        }

        [HttpGet("user/{userId:guid}/roles")]
        public async Task<IActionResult> GetUserWithRoles(Guid userId)
        {
            LogUserAuthorization($"GetUserWithRoles:{userId}");

            // FIX: Changed from "RoleAssignment.Read" to "User.Read" to match JWT permissions
            if (!IsSuperAdmin && !HasPermission("User.Read"))
            {
                _logger.LogWarning("User {CurrentUserId} attempted to view roles for user {UserId} without permission", CurrentUserId, userId);
                return ForbiddenResponse("You do not have permission to view user roles.");
            }

            // SuperAdmin can view any user's roles
            var tenantId = IsSuperAdmin ? Guid.Empty : CurrentTenantId;

            _logger.LogInformation(
                "Retrieving roles for user {UserId} in tenant {TenantId}, requested by {CurrentUserId}",
                userId, tenantId, CurrentUserId);

            var user = await _roleAssignmentService.GetUserWithRolesAsync(userId, tenantId);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found in tenant {TenantId}", userId, tenantId);
                return NotFoundResponse("User not found.");
            }

            _logger.LogInformation("Retrieved {RoleCount} role(s) for user {UserId}", user.Roles?.Count ?? 0, userId);

            return SuccessResponse(user, "User roles retrieved successfully");
        }

        #endregion

        #region Role Assignments

        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
        {
            LogUserAuthorization("AssignRole");

            // FIX: Changed from "RoleAssignment.Write" to "Role.Write" to match JWT permissions
            if (!IsSuperAdmin && !HasPermission("Role.Write"))
            {
                _logger.LogWarning("User {UserId} attempted to assign role without permission", CurrentUserId);
                return ForbiddenResponse("You do not have permission to assign roles.");
            }

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            // SuperAdmin can assign roles to any user
            var tenantId = IsSuperAdmin ? Guid.Empty : CurrentTenantId;

            _logger.LogInformation(
                "Assigning role {RoleId} to user {UserId} in tenant {TenantId} by {AssignedBy}",
                request.RoleId, request.UserId, tenantId, CurrentUserId);

            var result = await _roleAssignmentService.AssignRoleToUserAsync(
                request.UserId,
                request.RoleId,
                tenantId);

            if (!result.Success)
            {
                _logger.LogError(
                    "Failed to assign role {RoleId} to user {UserId}: {Message}",
                    request.RoleId, request.UserId, result.Message);
                return ErrorResponse(result.Message ?? "Failed to assign role.");
            }

            _logger.LogInformation(
                "Successfully assigned role {RoleId} to user {UserId}",
                request.RoleId, request.UserId);

            await LogUserActivityAsync(
                "role.assign",
                $"Assigned role {request.RoleId} to user {request.UserId}");

            return SuccessResponse(result.User, result.Message ?? "Role assigned successfully");
        }

        [HttpPost("assign-multiple-roles")]
        public async Task<IActionResult> AssignMultipleRoles([FromBody] AssignMultipleRolesRequest request)
        {
            LogUserAuthorization("AssignMultipleRoles");

            // FIX: Changed from "RoleAssignment.Write" to "Role.Write" to match JWT permissions
            if (!IsSuperAdmin && !HasPermission("Role.Write"))
            {
                _logger.LogWarning("User {UserId} attempted to assign multiple roles without permission", CurrentUserId);
                return ForbiddenResponse("You do not have permission to assign roles.");
            }

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            if (request.RoleIds == null || !request.RoleIds.Any())
            {
                _logger.LogWarning("Attempt to assign multiple roles to user {UserId} without providing any roles", request.UserId);
                return ErrorResponse("At least one role must be provided.");
            }

            // SuperAdmin can assign roles to any user
            var tenantId = IsSuperAdmin ? Guid.Empty : CurrentTenantId;

            _logger.LogInformation(
                "Assigning {RoleCount} role(s) to user {UserId} in tenant {TenantId} by {AssignedBy}",
                request.RoleIds.Count, request.UserId, tenantId, CurrentUserId);

            var result = await _roleAssignmentService.AssignMultipleRolesToUserAsync(
                request.UserId,
                request.RoleIds,
                tenantId);

            if (!result.Success)
            {
                _logger.LogError(
                    "Failed to assign {RoleCount} role(s) to user {UserId}: {Message}",
                    request.RoleIds.Count, request.UserId, result.Message);
                return ErrorResponse(result.Message ?? "Failed to assign roles.");
            }

            _logger.LogInformation(
                "Successfully assigned {RoleCount} role(s) to user {UserId}",
                request.RoleIds.Count, request.UserId);

            await LogUserActivityAsync(
                "role.assign.multiple",
                $"Assigned {request.RoleIds.Count} role(s) to user {request.UserId}");

            return SuccessResponse(result.User, result.Message ?? "Roles assigned successfully");
        }

        [HttpPost("remove-role")]
        public async Task<IActionResult> RemoveRole([FromBody] RemoveRoleRequest request)
        {
            LogUserAuthorization("RemoveRole");

            // FIX: Changed from "RoleAssignment.Write" to "Role.Write" to match JWT permissions
            if (!IsSuperAdmin && !HasPermission("Role.Write"))
            {
                _logger.LogWarning("User {UserId} attempted to remove role without permission", CurrentUserId);
                return ForbiddenResponse("You do not have permission to remove roles.");
            }

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            // Prevent users from modifying their own roles
            if (request.UserId == CurrentUserId)
            {
                _logger.LogWarning("User {UserId} attempted to remove their own role", CurrentUserId);
                return ErrorResponse("You cannot modify your own roles.", StatusCodes.Status400BadRequest);
            }

            // SuperAdmin can remove roles from any user
            var tenantId = IsSuperAdmin ? Guid.Empty : CurrentTenantId;

            _logger.LogInformation(
                "Removing role {RoleId} from user {UserId} in tenant {TenantId} by {RemovedBy}",
                request.RoleId, request.UserId, tenantId, CurrentUserId);

            var result = await _roleAssignmentService.RemoveRoleFromUserAsync(
                request.UserId,
                request.RoleId,
                tenantId);

            if (!result.Success)
            {
                _logger.LogError(
                    "Failed to remove role {RoleId} from user {UserId}: {Message}",
                    request.RoleId, request.UserId, result.Message);
                return ErrorResponse(result.Message ?? "Failed to remove role.");
            }

            _logger.LogInformation(
                "Successfully removed role {RoleId} from user {UserId}",
                request.RoleId, request.UserId);

            await LogUserActivityAsync(
                "role.remove",
                $"Removed role {request.RoleId} from user {request.UserId}");

            return SuccessResponse(result.User, result.Message ?? "Role removed successfully");
        }

        [HttpPut("update-user-roles")]
        public async Task<IActionResult> UpdateUserRoles([FromBody] UpdateUserRolesRequest request)
        {
            LogUserAuthorization("UpdateUserRoles");

            // FIX: Changed from "RoleAssignment.Write" to "Role.Write" to match JWT permissions
            if (!IsSuperAdmin && !HasPermission("Role.Write"))
            {
                _logger.LogWarning("User {UserId} attempted to update roles without permission", CurrentUserId);
                return ForbiddenResponse("You do not have permission to update roles.");
            }

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            if (request.RoleIds == null || !request.RoleIds.Any())
            {
                _logger.LogWarning("Attempt to update roles for user {UserId} without providing any roles", request.UserId);
                return ErrorResponse("At least one role must be assigned to the user.", StatusCodes.Status400BadRequest);
            }

            // Prevent users from modifying their own roles
            if (request.UserId == CurrentUserId)
            {
                _logger.LogWarning("User {UserId} attempted to modify their own roles", CurrentUserId);
                return ErrorResponse("You cannot modify your own roles.", StatusCodes.Status400BadRequest);
            }

            // SuperAdmin can update roles for any user
            var tenantId = IsSuperAdmin ? Guid.Empty : CurrentTenantId;

            _logger.LogInformation(
                "Updating roles for user {UserId} to {RoleCount} role(s) in tenant {TenantId} by {UpdatedBy}",
                request.UserId, request.RoleIds.Count, tenantId, CurrentUserId);

            var result = await _roleAssignmentService.UpdateUserRolesAsync(
                request.UserId,
                request.RoleIds,
                tenantId);

            if (!result.Success)
            {
                _logger.LogError(
                    "Failed to update roles for user {UserId}: {Message}",
                    request.UserId, result.Message);
                return ErrorResponse(result.Message ?? "Failed to update user roles.");
            }

            _logger.LogInformation(
                "Successfully updated roles for user {UserId} to {RoleCount} role(s)",
                request.UserId, request.RoleIds.Count);

            await LogUserActivityAsync(
                "role.update",
                $"Updated roles for user {request.UserId}");

            return SuccessResponse(result.User, result.Message ?? "User roles updated successfully");
        }

        [HttpDelete("user/{userId:guid}/roles")]
        public async Task<IActionResult> RemoveAllRoles(Guid userId)
        {
            LogUserAuthorization($"RemoveAllRoles:{userId}");

            // FIX: Changed from "RoleAssignment.Write" to "Role.Write" to match JWT permissions
            if (!IsSuperAdmin && !HasPermission("Role.Write"))
            {
                _logger.LogWarning("User {CurrentUserId} attempted to remove all roles from user {UserId} without permission", CurrentUserId, userId);
                return ForbiddenResponse("You do not have permission to remove roles.");
            }

            // Prevent users from modifying their own roles
            if (userId == CurrentUserId)
            {
                _logger.LogWarning("User {UserId} attempted to remove all their own roles", CurrentUserId);
                return ErrorResponse("You cannot remove all your own roles.", StatusCodes.Status400BadRequest);
            }

            // SuperAdmin can remove roles from any user
            var tenantId = IsSuperAdmin ? Guid.Empty : CurrentTenantId;

            _logger.LogInformation(
                "Removing all roles from user {UserId} in tenant {TenantId} by {RemovedBy}",
                userId, tenantId, CurrentUserId);

            var result = await _roleAssignmentService.RemoveAllRolesFromUserAsync(
                userId,
                tenantId);

            if (!result.Success)
            {
                _logger.LogError(
                    "Failed to remove all roles from user {UserId}: {Message}",
                    userId, result.Message);
                return ErrorResponse(result.Message ?? "Failed to remove roles.");
            }

            _logger.LogInformation("Successfully removed all roles from user {UserId}", userId);

            await LogUserActivityAsync(
                "role.remove.all",
                $"Removed all roles from user {userId}");

            return SuccessResponse(new { UserId = userId }, "All roles removed successfully");
        }

        #endregion

        #region Roles

        [HttpGet("role/{roleId:guid}/users")]
        public async Task<IActionResult> GetUsersByRole(
            Guid roleId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            LogUserAuthorization($"GetUsersByRole:{roleId}");

            // FIX: Changed from "RoleAssignment.Read" to "Role.Read" to match JWT permissions
            if (!IsSuperAdmin && !HasPermission("Role.Read"))
            {
                _logger.LogWarning("User {UserId} attempted to view users for role {RoleId} without permission", CurrentUserId, roleId);
                return ForbiddenResponse("You do not have permission to view role users.");
            }

            pageNumber = Math.Max(1, pageNumber);
            pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

            // SuperAdmin can view users for any role
            var tenantId = IsSuperAdmin ? Guid.Empty : CurrentTenantId;

            _logger.LogInformation(
                "Retrieving users for role {RoleId} in tenant {TenantId} - Page: {PageNumber}, PageSize: {PageSize}, RequestedBy: {UserId}",
                roleId, tenantId, pageNumber, pageSize, CurrentUserId);

            var users = await _roleAssignmentService.GetUsersByRoleAsync(
                roleId,
                tenantId,
                pageNumber,
                pageSize);

            // _logger.LogInformation("Retrieved {Count} user(s) with role {RoleId}", users?.Count ?? 0, roleId);

            return SuccessResponse(users, "Users retrieved successfully");
        }

        /// <summary>
        /// Get available roles for assignment.
        /// SuperAdmin gets all system roles.
        /// School users get roles for their tenant.
        /// </summary>
        [HttpGet("available-roles")]
        public async Task<IActionResult> GetAvailableRoles()
        {
            LogUserAuthorization("GetAvailableRoles");

            try
            {
                // SuperAdmin can see all roles
                if (IsSuperAdmin)
                {
                    _logger.LogInformation("Retrieving all available roles for SuperAdmin {UserId}", CurrentUserId);
                    var allRoles = await _roleAssignmentService.GetAllRolesAsync();
                    _logger.LogInformation("Retrieved {Count} role(s) for SuperAdmin", allRoles?.Count ?? 0);
                    return SuccessResponse(allRoles, "All available roles retrieved successfully");
                }

                // FIX: Changed from "RoleAssignment.Read" to "Role.Read" to match JWT permissions
                // School users need permission and see only their tenant's roles
                if (!HasPermission("Role.Read"))
                {
                    _logger.LogWarning("User {UserId} attempted to view available roles without permission", CurrentUserId);
                    return ForbiddenResponse("You do not have permission to view roles.");
                }

                // Fix: CurrentTenantId is Guid? but GetAvailableRolesAsync expects Guid
                if (!CurrentTenantId.HasValue)
                {
                    _logger.LogError("Tenant context required but not found for user {UserId}", CurrentUserId);
                    return ErrorResponse(
                        "Tenant context is required for this operation.",
                        StatusCodes.Status400BadRequest);
                }

                var tenantId = CurrentTenantId.Value;

                _logger.LogInformation(
                    "Retrieving available roles for tenant {TenantId}, requested by {UserId}",
                    tenantId, CurrentUserId);

                var roles = await _roleAssignmentService.GetAvailableRolesAsync(tenantId);

                _logger.LogInformation("Retrieved {Count} role(s) for tenant {TenantId}", roles?.Count ?? 0, tenantId);

                return SuccessResponse(roles, "Available roles retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve available roles for user {UserId}", CurrentUserId);
                return ErrorResponse(
                    $"Failed to retrieve roles: {ex.Message}",
                    StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("user/{userId:guid}/has-role/{roleId:guid}")]
        public async Task<IActionResult> UserHasRole(Guid userId, Guid roleId)
        {
            LogUserAuthorization($"UserHasRole:{userId}:{roleId}");

            // FIX: Changed from "RoleAssignment.Read" to "Role.Read" to match JWT permissions
            if (!IsSuperAdmin && !HasPermission("Role.Read"))
            {
                _logger.LogWarning(
                    "User {CurrentUserId} attempted to check role assignment for user {UserId} without permission",
                    CurrentUserId, userId);
                return ForbiddenResponse("You do not have permission to view role assignments.");
            }

            // SuperAdmin can check any user's roles
            var tenantId = IsSuperAdmin ? Guid.Empty : CurrentTenantId;

            _logger.LogInformation(
                "Checking if user {UserId} has role {RoleId} in tenant {TenantId}, requested by {CurrentUserId}",
                userId, roleId, tenantId, CurrentUserId);

            var hasRole = await _roleAssignmentService.UserHasRoleAsync(
                userId,
                roleId,
                tenantId);

            _logger.LogInformation(
                "User {UserId} {HasRole} role {RoleId}",
                userId, hasRole ? "has" : "does not have", roleId);

            return SuccessResponse(
                new { UserId = userId, RoleId = roleId, HasRole = hasRole },
                hasRole ? "User has the role." : "User does not have the role.");
        }

        #endregion

        #region Helpers

        private static IDictionary<string, string[]> ToErrorDictionary(
            Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState) =>
            modelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
                       ?? Array.Empty<string>());

        #endregion
    }
}