using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Security;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Identity
{
    [ApiController]
    [Route("api/auth/sso")]
    [AllowAnonymous]
    public class SsoController(
        AppDbContext db,
        IConfiguration cfg,
        IAuthService authService,
        JwtSettings jwtSettings,
        IUserActivityService activityService,
        ILogger<SsoController> logger)
        : BaseApiController(activityService, logger)
    {
        // ── Request DTOs ──────────────────────────────────────────────────────

        public record GoogleSsoRequest(string IdToken);

        public record VerifyOtpRequest(
            string OtpToken,
            string Otp);

        public record ResendOtpRequest(
            string OtpToken);

        public record SetSsoPasswordRequest(
            string SetupToken,
            string NewPassword,
            string ConfirmPassword);

        // ── POST /api/auth/sso/google ─────────────────────────────────────────
        // Validates the Google id_token, finds/provisions the user,
        // then ALWAYS issues an OTP — never a session directly.

        [HttpPost("google")]
        public async Task<IActionResult> GoogleAsync([FromBody] GoogleSsoRequest request)
        {
            // 0. Shape guard
            if (string.IsNullOrWhiteSpace(request?.IdToken))
                return ErrorResponse("id_token is required.", StatusCodes.Status400BadRequest);

            if (request.IdToken.Split('.').Length != 3)
            {
                logger.LogWarning("[SsoController] Received a non-JWT token value.");
                return ErrorResponse("Malformed id_token.", StatusCodes.Status400BadRequest);
            }

            // 1. Validate the Google id_token
            GoogleJsonWebSignature.Payload payload;
            try
            {
                var clientId = cfg["Sso:Google:ClientId"];
                if (string.IsNullOrWhiteSpace(clientId))
                {
                    logger.LogError("[SsoController] 'Sso:Google:ClientId' is not configured.");
                    return ErrorResponse(
                        "SSO is not configured on this server.",
                        StatusCodes.Status500InternalServerError);
                }

                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId },
                    IssuedAtClockTolerance = TimeSpan.FromMinutes(5),
                    ExpirationTimeClockTolerance = TimeSpan.FromMinutes(5),
                };

                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);

                logger.LogInformation(
                    "[SsoController] Token validated. Subject: {Sub}, Email: {Email}",
                    payload.Subject, payload.Email);
            }
            catch (InvalidJwtException ex)
            {
                logger.LogWarning(ex, "[SsoController] Google id_token validation failed: {Message}", ex.Message);
                return ErrorResponse(
                    $"Invalid Google token: {ex.Message}",
                    StatusCodes.Status401Unauthorized);
            }

            // 2. Validate email claim
            var email = payload.Email?.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(email))
                return Ok(new
                {
                    success = false,
                    requireEmailVerify = true,
                    requirePasswordSetup = false,
                    requireOtp = false,
                    message = "Google account has no email address.",
                    data = new
                    {
                        email = string.Empty,
                        reason = "no_email",
                        firstName = payload.GivenName ?? string.Empty,
                        lastName = payload.FamilyName ?? string.Empty,
                    },
                });

            if (!payload.EmailVerified)
                return Ok(new
                {
                    success = false,
                    requireEmailVerify = true,
                    requirePasswordSetup = false,
                    requireOtp = false,
                    message = "Google account email is not verified.",
                    data = new
                    {
                        email = email,
                        reason = "not_verified",
                        firstName = payload.GivenName ?? string.Empty,
                        lastName = payload.FamilyName ?? string.Empty,
                    },
                });

            // 3. Find or provision user
            var user = await db.Users
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                var defaultSchool = await db.Schools
                    .FirstOrDefaultAsync(s => s.SlugName == "default-school" && s.IsActive);

                if (defaultSchool == null)
                    return ErrorResponse(
                        "No active school found. Contact your administrator.",
                        StatusCodes.Status403Forbidden);

                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    FirstName = payload.GivenName ?? "Google",
                    LastName = payload.FamilyName ?? "User",
                    ProfileImageUrl = payload.Picture,
                    SchoolId = defaultSchool.Id,
                    TenantId = defaultSchool.Id,
                    IsActive = true,
                    IsEmailVerified = true,
                    RequirePasswordChange = true,
                    PasswordHash = string.Empty,
                    CreatedOn = DateTime.UtcNow,
                };

                db.Users.Add(user);
                await db.SaveChangesAsync();

                logger.LogInformation(
                    "[SsoController] Provisioned new Google SSO user: {Email} (ID: {Id})",
                    email, user.Id);
            }

            if (!user.IsActive)
                return ErrorResponse(
                    "Your account is deactivated. Contact your administrator.",
                    StatusCodes.Status403Forbidden);

            // 4. ALWAYS issue an OTP — never return a session from this endpoint.
            //    The OTP is sent to the user's email. The otpToken is a short-lived
            //    server-side binding token that links this attempt to the user.
            var (rawOtp, otpToken) = await authService.GenerateSsoOtpAsync(user.Id);

            await authService.SendSsoOtpEmailAsync(user.Email, user.FirstName, rawOtp);

            logger.LogInformation(
                "[SsoController] OTP issued for user {Email}. Token prefix: {Prefix}",
                email, otpToken[..Math.Min(8, otpToken.Length)]);

            return Ok(new
            {
                success = true,
                requireOtp = true,
                requirePasswordSetup = false,
                requireEmailVerify = false,
                otpToken,
                message = "A 6-digit verification code has been sent to your email.",
                data = new
                {
                    email = MaskEmail(email),
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    expiresInSeconds = 300,
                },
            });
        }

        // ── POST /api/auth/sso/verify-otp ─────────────────────────────────────
        // Validates the OTP. On success either:
        //   a) issues a full session  (existing user with password)
        //   b) returns requirePasswordSetup: true  (new user, no password yet)

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtpAsync([FromBody] VerifyOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.OtpToken))
                return ErrorResponse("OTP token is required.", StatusCodes.Status400BadRequest);

            if (string.IsNullOrWhiteSpace(request.Otp) || request.Otp.Length != 6)
                return ErrorResponse("A 6-digit code is required.", StatusCodes.Status400BadRequest);

            // Validate OTP — checks hash match, expiry, and one-time-use flag
            var otpResult = await authService.ValidateSsoOtpAsync(request.OtpToken, request.Otp);

            if (!otpResult.Success)
            {
                logger.LogWarning(
                    "[SsoController] OTP validation failed. Reason: {Reason}", otpResult.Message);

                return ErrorResponse(
                    otpResult.Message ?? "Invalid or expired verification code.",
                    StatusCodes.Status400BadRequest);
            }

            var user = await db.Users
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Id == otpResult.UserId);

            if (user == null || !user.IsActive)
                return ErrorResponse(
                    "User not found or deactivated.",
                    StatusCodes.Status403Forbidden);

            // New user — still needs to set a password
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                var setupToken = await authService.GenerateSsoSetupTokenAsync(user.Id);

                return Ok(new
                {
                    success = true,
                    requirePasswordSetup = true,
                    requireEmailVerify = false,
                    requireOtp = false,
                    setupToken,
                    message = "OTP verified. Please set a password to complete setup.",
                    data = new
                    {
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                    },
                });
            }

            // Existing user — issue full session
            var loginResult = await authService.LoginSsoAsync(user.Id, user.TenantId);

            if (loginResult == null)
                return ErrorResponse(
                    "Could not create session. Please try again.",
                    StatusCodes.Status500InternalServerError);

            Response.Cookies.Append("refreshToken", loginResult.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(jwtSettings.RefreshTokenLifetimeDays),
            });

            await LogUserActivityAsync(
                user.Id, user.TenantId, "GoogleSsoOtpLogin", $"Email: {user.Email}");

            var userDto = loginResult.User;
            if (userDto != null && loginResult.Permissions?.Length > 0)
                userDto.Permissions = loginResult.Permissions.ToList();

            return SuccessResponse(new LoginResponseDto
            {
                AccessToken = loginResult.AccessToken,
                ExpiresInSeconds = loginResult.AccessTokenExpiresInSeconds,
                RefreshToken = loginResult.RefreshToken,
                User = userDto,
                Message = "Google sign-in successful.",
            }, "Google sign-in successful.");
        }

        // ── POST /api/auth/sso/resend-otp ─────────────────────────────────────
        // Invalidates the existing OTP and sends a fresh one.

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtpAsync([FromBody] ResendOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.OtpToken))
                return ErrorResponse("OTP token is required.", StatusCodes.Status400BadRequest);

            var result = await authService.ResendSsoOtpAsync(request.OtpToken);

            if (!result.Success)
            {
                logger.LogWarning(
                    "[SsoController] Resend OTP failed. Reason: {Reason}", result.Message);

                return ErrorResponse(
                    result.Message ?? "Could not resend code. Please try again.",
                    StatusCodes.Status400BadRequest);
            }

            await authService.SendSsoOtpEmailAsync(result.Email, result.FirstName, result.RawOtp);

            return Ok(new
            {
                success = true,
                message = "A new verification code has been sent to your email.",
                expiresInSeconds = 300,
            });
        }

        // ── POST /api/auth/sso/set-password ───────────────────────────────────
        // Called after OTP is verified for new users who need to set a password.

        [HttpPost("set-password")]
        public async Task<IActionResult> SetPasswordAsync([FromBody] SetSsoPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.SetupToken))
                return ErrorResponse("Setup token is required.", StatusCodes.Status400BadRequest);

            if (string.IsNullOrWhiteSpace(request.NewPassword))
                return ErrorResponse("Password is required.", StatusCodes.Status400BadRequest);

            if (request.NewPassword != request.ConfirmPassword)
                return ErrorResponse("Passwords do not match.", StatusCodes.Status400BadRequest);

            if (!IsStrongPassword(request.NewPassword))
                return ErrorResponse(
                    "Password must be at least 8 characters and include uppercase, lowercase, a digit, and a special character.",
                    StatusCodes.Status400BadRequest);

            var result = await authService.ConsumeSsoSetupTokenAsync(
                request.SetupToken, request.NewPassword);

            if (!result.Success)
            {
                logger.LogWarning(
                    "[SsoController] set-password failed. Reason: {Reason}", result.Message);

                return ErrorResponse(
                    result.Message ?? "Invalid or expired setup token.",
                    StatusCodes.Status400BadRequest);
            }

            var loginResult = await authService.LoginSsoAsync(result.UserId, result.TenantId);

            if (loginResult == null)
                return ErrorResponse(
                    "Password set, but session could not be created. Please sign in.",
                    StatusCodes.Status500InternalServerError);

            Response.Cookies.Append("refreshToken", loginResult.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(jwtSettings.RefreshTokenLifetimeDays),
            });

            await LogUserActivityAsync(
                result.UserId, result.TenantId, "SsoPasswordSet", $"UserId: {result.UserId}");

            var userDto = loginResult.User;
            if (userDto != null && loginResult.Permissions?.Length > 0)
                userDto.Permissions = loginResult.Permissions.ToList();

            return SuccessResponse(new LoginResponseDto
            {
                AccessToken = loginResult.AccessToken,
                ExpiresInSeconds = loginResult.AccessTokenExpiresInSeconds,
                RefreshToken = loginResult.RefreshToken,
                User = userDto,
                Message = "Password set. Welcome to Devken CBC!",
            }, "Password set successfully.");
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static bool IsStrongPassword(string password)
        {
            if (password.Length < 8) return false;
            if (!password.Any(char.IsUpper)) return false;
            if (!password.Any(char.IsLower)) return false;
            if (!password.Any(char.IsDigit)) return false;
            if (!password.Any(c => !char.IsLetterOrDigit(c))) return false;
            return true;
        }

        /// <summary>Masks email for display: john.doe@gmail.com → j*******@gmail.com</summary>
        private static string MaskEmail(string email)
        {
            var at = email.IndexOf('@');
            if (at <= 1) return email;
            return email[0] + new string('*', at - 1) + email[at..];
        }
    }
}