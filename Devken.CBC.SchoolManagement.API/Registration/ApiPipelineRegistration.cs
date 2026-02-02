using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;

namespace Devken.CBC.SchoolManagement.API.Registration
{
    public static class ApiPipelineRegistration
    {
        private const string DefaultCorsPolicy = "DefaultCorsPolicy";

        // ────────────────────────────────────────────────
        // SERVICE REGISTRATION (Swagger UI + CORS)
        // ────────────────────────────────────────────────
        public static IServiceCollection AddApiServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ── CORS ─────────────────────────────────────
            services.AddCors(options =>
            {
                options.AddPolicy(DefaultCorsPolicy, policy =>
                {
                    policy
                        .AllowAnyOrigin()   // ⚠ tighten in production
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            // ── Swagger UI (Classic, no OpenAPI) ─────────
            services.AddSwaggerGen();

            return services;
        }

        // ────────────────────────────────────────────────
        // APPLICATION PIPELINE (Middleware)
        // ────────────────────────────────────────────────
        public static WebApplication UseApiPipeline(this WebApplication app)
        {
            // ── Swagger UI ───────────────────────────────
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.RoutePrefix = "swagger";         // Access via /swagger
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Devken CBC API v1");
                    c.DisplayRequestDuration();
                });
            }

            // ── CORS ────────────────────────────────────
            app.UseCors(DefaultCorsPolicy);

            return app;
        }
    }
}
