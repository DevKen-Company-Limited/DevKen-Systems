using Devken.CBC.SchoolManagement.API.Diagnostics;
using Devken.CBC.SchoolManagement.API.Registration;
using Devken.CBC.SchoolManagement.API.Services;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Middleware;
using Devken.CBC.SchoolManagement.Infrastructure.Security;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using System.Globalization;
using System.Reflection;

StartupErrorHandler.Initialize();

var builder = WebApplication.CreateBuilder(args);

var angularCorsPolicy = "AngularDevCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: angularCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

try
{
    Console.WriteLine("📦 Registering Controllers...");
    builder.Services.AddControllers();

    Console.WriteLine("🗄️  Configuring Database Context...");
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
        );

        if (builder.Environment.IsDevelopment())
        {
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                if (args.Name.StartsWith("Microsoft.Data.SqlClient.resources"))
                    return null;
                return null;
            };
        }
    });

    Console.WriteLine("🌐 Registering API Services...");
    builder.Services.AddApiServices(builder.Configuration);

    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "DevKen School Management API",
            Version = "v1"
        });

        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter 'Bearer {token}' to authenticate"
        };

        c.AddSecurityDefinition("Bearer", securityScheme);
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { securityScheme, Array.Empty<string>() }
        });
    });

    Console.WriteLine("⚙️  Registering Application Services...");
    builder.Services.AddSchoolManagement(builder.Configuration);

    Console.WriteLine("🧭 Registering Navigation Services...");
    builder.Services.AddScoped<INavigationService, NavigationService>();

    Console.WriteLine("🔧 Registering Infrastructure Services...");
    builder.Services.AddInfrastructure(builder.Configuration);

    Console.WriteLine("🔐 Configuring JWT Settings...");
    var jwtSection = builder.Configuration.GetSection("JwtSettings");
    if (!jwtSection.Exists())
        throw new InvalidOperationException("JwtSettings section not found in configuration.");

    builder.Services.Configure<JwtSettings>(jwtSection);
    builder.Services.AddSingleton<JwtSettings>(sp =>
        sp.GetRequiredService<IOptions<JwtSettings>>().Value
    );

    Console.WriteLine("🛠️  Configuring default culture...");
    var supportedCultures = new[] { new CultureInfo("en-US") };
    CultureInfo.DefaultThreadCurrentCulture = supportedCultures[0];
    CultureInfo.DefaultThreadCurrentUICulture = supportedCultures[0];

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("✅ Service registration completed successfully.");
    Console.ResetColor();
}
catch (ReflectionTypeLoadException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("🔥 ReflectionTypeLoadException during startup");
    foreach (var loaderException in ex.LoaderExceptions.Where(e => e != null))
    {
        Console.WriteLine(loaderException!.Message);
    }
    Console.ResetColor();
    throw;
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("🔥 Startup exception:");
    Console.WriteLine(ex);
    Console.ResetColor();
    throw;
}

Console.WriteLine("🏗️  Building application...");
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();

    if (pendingMigrations.Any())
    {
        Console.WriteLine("🛠️  Applying pending migrations...");
        dbContext.Database.Migrate();
    }
}

app.UseCors(angularCorsPolicy);

Console.WriteLine("🔌 Configuring middleware pipeline...");
app.UseApiPipeline();

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

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DevKen School Management API v1");
    c.RoutePrefix = string.Empty;
});

app.MapControllers();

if (builder.Environment.IsDevelopment())
{
    var relativeAngularPath = @"Devken.CBC.SchoolManagment.UI\School-System-UI";
    AngularLauncher.Launch(relativeAngularPath);

    app.Lifetime.ApplicationStopping.Register(() =>
    {
        AngularLauncher.Close();
    });
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("🚀 DevKen School Management API started successfully.");
Console.ResetColor();

app.Run();
