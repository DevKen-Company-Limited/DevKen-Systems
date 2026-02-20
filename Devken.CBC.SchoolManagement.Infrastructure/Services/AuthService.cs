using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Application.Service.Isubscription;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHashingService _passwordHashingService;
        private readonly JwtSettings _jwtSettings;
        private readonly IPermissionSeedService _permissionSeedService;
        private readonly ISubscriptionSeedService _subscriptionSeedService;
        private readonly ILogger<AuthService> _logger;
        private readonly IJwtService _jwtService;

        private static readonly string[] SuperAdminRoles = { "SuperAdmin" };
        private static readonly List<string> SuperAdminPermissions =
            PermissionCatalogue.All.Select(p => p.Key).Distinct().ToList();

        public AuthService(
            AppDbContext context,
            IPasswordHashingService passwordHashingService,
            JwtSettings jwtSettings,
            IPermissionSeedService permissionSeedService,
            ISubscriptionSeedService subscriptionSeedService,
            ILogger<AuthService> logger,
            IJwtService jwtService)
        {
            _context = context;
            _passwordHashingService = passwordHashingService;
            _jwtSettings = jwtSettings;
            _permissionSeedService = permissionSeedService;
            _subscriptionSeedService = subscriptionSeedService;
            _logger = logger;
            _jwtService = jwtService;
        }

        // ─── REGISTER SCHOOL ─────────────────────────────────────────────
        public async Task<RegisterSchoolResponse?> RegisterSchoolAsync(RegisterSchoolRequest request)
        {
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                if (await _context.Schools.AnyAsync(s => s.SlugName == request.SchoolSlug) ||
                    await _context.Users.AnyAsync(u => u.Email == request.AdminEmail))
                {
                    _logger.LogWarning("School registration failed: Slug or Email exists.");
                    return null;
                }

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
                    IsEmailVerified = true,
                    RequirePasswordChange = true,
                    PasswordHash = _passwordHashingService.HashPassword(request.AdminPassword)
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _context.UserRoles.Add(new UserRole { Id = Guid.NewGuid(), UserId = user.Id, RoleId = roleId });
                await _context.SaveChangesAsync();

                await _subscriptionSeedService.SeedTrialSubscriptionAsync(school.Id);

                var permissions = await GetUserPermissionsAsync(user.Id, school.Id);
                var roles = new List<string> { "SchoolAdmin" };
                var accessToken = _jwtService.GenerateToken(user, roles, permissions, school.Id);
                var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);
                await tx.CommitAsync();

                return new RegisterSchoolResponse(school.Id, accessToken, refreshToken,
                    new UserDto(user.Id, user.Email, $"{user.FirstName} {user.LastName}".Trim(),
                        school.Id, school.Name, roles.ToArray(), permissions.ToArray(), user.RequirePasswordChange));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error during school registration for {Email}", request.AdminEmail);
                throw;
            }
        }

        // ─── LOGIN ───────────────────────────────────────────────────────
        /// <summary>
        /// Authenticates a user by email and password only.
        /// No school slug lookup is performed — the school is resolved from the user's own TenantId
        /// and returned in the response. This means any active user can log in with just their
        /// email and password regardless of which school they belong to.
        /// </summary>
        public async Task<LoginResponse?> LoginAsync(LoginRequest request, string? ipAddress = null)
        {
            _logger.LogInformation("[Login] Attempt — Email: '{Email}'", request.Email);

            // ── STEP 1: Find the user by email alone (no school/slug check) ──
            var user = await _context.Users
                .Include(u => u.Tenant)   // load school so we can return its name + slug
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

            if (user == null)
            {
                // Distinguish "email not found" from "account inactive" for logging
                var exists = await _context.Users.AnyAsync(u => u.Email == request.Email);
                if (!exists)
                    _logger.LogWarning("[Login] FAIL — Email '{Email}' not found.", request.Email);
                else
                    _logger.LogWarning("[Login] FAIL — User '{Email}' exists but IsActive=false.", request.Email);

                return null;
            }

            _logger.LogInformation(
                "[Login] User found — Id: {UserId} | TenantId: {TenantId} | School: '{School}'",
                user.Id, user.TenantId, user.Tenant?.Name ?? "(none)");

            // ── STEP 2: Verify password ───────────────────────────────────
            if (!_passwordHashingService.VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("[Login] FAIL — Password mismatch for '{Email}'.", request.Email);
                return null;
            }

            // ── STEP 3: Opportunistic BCrypt rehash ───────────────────────
            if (_passwordHashingService is BCryptPasswordHashingService bcryptSvc &&
                bcryptSvc.NeedsRehash(user.PasswordHash))
            {
                user.PasswordHash = _passwordHashingService.HashPassword(request.Password);
                await _context.SaveChangesAsync();
                _logger.LogInformation("[Login] Password rehashed for '{Email}'.", user.Email);
            }

            // ── STEP 4: Resolve the school from the user's TenantId ───────
            // We load it separately in case the Include above didn't fully populate it.
            var schoolId = user.TenantId;
            var schoolName = user.Tenant?.Name ?? string.Empty;

            // ── STEP 5: Load roles and permissions ────────────────────────
            var permissions = await GetUserPermissionsAsync(user.Id, schoolId);
            var roles = await _context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
                .ToListAsync();

            if (roles.Count == 0)
                _logger.LogWarning("[Login] WARNING — User '{Email}' has no roles assigned.", user.Email);

            // ── STEP 6: Issue tokens ──────────────────────────────────────
            var accessToken = _jwtService.GenerateToken(user, roles, permissions, schoolId);
            var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id, ipAddress);

            _logger.LogInformation(
                "[Login] SUCCESS — '{Email}' authenticated. School: '{School}' ({SchoolId})",
                user.Email, schoolName, schoolId);

            return new LoginResponse(
                accessToken,
                refreshToken,
                _jwtSettings.AccessTokenLifetimeMinutes * 60,
                new UserDto(
                    user.Id,
                    user.Email,
                    $"{user.FirstName} {user.LastName}".Trim(),
                    schoolId,
                    schoolName,
                    roles.ToArray(),
                    permissions.ToArray(),
                    user.RequirePasswordChange
                )
            );
        }

        // ─── REFRESH TOKEN ───────────────────────────────────────────────
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
            var permissions = await GetUserPermissionsAsync(user.Id, user.TenantId);
            var roles = await _context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
                .ToListAsync();

            var accessToken = _jwtService.GenerateToken(user, roles, permissions, user.TenantId);
            await _context.SaveChangesAsync();

            return new RefreshTokenResponse(accessToken, newToken, _jwtSettings.AccessTokenLifetimeMinutes * 60);
        }

        // ─── LOGOUT ─────────────────────────────────────────────────────
        public async Task<bool> LogoutAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshToken && t.RevokedAt == null);
            if (token == null) return false;
            token.Revoke();
            await _context.SaveChangesAsync();
            return true;
        }

        // ─── CHANGE PASSWORD ─────────────────────────────────────────────
        public async Task<AuthResult> ChangePasswordAsync(Guid userId, Guid? tenantId, ChangePasswordRequest request)
        {
            if (tenantId == null) return new AuthResult(false, "Tenant ID is required");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId.Value);

            if (user == null) return new AuthResult(false, "User not found");

            if (!_passwordHashingService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
                return new AuthResult(false, "Invalid current password");

            user.PasswordHash = _passwordHashingService.HashPassword(request.NewPassword);
            user.RequirePasswordChange = false;

            var tokens = await _context.RefreshTokens.Where(t => t.UserId == userId).ToListAsync();
            tokens.ForEach(t => t.Revoke());

            await _context.SaveChangesAsync();
            return new AuthResult(true);
        }

        // ─── SUPER ADMIN LOGIN ───────────────────────────────────────────
        public async Task<SuperAdminLoginResponse?> SuperAdminLoginAsync(SuperAdminLoginRequest request)
        {
            var admin = await _context.SuperAdmins
                .FirstOrDefaultAsync(a => a.Email == request.Email && a.IsActive);
            if (admin == null) return null;

            if (!_passwordHashingService.VerifyPassword(request.Password, admin.PasswordHash))
                return null;

            if (_passwordHashingService is BCryptPasswordHashingService bcryptSvc &&
                bcryptSvc.NeedsRehash(admin.PasswordHash))
            {
                admin.PasswordHash = _passwordHashingService.HashPassword(request.Password);
                await _context.SaveChangesAsync();
            }

            var user = new User
            {
                Id = admin.Id,
                Email = admin.Email,
                FirstName = admin.FirstName,
                LastName = admin.LastName,
                TenantId = Guid.Empty
            };

            var accessToken = _jwtService.GenerateToken(user, SuperAdminRoles, SuperAdminPermissions);
            var refreshToken = await GenerateAndStoreSuperAdminRefreshTokenAsync(admin.Id);

            return new SuperAdminLoginResponse(
                accessToken,
                _jwtSettings.AccessTokenLifetimeMinutes * 60,
                new SuperAdminDto(admin.Id, admin.Email, admin.FirstName, admin.LastName),
                SuperAdminRoles.ToArray(),
                SuperAdminPermissions.ToArray(),
                refreshToken);
        }

        public async Task<RefreshTokenResponse?> SuperAdminRefreshTokenAsync(RefreshTokenRequest request)
        {
            var old = await _context.SuperAdminRefreshTokens
                .FirstOrDefaultAsync(t => t.Token == request.RefreshToken && t.RevokedAt == null);
            if (old == null) return null;

            old.Revoke();
            var admin = await _context.SuperAdmins
                .FirstOrDefaultAsync(a => a.Id == old.SuperAdminId && a.IsActive);
            if (admin == null) return null;

            var user = new User
            {
                Id = admin.Id,
                Email = admin.Email,
                FirstName = admin.FirstName,
                LastName = admin.LastName,
                TenantId = Guid.Empty
            };

            var newRefreshToken = await GenerateAndStoreSuperAdminRefreshTokenAsync(admin.Id, old.IpAddress);
            var accessToken = _jwtService.GenerateToken(user, SuperAdminRoles, SuperAdminPermissions);
            await _context.SaveChangesAsync();

            return new RefreshTokenResponse(accessToken, newRefreshToken, _jwtSettings.AccessTokenLifetimeMinutes * 60);
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

        // ─── HELPERS ─────────────────────────────────────────────────────
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
            var token = _jwtService.GenerateRefreshToken();
            _context.RefreshTokens.Add(new RefreshToken(
                userId, token,
                DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenLifetimeDays),
                ipAddress));
            await _context.SaveChangesAsync();
            return token;
        }

        private async Task<string> GenerateAndStoreSuperAdminRefreshTokenAsync(
            Guid superAdminId, string? ipAddress = null)
        {
            var token = _jwtService.GenerateRefreshToken();
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
    }
}