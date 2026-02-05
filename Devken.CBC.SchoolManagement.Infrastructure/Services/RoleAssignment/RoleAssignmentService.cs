using Devken.CBC.SchoolManagement.Application.DTOs.RoleAssignment;
using Devken.CBC.SchoolManagement.Application.Service.IRolesAssignment;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.RoleAssignment
{
    public class RoleAssignmentService : IRoleAssignmentService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RoleAssignmentService> _logger;

        public RoleAssignmentService(AppDbContext context, ILogger<RoleAssignmentService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region ================= USER SEARCH =================

        public async Task<List<UserSearchResultDto>> SearchUsersAsync(string searchTerm, Guid? tenantId)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<UserSearchResultDto>();

            try
            {
                searchTerm = searchTerm.Trim().ToLower();
                var results = new List<UserSearchResultDto>();

                // Execute queries sequentially to avoid concurrency issues
                if (tenantId.HasValue)
                {
                    // Search only in Users table for specific tenant
                    results = await _context.Users
                        .AsNoTracking()
                        .Where(u => u.TenantId == tenantId.Value &&
                            (u.Email.ToLower().Contains(searchTerm) ||
                             u.FirstName.ToLower().Contains(searchTerm) ||
                             u.LastName.ToLower().Contains(searchTerm)))
                        .OrderBy(u => u.FirstName)
                        .ThenBy(u => u.LastName)
                        .Take(10)
                        .Select(u => new UserSearchResultDto
                        {
                            UserId = u.Id,
                            Email = u.Email,
                            FullName = u.FullName,
                            UserName = u.FullName,
                            IsSuperAdmin = false
                        })
                        .ToListAsync();
                }
                else
                {
                    // Search globally - execute queries sequentially
                    var userResults = await _context.Users
                        .AsNoTracking()
                        .Where(u =>
                            u.Email.ToLower().Contains(searchTerm) ||
                            u.FirstName.ToLower().Contains(searchTerm) ||
                            u.LastName.ToLower().Contains(searchTerm))
                        .OrderBy(u => u.FirstName)
                        .ThenBy(u => u.LastName)
                        .Take(10)
                        .Select(u => new UserSearchResultDto
                        {
                            UserId = u.Id,
                            Email = u.Email,
                            FullName = u.FullName,
                            UserName = u.FullName,
                            IsSuperAdmin = false
                        })
                        .ToListAsync();

                    var superAdminResults = await _context.SuperAdmins
                        .AsNoTracking()
                        .Where(sa => sa.IsActive &&
                            (sa.Email.ToLower().Contains(searchTerm) ||
                             sa.FirstName.ToLower().Contains(searchTerm) ||
                             sa.LastName.ToLower().Contains(searchTerm)))
                        .OrderBy(sa => sa.FirstName)
                        .ThenBy(sa => sa.LastName)
                        .Take(10)
                        .Select(sa => new UserSearchResultDto
                        {
                            UserId = sa.Id,
                            Email = sa.Email,
                            FullName = $"{sa.FirstName} {sa.LastName}",
                            UserName = $"{sa.FirstName} {sa.LastName}",
                            IsSuperAdmin = true
                        })
                        .ToListAsync();

                    // Combine and limit to 10 total results
                    results.AddRange(userResults);
                    results.AddRange(superAdminResults);
                    results = results
                        .OrderBy(u => u.FullName)
                        .Take(10)
                        .ToList();
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users for tenant {TenantId}", tenantId);
                return new List<UserSearchResultDto>();
            }
        }

        #endregion

        #region ================= GET USERS & ROLES =================

        public async Task<UserWithRolesDto?> GetUserWithRolesAsync(Guid userId, Guid? tenantId)
        {
            try
            {
                User? user = null;

                // Check regular users first - execute query and materialize
                var userQuery = _context.Users
                    .AsNoTracking();

                if (tenantId.HasValue)
                {
                    user = await userQuery
                        .Include(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                                .ThenInclude(r => r.RolePermissions)
                                    .ThenInclude(rp => rp.Permission)
                        .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId.Value);
                }
                else
                {
                    user = await userQuery
                        .Include(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                                .ThenInclude(r => r.RolePermissions)
                                    .ThenInclude(rp => rp.Permission)
                        .FirstOrDefaultAsync(u => u.Id == userId);
                }

                if (user != null)
                    return MapUserToDto(user, tenantId);

                // If not found in Users and searching globally, check SuperAdmins
                if (!tenantId.HasValue)
                {
                    var superAdmin = await _context.SuperAdmins
                        .AsNoTracking()
                        .FirstOrDefaultAsync(sa => sa.Id == userId && sa.IsActive);

                    if (superAdmin != null)
                        return MapSuperAdminToDto(superAdmin);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user roles for user {UserId}", userId);
                return null;
            }
        }

        public async Task<List<UserRoleDto>> GetAvailableRolesAsync(Guid? tenantId)
        {
            try
            {
                var query = _context.Roles.AsNoTracking();

                if (tenantId.HasValue)
                    query = query.Where(r => r.TenantId == tenantId.Value || r.TenantId == Guid.Empty);

                return await query
                    .OrderBy(r => r.Name)
                    .Select(r => new UserRoleDto
                    {
                        RoleId = r.Id,
                        RoleName = r.Name,
                        Description = r.Description,
                        IsSystemRole = r.IsSystemRole,
                        PermissionCount = r.RolePermissions.Count
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles for tenant {TenantId}", tenantId);
                return new List<UserRoleDto>();
            }
        }

        public async Task<PaginatedResult<UserWithRolesDto>> GetAllUsersWithRolesAsync(Guid? tenantId, int pageNumber, int pageSize)
        {
            try
            {
                _logger.LogDebug("Executing query for users with tenantId: {TenantId}", tenantId);

                var results = new List<UserWithRolesDto>();
                int totalCount;

                if (tenantId.HasValue)
                {
                    // Get users for specific tenant
                    var userQuery = _context.Users
                        .AsNoTracking()
                        .Where(u => u.TenantId == tenantId.Value);

                    totalCount = await userQuery.CountAsync();

                    var users = await userQuery
                        .Include(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                                .ThenInclude(r => r.RolePermissions)
                                    .ThenInclude(rp => rp.Permission)
                        .OrderBy(u => u.FirstName)
                        .ThenBy(u => u.LastName)
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();

                    results.AddRange(users.Select(u => MapUserToDto(u, tenantId)));
                }
                else
                {
                    // Get all users without tenant filtering
                    var userQuery = _context.Users
                        .AsNoTracking()
                        .Where(u => u.TenantId == Guid.Empty || u.TenantId == null);

                    var userCount = await userQuery.CountAsync();

                    var users = await userQuery
                        .Include(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                                .ThenInclude(r => r.RolePermissions)
                                    .ThenInclude(rp => rp.Permission)
                        .OrderBy(u => u.FirstName)
                        .ThenBy(u => u.LastName)
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();

                    // Get super admins - execute separately to avoid concurrency
                    var superAdmins = await _context.SuperAdmins
                        .AsNoTracking()
                        .Where(sa => sa.IsActive)
                        .OrderBy(sa => sa.FirstName)
                        .ThenBy(sa => sa.LastName)
                        .ToListAsync();

                    results.AddRange(users.Select(u => MapUserToDto(u, null)));
                    results.AddRange(superAdmins.Select(MapSuperAdminToDto));

                    totalCount = userCount + superAdmins.Count;

                    // Apply in-memory sorting and pagination for combined results
                    results = results
                        .OrderBy(r => r.FullName)
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();
                }

                return new PaginatedResult<UserWithRolesDto>
                {
                    Items = results,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all users for tenant {TenantId}", tenantId);
                return PaginatedResult<UserWithRolesDto>.Empty(pageNumber, pageSize);
            }
        }

        public async Task<PaginatedResult<UserWithRolesDto>> GetUsersByRoleAsync(Guid roleId, Guid? tenantId, int pageNumber, int pageSize)
        {
            try
            {
                // Single query to get user IDs with pagination
                var userRoleQuery = _context.UserRoles
                    .AsNoTracking()
                    .Where(ur => ur.RoleId == roleId);

                if (tenantId.HasValue)
                {
                    userRoleQuery = userRoleQuery.Where(ur =>
                        ur.TenantId == tenantId.Value || ur.TenantId == Guid.Empty);
                }

                var userRoles = await userRoleQuery
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .OrderBy(userId => userId) // Add ordering for consistent pagination
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var totalCount = await userRoleQuery
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .CountAsync();

                if (!userRoles.Any())
                {
                    return PaginatedResult<UserWithRolesDto>.Empty(pageNumber, pageSize);
                }

                // Load users with their roles in a single query
                var users = await _context.Users
                    .AsNoTracking()
                    .Where(u => userRoles.Contains(u.Id))
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                            .ThenInclude(r => r.RolePermissions)
                                .ThenInclude(rp => rp.Permission)
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .ToListAsync();

                var result = users.Select(u => MapUserToDto(u, tenantId)).ToList();

                return new PaginatedResult<UserWithRolesDto>
                {
                    Items = result,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users by role {RoleId}", roleId);
                return PaginatedResult<UserWithRolesDto>.Empty(pageNumber, pageSize);
            }
        }

        #endregion

        #region ================= ROLE ASSIGNMENT =================

        public async Task<RoleAssignmentResult> AssignRoleToUserAsync(Guid userId, Guid roleId, Guid? tenantId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var validationResult = await ValidateUserAndRoleAsync(userId, roleId, tenantId);
                    if (!validationResult.IsValid)
                    {
                        await transaction.RollbackAsync();
                        return RoleAssignmentResult.Failed(validationResult.ErrorMessage);
                    }

                    // Check if role already exists - skip if exists
                    if (await UserHasRoleAsync(userId, roleId, tenantId))
                    {
                        await transaction.CommitAsync();
                        var userWithRoles = await GetUserWithRolesAsync(userId, tenantId);
                        return RoleAssignmentResult.Successful(
                            "Role already assigned to user",
                            userWithRoles);
                    }

                    var userRole = new UserRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        RoleId = roleId,
                        TenantId = tenantId ?? Guid.Empty,
                        CreatedOn = DateTime.UtcNow,
                        UpdatedOn = DateTime.UtcNow
                    };

                    await _context.UserRoles.AddAsync(userRole);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Detach the entity after transaction commit to prevent duplicate key errors
                    _context.Entry(userRole).State = EntityState.Detached;

                    var userWithRolesResult = await GetUserWithRolesAsync(userId, tenantId);
                    return RoleAssignmentResult.Successful(
                        "Role assigned successfully",
                        userWithRolesResult);
                }
                catch (DbUpdateException dbEx)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(dbEx, "Database error assigning role {RoleId} to user {UserId}", roleId, userId);

                    // Check if it's a duplicate key error - skip if exists
                    if (dbEx.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx &&
                        (sqlEx.Number == 2601 || sqlEx.Number == 2627)) // Duplicate key errors
                    {
                        // Detach any tracked UserRole entities to prevent further errors
                        DetachUserRoleEntities(userId, roleId, tenantId ?? Guid.Empty);

                        var userWithRoles = await GetUserWithRolesAsync(userId, tenantId);
                        return RoleAssignmentResult.Successful(
                            "Role already assigned to user",
                            userWithRoles);
                    }

                    return RoleAssignmentResult.Failed("Database error while assigning role. Please check constraints.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", roleId, userId);
                    return RoleAssignmentResult.Failed("Failed to assign role");
                }
            });
        }

        public async Task<RoleAssignmentResult> AssignMultipleRolesToUserAsync(Guid userId, List<Guid> roleIds, Guid? tenantId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var tenantFilterId = tenantId ?? Guid.Empty;

                    // Validate all roles exist - execute sequentially
                    var invalidRoles = new List<string>();
                    foreach (var roleId in roleIds.Distinct())
                    {
                        var validationResult = await ValidateUserAndRoleAsync(userId, roleId, tenantId);
                        if (!validationResult.IsValid)
                        {
                            invalidRoles.Add($"{roleId}: {validationResult.ErrorMessage}");
                        }
                    }

                    if (invalidRoles.Any())
                    {
                        await transaction.RollbackAsync();
                        return RoleAssignmentResult.Failed($"Invalid roles: {string.Join("; ", invalidRoles)}");
                    }

                    // Get distinct role IDs
                    var distinctRoleIds = roleIds.Distinct().ToList();

                    // Get existing roles for this user
                    var existingRoles = await _context.UserRoles
                        .Where(ur => ur.UserId == userId && ur.TenantId == tenantFilterId)
                        .Select(ur => ur.RoleId)
                        .ToListAsync();

                    // Filter out roles that already exist - skip if exists
                    var newRoleIds = distinctRoleIds
                        .Where(roleId => !existingRoles.Contains(roleId))
                        .ToList();

                    if (!newRoleIds.Any())
                    {
                        await transaction.CommitAsync();
                        var userWithRoles = await GetUserWithRolesAsync(userId, tenantId);
                        return RoleAssignmentResult.Successful(
                            "All roles already assigned to user",
                            userWithRoles);
                    }

                    // Create new user roles
                    var userRoles = newRoleIds.Select(roleId => new UserRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        RoleId = roleId,
                        TenantId = tenantFilterId,
                        CreatedOn = DateTime.UtcNow,
                        UpdatedOn = DateTime.UtcNow
                    }).ToList();

                    await _context.UserRoles.AddRangeAsync(userRoles);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Detach all newly added entities after transaction commit
                    foreach (var userRole in userRoles)
                    {
                        _context.Entry(userRole).State = EntityState.Detached;
                    }

                    var userWithRolesResult = await GetUserWithRolesAsync(userId, tenantId);
                    return RoleAssignmentResult.Successful(
                        $"Assigned {userRoles.Count} new role(s) successfully",
                        userWithRolesResult);
                }
                catch (DbUpdateException dbEx)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(dbEx, "Database error assigning multiple roles to user {UserId}", userId);

                    // Check if it's a duplicate key error - skip if exists
                    if (dbEx.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx &&
                        (sqlEx.Number == 2601 || sqlEx.Number == 2627)) // Duplicate key errors
                    {
                        // Detach all UserRole entities for this user to prevent further errors
                        DetachAllUserRoleEntities(userId, tenantId ?? Guid.Empty);

                        var userWithRoles = await GetUserWithRolesAsync(userId, tenantId);
                        return RoleAssignmentResult.Successful(
                            "Some roles may already be assigned. Operation partially completed.",
                            userWithRoles);
                    }

                    return RoleAssignmentResult.Failed("Database error while assigning roles. Please check constraints.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error assigning multiple roles to user {UserId}", userId);
                    return RoleAssignmentResult.Failed("Failed to assign roles");
                }
            });
        }

        public async Task<RoleAssignmentResult> RemoveRoleFromUserAsync(Guid userId, Guid roleId, Guid? tenantId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var tenantFilterId = tenantId ?? Guid.Empty;

                    var userRole = await _context.UserRoles
                        .FirstOrDefaultAsync(ur =>
                            ur.UserId == userId &&
                            ur.RoleId == roleId &&
                            ur.TenantId == tenantFilterId);

                    if (userRole == null)
                    {
                        await transaction.CommitAsync();
                        // Role not found - skip removal
                        var userWithRoles = await GetUserWithRolesAsync(userId, tenantId);
                        return RoleAssignmentResult.Successful(
                            "Role not assigned to user",
                            userWithRoles);
                    }

                    _context.UserRoles.Remove(userRole);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Detach the entity after transaction commit
                    _context.Entry(userRole).State = EntityState.Detached;

                    var userWithRolesResult = await GetUserWithRolesAsync(userId, tenantId);
                    return RoleAssignmentResult.Successful(
                        "Role removed successfully",
                        userWithRolesResult);
                }
                catch (DbUpdateException dbEx)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(dbEx, "Database error removing role {RoleId} from user {UserId}", roleId, userId);
                    return RoleAssignmentResult.Failed("Database error while removing role.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error removing role {RoleId} from user {UserId}", roleId, userId);
                    return RoleAssignmentResult.Failed("Failed to remove role");
                }
            });
        }

        public async Task<RoleAssignmentResult> RemoveAllRolesFromUserAsync(Guid userId, Guid? tenantId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var tenantFilterId = tenantId ?? Guid.Empty;
                    var roles = await _context.UserRoles
                        .Where(ur => ur.UserId == userId && ur.TenantId == tenantFilterId)
                        .ToListAsync();

                    if (!roles.Any())
                    {
                        await transaction.CommitAsync();
                        return RoleAssignmentResult.Successful("No roles to remove");
                    }

                    _context.UserRoles.RemoveRange(roles);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Detach all removed entities after transaction commit
                    foreach (var role in roles)
                    {
                        _context.Entry(role).State = EntityState.Detached;
                    }

                    return RoleAssignmentResult.Successful($"Removed {roles.Count} role(s) successfully");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error removing all roles from user {UserId}", userId);
                    return RoleAssignmentResult.Failed("Failed to remove roles");
                }
            });
        }

        public async Task<RoleAssignmentResult> UpdateUserRolesAsync(Guid userId, List<Guid> roleIds, Guid? tenantId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var tenantFilterId = tenantId ?? Guid.Empty;

                    // Validate all roles exist
                    var invalidRoles = new List<string>();
                    foreach (var roleId in roleIds.Distinct())
                    {
                        var validationResult = await ValidateUserAndRoleAsync(userId, roleId, tenantId);
                        if (!validationResult.IsValid)
                        {
                            invalidRoles.Add($"{roleId}: {validationResult.ErrorMessage}");
                        }
                    }

                    if (invalidRoles.Any())
                    {
                        await transaction.RollbackAsync();
                        return RoleAssignmentResult.Failed($"Invalid roles: {string.Join("; ", invalidRoles)}");
                    }

                    // Get current roles
                    var currentRoles = await _context.UserRoles
                        .Where(ur => ur.UserId == userId && ur.TenantId == tenantFilterId)
                        .ToListAsync();

                    var currentRoleIds = currentRoles.Select(ur => ur.RoleId).ToList();
                    var newRoleIds = roleIds.Distinct().ToList();

                    // Determine roles to remove
                    var rolesToRemove = currentRoles
                        .Where(ur => !newRoleIds.Contains(ur.RoleId))
                        .ToList();

                    // Determine roles to add
                    var rolesToAdd = newRoleIds
                        .Where(roleId => !currentRoleIds.Contains(roleId))
                        .Select(roleId => new UserRole
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            RoleId = roleId,
                            TenantId = tenantFilterId,
                            CreatedOn = DateTime.UtcNow,
                            UpdatedOn = DateTime.UtcNow
                        })
                        .ToList();

                    // Execute changes
                    if (rolesToRemove.Any())
                    {
                        _context.UserRoles.RemoveRange(rolesToRemove);
                    }

                    if (rolesToAdd.Any())
                    {
                        await _context.UserRoles.AddRangeAsync(rolesToAdd);
                    }

                    if (rolesToRemove.Any() || rolesToAdd.Any())
                    {
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        // Detach all entities AFTER transaction commit to prevent duplicate key errors
                        // when other services (like UserActivityService) call SaveChangesAsync
                        DetachAllUserRoleEntities(userId, tenantFilterId);
                    }
                    else
                    {
                        await transaction.CommitAsync();
                    }

                    var userWithRoles = await GetUserWithRolesAsync(userId, tenantId);

                    if (!rolesToRemove.Any() && !rolesToAdd.Any())
                    {
                        return RoleAssignmentResult.Successful(
                            "No changes needed - user already has the specified roles",
                            userWithRoles);
                    }

                    var changes = new List<string>();
                    if (rolesToRemove.Any()) changes.Add($"removed {rolesToRemove.Count}");
                    if (rolesToAdd.Any()) changes.Add($"added {rolesToAdd.Count}");

                    return RoleAssignmentResult.Successful(
                        $"Roles updated successfully ({string.Join(", ", changes)})",
                        userWithRoles);
                }
                catch (DbUpdateException dbEx)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(dbEx, "Database error updating roles for user {UserId}", userId);

                    // Check if it's a duplicate key error
                    if (dbEx.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx &&
                        (sqlEx.Number == 2601 || sqlEx.Number == 2627))
                    {
                        // Detach all UserRole entities to prevent further errors
                        DetachAllUserRoleEntities(userId, tenantId ?? Guid.Empty);

                        var userWithRoles = await GetUserWithRolesAsync(userId, tenantId);
                        return RoleAssignmentResult.Successful(
                            "Some roles may already be assigned. Operation partially completed.",
                            userWithRoles);
                    }

                    return RoleAssignmentResult.Failed("Database error while updating roles.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error updating roles for user {UserId}", userId);
                    return RoleAssignmentResult.Failed("Failed to update user roles");
                }
            });
        }

        #endregion

        #region ================= UTILITIES =================

        public async Task<bool> UserHasRoleAsync(Guid userId, Guid roleId, Guid? tenantId)
        {
            try
            {
                var tenantFilterId = tenantId ?? Guid.Empty;
                var exists = await _context.UserRoles
                    .AsNoTracking() // Use AsNoTracking to avoid tracking issues
                    .AnyAsync(ur =>
                        ur.UserId == userId &&
                        ur.RoleId == roleId &&
                        ur.TenantId == tenantFilterId);

                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {UserId} has role {RoleId}", userId, roleId);
                return false;
            }
        }

        private async Task<ValidationResult> ValidateUserAndRoleAsync(Guid userId, Guid roleId, Guid? tenantId)
        {
            try
            {
                // Execute queries sequentially to avoid concurrency
                bool userExists;

                if (tenantId.HasValue)
                {
                    userExists = await _context.Users
                        .AsNoTracking()
                        .AnyAsync(u => u.Id == userId && u.TenantId == tenantId.Value);
                }
                else
                {
                    // Check regular users first
                    userExists = await _context.Users
                        .AsNoTracking()
                        .AnyAsync(u => u.Id == userId);

                    // If not found, check SuperAdmins
                    if (!userExists)
                    {
                        userExists = await _context.SuperAdmins
                            .AsNoTracking()
                            .AnyAsync(sa => sa.Id == userId && sa.IsActive);
                    }
                }

                // Check role exists - FIXED VERSION
                // When tenantId is provided, we should check:
                // 1. Roles that belong to that specific tenant
                // 2. Global/system roles (TenantId = Guid.Empty)
                // 3. If the role exists at all (for better error messages)
                var roleExistsQuery = _context.Roles
                    .AsNoTracking()
                    .Where(r => r.Id == roleId);

                bool roleExists;

                if (tenantId.HasValue)
                {
                    // Check if role exists for this tenant OR is a global role
                    roleExists = await roleExistsQuery
                        .AnyAsync(r => r.TenantId == tenantId.Value || r.TenantId == Guid.Empty);

                    // If not found with tenant filter, check if role exists at all (for better error message)
                    if (!roleExists)
                    {
                        var roleExistsAnywhere = await roleExistsQuery.AnyAsync();
                        if (roleExistsAnywhere)
                        {
                            return ValidationResult.Failed($"Role with ID {roleId} exists but is not available for your tenant");
                        }
                    }
                }
                else
                {
                    // No tenant filter - check if role exists at all
                    roleExists = await roleExistsQuery.AnyAsync();
                }

                if (!userExists)
                    return ValidationResult.Failed($"User with ID {userId} not found");

                if (!roleExists)
                    return ValidationResult.Failed($"Role with ID {roleId} not found");

                return ValidationResult.Valid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user {UserId} and role {RoleId}", userId, roleId);
                return ValidationResult.Failed("Validation error occurred");
            }
        }

        private UserWithRolesDto MapUserToDto(User user, Guid? tenantId)
        {
            try
            {
                var userRoles = user.UserRoles?
                    .Where(ur => ur.Role != null)
                    .ToList() ?? new List<UserRole>();

                // Filter by tenant if specified
                if (tenantId.HasValue)
                {
                    userRoles = userRoles
                        .Where(ur => ur.TenantId == tenantId.Value || ur.TenantId == Guid.Empty)
                        .ToList();
                }

                var roles = userRoles
                    .Select(ur => new UserRoleDto
                    {
                        RoleId = ur.Role!.Id,
                        RoleName = ur.Role.Name,
                        Description = ur.Role.Description,
                        IsSystemRole = ur.Role.IsSystemRole,
                        PermissionCount = ur.Role.RolePermissions?.Count ?? 0
                    })
                    .ToList();

                var permissions = userRoles
                    .SelectMany(ur => ur.Role?.RolePermissions?
                        .Where(rp => rp.Permission != null)
                        .Select(rp => rp.Permission!.Key) ?? Enumerable.Empty<string>())
                    .Distinct()
                    .ToList();

                return new UserWithRolesDto
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    UserName = user.FullName,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    TenantId = user.TenantId,
                    RequirePasswordChange = user.RequirePasswordChange,
                    IsSuperAdmin = false,
                    Roles = roles,
                    Permissions = permissions
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping user {UserId} to DTO", user?.Id);
                return new UserWithRolesDto
                {
                    UserId = user?.Id ?? Guid.Empty,
                    Email = user?.Email ?? string.Empty,
                    FullName = user?.FullName ?? string.Empty,
                    UserName = user?.FullName ?? string.Empty,
                    IsSuperAdmin = false,
                    Roles = new List<UserRoleDto>(),
                    Permissions = new List<string>()
                };
            }
        }

        private UserWithRolesDto MapSuperAdminToDto(Domain.Entities.Identity.SuperAdmin superAdmin)
        {
            return new UserWithRolesDto
            {
                UserId = superAdmin.Id,
                Email = superAdmin.Email,
                FullName = $"{superAdmin.FirstName} {superAdmin.LastName}",
                UserName = $"{superAdmin.FirstName} {superAdmin.LastName}",
                FirstName = superAdmin.FirstName,
                LastName = superAdmin.LastName,
                TenantId = null,
                RequirePasswordChange = false,
                IsSuperAdmin = true,
                Roles = new List<UserRoleDto>(), // SuperAdmins don't have roles in the same way
                Permissions = new List<string> { "*" } // SuperAdmins have all permissions
            };
        }

        #endregion

        #region ================= ENTITY DETACHING HELPER METHODS =================

        /// <summary>
        /// Detaches a specific UserRole entity from the DbContext change tracker
        /// </summary>
        private void DetachUserRoleEntities(Guid userId, Guid roleId, Guid tenantId)
        {
            try
            {
                var trackedUserRoles = _context.ChangeTracker.Entries<UserRole>()
                    .Where(e => e.Entity.UserId == userId &&
                           e.Entity.RoleId == roleId &&
                           e.Entity.TenantId == tenantId)
                    .ToList();

                foreach (var entry in trackedUserRoles)
                {
                    entry.State = EntityState.Detached;
                }

                if (trackedUserRoles.Count > 0)
                {
                    _logger.LogDebug("Detached {Count} UserRole entries for user {UserId}, role {RoleId}",
                        trackedUserRoles.Count, userId, roleId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error detaching UserRole entity for user {UserId}, role {RoleId}",
                    userId, roleId);
            }
        }

        /// <summary>
        /// Detaches all UserRole entities for a specific user from the DbContext change tracker
        /// </summary>
        private void DetachAllUserRoleEntities(Guid userId, Guid tenantId)
        {
            try
            {
                var trackedUserRoles = _context.ChangeTracker.Entries<UserRole>()
                    .Where(e => e.Entity.UserId == userId && e.Entity.TenantId == tenantId)
                    .ToList();

                foreach (var entry in trackedUserRoles)
                {
                    entry.State = EntityState.Detached;
                }

                if (trackedUserRoles.Count > 0)
                {
                    _logger.LogDebug("Detached {Count} UserRole entries for user {UserId}",
                        trackedUserRoles.Count, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error detaching UserRole entities for user {UserId}", userId);
            }
        }

        /// <summary>
        /// Detaches all UserRole entities from the DbContext change tracker (use with caution)
        /// </summary>
        private void DetachAllUserRoleEntities()
        {
            try
            {
                var trackedUserRoles = _context.ChangeTracker.Entries<UserRole>().ToList();

                foreach (var entry in trackedUserRoles)
                {
                    entry.State = EntityState.Detached;
                }

                if (trackedUserRoles.Count > 0)
                {
                    _logger.LogDebug("Detached all {Count} UserRole entries from change tracker",
                        trackedUserRoles.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error detaching all UserRole entities");
            }
        }

        #endregion

        #region ================= HELPER CLASSES =================

        private class ValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;

            public static ValidationResult Valid() => new ValidationResult { IsValid = true };
            public static ValidationResult Failed(string message) => new ValidationResult { IsValid = false, ErrorMessage = message };
        }

        #endregion
    }
}