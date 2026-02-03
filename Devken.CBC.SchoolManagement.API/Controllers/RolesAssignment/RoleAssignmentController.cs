using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.RoleAssignment;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.IRolesAssignment;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Devken.CBC.SchoolManagement.API.Controllers.RolesAssignment
{
    [ApiController]
    [Route("api/role-assignments")]
    [Authorize]
    public class RoleAssignmentController : BaseApiController
    {
        private readonly IRoleAssignmentService _roleAssignmentService;

        public RoleAssignmentController(
            IRoleAssignmentService roleAssignmentService,
            IUserActivityService activityService)
            : base(activityService)
        {
            _roleAssignmentService = roleAssignmentService;
        }

        /// <summary>
        /// Assign a role to a user
        /// </summary>
        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
        {
            // Check permissions
            if (!HasPermission(PermissionKeys.UserWrite))
                return ForbiddenResponse("You do not have permission to assign roles");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            var result = await _roleAssignmentService.AssignRoleToUserAsync(
                request.UserId,
                request.RoleId,
                CurrentTenantId.Value);

            if (!result.Success)
                return ErrorResponse(result.Message ?? "Failed to assign role", StatusCodes.Status400BadRequest);

            await LogUserActivityAsync("AssignRole", $"User: {request.UserId}, Role: {request.RoleId}");

            return SuccessResponse(result.User, result.Message ?? "Role assigned successfully");
        }

        /// <summary>
        /// Assign multiple roles to a user
        /// </summary>
        [HttpPost("assign-multiple-roles")]
        public async Task<IActionResult> AssignMultipleRoles([FromBody] AssignMultipleRolesRequest request)
        {
            if (!HasPermission(PermissionKeys.UserWrite))
                return ForbiddenResponse("You do not have permission to assign roles");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            var result = await _roleAssignmentService.AssignMultipleRolesToUserAsync(
                request.UserId,
                request.RoleIds,
                CurrentTenantId.Value);

            if (!result.Success)
                return ErrorResponse(result.Message ?? "Failed to assign roles", StatusCodes.Status400BadRequest);

            await LogUserActivityAsync("AssignMultipleRoles", $"User: {request.UserId}, Roles: {request.RoleIds.Count}");

            return SuccessResponse(result.User, result.Message ?? "Roles assigned successfully");
        }

        /// <summary>
        /// Remove a role from a user
        /// </summary>
        [HttpPost("remove-role")]
        public async Task<IActionResult> RemoveRole([FromBody] RemoveRoleRequest request)
        {
            if (!HasPermission(PermissionKeys.UserWrite))
                return ForbiddenResponse("You do not have permission to remove roles");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            var result = await _roleAssignmentService.RemoveRoleFromUserAsync(
                request.UserId,
                request.RoleId,
                CurrentTenantId.Value);

            if (!result.Success)
                return ErrorResponse(result.Message ?? "Failed to remove role", StatusCodes.Status400BadRequest);

            await LogUserActivityAsync("RemoveRole", $"User: {request.UserId}, Role: {request.RoleId}");

            return SuccessResponse(result.User, result.Message ?? "Role removed successfully");
        }

        /// <summary>
        /// Update all roles for a user (replaces existing roles)
        /// </summary>
        [HttpPut("update-user-roles")]
        public async Task<IActionResult> UpdateUserRoles([FromBody] UpdateUserRolesRequest request)
        {
            if (!HasPermission(PermissionKeys.UserWrite))
                return ForbiddenResponse("You do not have permission to update user roles");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            var result = await _roleAssignmentService.UpdateUserRolesAsync(
                request.UserId,
                request.RoleIds,
                CurrentTenantId.Value);

            if (!result.Success)
                return ErrorResponse(result.Message ?? "Failed to update user roles", StatusCodes.Status400BadRequest);

            await LogUserActivityAsync("UpdateUserRoles", $"User: {request.UserId}, New role count: {request.RoleIds.Count}");

            return SuccessResponse(result.User, result.Message ?? "User roles updated successfully");
        }

        /// <summary>
        /// Get a user with all their roles
        /// </summary>
        [HttpGet("user/{userId}/roles")]
        public async Task<IActionResult> GetUserWithRoles(Guid userId)
        {
            if (!HasPermission(PermissionKeys.UserRead))
                return ForbiddenResponse("You do not have permission to view user roles");

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            var userWithRoles = await _roleAssignmentService.GetUserWithRolesAsync(userId, CurrentTenantId.Value);

            if (userWithRoles == null)
                return NotFoundResponse("User not found");

            return SuccessResponse(userWithRoles, "User roles retrieved successfully");
        }

        /// <summary>
        /// Get all users with a specific role
        /// </summary>
        [HttpGet("role/{roleId}/users")]
        public async Task<IActionResult> GetUsersByRole(
            Guid roleId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!HasPermission(PermissionKeys.UserRead))
                return ForbiddenResponse("You do not have permission to view users");

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var usersInRole = await _roleAssignmentService.GetUsersByRoleAsync(
                roleId,
                CurrentTenantId.Value,
                pageNumber,
                pageSize);

            return SuccessResponse(usersInRole, "Users retrieved successfully");
        }

        /// <summary>
        /// Get all available roles for the current tenant
        /// </summary>
        [HttpGet("available-roles")]
        public async Task<IActionResult> GetAvailableRoles()
        {
            if (!HasPermission(PermissionKeys.RoleRead))
                return ForbiddenResponse("You do not have permission to view roles");

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            var roles = await _roleAssignmentService.GetAvailableRolesAsync(CurrentTenantId.Value);

            return SuccessResponse(roles, "Available roles retrieved successfully");
        }

        /// <summary>
        /// Check if a user has a specific role
        /// </summary>
        [HttpGet("user/{userId}/has-role/{roleId}")]
        public async Task<IActionResult> UserHasRole(Guid userId, Guid roleId)
        {
            if (!HasPermission(PermissionKeys.UserRead))
                return ForbiddenResponse("You do not have permission to view user roles");

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            var hasRole = await _roleAssignmentService.UserHasRoleAsync(userId, roleId, CurrentTenantId.Value);

            return SuccessResponse(new { UserId = userId, RoleId = roleId, HasRole = hasRole },
                hasRole ? "User has the role" : "User does not have the role");
        }

        /// <summary>
        /// Remove all roles from a user
        /// </summary>
        [HttpDelete("user/{userId}/roles")]
        public async Task<IActionResult> RemoveAllRoles(Guid userId)
        {
            if (!HasPermission(PermissionKeys.UserWrite))
                return ForbiddenResponse("You do not have permission to modify user roles");

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            var result = await _roleAssignmentService.RemoveAllRolesFromUserAsync(userId, CurrentTenantId.Value);

            if (!result.Success)
                return ErrorResponse(result.Message ?? "Failed to remove roles", StatusCodes.Status400BadRequest);

            await LogUserActivityAsync("RemoveAllRoles", $"User: {userId}");

            return SuccessResponse(new { UserId = userId }, result.Message ?? "All roles removed successfully");
        }

        #region Helpers

        private static IDictionary<string, string[]> ToErrorDictionary(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState) =>
            modelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
            );

        #endregion
    }
}
