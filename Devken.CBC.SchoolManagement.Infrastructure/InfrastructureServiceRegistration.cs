// ═══════════════════════════════════════════════════════════════════
// UPDATED InfrastructureServiceRegistration.cs
// Add subscription seed service registration
// ═══════════════════════════════════════════════════════════════════

using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Identity;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Application.Service.Academic;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.IRolesAssignment;
using Devken.CBC.SchoolManagement.Application.Service.Isubscription; // ✨ ADD THIS
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Tenant;
using Devken.CBC.SchoolManagement.Infrastructure.Middleware;
using Devken.CBC.SchoolManagement.Infrastructure.Security;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Devken.CBC.SchoolManagement.Infrastructure.Services.Activities;
using Devken.CBC.SchoolManagement.Infrastructure.Services.RoleAssignment;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ══════════════════════════════════════════════════════════════
            // JWT Settings Configuration
            // ══════════════════════════════════════════════════════════════
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtSettings>>().Value);

            // ══════════════════════════════════════════════════════════════
            // JWT Authentication Configuration
            // ══════════════════════════════════════════════════════════════
            var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();

            if (jwtSettings == null)
                throw new InvalidOperationException("JwtSettings configuration is missing in appsettings.json");

            if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
                throw new InvalidOperationException("JWT SecretKey is not configured in appsettings.json");

            if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
                throw new InvalidOperationException("JWT Issuer is not configured in appsettings.json");

            if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
                throw new InvalidOperationException("JWT Audience is not configured in appsettings.json");

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.SecretKey)
                    ),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
                    RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
                        if (logger != null)
                        {
                            logger.LogError(context.Exception, "JWT Authentication failed: {Message}", context.Exception.Message);
                        }

                        if (context.Exception is SecurityTokenExpiredException)
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                            logger?.LogWarning("JWT token has expired");
                        }
                        return Task.CompletedTask;
                    },

                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
                        if (logger != null)
                        {
                            var userId = context.Principal?.FindFirst("user_id")?.Value;
                            var email = context.Principal?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
                            logger.LogInformation("JWT token validated successfully for user: {UserId} ({Email})", userId ?? "Unknown", email ?? "Unknown");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            // ══════════════════════════════════════════════════════════════
            // Authorization Configuration
            // ══════════════════════════════════════════════════════════════
            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            // ══════════════════════════════════════════════════════════════
            // Repositories Registration
            // ══════════════════════════════════════════════════════════════
            services.AddScoped<ISchoolRepository, SchoolRepository>();
            services.AddScoped<IStudentService, StudentService>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IPermissionRepository, PermissionRepository>();
            services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
            services.AddScoped<IUserRoleRepository, UserRoleRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<ISuperAdminRepository, SuperAdminRepository>();

            // ══════════════════════════════════════════════════════════════
            // Lazy<T> Dependency Injection Support
            // ══════════════════════════════════════════════════════════════
            services.AddScoped(typeof(Lazy<>), typeof(LazyServiceProvider<>));

            // ══════════════════════════════════════════════════════════════
            // Core Services Registration
            // ══════════════════════════════════════════════════════════════
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<ITenantSeedService, TenantSeedService>();
            services.AddScoped<IPermissionSeedService, PermissionSeedService>();

            // ══════════════════════════════════════════════════════════════
            // Subscription Services Registration
            // ══════════════════════════════════════════════════════════════
            services.AddScoped<ISubscriptionService, SubscriptionService>();
            services.AddScoped<ISubscriptionSeedService, SubscriptionSeedService>(); // ✨ ADD THIS

            // ══════════════════════════════════════════════════════════════
            // Identity & Authentication Services
            // ══════════════════════════════════════════════════════════════
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
            services.AddScoped<IAuthService, AuthService>();


            // ══════════════════════════════════════════════════════════════
            // User Activity Service (✨ NEW)
            // ══════════════════════════════════════════════════════════════
            services.AddScoped<IUserActivityService, UserActivityService>();

            services.AddScoped<IRoleAssignmentService, RoleAssignmentService>();
            // ══════════════════════════════════════════════════════════════
            // Repository Manager
            // ══════════════════════════════════════════════════════════════
            services.AddScoped<IRepositoryManager, RepositoryManager>();

            return services;
        }
    }

    public sealed class LazyServiceProvider<T> : Lazy<T> where T : class
    {
        public LazyServiceProvider(IServiceProvider provider)
            : base(() => provider.GetRequiredService<T>())
        {
        }
    }
}