using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    /// <summary>
    /// Configures the "CompetencyAssessments" table (TPT subtype).
    /// Only maps CompetencyAssessment-specific columns.
    /// Shared FK relationships (Teacher, AcademicYear, Term, etc.)
    /// are configured in AssessmentConfiguration — never repeat them here.
    /// </summary>
    public class CompetencyAssessmentConfiguration
        : IEntityTypeConfiguration<CompetencyAssessment>
    {
        private readonly TenantContext _tenantContext;

        public CompetencyAssessmentConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<CompetencyAssessment> builder)
        {
            builder.ToTable("CompetencyAssessments");

            // ── Subtype-specific Properties ──────────────────────────────

            builder.Property(c => c.CompetencyName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(c => c.Strand)
                   .HasMaxLength(50);

            builder.Property(c => c.SubStrand)
                   .HasMaxLength(50);

            builder.Property(c => c.PerformanceIndicators)
                   .HasMaxLength(1000);

            builder.Property(c => c.RatingScale)
                   .HasMaxLength(100);

            builder.Property(c => c.ToolsRequired)
                   .HasMaxLength(500);

            builder.Property(c => c.Instructions)
                   .HasMaxLength(1000);

            builder.Property(c => c.SpecificLearningOutcome)
                   .HasMaxLength(1000);

            builder.Property(c => c.IsObservationBased)
                   .HasDefaultValue(true);

            builder.Property(c => c.AssessmentMethod)
                   .HasConversion<string>()
                   .HasMaxLength(30);

            // 🚫 DO NOT configure:
            // - Teacher
            // - AcademicYear
            // - Term
            // - Subject
            // - Class
            // - Tenant
            // These are inherited and configured in AssessmentConfiguration.
        }
    }
}