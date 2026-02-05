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
            IUserActivityService activityService)
            : base(activityService)
        {
            _roleAssignmentService = roleAssignmentService;
        }

        #region Users

        [HttpGet("all-users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

            var users = await _roleAssignmentService.GetAllUsersWithRolesAsync(
                CurrentTenantId,
                pageNumber,
                pageSize);

            return SuccessResponse(users, "Users retrieved successfully");
        }

        [HttpGet("search-users")]
        public async Task<IActionResult> SearchUsers([FromQuery] string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
                return ValidationErrorResponse(new Dictionary<string, string[]>
                {
                    { "searchTerm", new[] { "Search term must be at least 2 characters" } }
                });

            var users = await _roleAssignmentService.SearchUsersAsync(searchTerm, CurrentTenantId);
            return SuccessResponse(users, $"Found {users.Count} user(s)");
        }

        [HttpGet("user/{userId}/roles")]
        public async Task<IActionResult> GetUserWithRoles(Guid userId)
        {
            var user = await _roleAssignmentService.GetUserWithRolesAsync(userId, CurrentTenantId);
            if (user == null)
                return NotFoundResponse("User not found");

            return SuccessResponse(user, "User roles retrieved successfully");
        }

        #endregion

        #region Role Assignments

        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            var result = await _roleAssignmentService.AssignRoleToUserAsync(request.UserId, request.RoleId, CurrentTenantId);
            if (!result.Success)
                return ErrorResponse(result.Message ?? "Failed to assign role");

            await LogUserActivityAsync("AssignRole", $"User: {request.UserId}, Role: {request.RoleId}");
            return SuccessResponse(result.User, result.Message ?? "Role assigned successfully");
        }

        [HttpPost("assign-multiple-roles")]
        public async Task<IActionResult> AssignMultipleRoles([FromBody] AssignMultipleRolesRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            var result = await _roleAssignmentService.AssignMultipleRolesToUserAsync(request.UserId, request.RoleIds, CurrentTenantId);
            if (!result.Success)
                return ErrorResponse(result.Message ?? "Failed to assign roles");

            await LogUserActivityAsync("AssignMultipleRoles", $"User: {request.UserId}, Roles: {request.RoleIds.Count}");
            return SuccessResponse(result.User, result.Message ?? "Roles assigned successfully");
        }

        [HttpPost("remove-role")]
        public async Task<IActionResult> RemoveRole([FromBody] RemoveRoleRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            var result = await _roleAssignmentService.RemoveRoleFromUserAsync(request.UserId, request.RoleId, CurrentTenantId);
            if (!result.Success)
                return ErrorResponse(result.Message ?? "Failed to remove role");

            await LogUserActivityAsync("RemoveRole", $"User: {request.UserId}, Role: {request.RoleId}");
            return SuccessResponse(result.User, result.Message ?? "Role removed successfully");
        }

        [HttpPut("update-user-roles")]
        public async Task<IActionResult> UpdateUserRoles([FromBody] UpdateUserRolesRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            var result = await _roleAssignmentService.UpdateUserRolesAsync(request.UserId, request.RoleIds, CurrentTenantId);
            if (!result.Success)
                return ErrorResponse(result.Message ?? "Failed to update user roles");

            await LogUserActivityAsync("UpdateUserRoles", $"User: {request.UserId}, New roles: {request.RoleIds.Count}");
            return SuccessResponse(result.User, result.Message ?? "User roles updated successfully");
        }

        [HttpDelete("user/{userId}/roles")]
        public async Task<IActionResult> RemoveAllRoles(Guid userId)
        {
            var result = await _roleAssignmentService.RemoveAllRolesFromUserAsync(userId, CurrentTenantId);
            if (!result.Success)
                return ErrorResponse(result.Message ?? "Failed to remove roles");

            await LogUserActivityAsync("RemoveAllRoles", $"User: {userId}");
            return SuccessResponse(new { UserId = userId }, result.Message ?? "All roles removed successfully");
        }

        #endregion

        #region Roles

        [HttpGet("role/{roleId}/users")]
        public async Task<IActionResult> GetUsersByRole(Guid roleId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

            var users = await _roleAssignmentService.GetUsersByRoleAsync(roleId, CurrentTenantId, pageNumber, pageSize);
            return SuccessResponse(users, "Users retrieved successfully");
        }

        [HttpGet("available-roles")]
        public async Task<IActionResult> GetAvailableRoles()
        {
            var roles = await _roleAssignmentService.GetAvailableRolesAsync(CurrentTenantId);
            return SuccessResponse(roles, "Available roles retrieved successfully");
        }

        [HttpGet("user/{userId}/has-role/{roleId}")]
        public async Task<IActionResult> UserHasRole(Guid userId, Guid roleId)
        {
            var hasRole = await _roleAssignmentService.UserHasRoleAsync(userId, roleId, CurrentTenantId);
            return SuccessResponse(new { UserId = userId, RoleId = roleId, HasRole = hasRole },
                                   hasRole ? "User has the role" : "User does not have the role");
        }

        #endregion

        #region Helpers

        private static IDictionary<string, string[]> ToErrorDictionary(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState) =>
            modelState.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>());

        #endregion
    }
}
