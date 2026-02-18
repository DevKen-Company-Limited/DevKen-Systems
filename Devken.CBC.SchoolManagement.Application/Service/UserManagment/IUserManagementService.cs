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
        /// Resets the user's password and returns user info along with the generated temporary password.
        /// The caller is responsible for securely communicating the temporary password to the user.
        /// </summary>
        Task<ServiceResult<PasswordResetResultDto>> ResetPasswordAsync(Guid userId, Guid resetBy);

        Task<ServiceResult<bool>> ResendWelcomeEmailAsync(Guid userId);

        /// <summary>
        /// Returns roles available for assignment within a given school.
        /// Pass <c>null</c> to return all roles across all schools (SuperAdmin use only).
        /// </summary>
        Task<ServiceResult<List<RoleDto>>> GetAvailableRolesAsync(Guid? schoolId);
    }
}