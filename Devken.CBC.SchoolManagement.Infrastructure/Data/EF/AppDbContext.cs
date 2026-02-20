using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
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
using Devken.CBC.SchoolManagement.Infrastructure.Persistence.Configurations;
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

        // ── CBC Curriculum Helpers ───────────────────────────────────────
        public DbSet<LearningArea> LearningAreas => Set<LearningArea>();
        public DbSet<Strand> Strands => Set<Strand>();
        public DbSet<SubStrand> SubStrands => Set<SubStrand>();
        public DbSet<LearningOutcome> LearningOutcomes => Set<LearningOutcome>();

        // ── Assessments (TPT) ───────────────────────────────────────────
        public DbSet<Assessment1> Assessments => Set<Assessment1>();
        public DbSet<FormativeAssessment> FormativeAssessments => Set<FormativeAssessment>();
        public DbSet<SummativeAssessment> SummativeAssessments => Set<SummativeAssessment>();
        public DbSet<CompetencyAssessment> CompetencyAssessments => Set<CompetencyAssessment>();

        public DbSet<Grade> Grades => Set<Grade>();

        // ── Assessment Scores ───────────────────────────────────────────
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
                if (typeof(BaseEntity<Guid>).IsAssignableFrom(entityType.ClrType)
                    && entityType.BaseType == null)
                {
                    mb.Entity(entityType.ClrType).HasKey("Id");
                }
            }

            // ── ASSESSMENT TPT MAPPING ───────────────────────────────────
            // NOTE: ToTable is also set in AssessmentConfiguration but must
            // be declared here first for TPT strategy to register correctly.
            mb.Entity<Assessment1>().UseTptMappingStrategy();

            // ── APPLY ENTITY CONFIGURATIONS ─────────────────────────────
            // All FK relationships, query filters, and property configs
            // are defined ONLY inside these configuration classes — never
            // duplicated here in OnModelCreating.

            // Identity & School
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

            // Grades
            mb.ApplyConfiguration(new GradeConfiguration(_tenantContext));

            // CBC Curriculum Helpers
            mb.ApplyConfiguration(new LearningAreaConfiguration());
            mb.ApplyConfiguration(new StrandConfiguration());
            mb.ApplyConfiguration(new SubStrandConfiguration());
            mb.ApplyConfiguration(new LearningOutcomeConfiguration());

            // Assessments — root first (TPT base), then each subtype
            mb.ApplyConfiguration(new AssessmentConfiguration(_tenantContext));
            mb.ApplyConfiguration(new FormativeAssessmentConfiguration(_tenantContext));
            mb.ApplyConfiguration(new SummativeAssessmentConfiguration(_tenantContext));
            mb.ApplyConfiguration(new CompetencyAssessmentConfiguration(_tenantContext));

            // Assessment scores
            mb.ApplyConfiguration(new FormativeAssessmentScoreConfiguration(_tenantContext));
            mb.ApplyConfiguration(new SummativeAssessmentScoreConfiguration(_tenantContext));
            mb.ApplyConfiguration(new CompetencyAssessmentScoreConfiguration(_tenantContext));

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
                if (entry.Entity is BaseEntity<Guid> baseEntity)
                {
                    if (entry.State == EntityState.Added)
                    {
                        if (baseEntity.Id == Guid.Empty)
                            baseEntity.Id = Guid.NewGuid();

                        baseEntity.CreatedOn = now;
                        baseEntity.UpdatedOn = now;
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        baseEntity.UpdatedOn = now;
                    }
                }

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