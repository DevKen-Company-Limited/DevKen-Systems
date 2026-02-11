using Devken.CBC.SchoolManagement.Application.DTOs.RoleAssignment;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.IRolesAssignment;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Common;
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
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region ================= QUERY METHODS =================

        /// <summary>
        /// Get user with their roles and permissions
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="tenantId">Tenant ID (Guid.Empty for SuperAdmin, specific ID for school context)</param>
        public async Task<UserWithRolesDto?> GetUserWithRolesAsync(Guid userId, Guid? tenantId)
        {
            try
            {
                _logger.LogInformation("Fetching user {UserId} with roles for tenant {TenantId}", userId, tenantId);

                var user = await _repository.User
                    .FindByCondition(u => u.Id == userId &&
                        (!tenantId.HasValue || tenantId == Guid.Empty || u.TenantId == tenantId), false)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found", userId);
                    return null;
                }

                // Build role query - SuperAdmin (Guid.Empty) sees all roles
                var roleQuery = _repository.UserRole
                    .FindByCondition(ur => ur.UserId == userId, false);

                if (tenantId.HasValue && tenantId != Guid.Empty)
                {
                    roleQuery = roleQuery.Where(ur => ur.TenantId == tenantId);
                }

                // Fetch roles with permissions eagerly
                var roles = await roleQuery
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user {UserId} with roles", userId);
                throw;
            }
        }

        /// <summary>
        /// Get users assigned to a specific role
        /// </summary>
        /// 
        public async Task<Dictionary<Guid, int>> GetRoleUserCountsAsync(Guid tenantId)
        {
            try
            {
                _logger.LogInformation("Getting role user counts for tenant {TenantId}", tenantId);

                // Build query for user roles
                var userRolesQuery = _repository.UserRole
                    .FindByCondition(ur =>
                        (tenantId == Guid.Empty || ur.TenantId == tenantId),
                        false);

                // Get user roles with user and role included
                var userRoles = await userRolesQuery
                    .Include(ur => ur.User)
                    .Include(ur => ur.Role)
                    .Where(ur => ur.User != null && ur.Role != null)
                    .ToListAsync();

                // Count distinct users per role
                var counts = userRoles
                    .GroupBy(ur => ur.RoleId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(ur => ur.UserId).Distinct().Count()
                    );

                // Get all roles for the tenant
                var rolesQuery = _repository.Role
                    .FindByCondition(r => tenantId == Guid.Empty || r.TenantId == tenantId, false);

                var allRoles = await rolesQuery.ToListAsync();

                // Include all roles even if they have 0 users
                foreach (var role in allRoles)
                {
                    if (!counts.ContainsKey(role.Id))
                    {
                        counts[role.Id] = 0;
                    }
                }

                _logger.LogInformation("Retrieved user counts for {Count} roles in tenant {TenantId}",
                    counts.Count, tenantId);

                return counts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role user counts for tenant {TenantId}", tenantId);
                throw;
            }
        }
        public async Task<PaginatedResult<UserWithRolesDto>> GetUsersByRoleAsync(
            Guid roleId,
            Guid? tenantId,
            int pageNumber,
            int pageSize)
        {
            try
            {
                _logger.LogInformation("Fetching users for role {RoleId}, tenant {TenantId}", roleId, tenantId);

                var query = _repository.UserRole
                    .FindByCondition(ur => ur.RoleId == roleId, false);

                if (tenantId.HasValue && tenantId != Guid.Empty)
                {
                    query = query.Where(ur => ur.TenantId == tenantId);
                }

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users for role {RoleId}", roleId);
                throw;
            }
        }

        /// <summary>
        /// Get all users with their roles (paginated)
        /// </summary>
        public async Task<PaginatedResult<UserWithRolesDto>> GetAllUsersWithRolesAsync(
            Guid? tenantId,
            int pageNumber,
            int pageSize)
        {
            try
            {
                _logger.LogInformation("Fetching all users for tenant {TenantId}, page {Page}", tenantId, pageNumber);

                var query = _repository.User
                    .FindByCondition(u => !tenantId.HasValue || tenantId == Guid.Empty || u.TenantId == tenantId, false);

                var totalCount = await query.CountAsync();

                // Fetch users with roles and permissions eagerly
                var users = await query
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                            .ThenInclude(r => r.RolePermissions)
                                .ThenInclude(rp => rp.Permission)
                    .OrderBy(u => u.FirstName)
                        .ThenBy(u => u.LastName)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = users.Select(u =>
                {
                    var roleDtos = u.UserRoles?
                        .Select(ur => ur.Role)
                        .Where(r => r != null)
                        .Select(r => new UserRoleDto
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

                _logger.LogInformation("Retrieved {Count} users out of {Total}", result.Count, totalCount);

                return new PaginatedResult<UserWithRolesDto>(result, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all users with roles");
                throw;
            }
        }

        /// <summary>
        /// Get all system roles (for SuperAdmin)
        /// Returns all roles regardless of tenant
        /// </summary>
        public async Task<List<RoleDto>> GetAllRolesAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all system roles for SuperAdmin");

                var roles = await _repository.Role
                    .FindAll(false)
                    .OrderBy(r => r.Name)
                    .ToListAsync();

                var roleDtos = roles.Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name ?? string.Empty,
                    Description = r.Description,
                    IsSystemRole = r.IsSystemRole,
                    TenantId = r.TenantId
                }).ToList();

                _logger.LogInformation("Retrieved {Count} roles for SuperAdmin", roleDtos.Count);

                return roleDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all system roles");
                throw;
            }
        }

        /// <summary>
        /// Get available roles for a specific tenant (for School users)
        /// Returns system roles and tenant-specific roles
        /// </summary>
        public async Task<List<RoleDto>> GetAvailableRolesAsync(Guid tenantId)
        {
            try
            {
                _logger.LogInformation("Fetching available roles for tenant {TenantId}", tenantId);

                // Get system roles (IsSystemRole = true or TenantId is null/empty) and roles for the specific tenant
                var roles = await _repository.Role
                    .FindByCondition(r =>
                        r.IsSystemRole ||
                        r.TenantId == Guid.Empty ||
                        r.TenantId == tenantId, false)
                    .OrderBy(r => r.Name)
                    .ToListAsync();

                var roleDtos = roles.Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name ?? string.Empty,
                    Description = r.Description,
                    IsSystemRole = r.IsSystemRole,
                    TenantId = r.TenantId
                }).ToList();

                _logger.LogInformation("Retrieved {Count} roles for tenant {TenantId}", roleDtos.Count, tenantId);

                return roleDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available roles for tenant {TenantId}", tenantId);
                throw;
            }
        }

        /// <summary>
        /// Check if a user has a specific role
        /// </summary>
        public async Task<bool> UserHasRoleAsync(Guid userId, Guid roleId, Guid? tenantId)
        {
            try
            {
                var query = _repository.UserRole
                    .FindByCondition(ur => ur.UserId == userId && ur.RoleId == roleId, false);

                if (tenantId.HasValue && tenantId != Guid.Empty)
                {
                    query = query.Where(ur => ur.TenantId == tenantId);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {UserId} has role {RoleId}", userId, roleId);
                throw;
            }
        }

        /// <summary>
        /// Search for users by email, first name, or last name
        /// </summary>
        public async Task<List<UserSearchResultDto>> SearchUsersAsync(string searchTerm, Guid? tenantId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return new List<UserSearchResultDto>();

                searchTerm = searchTerm.Trim().ToLower();

                _logger.LogInformation("Searching users with term '{Term}' for tenant {TenantId}", searchTerm, tenantId);

                var usersQuery = _repository.User
                    .FindByCondition(u =>
                        (!tenantId.HasValue || tenantId == Guid.Empty || u.TenantId == tenantId) &&
                        (u.Email.ToLower().Contains(searchTerm) ||
                         u.FirstName.ToLower().Contains(searchTerm) ||
                         u.LastName.ToLower().Contains(searchTerm)), false);

                var results = await usersQuery
                    .OrderBy(u => u.FirstName)
                        .ThenBy(u => u.LastName)
                    .Take(10)
                    .Select(u => new UserSearchResultDto
                    {
                        UserId = u.Id,
                        Email = u.Email ?? string.Empty,
                        FullName = u.FullName ?? string.Empty,
                        UserName = u.Email ?? string.Empty,
                        IsSuperAdmin = u.IsSuperAdmin
                    })
                    .ToListAsync();

                _logger.LogInformation("Found {Count} users matching '{Term}'", results.Count, searchTerm);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with term '{Term}'", searchTerm);
                throw;
            }
        }

        #endregion

        #region ================= COMMAND METHODS =================

        /// <summary>
        /// Assign a single role to a user
        /// </summary>
        public async Task<RoleAssignmentResult> AssignRoleToUserAsync(Guid userId, Guid roleId, Guid? tenantId)
        {
            return await AssignMultipleRolesToUserAsync(userId, new List<Guid> { roleId }, tenantId);
        }

        /// <summary>
        /// Assign multiple roles to a user
        /// </summary>
        public async Task<RoleAssignmentResult> AssignMultipleRolesToUserAsync(
            Guid userId,
            List<Guid> roleIds,
            Guid? tenantId)
        {
            try
            {
                _logger.LogInformation("Assigning {Count} roles to user {UserId}", roleIds.Count, userId);

                var tenantFilterId = tenantId ?? Guid.Empty;

                // Get existing role assignments
                var existingRoleIdsQuery = _repository.UserRole
                    .FindByCondition(ur => ur.UserId == userId, false);

                if (tenantId.HasValue && tenantId != Guid.Empty)
                {
                    existingRoleIdsQuery = existingRoleIdsQuery.Where(ur => ur.TenantId == tenantId);
                }

                var existingRoleIds = await existingRoleIdsQuery
                    .Select(ur => ur.RoleId)
                    .ToListAsync();

                // Find new roles to assign
                var newRoleIds = roleIds.Distinct().Except(existingRoleIds).ToList();

                if (!newRoleIds.Any())
                {
                    _logger.LogInformation("User {UserId} already has all specified roles", userId);
                    return RoleAssignmentResult.Successful(
                        "Roles already assigned",
                        await GetUserWithRolesAsync(userId, tenantId));
                }

                // Create new role assignments
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

                _logger.LogInformation("Successfully assigned {Count} new roles to user {UserId}", newRoleIds.Count, userId);

                return RoleAssignmentResult.Successful(
                    "Roles assigned successfully",
                    await GetUserWithRolesAsync(userId, tenantId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning roles to user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Update user's roles (replaces all existing roles)
        /// </summary>
        // FIXED VERSION of UpdateUserRolesAsync in RoleAssignmentService
        // This prevents the duplicate key error by checking which roles already exist

        public async Task<RoleAssignmentResult> UpdateUserRolesAsync(
            Guid userId,
            List<Guid> roleIds,
            Guid? tenantId)
        {
            try
            {
                _logger.LogInformation(
                    "Updating roles for user {UserId}. Incoming roles: {Roles}",
                    userId, string.Join(", ", roleIds));

                roleIds = roleIds?.Distinct().ToList() ?? new List<Guid>();

                // 1️⃣ Validate user
                var user = await _repository.User
                    .FindByCondition(u => u.Id == userId, false)
                    .FirstOrDefaultAsync();

                if (user == null)
                    return RoleAssignmentResult.Failed("User not found");

                if (tenantId.HasValue && tenantId != Guid.Empty && user.TenantId != tenantId)
                    return RoleAssignmentResult.Failed("User does not belong to the specified tenant");

                var effectiveTenantId = tenantId ?? Guid.Empty;

                // 2️⃣ Load current user roles
                var userRolesQuery = _repository.UserRole
                    .FindByCondition(ur => ur.UserId == userId, true);

                if (tenantId.HasValue && tenantId != Guid.Empty)
                    userRolesQuery = userRolesQuery.Where(ur => ur.TenantId == tenantId);

                var currentUserRoles = await userRolesQuery.ToListAsync();
                var currentRoleIds = currentUserRoles.Select(ur => ur.RoleId).ToHashSet();

                // 3️⃣ Diff roles
                var rolesToRemove = currentRoleIds.Except(roleIds).ToList();
                var rolesToAdd = roleIds.Except(currentRoleIds).ToList();

                _logger.LogInformation(
                    "User {UserId}: Removing {RemoveCount}, Adding {AddCount}",
                    userId, rolesToRemove.Count, rolesToAdd.Count);

                // 4️⃣ Validate roles to add
                if (rolesToAdd.Any())
                {
                    var roles = await _repository.Role
                        .FindByCondition(r => rolesToAdd.Contains(r.Id), false)
                        .ToListAsync();

                    if (roles.Count != rolesToAdd.Count)
                        return RoleAssignmentResult.Failed("One or more roles not found");

                    if (tenantId.HasValue && tenantId != Guid.Empty)
                    {
                        var invalidRoles = roles
                            .Where(r =>
                                !r.IsSystemRole &&
                                r.TenantId != Guid.Empty &&
                                r.TenantId != tenantId)
                            .ToList();

                        if (invalidRoles.Any())
                            return RoleAssignmentResult.Failed(
                                "One or more roles do not belong to the specified tenant");
                    }
                }

                // 5️⃣ Remove roles
                foreach (var userRole in currentUserRoles.Where(ur => rolesToRemove.Contains(ur.RoleId)))
                {
                    _repository.UserRole.Delete(userRole);
                }

                // 6️⃣ Add roles (safe insert)
                foreach (var roleId in rolesToAdd)
                {
                    _repository.UserRole.Create(new UserRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        RoleId = roleId,
                        TenantId = effectiveTenantId,
                        Status = (Domain.Enums.EntityStatus)1,
                        CreatedOn = DateTime.UtcNow,
                        UpdatedOn = DateTime.UtcNow,
                        CreatedBy = userId,
                        UpdatedBy = userId
                    });
                }

                await _repository.SaveAsync();

                _logger.LogInformation("Successfully updated roles for user {UserId}", userId);

                return RoleAssignmentResult.Successful(
                    "User roles updated successfully",
                    await GetUserWithRolesAsync(userId, tenantId));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex,
                    "Database constraint error while updating roles for user {UserId}",
                    userId);

                return RoleAssignmentResult.Failed(
                    "Failed to update roles due to a database constraint violation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error updating roles for user {UserId}",
                    userId);

                return RoleAssignmentResult.Failed("Failed to update user roles");
            }
        }

        /// <summary>
        /// Remove a specific role from a user
        /// </summary>
        public async Task<RoleAssignmentResult> RemoveRoleFromUserAsync(
            Guid userId,
            Guid roleId,
            Guid? tenantId)
        {
            try
            {
                _logger.LogInformation("Removing role {RoleId} from user {UserId}", roleId, userId);

                var roleQuery = _repository.UserRole
                    .FindByCondition(ur => ur.UserId == userId && ur.RoleId == roleId, true);

                if (tenantId.HasValue && tenantId != Guid.Empty)
                {
                    roleQuery = roleQuery.Where(ur => ur.TenantId == tenantId);
                }

                var role = await roleQuery.FirstOrDefaultAsync();

                if (role == null)
                {
                    _logger.LogWarning("Role {RoleId} not assigned to user {UserId}", roleId, userId);
                    return RoleAssignmentResult.Failed("Role not assigned to user");
                }

                _repository.UserRole.Delete(role);
                await _repository.SaveAsync();

                _logger.LogInformation("Successfully removed role {RoleId} from user {UserId}", roleId, userId);

                return RoleAssignmentResult.Successful(
                    "Role removed successfully",
                    await GetUserWithRolesAsync(userId, tenantId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role {RoleId} from user {UserId}", roleId, userId);
                throw;
            }
        }

        /// <summary>
        /// Remove all roles from a user
        /// </summary>
        public async Task<RoleAssignmentResult> RemoveAllRolesFromUserAsync(Guid userId, Guid? tenantId)
        {
            try
            {
                _logger.LogInformation("Removing all roles from user {UserId}", userId);

                var rolesQuery = _repository.UserRole
                    .FindByCondition(ur => ur.UserId == userId, true);

                if (tenantId.HasValue && tenantId != Guid.Empty)
                {
                    rolesQuery = rolesQuery.Where(ur => ur.TenantId == tenantId);
                }

                var roles = await rolesQuery.ToListAsync();

                foreach (var role in roles)
                {
                    _repository.UserRole.Delete(role);
                }

                await _repository.SaveAsync();

                _logger.LogInformation("Successfully removed {Count} roles from user {UserId}", roles.Count, userId);

                return RoleAssignmentResult.Successful(
                    "All roles removed successfully",
                    await GetUserWithRolesAsync(userId, tenantId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing all roles from user {UserId}", userId);
                throw;
            }
        }

        #endregion
    }
}