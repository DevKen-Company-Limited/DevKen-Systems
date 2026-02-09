using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Services
{
    /// <summary>
    /// Service for aggregating permissions from multiple roles
    /// </summary>
    public interface IPermissionAggregationService
    {
        Task<List<string>> GetUserPermissionsAsync(Guid userId);
        Task<List<string>> GetRolePermissionsAsync(Guid roleId);
        Task<List<string>> GetCombinedPermissionsFromRolesAsync(IEnumerable<Guid> roleIds);
    }

    public class PermissionAggregationService : IPermissionAggregationService
    {
        private readonly IRepositoryManager _repository;

        public PermissionAggregationService(IRepositoryManager repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// Gets all unique permissions for a user by combining permissions from all their roles
        /// </summary>
        public async Task<List<string>> GetUserPermissionsAsync(Guid userId)
        {
            // Get all roles assigned to the user
            var userRoles = await _repository.UserRole
                .FindByCondition(ur => ur.UserId == userId, false)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            if (!userRoles.Any())
                return new List<string>();

            return await GetCombinedPermissionsFromRolesAsync(userRoles);
        }

        /// <summary>
        /// Gets all permissions for a single role
        /// </summary>
        public async Task<List<string>> GetRolePermissionsAsync(Guid roleId)
        {
            var permissions = await _repository.RolePermission
                .FindByCondition(rp => rp.RoleId == roleId, false)
                .Include(rp => rp.Permission)
                .Where(rp => rp.Permission != null && !string.IsNullOrWhiteSpace(rp.Permission.Key))
                .Select(rp => rp.Permission!.Key!)
                .Distinct()
                .ToListAsync();

            return permissions;
        }

        /// <summary>
        /// Combines permissions from multiple roles and removes duplicates
        /// </summary>
        public async Task<List<string>> GetCombinedPermissionsFromRolesAsync(IEnumerable<Guid> roleIds)
        {
            var roleIdList = roleIds.ToList();

            if (!roleIdList.Any())
                return new List<string>();

            // Get all permissions from all roles in one query
            var permissions = await _repository.RolePermission
                .FindByCondition(rp => roleIdList.Contains(rp.RoleId), false)
                .Include(rp => rp.Permission)
                .Where(rp => rp.Permission != null && !string.IsNullOrWhiteSpace(rp.Permission.Key))
                .Select(rp => rp.Permission!.Key!)
                .Distinct() // Remove duplicates at database level
                .OrderBy(p => p) // Optional: order alphabetically
                .ToListAsync();

            return permissions;
        }
    }
}