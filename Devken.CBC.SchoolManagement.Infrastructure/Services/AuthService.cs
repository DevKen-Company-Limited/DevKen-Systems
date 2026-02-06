using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Application.Service.Isubscription;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly JwtSettings _jwtSettings;
        private readonly IPermissionSeedService _permissionSeedService;
        private readonly ISubscriptionSeedService _subscriptionSeedService;
        private readonly ILogger<AuthService> _logger;
        private readonly IJwtService _jwtService; // Updated: Use IJwtService

        private static readonly string[] SuperAdminRoles = { "SuperAdmin" };
        private static readonly List<string> SuperAdminPermissions =
            PermissionCatalogue.All.Select(p => p.Key).Distinct().ToList();

        public AuthService(
            AppDbContext context,
            IPasswordHasher<User> passwordHasher,
            JwtSettings jwtSettings,
            IPermissionSeedService permissionSeedService,
            ISubscriptionSeedService subscriptionSeedService,
            ILogger<AuthService> logger,
            IJwtService jwtService) // Updated: Added IJwtService
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtSettings = jwtSettings;
            _permissionSeedService = permissionSeedService;
            _subscriptionSeedService = subscriptionSeedService;
            _logger = logger;
            _jwtService = jwtService; // Updated: Store the service
        }

        // ─────────────────────────────────────────────────────────
        // REGISTER SCHOOL (with trial subscription)
        // ─────────────────────────────────────────────────────────
        public async Task<RegisterSchoolResponse?> RegisterSchoolAsync(RegisterSchoolRequest request)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1️⃣ Check uniqueness
                if (await _context.Schools.AnyAsync(s => s.SlugName == request.SchoolSlug))
                {
                    _logger.LogWarning("School registration failed: Slug {Slug} already exists", request.SchoolSlug);
                    return null;
                }

                if (await _context.Users.AnyAsync(u => u.Email == request.AdminEmail))
                {
                    _logger.LogWarning("School registration failed: Email {Email} already exists", request.AdminEmail);
                    return null;
                }

                // 2️⃣ Create school
                var school = new School
                {
                    Id = Guid.NewGuid(),
                    Name = request.SchoolName,
                    SlugName = request.SchoolSlug,
                    Email = request.SchoolEmail,
                    PhoneNumber = request.SchoolPhone,
                    Address = request.SchoolAddress,
                    IsActive = true
                };

                _context.Schools.Add(school);
                await _context.SaveChangesAsync();
                _logger.LogInformation("School created: {SchoolId} - {SchoolName}", school.Id, school.Name);

                // 3️⃣ Seed permissions and roles
                var roleId = await _permissionSeedService.SeedPermissionsAndRolesAsync(school.Id);

                // 4️⃣ Create admin user
                var names = request.AdminFullName.Split(' ', 2);
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = request.AdminEmail,
                    FirstName = names[0],
                    LastName = names.Length > 1 ? names[1] : null,
                    Tenant = school,
                    IsActive = true,
                    IsEmailVerified = true,
                    RequirePasswordChange = true
                };

                user.PasswordHash = _passwordHasher.HashPassword(user, request.AdminPassword);

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Admin user created: {UserId} - {Email}", user.Id, user.Email);

                // 5️⃣ Assign admin role
                _context.UserRoles.Add(new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    RoleId = roleId
                });
                await _context.SaveChangesAsync();

                // 6️⃣ Create trial subscription
                var trialSubscription = await _subscriptionSeedService.SeedTrialSubscriptionAsync(school.Id);
                _logger.LogInformation(
                    "Trial subscription created for school {SchoolId}. Expires: {ExpiryDate}",
                    school.Id,
                    trialSubscription.ExpiryDate);

                // 7️⃣ Generate tokens
                var permissions = await GetUserPermissionsAsync(user.Id, school.Id);
                var roles = new List<string> { "SchoolAdmin" };
                var accessToken = _jwtService.GenerateToken(user, roles, permissions, school.Id); // Updated
                var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);

                // 8️⃣ Commit transaction
                await tx.CommitAsync();

                _logger.LogInformation(
                    "School registration completed successfully: {SchoolId} - {SchoolName}",
                    school.Id,
                    school.Name);

                return new RegisterSchoolResponse(
                     school.Id,
                     accessToken,
                     refreshToken,
                     new UserDto(
                         user.Id,
                         user.Email,
                         $"{user.FirstName} {user.LastName}".Trim(),
                         user.Tenant!.Id,
                         school.Name,
                         roles.ToArray(), // Use the roles list
                         permissions.ToArray(),
                         user.RequirePasswordChange
                     )
                 );
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error during school registration for {Email}", request.AdminEmail);
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────
        // LOGIN
        // ─────────────────────────────────────────────────────────
        public async Task<LoginResponse?> LoginAsync(LoginRequest request, string? ipAddress = null)
        {
            var school = await _context.Schools
                .FirstOrDefaultAsync(s => s.SlugName == request.TenantSlug && s.IsActive);

            if (school == null) return null;

            var user = await _context.Users
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u =>
                    u.Email == request.Email &&
                    u.Tenant!.Id == school.Id &&
                    u.IsActive);

            if (user == null) return null;

            var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (verify == PasswordVerificationResult.Failed)
                return null;

            var permissions = await GetUserPermissionsAsync(user.Id, school.Id);
            var roles = await _context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
                .ToListAsync();

            var accessToken = _jwtService.GenerateToken(user, roles, permissions, school.Id); // Updated
            var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id, ipAddress);

            return new LoginResponse(
                accessToken,
                refreshToken,
                _jwtSettings.AccessTokenLifetimeMinutes * 60,
                new UserInfo(
                    user.Id,
                    school.Id,
                    user.Email,
                    $"{user.FirstName} {user.LastName}".Trim(),
                    roles.ToArray(),
                    permissions.ToArray(),
                    user.RequirePasswordChange
                )
            );
        }

        // ─────────────────────────────────────────────────────────
        // REFRESH TOKEN
        // ─────────────────────────────────────────────────────────
        public async Task<RefreshTokenResponse?> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var old = await _context.RefreshTokens
                .FirstOrDefaultAsync(t =>
                    t.Token == request.RefreshToken &&
                    t.RevokedAt == null &&
                    t.ExpiresAt > DateTime.UtcNow);

            if (old == null) return null;

            old.Revoke();

            var user = await _context.Users
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Id == old.UserId && u.IsActive);

            if (user == null) return null;

            var newToken = await GenerateAndStoreRefreshTokenAsync(user.Id, old.IpAddress);
            var permissions = await GetUserPermissionsAsync(user.Id, user.Tenant!.Id);
            var roles = await _context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
                .ToListAsync();

            var accessToken = _jwtService.GenerateToken(user, roles, permissions, user.Tenant.Id); // Updated

            await _context.SaveChangesAsync();

            return new RefreshTokenResponse(accessToken, newToken, _jwtSettings.AccessTokenLifetimeMinutes * 60);
        }

        // ─────────────────────────────────────────────────────────
        // LOGOUT
        // ─────────────────────────────────────────────────────────
        public async Task<bool> LogoutAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshToken && t.RevokedAt == null);

            if (token == null) return false;

            token.Revoke();
            await _context.SaveChangesAsync();
            return true;
        }

        // ─────────────────────────────────────────────────────────
        // CHANGE PASSWORD
        // ─────────────────────────────────────────────────────────
        public async Task<AuthResult> ChangePasswordAsync(Guid userId, Guid? tenantId, ChangePasswordRequest request)
        {
            if (tenantId == null)
            {
                _logger.LogWarning("Password change failed: Tenant ID is null for user {UserId}", userId);
                return new AuthResult(false, "Tenant ID is required");
            }

            var user = await _context.Users
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Id == userId && u.Tenant!.Id == tenantId.Value);

            if (user == null)
            {
                _logger.LogWarning("Password change failed: User {UserId} not found in tenant {TenantId}", userId, tenantId);
                return new AuthResult(false, "User not found");
            }

            var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
            if (verify == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Password change failed: Invalid current password for user {UserId}", userId);
                return new AuthResult(false, "Invalid current password");
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
            user.RequirePasswordChange = false;

            var tokens = await _context.RefreshTokens.Where(t => t.UserId == userId).ToListAsync();
            tokens.ForEach(t => t.Revoke());

            await _context.SaveChangesAsync();

            _logger.LogInformation("Password changed successfully for user {UserId}", userId);

            return new AuthResult(true);
        }

        // ─────────────────────────────────────────────────────────
        // SUPER ADMIN METHODS
        // ─────────────────────────────────────────────────────────
        public async Task<SuperAdminLoginResponse?> SuperAdminLoginAsync(SuperAdminLoginRequest request)
        {
            var admin = await _context.SuperAdmins
                .FirstOrDefaultAsync(a => a.Email == request.Email && a.IsActive);

            if (admin == null) return null;

            var verify = new PasswordHasher<SuperAdmin>()
                .VerifyHashedPassword(admin, admin.PasswordHash, request.Password);

            if (verify == PasswordVerificationResult.Failed)
                return null;

            // Create a User object for JWT generation
            var user = new User
            {
                Id = admin.Id,
                Email = admin.Email,
                FirstName = admin.FirstName,
                LastName = admin.LastName,
                TenantId = Guid.Empty // No tenant for SuperAdmin
            };

            var accessToken = _jwtService.GenerateToken(user, SuperAdminRoles, SuperAdminPermissions); // Updated
            var refreshToken = await GenerateAndStoreSuperAdminRefreshTokenAsync(admin.Id);

            return new SuperAdminLoginResponse(
               accessToken,
               _jwtSettings.AccessTokenLifetimeMinutes * 60,
               new SuperAdminDto(admin.Id, admin.Email, admin.FirstName, admin.LastName),
               SuperAdminRoles.ToArray(),
               SuperAdminPermissions.ToArray(),
               refreshToken
            );
        }

        public async Task<RefreshTokenResponse?> SuperAdminRefreshTokenAsync(RefreshTokenRequest request)
        {
            var old = await _context.SuperAdminRefreshTokens
                .FirstOrDefaultAsync(t =>
                    t.Token == request.RefreshToken &&
                    t.RevokedAt == null);

            if (old == null)
                return null;

            // Revoke old token
            old.Revoke();

            var admin = await _context.SuperAdmins
                .FirstOrDefaultAsync(a =>
                    a.Id == old.SuperAdminId &&
                    a.IsActive);

            if (admin == null)
                return null;

            // Create a User object for JWT generation
            var user = new User
            {
                Id = admin.Id,
                Email = admin.Email,
                FirstName = admin.FirstName,
                LastName = admin.LastName,
                TenantId = Guid.Empty // No tenant for SuperAdmin
            };

            // Prefer current IP
            var ipAddress = old.IpAddress;
            var newRefreshToken = await GenerateAndStoreSuperAdminRefreshTokenAsync(admin.Id, ipAddress);
            var accessToken = _jwtService.GenerateToken(user, SuperAdminRoles, SuperAdminPermissions); // Updated

            await _context.SaveChangesAsync();

            return new RefreshTokenResponse(
                accessToken,
                newRefreshToken,
                _jwtSettings.AccessTokenLifetimeMinutes * 60
            );
        }

        public async Task<bool> SuperAdminLogoutAsync(string refreshToken)
        {
            var token = await _context.SuperAdminRefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshToken && t.RevokedAt == null);

            if (token == null) return false;

            token.Revoke();
            await _context.SaveChangesAsync();
            return true;
        }

        // ─────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────
        private async Task<List<string>> GetUserPermissionsAsync(Guid userId, Guid tenantId)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r)
                .Where(r => r.TenantId == tenantId)
                .SelectMany(r => r.RolePermissions)
                .Select(rp => rp.Permission!.Key)
                .Distinct()
                .ToListAsync();
        }

        private async Task<string> GenerateAndStoreRefreshTokenAsync(Guid userId, string? ipAddress = null)
        {
            var token = _jwtService.GenerateRefreshToken(); // Updated: Use JwtService

            _context.RefreshTokens.Add(new RefreshToken(
                userId,
                token,
                DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenLifetimeDays),
                ipAddress));

            await _context.SaveChangesAsync();
            return token;
        }

        private async Task<string> GenerateAndStoreSuperAdminRefreshTokenAsync(Guid superAdminId, string? ipAddress = null)
        {
            var token = _jwtService.GenerateRefreshToken(); // Updated: Use JwtService

            _context.SuperAdminRefreshTokens.Add(new SuperAdminRefreshToken
            {
                Id = Guid.NewGuid(),
                SuperAdminId = superAdminId,
                Token = token,
                IpAddress = ipAddress,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenLifetimeDays)
            });

            await _context.SaveChangesAsync();
            return token;
        }

        // Removed the old helper methods:
        // - GenerateAccessToken()
        // - GenerateSuperAdminAccessToken()
    }
}