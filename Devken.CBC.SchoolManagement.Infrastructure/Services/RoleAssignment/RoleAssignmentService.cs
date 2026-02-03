using Devken.CBC.SchoolManagement.Application.DTOs.RoleAssignment;
using Devken.CBC.SchoolManagement.Application.Service.IRolesAssignment;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.RoleAssignment
{
    public class RoleAssignmentService : IRoleAssignmentService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RoleAssignmentService> _logger;

        public RoleAssignmentService(
            AppDbContext context,
            ILogger<RoleAssignmentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<RoleAssignmentResult> AssignRoleToUserAsync(Guid userId, Guid roleId, Guid tenantId)
        {
            try
            {
                // Validate user exists and belongs to tenant
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);

                if (user == null)
                {
                    return new RoleAssignmentResult
                    {
                        Success = false,
                        Message = "User not found or does not belong to this tenant"
                    };
                }

                // Validate role exists and belongs to tenant (or is a system role)
                var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Id == roleId &&
                        (r.TenantId == tenantId || r.TenantId == null));

                if (role == null)
                {
                    return new RoleAssignmentResult
                    {
                        Success = false,
                        Message = "Role not found or is not available for this tenant"
                    };
                }

                // Check if user already has this role
                var existingUserRole = await _context.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

                if (existingUserRole != null)
                {
                    return new RoleAssignmentResult
                    {
                        Success = false,
                        Message = "User already has this role assigned"
                    };
                }

                // Create new user role assignment
                var userRole = new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    RoleId = roleId,
                    TenantId = tenantId,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                };

                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Role {RoleId} ({RoleName}) assigned to user {UserId} in tenant {TenantId}",
                    roleId, role.Name, userId, tenantId);

                // Get updated user with roles
                var userWithRoles = await GetUserWithRolesAsync(userId, tenantId);

                return new RoleAssignmentResult
                {
                    Success = true,
                    Message = $"Role '{role.Name}' successfully assigned to user",
                    User = userWithRoles
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", roleId, userId);
                return new RoleAssignmentResult
                {
                    Success = false,
                    Message = "An error occurred while assigning the role"
                };
            }
        }

        public async Task<RoleAssignmentResult> AssignMultipleRolesToUserAsync(Guid userId, List<Guid> roleIds, Guid tenantId)
        {
            try
            {
                if (roleIds == null || !roleIds.Any())
                {
                    return new RoleAssignmentResult
                    {
                        Success = false,
                        Message = "No roles specified"
                    };
                }

                // Validate user
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);

                if (user == null)
                {
                    return new RoleAssignmentResult
                    {
                        Success = false,
                        Message = "User not found or does not belong to this tenant"
                    };
                }

                // Get valid roles
                var roles = await _context.Roles
                    .Where(r => roleIds.Contains(r.Id) &&
                        (r.TenantId == tenantId || r.TenantId == null))
                    .ToListAsync();

                if (!roles.Any())
                {
                    return new RoleAssignmentResult
                    {
                        Success = false,
                        Message = "No valid roles found"
                    };
                }

                // Get existing user roles
                var existingUserRoles = await _context.UserRoles
                    .Where(ur => ur.UserId == userId && roleIds.Contains(ur.RoleId))
                    .Select(ur => ur.RoleId)
                    .ToListAsync();

                // Filter out roles already assigned
                var newRoleIds = roleIds.Except(existingUserRoles).ToList();

                if (!newRoleIds.Any())
                {
                    return new RoleAssignmentResult
                    {
                        Success = false,
                        Message = "All specified roles are already assigned to this user"
                    };
                }

                // Create new user role assignments
                var newUserRoles = newRoleIds.Select(roleId => new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    RoleId = roleId,
                    TenantId = tenantId,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                }).ToList();

                _context.UserRoles.AddRange(newUserRoles);
                await _context.SaveChangesAsync();

                var assignedRoleNames = roles
                    .Where(r => newRoleIds.Contains(r.Id))
                    .Select(r => r.Name)
                    .ToList();

                _logger.LogInformation(
                    "Roles [{Roles}] assigned to user {UserId} in tenant {TenantId}",
                    string.Join(", ", assignedRoleNames), userId, tenantId);

                var userWithRoles = await GetUserWithRolesAsync(userId, tenantId);

                return new RoleAssignmentResult
                {
                    Success = true,
                    Message = $"{newUserRoles.Count} role(s) successfully assigned to user",
                    User = userWithRoles
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning multiple roles to user {UserId}", userId);
                return new RoleAssignmentResult
                {
                    Success = false,
                    Message = "An error occurred while assigning roles"
                };
            }
        }

        public async Task<RoleAssignmentResult> RemoveRoleFromUserAsync(Guid userId, Guid roleId, Guid tenantId)
        {
            try
            {
                var userRole = await _context.UserRoles
                    .Include(ur => ur.Role)
                    .FirstOrDefaultAsync(ur => ur.UserId == userId &&
                        ur.RoleId == roleId &&
                        ur.TenantId == tenantId);

                if (userRole == null)
                {
                    return new RoleAssignmentResult
                    {
                        Success = false,
                        Message = "User does not have this role assigned"
                    };
                }

                // Prevent removing the last role if business rules require at least one role
                var userRoleCount = await _context.UserRoles
                    .CountAsync(ur => ur.UserId == userId && ur.TenantId == tenantId);

                // Uncomment the below if you want to enforce at least one role per user
                /*
                if (userRoleCount == 1)
                {
                    return new RoleAssignmentResult
                    {
                        Success = false,
                        Message = "Cannot remove the last role from a user. Users must have at least one role."
                    };
                }
                */

                var roleName = userRole.Role?.Name ?? "Unknown";

                _context.UserRoles.Remove(userRole);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Role {RoleId} ({RoleName}) removed from user {UserId} in tenant {TenantId}",
                    roleId, roleName, userId, tenantId);

                var userWithRoles = await GetUserWithRolesAsync(userId, tenantId);

                return new RoleAssignmentResult
                {
                    Success = true,
                    Message = $"Role '{roleName}' successfully removed from user",
                    User = userWithRoles
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role {RoleId} from user {UserId}", roleId, userId);
                return new RoleAssignmentResult
                {
                    Success = false,
                    Message = "An error occurred while removing the role"
                };
            }
        }

        public async Task<RoleAssignmentResult> UpdateUserRolesAsync(Guid userId, List<Guid> roleIds, Guid tenantId)
        {
            try
            {
                // Validate user
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);

                if (user == null)
                {
                    return new RoleAssignmentResult
                    {
                        Success = false,
                        Message = "User not found or does not belong to this tenant"
                    };
                }

                // Validate roles
                var validRoles = await _context.Roles
                    .Where(r => roleIds.Contains(r.Id) &&
                        (r.TenantId == tenantId || r.TenantId == null))
                    .ToListAsync();

                if (roleIds.Any() && !validRoles.Any())
                {
                    return new RoleAssignmentResult
                    {
                        Success = false,
                        Message = "No valid roles found"
                    };
                }

                // Get existing user roles
                var existingUserRoles = await _context.UserRoles
                    .Where(ur => ur.UserId == userId && ur.TenantId == tenantId)
                    .ToListAsync();

                // Remove all existing roles
                _context.UserRoles.RemoveRange(existingUserRoles);

                // Add new roles
                if (roleIds.Any())
                {
                    var newUserRoles = roleIds.Select(roleId => new UserRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        RoleId = roleId,
                        TenantId = tenantId,
                        CreatedOn = DateTime.UtcNow,
                        UpdatedOn = DateTime.UtcNow
                    }).ToList();

                    _context.UserRoles.AddRange(newUserRoles);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Roles updated for user {UserId} in tenant {TenantId}. New role count: {Count}",
                    userId, tenantId, roleIds.Count);

                var userWithRoles = await GetUserWithRolesAsync(userId, tenantId);

                return new RoleAssignmentResult
                {
                    Success = true,
                    Message = "User roles successfully updated",
                    User = userWithRoles
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating roles for user {UserId}", userId);
                return new RoleAssignmentResult
                {
                    Success = false,
                    Message = "An error occurred while updating user roles"
                };
            }
        }

        public async Task<UserWithRolesResponse?> GetUserWithRolesAsync(Guid userId, Guid tenantId)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Id == userId && u.TenantId == tenantId)
                    .Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.FullName,
                        u.TenantId,
                        Roles = u.UserRoles
                            .Where(ur => ur.TenantId == tenantId)
                            .Select(ur => new
                            {
                                ur.Role!.Id,
                                ur.Role.Name,
                                ur.Role.Description,
                                ur.Role.IsSystemRole,
                                PermissionCount = ur.Role.RolePermissions.Count
                            })
                            .ToList(),
                        Permissions = u.UserRoles
                            .Where(ur => ur.TenantId == tenantId)
                            .SelectMany(ur => ur.Role!.RolePermissions.Select(rp => rp.Permission!.Key))
                            .Distinct()
                            .ToList()
                    })
                    .FirstOrDefaultAsync();

                if (user == null) return null;

                return new UserWithRolesResponse
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    TenantId = user.TenantId,
                    Roles = user.Roles.Select(r => new RoleInfoDto
                    {
                        RoleId = r.Id,
                        RoleName = r.Name,
                        Description = r.Description,
                        IsSystemRole = r.IsSystemRole,
                        PermissionCount = r.PermissionCount
                    }).ToList(),
                    Permissions = user.Permissions
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId} with roles", userId);
                return null;
            }
        }

        public async Task<UsersInRoleResponse> GetUsersByRoleAsync(Guid roleId, Guid tenantId, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Id == roleId &&
                        (r.TenantId == tenantId || r.TenantId == null));

                if (role == null)
                {
                    return new UsersInRoleResponse
                    {
                        RoleId = roleId,
                        RoleName = "Unknown",
                        Users = new List<UserBasicInfo>(),
                        TotalCount = 0,
                        PageNumber = pageNumber,
                        PageSize = pageSize
                    };
                }

                var query = _context.UserRoles
                    .Where(ur => ur.RoleId == roleId && ur.TenantId == tenantId)
                    .Include(ur => ur.User);

                var totalCount = await query.CountAsync();

                var users = await query
                    .OrderBy(ur => ur.User!.FullName)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(ur => new UserBasicInfo
                    {
                        UserId = ur.UserId,
                        Email = ur.User!.Email,
                        FullName = ur.User.FullName,
                        IsActive = ur.User.IsActive,
                        AssignedAt = ur.CreatedOn
                    })
                    .ToListAsync();

                return new UsersInRoleResponse
                {
                    RoleId = roleId,
                    RoleName = role.Name,
                    Users = users,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users for role {RoleId}", roleId);
                return new UsersInRoleResponse
                {
                    RoleId = roleId,
                    RoleName = "Error",
                    Users = new List<UserBasicInfo>(),
                    TotalCount = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }
        }

        public async Task<bool> UserHasRoleAsync(Guid userId, Guid roleId, Guid tenantId)
        {
            return await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId &&
                    ur.RoleId == roleId &&
                    ur.TenantId == tenantId);
        }

        public async Task<bool> UserHasAnyRoleAsync(Guid userId, List<Guid> roleIds, Guid tenantId)
        {
            return await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId &&
                    roleIds.Contains(ur.RoleId) &&
                    ur.TenantId == tenantId);
        }

        public async Task<List<RoleInfoDto>> GetAvailableRolesAsync(Guid tenantId)
        {
            try
            {
                return await _context.Roles
                    .Where(r => r.TenantId == tenantId || r.TenantId == null)
                    .OrderBy(r => r.Name)
                    .Select(r => new RoleInfoDto
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
                _logger.LogError(ex, "Error getting available roles for tenant {TenantId}", tenantId);
                return new List<RoleInfoDto>();
            }
        }

        public async Task<RoleAssignmentResult> RemoveAllRolesFromUserAsync(Guid userId, Guid tenantId)
        {
            try
            {
                var userRoles = await _context.UserRoles
                    .Where(ur => ur.UserId == userId && ur.TenantId == tenantId)
                    .ToListAsync();

                if (!userRoles.Any())
                {
                    return new RoleAssignmentResult
                    {
                        Success = false,
                        Message = "User has no roles assigned"
                    };
                }

                _context.UserRoles.RemoveRange(userRoles);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "All roles removed from user {UserId} in tenant {TenantId}",
                    userId, tenantId);

                return new RoleAssignmentResult
                {
                    Success = true,
                    Message = "All roles successfully removed from user"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing all roles from user {UserId}", userId);
                return new RoleAssignmentResult
                {
                    Success = false,
                    Message = "An error occurred while removing roles"
                };
            }
        }
    }
}
