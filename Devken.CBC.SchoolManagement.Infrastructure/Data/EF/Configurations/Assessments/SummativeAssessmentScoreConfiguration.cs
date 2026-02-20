using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    /// <summary>
    /// Configures the "SummativeAssessmentScores" table.
    /// This is a standalone entity — not part of the assessment TPT hierarchy.
    /// </summary>
    public class SummativeAssessmentScoreConfiguration : IEntityTypeConfiguration<SummativeAssessmentScore>
    {
        private readonly TenantContext _tenantContext;

        public SummativeAssessmentScoreConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<SummativeAssessmentScore> builder)
        {
            builder.ToTable("SummativeAssessmentScores");

            // ── Columns ──────────────────────────────────────────────────

            builder.Property(s => s.SummativeAssessmentId)
                   .IsRequired();

            builder.Property(s => s.StudentId)
                   .IsRequired();

            builder.Property(s => s.TheoryScore)
                   .HasColumnType("decimal(8,2)");

            builder.Property(s => s.PracticalScore)
                   .HasColumnType("decimal(8,2)")
                   .IsRequired(false);

            builder.Property(s => s.MaximumTheoryScore)
                   .HasColumnType("decimal(8,2)");

            builder.Property(s => s.MaximumPracticalScore)
                   .HasColumnType("decimal(8,2)")
                   .IsRequired(false);

            // Computed properties — never persisted to DB
            builder.Ignore(s => s.TotalScore);
            builder.Ignore(s => s.MaximumTotalScore);
            builder.Ignore(s => s.Percentage);
            builder.Ignore(s => s.PerformanceStatus);

            builder.Property(s => s.Grade)
                   .HasMaxLength(10)
                   .IsRequired(false);

            builder.Property(s => s.Remarks)
                   .HasMaxLength(20)
                   .IsRequired(false);   // Distinction | Credit | Pass | Fail

            builder.Property(s => s.PositionInClass)
                   .IsRequired(false);

            builder.Property(s => s.PositionInStream)
                   .IsRequired(false);

            builder.Property(s => s.IsPassed)
                   .HasDefaultValue(false);

            builder.Property(s => s.GradedDate)
                   .IsRequired(false);

            builder.Property(s => s.GradedById)
                   .IsRequired(false);

            builder.Property(s => s.Comments)
                   .HasMaxLength(1000)
                   .IsRequired(false);

            // ── Relationships ────────────────────────────────────────────
            // Declared in AppDbContext alongside the other explicit relationships.

            // ── Indexes ──────────────────────────────────────────────────
            builder.HasIndex(s => s.SummativeAssessmentId);
            builder.HasIndex(s => s.StudentId);
            builder.HasIndex(s => new { s.SummativeAssessmentId, s.StudentId })
                   .IsUnique()
                   .HasDatabaseName("IX_SummativeAssessmentScores_Assessment_Student");

            // ── Global Query Filter ───────────────────────────────────────
            if (_tenantContext?.TenantId != null)
            {
                builder.HasQueryFilter(s => s.TenantId == _tenantContext.TenantId);
            }
        }
    }
}