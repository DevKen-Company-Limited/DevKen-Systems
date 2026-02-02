using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
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

        private static readonly string[] SuperAdminRoles = { "SuperAdmin" };
        private static readonly List<string> SuperAdminPermissions =
            PermissionCatalogue.All.Select(p => p.Key).Distinct().ToList();

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

        // ─────────────────────────────────────────────────────────
        // REGISTER SCHOOL
        // ─────────────────────────────────────────────────────────
        public async Task<RegisterSchoolResponse?> RegisterSchoolAsync(RegisterSchoolRequest request)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

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
                IsActive = true
            };

            _context.Schools.Add(school);
            await _context.SaveChangesAsync();

            var roleId = await _permissionSeedService.SeedPermissionsAndRolesAsync(school.Id);

            var names = request.AdminFullName.Split(' ', 2);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.AdminEmail,
                FirstName = names[0],
                LastName = names.Length > 1 ? names[1] : null,
                Tenant = school,
                IsActive = true,
                IsEmailVerified = true
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.AdminPassword);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _context.UserRoles.Add(new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = roleId
            });

            await _context.SaveChangesAsync();

            var permissions = await GetUserPermissionsAsync(user.Id, school.Id);
            var accessToken = GenerateAccessToken(user, permissions);
            var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);

            await tx.CommitAsync();

            return new RegisterSchoolResponse(
                school.Id,
                accessToken,
                refreshToken,
                new UserDto(
                    user.Id,
                    user.Email,
                    $"{user.FirstName} {user.LastName}".Trim(),
                    school.Id,
                    school.Name,
                    new[] { "SchoolAdmin" },
                    permissions
                )
            );
        }

        // ─────────────────────────────────────────────────────────
        // USER LOGIN
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

            var accessToken = GenerateAccessToken(user, permissions);
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
                    roles,
                    permissions
                )
            );
        }

        // ─────────────────────────────────────────────────────────
        // USER REFRESH TOKEN
        // ─────────────────────────────────────────────────────────
        public async Task<RefreshTokenResponse?> RefreshTokenAsync(RefreshTokenRequest request)
        {
            // ✅ Use RevokedAt == null for EF Core
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
            var accessToken = GenerateAccessToken(user, permissions);

            await _context.SaveChangesAsync();

            return new RefreshTokenResponse(accessToken, newToken, _jwtSettings.AccessTokenLifetimeMinutes * 60);
        }

        // ─────────────────────────────────────────────────────────
        // USER LOGOUT
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
        public async Task<AuthResult> ChangePasswordAsync(Guid userId, Guid tenantId, ChangePasswordRequest request)
        {
            var user = await _context.Users
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Id == userId && u.Tenant!.Id == tenantId);

            if (user == null)
                return new AuthResult(false, "User not found");

            var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
            if (verify == PasswordVerificationResult.Failed)
                return new AuthResult(false, "Invalid password");

            user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);

            var tokens = await _context.RefreshTokens.Where(t => t.UserId == userId).ToListAsync();
            tokens.ForEach(t => t.Revoke());

            await _context.SaveChangesAsync();
            return new AuthResult(true);
        }

        // ─────────────────────────────────────────────────────────
        // SUPER ADMIN LOGIN
        // ─────────────────────────────────────────────────────────
        public async Task<SuperAdminLoginResponse?> SuperAdminLoginAsync(SuperAdminLoginRequest req)
        {
            var admin = await _context.SuperAdmins
                .FirstOrDefaultAsync(a => a.Email == req.Email && a.IsActive);

            if (admin == null) return null;

            var verify = new PasswordHasher<SuperAdmin>()
                .VerifyHashedPassword(admin, admin.PasswordHash, req.Password);

            if (verify == PasswordVerificationResult.Failed)
                return null;

            var accessToken = GenerateSuperAdminAccessToken(admin);
            var refreshToken = await GenerateAndStoreSuperAdminRefreshTokenAsync(admin.Id);

            return new SuperAdminLoginResponse(
                accessToken,
                _jwtSettings.AccessTokenLifetimeMinutes * 60,
                new SuperAdminDto(admin.Id, admin.Email, admin.FirstName, admin.LastName),
                SuperAdminRoles,
                SuperAdminPermissions,
                refreshToken
            );
        }

        // ─────────────────────────────────────────────────────────
        // SUPER ADMIN REFRESH
        // ─────────────────────────────────────────────────────────
        public async Task<RefreshTokenResponse?> SuperAdminRefreshTokenAsync(RefreshTokenRequest req)
        {
            var old = await _context.SuperAdminRefreshTokens
                .FirstOrDefaultAsync(t => t.Token == req.RefreshToken && t.RevokedAt == null);

            if (old == null) return null;

            old.Revoke();

            var admin = await _context.SuperAdmins
                .FirstOrDefaultAsync(a => a.Id == old.SuperAdminId && a.IsActive);

            if (admin == null) return null;

            var newToken = await GenerateAndStoreSuperAdminRefreshTokenAsync(admin.Id, old.IpAddress);
            var accessToken = GenerateSuperAdminAccessToken(admin);

            await _context.SaveChangesAsync();

            return new RefreshTokenResponse(accessToken, newToken, _jwtSettings.AccessTokenLifetimeMinutes * 60);
        }

        // ─────────────────────────────────────────────────────────
        // SUPER ADMIN LOGOUT
        // ─────────────────────────────────────────────────────────
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
        private async Task<string> GenerateAndStoreSuperAdminRefreshTokenAsync(Guid superAdminId, string? ipAddress = null)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

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

        private string GenerateSuperAdminAccessToken(SuperAdmin admin)
        {
            var claims = new List<System.Security.Claims.Claim>
            {
                new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, admin.Id.ToString()),
                new(System.Security.Claims.ClaimTypes.Email, admin.Email),
                new(System.Security.Claims.ClaimTypes.Name, $"{admin.FirstName} {admin.LastName}".Trim()),
                new(CustomClaimTypes.IsSuperAdmin, "true")
            };

            claims.AddRange(
                SuperAdminPermissions.Select(p =>
                    new System.Security.Claims.Claim(CustomClaimTypes.Permissions, p)));

            return JwtTokenBuilder.BuildToken(_jwtSettings, claims);
        }

        private string GenerateAccessToken(User user, List<string> permissions)
        {
            var claims = new List<System.Security.Claims.Claim>
            {
                new(CustomClaimTypes.UserId, user.Id.ToString()),
                new(CustomClaimTypes.TenantId, user.Tenant!.Id.ToString()),
                new(System.Security.Claims.ClaimTypes.Email, user.Email),
                new(System.Security.Claims.ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim())
            };

            claims.AddRange(
                permissions.Select(p =>
                    new System.Security.Claims.Claim(CustomClaimTypes.Permissions, p)));

            return JwtTokenBuilder.BuildToken(_jwtSettings, claims);
        }

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
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            _context.RefreshTokens.Add(new RefreshToken(
                userId,
                token,
                DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenLifetimeDays),
                ipAddress));

            await _context.SaveChangesAsync();
            return token;
        }
    }
}
