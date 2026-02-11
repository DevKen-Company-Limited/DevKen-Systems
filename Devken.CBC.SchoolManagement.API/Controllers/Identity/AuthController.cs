using System;
using System.Linq;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Identity
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;
        private readonly JwtSettings _jwtSettings;

        public AuthController(
            IAuthService authService,
            JwtSettings jwtSettings,
            IUserActivityService activityService,
            ILogger<AuthController> logger)
            : base(activityService, logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _jwtSettings = jwtSettings ?? throw new ArgumentNullException(nameof(jwtSettings));
        }

        #region School Registration / Login

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

            await LogUserActivitySafeAsync(
                result.User?.Id,
                result.User?.TenantId,
                "RegisterSchool",
                $"School: {request.SchoolName}");

            var responseDto = new RegisterSchoolResponseDto
            {
                AccessToken = result.AccessToken,
                ExpiresInSeconds = _jwtSettings.AccessTokenLifetimeMinutes * 60,
                RefreshToken = result.RefreshToken,
                User = result.User
            };

            return SuccessResponse(responseDto, "School registration successful");
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var tenantSlug = string.IsNullOrWhiteSpace(request.TenantSlug) ? "default-school" : request.TenantSlug;

            var result = await _authService.LoginAsync(
                new LoginRequest(tenantSlug, request.Email, request.Password),
                ip);

            if (result == null)
                return ErrorResponse("Invalid credentials.", StatusCodes.Status401Unauthorized);

            SetRefreshTokenCookie(result.RefreshToken);

            await LogUserActivitySafeAsync(
                result.User?.Id,
                result.User?.TenantId,
                "Login",
                $"IP: {ip}");

            // Ensure user DTO has permissions populated
            var userDto = result.User;
            if (userDto != null && result.Permissions?.Any() == true)
            {
                userDto.Permissions = result.Permissions.ToList();
            }

            var responseDto = new LoginResponseDto
            {
                AccessToken = result.AccessToken,
                ExpiresInSeconds = result.AccessTokenExpiresInSeconds,
                RefreshToken = result.RefreshToken,
                User = userDto,
                Message = result.User?.RequirePasswordChange == true
                    ? "Password change required. Please change your password to continue."
                    : "Login successful"
            };

            return SuccessResponse(responseDto, "Login successful");
        }

        #endregion

        #region Refresh / Logout

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken()
        {
            var token = Request.Cookies["refreshToken"];
            if (string.IsNullOrWhiteSpace(token))
                return ErrorResponse("Refresh token missing.", StatusCodes.Status401Unauthorized);

            var result = await _authService.RefreshTokenAsync(new RefreshTokenRequest(token));
            if (result == null)
                return ErrorResponse("Invalid or expired refresh token.", StatusCodes.Status401Unauthorized);

            SetRefreshTokenCookie(result.RefreshToken);

            await LogUserActivitySafeAsync(null, null, "RefreshToken");

            var response = new
            {
                AccessToken = result.AccessToken,
                AccessTokenExpiresInSeconds = result.AccessTokenExpiresInSeconds,
                RefreshToken = result.RefreshToken
            };

            return SuccessResponse(response, "Token refreshed successfully");
        }

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

            SetRefreshTokenCookie(result.RefreshToken);

            if (result.User?.Id != Guid.Empty)
                await LogUserActivitySafeAsync(result.User.Id, null, "SuperAdminLogin");

            var response = new
            {
                AccessToken = result.AccessToken,
                AccessTokenExpiresInSeconds = result.AccessTokenExpiresInSeconds,
                RefreshToken = result.RefreshToken,
                User = MapSuperAdminToUserDto(result.User, result.Permissions),
                Roles = result.Roles,
                Permissions = result.Permissions
            };

            return SuccessResponse(response, "Super admin login successful");
        }

        [HttpPost("super-admin/refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> SuperAdminRefresh()
        {
            var token = Request.Cookies["refreshToken"];
            if (string.IsNullOrWhiteSpace(token))
                return ErrorResponse("Refresh token missing.", StatusCodes.Status401Unauthorized);

            var result = await _authService.SuperAdminRefreshTokenAsync(new RefreshTokenRequest(token));
            if (result == null)
                return ErrorResponse("Invalid or expired refresh token.", StatusCodes.Status401Unauthorized);

            SetRefreshTokenCookie(result.RefreshToken);
            await LogUserActivitySafeAsync(null, null, "SuperAdminRefresh");

            var response = new
            {
                AccessToken = result.AccessToken,
                AccessTokenExpiresInSeconds = result.AccessTokenExpiresInSeconds,
                RefreshToken = result.RefreshToken
            };

            return SuccessResponse(response, "Super admin token refreshed successfully");
        }

        #endregion

        #region Password & User Info

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            var result = await _authService.ChangePasswordAsync(
                CurrentUserId,
                CurrentTenantId!.Value,
                request);

            if (!result.Success)
                return ErrorResponse(result.Error ?? "Password change failed", StatusCodes.Status400BadRequest);

            await LogUserActivitySafeAsync(CurrentUserId, CurrentTenantId, "ChangePassword");

            return SuccessResponse(new { }, "Password changed successfully. Please login again.");
        }

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
                IsSuperAdmin
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

        private void DeleteRefreshTokenCookie()
        {
            Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                Path = "/",
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
        }

        private static IDictionary<string, string[]> ToErrorDictionary(
            Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState) =>
            modelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>());

        private Task LogUserActivitySafeAsync(
            Guid? userId = null,
            Guid? tenantId = null,
            string? activityType = null,
            string? details = null)
        {
            if (!userId.HasValue || string.IsNullOrWhiteSpace(activityType))
                return Task.CompletedTask;
            return LogUserActivityAsync(userId.Value, tenantId, activityType, details ?? string.Empty);
        }

        private static UserDto MapToUserDto(UserInfo user, string[]? permissions = null)
        {
            if (user == null) return null!;

            // Split full name into first and last names
            string firstName = string.Empty;
            string lastName = string.Empty;

            if (!string.IsNullOrEmpty(user.FullName))
            {
                var nameParts = user.FullName.Split(' ', 2);
                firstName = nameParts[0];
                lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;
            }

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = firstName,
                LastName = lastName,
                SchoolId = user.TenantId,
                SchoolName = string.Empty, // SchoolName will be filled by service if needed
                IsActive = true, // Assuming active if we have UserInfo
                IsEmailVerified = true, // Assuming verified if we have UserInfo
                RequirePasswordChange = user.RequirePasswordChange,
                RoleNames = new List<string>(user.Roles ?? Array.Empty<string>()),
                Permissions = permissions?.ToList() ?? new List<string>(),
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow
            };
        }

        private static UserDto MapSuperAdminToUserDto(SuperAdminDto admin, string[]? permissions = null)
        {
            if (admin == null) return null!;

            return new UserDto
            {
                Id = admin.Id,
                Email = admin.Email,
                FirstName = admin.FirstName,
                LastName = admin.LastName,
                SchoolId = Guid.Empty, // SuperAdmin has no tenant
                SchoolName = "SuperAdmin",
                IsActive = true,
                IsEmailVerified = true,
                RequirePasswordChange = false,
                RoleNames = new List<string> { "SuperAdmin" },
                Permissions = permissions?.ToList() ?? new List<string>(),
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow
            };
        }

        // Additional mapping method for UserManagementDto to UserDto if needed
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
                RoleNames = userManagement.Roles?.Select(r => r.Name).ToList() ?? new List<string>(),
                Permissions = new List<string>(), // Add permissions if available in userManagement
                CreatedOn = userManagement.CreatedOn,
                UpdatedOn = userManagement.UpdatedOn
            };
        }
        #endregion
    }
}