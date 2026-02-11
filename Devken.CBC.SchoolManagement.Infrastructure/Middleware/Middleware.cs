using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Middleware
{
    /// <summary>
    /// Middleware to extract and populate tenant context from JWT claims
    /// UPDATED: Now extracts IsSuperAdmin flag to handle audit correctly
    /// </summary>
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                // Extract User ID
                var userIdClaim = context.User.FindFirst("user_id")
                               ?? context.User.FindFirst(ClaimTypes.NameIdentifier)
                               ?? context.User.FindFirst("sub");

                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    tenantContext.ActingUserId = userId;
                }

                // Extract Email
                var emailClaim = context.User.FindFirst(ClaimTypes.Email)
                              ?? context.User.FindFirst("email")
                              ?? context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");

                if (emailClaim != null)
                {
                    tenantContext.UserEmail = emailClaim.Value;
                }

                // Extract Display Name
                var nameClaim = context.User.FindFirst(ClaimTypes.Name)
                             ?? context.User.FindFirst("full_name")
                             ?? context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");

                if (nameClaim != null)
                {
                    tenantContext.UserDisplayName = nameClaim.Value;
                }

                // CRITICAL: Extract SuperAdmin status
                // Check multiple claim types for SuperAdmin flag
                var isSuperAdminClaim = context.User.FindFirst("is_super_admin");
                var roleClaim = context.User.FindFirst(ClaimTypes.Role)
                             ?? context.User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role");

                // User is SuperAdmin if:
                // 1. is_super_admin claim is "true", OR
                // 2. Role claim contains "SuperAdmin"
                tenantContext.IsSuperAdmin =
                    (isSuperAdminClaim?.Value?.ToLower() == "true") ||
                    (roleClaim?.Value == "SuperAdmin") ||
                    (context.User.IsInRole("SuperAdmin"));

                // Extract Tenant ID (only for non-SuperAdmins)
                // SuperAdmins don't belong to a specific tenant/school
                if (!tenantContext.IsSuperAdmin)
                {
                    var tenantIdClaim = context.User.FindFirst("tenant_id")
                                     ?? context.User.FindFirst("school_id");

                    if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
                    {
                        tenantContext.TenantId = tenantId;
                    }
                }
                else
                {
                    // SuperAdmin has no tenant
                    tenantContext.TenantId = null;
                }
            }

            await _next(context);
        }
    }
}