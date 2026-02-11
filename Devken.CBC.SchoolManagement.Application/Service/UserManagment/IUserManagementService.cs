using Devken.CBC.SchoolManagement.Application.Common;
using Devken.CBC.SchoolManagement.Application.Dtos;

namespace Devken.CBC.SchoolManagement.Application.Services.UserManagement
{
    public interface IUserManagementService
    {
        Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserRequest request, Guid schoolId, Guid createdBy);
        Task<ServiceResult<PaginatedUsersResponse>> GetUsersAsync(Guid? schoolId, int page, int pageSize, string? search, bool? isActive);
        Task<ServiceResult<UserDto>> GetUserByIdAsync(Guid userId);
        Task<ServiceResult<UserDto>> UpdateUserAsync(Guid userId, UpdateUserRequest request, Guid updatedBy);
        Task<ServiceResult<UserDto>> AssignRolesToUserAsync(Guid userId, List<string> roleIds, Guid assignedBy);
        Task<ServiceResult<UserDto>> UpdateUserRolesAsync(Guid userId, List<string> roleIds, Guid updatedBy);
        Task<ServiceResult<UserDto>> RemoveRoleFromUserAsync(Guid userId, string roleId, Guid removedBy);
        Task<ServiceResult<bool>> ActivateUserAsync(Guid userId, Guid activatedBy);
        Task<ServiceResult<bool>> DeactivateUserAsync(Guid userId, Guid deactivatedBy);
        Task<ServiceResult<bool>> DeleteUserAsync(Guid userId, Guid deletedBy);

        /// <summary>
        /// Resets user password and returns the user info with generated temporary password
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="resetBy">ID of user performing the reset</param>
        /// <returns>ServiceResult containing user information and temporary password</returns>
        Task<ServiceResult<PasswordResetResultDto>> ResetPasswordAsync(Guid userId, Guid resetBy);

        Task<ServiceResult<bool>> ResendWelcomeEmailAsync(Guid userId);
    }
}

