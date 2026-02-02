using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Identity;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Tenant;
using Devken.CBC.SchoolManagement.Infrastructure.Security;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace Devken.CBC.SchoolManagement.Infrastructure
{
    public static class InfrastructureServiceRegistration
    {
        /// <summary>
        /// Registers all infrastructure services and repositories.
        /// Configuration is accepted for future extensibility (JWT, Tenants, etc).
        /// </summary>
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ── Repositories ───────────────────────────────────
            services.AddScoped<ISchoolRepository, SchoolRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IPermissionRepository, PermissionRepository>();
            services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
            services.AddScoped<IUserRoleRepository, UserRoleRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<ISuperAdminRepository, SuperAdminRepository>();

            // ── Lazy<T> support ────────────────────────────────
            services.AddScoped(typeof(Lazy<>), typeof(LazyServiceProvider<>));
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtSettings>>().Value);

            // ── Core services ──────────────────────────────────
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<ITenantSeedService, TenantSeedService>();

            // ── New required services ──────────────────────────
            services.AddScoped<IPermissionSeedService, PermissionSeedService>(); // <-- fix
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();    // <-- fix

            // ── Repository Manager ────────────────────────────
            services.AddScoped<IRepositoryManager, RepositoryManager>();

            return services;
        }

    }

    /// <summary>
    /// Supports Lazy<T> dependency injection.
    /// </summary>
    public sealed class LazyServiceProvider<T> : Lazy<T> where T : class
    {
        public LazyServiceProvider(IServiceProvider provider)
            : base(() => provider.GetRequiredService<T>())
        {
        }
    }
}
