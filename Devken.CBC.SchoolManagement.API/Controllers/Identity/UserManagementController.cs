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
        /// SuperAdmin must specify SchoolId.
        /// SchoolAdmin/Admin can only create users in their own school.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            // Log user context for debugging
            LogUserAuthorization("CreateUser");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            // FIX: Changed from "User.Create" to "User.Write" to match JWT permissions
            if (!IsSuperAdmin && !HasPermission("User.Write"))
            {
                _logger.LogWarning("User {UserId} attempted to create user without permission", CurrentUserId);
                return ForbiddenResponse("You do not have permission to create users.");
            }

            Guid targetSchoolId;

            if (IsSuperAdmin)
            {
                if (request?.SchoolId is null)
                {
                    _logger.LogWarning("SuperAdmin {UserId} attempted to create user without SchoolId", CurrentUserId);
                    return ErrorResponse(
                        "SuperAdmin must specify a SchoolId when creating users.",
                        StatusCodes.Status400BadRequest);
                }

                targetSchoolId = request.SchoolId.Value;
            }
            else
            {
                targetSchoolId = GetCurrentUserSchoolId();

                if (request?.SchoolId.HasValue == true && request.SchoolId.Value != targetSchoolId)
                {
                    _logger.LogWarning(
                        "User {UserId} from school {UserSchoolId} attempted to create user in different school {TargetSchoolId}",
                        CurrentUserId, targetSchoolId, request.SchoolId.Value);
                    return ForbiddenResponse("You can only create users in your own school.");
                }
            }

            // Validate that RoleIds are provided
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
            LogUserAuthorization("GetUsers");

            if (!IsSuperAdmin && !HasPermission("User.Read"))
            {
                _logger.LogWarning("User {UserId} attempted to view users without permission", CurrentUserId);
                return ForbiddenResponse("You do not have permission to view users.");
            }

            Guid? targetSchoolId = IsSuperAdmin
                ? schoolId
                : GetCurrentUserSchoolId();

            if (!IsSuperAdmin && schoolId.HasValue && schoolId != targetSchoolId)
            {
                _logger.LogWarning(
                    "User {UserId} from school {UserSchoolId} attempted to view users from different school {TargetSchoolId}",
                    CurrentUserId, targetSchoolId, schoolId.Value);
                return ForbiddenResponse("You can only view users in your own school.");
            }

            _logger.LogInformation(
                "Retrieving users - SchoolId: {SchoolId}, Page: {Page}, PageSize: {PageSize}, Search: {Search}",
                targetSchoolId, page, pageSize, search);

            var result = await _userManagementService.GetUsersAsync(
                targetSchoolId,
                page,
                pageSize,
                search,
                isActive);

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
        /// Get user by ID
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
                        "User {CurrentUserId} from school {CurrentSchoolId} attempted to view user {UserId} from different school {UserSchoolId}",
                        CurrentUserId, CurrentTenantId, userId, result.Data.SchoolId);
                    return ForbiddenResponse("You do not have access to this user.");
                }
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
            LogUserAuthorization($"UpdateUser:{userId}");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
            {
                _logger.LogWarning("User {UserId} not found for update", userId);
                return NotFoundResponse("User not found");
            }

            // FIX: Changed from "User.Update" to "User.Write" to match JWT permissions
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
                        "User {CurrentUserId} from school {CurrentSchoolId} attempted to update user {UserId} from different school {UserSchoolId}",
                        CurrentUserId, CurrentTenantId, userId, userResult.Data.SchoolId);
                    return ForbiddenResponse("You can only update users in your own school.");
                }
            }

            // Prevent users from deactivating themselves
            if (userId == CurrentUserId && request.IsActive.HasValue && !request.IsActive.Value)
            {
                _logger.LogWarning("User {UserId} attempted to deactivate their own account", CurrentUserId);
                return ErrorResponse("You cannot deactivate your own account.", StatusCodes.Status400BadRequest);
            }

            _logger.LogInformation("Updating user {UserId} by {UpdatedBy}", userId, CurrentUserId);

            // Update user basic information
            var result = await _userManagementService.UpdateUserAsync(
                userId,
                request,
                CurrentUserId);

            if (!result.Success)
            {
                _logger.LogError("Failed to update user {UserId}: {Error}", userId, result.Error);
                return ErrorResponse(
                    result.Error ?? "User update failed",
                    StatusCodes.Status400BadRequest);
            }

            _logger.LogInformation("Successfully updated user {UserId}", userId);

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
            LogUserAuthorization($"AssignRoles:{userId}");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            // FIX: Changed from "User.AssignRoles" to "Role.Write" to match JWT permissions
            if (!IsSuperAdmin && !HasPermission("Role.Write"))
            {
                _logger.LogWarning(
                    "User {CurrentUserId} attempted to assign roles to user {UserId} without permission",
                    CurrentUserId, userId);
                return ForbiddenResponse("You do not have permission to assign roles.");
            }

            // Validate that roles are provided
            if (request.RoleIds == null || request.RoleIds.Count == 0)
            {
                _logger.LogWarning("Attempt to assign roles to user {UserId} without providing any roles", userId);
                return ErrorResponse(
                    "At least one role must be provided.",
                    StatusCodes.Status400BadRequest);
            }

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
            {
                _logger.LogWarning("User {UserId} not found for role assignment", userId);
                return NotFoundResponse("User not found");
            }

            if (!IsSuperAdmin && userResult.Data.SchoolId != CurrentTenantId)
            {
                _logger.LogWarning(
                    "User {CurrentUserId} from school {CurrentSchoolId} attempted to assign roles to user {UserId} from different school {UserSchoolId}",
                    CurrentUserId, CurrentTenantId, userId, userResult.Data.SchoolId);
                return ForbiddenResponse("You can only manage roles for users in your own school.");
            }

            _logger.LogInformation(
                "Assigning {RoleCount} role(s) to user {UserId} by {AssignedBy}",
                request.RoleIds.Count, userId, CurrentUserId);

            // Convert List<Guid> to List<string> for service call
            var roleIdsAsString = request.RoleIds.Select(r => r.ToString()).ToList();

            var result = await _userManagementService.AssignRolesToUserAsync(
                userId,
                roleIdsAsString,
                CurrentUserId);

            if (!result.Success)
            {
                _logger.LogError("Failed to assign roles to user {UserId}: {Error}", userId, result.Error);
                return ErrorResponse(
                    result.Error ?? "Role assignment failed",
                    StatusCodes.Status400BadRequest);
            }

            _logger.LogInformation("Successfully assigned roles to user {UserId}", userId);

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
            LogUserAuthorization($"RemoveRole:{userId}:{roleId}");

            // FIX: Changed from "User.AssignRoles" to "Role.Write" to match JWT permissions
            if (!IsSuperAdmin && !HasPermission("Role.Write"))
            {
                _logger.LogWarning(
                    "User {CurrentUserId} attempted to remove role {RoleId} from user {UserId} without permission",
                    CurrentUserId, roleId, userId);
                return ForbiddenResponse("You do not have permission to remove roles.");
            }

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
            {
                _logger.LogWarning("User {UserId} not found for role removal", userId);
                return NotFoundResponse("User not found");
            }

            if (!IsSuperAdmin && userResult.Data.SchoolId != CurrentTenantId)
            {
                _logger.LogWarning(
                    "User {CurrentUserId} from school {CurrentSchoolId} attempted to remove role from user {UserId} from different school {UserSchoolId}",
                    CurrentUserId, CurrentTenantId, userId, userResult.Data.SchoolId);
                return ForbiddenResponse("You can only manage roles for users in your own school.");
            }

            // Prevent removing the last role from a user
            if (userResult.Data.RoleNames != null && userResult.Data.RoleNames.Count <= 1)
            {
                _logger.LogWarning(
                    "Attempt to remove last role {RoleId} from user {UserId}",
                    roleId, userId);
                return ErrorResponse(
                    "Cannot remove the last role from a user. Users must have at least one role.",
                    StatusCodes.Status400BadRequest);
            }

            // Prevent users from modifying their own roles
            if (userId == CurrentUserId)
            {
                _logger.LogWarning("User {UserId} attempted to modify their own roles", CurrentUserId);
                return ErrorResponse(
                    "You cannot modify your own roles.",
                    StatusCodes.Status400BadRequest);
            }

            _logger.LogInformation(
                "Removing role {RoleId} from user {UserId} by {RemovedBy}",
                roleId, userId, CurrentUserId);

            var result = await _userManagementService.RemoveRoleFromUserAsync(
                userId,
                roleId.ToString(),
                CurrentUserId);

            if (!result.Success)
            {
                _logger.LogError(
                    "Failed to remove role {RoleId} from user {UserId}: {Error}",
                    roleId, userId, result.Error);
                return ErrorResponse(
                    result.Error ?? "Role removal failed",
                    StatusCodes.Status400BadRequest);
            }

            _logger.LogInformation("Successfully removed role {RoleId} from user {UserId}", roleId, userId);

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
            LogUserAuthorization($"UpdateUserRoles:{userId}");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            // FIX: Changed from "User.AssignRoles" to "Role.Write" to match JWT permissions
            if (!IsSuperAdmin && !HasPermission("Role.Write"))
            {
                _logger.LogWarning(
                    "User {CurrentUserId} attempted to update roles for user {UserId} without permission",
                    CurrentUserId, userId);
                return ForbiddenResponse("You do not have permission to update roles.");
            }

            // Validate that roles are provided
            if (request.RoleIds == null || request.RoleIds.Count == 0)
            {
                _logger.LogWarning("Attempt to update user {UserId} roles without providing any roles", userId);
                return ErrorResponse(
                    "At least one role must be assigned to the user.",
                    StatusCodes.Status400BadRequest);
            }

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
            {
                _logger.LogWarning("User {UserId} not found for role update", userId);
                return NotFoundResponse("User not found");
            }

            if (!IsSuperAdmin && userResult.Data.SchoolId != CurrentTenantId)
            {
                _logger.LogWarning(
                    "User {CurrentUserId} from school {CurrentSchoolId} attempted to update roles for user {UserId} from different school {UserSchoolId}",
                    CurrentUserId, CurrentTenantId, userId, userResult.Data.SchoolId);
                return ForbiddenResponse("You can only manage roles for users in your own school.");
            }

            // Prevent users from modifying their own roles
            if (userId == CurrentUserId)
            {
                _logger.LogWarning("User {UserId} attempted to modify their own roles", CurrentUserId);
                return ErrorResponse(
                    "You cannot modify your own roles.",
                    StatusCodes.Status400BadRequest);
            }

            _logger.LogInformation(
                "Updating roles for user {UserId} to {RoleCount} role(s) by {UpdatedBy}",
                userId, request.RoleIds.Count, CurrentUserId);

            // Convert List<Guid> to List<string> for service call
            var roleIdsAsString = request.RoleIds.Select(r => r.ToString()).ToList();

            var result = await _userManagementService.UpdateUserRolesAsync(
                userId,
                roleIdsAsString,
                CurrentUserId);

            if (!result.Success)
            {
                _logger.LogError("Failed to update roles for user {UserId}: {Error}", userId, result.Error);
                return ErrorResponse(
                    result.Error ?? "Role update failed",
                    StatusCodes.Status400BadRequest);
            }

            _logger.LogInformation("Successfully updated roles for user {UserId}", userId);

            await LogUserActivityAsync(
                "user.update-roles",
                $"Updated roles for user {userId} - assigned {request.RoleIds.Count} role(s)");

            return SuccessResponse(result.Data, "Roles updated successfully");
        }

        #endregion

        #region Password Management

        /// <summary>
        /// Reset user password (admin-initiated)
        /// Returns the generated temporary password that should be securely communicated to the user
        /// </summary>
        [HttpPost("{userId:guid}/reset-password")]
        public async Task<IActionResult> ResetPassword(Guid userId)
        {
            LogUserAuthorization($"ResetPassword:{userId}");

            // FIX: Changed from "User.ResetPassword" to "User.Write" to match JWT permissions
            if (!IsSuperAdmin && !HasPermission("User.Write"))
            {
                _logger.LogWarning(
                    "User {CurrentUserId} attempted to reset password for user {UserId} without permission",
                    CurrentUserId, userId);
                return ForbiddenResponse("You do not have permission to reset passwords.");
            }

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
            {
                _logger.LogWarning("User {UserId} not found for password reset", userId);
                return NotFoundResponse("User not found");
            }

            if (!IsSuperAdmin && userResult.Data.SchoolId != CurrentTenantId)
            {
                _logger.LogWarning(
                    "User {CurrentUserId} from school {CurrentSchoolId} attempted to reset password for user {UserId} from different school {UserSchoolId}",
                    CurrentUserId, CurrentTenantId, userId, userResult.Data.SchoolId);
                return ForbiddenResponse("You can only reset passwords for users in your own school.");
            }

            // Prevent users from resetting their own password via admin endpoint
            if (userId == CurrentUserId)
            {
                _logger.LogWarning("User {UserId} attempted to reset their own password via admin endpoint", CurrentUserId);
                return ErrorResponse(
                    "Please use the change password endpoint to update your own password.",
                    StatusCodes.Status400BadRequest);
            }

            _logger.LogInformation("Resetting password for user {UserId} by {ResetBy}", userId, CurrentUserId);

            var result = await _userManagementService.ResetPasswordAsync(userId, CurrentUserId);

            if (!result.Success || result.Data == null)
            {
                _logger.LogError("Failed to reset password for user {UserId}: {Error}", userId, result.Error);
                return ErrorResponse(
                    result.Error ?? "Password reset failed",
                    StatusCodes.Status400BadRequest);
            }

            _logger.LogInformation(
                "Successfully reset password for user {UserId}. Temporary password: {TempPasswordLength} characters",
                userId, result.Data.TemporaryPassword?.Length ?? 0);

            await LogUserActivityAsync(
                "user.reset-password",
                $"Reset password for user {userId} ({result.Data.User?.Email})");

            // Return the complete password reset result including temporary password
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

            return SuccessResponse(response, "Password reset successfully. Please securely communicate the temporary password to the user.");
        }

        /// <summary>
        /// Resend welcome email to user
        /// </summary>
        [HttpPost("{userId:guid}/resend-welcome")]
        public async Task<IActionResult> ResendWelcomeEmail(Guid userId)
        {
            LogUserAuthorization($"ResendWelcome:{userId}");

            // FIX: Changed from "User.Manage" to "User.Write" to match JWT permissions
            if (!IsSuperAdmin && !HasPermission("User.Write"))
            {
                _logger.LogWarning(
                    "User {CurrentUserId} attempted to resend welcome email for user {UserId} without permission",
                    CurrentUserId, userId);
                return ForbiddenResponse("You do not have permission to resend welcome emails.");
            }

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
            {
                _logger.LogWarning("User {UserId} not found for resend welcome email", userId);
                return NotFoundResponse("User not found");
            }

            if (!IsSuperAdmin && userResult.Data.SchoolId != CurrentTenantId)
            {
                _logger.LogWarning(
                    "User {CurrentUserId} from school {CurrentSchoolId} attempted to resend welcome email for user {UserId} from different school {UserSchoolId}",
                    CurrentUserId, CurrentTenantId, userId, userResult.Data.SchoolId);
                return ForbiddenResponse("You can only resend welcome emails for users in your own school.");
            }

            _logger.LogInformation("Resending welcome email to user {UserId} by {RequestedBy}", userId, CurrentUserId);

            var result = await _userManagementService.ResendWelcomeEmailAsync(userId);

            if (!result.Success)
            {
                _logger.LogError("Failed to resend welcome email to user {UserId}: {Error}", userId, result.Error);
                return ErrorResponse(
                    result.Error ?? "Failed to resend welcome email",
                    StatusCodes.Status400BadRequest);
            }

            _logger.LogInformation("Successfully resent welcome email to user {UserId}", userId);

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
            LogUserAuthorization($"ActivateUser:{userId}");

            // FIX: Changed from "User.Activate" to "User.Write" to match JWT permissions
            if (!IsSuperAdmin && !HasPermission("User.Write"))
            {
                _logger.LogWarning(
                    "User {CurrentUserId} attempted to activate user {UserId} without permission",
                    CurrentUserId, userId);
                return ForbiddenResponse("You do not have permission to activate users.");
            }

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
            {
                _logger.LogWarning("User {UserId} not found for activation", userId);
                return NotFoundResponse("User not found");
            }

            if (!IsSuperAdmin && userResult.Data.SchoolId != CurrentTenantId)
            {
                _logger.LogWarning(
                    "User {CurrentUserId} from school {CurrentSchoolId} attempted to activate user {UserId} from different school {UserSchoolId}",
                    CurrentUserId, CurrentTenantId, userId, userResult.Data.SchoolId);
                return ForbiddenResponse("You can only activate users in your own school.");
            }

            _logger.LogInformation("Activating user {UserId} by {ActivatedBy}", userId, CurrentUserId);

            var result = await _userManagementService.ActivateUserAsync(userId, CurrentUserId);

            if (!result.Success)
            {
                _logger.LogError("Failed to activate user {UserId}: {Error}", userId, result.Error);
                return ErrorResponse(
                    result.Error ?? "User activation failed",
                    StatusCodes.Status400BadRequest);
            }

            _logger.LogInformation("Successfully activated user {UserId}", userId);

            await LogUserActivityAsync("user.activate", $"Activated user {userId}");

            return SuccessResponse<object?>(null, "User activated successfully");
        }

        [HttpPost("{userId:guid}/deactivate")]
        public async Task<IActionResult> DeactivateUser(Guid userId)
        {
            LogUserAuthorization($"DeactivateUser:{userId}");

            if (userId == CurrentUserId)
            {
                _logger.LogWarning("User {UserId} attempted to deactivate their own account", CurrentUserId);
                return ErrorResponse("You cannot deactivate your own account.", StatusCodes.Status400BadRequest);
            }

            // FIX: Changed from "User.Deactivate" to "User.Write" to match JWT permissions
            if (!IsSuperAdmin && !HasPermission("User.Write"))
            {
                _logger.LogWarning(
                    "User {CurrentUserId} attempted to deactivate user {UserId} without permission",
                    CurrentUserId, userId);
                return ForbiddenResponse("You do not have permission to deactivate users.");
            }

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
            {
                _logger.LogWarning("User {UserId} not found for deactivation", userId);
                return NotFoundResponse("User not found");
            }

            if (!IsSuperAdmin && userResult.Data.SchoolId != CurrentTenantId)
            {
                _logger.LogWarning(
                    "User {CurrentUserId} from school {CurrentSchoolId} attempted to deactivate user {UserId} from different school {UserSchoolId}",
                    CurrentUserId, CurrentTenantId, userId, userResult.Data.SchoolId);
                return ForbiddenResponse("You can only deactivate users in your own school.");
            }

            _logger.LogInformation("Deactivating user {UserId} by {DeactivatedBy}", userId, CurrentUserId);

            var result = await _userManagementService.DeactivateUserAsync(userId, CurrentUserId);

            if (!result.Success)
            {
                _logger.LogError("Failed to deactivate user {UserId}: {Error}", userId, result.Error);
                return ErrorResponse(
                    result.Error ?? "User deactivation failed",
                    StatusCodes.Status400BadRequest);
            }

            _logger.LogInformation("Successfully deactivated user {UserId}", userId);

            await LogUserActivityAsync("user.deactivate", $"Deactivated user {userId}");

            return SuccessResponse<object?>(null, "User deactivated successfully");
        }

        [HttpDelete("{userId:guid}")]
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            LogUserAuthorization($"DeleteUser:{userId}");

            if (userId == CurrentUserId)
            {
                _logger.LogWarning("User {UserId} attempted to delete their own account", CurrentUserId);
                return ErrorResponse("You cannot delete your own account.", StatusCodes.Status400BadRequest);
            }

            if (!IsSuperAdmin && !HasPermission("User.Delete"))
            {
                _logger.LogWarning(
                    "User {CurrentUserId} attempted to delete user {UserId} without permission",
                    CurrentUserId, userId);
                return ForbiddenResponse("You do not have permission to delete users.");
            }

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
            {
                _logger.LogWarning("User {UserId} not found for deletion", userId);
                return NotFoundResponse("User not found");
            }

            if (!IsSuperAdmin && userResult.Data.SchoolId != CurrentTenantId)
            {
                _logger.LogWarning(
                    "User {CurrentUserId} from school {CurrentSchoolId} attempted to delete user {UserId} from different school {UserSchoolId}",
                    CurrentUserId, CurrentTenantId, userId, userResult.Data.SchoolId);
                return ForbiddenResponse("You can only delete users in your own school.");
            }

            _logger.LogInformation("Deleting user {UserId} by {DeletedBy}", userId, CurrentUserId);

            var result = await _userManagementService.DeleteUserAsync(userId, CurrentUserId);

            if (!result.Success)
            {
                _logger.LogError("Failed to delete user {UserId}: {Error}", userId, result.Error);
                return ErrorResponse(
                    result.Error ?? "User deletion failed",
                    StatusCodes.Status400BadRequest);
            }

            _logger.LogInformation("Successfully deleted user {UserId}", userId);

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
            Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState)
        {
            return modelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? []);
        }

        #endregion
    }
}