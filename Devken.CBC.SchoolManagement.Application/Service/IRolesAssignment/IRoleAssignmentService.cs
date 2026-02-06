using Devken.CBC.SchoolManagement.Application.DTOs.RoleAssignment;

namespace Devken.CBC.SchoolManagement.Application.Service.IRolesAssignment
{
    public interface IRoleAssignmentService
    {
        #region ================= QUERY METHODS =================

        /// <summary>
        /// Get a user with all their roles.
        /// SuperAdmin: tenantId = null (global lookup)
        /// Tenant user: tenantId REQUIRED
        /// </summary>
        Task<UserWithRolesDto?> GetUserWithRolesAsync(
            Guid userId,
            Guid? tenantId);

        /// <summary>
        /// Get users assigned to a specific role (paginated).
        /// TenantId REQUIRED for tenant roles.
        /// </summary>
        Task<PaginatedResult<UserWithRolesDto>> GetUsersByRoleAsync(
            Guid roleId,
            Guid? tenantId,
            int pageNumber,
            int pageSize);

        /// <summary>
        /// Get all users with their roles (paginated).
        /// SuperAdmin: tenantId = null (all tenants)
        /// Tenant user: tenantId REQUIRED
        /// </summary>
        Task<PaginatedResult<UserWithRolesDto>> GetAllUsersWithRolesAsync(
            Guid? tenantId,
            int pageNumber,
            int pageSize);

        /// <summary>
        /// Get all available roles.
        /// SuperAdmin: system + tenant roles
        /// Tenant user: tenant-specific + system roles
        /// </summary>
        Task<List<UserRoleDto>> GetAvailableRolesAsync(
            Guid? tenantId);

        /// <summary>
        /// Search users by name, email, or username.
        /// TenantId REQUIRED for tenant users.
        /// </summary>
        Task<List<UserSearchResultDto>> SearchUsersAsync(
            string searchTerm,
            Guid? tenantId);

        /// <summary>
        /// Check if a user has a specific role.
        /// TenantId REQUIRED for tenant users.
        /// </summary>
        Task<bool> UserHasRoleAsync(
            Guid userId,
            Guid roleId,
            Guid? tenantId);

        #endregion

        #region ================= COMMAND METHODS =================

        /// <summary>
        /// Assign a single role to a user.
        /// TenantId REQUIRED for tenant roles.
        /// </summary>
        Task<RoleAssignmentResult> AssignRoleToUserAsync(
            Guid userId,
            Guid roleId,
            Guid? tenantId);

        /// <summary>
        /// Assign multiple roles to a user.
        /// TenantId REQUIRED for tenant roles.
        /// </summary>
        Task<RoleAssignmentResult> AssignMultipleRolesToUserAsync(
            Guid userId,
            List<Guid> roleIds,
            Guid? tenantId);

        /// <summary>
        /// Remove a specific role from a user.
        /// TenantId REQUIRED for tenant roles.
        /// </summary>
        Task<RoleAssignmentResult> RemoveRoleFromUserAsync(
            Guid userId,
            Guid roleId,
            Guid? tenantId);

        /// <summary>
        /// Replace all user roles with the provided list.
        /// TenantId REQUIRED.
        /// </summary>
        Task<RoleAssignmentResult> UpdateUserRolesAsync(
            Guid userId,
            List<Guid> roleIds,
            Guid? tenantId);

        /// <summary>
        /// Remove all roles from a user.
        /// TenantId REQUIRED for tenant users.
        /// </summary>
        Task<RoleAssignmentResult> RemoveAllRolesFromUserAsync(
            Guid userId,
            Guid? tenantId);

        #endregion
    }
}
