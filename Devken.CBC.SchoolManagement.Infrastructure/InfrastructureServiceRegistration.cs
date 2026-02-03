// ═══════════════════════════════════════════════════════════════════
// UPDATED InfrastructureServiceRegistration.cs
// Complete setup for CBC School Management System
// ═══════════════════════════════════════════════════════════════════

using Devken.CBC.SchoolManagement.Application.Authorization; // ✨ ADD THIS for authorization handlers
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Academic;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academic; // ✨ ADD THIS
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Identity;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Application.Service.Academic;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.IRolesAssignment;
using Devken.CBC.SchoolManagement.Application.Service.Isubscription;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Common;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Tenant;
using Devken.CBC.SchoolManagement.Infrastructure.Middleware;
using Devken.CBC.SchoolManagement.Infrastructure.Security;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Devken.CBC.SchoolManagement.Infrastructure.Services.Activities;
using Devken.CBC.SchoolManagement.Infrastructure.Services.RoleAssignment;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
            // HTTP Context Accessor (Required for CurrentUserService)
            // ══════════════════════════════════════════════════════════════
            services.AddHttpContextAccessor(); // ✨ ADD THIS

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
            // Authorization Configuration with CBC Policies
            // ══════════════════════════════════════════════════════════════
            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

                // ✨ CBC-Specific Authorization Policies

                // Super Admin policy - can do everything
                options.AddPolicy("SuperAdmin", policy =>
                    policy.RequireRole("SuperAdmin"));

                // School Admin policy - manages a specific school
                options.AddPolicy("SchoolAdmin", policy =>
                    policy.RequireRole("SchoolAdmin"));

                // Teacher policy - can manage classes and students
                options.AddPolicy("Teacher", policy =>
                    policy.RequireRole("Teacher"));

                // Parent policy - can view their children's information
                options.AddPolicy("Parent", policy =>
                    policy.RequireRole("Parent"));

                // Student Management Policies
                options.AddPolicy("Student.Read", policy =>
                    policy.Requirements.Add(new PermissionRequirement("Student.Read")));

                options.AddPolicy("Student.Write", policy =>
                    policy.Requirements.Add(new PermissionRequirement("Student.Write")));

                options.AddPolicy("Student.Delete", policy =>
                    policy.Requirements.Add(new PermissionRequirement("Student.Delete")));

                // Assessment Management Policies
                options.AddPolicy("Assessment.Read", policy =>
                    policy.Requirements.Add(new PermissionRequirement("Assessment.Read")));

                options.AddPolicy("Assessment.Write", policy =>
                    policy.Requirements.Add(new PermissionRequirement("Assessment.Write")));

                // Finance Management Policies
                options.AddPolicy("Finance.Read", policy =>
                    policy.Requirements.Add(new PermissionRequirement("Finance.Read")));

                options.AddPolicy("Finance.Write", policy =>
                    policy.Requirements.Add(new PermissionRequirement("Finance.Write")));

                // Report Management Policies
                options.AddPolicy("Report.Read", policy =>
                    policy.Requirements.Add(new PermissionRequirement("Report.Read")));

                options.AddPolicy("Report.Write", policy =>
                    policy.Requirements.Add(new PermissionRequirement("Report.Write")));

                // Settings Management Policies
                options.AddPolicy("Settings.Read", policy =>
                    policy.Requirements.Add(new PermissionRequirement("Settings.Read")));

                options.AddPolicy("Settings.Write", policy =>
                    policy.Requirements.Add(new PermissionRequirement("Settings.Write")));

                // Class Management Policies
                options.AddPolicy("Class.Read", policy =>
                    policy.Requirements.Add(new PermissionRequirement("Class.Read")));

                options.AddPolicy("Class.Write", policy =>
                    policy.Requirements.Add(new PermissionRequirement("Class.Write")));

                // Academic Year Management Policies
                options.AddPolicy("AcademicYear.Read", policy =>
                    policy.Requirements.Add(new PermissionRequirement("AcademicYear.Read")));

                options.AddPolicy("AcademicYear.Write", policy =>
                    policy.Requirements.Add(new PermissionRequirement("AcademicYear.Write")));

                // Grade Management Policies
                options.AddPolicy("Grade.Read", policy =>
                    policy.Requirements.Add(new PermissionRequirement("Grade.Read")));

                options.AddPolicy("Grade.Write", policy =>
                    policy.Requirements.Add(new PermissionRequirement("Grade.Write")));

                // Progress Report Policies
                options.AddPolicy("ProgressReport.Read", policy =>
                    policy.Requirements.Add(new PermissionRequirement("ProgressReport.Read")));

                options.AddPolicy("ProgressReport.Write", policy =>
                    policy.Requirements.Add(new PermissionRequirement("ProgressReport.Write")));

                // Invoice Management Policies
                options.AddPolicy("Invoice.Read", policy =>
                    policy.Requirements.Add(new PermissionRequirement("Invoice.Read")));

                options.AddPolicy("Invoice.Write", policy =>
                    policy.Requirements.Add(new PermissionRequirement("Invoice.Write")));

                // Payment Management Policies
                options.AddPolicy("Payment.Read", policy =>
                    policy.Requirements.Add(new PermissionRequirement("Payment.Read")));

                options.AddPolicy("Payment.Write", policy =>
                    policy.Requirements.Add(new PermissionRequirement("Payment.Write")));

                // Tenant Access Policy - ensures user has tenant ID
                options.AddPolicy("TenantAccess", policy =>
                    policy.Requirements.Add(new TenantAccessRequirement()));
            });

            // Register Authorization Handlers
            services.AddScoped<IAuthorizationHandler, PermissionHandler>();
            services.AddScoped<IAuthorizationHandler, RoleHandler>();
            services.AddScoped<IAuthorizationHandler, TenantAccessHandler>();

            // ══════════════════════════════════════════════════════════════
            // Repositories Registration
            // ══════════════════════════════════════════════════════════════

            // ✨ Academic Repositories
            services.AddScoped<IStudentRepository, StudentRepository>();
            //services.AddScoped<IClassRepository, ClassRepository>(); // Assuming this exists
            //services.AddScoped<IAcademicYearRepository, AcademicYearRepository>(); // Assuming this exists
            //services.AddScoped<ITeacherRepository, TeacherRepository>(); // Assuming this exists

            // Identity & Tenant Repositories
            services.AddScoped<ISchoolRepository, SchoolRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IPermissionRepository, PermissionRepository>();
            services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
            services.AddScoped<IUserRoleRepository, UserRoleRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<ISuperAdminRepository, SuperAdminRepository>();

            // ✨ Finance Repositories
            //services.AddScoped<IInvoiceRepository, InvoiceRepository>(); // Assuming this exists
            //services.AddScoped<IPaymentRepository, PaymentRepository>(); // Assuming this exists
            //services.AddScoped<IInvoiceItemRepository, InvoiceItemRepository>(); // Assuming this exists

            //// ✨ Assessment Repositories
            //services.AddScoped<IAssessmentRepository, AssessmentRepository>(); // Assuming this exists
            //services.AddScoped<IFormativeAssessmentRepository, FormativeAssessmentRepository>(); // Assuming this exists
            //services.AddScoped<ISummativeAssessmentRepository, SummativeAssessmentRepository>(); // Assuming this exists
            //services.AddScoped<ICompetencyAssessmentRepository, CompetencyAssessmentRepository>(); // Assuming this exists

            //// ✨ Report Repositories
            //services.AddScoped<IProgressReportRepository, ProgressReportRepository>(); // Assuming this exists

            // ══════════════════════════════════════════════════════════════
            // Lazy<T> Dependency Injection Support
            // ══════════════════════════════════════════════════════════════
            services.AddScoped(typeof(Lazy<>), typeof(LazyServiceProvider<>));

            // ══════════════════════════════════════════════════════════════
            // Core Services Registration
            // ══════════════════════════════════════════════════════════════

            // JWT & Security Services
            services.AddScoped<IJwtService, JwtService>();

            // Seed Services
            //services.AddScoped<ITenantSeedService, TenantSeedService>();
            services.AddScoped<IPermissionSeedService, PermissionSeedService>();
            services.AddScoped<ISubscriptionSeedService, SubscriptionSeedService>();

            // Current User Service
            services.AddScoped<ICurrentUserService, CurrentUserService>(); // ✨ ADD THIS

            // Subscription Services
            services.AddScoped<ISubscriptionService, SubscriptionService>();

            // ✨ Academic Services
            services.AddScoped<IStudentService, StudentService>();
            //services.AddScoped<IClassService, ClassService>(); // Assuming this exists
            //services.AddScoped<IAcademicYearService, AcademicYearService>(); // Assuming this exists
            //services.AddScoped<ITeacherService, TeacherService>(); // Assuming this exists

            // ✨ Finance Services
            //services.AddScoped<IInvoiceService, InvoiceService>(); // Assuming this exists
            //services.AddScoped<IPaymentService, PaymentService>(); // Assuming this exists

            // ✨ Assessment Services
            //services.AddScoped<IAssessmentService, AssessmentService>(); // Assuming this exists
            //services.AddScoped<IGradeService, GradeService>(); // Assuming this exists

            //// ✨ Report Services
            //services.AddScoped<IProgressReportService, ProgressReportService>(); // Assuming this exists

            // Identity & Authentication Services
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
            services.AddScoped<IAuthService, AuthService>();

            // Activity & Role Services
            services.AddScoped<IUserActivityService, UserActivityService>();
            services.AddScoped<IRoleAssignmentService, RoleAssignmentService>();

            // ══════════════════════════════════════════════════════════════
            // Repository Manager
            // ══════════════════════════════════════════════════════════════
            services.AddScoped<IRepositoryManager, RepositoryManager>();

            // ══════════════════════════════════════════════════════════════
            // Middleware Registration
            // ══════════════════════════════════════════════════════════════
            //services.AddScoped<TenantValidationMiddleware>();

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

    // ✨ Authorization Requirement Classes (if not in separate files)
    public class PermissionRequirement : Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }

    public class RoleRequirement : Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
    {
        public string Role { get; }

        public RoleRequirement(string role)
        {
            Role = role;
        }
    }

    public class TenantAccessRequirement : Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
    {
    }
}

// ✨ ADD THESE Authorization Handlers in a separate file or at the bottom
namespace Devken.CBC.SchoolManagement.Application.Authorization
{
    public class PermissionHandler : Microsoft.AspNetCore.Authorization.AuthorizationHandler<PermissionRequirement>
    {
        private readonly ICurrentUserService _currentUserService;

        public PermissionHandler(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        protected override System.Threading.Tasks.Task HandleRequirementAsync(
            Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            if (_currentUserService.IsSuperAdmin)
            {
                context.Succeed(requirement);
                return System.Threading.Tasks.Task.CompletedTask;
            }

            if (_currentUserService.HasPermission(requirement.Permission))
            {
                context.Succeed(requirement);
            }

            return System.Threading.Tasks.Task.CompletedTask;
        }
    }

    public class RoleHandler : Microsoft.AspNetCore.Authorization.AuthorizationHandler<RoleRequirement>
    {
        private readonly ICurrentUserService _currentUserService;

        public RoleHandler(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        protected override System.Threading.Tasks.Task HandleRequirementAsync(
            Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext context,
            RoleRequirement requirement)
        {
            if (_currentUserService.IsSuperAdmin)
            {
                context.Succeed(requirement);
                return System.Threading.Tasks.Task.CompletedTask;
            }

            if (_currentUserService.IsInRole(requirement.Role))
            {
                context.Succeed(requirement);
            }

            return System.Threading.Tasks.Task.CompletedTask;
        }
    }

    public class TenantAccessHandler : Microsoft.AspNetCore.Authorization.AuthorizationHandler<TenantAccessRequirement>
    {
        private readonly ICurrentUserService _currentUserService;

        public TenantAccessHandler(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        protected override System.Threading.Tasks.Task HandleRequirementAsync(
            Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext context,
            TenantAccessRequirement requirement)
        {
            if (_currentUserService.IsSuperAdmin)
            {
                context.Succeed(requirement);
                return System.Threading.Tasks.Task.CompletedTask;
            }

            if (_currentUserService.TenantId.HasValue && _currentUserService.IsAuthenticated)
            {
                context.Succeed(requirement);
            }

            return System.Threading.Tasks.Task.CompletedTask;
        }
    }
}