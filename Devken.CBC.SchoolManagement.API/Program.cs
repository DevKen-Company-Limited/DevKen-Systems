using Devken.CBC.SchoolManagement.API.Diagnostics;
using Devken.CBC.SchoolManagement.API.Registration;
using Devken.CBC.SchoolManagement.API.Services;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Application.Service.ISubscription;
using Devken.CBC.SchoolManagement.Infrastructure;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Middleware;
using Devken.CBC.SchoolManagement.Infrastructure.Seed;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Reflection;
using System.Text;

StartupErrorHandler.Initialize();

var builder = WebApplication.CreateBuilder(args);

// ══════════════════════════════════════════════════════════════
// Uploads Directory
// ══════════════════════════════════════════════════════════════
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (!Directory.Exists(uploadsPath))
    Directory.CreateDirectory(uploadsPath);

QuestPDF.Settings.License = LicenseType.Community;

// ══════════════════════════════════════════════════════════════
// CORS Configuration
// ══════════════════════════════════════════════════════════════
var angularCorsPolicy = "AngularCors";

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? new[]
    {
        "https://dev-ken-systems.vercel.app",
        "http://localhost:4200"
    };

builder.Services.AddCors(options =>
{
    options.AddPolicy(angularCorsPolicy, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// ══════════════════════════════════════════════════════════════
// Controllers
// ══════════════════════════════════════════════════════════════
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ══════════════════════════════════════════════════════════════
// Database Configuration
// ══════════════════════════════════════════════════════════════
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseSqlServer(connectionString, sql =>
    {
        sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
        sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });

    options.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
    options.EnableServiceProviderCaching();

    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
    }
}, ServiceLifetime.Scoped);

// ══════════════════════════════════════════════════════════════
// Swagger Configuration
// ══════════════════════════════════════════════════════════════
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DevKen School Management API",
        Version = "v1",
        Description = "CBC School Management System API"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Do NOT include 'Bearer ' prefix — it will be added automatically."
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// ══════════════════════════════════════════════════════════════
// Application Services Registration
// ══════════════════════════════════════════════════════════════
builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddSchoolManagement(builder.Configuration);

// ══════════════════════════════════════════════════════════════
// Infrastructure Services
// ══════════════════════════════════════════════════════════════
builder.Services.AddInfrastructure(builder.Configuration);

// ══════════════════════════════════════════════════════════════
// Authentication
//
// ⚠️  JWT Bearer is registered inside AddInfrastructure() above.
//     Do NOT call AddAuthentication / AddJwtBearer here — ASP.NET Core
//     throws "Scheme already exists: Bearer" if the same scheme is
//     registered twice.
//
//     Google SSO also does NOT use AddGoogle() OAuth redirect middleware.
//     The SsoController validates Google id_tokens directly via
//     GoogleJsonWebSignature.ValidateAsync() (Google.Apis.Auth).
//     Angular gets the id_token from Google's JS SDK and POSTs it to
//     POST /api/auth/sso/google — no browser redirect or state cookie
//     involved, so the old AddGoogle() call was causing the
//     "oauth state was missing or invalid" crash.
// ══════════════════════════════════════════════════════════════

// ══════════════════════════════════════════════════════════════
// Culture Configuration
// ══════════════════════════════════════════════════════════════
var supportedCultures = new[] { new CultureInfo("en-US") };
CultureInfo.DefaultThreadCurrentCulture   = supportedCultures[0];
CultureInfo.DefaultThreadCurrentUICulture = supportedCultures[0];

// ══════════════════════════════════════════════════════════════
// Build Application
// ══════════════════════════════════════════════════════════════
var app = builder.Build();

// ══════════════════════════════════════════════════════════════
// Database Initialization
// ══════════════════════════════════════════════════════════════
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger    = app.Logger;

    try
    {
        logger.LogInformation("Starting database initialization...");

        try
        {
            var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();
            var appliedMigrations = dbContext.Database.GetAppliedMigrations().ToList();

            if (pendingMigrations.Any())
            {
                logger.LogInformation(
                    "Applying {Count} pending migrations...", pendingMigrations.Count);
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully.");
            }
            else if (!appliedMigrations.Any())
            {
                logger.LogInformation(
                    "No migrations found. Creating database schema from model...");
                await dbContext.Database.EnsureCreatedAsync();
                logger.LogInformation("Database schema created successfully.");
            }
            else
            {
                logger.LogInformation("Database is up to date. No pending migrations.");
            }
        }
        catch (Exception migEx)
        {
            logger.LogWarning(migEx, "Migration failed. Attempting EnsureCreated as fallback...");
            await dbContext.Database.EnsureCreatedAsync();
            logger.LogInformation("Database schema created via EnsureCreated fallback.");
        }

        await dbContext.SeedDatabaseAsync(logger);

        try
        {
            var planService = scope.ServiceProvider
                .GetRequiredService<ISubscriptionPlanService>();
            await SubscriptionPlanSeeder.SeedSubscriptionPlansAsync(planService, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding subscription plans.");
        }

        try
        {
            var defaultSchool = await dbContext.Schools
                .FirstOrDefaultAsync(s => s.SlugName == "default-school");

            if (defaultSchool != null)
            {
                var permissionSeeder = scope.ServiceProvider
                    .GetRequiredService<IPermissionSeedService>();
                await permissionSeeder.SeedPermissionsAndRolesAsync(defaultSchool.Id);
                logger.LogInformation("Default school permissions seeded successfully.");
            }
            else
            {
                logger.LogWarning(
                    "Default school not found. Skipping permission seeding.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding permissions.");
        }

        logger.LogInformation("Database initialization completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "A critical error occurred during database initialization.");
        throw;
    }
}

// ══════════════════════════════════════════════════════════════
// Middleware Pipeline
// ══════════════════════════════════════════════════════════════

// 1. CORS — must come first so preflight requests are handled before any auth checks
app.UseCors(angularCorsPolicy);

// 2. Static files
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath  = "/uploads"
});

// 3. Localisation
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US"),
    SupportedCultures     = new List<CultureInfo> { new("en-US") },
    SupportedUICultures   = new List<CultureInfo> { new("en-US") }
});

// 4. Swagger — registered BEFORE auth middleware so the UI is always reachable
//    Development : served at "/" (root)
//    Production  : served at "/swagger"
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DevKen School Management API v1");
    c.RoutePrefix    = app.Environment.IsDevelopment() ? string.Empty : "swagger";
    c.DocumentTitle  = "DevKen School Management API";
    c.DisplayRequestDuration();
    c.EnablePersistAuthorization();
});

// 5. Custom API pipeline (global exception handling, request logging, etc.)
app.UseApiPipeline();

// 6. Auth — always after Swagger so the UI is never blocked
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();

// 7. Controllers
app.MapControllers();

// ══════════════════════════════════════════════════════════════
// Angular Dev Server (Development Only)
// ══════════════════════════════════════════════════════════════
if (builder.Environment.IsDevelopment())
{
    var relativeAngularPath = @"Devken.CBC.SchoolManagment.UI\School-System-UI";

    try
    {
        AngularLauncher.Launch(relativeAngularPath);
        app.Logger.LogInformation("Angular development server launched successfully.");

        app.Lifetime.ApplicationStopping.Register(() =>
        {
            app.Logger.LogInformation("Stopping Angular development server...");
            AngularLauncher.Close();
        });
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(
            ex, "Failed to launch Angular development server. Start it manually.");
    }
}

// ══════════════════════════════════════════════════════════════
// Startup Logging
// ══════════════════════════════════════════════════════════════
app.Logger.LogInformation("DevKen School Management API is starting...");
app.Logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
app.Logger.LogInformation(
    "Allowed CORS Origins: {Origins}", string.Join(", ", allowedOrigins));
app.Logger.LogInformation(
    "Application started successfully. Press Ctrl+C to shut down.");

app.Run();