// Application/Service/IAuthService.cs
using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Application.DTOs.Identity;
using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service
{
    public interface IAuthService
    {
        // ─── NESTED RESULT TYPES ──────────────────────────────────────────────

        public class SsoOtpValidationResult
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public Guid UserId { get; set; }
            public Guid TenantId { get; set; }
        }

        public class SsoResendOtpResult
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public string RawOtp { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
        }

        // ─── SCHOOL REGISTRATION ──────────────────────────────────────────────

        /// <summary>
        /// Registers a new school and its first admin user.
        /// Seeds permissions, roles, and a trial subscription.
        /// Sends a welcome email to the admin on success.
        /// </summary>
        Task<RegisterSchoolResponse?> RegisterSchoolAsync(RegisterSchoolRequest request);

        // ─── EMAIL / PASSWORD LOGIN ───────────────────────────────────────────

        /// <summary>
        /// Authenticates a user by email and password.
        /// Returns an access + refresh token pair on success, null on failure.
        /// </summary>
        Task<LoginResponse?> LoginAsync(LoginRequest request, string? ipAddress = null);

        // ─── SSO LOGIN ────────────────────────────────────────────────────────

        /// <summary>
        /// Issues an access + refresh token pair for a user who was already
        /// authenticated by an external SSO provider (e.g. Google).
        /// No password check is performed — the caller is responsible for
        /// validating the provider's id_token before calling this method.
        /// </summary>
        Task<LoginResponse?> LoginSsoAsync(Guid userId, Guid tenantId);

        // ─── SSO SETUP TOKEN ──────────────────────────────────────────────────

        /// <summary>
        /// Generates a short-lived (15-minute), one-time setup token for a new
        /// SSO user who needs to set their initial password.
        /// Any previously unused setup tokens for this user are invalidated.
        /// </summary>
        Task<string> GenerateSsoSetupTokenAsync(Guid userId);

        /// <summary>
        /// Validates the raw setup token, sets the user's password, marks the
        /// token consumed (one-time use), and sends a welcome email.
        /// Returns UserId + TenantId so the controller can issue a session.
        /// </summary>
        Task<SsoSetupResult> ConsumeSsoSetupTokenAsync(string rawToken, string newPassword);

        // ─── SSO OTP ──────────────────────────────────────────────────────────

        /// <summary>
        /// Generates a 6-digit OTP, stores its SHA-256 hash in the DB with a
        /// 5-minute expiry, and sends it to the user's email automatically.
        /// Returns the raw OTP (for testing/logging only) and an opaque otpToken
        /// that binds the verify-otp call to this specific user/attempt.
        /// </summary>
        Task<(string RawOtp, string OtpToken)> GenerateSsoOtpAsync(Guid userId);

        /// <summary>
        /// Validates the otpToken + raw OTP pair.
        /// Checks: hash match, not expired, not already consumed.
        /// Marks the record consumed on success (one-time use only).
        /// </summary>
        Task<SsoOtpValidationResult> ValidateSsoOtpAsync(string otpToken, string rawOtp);

        /// <summary>
        /// Invalidates the OTP bound to otpToken, generates a fresh one, and
        /// sends it to the user's email automatically.
        /// Returns the same otpToken reference so the client session is unchanged.
        /// </summary>
        Task<SsoResendOtpResult> ResendSsoOtpAsync(string otpToken);

        // ─── TOKEN MANAGEMENT ─────────────────────────────────────────────────

        /// <summary>
        /// Exchanges a valid refresh token for a new access + refresh token pair.
        /// Rebuilds roles, permissions, and tenant (school) context from the DB.
        /// </summary>
        Task<RefreshTokenResponse?> RefreshTokenAsync(RefreshTokenRequest request);

        /// <summary>Revokes the given refresh token, ending the user's session.</summary>
        Task<bool> LogoutAsync(string refreshToken);

        // ─── PASSWORD MANAGEMENT ──────────────────────────────────────────────

        /// <summary>
        /// Changes the password for an authenticated school user.
        /// tenantId == schoolId (pass null only for SuperAdmin callers).
        /// Revokes all existing refresh tokens on success to force re-login
        /// on other devices.
        /// </summary>
        Task<AuthResult> ChangePasswordAsync(
            Guid userId,
            Guid? tenantId,
            ChangePasswordRequest request);

        /// <summary>
        /// Looks up the user by <paramref name="email"/>, generates a secure
        /// one-time reset token, persists its SHA-256 hash, and dispatches a
        /// password-reset email containing a link built from
        /// <paramref name="resetBaseUrl"/>.
        /// Always returns <c>true</c> even when the email is not found — callers
        /// must never reveal whether an address is registered (enumeration guard).
        /// </summary>
        Task<bool> ForgotPasswordAsync(string email, string resetBaseUrl);

        /// <summary>
        /// Validates the raw <paramref name="token"/>, sets the user's new password,
        /// marks the token consumed (one-time use), and revokes all existing refresh
        /// tokens so every other active session is terminated.
        /// </summary>
        Task<AuthResult> ResetPasswordAsync(string token, string newPassword);

        // ─── SUPER ADMIN AUTH ─────────────────────────────────────────────────

        /// <summary>Authenticates a SuperAdmin by email and password.</summary>
        Task<SuperAdminLoginResponse?> SuperAdminLoginAsync(SuperAdminLoginRequest request);

        /// <summary>
        /// Refreshes a SuperAdmin session.
        /// Must NOT inject tenant_id into the JWT.
        /// </summary>
        Task<RefreshTokenResponse?> SuperAdminRefreshTokenAsync(RefreshTokenRequest request);

        /// <summary>Revokes the given SuperAdmin refresh token.</summary>
        Task<bool> SuperAdminLogoutAsync(string refreshToken);
    }
}