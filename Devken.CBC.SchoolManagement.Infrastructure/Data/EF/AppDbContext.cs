// Devken.CBC.SchoolManagement.Infrastructure/Data/EF/AppDbContext.cs
//
// ── PostgreSQL migration notes ────────────────────────────────────────────────
//  1. Remove:   Microsoft.EntityFrameworkCore.SqlServer
//  2. Add:      Npgsql.EntityFrameworkCore.PostgreSQL
//  3. appsettings.json connection string format:
//       "DefaultConnection": "Host=localhost;Port=5432;Database=SchoolMgmt;Username=postgres;Password=yourpassword"
//  4. After adding the package, re-run:
//       dotnet ef migrations add InitPostgres
//       dotnet ef database update
// ─────────────────────────────────────────────────────────────────────────────

using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Devken.CBC.SchoolManagement.Domain.Entities.NumberSeries;
using Devken.CBC.SchoolManagement.Domain.Entities.Payments;
using Devken.CBC.SchoolManagement.Domain.Entities.Reports;
using Devken.CBC.SchoolManagement.Domain.Entities.Subscription;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Academic;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Finance;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Library;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Payments;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Reports;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.SchoolConf;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Subscription;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Conventions;
using Devken.CBC.SchoolManagement.Infrastructure.Persistence.Configurations;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CompetencyAssessmentConfiguration = Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments.CompetencyAssessmentConfiguration;
using CompetencyAssessmentScoreConfiguration = Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments.CompetencyAssessmentScoreConfiguration;
using FormativeAssessmentConfiguration = Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments.FormativeAssessmentConfiguration;
using FormativeAssessmentScoreConfiguration = Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments.FormativeAssessmentScoreConfiguration;
using SummativeAssessmentConfiguration = Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments.SummativeAssessmentConfiguration;
using SummativeAssessmentScoreConfiguration = Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments.SummativeAssessmentScoreConfiguration;

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

        // ── Identity & Admin ──────────────────────────────────────────────────
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

        // ── Academic ──────────────────────────────────────────────────────────
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Teacher> Teachers => Set<Teacher>();
        public DbSet<Class> Classes => Set<Class>();
        public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
        public DbSet<Term> Terms => Set<Term>();
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<Parent> Parents => Set<Parent>();
        public DbSet<Grade> Grades => Set<Grade>();

        // ── CBC Curriculum Helpers ─────────────────────────────────────────────
        public DbSet<LearningArea> LearningAreas => Set<LearningArea>();
        public DbSet<Strand> Strands => Set<Strand>();
        public DbSet<SubStrand> SubStrands => Set<SubStrand>();
        public DbSet<LearningOutcome> LearningOutcomes => Set<LearningOutcome>();

        // ── Assessments (TPT) ─────────────────────────────────────────────────
        public DbSet<Assessment1> Assessments => Set<Assessment1>();
        public DbSet<FormativeAssessment> FormativeAssessments => Set<FormativeAssessment>();
        public DbSet<SummativeAssessment> SummativeAssessments => Set<SummativeAssessment>();
        public DbSet<CompetencyAssessment> CompetencyAssessments => Set<CompetencyAssessment>();

        // ── Assessment Scores ─────────────────────────────────────────────────
        public DbSet<FormativeAssessmentScore> FormativeAssessmentScores => Set<FormativeAssessmentScore>();
        public DbSet<SummativeAssessmentScore> SummativeAssessmentScores => Set<SummativeAssessmentScore>();
        public DbSet<CompetencyAssessmentScore> CompetencyAssessmentScores => Set<CompetencyAssessmentScore>();

        // ── Reports ───────────────────────────────────────────────────────────
        public DbSet<ProgressReport> ProgressReports => Set<ProgressReport>();
        public DbSet<SubjectReport> SubjectReports => Set<SubjectReport>();
        public DbSet<ProgressReportComment> ProgressReportComments => Set<ProgressReportComment>();

        // ── Finance ───────────────────────────────────────────────────────────
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<CreditNote> CreditNotes => Set<CreditNote>();
        public DbSet<FeeItem> FeeItems => Set<FeeItem>();

        // ── PesaPal Transaction Log ───────────────────────────────────────────
        public DbSet<PesaPalTransaction> PesaPalTransactions => Set<PesaPalTransaction>();

        // ── Payments & Misc ───────────────────────────────────────────────────
        public DbSet<SubscriptionPlanEntity> SubscriptionPlans { get; set; }
        public DbSet<MpesaPaymentRecord> MpesaPayments { get; set; }
        public DbSet<TeacherCBCLevel> TeacherCBCLevels { get; set; } = null!;
        public DbSet<DocumentNumberSeries> DocumentNumberSeries => Set<DocumentNumberSeries>();
        public DbSet<SsoSetupToken> SsoSetupTokens => Set<SsoSetupToken>();
        public DbSet<SsoOtpToken> SsoOtpTokens => Set<SsoOtpToken>();

        // ── Library ───────────────────────────────────────────────────────────
        public DbSet<BookAuthor> BookAuthors => Set<BookAuthor>();
        public DbSet<BookCategory> BookCategories => Set<BookCategory>();
        public DbSet<BookPublisher> BookPublishers => Set<BookPublisher>();
        public DbSet<Book> Books => Set<Book>();
        public DbSet<LibraryBranch> LibraryBranches => Set<LibraryBranch>();
        public DbSet<BookCopy> BookCopies => Set<BookCopy>();
        public DbSet<BookInventory> BookInventories => Set<BookInventory>();
        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
        public DbSet<BookReservation> BookReservations => Set<BookReservation>();
        public DbSet<LibraryMember> LibraryMembers => Set<LibraryMember>();

        #endregion

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            // ── GLOBAL CONVENTIONS ────────────────────────────────────────────
            DecimalPrecisionConvention.Apply(mb);

            // ── PostgreSQL: use lowercase table / column names by convention ──
            // Npgsql recommends snake_case. If your existing migrations already
            // use PascalCase table names you can remove these two lines and keep
            // the explicit ToTable() calls you already have.
            foreach (var entity in mb.Model.GetEntityTypes())
            {
                entity.SetTableName(entity.GetTableName());   // keeps explicit names
                // If you want auto snake_case, replace the line above with:
                // entity.SetTableName(ToSnakeCase(entity.GetTableName()!));
            }

            // ── GENERIC BASE ENTITY KEY CONFIGURATION ─────────────────────────
            foreach (var entityType in mb.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity<Guid>).IsAssignableFrom(entityType.ClrType)
                    && entityType.BaseType == null)
                {
                    mb.Entity(entityType.ClrType).HasKey("Id");
                }
            }

            // ── ASSESSMENT TPT MAPPING ─────────────────────────────────────────
            mb.Entity<Assessment1>()
              .UseTptMappingStrategy()
              .ToTable("Assessments");

            mb.Entity<FormativeAssessment>().ToTable("FormativeAssessments");
            mb.Entity<SummativeAssessment>().ToTable("SummativeAssessments");
            mb.Entity<CompetencyAssessment>().ToTable("CompetencyAssessments");

            // ── EXPLICIT RELATIONSHIPS ─────────────────────────────────────────

            mb.Entity<Teacher>()
              .HasOne(t => t.CurrentClass)
              .WithMany()
              .HasForeignKey(t => t.CurrentClassId)
              .OnDelete(DeleteBehavior.Restrict);

            // ── FORMATIVE ASSESSMENT — CBC CURRICULUM LINKS ────────────────────
            mb.Entity<FormativeAssessment>()
              .HasOne(f => f.Strand)
              .WithMany()
              .HasForeignKey(f => f.StrandId)
              .IsRequired(false)
              .OnDelete(DeleteBehavior.NoAction);

            mb.Entity<FormativeAssessment>()
              .HasOne(f => f.SubStrand)
              .WithMany()
              .HasForeignKey(f => f.SubStrandId)
              .IsRequired(false)
              .OnDelete(DeleteBehavior.NoAction);

            mb.Entity<FormativeAssessment>()
              .HasOne(f => f.LearningOutcome)
              .WithMany(lo => lo.FormativeAssessments)
              .HasForeignKey(f => f.LearningOutcomeId)
              .IsRequired(false)
              .OnDelete(DeleteBehavior.NoAction);

            // ── SuperAdminRefreshToken → SuperAdmin ────────────────────────────
            mb.Entity<SuperAdminRefreshToken>()
              .HasOne(t => t.SuperAdmin)
              .WithMany()
              .HasForeignKey(t => t.SuperAdminId)
              .OnDelete(DeleteBehavior.Cascade);

            // ── FORMATIVE ASSESSMENT SCORE RELATIONSHIPS ───────────────────────
            mb.Entity<FormativeAssessmentScore>(entity =>
            {
                entity.HasOne(s => s.FormativeAssessment)
                      .WithMany(a => a.Scores)
                      .HasForeignKey(s => s.FormativeAssessmentId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.Student)
                      .WithMany()
                      .HasForeignKey(s => s.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.GradedBy)
                      .WithMany()
                      .HasForeignKey(s => s.GradedById)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Ignore(s => s.Percentage);
            });

            // ── SUMMATIVE ASSESSMENT SCORE RELATIONSHIPS ───────────────────────
            mb.Entity<SummativeAssessmentScore>(entity =>
            {
                entity.HasOne(s => s.SummativeAssessment)
                      .WithMany(a => a.Scores)
                      .HasForeignKey(s => s.SummativeAssessmentId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.Student)
                      .WithMany()
                      .HasForeignKey(s => s.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.GradedBy)
                      .WithMany()
                      .HasForeignKey(s => s.GradedById)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Ignore(s => s.TotalScore);
                entity.Ignore(s => s.MaximumTotalScore);
                entity.Ignore(s => s.Percentage);
                entity.Ignore(s => s.PerformanceStatus);
            });

            // ── COMPETENCY ASSESSMENT SCORE RELATIONSHIPS ──────────────────────
            mb.Entity<CompetencyAssessmentScore>(entity =>
            {
                entity.HasOne(s => s.CompetencyAssessment)
                      .WithMany(a => a.Scores)
                      .HasForeignKey(s => s.CompetencyAssessmentId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.Student)
                      .WithMany()
                      .HasForeignKey(s => s.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Assessor)
                      .WithMany()
                      .HasForeignKey(s => s.AssessorId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Ignore(s => s.CompetencyLevel);
            });

            // ── PESAPAL TRANSACTION LOG ────────────────────────────────────────
            mb.Entity<PesaPalTransaction>(entity =>
            {
                entity.ToTable("PesaPalTransactions");
                entity.HasKey(e => e.Id);

                // PostgreSQL: use uuid for Guid PKs (ValueGeneratedOnAdd works
                // with gen_random_uuid() which Npgsql sets automatically).
                entity.Property(e => e.Id)
                      .ValueGeneratedOnAdd();

                entity.Property(e => e.OrderTrackingId)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.HasIndex(e => e.OrderTrackingId)
                      .IsUnique()
                      .HasDatabaseName("IX_PesaPalTransactions_OrderTrackingId");

                entity.Property(e => e.MerchantReference)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.HasIndex(e => e.MerchantReference)
                      .HasDatabaseName("IX_PesaPalTransactions_MerchantReference");

                entity.Property(e => e.Status)
                      .IsRequired()
                      // PostgreSQL: store enum as text (same intent as HasConversion<string>)
                      .HasConversion<string>()
                      .HasMaxLength(20);

                entity.Property(e => e.Amount)
                      .HasPrecision(18, 2);

                entity.Property(e => e.Currency)
                      .HasMaxLength(10)
                      .HasDefaultValue("KES");

                entity.Property(e => e.Description)
                      .HasMaxLength(500);

                entity.Property(e => e.PaymentMethod)
                      .HasMaxLength(50);

                entity.Property(e => e.ConfirmationCode)
                      .HasMaxLength(100);

                entity.Property(e => e.PaymentAccount)
                      .HasMaxLength(100);

                entity.Property(e => e.ErrorMessage)
                      .HasMaxLength(1000);

                entity.Property(e => e.TenantId)
                      .IsRequired();

                entity.HasIndex(e => e.TenantId)
                      .HasDatabaseName("IX_PesaPalTransactions_TenantId");

                entity.HasIndex(e => e.Status)
                      .HasDatabaseName("IX_PesaPalTransactions_Status");

                entity.HasIndex(e => e.CreatedOn)
                      .HasDatabaseName("IX_PesaPalTransactions_CreatedOn");
            });

            // ── APPLY ENTITY CONFIGURATIONS ───────────────────────────────────

            // Library — lookups
            mb.ApplyConfiguration(new BookAuthorConfiguration(_tenantContext));
            mb.ApplyConfiguration(new BookCategoryConfiguration(_tenantContext));
            mb.ApplyConfiguration(new BookPublisherConfiguration(_tenantContext));

            // Library — core entities (order matters: Book before BookCopy/Inventory)
            mb.ApplyConfiguration(new BookConfiguration(_tenantContext));
            mb.ApplyConfiguration(new LibraryBranchConfiguration(_tenantContext));
            mb.ApplyConfiguration(new BookCopyConfiguration(_tenantContext));
            mb.ApplyConfiguration(new BookInventoryConfiguration(_tenantContext));
            mb.ApplyConfiguration(new BookReservationConfiguration(_tenantContext));
            mb.ApplyConfiguration(new LibraryMemberConfiguration(_tenantContext));

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
            mb.ApplyConfiguration(new GradeConfiguration(_tenantContext));

            // CBC Curriculum Helpers
            mb.ApplyConfiguration(new LearningAreaConfiguration());
            mb.ApplyConfiguration(new StrandConfiguration());
            mb.ApplyConfiguration(new SubStrandConfiguration());
            mb.ApplyConfiguration(new LearningOutcomeConfiguration());

            // Assessments
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
            mb.ApplyConfiguration(new CreditNoteConfiguration());

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

    // ─────────────────────────────────────────────────────────────────────────
    // DESIGN-TIME FACTORY  (PostgreSQL edition)
    //
    // Replaces UseSqlServer with UseNpgsql.
    // Connection string format (appsettings.json):
    //   "Host=localhost;Port=5432;Database=SchoolMgmt;Username=postgres;Password=secret"
    // ─────────────────────────────────────────────────────────────────────────
    public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // Walk up from Infrastructure project to find the API's appsettings.json
            var basePath = Path.Combine(Directory.GetCurrentDirectory(),
                "..", "Devken.CBC.SchoolManagement.API");

            if (!Directory.Exists(basePath))
                basePath = Directory.GetCurrentDirectory(); // fallback

            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' not found in appsettings.json.");

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            // ── KEY CHANGE: UseNpgsql instead of UseSqlServer ─────────────────
            optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
            {
                // Optional: enable retry on transient failures
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
            });

            // Stub dependencies — only needed at runtime, not during migrations
            var tenantContext = new TenantContext();
            var passwordHasher = new PasswordHasher<User>();

            return new AppDbContext(optionsBuilder.Options, tenantContext, passwordHasher);
        }
    }
}