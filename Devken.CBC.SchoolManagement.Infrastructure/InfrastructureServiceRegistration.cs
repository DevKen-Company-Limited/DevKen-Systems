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
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
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
            // JWT
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtSettings>>().Value);
            services.AddHttpContextAccessor();
            services.AddHttpClient();

            var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
                ?? throw new InvalidOperationException("JwtSettings missing.");

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
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
                        ClockSkew = TimeSpan.Zero
                    };
                });

            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

                RegisterPermissionPolicy(options, PermissionKeys.FinanceRead);
                RegisterPermissionPolicy(options, PermissionKeys.FinanceWrite);
                RegisterPermissionPolicy(options, PermissionKeys.FeeStructureRead);
                RegisterPermissionPolicy(options, PermissionKeys.FeeStructureWrite);
                RegisterPermissionPolicy(options, PermissionKeys.FeeStructureDelete);
            });

            services.AddScoped<IAuthorizationHandler, PermissionHandler>();
            services.AddScoped<IAuthorizationHandler, RoleHandler>();
            services.AddScoped<IAuthorizationHandler, TenantAccessHandler>();

            services.AddScoped<IRepositoryManager, RepositoryManager>();

            // Finance Repositories
            services.AddScoped<IFeeItemRepository, FeeItemRepository>();
            services.AddScoped<IFeeStructureRepository, FeeStructureRepository>();

            // Assessment Repositories
            services.AddScoped<IFormativeAssessmentRepository, FormativeAssessmentRepository>();
            services.AddScoped<IFormativeAssessmentScoreRepository, FormativeAssessmentScoreRepository>();
            services.AddScoped<ISummativeAssessmentRepository, SummativeAssessmentRepository>();
            services.AddScoped<ISummativeAssessmentScoreRepository, SummativeAssessmentScoreRepository>();
            services.AddScoped<ICompetencyAssessmentRepository, CompetencyAssessmentRepository>();
            services.AddScoped<ICompetencyAssessmentScoreRepository, CompetencyAssessmentScoreRepository>();

            // Application Services
            services.AddScoped<IStudentService, StudentService>();
            services.AddScoped<ITeacherService, TeacherService>();
            services.AddScoped<ITermService, TermService>();
            services.AddScoped<ISubjectService, SubjectService>();
            services.AddScoped<IGradeService, GradeService>();
            services.AddScoped<IAssessmentService, AssessmentService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IFeeItemService, FeeItemService>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IUserManagementService, UserManagementService>();

            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

            return services;
        }

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