using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Application.DTOs.Identity;
using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service
{
    public interface IAuthService
    {
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
        // =========================================================
        // SCHOOL REGISTRATION & AUTH
        // =========================================================
        // ── SSO OTP ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// Generates a 6-digit OTP, stores its SHA-256 hash in the DB with a 5-minute
        /// expiry, and returns the raw OTP (for emailing) plus an opaque otpToken
        /// (for binding the verify-otp call to this user/attempt).
        /// </summary>
        Task<(string RawOtp, string OtpToken)> GenerateSsoOtpAsync(Guid userId);

        /// <summary>
        /// Validates the otpToken + raw OTP pair.
        /// Checks: hash match, not expired, not already used.
        /// Marks the record consumed on success (one-time use).
        /// </summary>
        Task<SsoOtpValidationResult> ValidateSsoOtpAsync(string otpToken, string rawOtp);

        /// <summary>
        /// Invalidates the existing OTP bound to otpToken and generates a fresh one.
        /// Returns the new raw OTP + the same otpToken reference for the resend email.
        /// </summary>
        Task<SsoResendOtpResult> ResendSsoOtpAsync(string otpToken);

        /// <summary>Sends the OTP email using your existing email service.</summary>
        Task SendSsoOtpEmailAsync(string toEmail, string firstName, string rawOtp);
        Task<string> GenerateSsoSetupTokenAsync(Guid userId);
        Task<SsoSetupResult> ConsumeSsoSetupTokenAsync(string rawToken, string newPassword);
        Task<RegisterSchoolResponse?> RegisterSchoolAsync(RegisterSchoolRequest request);

        Task<LoginResponse?> LoginAsync(
            LoginRequest request,
            string? ipAddress = null);

        /// <summary>
        /// Refresh access token using refresh token.
        /// MUST rebuild roles, permissions and tenant (school) context from DB.
        /// </summary>
        Task<RefreshTokenResponse?> RefreshTokenAsync(RefreshTokenRequest request);

        Task<bool> LogoutAsync(string refreshToken);

        // =========================================================
        // SSO
        // =========================================================

        /// <summary>
        /// Issue an access + refresh token pair for a user who was
        /// already authenticated by an external SSO provider (e.g. Google).
        /// No password check is performed — the caller is responsible for
        /// validating the provider's id_token before calling this method.
        /// </summary>
        Task<LoginResponse?> LoginSsoAsync(Guid userId, Guid tenantId);

        // =========================================================
        // PASSWORD MANAGEMENT
        // =========================================================

        /// <summary>
        /// Change password for a user.
        /// tenantId == schoolId (nullable for SuperAdmin).
        /// </summary>
        Task<AuthResult> ChangePasswordAsync(
            Guid userId,
            Guid? tenantId,
            ChangePasswordRequest request);

        // =========================================================
        // SUPER ADMIN AUTH
        // =========================================================
        Task<SuperAdminLoginResponse?> SuperAdminLoginAsync(SuperAdminLoginRequest request);

        /// <summary>
        /// Refresh token for SuperAdmin.
        /// MUST NOT inject tenant_id into JWT.
        /// </summary>
        Task<RefreshTokenResponse?> SuperAdminRefreshTokenAsync(RefreshTokenRequest request);

        Task<bool> SuperAdminLogoutAsync(string refreshToken);
    }
}