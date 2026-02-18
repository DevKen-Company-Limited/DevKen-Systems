using Devken.CBC.SchoolManagement.Application.Common;
using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Services.UserManagement;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

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

        // ── Create ─────────────────────────────────────────────────────────
        public async Task<ServiceResult<UserDto>> CreateUserAsync(
            CreateUserRequest request,
            Guid schoolId,
            Guid createdBy)
        {
            try
            {
                var school = await _repository.School.GetByIdAsync(schoolId);
                if (school == null)
                    return ServiceResult<UserDto>.FailureResult("School not found.");

                var emailTaken = await _repository.User
                    .FindByCondition(
                        u => u.Email == request.Email && u.TenantId == schoolId,
                        trackChanges: false)
                    .AnyAsync();

                if (emailTaken)
                    return ServiceResult<UserDto>.FailureResult(
                        "A user with this email already exists in the selected school.");

                // Always auto-generate — never accept a client-supplied password.
                var tempPassword = GenerateTemporaryPassword();

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
                    RequirePasswordChange = true,   // always force change on first login
                    FailedLoginAttempts = 0
                };

                _repository.User.Create(user);
                await _repository.SaveAsync();

                if (request.RoleIds?.Count > 0)
                {
                    await AssignRolesInternalAsync(user.Id, request.RoleIds, schoolId);
                    await _repository.SaveAsync();
                }

                // Pass tempPassword into the DTO so the controller can return it to the admin.
                return ServiceResult<UserDto>.SuccessResult(MapToUserDto(user, tempPassword));
            }
            catch (Exception ex)
            {
                return ServiceResult<UserDto>.FailureResult($"Error creating user: {ex.Message}");
            }
        }

        // ── Get all (paginated) ────────────────────────────────────────────
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

                if (schoolId.HasValue)
                    query = query.Where(u => u.TenantId == schoolId.Value);

                if (isActive.HasValue)
                    query = query.Where(u => u.IsActive == isActive.Value);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.ToLower();
                    query = query.Where(u =>
                        u.Email.ToLower().Contains(s) ||
                        (u.FirstName != null && u.FirstName.ToLower().Contains(s)) ||
                        (u.LastName != null && u.LastName.ToLower().Contains(s)));
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

                var result = new PaginatedUsersResponse
                {
                    Users = users.Select(u => MapToUserDto(u)).ToList(),
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                return ServiceResult<PaginatedUsersResponse>.SuccessResult(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PaginatedUsersResponse>.FailureResult(
                    $"Error retrieving users: {ex.Message}");
            }
        }

        // ── Get by ID ──────────────────────────────────────────────────────
        public async Task<ServiceResult<UserDto>> GetUserByIdAsync(Guid userId)
        {
            try
            {
                var user = await _repository.User
                    .FindByCondition(u => u.Id == userId, trackChanges: false)
                    .Include(u => u.Tenant)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                            .ThenInclude(r => r.RolePermissions)
                                .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync();

                return user is null
                    ? ServiceResult<UserDto>.FailureResult("User not found.")
                    : ServiceResult<UserDto>.SuccessResult(MapToUserDto(user));
            }
            catch (Exception ex)
            {
                return ServiceResult<UserDto>.FailureResult($"Error retrieving user: {ex.Message}");
            }
        }

        // ── Update ─────────────────────────────────────────────────────────
        public async Task<ServiceResult<UserDto>> UpdateUserAsync(
            Guid userId,
            UpdateUserRequest request,
            Guid updatedBy)
        {
            try
            {
                var user = await _repository.User.GetByIdAsync(userId, trackChanges: true);
                if (user == null)
                    return ServiceResult<UserDto>.FailureResult("User not found.");

                // Email change — verify uniqueness within the same school
                if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
                {
                    var emailTaken = await _repository.User
                        .FindByCondition(
                            u => u.Email == request.Email &&
                                 u.TenantId == user.TenantId &&
                                 u.Id != userId,
                            trackChanges: false)
                        .AnyAsync();

                    if (emailTaken)
                        return ServiceResult<UserDto>.FailureResult(
                            "Email already exists in this school.");

                    user.Email = request.Email;
                    user.IsEmailVerified = false;
                }

                if (!string.IsNullOrWhiteSpace(request.FirstName)) user.FirstName = request.FirstName;
                if (!string.IsNullOrWhiteSpace(request.LastName)) user.LastName = request.LastName;
                if (request.PhoneNumber != null) user.PhoneNumber = request.PhoneNumber;
                if (request.ProfileImageUrl != null) user.ProfileImageUrl = request.ProfileImageUrl;
                if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;

                _repository.User.Update(user);
                await _repository.SaveAsync();

                return await GetUserByIdAsync(userId);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserDto>.FailureResult($"Error updating user: {ex.Message}");
            }
        }

        // ── Role management ────────────────────────────────────────────────
        public async Task<ServiceResult<UserDto>> AssignRolesToUserAsync(
            Guid userId,
            List<string> roleIds,
            Guid assignedBy)
        {
            try
            {
                var user = await _repository.User.GetByIdAsync(userId, trackChanges: false);
                if (user == null)
                    return ServiceResult<UserDto>.FailureResult("User not found.");

                var roleGuids = roleIds.Select(Guid.Parse).ToList();
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
                    return ServiceResult<UserDto>.FailureResult("User not found.");

                // Remove all existing role assignments
                var existing = await _repository.UserRole
                    .FindByCondition(ur => ur.UserId == userId, trackChanges: true)
                    .ToListAsync();

                foreach (var ur in existing)
                    _repository.UserRole.Delete(ur);

                // Assign the new set
                var roleGuids = roleIds.Select(Guid.Parse).ToList();
                foreach (var roleId in roleGuids)
                {
                    var role = await _repository.Role.GetByIdAsync(roleId, trackChanges: false);
                    if (role == null || role.TenantId != user.TenantId)
                        return ServiceResult<UserDto>.FailureResult(
                            $"Role {roleId} not found or does not belong to this school.");

                    _repository.UserRole.Create(new UserRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        RoleId = roleId
                    });
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
                var userRole = await _repository.UserRole
                    .FindByCondition(
                        ur => ur.UserId == userId && ur.RoleId == roleGuid,
                        trackChanges: true)
                    .FirstOrDefaultAsync();

                if (userRole == null)
                    return ServiceResult<UserDto>.FailureResult("User role assignment not found.");

                _repository.UserRole.Delete(userRole);
                await _repository.SaveAsync();

                return await GetUserByIdAsync(userId);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserDto>.FailureResult($"Error removing role: {ex.Message}");
            }
        }

        // ── Available roles ────────────────────────────────────────────────
        public async Task<ServiceResult<List<RoleDto>>> GetAvailableRolesAsync(Guid? schoolId)
        {
            try
            {
                var query = _repository.Role.FindAll(trackChanges: false);

                if (schoolId.HasValue)
                    query = query.Where(r => r.TenantId == schoolId.Value);

                var roles = await query
                    .OrderBy(r => r.Name)
                    .Select(r => new RoleDto
                    {
                        Id = r.Id,
                        Name = r.Name ?? string.Empty,
                        Description = r.Description,
                        IsSystemRole = r.IsSystemRole,
                        SchoolId = r.TenantId
                    })
                    .ToListAsync();

                return ServiceResult<List<RoleDto>>.SuccessResult(roles);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<RoleDto>>.FailureResult(
                    $"Error retrieving available roles: {ex.Message}");
            }
        }

        // ── Activate / Deactivate ──────────────────────────────────────────
        public async Task<ServiceResult<bool>> ActivateUserAsync(Guid userId, Guid activatedBy)
        {
            try
            {
                var user = await _repository.User.GetByIdAsync(userId, trackChanges: true);
                if (user == null)
                    return ServiceResult<bool>.FailureResult("User not found.");

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
                    return ServiceResult<bool>.FailureResult("User not found.");

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

        // ── Hard Delete ────────────────────────────────────────────────────
        /// <summary>
        /// Permanently removes the user and all their role assignments from the database.
        /// This action cannot be undone.
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteUserAsync(Guid userId, Guid deletedBy)
        {
            try
            {
                // Load with tracking so EF can cascade or we can clean up manually.
                var user = await _repository.User
                    .FindByCondition(u => u.Id == userId, trackChanges: true)
                    .Include(u => u.UserRoles)
                    .FirstOrDefaultAsync();

                if (user == null)
                    return ServiceResult<bool>.FailureResult("User not found.");

                // Remove all role assignments first to avoid FK constraint violations
                // if your database does not cascade deletes on UserRole → User.
                if (user.UserRoles?.Count > 0)
                {
                    foreach (var ur in user.UserRoles)
                        _repository.UserRole.Delete(ur);

                    await _repository.SaveAsync();
                }

                // Hard delete — row is gone from the database permanently.
                _repository.User.Delete(user);
                await _repository.SaveAsync();

                return ServiceResult<bool>.SuccessResult(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.FailureResult($"Error deleting user: {ex.Message}");
            }
        }

        // ── Password management ────────────────────────────────────────────
        public async Task<ServiceResult<PasswordResetResultDto>> ResetPasswordAsync(
            Guid userId,
            Guid resetBy)
        {
            try
            {
                var user = await _repository.User
                    .FindByCondition(u => u.Id == userId, trackChanges: true)
                    .Include(u => u.Tenant)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                            .ThenInclude(r => r.RolePermissions)
                                .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync();

                if (user == null)
                    return ServiceResult<PasswordResetResultDto>.FailureResult("User not found.");

                var tempPassword = GenerateTemporaryPassword();

                user.PasswordHash = _passwordHashingService.HashPassword(tempPassword);
                user.RequirePasswordChange = true;
                user.FailedLoginAttempts = 0;
                user.LockedUntil = null;

                _repository.User.Update(user);
                await _repository.SaveAsync();

                var result = new PasswordResetResultDto
                {
                    User = MapToUserDto(user),
                    TemporaryPassword = tempPassword,
                    Message = "Password reset. The user must change it on next login.",
                    ResetAt = DateTime.UtcNow,
                    ResetBy = resetBy
                };

                // TODO: dispatch email with temporary password
                return ServiceResult<PasswordResetResultDto>.SuccessResult(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PasswordResetResultDto>.FailureResult(
                    $"Error resetting password: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> ResendWelcomeEmailAsync(Guid userId)
        {
            try
            {
                var user = await _repository.User.GetByIdAsync(userId, trackChanges: false);
                if (user == null)
                    return ServiceResult<bool>.FailureResult("User not found.");

                // TODO: implement email dispatch
                return ServiceResult<bool>.SuccessResult(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.FailureResult($"Error resending welcome email: {ex.Message}");
            }
        }

        // ── Private helpers ────────────────────────────────────────────────
        private async Task AssignRolesInternalAsync(
            Guid userId,
            List<Guid> roleIds,
            Guid tenantId)
        {
            // Validate all roles belong to the school before writing anything
            foreach (var roleId in roleIds)
            {
                var role = await _repository.Role.GetByIdAsync(roleId, trackChanges: false);
                if (role == null || role.TenantId != tenantId)
                    throw new InvalidOperationException(
                        $"Role {roleId} not found or does not belong to school {tenantId}.");
            }

            foreach (var roleId in roleIds)
            {
                _repository.UserRole.Create(new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    RoleId = roleId
                });
            }
        }

        private static UserDto MapToUserDto(User user, string? tempPassword = null)
        {
            var roleNames = user.UserRoles?
                .Where(ur => ur.Role != null)
                .Select(ur => ur.Role!.Name ?? "Unknown")
                .ToList() ?? [];

            var permissions = user.UserRoles?
                .Where(ur => ur.Role?.RolePermissions != null)
                .SelectMany(ur => ur.Role!.RolePermissions!)
                .Where(rp => rp.Permission?.Key != null)
                .Select(rp => rp.Permission!.Key!)
                .Distinct()
                .ToList() ?? [];

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
                TenantId = user.TenantId,
                IsActive = user.IsActive,
                IsEmailVerified = user.IsEmailVerified,
                RequirePasswordChange = user.RequirePasswordChange,
                TemporaryPassword = tempPassword,
                RoleNames = roleNames,
                Permissions = permissions,
                CreatedOn = user.CreatedOn,
                UpdatedOn = user.UpdatedOn
            };
        }

        /// <summary>
        /// Generates a cryptographically secure 12-character temporary password
        /// containing upper, lower, digit, and symbol characters.
        /// Uses <see cref="RandomNumberGenerator"/> instead of <see cref="Random"/>
        /// to avoid predictable output.
        /// </summary>
        private static string GenerateTemporaryPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";  // no I/O (visual ambiguity)
            const string lower = "abcdefghjkmnpqrstuvwxyz";   // no l/o
            const string digits = "23456789";                   // no 0/1
            const string symbols = "!@#$%&*";
            const string all = upper + lower + digits + symbols;

            // Guarantee at least one character from each class
            var password = new char[12];
            password[0] = Pick(upper);
            password[1] = Pick(lower);
            password[2] = Pick(digits);
            password[3] = Pick(symbols);

            for (int i = 4; i < password.Length; i++)
                password[i] = Pick(all);

            // Shuffle with Fisher-Yates using cryptographic randomness
            for (int i = password.Length - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (password[i], password[j]) = (password[j], password[i]);
            }

            return new string(password);
        }

        private static char Pick(string chars) =>
            chars[RandomNumberGenerator.GetInt32(chars.Length)];
    }
}