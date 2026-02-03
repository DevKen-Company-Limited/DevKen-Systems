using Devken.CBC.SchoolManagement.Application.Service;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services
{
    /// <summary>
    /// Implementation of ICurrentUserService that reads user information from HttpContext
    /// Compatible with Devken.CBC.SchoolManagement.Infrastructure.Security.JwtService
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public ClaimsPrincipal? ClaimsPrincipal => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => ClaimsPrincipal?.Identity?.IsAuthenticated ?? false;

        public Guid? UserId
        {
            get
            {
                var userIdClaim = GetClaimValue("user_id") ?? GetClaimValue(ClaimTypes.NameIdentifier);

                if (Guid.TryParse(userIdClaim, out var userId))
                    return userId;

                return null;
            }
        }

        public string? Email => GetClaimValue(ClaimTypes.Email);

        public string? FullName => GetClaimValue(ClaimTypes.Name);

        public Guid? TenantId
        {
            get
            {
                var tenantIdClaim = GetClaimValue("tenant_id");

                if (Guid.TryParse(tenantIdClaim, out var tenantId))
                    return tenantId;

                return null;
            }
        }

        public Guid? SchoolId => TenantId; // For school systems, SchoolId is the same as TenantId

        public IEnumerable<string> Roles
        {
            get
            {
                return ClaimsPrincipal?.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    ?? Enumerable.Empty<string>();
            }
        }

        public IEnumerable<string> Permissions
        {
            get
            {
                return ClaimsPrincipal?.Claims
                    .Where(c => c.Type == "permissions")
                    .Select(c => c.Value)
                    ?? Enumerable.Empty<string>();
            }
        }

        public bool IsSuperAdmin
        {
            get
            {
                var isSuperAdminClaim = GetClaimValue("is_super_admin");
                return (isSuperAdminClaim?.ToLower() == "true") || IsInRole("SuperAdmin");
            }
        }

        public bool IsSchoolAdmin => IsInRole("SchoolAdmin");

        public bool IsTeacher => IsInRole("Teacher");

        public bool IsParent => IsInRole("Parent");

        public bool IsInRole(string role)
        {
            return Roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
        }

        public bool HasPermission(string permission)
        {
            // Super admin has all permissions
            if (IsSuperAdmin)
                return true;

            return Permissions.Any(p => string.Equals(p, permission, StringComparison.OrdinalIgnoreCase));
        }

        public bool HasAnyPermission(params string[] permissions)
        {
            // Super admin has all permissions
            if (IsSuperAdmin)
                return true;

            return permissions.Any(HasPermission);
        }

        public bool HasAllPermissions(params string[] permissions)
        {
            // Super admin has all permissions
            if (IsSuperAdmin)
                return true;

            return permissions.All(HasPermission);
        }

        public string? GetClaimValue(string claimType)
        {
            return ClaimsPrincipal?.Claims
                .FirstOrDefault(c => string.Equals(c.Type, claimType, StringComparison.OrdinalIgnoreCase))?
                .Value;
        }

        private IEnumerable<string> GetClaimValues(string claimType)
        {
            return ClaimsPrincipal?.Claims
                .Where(c => string.Equals(c.Type, claimType, StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Value)
                ?? Enumerable.Empty<string>();
        }
    }
}