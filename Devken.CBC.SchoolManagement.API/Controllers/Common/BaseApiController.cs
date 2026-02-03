using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Common
{
    [ApiController]
    [Produces("application/json")]
    public abstract class BaseApiController : ControllerBase
    {
        private readonly IUserActivityService? _activityService;

        protected BaseApiController(IUserActivityService? activityService = null)
        {
            _activityService = activityService;
        }

        #region Current User Info

        protected Guid CurrentUserId
        {
            get
            {
                var userIdClaim = User.FindFirst("user_id")?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                    throw new UnauthorizedAccessException("User ID not found in token");

                return userId;
            }
        }

        protected Guid? CurrentTenantId
        {
            get
            {
                var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
                if (string.IsNullOrEmpty(tenantIdClaim)) return null;
                if (Guid.TryParse(tenantIdClaim, out var tenantId)) return tenantId;
                return null;
            }
        }

        protected string CurrentUserEmail
        {
            get
            {
                var email = User.FindFirst(ClaimTypes.Email)?.Value
                    ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

                if (string.IsNullOrEmpty(email))
                    throw new UnauthorizedAccessException("Email not found in token");

                return email;
            }
        }

        protected string CurrentUserName
        {
            get
            {
                var name = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value
                    ?? User.Identity?.Name;

                return name ?? "Unknown";
            }
        }

        protected IEnumerable<string> CurrentUserPermissions =>
            User.FindAll("permissions").Select(c => c.Value).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct();

        protected IEnumerable<string> CurrentUserRoles =>
            User.FindAll(ClaimTypes.Role).Select(c => c.Value).Where(r => !string.IsNullOrWhiteSpace(r)).Distinct();

        protected bool IsSuperAdmin =>
            User.FindFirst("is_super_admin")?.Value?.ToLower() == "true"
            || CurrentUserRoles.Contains("SuperAdmin");

        #endregion

        #region Permissions & Roles

        protected bool HasPermission(string permission) =>
            !string.IsNullOrWhiteSpace(permission) && (IsSuperAdmin || CurrentUserPermissions.Contains(permission, StringComparer.OrdinalIgnoreCase));

        protected bool HasAllPermissions(params string[] permissions) =>
            permissions == null || permissions.Length == 0 || IsSuperAdmin || permissions.All(p => CurrentUserPermissions.Contains(p, StringComparer.OrdinalIgnoreCase));

        protected bool HasAnyPermission(params string[] permissions) =>
            permissions != null && (IsSuperAdmin || permissions.Any(p => CurrentUserPermissions.Contains(p, StringComparer.OrdinalIgnoreCase)));

        protected bool HasRole(string role) =>
            !string.IsNullOrWhiteSpace(role) && CurrentUserRoles.Contains(role, StringComparer.OrdinalIgnoreCase);

        #endregion

        #region User Activity Logging

        // Overload for authenticated requests (uses claims from JWT)
        protected async Task LogUserActivityAsync(string activityType, string? details = null)
        {
            if (_activityService == null) return;
            await _activityService.LogActivityAsync(CurrentUserId, CurrentTenantId, activityType, details);
        }

        // Overload for unauthenticated requests (login/register) where user ID comes from the auth result
        protected async Task LogUserActivityAsync(Guid userId, Guid? tenantId, string activityType, string? details = null)
        {
            if (_activityService == null) return;
            await _activityService.LogActivityAsync(userId, tenantId, activityType, details);
        }

        #endregion

        #region Standardized Responses

        protected IActionResult SuccessResponse<T>(T data, string message = "Success") =>
            Ok(new { Success = true, Message = message, Data = data });

        protected IActionResult ErrorResponse(string message, int statusCode = 400) =>
            StatusCode(statusCode, new { Success = false, Message = message, Data = (object)null });

        protected IActionResult ValidationErrorResponse(IDictionary<string, string[]> errors) =>
            BadRequest(new { Success = false, Message = "Validation failed", Errors = errors, Data = (object)null });

        protected IActionResult NotFoundResponse(string message = "Resource not found") =>
            NotFound(new { Success = false, Message = message, Data = (object)null });

        protected IActionResult UnauthorizedResponse(string message = "Unauthorized access") =>
            Unauthorized(new { Success = false, Message = message, Data = (object)null });

        protected IActionResult ForbiddenResponse(string message = "Access forbidden") =>
            StatusCode(403, new { Success = false, Message = message, Data = (object)null });

        #endregion

        #region Debug Helpers

        protected Dictionary<string, string> GetAllClaims() =>
            User.Claims.ToDictionary(c => c.Type, c => c.Value);

        #endregion
    }
}