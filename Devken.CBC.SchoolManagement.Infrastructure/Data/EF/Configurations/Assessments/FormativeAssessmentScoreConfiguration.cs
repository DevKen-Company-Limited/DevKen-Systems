using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    /// <summary>
    /// Configures the "FormativeAssessmentScores" table.
    /// This is a standalone entity — not part of the assessment TPT hierarchy.
    /// </summary>
    public class FormativeAssessmentScoreConfiguration : IEntityTypeConfiguration<FormativeAssessmentScore>
    {
        private readonly TenantContext _tenantContext;

        public FormativeAssessmentScoreConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<FormativeAssessmentScore> builder)
        {
            builder.ToTable("FormativeAssessmentScores");

            // ── Columns ──────────────────────────────────────────────────

            builder.Property(s => s.FormativeAssessmentId)
                   .IsRequired();

            builder.Property(s => s.StudentId)
                   .IsRequired();

            builder.Property(s => s.Score)
                   .HasColumnType("decimal(8,2)");

            builder.Property(s => s.MaximumScore)
                   .HasColumnType("decimal(8,2)");

            // Computed property — never write to DB
            builder.Ignore(s => s.Percentage);

            builder.Property(s => s.Grade)
                   .HasMaxLength(10)
                   .IsRequired(false);

            builder.Property(s => s.PerformanceLevel)
                   .HasMaxLength(20)
                   .IsRequired(false);   // Excellent | Good | Satisfactory | Needs Improvement

            builder.Property(s => s.Feedback)
                   .HasMaxLength(2000)
                   .IsRequired(false);

            builder.Property(s => s.Strengths)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.Property(s => s.AreasForImprovement)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.Property(s => s.IsSubmitted)
                   .HasDefaultValue(false);

            builder.Property(s => s.SubmissionDate)
                   .IsRequired(false);

            builder.Property(s => s.GradedDate)
                   .IsRequired(false);

            builder.Property(s => s.GradedById)
                   .IsRequired(false);

            builder.Property(s => s.CompetencyArea)
                   .HasMaxLength(100)
                   .IsRequired(false);

            builder.Property(s => s.CompetencyAchieved)
                   .HasDefaultValue(false);

            // ── Relationships ────────────────────────────────────────────
            // Declared in AppDbContext to keep all score relationships visible.

            // ── Indexes ──────────────────────────────────────────────────
            builder.HasIndex(s => s.FormativeAssessmentId);
            builder.HasIndex(s => s.StudentId);
            builder.HasIndex(s => new { s.FormativeAssessmentId, s.StudentId })
                   .IsUnique()
                   .HasDatabaseName("IX_FormativeAssessmentScores_Assessment_Student");

            // ── Global Query Filter ───────────────────────────────────────
            if (_tenantContext?.TenantId != null)
            {
                builder.HasQueryFilter(s => s.TenantId == _tenantContext.TenantId);
            }
        }
    }
}