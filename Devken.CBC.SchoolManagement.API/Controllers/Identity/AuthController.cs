using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public AuthController(IAuthService authService, JwtSettings jwtSettings, IUserActivityService activityService)
            : base(activityService)
        {
            _authService = authService;
            _jwtSettings = jwtSettings;
        }

        #region School Registration/Login

        [HttpPost("register-school")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterSchool([FromBody] RegisterSchoolRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            var result = await _authService.RegisterSchoolAsync(request);
            if (result == null)
                return ErrorResponse("School registration failed or slug already exists.", StatusCodes.Status400BadRequest);

            SetRefreshTokenCookie(result.RefreshToken);

            // Log activity with the newly created user's ID
            await LogUserActivityAsync(result.User.Id, result.User.SchoolId, "RegisterSchool", $"School: {request.SchoolName}");

            return SuccessResponse(new
            {
                AccessToken = result.AccessToken,
                ExpiresInSeconds = _jwtSettings.AccessTokenLifetimeMinutes * 60,
                RefreshToken = result.RefreshToken,
                User = new
                {
                    result.User.Id,
                    result.User.School1.Name,
                    result.User.Email,
                    result.User.FullName,
                    result.User.Roles,
                    result.User.Permissions,
                    result.User.RequirePasswordChange
                }
            }, "School registration successful");
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authService.LoginAsync(request, ip);

            if (result == null)
                return ErrorResponse("Invalid credentials.", StatusCodes.Status401Unauthorized);

            SetRefreshTokenCookie(result.RefreshToken);

            // Log activity with the logged-in user's ID
            await LogUserActivityAsync(result.User.Id, result.User.TenantId, "Login", $"IP: {ip}");

            return SuccessResponse(new
            {
                AccessToken = result.AccessToken,
                ExpiresInSeconds = result.AccessTokenExpiresInSeconds,
                RefreshToken = result.RefreshToken,
                User = new
                {
                    result.User.Id,
                    result.User.TenantId,
                    result.User.Email,
                    result.User.FullName,
                    result.User.Roles,
                    result.User.Permissions,
                    result.User.RequirePasswordChange
                },
                Message = result.User.RequirePasswordChange
                    ? "Password change required. Please change your password to continue."
                    : "Login successful"
            }, "Login successful");
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            var token = string.IsNullOrWhiteSpace(request?.Token) ? Request.Cookies["refreshToken"] : request.Token;
            if (string.IsNullOrWhiteSpace(token))
                return ErrorResponse("Refresh token missing.", StatusCodes.Status401Unauthorized);

            var result = await _authService.RefreshTokenAsync(new RefreshTokenRequest(token));
            if (result == null)
                return ErrorResponse("Invalid or expired refresh token.", StatusCodes.Status401Unauthorized);

            SetRefreshTokenCookie(result.RefreshToken);

            // For refresh, we can use CurrentUserId since the user is already authenticated
            await LogUserActivityAsync("RefreshToken");

            return SuccessResponse(new
            {
                AccessToken = result.AccessToken,
                ExpiresInSeconds = result.AccessTokenExpiresInSeconds,
                RefreshToken = result.RefreshToken
            }, "Token refreshed successfully");
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var token = Request.Cookies["refreshToken"];
            if (!string.IsNullOrWhiteSpace(token))
                await _authService.LogoutAsync(token);

            Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                Path = "/",
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            await LogUserActivityAsync("Logout");

            return SuccessResponse(new { }, "Logged out successfully");
        }

        #endregion

        #region Super Admin

        [HttpPost("super-admin/login")]
        [AllowAnonymous]
        public async Task<IActionResult> SuperAdminLogin([FromBody] SuperAdminLoginRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            var result = await _authService.SuperAdminLoginAsync(request);
            if (result == null)
                return ErrorResponse("Invalid super admin credentials.", StatusCodes.Status401Unauthorized);

            if (!string.IsNullOrWhiteSpace(result.RefreshToken))
                SetRefreshTokenCookie(result.RefreshToken);

            // Log activity with the super admin's user ID from the result
            if (result.User != null && result.User.Id != Guid.Empty)
            {
                await LogUserActivityAsync(result.User.Id, null, "SuperAdminLogin", null);
            }

            return SuccessResponse(new
            {
                AccessToken = result.AccessToken,
                ExpiresInSeconds = result.AccessTokenExpiresInSeconds,
                RefreshToken = result.RefreshToken,
                User = result.User,
                Roles = result.Roles,
                Permissions = result.Permissions
            }, "Super admin login successful");
        }

        [HttpPost("super-admin/refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> SuperAdminRefresh([FromBody] RefreshTokenRequestDto request)
        {
            var token = string.IsNullOrWhiteSpace(request?.Token) ? Request.Cookies["refreshToken"] : request.Token;
            if (string.IsNullOrWhiteSpace(token))
                return ErrorResponse("Refresh token missing.", StatusCodes.Status401Unauthorized);

            var result = await _authService.SuperAdminRefreshTokenAsync(new RefreshTokenRequest(token));
            if (result == null)
                return ErrorResponse("Invalid or expired refresh token.", StatusCodes.Status401Unauthorized);

            SetRefreshTokenCookie(result.RefreshToken);

            // For refresh, we can use CurrentUserId since the user is already authenticated
            await LogUserActivityAsync("SuperAdminRefresh");

            return SuccessResponse(new
            {
                AccessToken = result.AccessToken,
                ExpiresInSeconds = result.AccessTokenExpiresInSeconds,
                RefreshToken = result.RefreshToken
            }, "Super admin token refreshed successfully");
        }

        #endregion

        #region Password & User Info

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            var result = await _authService.ChangePasswordAsync(CurrentUserId, (Guid)CurrentTenantId, request);
            if (!result.Success)
                return ErrorResponse(result.Error ?? "Password change failed", StatusCodes.Status400BadRequest);

            await LogUserActivityAsync("ChangePassword");

            return SuccessResponse(new { }, "Password changed successfully. Please login again with your new password.");
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            return SuccessResponse(new
            {
                UserId = CurrentUserId,
                TenantId = CurrentTenantId,
                Email = CurrentUserEmail,
                Name = CurrentUserName,
                Permissions = CurrentUserPermissions.ToList(),
                Roles = CurrentUserRoles.ToList(),
                IsSuperAdmin = IsSuperAdmin
            });
        }

        #endregion

        #region Helpers

        private void SetRefreshTokenCookie(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)) return;

            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenLifetimeDays)
            });
        }

        private static IDictionary<string, string[]> ToErrorDictionary(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState) =>
            modelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
            );

        #endregion
    }
}