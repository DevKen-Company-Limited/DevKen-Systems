using Devken.CBC.SchoolManagement.Application.Common;
using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Services.UserManagement;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.UserManagement
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IRepositoryManager _repository;
        private readonly IPasswordHashingService _passwordHashingService;

        public UserManagementService(
            IRepositoryManager repository,
            IPasswordHashingService passwordHashingService)
        {
            _repository = repository;
            _passwordHashingService = passwordHashingService;
        }

        public async Task<ServiceResult<UserDto>> CreateUserAsync(
            CreateUserRequest request,
            Guid schoolId,
            Guid createdBy)
        {
            try
            {
                // Verify school exists
                var school = await _repository.School.GetByIdAsync(schoolId);
                if (school == null)
                    return ServiceResult<UserDto>.FailureResult("School not found");

                // Check if email already exists in this school
                var existingUser = await _repository.User.FindByCondition(
                    u => u.Email == request.Email && u.TenantId == schoolId,
                    trackChanges: false)
                    .FirstOrDefaultAsync();

                if (existingUser != null)
                    return ServiceResult<UserDto>.FailureResult("Email already exists in this school");

                // Generate temporary password if not provided
                var tempPassword = request.TemporaryPassword ?? GenerateTemporaryPassword();

                // Create user
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    TenantId = schoolId,
                    PasswordHash = _passwordHashingService.HashPassword(tempPassword),
                    IsActive = true,
                    IsEmailVerified = false,
                    RequirePasswordChange = request.RequirePasswordChange,
                    FailedLoginAttempts = 0
                };

                _repository.User.Create(user);
                await _repository.SaveAsync();

                // Assign roles if provided
                if (request.RoleIds != null && request.RoleIds.Count > 0)
                {
                    await AssignRolesInternalAsync(user.Id, request.RoleIds, schoolId);
                    await _repository.SaveAsync();
                }

                return ServiceResult<UserDto>.SuccessResult(MapToUserDto(user, tempPassword));
            }
            catch (Exception ex)
            {
                return ServiceResult<UserDto>.FailureResult($"Error creating user: {ex.Message}");
            }
        }

        public async Task<ServiceResult<PaginatedUsersResponse>> GetUsersAsync(
            Guid? schoolId,
            int page,
            int pageSize,
            string? search,
            bool? isActive)
        {
            try
            {
                var query = _repository.User.FindAll(trackChanges: false);

                // Filter by school if specified
                if (schoolId.HasValue)
                    query = query.Where(u => u.TenantId == schoolId.Value);

                // Filter by active status
                if (isActive.HasValue)
                    query = query.Where(u => u.IsActive == isActive.Value);

                // Search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(u =>
                        u.Email.ToLower().Contains(searchLower, StringComparison.OrdinalIgnoreCase) ||
                        (u.FirstName != null && u.FirstName.ToLower().Contains(searchLower, StringComparison.OrdinalIgnoreCase)) ||
                        (u.LastName != null && u.LastName.ToLower().Contains(searchLower, StringComparison.OrdinalIgnoreCase)));
                }

                var totalCount = await query.CountAsync();

                var users = await query
                    .Include(u => u.Tenant)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                            .ThenInclude(r => r.RolePermissions)
                                .ThenInclude(rp => rp.Permission)
                    .OrderByDescending(u => u.CreatedOn)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var userDtos = users.Select(user => MapToUserDto(user)).ToList();

                var result = new PaginatedUsersResponse
                {
                    Users = userDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                return ServiceResult<PaginatedUsersResponse>.SuccessResult(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PaginatedUsersResponse>.FailureResult($"Error retrieving users: {ex.Message}");
            }
        }

        public async Task<ServiceResult<UserDto>> GetUserByIdAsync(Guid userId)
        {
            try
            {
                var user = await _repository.User.FindByCondition(
                    u => u.Id == userId,
                    trackChanges: false)
                    .Include(u => u.Tenant)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                            .ThenInclude(r => r.RolePermissions)
                                .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync();

                if (user == null)
                    return ServiceResult<UserDto>.FailureResult("User not found");

                var userDto = MapToUserDto(user);
                return ServiceResult<UserDto>.SuccessResult(userDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserDto>.FailureResult($"Error retrieving user: {ex.Message}");
            }
        }

        public async Task<ServiceResult<UserDto>> UpdateUserAsync(
            Guid userId,
            UpdateUserRequest request,
            Guid updatedBy)
        {
            try
            {
                var user = await _repository.User.GetByIdAsync(userId, trackChanges: true);
                if (user == null)
                    return ServiceResult<UserDto>.FailureResult("User not found");

                // Update only provided fields
                if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
                {
                    // Check if new email already exists in the same school
                    var emailExists = await _repository.User.FindByCondition(
                        u => u.Email == request.Email && u.TenantId == user.TenantId && u.Id != userId,
                        trackChanges: false)
                        .AnyAsync();

                    if (emailExists)
                        return ServiceResult<UserDto>.FailureResult("Email already exists in this school");

                    user.Email = request.Email;
                    user.IsEmailVerified = false;
                }

                if (!string.IsNullOrWhiteSpace(request.FirstName))
                    user.FirstName = request.FirstName;

                if (!string.IsNullOrWhiteSpace(request.LastName))
                    user.LastName = request.LastName;

                if (request.PhoneNumber != null)
                    user.PhoneNumber = request.PhoneNumber;

                if (request.ProfileImageUrl != null)
                    user.ProfileImageUrl = request.ProfileImageUrl;

                if (request.IsActive.HasValue)
                    user.IsActive = request.IsActive.Value;

                _repository.User.Update(user);
                await _repository.SaveAsync();

                return await GetUserByIdAsync(userId);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserDto>.FailureResult($"Error updating user: {ex.Message}");
            }
        }

        public async Task<ServiceResult<UserDto>> AssignRolesToUserAsync(
            Guid userId,
            List<string> roleIds,
            Guid assignedBy)
        {
            try
            {
                var user = await _repository.User.GetByIdAsync(userId, trackChanges: false);
                if (user == null)
                    return ServiceResult<UserDto>.FailureResult("User not found");

                var roleGuids = roleIds.Select(r => Guid.Parse(r)).ToList();
                await AssignRolesInternalAsync(userId, roleGuids, user.TenantId);
                await _repository.SaveAsync();

                return await GetUserByIdAsync(userId);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserDto>.FailureResult($"Error assigning roles: {ex.Message}");
            }
        }

        public async Task<ServiceResult<UserDto>> UpdateUserRolesAsync(
            Guid userId,
            List<string> roleIds,
            Guid updatedBy)
        {
            try
            {
                var user = await _repository.User.GetByIdAsync(userId, trackChanges: false);
                if (user == null)
                    return ServiceResult<UserDto>.FailureResult("User not found");

                // Remove all existing roles first
                var existingRoles = await _repository.UserRole.FindByCondition(
                    ur => ur.UserId == userId,
                    trackChanges: true)
                    .ToListAsync();

                foreach (var existingRole in existingRoles)
                {
                    _repository.UserRole.Delete(existingRole);
                }

                // Add new roles
                var roleGuids = roleIds.Select(r => Guid.Parse(r)).ToList();
                foreach (var roleId in roleGuids)
                {
                    var role = await _repository.Role.GetByIdAsync(roleId, trackChanges: false);
                    if (role == null || role.TenantId != user.TenantId)
                        return ServiceResult<UserDto>.FailureResult($"Role {roleId} not found or doesn't belong to this school");

                    var userRole = new UserRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        RoleId = roleId
                    };
                    _repository.UserRole.Create(userRole);
                }

                await _repository.SaveAsync();

                return await GetUserByIdAsync(userId);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserDto>.FailureResult($"Error updating roles: {ex.Message}");
            }
        }

        public async Task<ServiceResult<UserDto>> RemoveRoleFromUserAsync(
            Guid userId,
            string roleId,
            Guid removedBy)
        {
            try
            {
                var roleGuid = Guid.Parse(roleId);
                var userRole = await _repository.UserRole.FindByCondition(
                    ur => ur.UserId == userId && ur.RoleId == roleGuid,
                    trackChanges: true)
                    .FirstOrDefaultAsync();

                if (userRole == null)
                    return ServiceResult<UserDto>.FailureResult("User role not found");

                _repository.UserRole.Delete(userRole);
                await _repository.SaveAsync();

                return await GetUserByIdAsync(userId);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserDto>.FailureResult($"Error removing role: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> ActivateUserAsync(Guid userId, Guid activatedBy)
        {
            try
            {
                var user = await _repository.User.GetByIdAsync(userId, trackChanges: true);
                if (user == null)
                    return ServiceResult<bool>.FailureResult("User not found");

                user.IsActive = true;
                user.LockedUntil = null;
                user.FailedLoginAttempts = 0;

                _repository.User.Update(user);
                await _repository.SaveAsync();

                return ServiceResult<bool>.SuccessResult(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.FailureResult($"Error activating user: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> DeactivateUserAsync(Guid userId, Guid deactivatedBy)
        {
            try
            {
                var user = await _repository.User.GetByIdAsync(userId, trackChanges: true);
                if (user == null)
                    return ServiceResult<bool>.FailureResult("User not found");

                user.IsActive = false;

                _repository.User.Update(user);
                await _repository.SaveAsync();

                return ServiceResult<bool>.SuccessResult(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.FailureResult($"Error deactivating user: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> DeleteUserAsync(Guid userId, Guid deletedBy)
        {
            try
            {
                var user = await _repository.User.GetByIdAsync(userId, trackChanges: true);
                if (user == null)
                    return ServiceResult<bool>.FailureResult("User not found");

                // Soft delete
                user.Status = EntityStatus.Deleted;
                user.IsActive = false;

                _repository.User.Update(user);
                await _repository.SaveAsync();

                return ServiceResult<bool>.SuccessResult(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.FailureResult($"Error deleting user: {ex.Message}");
            }
        }

        public async Task<ServiceResult<PasswordResetResultDto>> ResetPasswordAsync(Guid userId, Guid resetBy)
        {
            try
            {
                var user = await _repository.User.FindByCondition(
                    u => u.Id == userId,
                    trackChanges: true)
                    .Include(u => u.Tenant)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                            .ThenInclude(r => r.RolePermissions)
                                .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync();

                if (user == null)
                    return ServiceResult<PasswordResetResultDto>.FailureResult("User not found");

                // Generate temporary password
                var tempPassword = GenerateTemporaryPassword();

                // Update user password
                user.PasswordHash = _passwordHashingService.HashPassword(tempPassword);
                user.RequirePasswordChange = true;
                user.FailedLoginAttempts = 0;
                user.LockedUntil = null;

                _repository.User.Update(user);
                await _repository.SaveAsync();

                // Create result with user info and temp password
                var result = new PasswordResetResultDto
                {
                    User = MapToUserDto(user),
                    TemporaryPassword = tempPassword,
                    Message = "Password has been reset. User must change password on next login.",
                    ResetAt = DateTime.UtcNow,
                    ResetBy = resetBy
                };

                // TODO: Send email with temp password
                return ServiceResult<PasswordResetResultDto>.SuccessResult(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PasswordResetResultDto>.FailureResult($"Error resetting password: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> ResendWelcomeEmailAsync(Guid userId)
        {
            try
            {
                var user = await _repository.User.GetByIdAsync(userId, trackChanges: false);
                if (user == null)
                    return ServiceResult<bool>.FailureResult("User not found");

                // TODO: Implement email sending logic
                return ServiceResult<bool>.SuccessResult(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.FailureResult($"Error resending welcome email: {ex.Message}");
            }
        }

        #region Private Helper Methods

        private async Task AssignRolesInternalAsync(Guid userId, List<Guid> roleIds, Guid tenantId)
        {
            // Verify all roles exist and belong to the same tenant
            foreach (var roleId in roleIds)
            {
                var role = await _repository.Role.GetByIdAsync(roleId, trackChanges: false);
                if (role == null || role.TenantId != tenantId)
                    throw new InvalidOperationException($"Role {roleId} not found or doesn't belong to this school");
            }

            // Add new roles
            foreach (var roleId in roleIds)
            {
                var userRole = new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    RoleId = roleId
                };
                _repository.UserRole.Create(userRole);
            }
        }

        private static UserDto MapToUserDto(User user, string? tempPassword = null)
        {
            // Get role names safely with null checks
            var roleNames = user.UserRoles?
                .Where(ur => ur.Role != null)
                .Select(ur => ur.Role!.Name ?? "Unknown")
                .ToList() ?? new List<string>();

            // Get permissions from all roles
            var permissions = user.UserRoles?
                .Where(ur => ur.Role?.RolePermissions != null)
                .SelectMany(ur => ur.Role!.RolePermissions!)
                .Where(rp => rp.Permission != null)
                .Select(rp => rp.Permission!.Key ?? string.Empty)
                .Distinct()
                .Where(key => !string.IsNullOrEmpty(key))
                .ToList() ?? new List<string>();

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.ProfileImageUrl,
                SchoolId = user.TenantId,
                SchoolName = user.Tenant?.Name,
                IsActive = user.IsActive,
                IsEmailVerified = user.IsEmailVerified,
                RequirePasswordChange = user.RequirePasswordChange,
                TemporaryPassword = tempPassword,
                RoleNames = roleNames,
                Permissions = permissions,
                CreatedOn = user.CreatedOn,
                UpdatedOn = user.UpdatedOn,
                TenantId = user.TenantId
            };
        }

        private static string GenerateTemporaryPassword()
        {
            const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
            var random = new Random();
            var chars = new char[12];

            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = validChars[random.Next(validChars.Length)];
            }

            return new string(chars);
        }

        #endregion
    }
}