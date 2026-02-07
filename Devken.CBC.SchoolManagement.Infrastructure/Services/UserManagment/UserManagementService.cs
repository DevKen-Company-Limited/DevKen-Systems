using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.UserManagment;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.UserManagment
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IRepositoryManager _repository;

        public UserManagementService(IRepositoryManager repository)
        {
            _repository = repository;
        }

        public async Task<ServiceResult<CreateUserResponseDto>> CreateUserAsync(
            CreateUserRequest request,
            Guid schoolId,
            Guid createdByUserId)
        {
            try
            {
                // Verify school exists
                var school = await _repository.School.GetByIdAsync(schoolId);
                if (school == null)
                    return ServiceResult<CreateUserResponseDto>.FailureResult("School not found");

                // Check if email already exists in this school
                var existingUser = await _repository.User.FindByCondition(
                    u => u.Email == request.Email && u.TenantId == schoolId,
                    trackChanges: false)
                    .FirstOrDefaultAsync();

                if (existingUser != null)
                    return ServiceResult<CreateUserResponseDto>.FailureResult("Email already exists in this school");

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
                    PasswordHash = HashPassword(tempPassword),
                    IsActive = true,
                    IsEmailVerified = false,
                    RequirePasswordChange = request.RequirePasswordChange,
                    FailedLoginAttempts = 0
                };

                _repository.User.Create(user);
                await _repository.SaveAsync();

                // Assign roles if provided
                if (request.RoleIds != null && request.RoleIds.Any())
                {
                    await AssignRolesInternalAsync(user.Id, request.RoleIds, schoolId);
                    await _repository.SaveAsync();
                }

                return ServiceResult<CreateUserResponseDto>.SuccessResult(new CreateUserResponseDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    TemporaryPassword = tempPassword,
                    RequirePasswordChange = user.RequirePasswordChange
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<CreateUserResponseDto>.FailureResult($"Error creating user: {ex.Message}");
            }
        }

        public async Task<ServiceResult<UserListDto>> GetUsersAsync(
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
                        u.Email.ToLower().Contains(searchLower) ||
                        (u.FirstName != null && u.FirstName.ToLower().Contains(searchLower)) ||
                        (u.LastName != null && u.LastName.ToLower().Contains(searchLower)));
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

                var userDtos = users.Select(MapToUserManagementDto).ToList();

                var result = new UserListDto
                {
                    Users = userDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                return ServiceResult<UserListDto>.SuccessResult(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserListDto>.FailureResult($"Error retrieving users: {ex.Message}");
            }
        }

        public async Task<ServiceResult<UserManagementDto>> GetUserByIdAsync(Guid userId)
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
                    return ServiceResult<UserManagementDto>.FailureResult("User not found");

                var userDto = MapToUserManagementDto(user);
                return ServiceResult<UserManagementDto>.SuccessResult(userDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserManagementDto>.FailureResult($"Error retrieving user: {ex.Message}");
            }
        }

        public async Task<ServiceResult<UserManagementDto>> UpdateUserAsync(
            Guid userId,
            UpdateUserRequest request,
            Guid updatedByUserId)
        {
            try
            {
                var user = await _repository.User.GetByIdAsync(userId, trackChanges: true);
                if (user == null)
                    return ServiceResult<UserManagementDto>.FailureResult("User not found");

                // Update only provided fields
                if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
                {
                    // Check if new email already exists in the same school
                    var emailExists = await _repository.User.FindByCondition(
                        u => u.Email == request.Email && u.TenantId == user.TenantId && u.Id != userId,
                        trackChanges: false)
                        .AnyAsync();

                    if (emailExists)
                        return ServiceResult<UserManagementDto>.FailureResult("Email already exists in this school");

                    user.Email = request.Email;
                    user.IsEmailVerified = false; // Reset verification if email changed
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

                // Reload with related data
                return await GetUserByIdAsync(userId);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserManagementDto>.FailureResult($"Error updating user: {ex.Message}");
            }
        }

        public async Task<ServiceResult<UserManagementDto>> AssignRolesToUserAsync(
            Guid userId,
            List<Guid> roleIds,
            Guid assignedByUserId)
        {
            try
            {
                var user = await _repository.User.GetByIdAsync(userId, trackChanges: false);
                if (user == null)
                    return ServiceResult<UserManagementDto>.FailureResult("User not found");

                await AssignRolesInternalAsync(userId, roleIds, user.TenantId);
                await _repository.SaveAsync();

                return await GetUserByIdAsync(userId);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserManagementDto>.FailureResult($"Error assigning roles: {ex.Message}");
            }
        }

        public async Task<ServiceResult<UserManagementDto>> RemoveRoleFromUserAsync(
            Guid userId,
            Guid roleId,
            Guid removedByUserId)
        {
            try
            {
                var userRole = await _repository.UserRole.FindByCondition(
                    ur => ur.UserId == userId && ur.RoleId == roleId,
                    trackChanges: true)
                    .FirstOrDefaultAsync();

                if (userRole == null)
                    return ServiceResult<UserManagementDto>.FailureResult("User role not found");

                _repository.UserRole.Delete(userRole);
                await _repository.SaveAsync();

                return await GetUserByIdAsync(userId);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserManagementDto>.FailureResult($"Error removing role: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> ActivateUserAsync(Guid userId, Guid activatedByUserId)
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

        public async Task<ServiceResult<bool>> DeactivateUserAsync(Guid userId, Guid deactivatedByUserId)
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

        public async Task<ServiceResult<bool>> DeleteUserAsync(Guid userId, Guid deletedByUserId)
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

        public async Task<ServiceResult<ResetPasswordResponseDto>> ResetUserPasswordAsync(
            Guid userId,
            Guid resetByUserId)
        {
            try
            {
                var user = await _repository.User.GetByIdAsync(userId, trackChanges: true);
                if (user == null)
                    return ServiceResult<ResetPasswordResponseDto>.FailureResult("User not found");

                var tempPassword = GenerateTemporaryPassword();
                user.PasswordHash = HashPassword(tempPassword);
                user.RequirePasswordChange = true;
                user.FailedLoginAttempts = 0;
                user.LockedUntil = null;

                _repository.User.Update(user);
                await _repository.SaveAsync();

                return ServiceResult<ResetPasswordResponseDto>.SuccessResult(new ResetPasswordResponseDto
                {
                    TemporaryPassword = tempPassword,
                    Message = "Password has been reset. User must change password on next login."
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<ResetPasswordResponseDto>.FailureResult($"Error resetting password: {ex.Message}");
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

            // Remove existing roles
            var existingRoles = await _repository.UserRole.FindByCondition(
                ur => ur.UserId == userId,
                trackChanges: true)
                .ToListAsync();

            foreach (var existingRole in existingRoles)
            {
                _repository.UserRole.Delete(existingRole);
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

        private static UserManagementDto MapToUserManagementDto(User user)
        {
            return new UserManagementDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.ProfileImageUrl,
                TenantId = user.TenantId,
                SchoolName = user.Tenant?.Name,
                IsActive = user.IsActive,
                IsEmailVerified = user.IsEmailVerified,
                RequirePasswordChange = user.RequirePasswordChange,
                IsLockedOut = user.IsLockedOut,
                LockedUntil = user.LockedUntil,
                Roles = user.UserRoles?.Select(ur => new RoleDto
                {
                    Id = ur.Role.Id,
                    Name = ur.Role.Name,
                    Description = ur.Role.Description
                }).ToList() ?? new List<RoleDto>(),
                Permissions = user.UserRoles?
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .Select(rp => rp.Permission.DisplayName)
                    .Distinct()
                    .ToList() ?? new List<string>(),
                CreatedOn = user.CreatedOn,
                UpdatedOn = user.UpdatedOn
            };
        }

        private static string GenerateTemporaryPassword()
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%";
            var random = new Random();
            var chars = new char[12];

            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = validChars[random.Next(validChars.Length)];
            }

            return new string(chars);
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        #endregion
    }
}
