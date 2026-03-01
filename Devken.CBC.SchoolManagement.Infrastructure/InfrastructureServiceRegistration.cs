using Devken.CBC.SchoolManagement.Application.Authorization;
using Devken.CBC.SchoolManagement.Application.DTOs.userActivities;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academics;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Assessments;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Finance;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Identity;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.NumberSeries;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Payments;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Reports;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Tenant;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.UserActivities1;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Application.Service.Academics;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.Administration.Student;
using Devken.CBC.SchoolManagement.Application.Service.Assessments;
using Devken.CBC.SchoolManagement.Application.Service.Curriculum;
using Devken.CBC.SchoolManagement.Application.Service.Finance;
using Devken.CBC.SchoolManagement.Application.Service.IRolesAssignment;
using Devken.CBC.SchoolManagement.Application.Service.Isubscription;
using Devken.CBC.SchoolManagement.Application.Service.ISubscription;
using Devken.CBC.SchoolManagement.Application.Service.Navigation;
using Devken.CBC.SchoolManagement.Application.Service.Subscription;
using Devken.CBC.SchoolManagement.Application.Services.Implementations.Academic;
using Devken.CBC.SchoolManagement.Application.Services.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Application.Services.Interfaces.Images;
using Devken.CBC.SchoolManagement.Application.Services.UserManagement;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Academic;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Academics;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Common;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Curriculum;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Finance;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.NumberSeries;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Payments;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Tenant;
using Devken.CBC.SchoolManagement.Infrastructure.RepositoryManagers.UserActivities;
using Devken.CBC.SchoolManagement.Infrastructure.Security;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Devken.CBC.SchoolManagement.Infrastructure.Services.Academics;
using Devken.CBC.SchoolManagement.Infrastructure.Services.Administration.Students;
using Devken.CBC.SchoolManagement.Infrastructure.Services.Curriculum;
using Devken.CBC.SchoolManagement.Infrastructure.Services.Finance;
using Devken.CBC.SchoolManagement.Infrastructure.Services.Images;
using Devken.CBC.SchoolManagement.Infrastructure.Services.Reports;
using Devken.CBC.SchoolManagement.Infrastructure.Services.RoleAssignment;
using Devken.CBC.SchoolManagement.Infrastructure.Services.UserManagement;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System;
using System.Collections.Generic;
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
            // ── JWT Settings ─────────────────────────────────────────────────
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtSettings>>().Value);
            services.AddHttpContextAccessor();
            services.AddHttpClient();

            var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();

            if (jwtSettings == null)
                throw new InvalidOperationException("JwtSettings configuration is missing in appsettings.json");
            if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
                throw new InvalidOperationException("JWT SecretKey is not configured in appsettings.json");
            if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
                throw new InvalidOperationException("JWT Issuer is not configured in appsettings.json");
            if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
                throw new InvalidOperationException("JWT Audience is not configured in appsettings.json");

            // ── Authentication ───────────────────────────────────────────────
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
                        Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
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
                        var logger = context.HttpContext.RequestServices
                            .GetService<ILogger<Program>>();
                        if (logger != null)
                        {
                            logger.LogError(context.Exception,
                                "JWT Authentication failed: {Message}", context.Exception.Message);
                            if (context.Exception is SecurityTokenExpiredException)
                                context.Response.Headers.Append("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetService<ILogger<Program>>();
                        if (logger != null)
                        {
                            var userId = context.Principal?
                                .FindFirst("user_id")?.Value ?? "Unknown";
                            var email = context.Principal?
                                .FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")
                                ?.Value ?? "Unknown";
                            logger.LogInformation(
                                "JWT token validated for user: {UserId} ({Email})", userId, email);
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            // ── Authorization ────────────────────────────────────────────────
            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

                // ── Role-based policies ──────────────────────────────────────
                options.AddPolicy("SuperAdmin", policy => policy.RequireRole("SuperAdmin"));
                options.AddPolicy("SchoolAdmin", policy => policy.RequireRole("SchoolAdmin"));
                options.AddPolicy("Teacher", policy => policy.RequireRole("Teacher"));
                options.AddPolicy("Parent", policy => policy.RequireRole("Parent"));

                // ── Settings / Configuration permissions ─────────────────────
                RegisterPermissionPolicy(options, PermissionKeys.DocumentNumberSeriesRead);
                RegisterPermissionPolicy(options, PermissionKeys.DocumentNumberSeriesWrite);
                RegisterPermissionPolicy(options, PermissionKeys.DocumentNumberSeriesDelete);

                // ── Administration permissions ───────────────────────────────
                RegisterPermissionPolicy(options, PermissionKeys.SchoolRead);
                RegisterPermissionPolicy(options, PermissionKeys.SchoolWrite);
                RegisterPermissionPolicy(options, PermissionKeys.SchoolDelete);
                RegisterPermissionPolicy(options, PermissionKeys.UserRead);
                RegisterPermissionPolicy(options, PermissionKeys.UserWrite);
                RegisterPermissionPolicy(options, PermissionKeys.UserDelete);
                RegisterPermissionPolicy(options, PermissionKeys.RoleRead);
                RegisterPermissionPolicy(options, PermissionKeys.RoleWrite);
                RegisterPermissionPolicy(options, PermissionKeys.RoleDelete);

                // ── Academic Year permissions ────────────────────────────────
                RegisterPermissionPolicy(options, PermissionKeys.AcademicYearRead);
                RegisterPermissionPolicy(options, PermissionKeys.AcademicYearWrite);
                RegisterPermissionPolicy(options, PermissionKeys.AcademicYearDelete);
                RegisterPermissionPolicy(options, PermissionKeys.AcademicYearClose);

                // ── Term permissions ─────────────────────────────────────────
                RegisterPermissionPolicy(options, PermissionKeys.TermRead);
                RegisterPermissionPolicy(options, PermissionKeys.TermWrite);
                RegisterPermissionPolicy(options, PermissionKeys.TermDelete);

                // ── Academic permissions ─────────────────────────────────────
                RegisterPermissionPolicy(options, PermissionKeys.StudentRead);
                RegisterPermissionPolicy(options, PermissionKeys.StudentWrite);
                RegisterPermissionPolicy(options, PermissionKeys.StudentDelete);
                RegisterPermissionPolicy(options, PermissionKeys.TeacherRead);
                RegisterPermissionPolicy(options, PermissionKeys.TeacherWrite);
                RegisterPermissionPolicy(options, PermissionKeys.TeacherDelete);
                RegisterPermissionPolicy(options, PermissionKeys.SubjectRead);
                RegisterPermissionPolicy(options, PermissionKeys.SubjectWrite);
                RegisterPermissionPolicy(options, PermissionKeys.SubjectDelete);
                RegisterPermissionPolicy(options, PermissionKeys.ClassRead);
                RegisterPermissionPolicy(options, PermissionKeys.ClassWrite);
                RegisterPermissionPolicy(options, PermissionKeys.GradeRead);
                RegisterPermissionPolicy(options, PermissionKeys.GradeWrite);

                // ── Parent permissions ───────────────────────────────────────
                RegisterPermissionPolicy(options, PermissionKeys.ParentRead);
                RegisterPermissionPolicy(options, PermissionKeys.ParentWrite);
                RegisterPermissionPolicy(options, PermissionKeys.ParentDelete);

                // ── Assessment permissions ───────────────────────────────────
                RegisterPermissionPolicy(options, PermissionKeys.AssessmentRead);
                RegisterPermissionPolicy(options, PermissionKeys.AssessmentWrite);
                RegisterPermissionPolicy(options, PermissionKeys.AssessmentDelete);
                RegisterPermissionPolicy(options, PermissionKeys.ReportRead);
                RegisterPermissionPolicy(options, PermissionKeys.ReportWrite);

                // ── Finance — Broad permissions ──────────────────────────────
                RegisterPermissionPolicy(options, PermissionKeys.FinanceRead);
                RegisterPermissionPolicy(options, PermissionKeys.FinanceWrite);

                // ── Finance — Granular permissions ───────────────────────────
                RegisterPermissionPolicy(options, PermissionKeys.FeeRead);
                RegisterPermissionPolicy(options, PermissionKeys.FeeWrite);
                RegisterPermissionPolicy(options, PermissionKeys.PaymentRead);
                RegisterPermissionPolicy(options, PermissionKeys.PaymentWrite);
                RegisterPermissionPolicy(options, PermissionKeys.InvoiceRead);
                RegisterPermissionPolicy(options, PermissionKeys.InvoiceWrite);
             //   RegisterPermissionPolicy(options, PermissionKeys.InvoiceDelete);  // ← NEW

                // ── Finance — Fee Structure permissions ──────────────────────
                RegisterPermissionPolicy(options, PermissionKeys.FeeStructureRead);
                RegisterPermissionPolicy(options, PermissionKeys.FeeStructureWrite);
                RegisterPermissionPolicy(options, PermissionKeys.FeeStructureDelete);

                // ── Curriculum permissions ───────────────────────────────────
                RegisterPermissionPolicy(options, PermissionKeys.CurriculumRead);
                RegisterPermissionPolicy(options, PermissionKeys.CurriculumWrite);
                RegisterPermissionPolicy(options, PermissionKeys.LessonPlanRead);
                RegisterPermissionPolicy(options, PermissionKeys.LessonPlanWrite);

                // ── M-Pesa permissions ───────────────────────────────────────
                RegisterPermissionPolicy(options, PermissionKeys.MpesaInitiate);
                RegisterPermissionPolicy(options, PermissionKeys.MpesaViewTransactions);
                RegisterPermissionPolicy(options, PermissionKeys.MpesaRefund);
                RegisterPermissionPolicy(options, PermissionKeys.MpesaReconcile);

                // ── Convenience / composite policies ────────────────────────
                options.AddPolicy("Roles.View", policy =>
                    policy.Requirements.Add(new PermissionRequirement(PermissionKeys.RoleRead)));

                options.AddPolicy("Roles.AssignPermissions", policy =>
                    policy.Requirements.Add(new PermissionRequirement(PermissionKeys.RoleWrite)));

                // Tenant access policy
                options.AddPolicy("TenantAccess", policy =>
                    policy.Requirements.Add(new TenantAccessRequirement()));

                // ── Legacy / backward-compat permissions ─────────────────────
                var legacyPermissions = new List<string>
                {
                    "Student.Read",    "Student.Write",    "Student.Delete",
                    "Assessment.Read", "Assessment.Write",
                    "Finance.Read",    "Finance.Write",
                    "Report.Read",     "Report.Write",
                    "Settings.Read",   "Settings.Write",
                    "Class.Read",      "Class.Write",
                    "AcademicYear.Read", "AcademicYear.Write",
                    "Subject.Read",    "Subject.Write",    "Subject.Delete",
                    "Grade.Read",      "Grade.Write",
                    "ProgressReport.Read", "ProgressReport.Write",
                    "Invoice.Read",    "Invoice.Write",
                    "Payment.Read",    "Payment.Write",
                    "Parent.Read",     "Parent.Write",     "Parent.Delete",  // ← NEW
                };

                foreach (var permission in legacyPermissions)
                {
                    if (!options.GetPolicy(permission)?.Requirements.Any() ?? true)
                        RegisterPermissionPolicy(options, permission);
                }
            });

            // ── Authorization Handlers ───────────────────────────────────────
            services.AddScoped<IAuthorizationHandler, PermissionHandler>();
            services.AddScoped<IAuthorizationHandler, RoleHandler>();
            services.AddScoped<IAuthorizationHandler, TenantAccessHandler>();

            // ── Infrastructure Services ──────────────────────────────────────
            services.AddScoped<IPasswordHashingService, BCryptPasswordHashingService>();
            services.AddMemoryCache();
            services.AddScoped(typeof(Lazy<>), typeof(LazyServiceProvider<>));

            // ── Repository Manager (unit of work) ────────────────────────────
            services.AddScoped<IRepositoryManager, RepositoryManager>();

            // ── Individual Repositories ──────────────────────────────────────
            // (Kept for code that resolves them directly rather than via IRepositoryManager)

            // Academic Repositories
            services.AddScoped<IParentRepository, ParentRepository>();        // ← NEW
            services.AddScoped<IStudentRepository, StudentRepository>();
            services.AddScoped<ITeacherRepository, TeacherRepository>();
            services.AddScoped<ISchoolRepository, SchoolRepository>();

            // Identity Repositories
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IPermissionRepository, PermissionRepository>();
            services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
            services.AddScoped<IUserRoleRepository, UserRoleRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<ISuperAdminRepository, SuperAdminRepository>();

            // Other Academic Repositories
            services.AddScoped<ISubjectRepository, SubjectRepository>();
            services.AddScoped<IGradeRepository, GradeRepository>();
            services.AddScoped<IUserActivityRepository, UserActivityRepository>();

            // Payment Repositories
            services.AddScoped<IMpesaPaymentRepository, MpesaPaymentRepository>();

            // Number Series
            services.AddScoped<IDocumentNumberSeriesRepository, DocumentNumberService>();

            // Finance Repositories
            services.AddScoped<IFeeItemRepository, FeeItemRepository>();
            services.AddScoped<IFeeStructureRepository, FeeStructureRepository>();
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();      // ← NEW

            // Assessment Repositories
            services.AddScoped<IFormativeAssessmentRepository, FormativeAssessmentRepository>();
            services.AddScoped<IFormativeAssessmentScoreRepository, FormativeAssessmentScoreRepository>();
            services.AddScoped<ISummativeAssessmentRepository, SummativeAssessmentRepository>();
            services.AddScoped<ISummativeAssessmentScoreRepository, SummativeAssessmentScoreRepository>();
            services.AddScoped<ICompetencyAssessmentRepository, CompetencyAssessmentRepository>();
            services.AddScoped<ICompetencyAssessmentScoreRepository, CompetencyAssessmentScoreRepository>();

            // ── Application Services ─────────────────────────────────────────
            services.AddScoped<IStudentService, StudentService>();
            services.AddScoped<ITeacherService, TeacherService>();
            services.AddScoped<ITermService, TermService>();
            services.AddScoped<ISubjectService, SubjectService>();
            services.AddScoped<IGradeService, GradeService>();
            services.AddScoped<ILearningAreaService, LearningAreaService>();
            services.AddScoped<IStrandService, StrandService>();
            services.AddScoped<ISubStrandService, SubStrandService>();
            services.AddScoped<ILearningOutcomeService, LearningOutcomeService>();
            services.AddScoped<IAssessmentService, AssessmentService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IUserManagementService, UserManagementService>();
            services.AddScoped<IFeeItemService, FeeItemService>();
            //services.AddScoped<IFeeStructureService, FeeStructureService>();  // ← NEW
            services.AddScoped<IInvoiceService, InvoiceService>();            // ← NEW
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IPermissionSeedService, PermissionSeedService>();
            services.AddScoped<ISubscriptionSeedService, SubscriptionSeedService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<ISubscriptionService, SubscriptionService>();
            services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserActivityService, UserActivityService>();
            services.AddScoped<IUserActivityService1, UserActivityService1>();
            services.AddScoped<IRoleAssignmentService, RoleAssignmentService>();
            services.AddScoped<INavigationService, NavigationService>();
            services.AddScoped<IImageUploadService, ImageUploadService>();

            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

            return services;
        }

        /// <summary>
        /// Registers a permission string as an ASP.NET Core authorization policy
        /// backed by a <see cref="PermissionRequirement"/>.
        /// </summary>
        private static void RegisterPermissionPolicy(
            AuthorizationOptions options,
            string permissionKey)
        {
            options.AddPolicy(permissionKey, policy =>
                policy.Requirements.Add(new PermissionRequirement(permissionKey)));
        }
    }

    public sealed class LazyServiceProvider<T> : Lazy<T> where T : class
    {
        public LazyServiceProvider(IServiceProvider provider)
            : base(() => provider.GetRequiredService<T>()) { }
    }
}