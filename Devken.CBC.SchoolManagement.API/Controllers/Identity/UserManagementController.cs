using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Application.Services.UserManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Identity
{
    [ApiController]
    [Route("api/user-management")]
    [Authorize]
    public class UserManagementController : BaseApiController
    {
        private readonly IUserManagementService _userManagementService;

        public UserManagementController(
            IUserManagementService userManagementService)
            : base(null) // Remove activityService parameter from base if not needed
        {
            _userManagementService = userManagementService
                ?? throw new ArgumentNullException(nameof(userManagementService));
        }

        #region Create User

        /// <summary>
        /// Create a new user.
        /// SuperAdmin must specify SchoolId.
        /// SchoolAdmin/Admin can only create users in their own school.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            if (!IsSuperAdmin && !HasPermission("User.Create"))
                return ForbiddenResponse("You do not have permission to create users.");

            Guid targetSchoolId;

            if (IsSuperAdmin)
            {
                if (request is null || !request.SchoolId.HasValue)
                    return ErrorResponse(
                        "SuperAdmin must specify a SchoolId when creating users.",
                        StatusCodes.Status400BadRequest);

                targetSchoolId = request.SchoolId.Value;
            }
            else
            {
                targetSchoolId = GetCurrentUserSchoolId();

                if (request.SchoolId.HasValue && request.SchoolId.Value != targetSchoolId)
                    return ForbiddenResponse("You can only create users in your own school.");
            }

            // Validate that RoleIds are provided
            if (request.RoleIds == null || request.RoleIds.Count == 0)
                return ErrorResponse(
                    "At least one role must be assigned to the user.",
                    StatusCodes.Status400BadRequest);

            // Remove SchoolId from the request since it's now handled separately
            var createRequest = new CreateUserRequest
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                TemporaryPassword = request.TemporaryPassword,
                RequirePasswordChange = request.RequirePasswordChange,
                RoleIds = request.RoleIds
            };

            var result = await _userManagementService.CreateUserAsync(
                createRequest,
                targetSchoolId,
                CurrentUserId);

            if (!result.Success)
                return ErrorResponse(
                    result.Error ?? "User creation failed",
                    StatusCodes.Status400BadRequest);

            await LogUserActivityAsync(
                "user.create",
                $"Created user {request.Email} in school {targetSchoolId}");

            return CreatedResponse(
                $"/api/user-management/{result.Data?.Id}",
                result.Data,
                "User created successfully");
        }

        #endregion

        #region Get Users

        /// <summary>
        /// Get users.
        /// SuperAdmin can view all or filter by school.
        /// School users see only their school.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] Guid? schoolId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null)
        {
            if (!IsSuperAdmin && !HasPermission("User.Read"))
                return ForbiddenResponse("You do not have permission to view users.");

            Guid? targetSchoolId = IsSuperAdmin
                ? schoolId
                : GetCurrentUserSchoolId();

            if (!IsSuperAdmin && schoolId.HasValue && schoolId != targetSchoolId)
                return ForbiddenResponse("You can only view users in your own school.");

            var result = await _userManagementService.GetUsersAsync(
                targetSchoolId,
                page,
                pageSize,
                search,
                isActive);

            if (!result.Success)
                return ErrorResponse(
                    result.Error ?? "Failed to retrieve users",
                    StatusCodes.Status400BadRequest);

            return SuccessResponse(result.Data, "Users retrieved successfully");
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("{userId:guid}")]
        public async Task<IActionResult> GetUser(Guid userId)
        {
            var result = await _userManagementService.GetUserByIdAsync(userId);

            if (!result.Success || result.Data == null)
                return NotFoundResponse("User not found");

            if (!IsSuperAdmin)
            {
                if (!HasPermission("User.Read"))
                    return ForbiddenResponse("You do not have permission to view users.");

                if (result.Data.SchoolId != CurrentTenantId)
                    return ForbiddenResponse("You do not have access to this user.");
            }

            return SuccessResponse(result.Data, "User retrieved successfully");
        }

        #endregion

        #region Update User

        /// <summary>
        /// Update user details and roles
        /// </summary>
        [HttpPut("{userId:guid}")]
        public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
                return NotFoundResponse("User not found");

            if (!IsSuperAdmin)
            {
                if (!HasPermission("User.Update"))
                    return ForbiddenResponse("You do not have permission to update users.");

                if (userResult.Data.SchoolId != CurrentTenantId)
                    return ForbiddenResponse("You can only update users in your own school.");
            }

            // Prevent users from deactivating themselves
            if (userId == CurrentUserId && request.IsActive.HasValue && !request.IsActive.Value)
                return ErrorResponse("You cannot deactivate your own account.", StatusCodes.Status400BadRequest);

            // Update user basic information
            var result = await _userManagementService.UpdateUserAsync(
                userId,
                request,
                CurrentUserId);

            if (!result.Success)
                return ErrorResponse(
                    result.Error ?? "User update failed",
                    StatusCodes.Status400BadRequest);

            await LogUserActivityAsync(
                "user.update",
                $"Updated user {userId}");

            return SuccessResponse(result.Data, "User updated successfully");
        }

        #endregion

        #region Roles Management

        /// <summary>
        /// Assign roles to a user (adds to existing roles)
        /// </summary>
        [HttpPost("{userId:guid}/roles")]
        public async Task<IActionResult> AssignRoles(Guid userId, [FromBody] AssignRolesRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            if (!IsSuperAdmin && !HasPermission("User.AssignRoles"))
                return ForbiddenResponse("You do not have permission to assign roles.");

            // Validate that roles are provided
            if (request.RoleIds == null || request.RoleIds.Count == 0)
                return ErrorResponse(
                    "At least one role must be provided.",
                    StatusCodes.Status400BadRequest);

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
                return NotFoundResponse("User not found");

            if (!IsSuperAdmin && userResult.Data.SchoolId != CurrentTenantId)
                return ForbiddenResponse("You can only manage roles for users in your own school.");

            // Convert List<Guid> to List<string> for service call
            var roleIdsAsString = request.RoleIds.Select(r => r.ToString()).ToList();

            var result = await _userManagementService.AssignRolesToUserAsync(
                userId,
                roleIdsAsString,
                CurrentUserId);

            if (!result.Success)
                return ErrorResponse(
                    result.Error ?? "Role assignment failed",
                    StatusCodes.Status400BadRequest);

            await LogUserActivityAsync(
                "user.assign-roles",
                $"Assigned {request.RoleIds.Count} role(s) to user {userId}");

            return SuccessResponse(result.Data, "Roles assigned successfully");
        }

        /// <summary>
        /// Remove role from user
        /// </summary>
        [HttpDelete("{userId:guid}/roles/{roleId:guid}")]
        public async Task<IActionResult> RemoveRole(Guid userId, Guid roleId)
        {
            if (!IsSuperAdmin && !HasPermission("User.AssignRoles"))
                return ForbiddenResponse("You do not have permission to remove roles.");

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
                return NotFoundResponse("User not found");

            if (!IsSuperAdmin && userResult.Data.SchoolId != CurrentTenantId)
                return ForbiddenResponse("You can only manage roles for users in your own school.");

            // Prevent removing the last role from a user
            if (userResult.Data.RoleNames != null && userResult.Data.RoleNames.Count <= 1)
                return ErrorResponse(
                    "Cannot remove the last role from a user. Users must have at least one role.",
                    StatusCodes.Status400BadRequest);

            // Prevent users from modifying their own roles
            if (userId == CurrentUserId)
                return ErrorResponse(
                    "You cannot modify your own roles.",
                    StatusCodes.Status400BadRequest);

            var result = await _userManagementService.RemoveRoleFromUserAsync(
                userId,
                roleId.ToString(),
                CurrentUserId);

            if (!result.Success)
                return ErrorResponse(
                    result.Error ?? "Role removal failed",
                    StatusCodes.Status400BadRequest);

            await LogUserActivityAsync(
                "user.remove-role",
                $"Removed role {roleId} from user {userId}");

            return SuccessResponse<object?>(null, "Role removed successfully");
        }

        /// <summary>
        /// Update user roles (replace all existing roles)
        /// </summary>
        [HttpPut("{userId:guid}/roles")]
        public async Task<IActionResult> UpdateUserRoles(Guid userId, [FromBody] AssignRolesRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            if (!IsSuperAdmin && !HasPermission("User.AssignRoles"))
                return ForbiddenResponse("You do not have permission to update roles.");

            // Validate that roles are provided
            if (request.RoleIds == null || request.RoleIds.Count == 0)
                return ErrorResponse(
                    "At least one role must be assigned to the user.",
                    StatusCodes.Status400BadRequest);

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
                return NotFoundResponse("User not found");

            if (!IsSuperAdmin && userResult.Data.SchoolId != CurrentTenantId)
                return ForbiddenResponse("You can only manage roles for users in your own school.");

            // Prevent users from modifying their own roles
            if (userId == CurrentUserId)
                return ErrorResponse(
                    "You cannot modify your own roles.",
                    StatusCodes.Status400BadRequest);

            // Convert List<Guid> to List<string> for service call
            var roleIdsAsString = request.RoleIds.Select(r => r.ToString()).ToList();

            var result = await _userManagementService.UpdateUserRolesAsync(
                userId,
                roleIdsAsString,
                CurrentUserId);

            if (!result.Success)
                return ErrorResponse(
                    result.Error ?? "Role update failed",
                    StatusCodes.Status400BadRequest);

            await LogUserActivityAsync(
                "user.update-roles",
                $"Updated roles for user {userId} - assigned {request.RoleIds.Count} role(s)");

            return SuccessResponse(result.Data, "Roles updated successfully");
        }

        #endregion

        #region Password Management

        /// <summary>
        /// Reset user password (admin-initiated)
        /// </summary>
        [HttpPost("{userId:guid}/reset-password")]
        public async Task<IActionResult> ResetPassword(Guid userId)
        {
            if (!IsSuperAdmin && !HasPermission("User.ResetPassword"))
                return ForbiddenResponse("You do not have permission to reset passwords.");

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
                return NotFoundResponse("User not found");

            if (!IsSuperAdmin && userResult.Data.SchoolId != CurrentTenantId)
                return ForbiddenResponse("You can only reset passwords for users in your own school.");

            // Prevent users from resetting their own password via admin endpoint
            if (userId == CurrentUserId)
                return ErrorResponse(
                    "Please use the change password endpoint to update your own password.",
                    StatusCodes.Status400BadRequest);

            var result = await _userManagementService.ResetPasswordAsync(userId, CurrentUserId);

            if (!result.Success)
                return ErrorResponse(
                    result.Error ?? "Password reset failed",
                    StatusCodes.Status400BadRequest);

            await LogUserActivityAsync(
                "user.reset-password",
                $"Reset password for user {userId}");

            return SuccessResponse<object?>(null, "Password reset successfully. User will receive a temporary password.");
        }

        /// <summary>
        /// Resend welcome email to user
        /// </summary>
        [HttpPost("{userId:guid}/resend-welcome")]
        public async Task<IActionResult> ResendWelcomeEmail(Guid userId)
        {
            if (!IsSuperAdmin && !HasPermission("User.Manage"))
                return ForbiddenResponse("You do not have permission to resend welcome emails.");

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
                return NotFoundResponse("User not found");

            if (!IsSuperAdmin && userResult.Data.SchoolId != CurrentTenantId)
                return ForbiddenResponse("You can only resend welcome emails for users in your own school.");

            var result = await _userManagementService.ResendWelcomeEmailAsync(userId);

            if (!result.Success)
                return ErrorResponse(
                    result.Error ?? "Failed to resend welcome email",
                    StatusCodes.Status400BadRequest);

            await LogUserActivityAsync(
                "user.resend-welcome",
                $"Resent welcome email to user {userId}");

            return SuccessResponse<object?>(null, "Welcome email resent successfully");
        }

        #endregion

        #region Activate / Deactivate / Delete

        [HttpPost("{userId:guid}/activate")]
        public async Task<IActionResult> ActivateUser(Guid userId)
        {
            if (!IsSuperAdmin && !HasPermission("User.Activate"))
                return ForbiddenResponse("You do not have permission to activate users.");

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
                return NotFoundResponse("User not found");

            if (!IsSuperAdmin && userResult.Data.SchoolId != CurrentTenantId)
                return ForbiddenResponse("You can only activate users in your own school.");

            var result = await _userManagementService.ActivateUserAsync(userId, CurrentUserId);

            if (!result.Success)
                return ErrorResponse(
                    result.Error ?? "User activation failed",
                    StatusCodes.Status400BadRequest);

            await LogUserActivityAsync("user.activate", $"Activated user {userId}");

            return SuccessResponse<object?>(null, "User activated successfully");
        }

        [HttpPost("{userId:guid}/deactivate")]
        public async Task<IActionResult> DeactivateUser(Guid userId)
        {
            if (userId == CurrentUserId)
                return ErrorResponse("You cannot deactivate your own account.", StatusCodes.Status400BadRequest);

            if (!IsSuperAdmin && !HasPermission("User.Deactivate"))
                return ForbiddenResponse("You do not have permission to deactivate users.");

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
                return NotFoundResponse("User not found");

            if (!IsSuperAdmin && userResult.Data.SchoolId != CurrentTenantId)
                return ForbiddenResponse("You can only deactivate users in your own school.");

            var result = await _userManagementService.DeactivateUserAsync(userId, CurrentUserId);

            if (!result.Success)
                return ErrorResponse(
                    result.Error ?? "User deactivation failed",
                    StatusCodes.Status400BadRequest);

            await LogUserActivityAsync("user.deactivate", $"Deactivated user {userId}");

            return SuccessResponse<object?>(null, "User deactivated successfully");
        }

        [HttpDelete("{userId:guid}")]
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            if (userId == CurrentUserId)
                return ErrorResponse("You cannot delete your own account.", StatusCodes.Status400BadRequest);

            if (!IsSuperAdmin && !HasPermission("User.Delete"))
                return ForbiddenResponse("You do not have permission to delete users.");

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
                return NotFoundResponse("User not found");

            if (!IsSuperAdmin && userResult.Data.SchoolId != CurrentTenantId)
                return ForbiddenResponse("You can only delete users in your own school.");

            var result = await _userManagementService.DeleteUserAsync(userId, CurrentUserId);

            if (!result.Success)
                return ErrorResponse(
                    result.Error ?? "User deletion failed",
                    StatusCodes.Status400BadRequest);

            await LogUserActivityAsync("user.delete", $"Deleted user {userId}");

            return SuccessResponse<object?>(null, "User deleted successfully");
        }

        #endregion

        #region DTO Classes

        public class AssignRolesRequest
        {
            public List<Guid> RoleIds { get; set; } = new();
        }

        #endregion

        #region Helpers

        private static Dictionary<string, string[]> ToErrorDictionary(
            Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState)
        {
            return modelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
                    ?? Array.Empty<string>());
        }

        #endregion
    }
}