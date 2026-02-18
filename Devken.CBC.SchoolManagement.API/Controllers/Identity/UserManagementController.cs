using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Services.UserManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<UserManagementController> _logger;

        public UserManagementController(
            IUserManagementService userManagementService,
            IUserActivityService activityService,
            ILogger<UserManagementController> logger)
            : base(activityService, logger)
        {
            ArgumentNullException.ThrowIfNull(userManagementService);
            ArgumentNullException.ThrowIfNull(logger);

            _userManagementService = userManagementService;
            _logger = logger;
        }

        #region Create User

        /// <summary>
        /// Create a new user.
        /// SuperAdmin MUST specify SchoolId — they have no school of their own (TenantId = Guid.Empty).
        /// SchoolAdmin/Admin can only create users in their own school; any SchoolId they supply
        /// is ignored and their own TenantId is used instead.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            LogUserAuthorization("CreateUser");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            if (!IsSuperAdmin && !HasPermission("User.Write"))
            {
                _logger.LogWarning("User {UserId} attempted to create user without permission", CurrentUserId);
                return ForbiddenResponse("You do not have permission to create users.");
            }

            Guid targetSchoolId;

            if (IsSuperAdmin)
            {
                // SuperAdmin has TenantId = Guid.Empty so we MUST use the supplied SchoolId.
                if (request?.SchoolId is null || request.SchoolId == Guid.Empty)
                {
                    _logger.LogWarning(
                        "SuperAdmin {UserId} attempted to create user without SchoolId", CurrentUserId);
                    return ErrorResponse(
                        "SuperAdmin must specify a SchoolId when creating users.",
                        StatusCodes.Status400BadRequest);
                }

                targetSchoolId = request.SchoolId.Value;
            }
            else
            {
                // Regular users are always locked to their own school.
                // Silently override any SchoolId they pass — never trust client-supplied school context.
                targetSchoolId = GetCurrentUserSchoolId();

                if (request?.SchoolId.HasValue == true && request.SchoolId.Value != targetSchoolId)
                {
                    _logger.LogWarning(
                        "User {UserId} from school {UserSchoolId} attempted to create user in " +
                        "different school {TargetSchoolId}",
                        CurrentUserId, targetSchoolId, request.SchoolId.Value);
                    return ForbiddenResponse("You can only create users in your own school.");
                }
            }

            if (request?.RoleIds is null || request.RoleIds.Count == 0)
            {
                _logger.LogWarning("User {UserId} attempted to create user without roles", CurrentUserId);
                return ErrorResponse(
                    "At least one role must be assigned to the user.",
                    StatusCodes.Status400BadRequest);
            }

            _logger.LogInformation(
                "Creating user {Email} in school {SchoolId} by user {CreatedBy} with {RoleCount} role(s)",
                request.Email, targetSchoolId, CurrentUserId, request.RoleIds.Count);

            // Strip SchoolId from the forwarded request — it is passed separately as targetSchoolId.
            var createRequest = new CreateUserRequest
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                TemporaryPassword = request.TemporaryPassword,
                RequirePasswordChange = request.RequirePasswordChange,
                RoleIds = request.RoleIds
                // SchoolId deliberately omitted here — service receives targetSchoolId directly
            };

            var result = await _userManagementService.CreateUserAsync(
                createRequest, targetSchoolId, CurrentUserId);

            if (!result.Success)
            {
                _logger.LogError(
                    "Failed to create user {Email} in school {SchoolId}: {Error}",
                    request.Email, targetSchoolId, result.Error);
                return ErrorResponse(
                    result.Error ?? "User creation failed",
                    StatusCodes.Status400BadRequest);
            }

            _logger.LogInformation(
                "Successfully created user {UserId} ({Email}) in school {SchoolId}",
                result.Data?.Id, request.Email, targetSchoolId);

            await LogUserActivityAsync(
                "user.create",
                $"Created user {request.Email} in school {targetSchoolId}");

            return CreatedResponse(
                $"/api/user-management/{result.Data?.Id}",
                result.Data!,
                "User created successfully");
        }

        #endregion

        #region Get Users

        /// <summary>
        /// Get users.
        /// SuperAdmin can view all schools or filter by schoolId.
        /// School users always see only their own school.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] Guid? schoolId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null)
        {
            LogUserAuthorization("GetUsers");

            if (!IsSuperAdmin && !HasPermission("User.Read"))
            {
                _logger.LogWarning("User {UserId} attempted to view users without permission", CurrentUserId);
                return ForbiddenResponse("You do not have permission to view users.");
            }

            Guid? targetSchoolId = IsSuperAdmin
                ? schoolId                       // null → all schools
                : GetCurrentUserSchoolId();      // always own school

            if (!IsSuperAdmin && schoolId.HasValue && schoolId != targetSchoolId)
            {
                _logger.LogWarning(
                    "User {UserId} from school {UserSchoolId} attempted to view users from " +
                    "different school {TargetSchoolId}",
                    CurrentUserId, targetSchoolId, schoolId.Value);
                return ForbiddenResponse("You can only view users in your own school.");
            }

            _logger.LogInformation(
                "Retrieving users — SchoolId: {SchoolId}, Page: {Page}, PageSize: {PageSize}, Search: {Search}",
                targetSchoolId, page, pageSize, search);

            var result = await _userManagementService.GetUsersAsync(
                targetSchoolId, page, pageSize, search, isActive);

            if (!result.Success)
            {
                _logger.LogError("Failed to retrieve users: {Error}", result.Error);
                return ErrorResponse(
                    result.Error ?? "Failed to retrieve users",
                    StatusCodes.Status400BadRequest);
            }

            return SuccessResponse(result.Data, "Users retrieved successfully");
        }

        /// <summary>
        /// Get a single user by ID.
        /// </summary>
        [HttpGet("{userId:guid}")]
        public async Task<IActionResult> GetUser(Guid userId)
        {
            LogUserAuthorization($"GetUser:{userId}");

            var result = await _userManagementService.GetUserByIdAsync(userId);

            if (!result.Success || result.Data == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return NotFoundResponse("User not found");
            }

            if (!IsSuperAdmin)
            {
                if (!HasPermission("User.Read"))
                {
                    _logger.LogWarning(
                        "User {CurrentUserId} attempted to view user {UserId} without permission",
                        CurrentUserId, userId);
                    return ForbiddenResponse("You do not have permission to view users.");
                }

                if (result.Data.SchoolId != CurrentTenantId)
                {
                    _logger.LogWarning(
                        "User {CurrentUserId} from school {CurrentSchoolId} attempted to view user " +
                        "{UserId} from different school {UserSchoolId}",
                        CurrentUserId, CurrentTenantId, userId, result.Data.SchoolId);
                    return ForbiddenResponse("You do not have access to this user.");
                }
            }

            return SuccessResponse(result.Data, "User retrieved successfully");
        }

        #endregion

        #region Update User

        /// <summary>
        /// Update user details and roles.
        /// </summary>
        [HttpPut("{userId:guid}")]
        public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] UpdateUserRequest request)
        {
            LogUserAuthorization($"UpdateUser:{userId}");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
            {
                _logger.LogWarning("User {UserId} not found for update", userId);
                return NotFoundResponse("User not found");
            }

            if (!IsSuperAdmin)
            {
                if (!HasPermission("User.Write"))
                {
                    _logger.LogWarning(
                        "User {CurrentUserId} attempted to update user {UserId} without permission",
                        CurrentUserId, userId);
                    return ForbiddenResponse("You do not have permission to update users.");
                }

                if (userResult.Data.SchoolId != CurrentTenantId)
                {
                    _logger.LogWarning(
                        "User {CurrentUserId} from school {CurrentSchoolId} attempted to update user " +
                        "{UserId} from different school {UserSchoolId}",
                        CurrentUserId, CurrentTenantId, userId, userResult.Data.SchoolId);
                    return ForbiddenResponse("You can only update users in your own school.");
                }
            }

            if (userId == CurrentUserId && request.IsActive.HasValue && !request.IsActive.Value)
            {
                _logger.LogWarning("User {UserId} attempted to deactivate their own account", CurrentUserId);
                return ErrorResponse(
                    "You cannot deactivate your own account.",
                    StatusCodes.Status400BadRequest);
            }

            _logger.LogInformation("Updating user {UserId} by {UpdatedBy}", userId, CurrentUserId);

            var result = await _userManagementService.UpdateUserAsync(userId, request, CurrentUserId);

            if (!result.Success)
            {
                _logger.LogError("Failed to update user {UserId}: {Error}", userId, result.Error);
                return ErrorResponse(
                    result.Error ?? "User update failed",
                    StatusCodes.Status400BadRequest);
            }

            _logger.LogInformation("Successfully updated user {UserId}", userId);

            await LogUserActivityAsync("user.update", $"Updated user {userId}");

            return SuccessResponse(result.Data, "User updated successfully");
        }

        #endregion

        #region Roles Management

        /// <summary>
        /// Get available roles, optionally scoped to a specific school.
        /// Used by the frontend dialog to populate the roles dropdown.
        /// SuperAdmin uses this with ?schoolId=... to get roles for the school they are
        /// assigning the new user to; regular users call it without schoolId.
        /// </summary>
        [HttpGet("available-roles")]
        public async Task<IActionResult> GetAvailableRoles([FromQuery] Guid? schoolId = null)
        {
            LogUserAuthorization("GetAvailableRoles");

            Guid? targetSchoolId;

            if (IsSuperAdmin)
            {
                // SuperAdmin must pass schoolId so we return only that school's roles.
                // If omitted, fall through and return all roles (useful for SuperAdmin tooling).
                targetSchoolId = schoolId;
            }
            else
            {
                // Regular users always get their own school's roles regardless of query param.
                targetSchoolId = GetCurrentUserSchoolId();
            }

            _logger.LogInformation(
                "Retrieving available roles for school {SchoolId} by user {UserId}",
                targetSchoolId, CurrentUserId);

            var result = await _userManagementService.GetAvailableRolesAsync(targetSchoolId);

            if (!result.Success)
            {
                _logger.LogError("Failed to retrieve roles: {Error}", result.Error);
                return ErrorResponse(result.Error ?? "Failed to retrieve roles", StatusCodes.Status400BadRequest);
            }

            return SuccessResponse(result.Data, "Roles retrieved successfully");
        }

        /// <summary>
        /// Assign roles to a user (adds to existing roles).
        /// </summary>
        [HttpPost("{userId:guid}/roles")]
        public async Task<IActionResult> AssignRoles(Guid userId, [FromBody] AssignRolesRequest request)
        {
            LogUserAuthorization($"AssignRoles:{userId}");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            if (!IsSuperAdmin && !HasPermission("Role.Write"))
            {
                _logger.LogWarning(
                    "User {CurrentUserId} attempted to assign roles to user {UserId} without permission",
                    CurrentUserId, userId);
                return ForbiddenResponse("You do not have permission to assign roles.");
            }

            if (request.RoleIds == null || request.RoleIds.Count == 0)
                return ErrorResponse(
                    "At least one role must be provided.",
                    StatusCodes.Status400BadRequest);

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
                return NotFoundResponse("User not found");

            if (!IsSuperAdmin && userResult.Data.SchoolId != CurrentTenantId)
            {
                _logger.LogWarning(
                    "User {CurrentUserId} from school {CurrentSchoolId} attempted to assign roles to " +
                    "user {UserId} from different school {UserSchoolId}",
                    CurrentUserId, CurrentTenantId, userId, userResult.Data.SchoolId);
                return ForbiddenResponse("You can only manage roles for users in your own school.");
            }

            _logger.LogInformation(
                "Assigning {RoleCount} role(s) to user {UserId} by {AssignedBy}",
                request.RoleIds.Count, userId, CurrentUserId);

            var result = await _userManagementService.AssignRolesToUserAsync(
                userId,
                request.RoleIds.Select(r => r.ToString()).ToList(),
                CurrentUserId);

            if (!result.Success)
            {
                _logger.LogError("Failed to assign roles to user {UserId}: {Error}", userId, result.Error);
                return ErrorResponse(result.Error ?? "Role assignment failed", StatusCodes.Status400BadRequest);
            }

            await LogUserActivityAsync("user.assign-roles",
                $"Assigned {request.RoleIds.Count} role(s) to user {userId}");

            return SuccessResponse(result.Data, "Roles assigned successfully");
        }

        /// <summary>
        /// Remove a specific role from a user.
        /// </summary>
        [HttpDelete("{userId:guid}/roles/{roleId:guid}")]
        public async Task<IActionResult> RemoveRole(Guid userId, Guid roleId)
        {
            LogUserAuthorization($"RemoveRole:{userId}:{roleId}");

            if (!IsSuperAdmin && !HasPermission("Role.Write"))
                return ForbiddenResponse("You do not have permission to remove roles.");

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
                return NotFoundResponse("User not found");

            if (!IsSuperAdmin && userResult.Data.SchoolId != CurrentTenantId)
                return ForbiddenResponse("You can only manage roles for users in your own school.");

            if (userResult.Data.RoleNames != null && userResult.Data.RoleNames.Count <= 1)
                return ErrorResponse(
                    "Cannot remove the last role from a user. Users must have at least one role.",
                    StatusCodes.Status400BadRequest);

            if (userId == CurrentUserId)
                return ErrorResponse("You cannot modify your own roles.", StatusCodes.Status400BadRequest);

            _logger.LogInformation(
                "Removing role {RoleId} from user {UserId} by {RemovedBy}",
                roleId, userId, CurrentUserId);

            var result = await _userManagementService.RemoveRoleFromUserAsync(
                userId, roleId.ToString(), CurrentUserId);

            if (!result.Success)
                return ErrorResponse(result.Error ?? "Role removal failed", StatusCodes.Status400BadRequest);

            await LogUserActivityAsync("user.remove-role", $"Removed role {roleId} from user {userId}");

            return SuccessResponse<object?>(null, "Role removed successfully");
        }

        /// <summary>
        /// Replace all roles on a user.
        /// </summary>
        [HttpPut("{userId:guid}/roles")]
        public async Task<IActionResult> UpdateUserRoles(Guid userId, [FromBody] AssignRolesRequest request)
        {
            LogUserAuthorization($"UpdateUserRoles:{userId}");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            if (!IsSuperAdmin && !HasPermission("Role.Write"))
                return ForbiddenResponse("You do not have permission to update roles.");

            if (request.RoleIds == null || request.RoleIds.Count == 0)
                return ErrorResponse(
                    "At least one role must be assigned to the user.",
                    StatusCodes.Status400BadRequest);

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
                return NotFoundResponse("User not found");

            if (!IsSuperAdmin && userResult.Data.SchoolId != CurrentTenantId)
                return ForbiddenResponse("You can only manage roles for users in your own school.");

            if (userId == CurrentUserId)
                return ErrorResponse("You cannot modify your own roles.", StatusCodes.Status400BadRequest);

            _logger.LogInformation(
                "Updating roles for user {UserId} to {RoleCount} role(s) by {UpdatedBy}",
                userId, request.RoleIds.Count, CurrentUserId);

            var result = await _userManagementService.UpdateUserRolesAsync(
                userId,
                request.RoleIds.Select(r => r.ToString()).ToList(),
                CurrentUserId);

            if (!result.Success)
                return ErrorResponse(result.Error ?? "Role update failed", StatusCodes.Status400BadRequest);

            await LogUserActivityAsync("user.update-roles",
                $"Updated roles for user {userId} — assigned {request.RoleIds.Count} role(s)");

            return SuccessResponse(result.Data, "Roles updated successfully");
        }

        #endregion

        #region Password Management

        /// <summary>
        /// Admin-initiated password reset. Returns a temporary password.
        /// </summary>
        [HttpPost("{userId:guid}/reset-password")]
        public async Task<IActionResult> ResetPassword(Guid userId)
        {
            LogUserAuthorization($"ResetPassword:{userId}");

            if (!IsSuperAdmin && !HasPermission("User.Write"))
                return ForbiddenResponse("You do not have permission to reset passwords.");

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
                return NotFoundResponse("User not found");

            if (!IsSuperAdmin && userResult.Data.SchoolId != CurrentTenantId)
                return ForbiddenResponse("You can only reset passwords for users in your own school.");

            if (userId == CurrentUserId)
                return ErrorResponse(
                    "Please use the change password endpoint to update your own password.",
                    StatusCodes.Status400BadRequest);

            _logger.LogInformation("Resetting password for user {UserId} by {ResetBy}", userId, CurrentUserId);

            var result = await _userManagementService.ResetPasswordAsync(userId, CurrentUserId);

            if (!result.Success || result.Data == null)
                return ErrorResponse(result.Error ?? "Password reset failed", StatusCodes.Status400BadRequest);

            await LogUserActivityAsync("user.reset-password",
                $"Reset password for user {userId} ({result.Data.User?.Email})");

            var response = new
            {
                User = new
                {
                    result.Data.User.Id,
                    result.Data.User.Email,
                    result.Data.User.FirstName,
                    result.Data.User.LastName,
                    result.Data.User.RequirePasswordChange,
                    result.Data.User.IsActive
                },
                result.Data.TemporaryPassword,
                result.Data.Message,
                result.Data.ResetAt,
                ResetByUserId = result.Data.ResetBy
            };

            return SuccessResponse(response,
                "Password reset successfully. Please securely communicate the temporary password to the user.");
        }

        /// <summary>
        /// Resend welcome email to a user.
        /// </summary>
        [HttpPost("{userId:guid}/resend-welcome")]
        public async Task<IActionResult> ResendWelcomeEmail(Guid userId)
        {
            LogUserAuthorization($"ResendWelcome:{userId}");

            if (!IsSuperAdmin && !HasPermission("User.Write"))
                return ForbiddenResponse("You do not have permission to resend welcome emails.");

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
                return NotFoundResponse("User not found");

            if (!IsSuperAdmin && userResult.Data.SchoolId != CurrentTenantId)
                return ForbiddenResponse("You can only resend welcome emails for users in your own school.");

            _logger.LogInformation(
                "Resending welcome email to user {UserId} by {RequestedBy}", userId, CurrentUserId);

            var result = await _userManagementService.ResendWelcomeEmailAsync(userId);

            if (!result.Success)
                return ErrorResponse(result.Error ?? "Failed to resend welcome email", StatusCodes.Status400BadRequest);

            await LogUserActivityAsync("user.resend-welcome", $"Resent welcome email to user {userId}");

            return SuccessResponse<object?>(null, "Welcome email resent successfully");
        }

        #endregion

        #region Activate / Deactivate / Delete

        [HttpPost("{userId:guid}/activate")]
        public async Task<IActionResult> ActivateUser(Guid userId)
        {
            LogUserAuthorization($"ActivateUser:{userId}");

            if (!IsSuperAdmin && !HasPermission("User.Write"))
                return ForbiddenResponse("You do not have permission to activate users.");

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
                return NotFoundResponse("User not found");

            if (!IsSuperAdmin && userResult.Data.SchoolId != CurrentTenantId)
                return ForbiddenResponse("You can only activate users in your own school.");

            _logger.LogInformation("Activating user {UserId} by {ActivatedBy}", userId, CurrentUserId);

            var result = await _userManagementService.ActivateUserAsync(userId, CurrentUserId);

            if (!result.Success)
                return ErrorResponse(result.Error ?? "User activation failed", StatusCodes.Status400BadRequest);

            await LogUserActivityAsync("user.activate", $"Activated user {userId}");

            return SuccessResponse<object?>(null, "User activated successfully");
        }

        [HttpPost("{userId:guid}/deactivate")]
        public async Task<IActionResult> DeactivateUser(Guid userId)
        {
            LogUserAuthorization($"DeactivateUser:{userId}");

            if (userId == CurrentUserId)
                return ErrorResponse("You cannot deactivate your own account.", StatusCodes.Status400BadRequest);

            if (!IsSuperAdmin && !HasPermission("User.Write"))
                return ForbiddenResponse("You do not have permission to deactivate users.");

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
                return NotFoundResponse("User not found");

            if (!IsSuperAdmin && userResult.Data.SchoolId != CurrentTenantId)
                return ForbiddenResponse("You can only deactivate users in your own school.");

            _logger.LogInformation("Deactivating user {UserId} by {DeactivatedBy}", userId, CurrentUserId);

            var result = await _userManagementService.DeactivateUserAsync(userId, CurrentUserId);

            if (!result.Success)
                return ErrorResponse(result.Error ?? "User deactivation failed", StatusCodes.Status400BadRequest);

            await LogUserActivityAsync("user.deactivate", $"Deactivated user {userId}");

            return SuccessResponse<object?>(null, "User deactivated successfully");
        }

        [HttpDelete("{userId:guid}")]
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            LogUserAuthorization($"DeleteUser:{userId}");

            if (userId == CurrentUserId)
                return ErrorResponse("You cannot delete your own account.", StatusCodes.Status400BadRequest);

            if (!IsSuperAdmin && !HasPermission("User.Delete"))
                return ForbiddenResponse("You do not have permission to delete users.");

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
                return NotFoundResponse("User not found");

            if (!IsSuperAdmin && userResult.Data.SchoolId != CurrentTenantId)
                return ForbiddenResponse("You can only delete users in your own school.");

            _logger.LogInformation("Deleting user {UserId} by {DeletedBy}", userId, CurrentUserId);

            var result = await _userManagementService.DeleteUserAsync(userId, CurrentUserId);

            if (!result.Success)
                return ErrorResponse(result.Error ?? "User deletion failed", StatusCodes.Status400BadRequest);

            await LogUserActivityAsync("user.delete", $"Deleted user {userId}");

            return SuccessResponse<object?>(null, "User deleted successfully");
        }

        #endregion

        #region DTO Classes

        public class AssignRolesRequest
        {
            public List<Guid> RoleIds { get; set; } = [];
        }

        #endregion

        #region Helpers

        private static Dictionary<string, string[]> ToErrorDictionary(
            Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState) =>
            modelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? []);

        #endregion
    }
}