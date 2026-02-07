using Devken.CBC.SchoolManagement.Application.Common;
using Devken.CBC.SchoolManagement.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace Devken.CBC.SchoolManagement.Application.Services.UserManagement
{
    public interface IUserManagementService
    {
        #region User CRUD Operations

        /// <summary>
        /// Create a new user
        /// </summary>
        Task<Common.ServiceResult<UserDto>> CreateUserAsync(
            CreateUserRequest request,
            Guid schoolId,
            Guid createdBy);

        /// <summary>
        /// Get user by ID
        /// </summary>
        Task<Common.ServiceResult<UserDto>> GetUserByIdAsync(Guid userId);

        /// <summary>
        /// Get users with filtering and pagination
        /// </summary>
        Task<Common.ServiceResult<PaginatedUsersResponse>> GetUsersAsync(
            Guid? schoolId,
            int page,
            int pageSize,
            string? search,
            bool? isActive);

        /// <summary>
        /// Update user details (basic information)
        /// </summary>
        Task<Common.ServiceResult<UserDto>> UpdateUserAsync(
            Guid userId,
            UpdateUserRequest request,
            Guid updatedBy);

        /// <summary>
        /// Delete user
        /// </summary>
        Task<Common.ServiceResult<bool>> DeleteUserAsync(
            Guid userId,
            Guid deletedBy);

        #endregion

        #region User Status Management

        /// <summary>
        /// Activate user account
        /// </summary>
        Task<Common.ServiceResult<bool>> ActivateUserAsync(
            Guid userId,
            Guid activatedBy);

        /// <summary>
        /// Deactivate user account
        /// </summary>
        Task<Common.ServiceResult<bool>> DeactivateUserAsync(
            Guid userId,
            Guid deactivatedBy);

        #endregion

        #region Role Management

        /// <summary>
        /// Assign multiple roles to a user (adds to existing roles)
        /// </summary>
        Task<Common.ServiceResult<UserDto>> AssignRolesToUserAsync(
            Guid userId,
            List<string> roleIds,
            Guid assignedBy);

        /// <summary>
        /// Update user roles (replaces all existing roles)
        /// </summary>
        Task<Common.ServiceResult<UserDto>> UpdateUserRolesAsync(
            Guid userId,
            List<string> roleIds,
            Guid updatedBy);

        /// <summary>
        /// Remove a specific role from user
        /// </summary>
        Task<Common.ServiceResult<UserDto>> RemoveRoleFromUserAsync(
            Guid userId,
            string roleId,
            Guid removedBy);

        #endregion

        #region Password Management

        /// <summary>
        /// Reset user password (admin-initiated)
        /// </summary>
        Task<Common.ServiceResult<bool>> ResetPasswordAsync(
            Guid userId,
            Guid resetBy);

        /// <summary>
        /// Resend welcome email
        /// </summary>
        Task<Common.ServiceResult<bool>> ResendWelcomeEmailAsync(Guid userId);

        #endregion
    }
}