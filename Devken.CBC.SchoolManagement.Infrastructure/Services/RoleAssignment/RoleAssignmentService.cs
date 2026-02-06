using Devken.CBC.SchoolManagement.Application.DTOs.RoleAssignment;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.IRolesAssignment;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.RoleAssignment
{
    public class RoleAssignmentService : IRoleAssignmentService
    {
        private readonly IRepositoryManager _repository;
        private readonly AppDbContext _context;
        private readonly ILogger<RoleAssignmentService> _logger;

        public RoleAssignmentService(
            IRepositoryManager repository,
            AppDbContext context,
            ILogger<RoleAssignmentService> logger)
        {
            _repository = repository;
            _context = context;
            _logger = logger;
        }

        #region ================= QUERY METHODS =================

        public async Task<UserWithRolesDto?> GetUserWithRolesAsync(Guid userId, Guid? tenantId)
        {
            var tenantFilterId = tenantId ?? Guid.Empty;

            var user = await _repository.User
                .FindByCondition(u => u.Id == userId && (!tenantId.HasValue || u.TenantId == tenantId), false)
                .FirstOrDefaultAsync();

            if (user == null)
                return null;

            // Fetch roles including permissions
            var roles = await _repository.UserRole
                .FindByCondition(ur => ur.UserId == userId && ur.TenantId == tenantFilterId, false)
                .Include(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .Select(ur => ur.Role)
                .ToListAsync();

            var roleDtos = roles.Select(r => new UserRoleDto
            {
                RoleId = r.Id,
                RoleName = r.Name ?? string.Empty,
                Description = r.Description,
                IsSystemRole = r.IsSystemRole,
                PermissionCount = r.RolePermissions?.Count(rp => rp.Permission != null) ?? 0,
                Permissions = r.RolePermissions?
                    .Where(rp => rp.Permission != null)
                    .Select(rp => new RolePermissionDto
                    {
                        PermissionId = rp.PermissionId,
                        PermissionName = rp.Permission?.DisplayName ?? string.Empty,
                        Description = rp.Permission?.Description
                    })
                    .ToList() ?? new List<RolePermissionDto>()
            }).ToList();

            var permissions = roleDtos
                .SelectMany(r => r.Permissions ?? new List<RolePermissionDto>())
                .Select(p => p.PermissionName)
                .Distinct()
                .ToList();

            return new UserWithRolesDto
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName ?? string.Empty,
                UserName = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                TenantId = user.TenantId,
                Roles = roleDtos,
                Permissions = permissions,
                RequirePasswordChange = user.RequirePasswordChange,
                IsSuperAdmin = user.IsSuperAdmin
            };
        }

        public async Task<PaginatedResult<UserWithRolesDto>> GetUsersByRoleAsync(Guid roleId, Guid? tenantId, int pageNumber, int pageSize)
        {
            var tenantFilterId = tenantId ?? Guid.Empty;

            var query = _repository.UserRole
                .FindByCondition(ur => ur.RoleId == roleId && ur.TenantId == tenantFilterId, false);

            var totalCount = await query.CountAsync();

            var users = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(ur => ur.User)
                .Distinct()
                .ToListAsync();

            var result = new List<UserWithRolesDto>();
            foreach (var user in users)
            {
                var dto = await GetUserWithRolesAsync(user.Id, tenantId);
                if (dto != null)
                    result.Add(dto);
            }

            return new PaginatedResult<UserWithRolesDto>(result, totalCount, pageNumber, pageSize);
        }

        public async Task<PaginatedResult<UserWithRolesDto>> GetAllUsersWithRolesAsync(Guid? tenantId, int pageNumber, int pageSize)
        {
            var query = _repository.User
                .FindByCondition(u => !tenantId.HasValue || u.TenantId == tenantId, false);

            var totalCount = await query.CountAsync();

            // Fetch users with roles and permissions eagerly
            var users = await query
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = users.Select(u =>
            {
                var roleDtos = u.UserRoles?.Select(ur => ur.Role).Where(r => r != null).Select(r => new UserRoleDto
                {
                    RoleId = r.Id,
                    RoleName = r.Name ?? string.Empty,
                    Description = r.Description,
                    IsSystemRole = r.IsSystemRole,
                    PermissionCount = r.RolePermissions?.Count(rp => rp.Permission != null) ?? 0,
                    Permissions = r.RolePermissions?
                        .Where(rp => rp.Permission != null)
                        .Select(rp => new RolePermissionDto
                        {
                            PermissionId = rp.PermissionId,
                            PermissionName = rp.Permission?.DisplayName ?? string.Empty,
                            Description = rp.Permission?.Description
                        })
                        .ToList() ?? new List<RolePermissionDto>()
                }).ToList() ?? new List<UserRoleDto>();

                var permissions = roleDtos
                    .SelectMany(r => r.Permissions ?? new List<RolePermissionDto>())
                    .Select(p => p.PermissionName)
                    .Distinct()
                    .ToList();

                return new UserWithRolesDto
                {
                    UserId = u.Id,
                    Email = u.Email ?? string.Empty,
                    FullName = u.FullName ?? string.Empty,
                    UserName = u.Email ?? string.Empty,
                    FirstName = u.FirstName ?? string.Empty,
                    LastName = u.LastName ?? string.Empty,
                    TenantId = u.TenantId,
                    Roles = roleDtos,
                    Permissions = permissions,
                    RequirePasswordChange = u.RequirePasswordChange,
                    IsSuperAdmin = u.IsSuperAdmin
                };
            }).ToList();

            return new PaginatedResult<UserWithRolesDto>(result, totalCount, pageNumber, pageSize);
        }

        public async Task<List<UserRoleDto>> GetAvailableRolesAsync(Guid? tenantId)
        {
            var roles = await _repository.Role
                .FindByCondition(r => !tenantId.HasValue || r.TenantId == tenantId || r.TenantId == Guid.Empty, false)
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .ToListAsync();

            return roles.Select(r => new UserRoleDto
            {
                RoleId = r.Id,
                RoleName = r.Name ?? string.Empty,
                Description = r.Description,
                IsSystemRole = r.IsSystemRole,
                PermissionCount = r.RolePermissions?.Count(rp => rp.Permission != null) ?? 0,
                Permissions = r.RolePermissions?
                    .Where(rp => rp.Permission != null)
                    .Select(rp => new RolePermissionDto
                    {
                        PermissionId = rp.PermissionId,
                        PermissionName = rp.Permission?.DisplayName ?? string.Empty,
                        Description = rp.Permission?.Description
                    })
                    .ToList() ?? new List<RolePermissionDto>()
            }).ToList();
        }

        public async Task<bool> UserHasRoleAsync(Guid userId, Guid roleId, Guid? tenantId)
        {
            var tenantFilterId = tenantId ?? Guid.Empty;

            return await _repository.UserRole
                .FindByCondition(ur => ur.UserId == userId && ur.RoleId == roleId && ur.TenantId == tenantFilterId, false)
                .AnyAsync();
        }

        public async Task<List<UserSearchResultDto>> SearchUsersAsync(string searchTerm, Guid? tenantId)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<UserSearchResultDto>();

            searchTerm = searchTerm.Trim();

            var usersQuery = _repository.User
                .FindByCondition(u =>
                    (!tenantId.HasValue || u.TenantId == tenantId) &&
                    (u.Email.Contains(searchTerm) ||
                     u.FirstName.Contains(searchTerm) ||
                     u.LastName.Contains(searchTerm)), false);

            return await usersQuery
                .OrderBy(u => u.FirstName)
                .Take(10)
                .Select(u => new UserSearchResultDto
                {
                    UserId = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    UserName = u.Email,
                    IsSuperAdmin = u.IsSuperAdmin
                })
                .ToListAsync();
        }

        #endregion

        #region ================= COMMAND METHODS =================

        public async Task<RoleAssignmentResult> AssignRoleToUserAsync(Guid userId, Guid roleId, Guid? tenantId)
            => await AssignMultipleRolesToUserAsync(userId, new List<Guid> { roleId }, tenantId);

        public async Task<RoleAssignmentResult> AssignMultipleRolesToUserAsync(Guid userId, List<Guid> roleIds, Guid? tenantId)
        {
            var tenantFilterId = tenantId ?? Guid.Empty;

            var existingRoleIds = await _repository.UserRole
                .FindByCondition(ur => ur.UserId == userId && ur.TenantId == tenantFilterId, false)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            var newRoleIds = roleIds.Distinct().Except(existingRoleIds).ToList();

            if (!newRoleIds.Any())
                return RoleAssignmentResult.Successful("Roles already assigned", await GetUserWithRolesAsync(userId, tenantId));

            foreach (var roleId in newRoleIds)
            {
                _repository.UserRole.Create(new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    RoleId = roleId,
                    TenantId = tenantFilterId
                });
            }

            await _repository.SaveAsync();

            return RoleAssignmentResult.Successful("Roles assigned successfully", await GetUserWithRolesAsync(userId, tenantId));
        }

        public async Task<RoleAssignmentResult> UpdateUserRolesAsync(Guid userId, List<Guid> roleIds, Guid? tenantId)
        {
            var tenantFilterId = tenantId ?? Guid.Empty;

            var existingRoles = await _repository.UserRole
                .FindByCondition(ur => ur.UserId == userId && ur.TenantId == tenantFilterId, true)
                .ToListAsync();

            foreach (var role in existingRoles)
                _repository.UserRole.Delete(role);

            foreach (var roleId in roleIds.Distinct())
            {
                _repository.UserRole.Create(new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    RoleId = roleId,
                    TenantId = tenantFilterId
                });
            }

            await _repository.SaveAsync();

            return RoleAssignmentResult.Successful("User roles updated", await GetUserWithRolesAsync(userId, tenantId));
        }

        public async Task<RoleAssignmentResult> RemoveRoleFromUserAsync(Guid userId, Guid roleId, Guid? tenantId)
        {
            var tenantFilterId = tenantId ?? Guid.Empty;

            var role = await _repository.UserRole
                .FindByCondition(ur => ur.UserId == userId && ur.RoleId == roleId && ur.TenantId == tenantFilterId, true)
                .FirstOrDefaultAsync();

            if (role == null)
                return RoleAssignmentResult.Failed("Role not assigned");

            _repository.UserRole.Delete(role);
            await _repository.SaveAsync();

            return RoleAssignmentResult.Successful("Role removed", await GetUserWithRolesAsync(userId, tenantId));
        }

        public async Task<RoleAssignmentResult> RemoveAllRolesFromUserAsync(Guid userId, Guid? tenantId)
        {
            var tenantFilterId = tenantId ?? Guid.Empty;

            var roles = await _repository.UserRole
                .FindByCondition(ur => ur.UserId == userId && ur.TenantId == tenantFilterId, true)
                .ToListAsync();

            foreach (var role in roles)
                _repository.UserRole.Delete(role);

            await _repository.SaveAsync();

            return RoleAssignmentResult.Successful("All roles removed", await GetUserWithRolesAsync(userId, tenantId));
        }

        #endregion
    }
}
