using Azure;
using Azure.Core;
using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Security;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using LoginRequest = Devken.CBC.SchoolManagement.Application.Dtos.LoginRequest;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Identity
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : BaseApiController
    {
        private readonly AuthService _authService;
        private readonly JwtSettings _jwtSettings;

        public AuthController(AuthService authService, JwtSettings jwtSettings)
        {
            _authService = authService;
            _jwtSettings = jwtSettings;
        }

        [HttpPost("register-school")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterSchool([FromBody] RegisterSchoolRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(
                    ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    )
                );

            var result = await _authService.RegisterSchoolAsync(request);
            if (result == null)
                return ErrorResponse(
                    "School slug is already taken or registration failed.",
                    StatusCodes.Status400BadRequest
                );

            SetRefreshTokenCookie(result.RefreshToken);

            return CreatedAtAction(nameof(RegisterSchool), new
            {
                AccessToken = result.AccessToken,
                SchoolId = result.SchoolId,          
            });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(
                    ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    )
                );

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authService.LoginAsync(request, ipAddress);

            if (result == null)
                return ErrorResponse(
                    "Invalid credentials, account locked, or school not found.",
                    StatusCodes.Status401Unauthorized
                );

            SetRefreshTokenCookie(result.RefreshToken);

            return SuccessResponse(new
            {
                AccessToken = result.AccessToken,
                AccessTokenExpiresInSeconds = result.AccessTokenExpiresInSeconds,
                User = result.User
            }, "Login successful");
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrWhiteSpace(refreshToken))
                return ErrorResponse("Refresh token missing.", StatusCodes.Status401Unauthorized);

            var result = await _authService.RefreshTokenAsync(
                new RefreshTokenRequest(refreshToken)
            );

            if (result == null)
                return ErrorResponse(
                    "Invalid or expired refresh token.",
                    StatusCodes.Status401Unauthorized
                );

            SetRefreshTokenCookie(result.RefreshToken);

            return SuccessResponse(new
            {
                AccessToken = result.AccessToken,
                AccessTokenExpiresInSeconds = result.AccessTokenExpiresInSeconds
            }, "Token refreshed successfully");
        }

        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (!string.IsNullOrWhiteSpace(refreshToken))
                await _authService.LogoutAsync(refreshToken);

            Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                Path = "/",
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            return SuccessResponse(new { }, "Logged out successfully");
        }

        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(
                    ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    )
                );

            var result = await _authService.ChangePasswordAsync(
                CurrentUserId,
                CurrentTenantId,
                request
            );

            if (!result.Success)
                return ErrorResponse(
                    result.Error ?? "Password change failed",
                    StatusCodes.Status400BadRequest
                );

            return SuccessResponse(new { }, "Password changed successfully");
        }

        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetCurrentUser()
        {
            return SuccessResponse(new
            {
                UserId = CurrentUserId,
                TenantId = CurrentTenantId,
                Email = CurrentUserEmail,
                Name = CurrentUserName,
                Permissions = CurrentUserPermissions.ToList()
            });
        }

        [HttpPost("super-admin/login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SuperAdminLogin([FromBody] SuperAdminLoginRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(
                    ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    )
                );

            var result = await _authService.SuperAdminLoginAsync(request);
            if (result == null)
                return ErrorResponse(
                    "Invalid super admin credentials.",
                    StatusCodes.Status401Unauthorized
                );

            return SuccessResponse(new
            {
                AccessToken = result.AccessToken
            }, "Super admin login successful");
        }

        [HttpPost("verify-token")]
        [Authorize]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public IActionResult VerifyToken()
        {
            return SuccessResponse(new
            {
                Valid = true,
                UserId = CurrentUserId,
                TenantId = CurrentTenantId,
                Email = CurrentUserEmail
            }, "Token is valid");
        }

        private void SetRefreshTokenCookie(string refreshToken)
        {
            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenLifetimeDays)
            });
        }
    }
}
