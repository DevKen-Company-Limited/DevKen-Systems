using Devken.CBC.SchoolManagement.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.UserManagment
{
    public interface IUserManagementService
    {
        /// <summary>
        /// Create a new user in a specific school
        /// </summary>
        Task<ServiceResult<CreateUserResponseDto>> CreateUserAsync(
            CreateUserRequest request,
            Guid schoolId,
            Guid createdByUserId);

        /// <summary>
        /// Get users with optional filtering and pagination
        /// </summary>
        Task<ServiceResult<UserListDto>> GetUsersAsync(
            Guid? schoolId,
            int page,
            int pageSize,
            string? search,
            bool? isActive);

        /// <summary>
        /// Get a specific user by ID
        /// </summary>
        Task<ServiceResult<UserManagementDto>> GetUserByIdAsync(Guid userId);

        /// <summary>
        /// Update user information
        /// </summary>
        Task<ServiceResult<UserManagementDto>> UpdateUserAsync(
            Guid userId,
            UpdateUserRequest request,
            Guid updatedByUserId);

        /// <summary>
        /// Assign multiple roles to a user
        /// </summary>
        Task<ServiceResult<UserManagementDto>> AssignRolesToUserAsync(
            Guid userId,
            List<Guid> roleIds,
            Guid assignedByUserId);

        /// <summary>
        /// Remove a specific role from a user
        /// </summary>
        Task<ServiceResult<UserManagementDto>> RemoveRoleFromUserAsync(
            Guid userId,
            Guid roleId,
            Guid removedByUserId);

        /// <summary>
        /// Activate a user account
        /// </summary>
        Task<ServiceResult<bool>> ActivateUserAsync(Guid userId, Guid activatedByUserId);

        /// <summary>
        /// Deactivate a user account
        /// </summary>
        Task<ServiceResult<bool>> DeactivateUserAsync(Guid userId, Guid deactivatedByUserId);

        /// <summary>
        /// Soft delete a user
        /// </summary>
        Task<ServiceResult<bool>> DeleteUserAsync(Guid userId, Guid deletedByUserId);

        /// <summary>
        /// Admin-initiated password reset
        /// </summary>
        Task<ServiceResult<ResetPasswordResponseDto>> ResetUserPasswordAsync(
            Guid userId,
            Guid resetByUserId);
    }
}
