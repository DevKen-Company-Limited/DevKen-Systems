// Api/Controllers/Identity/AuthController.cs
using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Application.DTOs.Identity;
using Devken.CBC.SchoolManagement.Application.DTOs.Identity.Devken.CBC.SchoolManagement.Application.DTOs.Identity;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Identity
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;
        private readonly JwtSettings _jwtSettings;
        private readonly IConfiguration _configuration;

        public AuthController(
            IAuthService authService,
            JwtSettings jwtSettings,
            IConfiguration configuration,
            IUserActivityService activityService,
            ILogger<AuthController> logger)
            : base(activityService, logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _jwtSettings = jwtSettings ?? throw new ArgumentNullException(nameof(jwtSettings));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        // ─── SCHOOL REGISTRATION / LOGIN ──────────────────────────────────────

        /// <summary>Registers a new school and seeds its first admin user.</summary>
        [HttpPost("register-school")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterSchool([FromBody] RegisterSchoolRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            var result = await _authService.RegisterSchoolAsync(request);
            if (result == null)
                return ErrorResponse(
                    "School registration failed. The slug or admin email may already be in use.",
                    StatusCodes.Status400BadRequest);

            SetRefreshTokenCookie(result.RefreshToken);

            await LogUserActivitySafeAsync(
                result.User?.Id,
                result.User?.TenantId,
                "RegisterSchool",
                $"School: {request.SchoolName}");

            return SuccessResponse(new RegisterSchoolResponseDto
            {
                AccessToken = result.AccessToken,
                ExpiresInSeconds = _jwtSettings.AccessTokenLifetimeMinutes * 60,
                RefreshToken = result.RefreshToken,
                User = result.User,
            }, "School registration successful");
        }

        /// <summary>Authenticates a school user by email and password.</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var tenantSlug = string.IsNullOrWhiteSpace(request.TenantSlug)
                ? "default-school"
                : request.TenantSlug;

            var result = await _authService.LoginAsync(
                new LoginRequest(tenantSlug, request.Email, request.Password), ip);

            if (result == null)
                return ErrorResponse("Invalid credentials.", StatusCodes.Status401Unauthorized);

            SetRefreshTokenCookie(result.RefreshToken);

            await LogUserActivitySafeAsync(
                result.User?.Id,
                result.User?.TenantId,
                "Login",
                $"IP: {ip}");

            var userDto = result.User;
            if (userDto != null && result.Permissions?.Any() == true)
                userDto.Permissions = result.Permissions.ToList();

            return SuccessResponse(new LoginResponseDto
            {
                AccessToken = result.AccessToken,
                ExpiresInSeconds = result.AccessTokenExpiresInSeconds,
                RefreshToken = result.RefreshToken,
                User = userDto,
                Message = result.User?.RequirePasswordChange == true
                    ? "Password change required. Please change your password to continue."
                    : "Login successful",
            }, "Login successful");
        }

        // ─── REFRESH / LOGOUT ─────────────────────────────────────────────────

        /// <summary>Exchanges a valid refresh token cookie for a new token pair.</summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken()
        {
            var token = Request.Cookies["refreshToken"];
            if (string.IsNullOrWhiteSpace(token))
                return ErrorResponse("Refresh token missing.", StatusCodes.Status401Unauthorized);

            var result = await _authService.RefreshTokenAsync(new RefreshTokenRequest(token));
            if (result == null)
                return ErrorResponse(
                    "Invalid or expired refresh token.", StatusCodes.Status401Unauthorized);

            SetRefreshTokenCookie(result.RefreshToken);
            await LogUserActivitySafeAsync(null, null, "RefreshToken");

            return SuccessResponse(new
            {
                result.AccessToken,
                result.AccessTokenExpiresInSeconds,
                result.RefreshToken,
            }, "Token refreshed successfully");
        }

        /// <summary>Revokes the refresh token cookie and ends the user's session.</summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var token = Request.Cookies["refreshToken"];
            if (!string.IsNullOrWhiteSpace(token))
                await _authService.LogoutAsync(token);

            DeleteRefreshTokenCookie();
            await LogUserActivitySafeAsync(CurrentUserId, CurrentTenantId, "Logout");

            return SuccessResponse(new { }, "Logged out successfully");
        }

        // ─── SUPER ADMIN ──────────────────────────────────────────────────────

        /// <summary>Authenticates a SuperAdmin by email and password.</summary>
        [HttpPost("super-admin/login")]
        [AllowAnonymous]
        public async Task<IActionResult> SuperAdminLogin([FromBody] SuperAdminLoginRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            var result = await _authService.SuperAdminLoginAsync(request);
            if (result == null)
                return ErrorResponse(
                    "Invalid super admin credentials.", StatusCodes.Status401Unauthorized);

            SetRefreshTokenCookie(result.RefreshToken);

            if (result.User?.Id != Guid.Empty)
                await LogUserActivitySafeAsync(result.User!.Id, null, "SuperAdminLogin");

            return SuccessResponse(new
            {
                result.AccessToken,
                result.AccessTokenExpiresInSeconds,
                result.RefreshToken,
                User = MapSuperAdminToUserDto(result.User, result.Permissions),
                result.Roles,
                result.Permissions,
            }, "Super admin login successful");
        }

        /// <summary>Exchanges a valid SuperAdmin refresh token for a new token pair.</summary>
        [HttpPost("super-admin/refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> SuperAdminRefresh()
        {
            var token = Request.Cookies["refreshToken"];
            if (string.IsNullOrWhiteSpace(token))
                return ErrorResponse("Refresh token missing.", StatusCodes.Status401Unauthorized);

            var result = await _authService.SuperAdminRefreshTokenAsync(
                new RefreshTokenRequest(token));
            if (result == null)
                return ErrorResponse(
                    "Invalid or expired refresh token.", StatusCodes.Status401Unauthorized);

            SetRefreshTokenCookie(result.RefreshToken);
            await LogUserActivitySafeAsync(null, null, "SuperAdminRefresh");

            return SuccessResponse(new
            {
                result.AccessToken,
                result.AccessTokenExpiresInSeconds,
                result.RefreshToken,
            }, "Super admin token refreshed successfully");
        }

        /// <summary>Revokes the SuperAdmin refresh token cookie and ends their session.</summary>
        [HttpPost("super-admin/logout")]
        [Authorize(Policy = "SuperAdmin")]
        public async Task<IActionResult> SuperAdminLogout()
        {
            var token = Request.Cookies["refreshToken"];
            if (!string.IsNullOrWhiteSpace(token))
                await _authService.SuperAdminLogoutAsync(token);

            DeleteRefreshTokenCookie();
            await LogUserActivitySafeAsync(CurrentUserId, null, "SuperAdminLogout");

            return SuccessResponse(new { }, "Super admin logged out successfully");
        }

        // ─── PASSWORD & USER INFO ─────────────────────────────────────────────

        /// <summary>Changes the authenticated user's password and revokes all sessions.</summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            var result = await _authService.ChangePasswordAsync(
                CurrentUserId,
                CurrentTenantId,
                request);

            if (!result.Success)
                return ErrorResponse(
                    result.Error ?? "Password change failed.", StatusCodes.Status400BadRequest);

            await LogUserActivitySafeAsync(CurrentUserId, CurrentTenantId, "ChangePassword");

            return SuccessResponse(new { }, "Password changed successfully. Please log in again.");
        }

        /// <summary>
        /// Accepts an email address and dispatches a password-reset link.
        /// Always returns 200 OK regardless of whether the email is registered —
        /// this prevents user-enumeration attacks.
        /// </summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            // Read base URL from configuration.
            // Set  AppSettings:PasswordResetBaseUrl  in appsettings.json or as an
            // environment variable.
            // Example:  "https://app.devkencbc.com/reset-password"
            var resetBaseUrl = _configuration["AppSettings:PasswordResetBaseUrl"]
                               ?? "https://app.devkencbc.com/reset-password";

            await _authService.ForgotPasswordAsync(request.Email, resetBaseUrl);

            // Always return the same message — never reveal whether the email exists
            return SuccessResponse(
                new { },
                "If that email is registered you will receive a password-reset link shortly.");
        }

        /// <summary>
        /// Validates the reset token and sets the user's new password.
        /// On success all existing sessions are terminated.
        /// </summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            var result = await _authService.ResetPasswordAsync(request.Token, request.NewPassword);

            if (!result.Success)
                return ErrorResponse(
                    result.Error ?? "Password reset failed.", StatusCodes.Status400BadRequest);

            await LogUserActivitySafeAsync(null, null, "ResetPassword");

            return SuccessResponse(new { }, "Password reset successfully. Please sign in.");
        }

        /// <summary>Returns the current user's identity claims decoded from the JWT.</summary>
        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            return SuccessResponse(new
            {
                UserId = CurrentUserId,
                TenantId = CurrentTenantId,
                Email = CurrentUserEmail ?? string.Empty,
                Name = CurrentUserName ?? string.Empty,
                Permissions = CurrentUserPermissions?.ToArray() ?? Array.Empty<string>(),
                Roles = CurrentUserRoles?.ToArray() ?? Array.Empty<string>(),
                IsSuperAdmin,
            });
        }

        // ─── PRIVATE HELPERS ──────────────────────────────────────────────────

        private void SetRefreshTokenCookie(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)) return;

            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenLifetimeDays),
            });
        }

        private void DeleteRefreshTokenCookie()
        {
            Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                Path = "/",
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
            });
        }

        private Task LogUserActivitySafeAsync(
            Guid? userId = null,
            Guid? tenantId = null,
            string? activityType = null,
            string? details = null)
        {
            if (!userId.HasValue || string.IsNullOrWhiteSpace(activityType))
                return Task.CompletedTask;

            return LogUserActivityAsync(
                userId.Value, tenantId, activityType, details ?? string.Empty);
        }

        private static IDictionary<string, string[]> ToErrorDictionary(
            Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState) =>
            modelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors
                           .Select(e => e.ErrorMessage)
                           .ToArray() ?? Array.Empty<string>());

        private static UserDto MapSuperAdminToUserDto(
            SuperAdminDto? admin,
            string[]? permissions = null)
        {
            if (admin == null) return null!;

            return new UserDto
            {
                Id = admin.Id,
                Email = admin.Email,
                FirstName = admin.FirstName,
                LastName = admin.LastName,
                SchoolId = Guid.Empty,
                SchoolName = "SuperAdmin",
                IsActive = true,
                IsEmailVerified = true,
                RequirePasswordChange = false,
                RoleNames = new List<string> { "SuperAdmin" },
                Permissions = permissions?.ToList() ?? new List<string>(),
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow,
            };
        }

        private static UserDto MapUserManagementToUserDto(UserManagementDto userManagement)
        {
            if (userManagement == null) return null!;

            return new UserDto
            {
                Id = userManagement.Id,
                Email = userManagement.Email,
                FirstName = userManagement.FirstName,
                LastName = userManagement.LastName,
                PhoneNumber = userManagement.PhoneNumber,
                ProfileImageUrl = userManagement.ProfileImageUrl,
                SchoolId = userManagement.TenantId,
                SchoolName = userManagement.SchoolName,
                IsActive = userManagement.IsActive,
                IsEmailVerified = userManagement.IsEmailVerified,
                RequirePasswordChange = userManagement.RequirePasswordChange,
                RoleNames = userManagement.Roles?.Select(r => r.Name).ToList()
                                            ?? new List<string>(),
                Permissions = new List<string>(),
                CreatedOn = userManagement.CreatedOn,
                UpdatedOn = userManagement.UpdatedOn,
            };
        }
    }
}