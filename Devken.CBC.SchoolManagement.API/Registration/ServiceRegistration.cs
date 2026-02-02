using Devken.CBC.SchoolManagement.API.Authorization;
using Devken.CBC.SchoolManagement.API.Services;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Infrastructure.Security;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;

namespace Devken.CBC.SchoolManagement.API.Registration
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddSchoolManagement(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ── Tenant Context ────────────────────────────────
            services.AddScoped<TenantContext>();

            // ── Navigation Service ────────────────────────────
            services.AddScoped<INavigationService, NavigationService>();

            // ── Authorization (Permissions) ───────────────────
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

            return services;
        }
    }
}