using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    /// <summary>
    /// Configures the "CompetencyAssessmentScores" table.
    /// This is a standalone entity — not part of the assessment TPT hierarchy.
    /// </summary>
    public class CompetencyAssessmentScoreConfiguration : IEntityTypeConfiguration<CompetencyAssessmentScore>
    {
        private readonly TenantContext _tenantContext;

        public CompetencyAssessmentScoreConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<CompetencyAssessmentScore> builder)
        {
            builder.ToTable("CompetencyAssessmentScores");

            // ── Columns ──────────────────────────────────────────────────

            builder.Property(s => s.CompetencyAssessmentId)
                   .IsRequired();

            builder.Property(s => s.StudentId)
                   .IsRequired();

            builder.Property(s => s.AssessorId)
                   .IsRequired(false);

            builder.Property(s => s.Rating)
                   .IsRequired()
                   .HasMaxLength(50);    // Exceeds | Meets | Approaching | Below

            builder.Property(s => s.ScoreValue)
                   .IsRequired(false);

            builder.Property(s => s.Evidence)
                   .HasMaxLength(1000)
                   .IsRequired(false);

            builder.Property(s => s.AssessmentDate)
                   .IsRequired();

            builder.Property(s => s.AssessmentMethod)
                   .HasMaxLength(20)
                   .IsRequired(false);  // Observation | Oral | Written | Practical

            builder.Property(s => s.ToolsUsed)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.Property(s => s.Feedback)
                   .HasMaxLength(2000)
                   .IsRequired(false);

            builder.Property(s => s.AreasForImprovement)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.Property(s => s.IsFinalized)
                   .HasDefaultValue(false);

            builder.Property(s => s.Strand)
                   .HasMaxLength(100)
                   .IsRequired(false);

            builder.Property(s => s.SubStrand)
                   .HasMaxLength(100)
                   .IsRequired(false);

            builder.Property(s => s.SpecificLearningOutcome)
                   .HasMaxLength(100)
                   .IsRequired(false);

            // Computed property — never persisted to DB
            builder.Ignore(s => s.CompetencyLevel);

            // ── Relationships ────────────────────────────────────────────
            // Declared in AppDbContext alongside the other explicit relationships.

            // ── Indexes ──────────────────────────────────────────────────
            builder.HasIndex(s => s.CompetencyAssessmentId);
            builder.HasIndex(s => s.StudentId);
            builder.HasIndex(s => s.AssessorId);
            builder.HasIndex(s => new { s.CompetencyAssessmentId, s.StudentId })
                   .HasDatabaseName("IX_CompetencyAssessmentScores_Assessment_Student");
            // Note: Not unique here because a student can have multiple competency
            // observations for the same assessment (e.g. different dates/methods).

            // ── Global Query Filter ───────────────────────────────────────
            if (_tenantContext?.TenantId != null)
            {
                builder.HasQueryFilter(s => s.TenantId == _tenantContext.TenantId);
            }
        }
    }
}