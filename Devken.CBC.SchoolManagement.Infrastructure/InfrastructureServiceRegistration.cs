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
        /// <summary>
        /// Registers all infrastructure services, repositories, and application services.
        /// </summary>
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
                options.RequireHttpsMetadata = false; // Set to true in production with HTTPS

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Validate the token signature
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.SecretKey)
                    ),

                    // Validate issuer and audience
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,

                    // Validate token expiration
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero, // Remove 5-minute default tolerance for immediate expiration

                    // Map standard claims correctly
                    NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
                    RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
                };

                // ══════════════════════════════════════════════════════════════
                // Event Handlers for Debugging and Logging
                // ══════════════════════════════════════════════════════════════
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetService<ILogger<Program>>();

                        if (logger != null)
                        {
                            logger.LogError(context.Exception,
                                "JWT Authentication failed: {Message}",
                                context.Exception.Message);
                        }

                        // Add specific headers for different error types
                        if (context.Exception is SecurityTokenExpiredException)
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                            logger?.LogWarning("JWT token has expired");
                        }
                        else if (context.Exception is SecurityTokenInvalidSignatureException)
                        {
                            logger?.LogError("JWT token has invalid signature");
                        }
                        else if (context.Exception is SecurityTokenInvalidIssuerException)
                        {
                            logger?.LogError("JWT token has invalid issuer. Expected: {ExpectedIssuer}",
                                jwtSettings.Issuer);
                        }
                        else if (context.Exception is SecurityTokenInvalidAudienceException)
                        {
                            logger?.LogError("JWT token has invalid audience. Expected: {ExpectedAudience}",
                                jwtSettings.Audience);
                        }

                        return Task.CompletedTask;
                    },

                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetService<ILogger<Program>>();

                        if (logger != null)
                        {
                            var userId = context.Principal?.FindFirst("user_id")?.Value;
                            var email = context.Principal?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

                            logger.LogInformation(
                                "JWT token validated successfully for user: {UserId} ({Email})",
                                userId ?? "Unknown",
                                email ?? "Unknown");
                        }

                        return Task.CompletedTask;
                    },

                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetService<ILogger<Program>>();

                        if (logger != null)
                        {
                            logger.LogWarning(
                                "JWT Challenge triggered. Error: {Error}, Description: {Description}, Path: {Path}",
                                context.Error ?? "None",
                                context.ErrorDescription ?? "None",
                                context.Request.Path);
                        }

                        return Task.CompletedTask;
                    },

                    OnMessageReceived = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetService<ILogger<Program>>();

                        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

                        if (logger != null && authHeader != null)
                        {
                            logger.LogDebug("Authorization header received for {Path}", context.Request.Path);
                        }
                        else if (logger != null && authHeader == null)
                        {
                            logger.LogWarning("No Authorization header found for {Path}", context.Request.Path);
                        }

                        return Task.CompletedTask;
                    },

                    OnForbidden = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetService<ILogger<Program>>();

                        logger?.LogWarning(
                            "Access forbidden for user attempting to access {Path}",
                            context.Request.Path);

                        return Task.CompletedTask;
                    }
                };
            });

            // ══════════════════════════════════════════════════════════════
            // Authorization Configuration
            // ══════════════════════════════════════════════════════════════
            services.AddAuthorization(options =>
            {
                // Default policy: require authenticated user
                options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

                // Add custom authorization policies here if needed
                // Example: options.AddPolicy("AdminOnly", policy => 
                //     policy.RequireRole("Admin", "SuperAdmin"));
            });

            // ══════════════════════════════════════════════════════════════
            // Repositories Registration
            // ══════════════════════════════════════════════════════════════
            services.AddScoped<ISchoolRepository, SchoolRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
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
            // Identity & Authentication Services
            // ══════════════════════════════════════════════════════════════
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
            services.AddScoped<IAuthService, AuthService>();

            // ══════════════════════════════════════════════════════════════
            // Repository Manager
            // ══════════════════════════════════════════════════════════════
            services.AddScoped<IRepositoryManager, RepositoryManager>();

            return services;
        }
    }

    /// <summary>
    /// Supports Lazy<T> dependency injection for performance optimization.
    /// Allows services to be resolved only when actually needed.
    /// </summary>
    public sealed class LazyServiceProvider<T> : Lazy<T> where T : class
    {
        public LazyServiceProvider(IServiceProvider provider)
            : base(() => provider.GetRequiredService<T>())
        {
        }
    }
}