using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Common
{
    /// <summary>
    /// Base controller providing common functionality for all API controllers
    /// including user context, permission checks, and standardized responses
    /// </summary>
    [ApiController]
    [Produces("application/json")]
    public abstract class BaseApiController : ControllerBase
    {
        private readonly IUserActivityService? _activityService;
        private readonly ILogger? _logger;

        protected BaseApiController(
            IUserActivityService? activityService = null,
            ILogger? logger = null)
        {
            _activityService = activityService;
            _logger = logger;
        }

        #region Current User Info

        /// <summary>
        /// Gets the current authenticated user's ID from JWT claims
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Thrown when user ID is not found or invalid</exception>
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
        /// <exception cref="UnauthorizedAccessException">Thrown when tenant context is required but not found</exception>
        protected Guid? CurrentTenantId
        {
            get
            {
                if (IsSuperAdmin) return null;

                var tenantIdClaim = User.FindFirst("tenant_id")?.Value
                                 ?? User.FindFirst("school_id")?.Value
                                 ?? User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/tenantid")?.Value;

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
        /// Checks if the current user is a SuperAdmin.
        /// SuperAdmin has unrestricted access to all resources across all tenants.
        /// </summary>
        protected bool IsSuperAdmin
        {
            get
            {
                // Check is_super_admin claim
                var isSuperAdminClaim = User.FindFirst("is_super_admin")?.Value
                                     ?? User.FindFirst("is_superadmin")?.Value
                                     ?? User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/is_super_admin")?.Value;

                if (bool.TryParse(isSuperAdminClaim, out var isSuperAdmin) && isSuperAdmin)
                    return true;

                // Check roles
                return User.FindAll(ClaimTypes.Role)
                       .Any(r => string.Equals(r.Value, "SuperAdmin", StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Gets the current user's email address
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Thrown when email is not found in token</exception>
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
                    ?? User.FindFirst("full_name")?.Value
                    ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value
                    ?? User.Identity?.Name
                    ?? "Unknown";
            }
        }

        /// <summary>
        /// Gets the current user's first name
        /// </summary>
        protected string? CurrentUserFirstName =>
            User.FindFirst("first_name")?.Value
            ?? User.FindFirst("given_name")?.Value
            ?? User.FindFirst(ClaimTypes.GivenName)?.Value;

        /// <summary>
        /// Gets the current user's last name
        /// </summary>
        protected string? CurrentUserLastName =>
            User.FindFirst("last_name")?.Value
            ?? User.FindFirst("family_name")?.Value
            ?? User.FindFirst(ClaimTypes.Surname)?.Value;

        /// <summary>
        /// Gets all permissions assigned to the current user.
        /// Parses permissions from both individual claims and JSON array claims.
        /// </summary>
        protected IReadOnlyCollection<string> CurrentUserPermissions
        {
            get
            {
                var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Check for permissions claim (JSON array)
                var permissionsClaim = User.FindFirst("permissions")?.Value;
                if (!string.IsNullOrWhiteSpace(permissionsClaim))
                {
                    try
                    {
                        var permArray = JsonSerializer.Deserialize<string[]>(permissionsClaim);
                        if (permArray != null)
                        {
                            foreach (var perm in permArray)
                            {
                                if (!string.IsNullOrWhiteSpace(perm))
                                    permissions.Add(perm);
                            }
                        }
                    }
                    catch
                    {
                        // If not JSON, treat as comma-separated list
                        var permList = permissionsClaim.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var perm in permList)
                        {
                            if (!string.IsNullOrWhiteSpace(perm))
                                permissions.Add(perm.Trim());
                        }
                    }
                }

                // Also check for individual permission claims
                var individualPermissions = User.FindAll("permission")
                    .Concat(User.FindAll("http://schemas.microsoft.com/ws/2008/06/identity/claims/permission"))
                    .Select(c => c.Value)
                    .Where(v => !string.IsNullOrWhiteSpace(v));

                foreach (var perm in individualPermissions)
                {
                    permissions.Add(perm);
                }

                return permissions.ToList();
            }
        }

        /// <summary>
        /// Gets all roles assigned to the current user
        /// </summary>
        protected IReadOnlyCollection<string> CurrentUserRoles
        {
            get
            {
                var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Get roles from standard claims
                var roleClaims = User.FindAll(ClaimTypes.Role)
                    .Concat(User.FindAll("http://schemas.microsoft.com/ws/2008/06/identity/claims/role"))
                    .Concat(User.FindAll("role"))
                    .Select(c => c.Value)
                    .Where(v => !string.IsNullOrWhiteSpace(v));

                foreach (var role in roleClaims)
                {
                    roles.Add(role);
                }

                return roles.ToList();
            }
        }

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
            if (string.IsNullOrWhiteSpace(permission))
                return false;

            if (IsSuperAdmin)
                return true;

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
            if (permissions == null || permissions.Length == 0)
                return false;

            if (IsSuperAdmin)
                return true;

            return permissions.Any(p =>
                !string.IsNullOrWhiteSpace(p) &&
                CurrentUserPermissions.Contains(p, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if the current user has ALL of the specified permissions.
        /// SuperAdmin always returns true.
        /// </summary>
        /// <param name="permissions">List of permissions to check</param>
        /// <returns>True if user has all permissions or is SuperAdmin</returns>
        protected bool HasAllPermissions(params string[] permissions)
        {
            if (permissions == null || permissions.Length == 0)
                return true;

            if (IsSuperAdmin)
                return true;

            return permissions.All(p =>
                !string.IsNullOrWhiteSpace(p) &&
                CurrentUserPermissions.Contains(p, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if the current user has a specific role
        /// </summary>
        /// <param name="role">The role to check (e.g., "Teacher", "SchoolAdmin")</param>
        /// <returns>True if user has the role</returns>
        protected bool HasRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return false;

            return CurrentUserRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if the current user has ANY of the specified roles
        /// </summary>
        /// <param name="roles">List of roles to check</param>
        /// <returns>True if user has at least one role</returns>
        protected bool HasAnyRole(params string[] roles)
        {
            if (roles == null || roles.Length == 0)
                return false;

            return roles.Any(role =>
                !string.IsNullOrWhiteSpace(role) &&
                CurrentUserRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if the current user has ALL of the specified roles
        /// </summary>
        /// <param name="roles">List of roles to check</param>
        /// <returns>True if user has all roles</returns>
        protected bool HasAllRoles(params string[] roles)
        {
            if (roles == null || roles.Length == 0)
                return true;

            return roles.All(role =>
                !string.IsNullOrWhiteSpace(role) &&
                CurrentUserRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
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
            if (IsSuperAdmin)
                return true;

            return CurrentSchoolId.HasValue && CurrentSchoolId.Value == schoolId;
        }

        /// <summary>
        /// Checks if the current user can access a specific tenant/school.
        /// Handles null tenant ID (system-wide resources).
        /// </summary>
        /// <param name="tenantId">The tenant ID to check (null for system resources)</param>
        /// <returns>True if user can access the tenant</returns>
        protected bool CanAccessTenant(Guid? tenantId)
        {
            if (IsSuperAdmin)
                return true;

            // Null tenant means system-wide resource (accessible to SuperAdmin only)
            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
                return false;

            return CurrentTenantId.HasValue && CurrentTenantId.Value == tenantId.Value;
        }

        /// <summary>
        /// Gets the current user's school ID or throws an exception if not found.
        /// Use this when a school context is absolutely required.
        /// WARNING: This will throw for SuperAdmin users. Use GetUserSchoolIdOrNull() instead
        /// when you need to support SuperAdmin access.
        /// </summary>
        /// <returns>The school ID</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when school ID is not found</exception>
        protected Guid GetCurrentUserSchoolId()
        {
            var schoolId = CurrentSchoolId;
            if (!schoolId.HasValue)
                throw new UnauthorizedAccessException("School context is required for this operation");
            return schoolId.Value;
        }

        /// <summary>
        /// Gets the current user's school ID or throws an exception if not found.
        /// Alias for GetCurrentUserSchoolId for consistency.
        /// WARNING: This will throw for SuperAdmin users. Use GetUserSchoolIdOrNull() instead
        /// when you need to support SuperAdmin access.
        /// </summary>
        /// <returns>The school ID</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when school ID is not found</exception>
        protected Guid GetRequiredSchoolId()
        {
            return GetCurrentUserSchoolId();
        }

        /// <summary>
        /// Gets the current user's tenant ID or throws an exception if not found.
        /// Use this when a tenant context is absolutely required.
        /// WARNING: This will throw for SuperAdmin users. Use GetUserSchoolIdOrNull() instead
        /// when you need to support SuperAdmin access.
        /// </summary>
        /// <returns>The tenant ID</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when tenant ID is not found</exception>
        protected Guid GetCurrentUserTenantId()
        {
            var tenantId = CurrentTenantId;
            if (!tenantId.HasValue)
                throw new UnauthorizedAccessException("Tenant context is required for this operation");
            return tenantId.Value;
        }

        /// <summary>
        /// Gets the current user's tenant ID or throws an exception if not found.
        /// Alias for GetCurrentUserTenantId for consistency.
        /// WARNING: This will throw for SuperAdmin users. Use GetUserSchoolIdOrNull() instead
        /// when you need to support SuperAdmin access.
        /// </summary>
        /// <returns>The tenant ID</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when tenant ID is not found</exception>
        protected Guid GetRequiredTenantId()
        {
            return GetCurrentUserTenantId();
        }

        /// <summary>
        /// Gets the current user's school ID for non-SuperAdmins, or null for SuperAdmins.
        /// This is the RECOMMENDED method to use when passing userSchoolId to service layers
        /// that need to support both SuperAdmin and regular user access.
        /// Does NOT validate that non-SuperAdmin users have a school - returns null if missing.
        /// </summary>
        /// <returns>School ID for regular users, null for SuperAdmin or users without school context</returns>
        protected Guid? GetUserSchoolIdOrNull()
        {
            return IsSuperAdmin ? null : CurrentSchoolId;
        }

        /// <summary>
        /// Gets the current user's school ID for non-SuperAdmins, or null for SuperAdmins.
        /// This is the RECOMMENDED method to use when passing userSchoolId to service layers.
        /// VALIDATES that non-SuperAdmin users have a valid school context.
        /// Use this in most controller methods to ensure proper access control.
        /// </summary>
        /// <returns>School ID for regular users (validated), null for SuperAdmin</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when non-SuperAdmin has no school</exception>
        protected Guid? GetUserSchoolIdOrNullWithValidation()
        {
            if (IsSuperAdmin)
                return null;

            var schoolId = CurrentSchoolId;
            if (!schoolId.HasValue || schoolId.Value == Guid.Empty)
                throw new UnauthorizedAccessException("User is not associated with any school.");

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
                LogUserAuthorization("School.Access", schoolId.ToString(), false, "School access denied");
                return ForbiddenResponse(customMessage ?? "You do not have access to this school.");
            }
            return null;
        }

        /// <summary>
        /// Validates that the user can access the specified tenant.
        /// Returns a ForbiddenResponse if access is denied.
        /// </summary>
        /// <param name="tenantId">The tenant ID to validate access for</param>
        /// <param name="customMessage">Optional custom error message</param>
        /// <returns>Null if access is allowed, ForbiddenResponse if denied</returns>
        protected IActionResult? ValidateTenantAccess(Guid? tenantId, string? customMessage = null)
        {
            if (!CanAccessTenant(tenantId))
            {
                LogUserAuthorization("Tenant.Access", tenantId?.ToString() ?? "NULL", false, "Tenant access denied");
                return ForbiddenResponse(customMessage ?? "You do not have access to this resource.");
            }
            return null;
        }

        /// <summary>
        /// Validates that the user has the required permission.
        /// Returns a ForbiddenResponse if permission is denied.
        /// </summary>
        /// <param name="permission">The permission to check</param>
        /// <param name="resourceId">Optional resource ID being accessed</param>
        /// <param name="customMessage">Optional custom error message</param>
        /// <returns>Null if permission is granted, ForbiddenResponse if denied</returns>
        protected IActionResult? ValidatePermission(string permission, string? resourceId = null, string? customMessage = null)
        {
            if (!HasPermission(permission))
            {
                LogUserAuthorization(permission, resourceId, false, "Permission denied");
                return ForbiddenResponse(customMessage ?? $"You do not have permission: {permission}");
            }
            return null;
        }

        /// <summary>
        /// Validates that the user has at least one of the required permissions.
        /// Returns a ForbiddenResponse if all permissions are denied.
        /// </summary>
        /// <param name="permissions">The permissions to check</param>
        /// <param name="resourceId">Optional resource ID being accessed</param>
        /// <param name="customMessage">Optional custom error message</param>
        /// <returns>Null if any permission is granted, ForbiddenResponse if all denied</returns>
        protected IActionResult? ValidateAnyPermission(string[] permissions, string? resourceId = null, string? customMessage = null)
        {
            if (!HasAnyPermission(permissions))
            {
                var permList = string.Join(", ", permissions);
                LogUserAuthorization($"Any of: {permList}", resourceId, false, "All permissions denied");
                return ForbiddenResponse(customMessage ?? "You do not have the required permissions.");
            }
            return null;
        }

        #endregion

        #region User Activity & Authorization Logging

        /// <summary>
        /// Logs an authorization attempt for the current user
        /// </summary>
        /// <param name="action">The action being authorized (e.g., "School.Read", "Student.Create")</param>
        /// <param name="resourceId">Optional resource ID being accessed</param>
        /// <param name="granted">Whether authorization was granted</param>
        /// <param name="reason">Optional reason for denial</param>
        protected void LogUserAuthorization(string action, string? resourceId = null, bool granted = true, string? reason = null)
        {
            try
            {
                if (_logger != null && _logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "Authorization {Status} - User: {UserId}, Email: {Email}, Action: {Action}, Resource: {ResourceId}, Reason: {Reason}",
                        granted ? "GRANTED" : "DENIED",
                        CurrentUserId,
                        CurrentUserEmail,
                        action,
                        resourceId ?? "N/A",
                        reason ?? (granted ? "Authorized" : "Unauthorized")
                    );
                }
            }
            catch (Exception ex)
            {
                // Silently fail - logging shouldn't break the main operation
                _logger?.LogWarning(ex, "Failed to log authorization event");
            }
        }

        /// <summary>
        /// Logs an activity for the current user
        /// </summary>
        /// <param name="activityType">Type of activity (e.g., "school.update", "student.create")</param>
        /// <param name="details">Optional details about the activity</param>
        protected async Task LogUserActivityAsync(string activityType, string? details = null)
        {
            if (_activityService == null)
                return;

            try
            {
                await _activityService.LogActivityAsync(
                    CurrentUserId,
                    CurrentTenantId,
                    activityType,
                    details);
            }
            catch (Exception ex)
            {
                // Silently fail - logging shouldn't break the main operation
                _logger?.LogWarning(ex, "Failed to log user activity: {ActivityType}", activityType);
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
            if (_activityService == null)
                return;

            try
            {
                await _activityService.LogActivityAsync(userId, tenantId, activityType, details);
            }
            catch (Exception ex)
            {
                // Silently fail - logging shouldn't break the main operation
                _logger?.LogWarning(ex, "Failed to log user activity for user {UserId}: {ActivityType}", userId, activityType);
            }
        }

        #endregion

        #region Standardized Responses

        /// <summary>
        /// Returns a successful response with data
        /// </summary>
        /// <typeparam name="T">Type of data being returned</typeparam>
        /// <param name="data">The data to return</param>
        /// <param name="message">Success message</param>
        /// <returns>200 OK response with data</returns>
        protected IActionResult SuccessResponse<T>(T data, string message = "Success")
        {
            return Ok(new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                StatusCode = 200
            });
        }

        /// <summary>
        /// Returns a successful response without data
        /// </summary>
        /// <param name="message">Success message</param>
        /// <returns>200 OK response</returns>
        protected IActionResult SuccessResponse(string message = "Success")
        {
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = message,
                Data = null,
                StatusCode = 200
            });
        }

        /// <summary>
        /// Returns a 201 Created response
        /// </summary>
        /// <typeparam name="T">Type of created resource</typeparam>
        /// <param name="data">The created resource data</param>
        /// <param name="message">Success message</param>
        /// <returns>201 Created response</returns>
        protected IActionResult CreatedResponse<T>(T data, string message = "Resource created successfully")
        {
            return StatusCode(201, new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                StatusCode = 201
            });
        }

        /// <summary>
        /// Returns a 201 Created response with location header
        /// </summary>
        /// <typeparam name="T">Type of created resource</typeparam>
        /// <param name="location">URI of the created resource</param>
        /// <param name="data">The created resource data</param>
        /// <param name="message">Success message</param>
        /// <returns>201 Created response</returns>
        protected IActionResult CreatedResponse<T>(string location, T data, string message = "Resource created successfully")
        {
            Response.Headers.Add("Location", location);
            return StatusCode(201, new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                StatusCode = 201
            });
        }

        /// <summary>
        /// Returns an error response with custom status code
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="statusCode">HTTP status code (default: 400)</param>
        /// <returns>Error response with specified status code</returns>
        protected IActionResult ErrorResponse(string message, int statusCode = 400)
        {
            return StatusCode(statusCode, new ApiResponse<object>
            {
                Success = false,
                Message = message,
                Data = null,
                StatusCode = statusCode
            });
        }

        /// <summary>
        /// Returns a validation error response with ModelState errors
        /// </summary>
        /// <param name="modelState">ModelState dictionary containing validation errors</param>
        /// <returns>400 Bad Request with validation errors</returns>
        protected IActionResult ValidationErrorResponse(ModelStateDictionary modelState)
        {
            var errors = modelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );

            return BadRequest(new ApiValidationResponse
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors,
                StatusCode = 400
            });
        }

        /// <summary>
        /// Returns a validation error response with field-specific errors
        /// </summary>
        /// <param name="errors">Dictionary of field names to error messages</param>
        /// <returns>400 Bad Request with validation errors</returns>
        protected IActionResult ValidationErrorResponse(IDictionary<string, string[]> errors)
        {
            return BadRequest(new ApiValidationResponse
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors,
                StatusCode = 400
            });
        }

        /// <summary>
        /// Returns a validation error response with a single error message
        /// </summary>
        /// <param name="message">Validation error message</param>
        /// <returns>400 Bad Request with validation error</returns>
        protected IActionResult ValidationErrorResponse(string message)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = message,
                Data = null,
                StatusCode = 400
            });
        }

        /// <summary>
        /// Returns a 404 Not Found response
        /// </summary>
        /// <param name="message">Error message</param>
        /// <returns>404 Not Found response</returns>
        protected IActionResult NotFoundResponse(string message = "Resource not found")
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = message,
                Data = null,
                StatusCode = 404
            });
        }

        /// <summary>
        /// Returns a 403 Forbidden response
        /// </summary>
        /// <param name="message">Error message</param>
        /// <returns>403 Forbidden response</returns>
        protected IActionResult ForbiddenResponse(string message = "Access forbidden")
        {
            return StatusCode(403, new ApiResponse<object>
            {
                Success = false,
                Message = message,
                Data = null,
                StatusCode = 403
            });
        }

        /// <summary>
        /// Returns a 401 Unauthorized response
        /// </summary>
        /// <param name="message">Error message</param>
        /// <returns>401 Unauthorized response</returns>
        protected IActionResult UnauthorizedResponse(string message = "Unauthorized access")
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = message,
                Data = null,
                StatusCode = 401
            });
        }

        /// <summary>
        /// Returns a 409 Conflict response
        /// </summary>
        /// <param name="message">Error message</param>
        /// <returns>409 Conflict response</returns>
        protected IActionResult ConflictResponse(string message = "Resource conflict")
        {
            return StatusCode(409, new ApiResponse<object>
            {
                Success = false,
                Message = message,
                Data = null,
                StatusCode = 409
            });
        }

        /// <summary>
        /// Returns a 204 No Content response
        /// </summary>
        /// <returns>204 No Content response</returns>
        protected IActionResult NoContentResponse()
        {
            return NoContent();
        }

        /// <summary>
        /// Returns a 500 Internal Server Error response
        /// </summary>
        /// <param name="message">Error message</param>
        /// <returns>500 Internal Server Error response</returns>
        protected IActionResult InternalServerErrorResponse(string message = "An internal server error occurred")
        {
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = message,
                Data = null,
                StatusCode = 500
            });
        }

        #endregion
    }

    #region API Response Models

    /// <summary>
    /// Standard API response wrapper
    /// </summary>
    /// <typeparam name="T">Type of data being returned</typeparam>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public int StatusCode { get; set; }
    }

    /// <summary>
    /// Validation error response with field-specific errors
    /// </summary>
    public class ApiValidationResponse : ApiResponse<object>
    {
        public IDictionary<string, string[]> Errors { get; set; } = new Dictionary<string, string[]>();
    }

    #endregion
}