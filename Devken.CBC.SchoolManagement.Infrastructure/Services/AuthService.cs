using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data;
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
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            AppDbContext context,
            IPasswordHasher<User> passwordHasher,
            JwtSettings jwtSettings,
            IPermissionSeedService permissionSeedService,
            ILogger<AuthService> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtSettings = jwtSettings;
            _permissionSeedService = permissionSeedService;
            _logger = logger;
        }

        // ── REGISTER SCHOOL ───────────────────────────────
        public async Task<RegisterSchoolResponse?> RegisterSchoolAsync(RegisterSchoolRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Starting school registration for {SchoolName}", request.SchoolName);

                if (await _context.Schools.AnyAsync(s => s.SlugName == request.SchoolSlug))
                    return null;

                if (await _context.Users.AnyAsync(u => u.Email == request.AdminEmail))
                    return null;

                var school = new School
                {
                    Id = Guid.NewGuid(),
                    Name = request.SchoolName,
                    SlugName = request.SchoolSlug,
                    Email = request.SchoolEmail,
                    PhoneNumber = request.SchoolPhone,
                    Address = request.SchoolAddress,
                    IsActive = true,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                };
                _context.Schools.Add(school);
                await _context.SaveChangesAsync();

                var schoolAdminRoleId = await _permissionSeedService.SeedPermissionsAndRolesAsync(school.Id);

                var names = request.AdminFullName.Split(' ', 2);
                var adminUser = new User
                {
                    Id = Guid.NewGuid(),
                    Email = request.AdminEmail,
                    FirstName = names.FirstOrDefault(),
                    LastName = names.Length > 1 ? names[1] : null,
                    PhoneNumber = request.AdminPhone,
                    Tenant = school,
                    IsActive = true,
                    IsEmailVerified = true,
                    RequirePasswordChange = false,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                };
                adminUser.PasswordHash = _passwordHasher.HashPassword(adminUser, request.AdminPassword);

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();

                var userRole = new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = adminUser.Id,
                    RoleId = schoolAdminRoleId,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                };
                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();

                var permissions = await GetUserPermissionsAsync(adminUser.Id, school.Id);

                var accessToken = GenerateAccessToken(adminUser, permissions);
                var refreshToken = await GenerateAndStoreRefreshTokenAsync(adminUser.Id);

                await transaction.CommitAsync();

                return new RegisterSchoolResponse(
                    school.Id,
                    accessToken,
                    refreshToken,
                    new UserDto(
                        adminUser.Id,
                        adminUser.Email,
                        $"{adminUser.FirstName} {adminUser.LastName}".Trim(),
                        school.Id,
                        school.Name,
                        new[] { "SchoolAdmin" },
                        permissions
                    )
                );
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ── LOGIN ─────────────────────────────────────────
        public async Task<LoginResponse?> LoginAsync(LoginRequest req, string? ipAddress = null)
        {
            var school = await _context.Schools.FirstOrDefaultAsync(s => s.SlugName == req.TenantSlug && s.IsActive);
            if (school == null) return null;

            var user = await _context.Users
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Email == req.Email && u.Tenant.Id == school.Id && u.IsActive);

            if (user == null || user.IsLockedOut) return null;

            var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
            if (verify == PasswordVerificationResult.Failed)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= _jwtSettings.MaxFailedLoginAttempts)
                    user.LockedUntil = DateTime.UtcNow.AddMinutes(_jwtSettings.LockoutDurationMinutes);

                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return null;
            }

            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            var permissions = await GetUserPermissionsAsync(user.Id, school.Id);
            var roles = await _context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                .ToListAsync();

            var accessToken = GenerateAccessToken(user, permissions);
            var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);

            return new LoginResponse(
                accessToken,
                refreshToken,
                _jwtSettings.AccessTokenLifetimeMinutes * 60,
                new UserInfo(
                    user.Id,
                    school.Id,
                    user.Email,
                    $"{user.FirstName} {user.LastName}".Trim(),
                    roles,
                    permissions
                )
            );
        }

        // ── REFRESH TOKEN ───────────────────────────────
        public async Task<RefreshTokenResponse?> RefreshTokenAsync(RefreshTokenRequest req)
        {
            var oldToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == req.RefreshToken && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow);
            if (oldToken == null) return null;

            var user = await _context.Users
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Id == oldToken.UserId && u.IsActive);
            if (user == null) return null;

            // Use Revoke() method instead of direct assignment
            oldToken.Revoke();
            _context.RefreshTokens.Update(oldToken);

            var newRefreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id, oldToken.IpAddress);
            var permissions = await GetUserPermissionsAsync(user.Id, user.Tenant!.Id);
            var accessToken = GenerateAccessToken(user, permissions);

            await _context.SaveChangesAsync();
            return new RefreshTokenResponse(accessToken, newRefreshToken, _jwtSettings.AccessTokenLifetimeMinutes * 60);
        }

        // ── LOGOUT ───────────────────────────────────────
        public async Task<bool> LogoutAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken && !t.IsRevoked);
            if (token == null) return false;

            token.Revoke();
            _context.RefreshTokens.Update(token);
            await _context.SaveChangesAsync();
            return true;
        }

        // ── CHANGE PASSWORD ─────────────────────────────
        public async Task<AuthResult> ChangePasswordAsync(Guid userId, Guid tenantId, ChangePasswordRequest req)
        {
            var user = await _context.Users
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Id == userId && u.Tenant!.Id == tenantId);
            if (user == null) return new AuthResult(false, "User not found");

            var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, req.CurrentPassword);
            if (verify == PasswordVerificationResult.Failed)
                return new AuthResult(false, "Current password is incorrect");

            user.PasswordHash = _passwordHasher.HashPassword(user, req.NewPassword);
            user.RequirePasswordChange = false;
            _context.Users.Update(user);

            var tokens = await _context.RefreshTokens.Where(t => t.UserId == userId).ToListAsync();
            foreach (var t in tokens) t.Revoke();
            _context.RefreshTokens.UpdateRange(tokens);

            await _context.SaveChangesAsync();
            return new AuthResult(true);
        }

        // ── SUPER ADMIN LOGIN ──────────────────────────
        public async Task<SuperAdminLoginResponse?> SuperAdminLoginAsync(SuperAdminLoginRequest req)
        {
            var admin = await _context.SuperAdmins.FirstOrDefaultAsync(a => a.Email == req.Email && a.IsActive);
            if (admin == null) return null;

            var fakeUser = new User();
            var verify = _passwordHasher.VerifyHashedPassword(fakeUser, admin.PasswordHash, req.Password);
            if (verify == PasswordVerificationResult.Failed) return null;

            var token = GenerateSuperAdminAccessToken(admin);
            return new SuperAdminLoginResponse(token);
        }

        // ── PRIVATE HELPERS ─────────────────────────────
        private async Task<List<string>> GetUserPermissionsAsync(Guid userId, Guid tenantId)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r)
                .Where(r => r.TenantId == tenantId)
                .SelectMany(r => r.RolePermissions)
                .Select(rp => rp.Permission!.Key)
                .Distinct()
                .ToListAsync();
        }

        private string GenerateAccessToken(User user, List<string> permissions)
        {
            var claims = new List<System.Security.Claims.Claim>
            {
                new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email, user.Email),
                new(System.Security.Claims.ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
                new(CustomClaimTypes.UserId, user.Id.ToString()),
                new(CustomClaimTypes.TenantId, user.Tenant!.Id.ToString())
            };
            claims.AddRange(permissions.Select(p => new System.Security.Claims.Claim(CustomClaimTypes.Permissions, p)));

            var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenLifetimeMinutes),
                signingCredentials: creds
            );

            return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<string> GenerateAndStoreRefreshTokenAsync(Guid userId, string? ipAddress = null)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var entity = new RefreshToken(userId, token, DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenLifetimeDays), ipAddress);
            _context.RefreshTokens.Add(entity);
            await _context.SaveChangesAsync();
            return token;
        }

        private string GenerateSuperAdminAccessToken(SuperAdmin admin)
        {
            var claims = new List<System.Security.Claims.Claim>
            {
                new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, admin.Id.ToString()),
                new(System.Security.Claims.ClaimTypes.Email, admin.Email),
                new(System.Security.Claims.ClaimTypes.Name, $"{admin.FirstName} {admin.LastName}".Trim())
            };

            var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenLifetimeMinutes),
                signingCredentials: creds
            );

            return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
