using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Middleware
{
    public class SubscriptionValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SubscriptionValidationMiddleware> _logger;

        private static readonly string[] BypassPaths =
        {
            "/api/auth/login",
            "/api/auth/refresh",
            "/api/auth/superadmin",
            "/api/health",
            "/api/subscription/status"
        };

        public SubscriptionValidationMiddleware(
            RequestDelegate next,
            ILogger<SubscriptionValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
        {
            // Bypass specific paths
            if (ShouldBypass(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Skip unauthenticated requests
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                await _next(context);
                return;
            }

            // SuperAdmin bypass
            var isSuperAdmin = context.User.Claims
                .Any(c => c.Type == CustomClaimTypes.IsSuperAdmin && c.Value == "true");

            if (isSuperAdmin)
            {
                await _next(context);
                return;
            }

            // Get tenant ID
            var tenantIdClaim = context.User.Claims
                .FirstOrDefault(c => c.Type == CustomClaimTypes.TenantId);

            if (tenantIdClaim == null || !Guid.TryParse(tenantIdClaim.Value, out var tenantId))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "InvalidTenant",
                    message = "No valid tenant found in token"
                });
                return;
            }

            // Get active subscription
            var subscription = await dbContext.Subscriptions
                .Where(s =>
                    s.SchoolId == tenantId &&
                    s.Status == SubscriptionStatus.Active
                )
                .OrderByDescending(s => s.ExpiryDate)
                .FirstOrDefaultAsync();

            if (subscription == null)
            {
                context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "NoSubscription",
                    message = "No active subscription found. Please contact support."
                });
                return;
            }

            // Check if subscription allows access
            if (!subscription.CanAccess)
            {
                var reason = subscription.Status switch
                {
                    SubscriptionStatus.Expired => "Your subscription has expired",
                    SubscriptionStatus.Suspended => "Your subscription has been suspended",
                    SubscriptionStatus.Cancelled => "Your subscription has been cancelled",
                    SubscriptionStatus.PendingPayment => "Payment pending for your subscription",
                    _ => "Subscription is not active"
                };

                context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "SubscriptionInactive",
                    message = reason,
                    expiryDate = subscription.ExpiryDate,
                    status = subscription.Status.ToString()
                });
                return;
            }

            // Add warning headers if expiring soon
            if (subscription.DaysRemaining <= 7 && subscription.DaysRemaining > 0)
            {
                context.Response.Headers.Append(
                    "X-Subscription-Warning",
                    $"Subscription expires in {subscription.DaysRemaining} days");
            }

            if (subscription.IsInGracePeriod)
            {
                context.Response.Headers.Append(
                    "X-Subscription-Grace-Period",
                    "true");
            }

            context.Response.Headers.Append(
                "X-Subscription-Plan",
                subscription.Plan.ToString());

            context.Response.Headers.Append(
                "X-Subscription-Expiry",
                subscription.ExpiryDate.ToString("o"));

            await _next(context);
        }

        private static bool ShouldBypass(PathString path)
        {
            return BypassPaths.Any(p =>
                path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
        }
    }
}
