using Devken.CBC.SchoolManagement.Application.DTOs.RoleAssignment;

namespace Devken.CBC.SchoolManagement.Application.Service.IRolesAssignment
{
    public interface IRoleAssignmentService
    {
        #region Query Methods

        /// <summary>
        /// Get a user with all their roles.
        /// SuperAdmin: tenantId = null (global search)
        /// Tenant user: tenantId required
        /// </summary>
        Task<UserWithRolesDto?> GetUserWithRolesAsync(
            Guid userId,
            Guid? tenantId);

        /// <summary>
        /// Get users assigned to a specific role (paginated).
        /// </summary>
        Task<PaginatedResult<UserWithRolesDto>> GetUsersByRoleAsync(
            Guid roleId,
            Guid? tenantId,
            int pageNumber,
            int pageSize);

        /// <summary>
        /// Get all users with their roles (paginated).
        /// </summary>
        Task<PaginatedResult<UserWithRolesDto>> GetAllUsersWithRolesAsync(
            Guid? tenantId,
            int pageNumber,
            int pageSize);

        /// <summary>
        /// Get all available roles.
        /// SuperAdmin: returns system + tenant roles
        /// Tenant user: tenant-specific roles only
        /// </summary>
        Task<List<UserRoleDto>> GetAvailableRolesAsync(
            Guid? tenantId);

        /// <summary>
        /// Search users by name, email, or username.
        /// </summary>
        Task<List<UserSearchResultDto>> SearchUsersAsync(
            string searchTerm,
            Guid? tenantId);

        /// <summary>
        /// Check if a user has a specific role.
        /// </summary>
        Task<bool> UserHasRoleAsync(
            Guid userId,
            Guid roleId,
            Guid? tenantId);

        #endregion

        #region Command Methods

        /// <summary>
        /// Assign a role to a user.
        /// TenantId required for tenant roles.
        /// </summary>
        Task<RoleAssignmentResult> AssignRoleToUserAsync(
            Guid userId,
            Guid roleId,
            Guid? tenantId);

        /// <summary>
        /// Assign multiple roles to a user.
        /// </summary>
        Task<RoleAssignmentResult> AssignMultipleRolesToUserAsync(
            Guid userId,
            List<Guid> roleIds,
            Guid? tenantId);

        /// <summary>
        /// Remove a role from a user.
        /// </summary>
        Task<RoleAssignmentResult> RemoveRoleFromUserAsync(
            Guid userId,
            Guid roleId,
            Guid? tenantId);

        /// <summary>
        /// Replace all user roles with the provided list.
        /// </summary>
        Task<RoleAssignmentResult> UpdateUserRolesAsync(
            Guid userId,
            List<Guid> roleIds,
            Guid? tenantId);

        /// <summary>
        /// Remove all roles from a user.
        /// </summary>
        Task<RoleAssignmentResult> RemoveAllRolesFromUserAsync(
            Guid userId,
            Guid? tenantId);

        #endregion
    }
}
