using Devken.CBC.SchoolManagement.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Common
{
    [ApiController]
    [Produces("application/json")]
    public abstract class BaseApiController : ControllerBase
    {
        /// <summary>
        /// Gets the current user's ID from the JWT token
        /// </summary>
        protected Guid CurrentUserId
        {
            get
            {
                // Try custom claim first (tenant users)
                var userIdClaim = User.FindFirst("user_id")?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    throw new UnauthorizedAccessException("User ID not found in token");
                }
                return userId;
            }
        }

        /// <summary>
        /// Gets the current tenant ID from the JWT token (null for SuperAdmin)
        /// </summary>
        protected Guid? CurrentTenantId
        {
            get
            {
                var tenantIdClaim = User.FindFirst("tenant_id")?.Value;

                // SuperAdmin won't have tenant_id
                if (string.IsNullOrEmpty(tenantIdClaim))
                {
                    return null;
                }

                if (Guid.TryParse(tenantIdClaim, out var tenantId))
                {
                    return tenantId;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the current user's email from the JWT token
        /// </summary>
        protected string CurrentUserEmail
        {
            get
            {
                var email = User.FindFirst(ClaimTypes.Email)?.Value
                    ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

                if (string.IsNullOrEmpty(email))
                {
                    throw new UnauthorizedAccessException("Email not found in token");
                }
                return email;
            }
        }

        /// <summary>
        /// Gets the current user's name from the JWT token
        /// </summary>
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

        /// <summary>
        /// Gets all permissions for the current user from the JWT token
        /// </summary>
        protected IEnumerable<string> CurrentUserPermissions
        {
            get
            {
                // Get all permission claims (the claim type is "permissions" from JwtService)
                var permissions = User.FindAll("permissions")
                    .Select(c => c.Value)
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Distinct()
                    .ToList();

                return permissions;
            }
        }

        /// <summary>
        /// Gets all roles for the current user from the JWT token
        /// </summary>
        protected IEnumerable<string> CurrentUserRoles
        {
            get
            {
                return User.FindAll(ClaimTypes.Role)
                    .Select(c => c.Value)
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Distinct()
                    .ToList();
            }
        }

        /// <summary>
        /// Checks if the current user is a SuperAdmin
        /// </summary>
        protected bool IsSuperAdmin
        {
            get
            {
                var isSuperAdminClaim = User.FindFirst("is_super_admin")?.Value;
                return isSuperAdminClaim?.ToLower() == "true"
                    || CurrentUserRoles.Contains("SuperAdmin");
            }
        }

        /// <summary>
        /// Checks if the user has a specific permission
        /// </summary>
        protected bool HasPermission(string permission)
        {
            if (string.IsNullOrWhiteSpace(permission))
                return false;

            // SuperAdmin has all permissions
            if (IsSuperAdmin)
                return true;

            return CurrentUserPermissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if the user has all specified permissions
        /// </summary>
        protected bool HasAllPermissions(params string[] permissions)
        {
            if (permissions == null || permissions.Length == 0)
                return true;

            // SuperAdmin has all permissions
            if (IsSuperAdmin)
                return true;

            var userPermissions = new HashSet<string>(
                CurrentUserPermissions,
                StringComparer.OrdinalIgnoreCase
            );

            return permissions.All(p => userPermissions.Contains(p));
        }

        /// <summary>
        /// Checks if the user has any of the specified permissions
        /// </summary>
        protected bool HasAnyPermission(params string[] permissions)
        {
            if (permissions == null || permissions.Length == 0)
                return false;

            // SuperAdmin has all permissions
            if (IsSuperAdmin)
                return true;

            var userPermissions = new HashSet<string>(
                CurrentUserPermissions,
                StringComparer.OrdinalIgnoreCase
            );

            return permissions.Any(p => userPermissions.Contains(p));
        }

        /// <summary>
        /// Checks if the user has a specific role
        /// </summary>
        protected bool HasRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return false;

            return CurrentUserRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns a standardized success response
        /// </summary>
        protected IActionResult SuccessResponse<T>(T data, string message = "Success")
        {
            return Ok(new
            {
                Success = true,
                Message = message,
                Data = data
            });
        }

        /// <summary>
        /// Returns a standardized error response
        /// </summary>
        protected IActionResult ErrorResponse(string message, int statusCode = 400)
        {
            return StatusCode(statusCode, new
            {
                Success = false,
                Message = message,
                Data = (object)null
            });
        }

        /// <summary>
        /// Returns a standardized validation error response
        /// </summary>
        protected IActionResult ValidationErrorResponse(IDictionary<string, string[]> errors)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors,
                Data = (object)null
            });
        }

        /// <summary>
        /// Returns a not found response
        /// </summary>
        protected IActionResult NotFoundResponse(string message = "Resource not found")
        {
            return NotFound(new
            {
                Success = false,
                Message = message,
                Data = (object)null
            });
        }

        /// <summary>
        /// Returns an unauthorized response
        /// </summary>
        protected IActionResult UnauthorizedResponse(string message = "Unauthorized access")
        {
            return Unauthorized(new
            {
                Success = false,
                Message = message,
                Data = (object)null
            });
        }

        /// <summary>
        /// Returns a forbidden response
        /// </summary>
        protected IActionResult ForbiddenResponse(string message = "Access forbidden")
        {
            return StatusCode(403, new
            {
                Success = false,
                Message = message,
                Data = (object)null
            });
        }

        /// <summary>
        /// Gets all claims for debugging purposes
        /// </summary>
        protected Dictionary<string, string> GetAllClaims()
        {
            return User.Claims.ToDictionary(
                c => c.Type,
                c => c.Value
            );
        }
    }
}