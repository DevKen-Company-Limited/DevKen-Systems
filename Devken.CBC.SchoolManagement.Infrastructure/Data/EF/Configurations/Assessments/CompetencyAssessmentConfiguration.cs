using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    /// <summary>
    /// Configures the "CompetencyAssessments" TPT table.
    /// Only columns exclusive to CompetencyAssessment are mapped here.
    /// </summary>
    public class CompetencyAssessmentConfiguration : IEntityTypeConfiguration<CompetencyAssessment>
    {
        private readonly TenantContext _tenantContext;

        public CompetencyAssessmentConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<CompetencyAssessment> builder)
        {
            // ── Subtype-only columns ─────────────────────────────────────

            builder.Property(c => c.CompetencyName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(c => c.Strand)
                   .HasMaxLength(50)
                   .IsRequired(false);

            builder.Property(c => c.SubStrand)
                   .HasMaxLength(50)
                   .IsRequired(false);

            builder.Property(c => c.TargetLevel)
                   .HasConversion<int>();   // store enum as int

            builder.Property(c => c.PerformanceIndicators)
                   .HasMaxLength(1000)
                   .IsRequired(false);

            builder.Property(c => c.AssessmentMethod)
                   .HasConversion<int>();   // store enum as int

            builder.Property(c => c.RatingScale)
                   .HasMaxLength(20)
                   .IsRequired(false);      // Exceeds | Meets | Approaching | Below

            builder.Property(c => c.IsObservationBased)
                   .HasDefaultValue(true);

            builder.Property(c => c.ToolsRequired)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.Property(c => c.Instructions)
                   .HasMaxLength(1000)
                   .IsRequired(false);

            builder.Property(c => c.SpecificLearningOutcome)
                   .HasMaxLength(1000)
                   .IsRequired(false);

            // ── Navigation: Scores ───────────────────────────────────────
            builder.HasMany(c => c.Scores)
                   .WithOne(s => s.CompetencyAssessment)
                   .HasForeignKey(s => s.CompetencyAssessmentId)
                   .OnDelete(DeleteBehavior.Cascade);

            // ── Indexes ──────────────────────────────────────────────────
            builder.HasIndex(c => c.CompetencyName);
            builder.HasIndex(c => c.Strand);
            builder.HasIndex(c => c.AssessmentMethod);
        }
    }
}