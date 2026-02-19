using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessment;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Entities.NumberSeries;
using Devken.CBC.SchoolManagement.Domain.Entities.Payments;
using Devken.CBC.SchoolManagement.Domain.Entities.Reports;
using Devken.CBC.SchoolManagement.Domain.Entities.Subscription;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Academic;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Finance;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Payments;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Reports;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.SchoolConf;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Subscription;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Conventions;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        #region DbSets

        // ── Identity & Admin ────────────────────────────────────────────
        public DbSet<School> Schools => Set<School>();
        public DbSet<User> Users { get; set; }
        public DbSet<SuperAdmin> SuperAdmins => Set<SuperAdmin>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<SuperAdminRefreshToken> SuperAdminRefreshTokens => Set<SuperAdminRefreshToken>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<UserActivity> UserActivities => Set<UserActivity>();

        // ── Academic ────────────────────────────────────────────────────
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Teacher> Teachers => Set<Teacher>();
        public DbSet<Class> Classes => Set<Class>();
        public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
        public DbSet<Term> Terms => Set<Term>();
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<Parent> Parents => Set<Parent>();
        public DbSet<LearningOutcome> LearningOutcomes => Set<LearningOutcome>();

        // ── Assessments ─────────────────────────────────────────────────
        // TPH: Assessment1 is the root — FormativeAssessment, SummativeAssessment,
        // and CompetencyAssessment are all stored in the same Assessments table,
        // distinguished by the AssessmentType discriminator column.
        // Do NOT add separate DbSets for derived assessment types.
        public DbSet<Assessment1> Assessments => Set<Assessment1>();
        public DbSet<Grade> Grades => Set<Grade>();

        // Score tables are separate entities (not TPH), so each has its own DbSet.
        public DbSet<FormativeAssessmentScore> FormativeAssessmentScores => Set<FormativeAssessmentScore>();
        public DbSet<SummativeAssessmentScore> SummativeAssessmentScores => Set<SummativeAssessmentScore>();
        public DbSet<CompetencyAssessmentScore> CompetencyAssessmentScores => Set<CompetencyAssessmentScore>();

        // ── Reports ─────────────────────────────────────────────────────
        public DbSet<ProgressReport> ProgressReports => Set<ProgressReport>();
        public DbSet<SubjectReport> SubjectReports => Set<SubjectReport>();
        public DbSet<ProgressReportComment> ProgressReportComments => Set<ProgressReportComment>();

        // ── Finance ─────────────────────────────────────────────────────
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<FeeItem> FeeItems => Set<FeeItem>();
        public DbSet<SubscriptionPlanEntity> SubscriptionPlans { get; set; }
        public DbSet<MpesaPaymentRecord> MpesaPayments { get; set; }
        public DbSet<TeacherCBCLevel> TeacherCBCLevels { get; set; } = null!;
        public DbSet<DocumentNumberSeries> DocumentNumberSeries => Set<DocumentNumberSeries>();

        #endregion

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            // ── GLOBAL CONVENTIONS ───────────────────────────────────────
            DecimalPrecisionConvention.Apply(mb);

            // ── GENERIC BASE ENTITY KEY CONFIGURATION ───────────────────
            foreach (var entityType in mb.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity<Guid>).IsAssignableFrom(entityType.ClrType))
                {
                    // Skip derived types in TPH hierarchy (they share the root's key)
                    if (entityType.BaseType == null)
                    {
                        mb.Entity(entityType.ClrType).HasKey("Id");
                    }
                }
            }

            // ── RELATIONSHIPS NOT COVERED BY ENTITY CONFIGURATIONS ───────

            mb.Entity<Teacher>()
                .HasOne(t => t.CurrentClass)
                .WithMany()
                .HasForeignKey(t => t.CurrentClassId)
                .OnDelete(DeleteBehavior.Restrict);

            mb.Entity<FormativeAssessment>()
                .HasOne(f => f.LearningOutcome)
                .WithMany(lo => lo.FormativeAssessments)
                .HasForeignKey(f => f.LearningOutcomeId)
                .OnDelete(DeleteBehavior.Restrict);

            mb.Entity<SuperAdminRefreshToken>()
                .HasOne(t => t.SuperAdmin)
                .WithMany()
                .HasForeignKey(t => t.SuperAdminId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── SUMMATIVE ASSESSMENT SCORE — relationships & computed ────
            //
            // The SummativeAssessmentScoreConfiguration handles column mapping,
            // but we wire the FK relationships here to keep them visible alongside
            // the other explicit relationship definitions above.

            mb.Entity<SummativeAssessmentScore>(entity =>
            {
                // FK → SummativeAssessment (the TPH-stored parent)
                entity.HasOne(s => s.SummativeAssessment)
                      .WithMany(a => a.Scores)
                      .HasForeignKey(s => s.SummativeAssessmentId)
                      .OnDelete(DeleteBehavior.Cascade);

                // FK → Student
                entity.HasOne(s => s.Student)
                      .WithMany()
                      .HasForeignKey(s => s.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);

                // FK → Teacher (grader)
                entity.HasOne(s => s.GradedBy)
                      .WithMany()
                      .HasForeignKey(s => s.GradedById)
                      .OnDelete(DeleteBehavior.Restrict);

                // Computed properties — never mapped to columns
                entity.Ignore(s => s.TotalScore);
                entity.Ignore(s => s.MaximumTotalScore);
                entity.Ignore(s => s.Percentage);
                entity.Ignore(s => s.PerformanceStatus);
            });

            // ── APPLY ENTITY CONFIGURATIONS ─────────────────────────────

            // Identity
            mb.ApplyConfiguration(new SchoolConfiguration());
            mb.ApplyConfiguration(new PermissionConfiguration());
            mb.ApplyConfiguration(new RoleConfiguration(_tenantContext));
            mb.ApplyConfiguration(new UserConfiguration(_tenantContext));
            mb.ApplyConfiguration(new RolePermissionConfiguration(_tenantContext));
            mb.ApplyConfiguration(new UserRoleConfiguration(_tenantContext));
            mb.ApplyConfiguration(new RefreshTokenConfiguration(_tenantContext));
            mb.ApplyConfiguration(new SubscriptionConfiguration(_tenantContext));

            // Academic
            mb.ApplyConfiguration(new StudentConfiguration(_tenantContext));
            mb.ApplyConfiguration(new TeacherConfiguration(_tenantContext));
            mb.ApplyConfiguration(new ClassConfiguration(_tenantContext));
            mb.ApplyConfiguration(new AcademicYearConfiguration(_tenantContext));
            mb.ApplyConfiguration(new TermConfiguration(_tenantContext));
            mb.ApplyConfiguration(new SubjectConfiguration(_tenantContext));
            mb.ApplyConfiguration(new ParentConfiguration(_tenantContext));
            mb.ApplyConfiguration(new LearningOutcomeConfiguration(_tenantContext));

            // Grades
            mb.ApplyConfiguration(new GradeConfiguration(_tenantContext));

            // Assessments — TPH root first, then each discriminated type
            mb.ApplyConfiguration(new AssessmentConfiguration(_tenantContext));
            mb.ApplyConfiguration(new FormativeAssessmentConfiguration());
            mb.ApplyConfiguration(new SummativeAssessmentConfiguration());         // ← registers SummativeAssessment TPH columns
            mb.ApplyConfiguration(new CompetencyAssessmentConfiguration());

            // Assessment scores
            mb.ApplyConfiguration(new FormativeAssessmentScoreConfiguration());
            mb.ApplyConfiguration(new SummativeAssessmentScoreConfiguration(_tenantContext)); // ← registers SummativeAssessmentScore table
            mb.ApplyConfiguration(new CompetencyAssessmentScoreConfiguration());

            // Reports
            mb.ApplyConfiguration(new ProgressReportConfiguration(_tenantContext));
            mb.ApplyConfiguration(new SubjectReportConfiguration(_tenantContext));
            mb.ApplyConfiguration(new ProgressReportCommentConfiguration(_tenantContext));

            // Finance
            mb.ApplyConfiguration(new InvoiceConfiguration(_tenantContext));
            mb.ApplyConfiguration(new InvoiceItemConfiguration(_tenantContext));
            mb.ApplyConfiguration(new PaymentConfiguration(_tenantContext));
            mb.ApplyConfiguration(new FeeItemConfiguration(_tenantContext));

            // Payments & misc
            mb.ApplyConfiguration(new MpesaPaymentRecordConfiguration1());
            mb.ApplyConfiguration(new SubscriptionPlanConfiguration());
            mb.ApplyConfiguration(new TeacherCBCLevelConfiguration(_tenantContext));
            mb.ApplyConfiguration(new DocumentNumberSeriesConfiguration(_tenantContext));
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditInformation();
            UpdateTenantEntities();
            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            ApplyAuditInformation();
            UpdateTenantEntities();
            return base.SaveChanges();
        }

        private void ApplyAuditInformation()
        {
            var now = DateTime.UtcNow;
            var userId = _tenantContext?.ActingUserId;

            foreach (var entry in ChangeTracker.Entries())
            {
                // Handle BaseEntity<Guid>
                if (entry.Entity is BaseEntity<Guid> baseEntity)
                {
                    if (entry.State == EntityState.Added)
                    {
                        if (baseEntity.Id == null || baseEntity.Id == Guid.Empty)
                            baseEntity.Id = Guid.NewGuid();

                        baseEntity.CreatedOn = now;
                        baseEntity.UpdatedOn = now;
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        baseEntity.UpdatedOn = now;
                    }
                }

                // Handle IAuditableEntity
                if (entry.Entity is IAuditableEntity auditable &&
                    (entry.State == EntityState.Added || entry.State == EntityState.Modified))
                {
                    if (entry.State == EntityState.Added)
                    {
                        auditable.CreatedOn = now;
                        auditable.CreatedBy = userId;
                    }

                    auditable.UpdatedOn = now;
                    auditable.UpdatedBy = userId;
                }
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
                    entity.TenantId = _tenantContext.TenantId.Value;
            }
        }
    }
}