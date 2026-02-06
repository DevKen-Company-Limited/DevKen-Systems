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

                if (!Guid.TryParse(userIdClaim, out var userId))
                    throw new UnauthorizedAccessException("User ID not found or invalid in token");

                return userId;
            }
        }

        protected Guid? CurrentTenantId
        {
            get
            {
                if (IsSuperAdmin) return null;

                var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
                if (Guid.TryParse(tenantIdClaim, out var tenantId)) return tenantId;

                throw new UnauthorizedAccessException("Tenant context is required");
            }
        }

        protected bool IsSuperAdmin =>
            string.Equals(User.FindFirst("is_superadmin")?.Value, "true", StringComparison.OrdinalIgnoreCase)
            || User.FindAll(ClaimTypes.Role)
                   .Any(r => string.Equals(r.Value, "SuperAdmin", StringComparison.OrdinalIgnoreCase));

        protected string CurrentUserEmail
        {
            get
            {
                var email = User.FindFirst(ClaimTypes.Email)?.Value
                         ?? User.FindFirst("email")?.Value
                         ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

                if (string.IsNullOrWhiteSpace(email))
                    throw new UnauthorizedAccessException("Email not found in token");

                return email;
            }
        }

        protected string CurrentUserName
        {
            get
            {
                return User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("name")?.Value
                    ?? User.Identity?.Name
                    ?? "Unknown";
            }
        }

        protected IReadOnlyCollection<string> CurrentUserPermissions =>
            User.FindAll("permission")
                .Concat(User.FindAll("permissions"))
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        protected IReadOnlyCollection<string> CurrentUserRoles =>
            User.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        #endregion

        #region Permission Helpers

        protected bool HasPermission(string permission) =>
            IsSuperAdmin || CurrentUserPermissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

        protected bool HasAnyPermission(params string[] permissions) =>
            IsSuperAdmin || permissions.Any(p => CurrentUserPermissions.Contains(p, StringComparer.OrdinalIgnoreCase));

        protected bool HasRole(string role) =>
            CurrentUserRoles.Contains(role, StringComparer.OrdinalIgnoreCase);

        #endregion

        #region User Activity Logging

        protected async Task LogUserActivityAsync(string activityType, string? details = null)
        {
            if (_activityService == null) return;
            await _activityService.LogActivityAsync(CurrentUserId, CurrentTenantId, activityType, details);
        }

        protected async Task LogUserActivityAsync(Guid userId, Guid? tenantId, string activityType, string? details = null)
        {
            if (_activityService == null) return;
            await _activityService.LogActivityAsync(userId, tenantId, activityType, details);
        }

        #endregion

        #region Standardized Responses

        protected IActionResult SuccessResponse<T>(T data, string message = "Success") =>
            Ok(new { success = true, message, data });

        protected IActionResult ErrorResponse(string message, int statusCode = 400) =>
            StatusCode(statusCode, new { success = false, message, data = (object?)null });

        protected IActionResult ValidationErrorResponse(IDictionary<string, string[]> errors) =>
            BadRequest(new { success = false, message = "Validation failed", errors });

        protected IActionResult NotFoundResponse(string message = "Resource not found") =>
            NotFound(new { success = false, message });

        protected IActionResult ForbiddenResponse(string message = "Access forbidden") =>
            StatusCode(403, new { success = false, message });

        #endregion
    }
}
