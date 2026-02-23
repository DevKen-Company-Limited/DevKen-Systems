using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    /// <summary>
    /// Configures the "FormativeAssessments" TPT sub-table.
    /// Only columns and relationships that are specific to <see cref="FormativeAssessment"/>
    /// are declared here — shared columns (Title, TeacherId, etc.) and their FK
    /// relationships are owned entirely by <see cref="AssessmentConfiguration"/>.
    ///
    /// CBC curriculum links (Strand → SubStrand → LearningOutcome) are all optional
    /// nullable FKs so teachers can tag an assessment as broadly or specifically as
    /// they need. Deleting a curriculum node uses SetNull so the assessment is simply
    /// "untagged" rather than blocked or cascade-deleted.
    /// </summary>
    public class FormativeAssessmentConfiguration : IEntityTypeConfiguration<FormativeAssessment>
    {
        private readonly TenantContext _tenantContext;

        public FormativeAssessmentConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<FormativeAssessment> builder)
        {
            builder.ToTable("FormativeAssessments");

            // ── Formative-specific columns ────────────────────────────────────
            builder.Property(f => f.FormativeType).HasMaxLength(50);
            builder.Property(f => f.CompetencyArea).HasMaxLength(100);
            builder.Property(f => f.Criteria).HasMaxLength(500);
            builder.Property(f => f.Instructions).HasMaxLength(1000);
            builder.Property(f => f.FeedbackTemplate).HasMaxLength(1000);
            builder.Property(f => f.RequiresRubric).HasDefaultValue(false);
            builder.Property(f => f.AssessmentWeight)
                   .HasColumnType("decimal(5,2)")
                   .HasDefaultValue(100.0m);

            // ── CBC curriculum FK columns (all nullable) ──────────────────────
            builder.Property(f => f.StrandId).IsRequired(false);
            builder.Property(f => f.SubStrandId).IsRequired(false);
            builder.Property(f => f.LearningOutcomeId).IsRequired(false);

            // ── CBC Curriculum Relationships ──────────────────────────────────
            //
            // FIX: Changed from SetNull → NoAction on all three CBC FKs.
            //
            // SQL Server rejects SetNull here because it creates multiple cascade
            // paths to the same table:
            //   Path 1: Strand → (SET NULL) → FormativeAssessment
            //   Path 2: Assessment → (CASCADE) → FormativeAssessment
            // Both paths terminate at FormativeAssessments, which SQL Server
            // error 1785 disallows.
            //
            // With NoAction, deleting a Strand/SubStrand/LearningOutcome will
            // fail at the DB level if any FormativeAssessment still references it.
            // Your application service layer must null-out these FKs before
            // deleting any curriculum node (see ICurriculumService).

            builder.HasOne(f => f.Strand)
                   .WithMany()
                   .HasForeignKey(f => f.StrandId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.NoAction);   // ← was SetNull

            builder.HasOne(f => f.SubStrand)
                   .WithMany()
                   .HasForeignKey(f => f.SubStrandId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.NoAction);   // ← was SetNull

            builder.HasOne(f => f.LearningOutcome)
                   .WithMany(lo => lo.FormativeAssessments)
                   .HasForeignKey(f => f.LearningOutcomeId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.NoAction);   // ← was SetNull

            // ── Scores relationship ───────────────────────────────────────────
            builder.HasMany(f => f.Scores)
                   .WithOne(s => s.FormativeAssessment)
                   .HasForeignKey(s => s.FormativeAssessmentId)
                   .OnDelete(DeleteBehavior.Cascade);

            // ── Indexes ───────────────────────────────────────────────────────
            builder.HasIndex(f => f.StrandId);
            builder.HasIndex(f => f.SubStrandId);
            builder.HasIndex(f => f.LearningOutcomeId);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Configures the "SummativeAssessments" TPT sub-table.
    /// Shared columns are owned by <see cref="AssessmentConfiguration"/>.
    /// </summary>
    public class SummativeAssessmentConfiguration : IEntityTypeConfiguration<SummativeAssessment>
    {
        private readonly TenantContext _tenantContext;

        public SummativeAssessmentConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<SummativeAssessment> builder)
        {
            builder.ToTable("SummativeAssessments");

            // ── Summative-specific columns ────────────────────────────────────
            builder.Property(s => s.ExamType).HasMaxLength(50);
            builder.Property(s => s.NumberOfQuestions).HasDefaultValue(0);
            builder.Property(s => s.PassMark)
                   .HasColumnType("decimal(5,2)")
                   .HasDefaultValue(50.0m);
            builder.Property(s => s.HasPracticalComponent).HasDefaultValue(false);
            builder.Property(s => s.PracticalWeight)
                   .HasColumnType("decimal(5,2)")
                   .HasDefaultValue(0.0m);
            builder.Property(s => s.TheoryWeight)
                   .HasColumnType("decimal(5,2)")
                   .HasDefaultValue(100.0m);
            builder.Property(s => s.Instructions).HasMaxLength(1000);

            // ── Scores relationship ───────────────────────────────────────────
            builder.HasMany(s => s.Scores)
                   .WithOne(sc => sc.SummativeAssessment)
                   .HasForeignKey(sc => sc.SummativeAssessmentId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Configures the "CompetencyAssessments" TPT sub-table.
    /// Shared columns are owned by <see cref="AssessmentConfiguration"/>.
    /// CompetencyStrand and CompetencySubStrand are stored as free-text strings
    /// (not FK-linked) because competency descriptions are often ad-hoc and may
    /// not align to the formal Strand/SubStrand hierarchy.
    /// </summary>
    public class CompetencyAssessmentConfiguration : IEntityTypeConfiguration<CompetencyAssessment>
    {
        private readonly TenantContext _tenantContext;

        public CompetencyAssessmentConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<CompetencyAssessment> builder)
        {
            builder.ToTable("CompetencyAssessments");

            // ── Competency-specific columns ───────────────────────────────────
            builder.Property(c => c.CompetencyName).IsRequired().HasMaxLength(100);
            builder.Property(c => c.CompetencyStrand).HasMaxLength(100);
            builder.Property(c => c.CompetencySubStrand).HasMaxLength(100);
            builder.Property(c => c.PerformanceIndicators).HasMaxLength(1000);
            builder.Property(c => c.RatingScale).HasMaxLength(20);
            builder.Property(c => c.ToolsRequired).HasMaxLength(500);
            builder.Property(c => c.Instructions).HasMaxLength(1000);
            builder.Property(c => c.SpecificLearningOutcome).HasMaxLength(1000);
            builder.Property(c => c.IsObservationBased).HasDefaultValue(true);

            // ── Enum columns ──────────────────────────────────────────────────
            builder.Property(c => c.TargetLevel)
                   .HasConversion<string>()
                   .HasMaxLength(20);

            builder.Property(c => c.AssessmentMethod)
                   .HasConversion<string>()
                   .HasMaxLength(30);

            // ── Scores relationship ───────────────────────────────────────────
            builder.HasMany(c => c.Scores)
                   .WithOne(s => s.CompetencyAssessment)
                   .HasForeignKey(s => s.CompetencyAssessmentId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Configures the "FormativeAssessmentScores" table.
    /// The assessment FK and student/gradedBy FKs are declared here.
    /// Computed properties (Percentage) are ignored so EF never tries to persist them.
    /// </summary>
    public class FormativeAssessmentScoreConfiguration
        : IEntityTypeConfiguration<FormativeAssessmentScore>
    {
        private readonly TenantContext _tenantContext;

        public FormativeAssessmentScoreConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<FormativeAssessmentScore> builder)
        {
            builder.ToTable("FormativeAssessmentScores");
            builder.HasKey(s => s.Id);

            // ── Score columns ─────────────────────────────────────────────────
            builder.Property(s => s.Score).HasColumnType("decimal(8,2)");
            builder.Property(s => s.MaximumScore).HasColumnType("decimal(8,2)");
            builder.Property(s => s.Grade).HasMaxLength(10);
            builder.Property(s => s.PerformanceLevel).HasMaxLength(20);
            builder.Property(s => s.Feedback).HasMaxLength(2000);
            builder.Property(s => s.Strengths).HasMaxLength(500);
            builder.Property(s => s.AreasForImprovement).HasMaxLength(500);
            builder.Property(s => s.CompetencyArea).HasMaxLength(100);
            builder.Property(s => s.IsSubmitted).HasDefaultValue(false);
            builder.Property(s => s.CompetencyAchieved).HasDefaultValue(false);

            // ── Computed — never persisted ────────────────────────────────────
            builder.Ignore(s => s.Percentage);

            // ── Relationships ─────────────────────────────────────────────────
            // Parent assessment: cascade (score dies with the assessment)
            builder.HasOne(s => s.FormativeAssessment)
                   .WithMany(a => a.Scores)
                   .HasForeignKey(s => s.FormativeAssessmentId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Student: restrict (do not delete scores when a student is removed)
            builder.HasOne(s => s.Student)
                   .WithMany()
                   .HasForeignKey(s => s.StudentId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Teacher who graded (optional)
            builder.HasOne(s => s.GradedBy)
                   .WithMany()
                   .HasForeignKey(s => s.GradedById)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.Restrict);

            // ── Indexes ───────────────────────────────────────────────────────
            // Unique per student per assessment (upsert key)
            builder.HasIndex(s => new { s.FormativeAssessmentId, s.StudentId }).IsUnique();
            builder.HasIndex(s => s.StudentId);

            // ── Tenant filter ─────────────────────────────────────────────────
            builder.HasQueryFilter(s =>
                _tenantContext.TenantId == null ||
                s.TenantId == _tenantContext.TenantId);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Configures the "SummativeAssessmentScores" table.
    /// Computed properties (TotalScore, MaximumTotalScore, Percentage,
    /// PerformanceStatus) are ignored so EF never tries to persist them.
    /// </summary>
    public class SummativeAssessmentScoreConfiguration
        : IEntityTypeConfiguration<SummativeAssessmentScore>
    {
        private readonly TenantContext _tenantContext;

        public SummativeAssessmentScoreConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<SummativeAssessmentScore> builder)
        {
            builder.ToTable("SummativeAssessmentScores");
            builder.HasKey(s => s.Id);

            // ── Score columns ─────────────────────────────────────────────────
            builder.Property(s => s.TheoryScore).HasColumnType("decimal(8,2)");
            builder.Property(s => s.PracticalScore).HasColumnType("decimal(8,2)");
            builder.Property(s => s.MaximumTheoryScore).HasColumnType("decimal(8,2)");
            builder.Property(s => s.MaximumPracticalScore).HasColumnType("decimal(8,2)");
            builder.Property(s => s.Grade).HasMaxLength(10);
            builder.Property(s => s.Remarks).HasMaxLength(20);
            builder.Property(s => s.Comments).HasMaxLength(1000);
            builder.Property(s => s.IsPassed).HasDefaultValue(false);

            // ── Computed — never persisted ────────────────────────────────────
            builder.Ignore(s => s.TotalScore);
            builder.Ignore(s => s.MaximumTotalScore);
            builder.Ignore(s => s.Percentage);
            builder.Ignore(s => s.PerformanceStatus);

            // ── Relationships ─────────────────────────────────────────────────
            builder.HasOne(s => s.SummativeAssessment)
                   .WithMany(a => a.Scores)
                   .HasForeignKey(s => s.SummativeAssessmentId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.Student)
                   .WithMany()
                   .HasForeignKey(s => s.StudentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.GradedBy)
                   .WithMany()
                   .HasForeignKey(s => s.GradedById)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.Restrict);

            // ── Indexes ───────────────────────────────────────────────────────
            builder.HasIndex(s => new { s.SummativeAssessmentId, s.StudentId }).IsUnique();
            builder.HasIndex(s => s.StudentId);

            // ── Tenant filter ─────────────────────────────────────────────────
            builder.HasQueryFilter(s =>
                _tenantContext.TenantId == null ||
                s.TenantId == _tenantContext.TenantId);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Configures the "CompetencyAssessmentScores" table.
    /// CompetencyLevel is a computed property and is ignored by EF.
    /// </summary>
    public class CompetencyAssessmentScoreConfiguration
        : IEntityTypeConfiguration<CompetencyAssessmentScore>
    {
        private readonly TenantContext _tenantContext;

        public CompetencyAssessmentScoreConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<CompetencyAssessmentScore> builder)
        {
            builder.ToTable("CompetencyAssessmentScores");
            builder.HasKey(s => s.Id);

            // ── Score columns ─────────────────────────────────────────────────
            builder.Property(s => s.Rating).IsRequired().HasMaxLength(50);
            builder.Property(s => s.Evidence).HasMaxLength(1000);
            builder.Property(s => s.AssessmentMethod).HasMaxLength(20);
            builder.Property(s => s.ToolsUsed).HasMaxLength(500);
            builder.Property(s => s.Feedback).HasMaxLength(2000);
            builder.Property(s => s.AreasForImprovement).HasMaxLength(500);
            builder.Property(s => s.Strand).HasMaxLength(100);
            builder.Property(s => s.SubStrand).HasMaxLength(100);
            builder.Property(s => s.SpecificLearningOutcome).HasMaxLength(500);
            builder.Property(s => s.IsFinalized).HasDefaultValue(false);

            // ── Computed — never persisted ────────────────────────────────────
            builder.Ignore(s => s.CompetencyLevel);

            // ── Relationships ─────────────────────────────────────────────────
            builder.HasOne(s => s.CompetencyAssessment)
                   .WithMany(a => a.Scores)
                   .HasForeignKey(s => s.CompetencyAssessmentId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.Student)
                   .WithMany()
                   .HasForeignKey(s => s.StudentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.Assessor)
                   .WithMany()
                   .HasForeignKey(s => s.AssessorId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.Restrict);

            // ── Indexes ───────────────────────────────────────────────────────
            builder.HasIndex(s => new { s.CompetencyAssessmentId, s.StudentId }).IsUnique();
            builder.HasIndex(s => s.StudentId);

            // ── Tenant filter ─────────────────────────────────────────────────
            builder.HasQueryFilter(s =>
                _tenantContext.TenantId == null ||
                s.TenantId == _tenantContext.TenantId);
        }
    }
}