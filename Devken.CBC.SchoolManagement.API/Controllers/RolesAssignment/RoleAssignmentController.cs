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

        /// <summary>
        /// Get all users with roles – SuperAdmin only
        /// </summary>
        [HttpGet("all-users")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!HasPermission("RoleAssignment.Read"))
                return ForbiddenResponse("You do not have permission to view users.");

            pageNumber = Math.Max(1, pageNumber);
            pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

            var users = await _roleAssignmentService.GetAllUsersWithRolesAsync(
                CurrentTenantId,
                pageNumber,
                pageSize);

            return SuccessResponse(users, "Users retrieved successfully");
        }

        /// <summary>
        /// Search users – SuperAdmin only
        /// </summary>
        [HttpGet("search-users")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> SearchUsers([FromQuery] string searchTerm)
        {
            if (!HasPermission("RoleAssignment.Read"))
                return ForbiddenResponse("You do not have permission to search users.");

            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
                return ValidationErrorResponse(new Dictionary<string, string[]>
                {
                    { "searchTerm", new[] { "Search term must be at least 2 characters." } }
                });

            var users = await _roleAssignmentService.SearchUsersAsync(searchTerm, CurrentTenantId);
            return SuccessResponse(users, $"Found {users.Count} user(s)");
        }

        /// <summary>
        /// Get user with roles – SuperAdmin only
        /// </summary>
        [HttpGet("user/{userId:guid}/roles")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetUserWithRoles(Guid userId)
        {
            if (!HasPermission("RoleAssignment.Read"))
                return ForbiddenResponse("You do not have permission to view user roles.");

            var user = await _roleAssignmentService.GetUserWithRolesAsync(userId, CurrentTenantId);
            if (user == null)
                return NotFoundResponse("User not found.");

            return SuccessResponse(user, "User roles retrieved successfully");
        }

        #endregion

        #region Role Assignments

        /// <summary>
        /// Assign a role to a user – SuperAdmin only
        /// </summary>
        [HttpPost("assign-role")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
        {
            if (!HasPermission("RoleAssignment.Write"))
                return ForbiddenResponse("You do not have permission to assign roles.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            var result = await _roleAssignmentService.AssignRoleToUserAsync(
                request.UserId,
                request.RoleId,
                CurrentTenantId);

            if (!result.Success)
                return ErrorResponse(result.Message ?? "Failed to assign role.");

            await LogUserActivityAsync(
                "role.assign",
                $"Assigned role {request.RoleId} to user {request.UserId}");

            return SuccessResponse(result.User, result.Message ?? "Role assigned successfully");
        }

        /// <summary>
        /// Assign multiple roles – SuperAdmin only
        /// </summary>
        [HttpPost("assign-multiple-roles")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> AssignMultipleRoles([FromBody] AssignMultipleRolesRequest request)
        {
            if (!HasPermission("RoleAssignment.Write"))
                return ForbiddenResponse("You do not have permission to assign roles.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            var result = await _roleAssignmentService.AssignMultipleRolesToUserAsync(
                request.UserId,
                request.RoleIds,
                CurrentTenantId);

            if (!result.Success)
                return ErrorResponse(result.Message ?? "Failed to assign roles.");

            await LogUserActivityAsync(
                "role.assign.multiple",
                $"Assigned multiple roles to user {request.UserId}");

            return SuccessResponse(result.User, result.Message ?? "Roles assigned successfully");
        }

        /// <summary>
        /// Remove a role from a user – SuperAdmin only
        /// </summary>
        [HttpPost("remove-role")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> RemoveRole([FromBody] RemoveRoleRequest request)
        {
            if (!HasPermission("RoleAssignment.Write"))
                return ForbiddenResponse("You do not have permission to remove roles.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            var result = await _roleAssignmentService.RemoveRoleFromUserAsync(
                request.UserId,
                request.RoleId,
                CurrentTenantId);

            if (!result.Success)
                return ErrorResponse(result.Message ?? "Failed to remove role.");

            await LogUserActivityAsync(
                "role.remove",
                $"Removed role {request.RoleId} from user {request.UserId}");

            return SuccessResponse(result.User, result.Message ?? "Role removed successfully");
        }

        /// <summary>
        /// Update user roles – SuperAdmin only
        /// </summary>
        [HttpPut("update-user-roles")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateUserRoles([FromBody] UpdateUserRolesRequest request)
        {
            if (!HasPermission("RoleAssignment.Write"))
                return ForbiddenResponse("You do not have permission to update roles.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            var result = await _roleAssignmentService.UpdateUserRolesAsync(
                request.UserId,
                request.RoleIds,
                CurrentTenantId);

            if (!result.Success)
                return ErrorResponse(result.Message ?? "Failed to update user roles.");

            await LogUserActivityAsync(
                "role.update",
                $"Updated roles for user {request.UserId}");

            return SuccessResponse(result.User, result.Message ?? "User roles updated successfully");
        }

        /// <summary>
        /// Remove all roles from a user – SuperAdmin only
        /// </summary>
        [HttpDelete("user/{userId:guid}/roles")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> RemoveAllRoles(Guid userId)
        {
            if (!HasPermission("RoleAssignment.Write"))
                return ForbiddenResponse("You do not have permission to remove roles.");

            var result = await _roleAssignmentService.RemoveAllRolesFromUserAsync(
                userId,
                CurrentTenantId);

            if (!result.Success)
                return ErrorResponse(result.Message ?? "Failed to remove roles.");

            await LogUserActivityAsync(
                "role.remove.all",
                $"Removed all roles from user {userId}");

            return SuccessResponse(new { UserId = userId }, "All roles removed successfully");
        }

        #endregion

        #region Roles

        /// <summary>
        /// Get users by role – SuperAdmin only
        /// </summary>
        [HttpGet("role/{roleId:guid}/users")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetUsersByRole(
            Guid roleId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!HasPermission("RoleAssignment.Read"))
                return ForbiddenResponse("You do not have permission to view role users.");

            pageNumber = Math.Max(1, pageNumber);
            pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

            var users = await _roleAssignmentService.GetUsersByRoleAsync(
                roleId,
                CurrentTenantId,
                pageNumber,
                pageSize);

            return SuccessResponse(users, "Users retrieved successfully");
        }

        /// <summary>
        /// Get available roles – SuperAdmin only
        /// </summary>
        [HttpGet("available-roles")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetAvailableRoles()
        {
            if (!HasPermission("RoleAssignment.Read"))
                return ForbiddenResponse("You do not have permission to view roles.");

            var roles = await _roleAssignmentService.GetAvailableRolesAsync(CurrentTenantId);
            return SuccessResponse(roles, "Available roles retrieved successfully");
        }

        /// <summary>
        /// Check if user has role – SuperAdmin only
        /// </summary>
        [HttpGet("user/{userId:guid}/has-role/{roleId:guid}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UserHasRole(Guid userId, Guid roleId)
        {
            if (!HasPermission("RoleAssignment.Read"))
                return ForbiddenResponse("You do not have permission to view role assignments.");

            var hasRole = await _roleAssignmentService.UserHasRoleAsync(
                userId,
                roleId,
                CurrentTenantId);

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
