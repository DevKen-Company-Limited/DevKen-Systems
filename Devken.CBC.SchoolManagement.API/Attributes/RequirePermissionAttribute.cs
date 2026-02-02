using Devken.CBC.SchoolManagement.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Devken.CBC.SchoolManagement.Api.Attributes
{
    /// <summary>
    /// Authorization attribute that checks if user has required permissions
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _permissions;
        private readonly bool _requireAll;

        /// <summary>
        /// Require specific permission(s) to access this resource
        /// </summary>
        /// <param name="permissions">One or more permission keys required</param>
        public RequirePermissionAttribute(params string[] permissions)
        {
            _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
            _requireAll = true; // Default: require ALL permissions
        }

        /// <summary>
        /// Require specific permission(s) to access this resource
        /// </summary>
        /// <param name="requireAll">If true, user must have ALL permissions; if false, user needs ANY permission</param>
        /// <param name="permissions">One or more permission keys required</param>
        public RequirePermissionAttribute(bool requireAll, params string[] permissions)
        {
            _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
            _requireAll = requireAll;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Check if user is authenticated
            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    Success = false,
                    Message = "Authentication required"
                });
                return;
            }

            // Get user's permissions from claims
            var userPermissions = context.HttpContext.User
                .FindAll(CustomClaimTypes.Permissions)
                .Select(c => c.Value)
                .ToHashSet();

            bool hasPermission;

            if (_requireAll)
            {
                // User must have ALL required permissions
                hasPermission = _permissions.All(p => userPermissions.Contains(p));
            }
            else
            {
                // User must have AT LEAST ONE required permission
                hasPermission = _permissions.Any(p => userPermissions.Contains(p));
            }

            if (!hasPermission)
            {
                context.Result = new ObjectResult(new
                {
                    Success = false,
                    Message = "You do not have permission to access this resource",
                    RequiredPermissions = _permissions,
                    RequireAll = _requireAll
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }

    /// <summary>
    /// Convenience attribute: Require ANY of the specified permissions
    /// </summary>
    public class RequireAnyPermissionAttribute : RequirePermissionAttribute
    {
        public RequireAnyPermissionAttribute(params string[] permissions)
            : base(requireAll: false, permissions)
        {
        }
    }
}

/*
 * USAGE EXAMPLES:
 * 
 * // Require single permission
 * [RequirePermission(PermissionKeys.StudentRead)]
 * public IActionResult GetStudents() { }
 * 
 * // Require ALL permissions
 * [RequirePermission(PermissionKeys.StudentWrite, PermissionKeys.ClassRead)]
 * public IActionResult CreateStudent() { }
 * 
 * // Require ANY permission (using custom attribute)
 * [RequireAnyPermission(PermissionKeys.FeeRead, PermissionKeys.PaymentRead)]
 * public IActionResult GetFinancialSummary() { }
 * 
 * // Require ANY permission (using base attribute)
 * [RequirePermission(false, PermissionKeys.FeeRead, PermissionKeys.PaymentRead)]
 * public IActionResult GetFinancialSummary() { }
 * 
 * // Apply to entire controller
 * [RequirePermission(PermissionKeys.StudentRead)]
 * public class StudentsController : BaseApiController
 * {
 *     // All actions require Student.Read
 *     
 *     // But specific action can require additional permissions
 *     [RequirePermission(PermissionKeys.StudentWrite)]
 *     public IActionResult CreateStudent() { }
 * }
 */