using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Application.DTOs.RoleAssignment;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.UserManagment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
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
            IUserManagementService userManagementService,
            IUserActivityService activityService)
            : base(activityService)
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
                if (!request.SchoolId.HasValue)
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

            var result = await _userManagementService.CreateUserAsync(
                request,
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

                if (result.Data.TenantId != CurrentTenantId)
                    return ForbiddenResponse("You do not have access to this user.");
            }

            return SuccessResponse(result.Data, "User retrieved successfully");
        }

        #endregion

        #region Update User

        /// <summary>
        /// Update user details
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

                if (userResult.Data.TenantId != CurrentTenantId)
                    return ForbiddenResponse("You can only update users in your own school.");
            }

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
        /// Assign roles to a user
        /// </summary>
        [HttpPost("{userId:guid}/roles")]
        public async Task<IActionResult> AssignRoles(Guid userId, [FromBody] AssignRolesRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            if (!IsSuperAdmin && !HasPermission("User.AssignRoles"))
                return ForbiddenResponse("You do not have permission to assign roles.");

            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
                return NotFoundResponse("User not found");

            if (!IsSuperAdmin && userResult.Data.TenantId != CurrentTenantId)
                return ForbiddenResponse("You can only manage roles for users in your own school.");

            var result = await _userManagementService.AssignRolesToUserAsync(
                userId,
                request.RoleIds,
                CurrentUserId);

            if (!result.Success)
                return ErrorResponse(
                    result.Error ?? "Role assignment failed",
                    StatusCodes.Status400BadRequest);

            await LogUserActivityAsync(
                "user.assign-roles",
                $"Assigned roles to user {userId}");

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

            if (!IsSuperAdmin && userResult.Data.TenantId != CurrentTenantId)
                return ForbiddenResponse("You can only manage roles for users in your own school.");

            var result = await _userManagementService.RemoveRoleFromUserAsync(
                userId,
                roleId,
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

        #endregion

        #region Activate / Deactivate / Delete

        [HttpPost("{userId:guid}/activate")]
        public async Task<IActionResult> ActivateUser(Guid userId)
        {
            if (!IsSuperAdmin && !HasPermission("User.Activate"))
                return ForbiddenResponse("You do not have permission to activate users.");

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

            var result = await _userManagementService.DeleteUserAsync(userId, CurrentUserId);

            if (!result.Success)
                return ErrorResponse(
                    result.Error ?? "User deletion failed",
                    StatusCodes.Status400BadRequest);

            await LogUserActivityAsync("user.delete", $"Deleted user {userId}");

            return SuccessResponse<object?>(null, "User deleted successfully");
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
