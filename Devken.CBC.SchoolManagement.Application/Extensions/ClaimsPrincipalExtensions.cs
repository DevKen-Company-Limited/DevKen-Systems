using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Devken.CBC.SchoolManagement.Application.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid? GetUserId(this ClaimsPrincipal principal)
        {
            var userIdClaim = principal.FindFirstValue("user_id") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (Guid.TryParse(userIdClaim, out var userId))
                return userId;

            return null;
        }

        public static Guid? GetTenantId(this ClaimsPrincipal principal)
        {
            var tenantIdClaim = principal.FindFirstValue("tenant_id");

            if (Guid.TryParse(tenantIdClaim, out var tenantId))
                return tenantId;

            return null;
        }

        public static Guid? GetSchoolId(this ClaimsPrincipal principal)
        {
            return principal.GetTenantId(); // SchoolId is same as TenantId
        }

        public static string? GetEmail(this ClaimsPrincipal principal)
        {
            return principal.FindFirstValue(ClaimTypes.Email);
        }

        public static string? GetFullName(this ClaimsPrincipal principal)
        {
            return principal.FindFirstValue(ClaimTypes.Name);
        }

        public static IEnumerable<string> GetRoles(this ClaimsPrincipal principal)
        {
            return principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
        }

        public static IEnumerable<string> GetPermissions(this ClaimsPrincipal principal)
        {
            return principal.FindAll("permissions").Select(c => c.Value);
        }

        public static bool IsInRole(this ClaimsPrincipal principal, string role)
        {
            return principal.GetRoles().Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
        }

        public static bool HasPermission(this ClaimsPrincipal principal, string permission)
        {
            if (principal.IsSuperAdmin())
                return true;

            return principal.GetPermissions().Any(p => string.Equals(p, permission, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsSuperAdmin(this ClaimsPrincipal principal)
        {
            var isSuperAdminClaim = principal.FindFirstValue("is_super_admin");
            return (isSuperAdminClaim?.ToLower() == "true") || principal.IsInRole("SuperAdmin");
        }

        public static bool IsSchoolAdmin(this ClaimsPrincipal principal)
        {
            return principal.IsInRole("SchoolAdmin");
        }

        public static bool IsTeacher(this ClaimsPrincipal principal)
        {
            return principal.IsInRole("Teacher");
        }

        public static bool IsParent(this ClaimsPrincipal principal)
        {
            return principal.IsInRole("Parent");
        }

        private static string? FindFirstValue(this ClaimsPrincipal principal, string claimType)
        {
            return principal.FindFirst(claimType)?.Value;
        }
    }
}