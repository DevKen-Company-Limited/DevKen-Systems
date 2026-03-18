using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Application.DTOs.Identity;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Application.Service.Email;
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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Devken.CBC.SchoolManagement.Application.Service.IAuthService;

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
        private readonly IEmailService _emailService;
        private readonly IJwtService _jwtService;

        private static readonly string[] SuperAdminRoles = ["SuperAdmin"];
        private static readonly List<string> SuperAdminPermissions =
            PermissionCatalogue.All.Select(p => p.Key).Distinct().ToList();

        public AuthService(
            AppDbContext context,
            IEmailService emailService,
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
            _emailService = emailService;
        }

        // ─── REGISTER SCHOOL ─────────────────────────────────────────────────

        public async Task<RegisterSchoolResponse?> RegisterSchoolAsync(RegisterSchoolRequest request)
        {
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                if (await _context.Schools.AnyAsync(s => s.SlugName == request.SchoolSlug) ||
                    await _context.Users.AnyAsync(u => u.Email == request.AdminEmail))
                {
                    _logger.LogWarning("School registration failed: Slug or Email already exists.");
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
                    IsActive = true,
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
                    PasswordHash = _passwordHashingService.HashPassword(request.AdminPassword),
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _context.UserRoles.Add(new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    RoleId = roleId,
                });
                await _context.SaveChangesAsync();

                await _subscriptionSeedService.SeedTrialSubscriptionAsync(school.Id);

                var permissions = await GetUserPermissionsAsync(user.Id, school.Id);
                var roles = new List<string> { "SchoolAdmin" };
                var accessToken = _jwtService.GenerateToken(user, roles, permissions, school.Id);
                var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);

                await tx.CommitAsync();

                // Send welcome email after successful registration (fire-and-forget — don't fail
                // the registration if the email bounces; just log the error)
                _ = SendWelcomeEmailSafeAsync(user.Email, user.FirstName);

                return new RegisterSchoolResponse(
                    school.Id, accessToken, refreshToken,
                    new UserDto(
                        user.Id, user.Email,
                        $"{user.FirstName} {user.LastName}".Trim(),
                        school.Id, school.Name,
                        [.. roles], [.. permissions],
                        user.RequirePasswordChange));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error during school registration for {Email}", request.AdminEmail);
                throw;
            }
        }

        // ─── EMAIL / PASSWORD LOGIN ───────────────────────────────────────────

        public async Task<LoginResponse?> LoginAsync(LoginRequest request, string? ipAddress = null)
        {
            _logger.LogInformation("[Login] Attempt — Email: '{Email}'", request.Email);

            var user = await _context.Users
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

            if (user == null)
            {
                var exists = await _context.Users.AnyAsync(u => u.Email == request.Email);
                _logger.LogWarning(
                    exists
                        ? "[Login] FAIL — User '{Email}' exists but IsActive=false."
                        : "[Login] FAIL — Email '{Email}' not found.",
                    request.Email);
                return null;
            }

            if (!_passwordHashingService.VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("[Login] FAIL — Password mismatch for '{Email}'.", request.Email);
                return null;
            }

            // Opportunistic BCrypt rehash
            if (_passwordHashingService is BCryptPasswordHashingService bcryptSvc &&
                bcryptSvc.NeedsRehash(user.PasswordHash))
            {
                user.PasswordHash = _passwordHashingService.HashPassword(request.Password);
                await _context.SaveChangesAsync();
                _logger.LogInformation("[Login] Password rehashed for '{Email}'.", user.Email);
            }

            return await BuildLoginResponseAsync(
                user, user.TenantId, user.Tenant?.Name ?? string.Empty, ipAddress);
        }

        // ─── SSO LOGIN ────────────────────────────────────────────────────────

        /// <summary>
        /// Issues an access + refresh token pair for a user who was authenticated
        /// by an external SSO provider. The caller (SsoController) is responsible
        /// for validating the provider's id_token before invoking this method.
        /// </summary>
        public async Task<LoginResponse?> LoginSsoAsync(Guid userId, Guid tenantId)
        {
            _logger.LogInformation("[SsoLogin] Issuing tokens for UserId: {UserId}", userId);

            var user = await _context.Users
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u =>
                    u.Id == userId && u.TenantId == tenantId && u.IsActive);

            if (user == null)
            {
                _logger.LogWarning("[SsoLogin] User {UserId} not found or inactive.", userId);
                return null;
            }

            return await BuildLoginResponseAsync(
                user, tenantId, user.Tenant?.Name ?? string.Empty);
        }

        // ─── SHARED RESPONSE BUILDER ──────────────────────────────────────────

        private async Task<LoginResponse> BuildLoginResponseAsync(
            User user,
            Guid schoolId,
            string schoolName,
            string? ipAddress = null)
        {
            var permissions = await GetUserPermissionsAsync(user.Id, schoolId);
            var roles = await _context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
                .ToListAsync();

            if (roles.Count == 0)
                _logger.LogWarning(
                    "[Login] WARNING — User '{Email}' has no roles assigned.", user.Email);

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
                    [.. roles],
                    [.. permissions],
                    user.RequirePasswordChange));
        }

        // ─── SSO SETUP TOKEN ──────────────────────────────────────────────────

        public async Task<string> GenerateSsoSetupTokenAsync(Guid userId)
        {
            // Invalidate any previous unused setup tokens for this user
            var existing = await _context.SsoSetupTokens
                .Where(t => t.UserId == userId && t.ConsumedAt == null)
                .ToListAsync();
            _context.SsoSetupTokens.RemoveRange(existing);

            // Generate 32 random bytes → URL-safe base64 string
            var rawToken = Convert.ToBase64String(
                    RandomNumberGenerator.GetBytes(32))
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');

            // Store the SHA-256 hash — raw token is never persisted
            var hash = Convert.ToBase64String(
                SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

            _context.SsoSetupTokens.Add(new SsoSetupToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = hash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                CreatedAt = DateTime.UtcNow,
            });

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "[AuthService] SSO setup token generated for UserId: {UserId}", userId);

            return rawToken;
        }

        // ─── SSO OTP — GENERATE ───────────────────────────────────────────────

        public async Task<(string RawOtp, string OtpToken)> GenerateSsoOtpAsync(Guid userId)
        {
            // Invalidate any existing unused OTPs for this user
            var existing = await _context.SsoOtpTokens
                .Where(t => t.UserId == userId
                         && t.ConsumedAt == null
                         && t.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var old in existing)
                old.ConsumedAt = DateTime.UtcNow;

            // Fetch the user so we can address the email correctly
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive)
                ?? throw new InvalidOperationException(
                    $"User {userId} not found or inactive.");

            // Generate 6-digit OTP + binding token
            var rawOtp = Random.Shared.Next(100_000, 999_999).ToString();
            var otpToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                               .Replace("+", "-").Replace("/", "_").TrimEnd('=');
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawOtp)));

            _context.SsoOtpTokens.Add(new SsoOtpToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OtpHash = hash,
                BindingToken = otpToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                CreatedAt = DateTime.UtcNow,
                ConsumedAt = null,
            });

            await _context.SaveChangesAsync();

            // Send via the typed interface method — guaranteed to fire every time
            try
            {
                await _emailService.SendOtpAsync(user.Email, user.FirstName, rawOtp);
                _logger.LogInformation(
                    "[SsoOtp] OTP email sent to '{Email}'.", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[SsoOtp] Failed to send OTP email to '{Email}'.", user.Email);
                throw;
            }

            return (rawOtp, otpToken);
        }

        // ─── SSO OTP — VALIDATE ───────────────────────────────────────────────

        public async Task<SsoOtpValidationResult> ValidateSsoOtpAsync(
            string otpToken, string rawOtp)
        {
            var hash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(rawOtp)));

            var record = await _context.SsoOtpTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t =>
                    t.BindingToken == otpToken &&
                    t.OtpHash == hash &&
                    t.ConsumedAt == null &&
                    t.ExpiresAt > DateTime.UtcNow);

            if (record == null)
                return new SsoOtpValidationResult
                {
                    Success = false,
                    Message = "Invalid or expired verification code.",
                };

            record.ConsumedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new SsoOtpValidationResult
            {
                Success = true,
                UserId = record.UserId,
                TenantId = record.User.TenantId,
            };
        }

        // ─── SSO OTP — RESEND ─────────────────────────────────────────────────

        public async Task<SsoResendOtpResult> ResendSsoOtpAsync(string otpToken)
        {
            var original = await _context.SsoOtpTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.BindingToken == otpToken);

            if (original == null)
                return new SsoResendOtpResult
                {
                    Success = false,
                    Message = "Invalid session. Please sign in again.",
                };

            if (original.ConsumedAt != null)
                return new SsoResendOtpResult
                {
                    Success = false,
                    Message = "This session has already been used.",
                };

            // Consume the old record
            original.ConsumedAt = DateTime.UtcNow;

            // Generate a fresh OTP — reuse the same binding token so the client
            // doesn't need to update sessionStorage
            var rawOtp = Random.Shared.Next(100_000, 999_999).ToString();
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawOtp)));

            _context.SsoOtpTokens.Add(new SsoOtpToken
            {
                Id = Guid.NewGuid(),
                UserId = original.UserId,
                OtpHash = hash,
                BindingToken = otpToken,          // same token — client keeps the same key
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                CreatedAt = DateTime.UtcNow,
                ConsumedAt = null,
            });

            await _context.SaveChangesAsync();

            // Send the fresh OTP via the typed interface method
            try
            {
                await _emailService.SendOtpAsync(
                    original.User.Email, original.User.FirstName, rawOtp);
                _logger.LogInformation(
                    "[SsoOtp] Resend OTP email sent to '{Email}'.", original.User.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[SsoOtp] Failed to resend OTP email to '{Email}'.", original.User.Email);
                throw;
            }

            return new SsoResendOtpResult
            {
                Success = true,
                RawOtp = rawOtp,
                Email = original.User.Email,
                FirstName = original.User.FirstName,
            };
        }

        // ─── SSO SETUP TOKEN — CONSUME ────────────────────────────────────────

        /// <summary>
        /// Validates the raw setup token, sets the user's password, marks the token
        /// consumed, then sends a welcome email. Returns UserId + TenantId so the
        /// controller can issue a session immediately.
        /// </summary>
        public async Task<SsoSetupResult> ConsumeSsoSetupTokenAsync(
            string rawToken, string newPassword)
        {
            var hash = Convert.ToBase64String(
                SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

            var setupToken = await _context.SsoSetupTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t =>
                    t.TokenHash == hash &&
                    t.ConsumedAt == null &&
                    t.ExpiresAt > DateTime.UtcNow);

            if (setupToken == null)
            {
                _logger.LogWarning(
                    "[AuthService] SSO setup token not found, already used, or expired.");
                return new SsoSetupResult(
                    false,
                    "Invalid or expired setup link. Please sign in with Google again.");
            }

            var user = setupToken.User;
            if (user == null || !user.IsActive)
                return new SsoSetupResult(false, "User not found or deactivated.");

            // Set the password and clear the "must set password" flag
            user.PasswordHash = _passwordHashingService.HashPassword(newPassword);
            user.RequirePasswordChange = false;

            // Consume the token — one-time use only
            setupToken.ConsumedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "[AuthService] SSO password set for UserId: {UserId}", user.Id);

            // Send welcome email (fire-and-forget — don't fail the setup if email bounces)
            _ = SendWelcomeEmailSafeAsync(user.Email, user.FirstName);

            return new SsoSetupResult(true, "Password set successfully.")
            {
                UserId = user.Id,
                TenantId = user.TenantId,
            };
        }

        // ─── REFRESH TOKEN ────────────────────────────────────────────────────

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

            return new RefreshTokenResponse(
                accessToken, newToken,
                _jwtSettings.AccessTokenLifetimeMinutes * 60);
        }

        // ─── LOGOUT ───────────────────────────────────────────────────────────

        public async Task<bool> LogoutAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshToken && t.RevokedAt == null);

            if (token == null) return false;

            token.Revoke();
            await _context.SaveChangesAsync();
            return true;
        }

        // ─── CHANGE PASSWORD ──────────────────────────────────────────────────

        public async Task<AuthResult> ChangePasswordAsync(
            Guid userId, Guid? tenantId, ChangePasswordRequest request)
        {
            if (tenantId == null)
                return new AuthResult(false, "Tenant ID is required");

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Id == userId && u.TenantId == tenantId.Value);

            if (user == null)
                return new AuthResult(false, "User not found");

            if (!_passwordHashingService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
                return new AuthResult(false, "Invalid current password");

            user.PasswordHash = _passwordHashingService.HashPassword(request.NewPassword);
            user.RequirePasswordChange = false;

            // Revoke all existing refresh tokens to force re-login on other devices
            var tokens = await _context.RefreshTokens
                .Where(t => t.UserId == userId)
                .ToListAsync();
            tokens.ForEach(t => t.Revoke());

            await _context.SaveChangesAsync();
            return new AuthResult(true);
        }

        // ─── SUPER ADMIN LOGIN ────────────────────────────────────────────────

        public async Task<SuperAdminLoginResponse?> SuperAdminLoginAsync(
            SuperAdminLoginRequest request)
        {
            var admin = await _context.SuperAdmins
                .FirstOrDefaultAsync(a => a.Email == request.Email && a.IsActive);

            if (admin == null) return null;

            if (!_passwordHashingService.VerifyPassword(request.Password, admin.PasswordHash))
                return null;

            // Opportunistic rehash
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
                TenantId = Guid.Empty,
            };

            var accessToken = _jwtService.GenerateToken(user, SuperAdminRoles, SuperAdminPermissions);
            var refreshToken = await GenerateAndStoreSuperAdminRefreshTokenAsync(admin.Id);

            return new SuperAdminLoginResponse(
                accessToken,
                _jwtSettings.AccessTokenLifetimeMinutes * 60,
                new SuperAdminDto(admin.Id, admin.Email, admin.FirstName, admin.LastName),
                SuperAdminRoles,
                [.. SuperAdminPermissions],
                refreshToken);
        }

        public async Task<RefreshTokenResponse?> SuperAdminRefreshTokenAsync(
            RefreshTokenRequest request)
        {
            var old = await _context.SuperAdminRefreshTokens
                .FirstOrDefaultAsync(t =>
                    t.Token == request.RefreshToken && t.RevokedAt == null);

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
                TenantId = Guid.Empty,
            };

            var newRefreshToken = await GenerateAndStoreSuperAdminRefreshTokenAsync(
                admin.Id, old.IpAddress);
            var accessToken = _jwtService.GenerateToken(
                user, SuperAdminRoles, SuperAdminPermissions);

            await _context.SaveChangesAsync();

            return new RefreshTokenResponse(
                accessToken, newRefreshToken,
                _jwtSettings.AccessTokenLifetimeMinutes * 60);
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

        // ─── PRIVATE HELPERS ──────────────────────────────────────────────────

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

        private async Task<string> GenerateAndStoreRefreshTokenAsync(
            Guid userId, string? ipAddress = null)
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
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenLifetimeDays),
            });
            await _context.SaveChangesAsync();
            return token;
        }
        public async Task<bool> ForgotPasswordAsync(string email, string resetBaseUrl)
        {
            // Always return true — never leak whether an email exists
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            if (user == null)
            {
                _logger.LogWarning(
                    "[ForgotPassword] Request for unknown / inactive email '{Email}'.", email);
                return true; // silent no-op
            }

            // Invalidate any previous unused tokens for this user
            var existing = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.Id && t.ConsumedAt == null)
                .ToListAsync();
            _context.PasswordResetTokens.RemoveRange(existing);

            // Generate a 32-byte cryptographically secure raw token
            var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                               .Replace('+', '-').Replace('/', '_').TrimEnd('=');

            // Persist only the SHA-256 hash — raw token never touches the DB
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

            _context.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = hash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                CreatedAt = DateTime.UtcNow,
            });

            await _context.SaveChangesAsync();

            // Build the full reset link and send it
            var resetLink = $"{resetBaseUrl.TrimEnd('/')}?token={Uri.EscapeDataString(rawToken)}";

            try
            {
                await _emailService.SendPasswordResetAsync(user.Email, user.FirstName, resetLink);
                _logger.LogInformation(
                    "[ForgotPassword] Reset email dispatched to '{Email}'.", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[ForgotPassword] Failed to send reset email to '{Email}'.", user.Email);
                // Re-throw so the controller can return a 500 — a lost reset link is not silent
                throw;
            }

            return true;
        }

        // ─── RESET PASSWORD ───────────────────────────────────────────────────────────

        public async Task<AuthResult> ResetPasswordAsync(string rawToken, string newPassword)
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

            var record = await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t =>
                    t.TokenHash == hash &&
                    t.ConsumedAt == null &&
                    t.ExpiresAt > DateTime.UtcNow);

            if (record == null)
            {
                _logger.LogWarning("[ResetPassword] Invalid, expired, or already-used token.");
                return new AuthResult(false, "Invalid or expired reset link. Please request a new one.");
            }

            var user = record.User;
            if (user == null || !user.IsActive)
                return new AuthResult(false, "User not found or deactivated.");

            // Set the new password
            user.PasswordHash = _passwordHashingService.HashPassword(newPassword);
            user.RequirePasswordChange = false;

            // Consume the reset token — one-time use
            record.ConsumedAt = DateTime.UtcNow;

            // Revoke all existing refresh tokens so all other sessions are terminated
            var tokens = await _context.RefreshTokens
                .Where(t => t.UserId == user.Id)
                .ToListAsync();
            tokens.ForEach(t => t.Revoke());

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "[ResetPassword] Password reset successfully for '{Email}'.", user.Email);

            return new AuthResult(true);
        }
        /// <summary>
        /// Sends a welcome email without throwing — used as fire-and-forget after
        /// registration and SSO password setup so a broken SMTP config never blocks
        /// the user from getting their session.
        /// </summary>
        private async Task SendWelcomeEmailSafeAsync(string email, string firstName)
        {
            try
            {
                await _emailService.SendWelcomeAsync(email, firstName);
                _logger.LogInformation(
                    "[AuthService] Welcome email sent to '{Email}'.", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[AuthService] Failed to send welcome email to '{Email}'.", email);
                // Intentionally not re-thrown — welcome email failure is non-fatal
            }
        }
    }
}