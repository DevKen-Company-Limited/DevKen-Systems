using Devken.CBC.SchoolManagement.Application.DTOs.RoleAssignment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.IRolesAssignment
{
    /// <summary>
    /// Service for managing user role assignments
    /// </summary>
    public interface IRoleAssignmentService
    {
        /// <summary>
        /// Assign a single role to a user
        /// </summary>
        Task<RoleAssignmentResult> AssignRoleToUserAsync(Guid userId, Guid roleId, Guid tenantId);

        /// <summary>
        /// Assign multiple roles to a user
        /// </summary>
        Task<RoleAssignmentResult> AssignMultipleRolesToUserAsync(Guid userId, List<Guid> roleIds, Guid tenantId);

        /// <summary>
        /// Remove a role from a user
        /// </summary>
        Task<RoleAssignmentResult> RemoveRoleFromUserAsync(Guid userId, Guid roleId, Guid tenantId);

        /// <summary>
        /// Update user roles (replace all existing roles)
        /// </summary>
        Task<RoleAssignmentResult> UpdateUserRolesAsync(Guid userId, List<Guid> roleIds, Guid tenantId);

        /// <summary>
        /// Get all roles assigned to a user
        /// </summary>
        Task<UserWithRolesResponse?> GetUserWithRolesAsync(Guid userId, Guid tenantId);

        /// <summary>
        /// Get all users with a specific role
        /// </summary>
        Task<UsersInRoleResponse> GetUsersByRoleAsync(Guid roleId, Guid tenantId, int pageNumber = 1, int pageSize = 20);

        /// <summary>
        /// Check if a user has a specific role
        /// </summary>
        Task<bool> UserHasRoleAsync(Guid userId, Guid roleId, Guid tenantId);

        /// <summary>
        /// Check if a user has any of the specified roles
        /// </summary>
        Task<bool> UserHasAnyRoleAsync(Guid userId, List<Guid> roleIds, Guid tenantId);

        /// <summary>
        /// Get all available roles for a tenant
        /// </summary>
        Task<List<RoleInfoDto>> GetAvailableRolesAsync(Guid tenantId);

        /// <summary>
        /// Remove all roles from a user
        /// </summary>
        Task<RoleAssignmentResult> RemoveAllRolesFromUserAsync(Guid userId, Guid tenantId);
    }
}
