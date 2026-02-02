using System;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Infrastructure.Security;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Devken.CBC.SchoolManagement.Infrastructure.Middleware
{
    /// <summary>
    /// Extracts the tenant (school) from either:
    ///   1. The JWT claim (tenant_id) – for authenticated requests.
    ///   2. A query-string parameter – for the login flow before a token exists.
    ///
    /// Sets TenantContext.TenantId so that EF global filters activate.
    /// </summary>
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantMiddleware> _logger;

        public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var tenantContext = context.RequestServices.GetRequiredService<TenantContext>();

            if (tenantContext == null)
            {
                _logger.LogError("TenantContext is not registered in DI.");
                await _next(context);
                return;
            }

            // 1. Try JWT claim first (already-authenticated user)
            if (context.User?.Identity != null && context.User.Identity.IsAuthenticated)
            {
                var tenantClaim = context.User.FindFirst(CustomClaimTypes.TenantId);
                if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var tenantId))
                {
                    tenantContext.TenantId = tenantId;
                    _logger.LogDebug("TenantMiddleware: resolved tenant {TenantId} from JWT", tenantId);
                }

                // Resolve the acting user so RepositoryBase can stamp CreatedBy/UpdatedBy
                var userIdClaim = context.User.FindFirst(CustomClaimTypes.UserId);
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var actingUserId))
                {
                    tenantContext.ActingUserId = actingUserId;
                    _logger.LogDebug("TenantMiddleware: acting user {ActingUserId}", actingUserId);
                }
            }

            // 2. Fall back to ?tenant=<slug> query parameter (login flow)
            if (tenantContext.TenantId == null)
            {
                var slug = context.Request.Query["tenant"].ToString();
                if (!string.IsNullOrWhiteSpace(slug))
                {
                    var repoManager = context.RequestServices.GetRequiredService<IRepositoryManager>();
                    var school = await repoManager.School.GetBySlugAsync(slug);
                    if (school != null)
                    {
                        tenantContext.TenantId = school.Id;
                        tenantContext.CurrentTenant = school;
                        _logger.LogDebug("TenantMiddleware: resolved tenant {TenantId} from slug '{Slug}'", school.Id, slug);
                    }
                }
            }

            // 3. If still null, request will hit global filters with TenantId == null,
            //    which allows cross-tenant queries (only for SuperAdmin or unauthenticated login endpoints).

            await _next(context);
        }
    }
}
