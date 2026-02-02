using Devken.CBC.SchoolManagement.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Common
{
    [ApiController]
    [Produces("application/json")]
    public abstract class BaseApiController : ControllerBase
    {
        protected Guid CurrentUserId
        {
            get
            {
                var userIdClaim = User.FindFirst(CustomClaimTypes.UserId)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    throw new UnauthorizedAccessException("User ID not found in token");
                }
                return userId;
            }
        }

        protected Guid CurrentTenantId
        {
            get
            {
                var tenantIdClaim = User.FindFirst(CustomClaimTypes.TenantId)?.Value;
                if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
                {
                    throw new UnauthorizedAccessException("Tenant ID not found in token");
                }
                return tenantId;
            }
        }

        protected string CurrentUserEmail
        {
            get
            {
                var email = User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(email))
                {
                    throw new UnauthorizedAccessException("Email not found in token");
                }
                return email;
            }
        }

        protected string CurrentUserName
        {
            get
            {
                var name = User.FindFirst(ClaimTypes.Name)?.Value;
                return name ?? "Unknown";
            }
        }

        protected IEnumerable<string> CurrentUserPermissions
        {
            get
            {
                return User.FindAll(CustomClaimTypes.Permissions)
                    .Select(c => c.Value)
                    .Distinct();
            }
        }

        protected bool HasPermission(string permission)
        {
            return CurrentUserPermissions.Contains(permission);
        }

        protected bool HasAllPermissions(params string[] permissions)
        {
            var userPermissions = CurrentUserPermissions.ToHashSet();
            return permissions.All(p => userPermissions.Contains(p));
        }

        protected bool HasAnyPermission(params string[] permissions)
        {
            var userPermissions = CurrentUserPermissions.ToHashSet();
            return permissions.Any(p => userPermissions.Contains(p));
        }

        protected IActionResult SuccessResponse<T>(T data, string message = "Success")
        {
            return Ok(new
            {
                Success = true,
                Message = message,
                Data = data
            });
        }

        protected IActionResult ErrorResponse(string message, int statusCode = 400)
        {
            return StatusCode(statusCode, new
            {
                Success = false,
                Message = message
            });
        }

        protected IActionResult ValidationErrorResponse(IDictionary<string, string[]> errors)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors
            });
        }
    }
}
