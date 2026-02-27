using Devken.CBC.SchoolManagement.API.Diagnostics;
using Devken.CBC.SchoolManagement.API.Registration;
using Devken.CBC.SchoolManagement.API.Services;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Application.Service.ISubscription; // ADD THIS
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

StartupErrorHandler.Initialize();

var builder = WebApplication.CreateBuilder(args);

var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

QuestPDF.Settings.License = LicenseType.Community;



// ══════════════════════════════════════════════════════════════
// CORS Configuration
// ══════════════════════════════════════════════════════════════
//var angularCorsPolicy = "AngularDevCors";
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy(angularCorsPolicy, policy =>
//    {
//        policy.WithOrigins(
//                "http://localhost:4200",
//                "https://dev-ken-systems.vercel.app"
//              )
//              .AllowAnyHeader()
//              .AllowAnyMethod()
//              .AllowCredentials();
//    });
//});

var angularCorsPolicy = "AngularDevCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(angularCorsPolicy, policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ══════════════════════════════════════════════════════════════
// Controllers
// ══════════════════════════════════════════════════════════════
builder.Services.AddControllers();

// ══════════════════════════════════════════════════════════════
// Database Configuration
// ══════════════════════════════════════════════════════════════
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseSqlServer(connectionString, sql =>
    {
        sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
        sql.EnableRetryOnFailure();
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
        Description = "Enter your JWT token. Do NOT include 'Bearer ' prefix - it will be added automatically."
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
// Infrastructure Services (includes JWT Authentication & Authorization)
// ══════════════════════════════════════════════════════════════
builder.Services.AddInfrastructure(builder.Configuration);

// ══════════════════════════════════════════════════════════════
// Culture Configuration
// ══════════════════════════════════════════════════════════════
var supportedCultures = new[] { new CultureInfo("en-US") };
CultureInfo.DefaultThreadCurrentCulture = supportedCultures[0];
CultureInfo.DefaultThreadCurrentUICulture = supportedCultures[0];

// ══════════════════════════════════════════════════════════════
// Build Application
// ══════════════════════════════════════════════════════════════
var app = builder.Build();

// ══════════════════════════════════════════════════════════════
// Database Seeding
// ══════════════════════════════════════════════════════════════
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = app.Logger;

    try
    {
        logger.LogInformation("Starting database initialization...");

        // Run pending migrations
        var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();
        if (pendingMigrations.Any())
        {
            logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations.Count);
            dbContext.Database.Migrate();
            logger.LogInformation("Migrations applied successfully.");
        }
        else
        {
            logger.LogInformation("Database is up to date. No pending migrations.");
        }

        // Seed database
        await dbContext.SeedDatabaseAsync(logger);

        // ═══════════════════════════════════════════════════════════
        // NEW: Seed Subscription Plans
        // ═══════════════════════════════════════════════════════════
        try
        {
            var planService = scope.ServiceProvider.GetRequiredService<ISubscriptionPlanService>();
            await SubscriptionPlanSeeder.SeedSubscriptionPlansAsync(planService, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding subscription plans.");
            // Don't throw - allow application to continue even if plan seeding fails
        }
        // ═══════════════════════════════════════════════════════════

        // Seed permissions for default school
        var defaultSchool = await dbContext.Schools
            .FirstOrDefaultAsync(s => s.SlugName == "default-school");

        if (defaultSchool != null)
        {
            var permissionSeeder = scope.ServiceProvider.GetRequiredService<IPermissionSeedService>();
            await permissionSeeder.SeedPermissionsAndRolesAsync(defaultSchool.Id);
            logger.LogInformation("Default school permissions seeded successfully.");
        }
        else
        {
            logger.LogWarning("Default school not found. Skipping permission seeding.");
        }

        logger.LogInformation("Database initialization completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database initialization.");
        throw;
    }
}

// ══════════════════════════════════════════════════════════════
// Middleware Pipeline Configuration
// ══════════════════════════════════════════════════════════════

app.UseCors(angularCorsPolicy);
app.UseApiPipeline();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US"),
    SupportedCultures = new List<CultureInfo> { new CultureInfo("en-US") },
    SupportedUICultures = new List<CultureInfo> { new CultureInfo("en-US") }
};
app.UseRequestLocalization(localizationOptions);

app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();

// ══════════════════════════════════════════════════════════════
// Swagger UI (Development Only)
// ══════════════════════════════════════════════════════════════
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DevKen School Management API v1");
        c.RoutePrefix = string.Empty;
        c.DocumentTitle = "DevKen School Management API";
        c.DisplayRequestDuration();
    });

    app.Logger.LogInformation("Swagger UI is available at: https://localhost:7258");
}

// ══════════════════════════════════════════════════════════════
// Map Controllers
// ══════════════════════════════════════════════════════════════
app.MapControllers();
app.UseStaticFiles(); // wwwroot
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});


// ══════════════════════════════════════════════════════════════
// Angular Development Server Launcher (Development Only)
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
        app.Logger.LogWarning(ex, "Failed to launch Angular development server. You may need to start it manually.");
    }
}

// ══════════════════════════════════════════════════════════════
// Application Startup Complete
// ══════════════════════════════════════════════════════════════
app.Logger.LogInformation("DevKen School Management API is starting...");
app.Logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
app.Logger.LogInformation("Application started successfully. Press Ctrl+C to shut down.");

// ══════════════════════════════════════════════════════════════
// Run Application
// ══════════════════════════════════════════════════════════════
app.Run();