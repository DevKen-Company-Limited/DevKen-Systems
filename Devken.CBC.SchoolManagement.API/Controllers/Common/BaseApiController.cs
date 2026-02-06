using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

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

        /// <summary>
        /// Gets the current authenticated user's ID from JWT claims
        /// </summary>
        protected Guid CurrentUserId
        {
            get
            {
                var userIdClaim = User.FindFirst("sub")?.Value
                               ?? User.FindFirst("user_id")?.Value
                               ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdClaim, out var userId))
                    throw new UnauthorizedAccessException("User ID not found or invalid in token");

                return userId;
            }
        }

        /// <summary>
        /// Gets the current user's school/tenant ID. Returns null for SuperAdmin.
        /// </summary>
        protected Guid? CurrentTenantId
        {
            get
            {
                if (IsSuperAdmin) return null;

                var tenantIdClaim = User.FindFirst("tenant_id")?.Value
                                 ?? User.FindFirst("school_id")?.Value;

                if (Guid.TryParse(tenantIdClaim, out var tenantId))
                    return tenantId;

                // Non-SuperAdmin users must have a tenant context
                throw new UnauthorizedAccessException("Tenant context is required for this user");
            }
        }

        /// <summary>
        /// Gets the current user's school ID (alias for CurrentTenantId for clarity)
        /// </summary>
        protected Guid? CurrentSchoolId => CurrentTenantId;

        /// <summary>
        /// Checks if the current user is a SuperAdmin
        /// </summary>
        protected bool IsSuperAdmin =>
            string.Equals(User.FindFirst("is_super_admin")?.Value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(User.FindFirst("is_superadmin")?.Value, "true", StringComparison.OrdinalIgnoreCase)
            || User.FindAll(ClaimTypes.Role)
                   .Any(r => string.Equals(r.Value, "SuperAdmin", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Gets the current user's email address
        /// </summary>
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

        /// <summary>
        /// Gets the current user's full name
        /// </summary>
        protected string CurrentUserName
        {
            get
            {
                return User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("name")?.Value
                    ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value
                    ?? User.Identity?.Name
                    ?? "Unknown";
            }
        }

        /// <summary>
        /// Gets all permissions assigned to the current user
        /// </summary>
        protected IReadOnlyCollection<string> CurrentUserPermissions
        {
            get
            {
                var permissions = new List<string>();

                // Check for permissions claim (array)
                var permissionsClaim = User.FindFirst("permissions")?.Value;
                if (!string.IsNullOrWhiteSpace(permissionsClaim))
                {
                    // If it's a JSON array, try to parse it
                    try
                    {
                        var permArray = System.Text.Json.JsonSerializer.Deserialize<string[]>(permissionsClaim);
                        if (permArray != null)
                            permissions.AddRange(permArray);
                    }
                    catch
                    {
                        // If not JSON, treat as single permission
                        permissions.Add(permissionsClaim);
                    }
                }

                // Also check for individual permission claims
                permissions.AddRange(
                    User.FindAll("permission")
                        .Select(c => c.Value)
                        .Where(v => !string.IsNullOrWhiteSpace(v))
                );

                return permissions
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }

        /// <summary>
        /// Gets all roles assigned to the current user
        /// </summary>
        protected IReadOnlyCollection<string> CurrentUserRoles =>
            User.FindAll(ClaimTypes.Role)
                .Concat(User.FindAll("http://schemas.microsoft.com/ws/2008/06/identity/claims/role"))
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        #endregion

        #region Permission & Role Helpers

        /// <summary>
        /// Checks if the current user has a specific permission.
        /// SuperAdmin always returns true.
        /// </summary>
        /// <param name="permission">The permission to check (e.g., "School.Read")</param>
        /// <returns>True if user has the permission or is SuperAdmin</returns>
        protected bool HasPermission(string permission)
        {
            if (IsSuperAdmin) return true;
            return CurrentUserPermissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if the current user has ANY of the specified permissions.
        /// SuperAdmin always returns true.
        /// </summary>
        /// <param name="permissions">List of permissions to check</param>
        /// <returns>True if user has at least one permission or is SuperAdmin</returns>
        protected bool HasAnyPermission(params string[] permissions)
        {
            if (IsSuperAdmin) return true;
            return permissions.Any(p => CurrentUserPermissions.Contains(p, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if the current user has ALL of the specified permissions.
        /// SuperAdmin always returns true.
        /// </summary>
        /// <param name="permissions">List of permissions to check</param>
        /// <returns>True if user has all permissions or is SuperAdmin</returns>
        protected bool HasAllPermissions(params string[] permissions)
        {
            if (IsSuperAdmin) return true;
            return permissions.All(p => CurrentUserPermissions.Contains(p, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if the current user has a specific role
        /// </summary>
        /// <param name="role">The role to check (e.g., "Teacher", "SchoolAdmin")</param>
        /// <returns>True if user has the role</returns>
        protected bool HasRole(string role)
        {
            return CurrentUserRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if the current user has ANY of the specified roles
        /// </summary>
        /// <param name="roles">List of roles to check</param>
        /// <returns>True if user has at least one role</returns>
        protected bool HasAnyRole(params string[] roles)
        {
            return roles.Any(role => CurrentUserRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if the current user has ALL of the specified roles
        /// </summary>
        /// <param name="roles">List of roles to check</param>
        /// <returns>True if user has all roles</returns>
        protected bool HasAllRoles(params string[] roles)
        {
            return roles.All(role => CurrentUserRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if the current user can access a specific school.
        /// SuperAdmin can access any school.
        /// Other users can only access their assigned school.
        /// </summary>
        /// <param name="schoolId">The school ID to check access for</param>
        /// <returns>True if user can access the school</returns>
        protected bool CanAccessSchool(Guid schoolId)
        {
            if (IsSuperAdmin) return true;
            return CurrentSchoolId == schoolId;
        }

        /// <summary>
        /// Gets the current user's school ID or throws an exception if not found.
        /// Use this when a school context is absolutely required.
        /// </summary>
        /// <returns>The school ID</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when school ID is not found</exception>
        protected Guid GetCurrentUserSchoolId()
        {
            var schoolId = CurrentSchoolId;
            if (schoolId == null)
                throw new UnauthorizedAccessException("School context is required for this operation");
            return schoolId.Value;
        }

        /// <summary>
        /// Validates that the user can access the specified school.
        /// Returns a ForbiddenResponse if access is denied.
        /// </summary>
        /// <param name="schoolId">The school ID to validate access for</param>
        /// <param name="customMessage">Optional custom error message</param>
        /// <returns>Null if access is allowed, ForbiddenResponse if denied</returns>
        protected IActionResult? ValidateSchoolAccess(Guid schoolId, string? customMessage = null)
        {
            if (!CanAccessSchool(schoolId))
            {
                return ForbiddenResponse(customMessage ?? "You do not have access to this school.");
            }
            return null;
        }

        #endregion

        #region User Activity Logging

        /// <summary>
        /// Logs an activity for the current user
        /// </summary>
        /// <param name="activityType">Type of activity (e.g., "school.update", "student.create")</param>
        /// <param name="details">Optional details about the activity</param>
        protected async Task LogUserActivityAsync(string activityType, string? details = null)
        {
            if (_activityService == null) return;

            try
            {
                await _activityService.LogActivityAsync(CurrentUserId, CurrentTenantId, activityType, details);
            }
            catch (Exception)
            {
                // Silently fail - logging shouldn't break the main operation
            }
        }

        /// <summary>
        /// Logs an activity for a specific user
        /// </summary>
        /// <param name="userId">The user ID to log for</param>
        /// <param name="tenantId">The tenant/school ID</param>
        /// <param name="activityType">Type of activity</param>
        /// <param name="details">Optional details</param>
        protected async Task LogUserActivityAsync(Guid userId, Guid? tenantId, string activityType, string? details = null)
        {
            if (_activityService == null) return;

            try
            {
                await _activityService.LogActivityAsync(userId, tenantId, activityType, details);
            }
            catch (Exception)
            {
                // Silently fail - logging shouldn't break the main operation
            }
        }

        #endregion

        #region Standardized Responses

        /// <summary>
        /// Returns a successful response with data
        /// </summary>
        protected IActionResult SuccessResponse<T>(T data, string message = "Success")
        {
            return Ok(new
            {
                success = true,
                message,
                data
            });
        }

        /// <summary>
        /// Returns a successful response without data
        /// </summary>
        protected IActionResult SuccessResponse(string message = "Success")
        {
            return Ok(new
            {
                success = true,
                message,
                data = (object?)null
            });
        }

        /// <summary>
        /// Returns an error response with custom status code
        /// </summary>
        protected IActionResult ErrorResponse(string message, int statusCode = 400)
        {
            return StatusCode(statusCode, new
            {
                success = false,
                message,
                data = (object?)null
            });
        }

        /// <summary>
        /// Returns a validation error response with field-specific errors
        /// </summary>
        protected IActionResult ValidationErrorResponse(IDictionary<string, string[]> errors)
        {
            return BadRequest(new
            {
                success = false,
                message = "Validation failed",
                errors
            });
        }

        /// <summary>
        /// Returns a 404 Not Found response
        /// </summary>
        protected IActionResult NotFoundResponse(string message = "Resource not found")
        {
            return NotFound(new
            {
                success = false,
                message
            });
        }

        /// <summary>
        /// Returns a 403 Forbidden response
        /// </summary>
        protected IActionResult ForbiddenResponse(string message = "Access forbidden")
        {
            return StatusCode(403, new
            {
                success = false,
                message
            });
        }

        /// <summary>
        /// Returns a 401 Unauthorized response
        /// </summary>
        protected IActionResult UnauthorizedResponse(string message = "Unauthorized access")
        {
            return Unauthorized(new
            {
                success = false,
                message
            });
        }

        /// <summary>
        /// Returns a 409 Conflict response
        /// </summary>
        protected IActionResult ConflictResponse(string message = "Resource conflict")
        {
            return StatusCode(409, new
            {
                success = false,
                message
            });
        }

        /// <summary>
        /// Returns a 201 Created response with location header
        /// </summary>
        protected IActionResult CreatedResponse<T>(string location, T data, string message = "Resource created")
        {
            return Created(location, new
            {
                success = true,
                message,
                data
            });
        }

        #endregion
    }
}