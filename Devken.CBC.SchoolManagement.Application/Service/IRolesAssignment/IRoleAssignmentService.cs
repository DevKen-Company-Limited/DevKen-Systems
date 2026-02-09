using Devken.CBC.SchoolManagement.Application.DTOs.RoleAssignment;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.IRolesAssignment
{
    /// <summary>
    /// Service interface for role assignment operations
    /// Supports both SuperAdmin (cross-tenant) and School-level (single-tenant) operations
    /// </summary>
    public interface IRoleAssignmentService
    {
        #region User Queries

        /// <summary>
        /// Get all users with their roles (paginated)
        /// </summary>
        /// <param name="tenantId">Tenant ID (null or Guid.Empty for SuperAdmin to see all users)</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        Task<PaginatedResult<UserWithRolesDto>> GetAllUsersWithRolesAsync(
            Guid? tenantId,
            int pageNumber,
            int pageSize);

        /// <summary>
        /// Search for users by email, first name, or last name
        /// </summary>
        /// <param name="searchTerm">Search term (minimum 2 characters)</param>
        /// <param name="tenantId">Tenant ID (null or Guid.Empty for SuperAdmin)</param>
        Task<List<UserSearchResultDto>> SearchUsersAsync(
            string searchTerm,
            Guid? tenantId);

        /// <summary>
        /// Get a specific user with their roles and permissions
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="tenantId">Tenant ID (null or Guid.Empty for SuperAdmin)</param>
        Task<UserWithRolesDto?> GetUserWithRolesAsync(
            Guid userId,
            Guid? tenantId);

        #endregion

        #region Role Assignment Commands

        /// <summary>
        /// Assign a single role to a user
        /// </summary>
        Task<RoleAssignmentResult> AssignRoleToUserAsync(
            Guid userId,
            Guid roleId,
            Guid? tenantId);

        /// <summary>
        /// Assign multiple roles to a user
        /// </summary>
        Task<RoleAssignmentResult> AssignMultipleRolesToUserAsync(
            Guid userId,
            List<Guid> roleIds,
            Guid? tenantId);

        /// <summary>
        /// Remove a specific role from a user
        /// </summary>
        Task<RoleAssignmentResult> RemoveRoleFromUserAsync(
            Guid userId,
            Guid roleId,
            Guid? tenantId);

        /// <summary>
        /// Update user's roles (replaces all existing roles)
        /// </summary>
        Task<RoleAssignmentResult> UpdateUserRolesAsync(
            Guid userId,
            List<Guid> roleIds,
            Guid? tenantId);

        Task<Dictionary<Guid, int>> GetRoleUserCountsAsync(Guid tenantId);

        /// <summary>
        /// Remove all roles from a user
        /// </summary>
        Task<RoleAssignmentResult> RemoveAllRolesFromUserAsync(
            Guid userId,
            Guid? tenantId);

        #endregion

        #region Role Queries

        /// <summary>
        /// Get users assigned to a specific role (paginated)
        /// </summary>
        Task<PaginatedResult<UserWithRolesDto>> GetUsersByRoleAsync(
            Guid roleId,
            Guid? tenantId,
            int pageNumber,
            int pageSize);

        /// <summary>
        /// Get all system roles (for SuperAdmin)
        /// Returns all roles regardless of tenant
        /// </summary>
        Task<List<RoleDto>> GetAllRolesAsync();

        /// <summary>
        /// Get available roles for a specific tenant (for School users)
        /// Returns system roles and tenant-specific roles
        /// </summary>
        Task<List<RoleDto>> GetAvailableRolesAsync(Guid tenantId);

        /// <summary>
        /// Check if a user has a specific role
        /// </summary>
        Task<bool> UserHasRoleAsync(
            Guid userId,
            Guid roleId,
            Guid? tenantId);

        #endregion
    }
}