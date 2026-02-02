using Devken.CBC.SchoolManagement.API.Authorization;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Infrastructure.Security;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using System.Text;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;

namespace Devken.CBC.SchoolManagement.API.Registration
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddSchoolManagement(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ── JWT Settings (Options Pattern) ───────────────────
            var jwtSection = configuration.GetSection("JwtSettings");
            if (!jwtSection.Exists())
                throw new InvalidOperationException("JwtSettings section is missing in configuration.");

            services.Configure<JwtSettings>(jwtSection);

            // Expose IJwtSettings safely
            services.AddSingleton<JwtSettings>(sp =>
                sp.GetRequiredService<IOptions<JwtSettings>>().Value
            );

            // ── Tenant Context ────────────────────────────────
            services.AddScoped<TenantContext>();

            // ── Application Services ─────────────────────────
            services.AddScoped<JwtService>();
            services.AddScoped<TenantSeedService>();
            services.AddScoped<AuthService>();

            // ── Authentication (JWT) ──────────────────────────
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    var jwtSettings = services.BuildServiceProvider()
                        .GetRequiredService<JwtSettings>();

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings.Issuer,

                        ValidateAudience = true,
                        ValidAudience = jwtSettings.Audience,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(10)
                    };
                });

            // ── Authorization (Permissions) ───────────────────
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
            services.AddAuthorization();

            return services;
        }
    }
}
