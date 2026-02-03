using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessment;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Entities.Reports;
using Devken.CBC.SchoolManagement.Domain.Entities.Subscription;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Academic;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Finance;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Reports;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.SchoolConf;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Subscription;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF
{
    public class AppDbContext : DbContext
    {
        private readonly TenantContext _tenantContext;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            TenantContext tenantContext,
            IPasswordHasher<User> passwordHasher)
            : base(options)
        {
            _tenantContext = tenantContext;
            _passwordHasher = passwordHasher;
        }

        #region DbSets - Identity & Administration
        public DbSet<School> Schools => Set<School>();
        public DbSet<User> Users => Set<User>();
        public DbSet<SuperAdmin> SuperAdmins => Set<SuperAdmin>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<SuperAdminRefreshToken> SuperAdminRefreshTokens => Set<SuperAdminRefreshToken>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<UserActivity> UserActivities => Set<UserActivity>();
        #endregion

        #region DbSets - Academic
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Teacher> Teachers => Set<Teacher>();
        public DbSet<Class> Classes => Set<Class>();
        public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
        public DbSet<Term> Terms => Set<Term>();
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<Parent> Parents => Set<Parent>();
        public DbSet<LearningOutcome> LearningOutcomes => Set<LearningOutcome>();
        #endregion

        #region DbSets - Assessments
        public DbSet<Grade> Grades => Set<Grade>();
        public DbSet<Assessment1> Assessments => Set<Assessment1>();
        public DbSet<FormativeAssessment> FormativeAssessments => Set<FormativeAssessment>();
        public DbSet<SummativeAssessment> SummativeAssessments => Set<SummativeAssessment>();
        public DbSet<CompetencyAssessment> CompetencyAssessments => Set<CompetencyAssessment>();
        public DbSet<FormativeAssessmentScore> FormativeAssessmentScores => Set<FormativeAssessmentScore>();
        public DbSet<SummativeAssessmentScore> SummativeAssessmentScores => Set<SummativeAssessmentScore>();
        public DbSet<CompetencyAssessmentScore> CompetencyAssessmentScores => Set<CompetencyAssessmentScore>();
        #endregion

        #region DbSets - Reports
        public DbSet<ProgressReport> ProgressReports => Set<ProgressReport>();
        public DbSet<SubjectReport> SubjectReports => Set<SubjectReport>();
        public DbSet<ProgressReportComment> ProgressReportComments => Set<ProgressReportComment>();
        #endregion

        #region DbSets - Finance
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<FeeItem> FeeItems => Set<FeeItem>();
        #endregion

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            // Configure SuperAdminRefreshToken relationship
            mb.Entity<SuperAdminRefreshToken>()
               .HasOne(t => t.SuperAdmin)
               .WithMany()
               .HasForeignKey(t => t.SuperAdminId)
               .OnDelete(DeleteBehavior.Cascade);

            // ── APPLY CONFIGURATIONS ─────────────────────────────

            // Identity & Administration Configurations
            mb.ApplyConfiguration(new SchoolConfiguration());
            mb.ApplyConfiguration(new PermissionConfiguration());
            mb.ApplyConfiguration(new RoleConfiguration(_tenantContext));
            mb.ApplyConfiguration(new UserConfiguration(_tenantContext));
            mb.ApplyConfiguration(new RolePermissionConfiguration(_tenantContext));
            mb.ApplyConfiguration(new UserRoleConfiguration(_tenantContext));
            mb.ApplyConfiguration(new RefreshTokenConfiguration(_tenantContext));
            mb.ApplyConfiguration(new SubscriptionConfiguration(_tenantContext));

            // Academic Configurations
            mb.ApplyConfiguration(new StudentConfiguration(_tenantContext));
            mb.ApplyConfiguration(new TeacherConfiguration(_tenantContext));
            mb.ApplyConfiguration(new ClassConfiguration(_tenantContext));
            mb.ApplyConfiguration(new AcademicYearConfiguration(_tenantContext));
            mb.ApplyConfiguration(new TermConfiguration(_tenantContext));
            mb.ApplyConfiguration(new SubjectConfiguration(_tenantContext));
            mb.ApplyConfiguration(new ParentConfiguration(_tenantContext));
            mb.ApplyConfiguration(new LearningOutcomeConfiguration(_tenantContext));

            // Assessment Configurations
            mb.ApplyConfiguration(new GradeConfiguration(_tenantContext));
            mb.ApplyConfiguration(new AssessmentConfiguration(_tenantContext));
      


            mb.ApplyConfiguration(new SummativeAssessmentScoreConfiguration(_tenantContext));
          





            // Root entity with tenant filter
            mb.ApplyConfiguration(new AssessmentConfiguration(_tenantContext));

            // Derived entities (no tenant filter in constructor)
            mb.ApplyConfiguration(new SummativeAssessmentConfiguration());
            mb.ApplyConfiguration(new FormativeAssessmentConfiguration());
            mb.ApplyConfiguration(new CompetencyAssessmentConfiguration());

            // Score entities (child entities, no tenant filter)
            mb.ApplyConfiguration(new FormativeAssessmentScoreConfiguration());
            mb.ApplyConfiguration(new CompetencyAssessmentScoreConfiguration());

            // Report Configurations
            mb.ApplyConfiguration(new ProgressReportConfiguration(_tenantContext));
            mb.ApplyConfiguration(new SubjectReportConfiguration(_tenantContext));
            mb.ApplyConfiguration(new ProgressReportCommentConfiguration(_tenantContext));

            // Finance Configurations
            mb.ApplyConfiguration(new InvoiceConfiguration(_tenantContext));
            mb.ApplyConfiguration(new InvoiceItemConfiguration(_tenantContext));
            mb.ApplyConfiguration(new PaymentConfiguration(_tenantContext));
            mb.ApplyConfiguration(new FeeItemConfiguration(_tenantContext));

            // Note: Do NOT seed dynamic data like Users or hashed passwords here.
            // All dynamic seeding will be handled via PermissionSeedService and app startup.
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditableEntities();
            UpdateTenantEntities();
            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            UpdateAuditableEntities();
            UpdateTenantEntities();
            return base.SaveChanges();
        }

        private void UpdateAuditableEntities()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is IAuditableEntity &&
                    (e.State == EntityState.Added || e.State == EntityState.Modified));

            var now = DateTime.UtcNow;
            var userId = _tenantContext?.ActingUserId;

            foreach (var entry in entries)
            {
                var entity = (IAuditableEntity)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedOn = now;
                    entity.CreatedBy = userId;
                }

                entity.UpdatedOn = now;
                entity.UpdatedBy = userId;
            }
        }

        private void UpdateTenantEntities()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is ITenantEntity && e.State == EntityState.Added);

            foreach (var entry in entries)
            {
                var entity = (ITenantEntity)entry.Entity;

                if (entity.TenantId == Guid.Empty && _tenantContext?.TenantId != null)
                {
                    entity.TenantId = _tenantContext.TenantId.Value;
                }
            }
        }
    }
}