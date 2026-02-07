using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.RoleAssignment;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.IRolesAssignment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public RoleAssignmentController(
            IRoleAssignmentService roleAssignmentService,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _roleAssignmentService = roleAssignmentService
                ?? throw new ArgumentNullException(nameof(roleAssignmentService));
        }

        #region Users

        [HttpGet("all-users")]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!IsSuperAdmin && !HasPermission("RoleAssignment.Read"))
                return ForbiddenResponse("You do not have permission to view users.");

            pageNumber = Math.Max(1, pageNumber);
            pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

            // SuperAdmin can see users across all tenants
            var tenantId = IsSuperAdmin ? Guid.Empty : CurrentTenantId;

            var users = await _roleAssignmentService.GetAllUsersWithRolesAsync(
                tenantId,
                pageNumber,
                pageSize);

            return SuccessResponse(users, "Users retrieved successfully");
        }

        [HttpGet("search-users")]
        public async Task<IActionResult> SearchUsers([FromQuery] string searchTerm)
        {
            if (!IsSuperAdmin && !HasPermission("RoleAssignment.Read"))
                return ForbiddenResponse("You do not have permission to search users.");

            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
                return ValidationErrorResponse(new Dictionary<string, string[]>
                {
                    { "searchTerm", new[] { "Search term must be at least 2 characters." } }
                });

            // SuperAdmin can search users across all tenants
            var tenantId = IsSuperAdmin ? Guid.Empty : CurrentTenantId;

            var users = await _roleAssignmentService.SearchUsersAsync(searchTerm, tenantId);
            return SuccessResponse(users, $"Found {users.Count} user(s)");
        }

        [HttpGet("user/{userId:guid}/roles")]
        public async Task<IActionResult> GetUserWithRoles(Guid userId)
        {
            if (!IsSuperAdmin && !HasPermission("RoleAssignment.Read"))
                return ForbiddenResponse("You do not have permission to view user roles.");

            // SuperAdmin can view any user's roles
            var tenantId = IsSuperAdmin ? Guid.Empty : CurrentTenantId;

            var user = await _roleAssignmentService.GetUserWithRolesAsync(userId, tenantId);
            if (user == null)
                return NotFoundResponse("User not found.");

            return SuccessResponse(user, "User roles retrieved successfully");
        }

        #endregion

        #region Role Assignments

        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
        {
            if (!IsSuperAdmin && !HasPermission("RoleAssignment.Write"))
                return ForbiddenResponse("You do not have permission to assign roles.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            // SuperAdmin can assign roles to any user
            var tenantId = IsSuperAdmin ? Guid.Empty : CurrentTenantId;

            var result = await _roleAssignmentService.AssignRoleToUserAsync(
                request.UserId,
                request.RoleId,
                tenantId);

            if (!result.Success)
                return ErrorResponse(result.Message ?? "Failed to assign role.");

            await LogUserActivityAsync(
                "role.assign",
                $"Assigned role {request.RoleId} to user {request.UserId}");

            return SuccessResponse(result.User, result.Message ?? "Role assigned successfully");
        }

        [HttpPost("assign-multiple-roles")]
        public async Task<IActionResult> AssignMultipleRoles([FromBody] AssignMultipleRolesRequest request)
        {
            if (!IsSuperAdmin && !HasPermission("RoleAssignment.Write"))
                return ForbiddenResponse("You do not have permission to assign roles.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            // SuperAdmin can assign roles to any user
            var tenantId = IsSuperAdmin ? Guid.Empty : CurrentTenantId;

            var result = await _roleAssignmentService.AssignMultipleRolesToUserAsync(
                request.UserId,
                request.RoleIds,
                tenantId);

            if (!result.Success)
                return ErrorResponse(result.Message ?? "Failed to assign roles.");

            await LogUserActivityAsync(
                "role.assign.multiple",
                $"Assigned multiple roles to user {request.UserId}");

            return SuccessResponse(result.User, result.Message ?? "Roles assigned successfully");
        }

        [HttpPost("remove-role")]
        public async Task<IActionResult> RemoveRole([FromBody] RemoveRoleRequest request)
        {
            if (!IsSuperAdmin && !HasPermission("RoleAssignment.Write"))
                return ForbiddenResponse("You do not have permission to remove roles.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            // SuperAdmin can remove roles from any user
            var tenantId = IsSuperAdmin ? Guid.Empty : CurrentTenantId;

            var result = await _roleAssignmentService.RemoveRoleFromUserAsync(
                request.UserId,
                request.RoleId,
                tenantId);

            if (!result.Success)
                return ErrorResponse(result.Message ?? "Failed to remove role.");

            await LogUserActivityAsync(
                "role.remove",
                $"Removed role {request.RoleId} from user {request.UserId}");

            return SuccessResponse(result.User, result.Message ?? "Role removed successfully");
        }

        [HttpPut("update-user-roles")]
        public async Task<IActionResult> UpdateUserRoles([FromBody] UpdateUserRolesRequest request)
        {
            if (!IsSuperAdmin && !HasPermission("RoleAssignment.Write"))
                return ForbiddenResponse("You do not have permission to update roles.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            // SuperAdmin can update roles for any user
            var tenantId = IsSuperAdmin ? Guid.Empty : CurrentTenantId;

            var result = await _roleAssignmentService.UpdateUserRolesAsync(
                request.UserId,
                request.RoleIds,
                tenantId);

            if (!result.Success)
                return ErrorResponse(result.Message ?? "Failed to update user roles.");

            await LogUserActivityAsync(
                "role.update",
                $"Updated roles for user {request.UserId}");

            return SuccessResponse(result.User, result.Message ?? "User roles updated successfully");
        }

        [HttpDelete("user/{userId:guid}/roles")]
        public async Task<IActionResult> RemoveAllRoles(Guid userId)
        {
            if (!IsSuperAdmin && !HasPermission("RoleAssignment.Write"))
                return ForbiddenResponse("You do not have permission to remove roles.");

            // SuperAdmin can remove roles from any user
            var tenantId = IsSuperAdmin ? Guid.Empty : CurrentTenantId;

            var result = await _roleAssignmentService.RemoveAllRolesFromUserAsync(
                userId,
                tenantId);

            if (!result.Success)
                return ErrorResponse(result.Message ?? "Failed to remove roles.");

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
            if (!IsSuperAdmin && !HasPermission("RoleAssignment.Read"))
                return ForbiddenResponse("You do not have permission to view role users.");

            pageNumber = Math.Max(1, pageNumber);
            pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

            // SuperAdmin can view users for any role
            var tenantId = IsSuperAdmin ? Guid.Empty : CurrentTenantId;

            var users = await _roleAssignmentService.GetUsersByRoleAsync(
                roleId,
                tenantId,
                pageNumber,
                pageSize);

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
            try
            {
                // SuperAdmin can see all roles
                if (IsSuperAdmin)
                {
                    var allRoles = await _roleAssignmentService.GetAllRolesAsync();
                    return SuccessResponse(allRoles, "All available roles retrieved successfully");
                }

                // School users need permission and see only their tenant's roles
                if (!HasPermission("RoleAssignment.Read"))
                    return ForbiddenResponse("You do not have permission to view roles.");

                // Fix: CurrentTenantId is Guid? but GetAvailableRolesAsync expects Guid
                var tenantId = CurrentTenantId ?? Guid.Empty;
                var roles = await _roleAssignmentService.GetAvailableRolesAsync(tenantId);
                return SuccessResponse(roles, "Available roles retrieved successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse(
                    $"Failed to retrieve roles: {ex.Message}",
                    Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("user/{userId:guid}/has-role/{roleId:guid}")]
        public async Task<IActionResult> UserHasRole(Guid userId, Guid roleId)
        {
            if (!IsSuperAdmin && !HasPermission("RoleAssignment.Read"))
                return ForbiddenResponse("You do not have permission to view role assignments.");

            // SuperAdmin can check any user's roles
            var tenantId = IsSuperAdmin ? Guid.Empty : CurrentTenantId;

            var hasRole = await _roleAssignmentService.UserHasRoleAsync(
                userId,
                roleId,
                tenantId);

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