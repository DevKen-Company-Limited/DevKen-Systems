using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    /// <summary>
<<<<<<< HEAD
    /// Configures the "CompetencyAssessments" TPT table.
    /// Only columns exclusive to CompetencyAssessment are mapped here.
    /// </summary>
    public class CompetencyAssessmentConfiguration : IEntityTypeConfiguration<CompetencyAssessment>
=======
    /// Configures the "CompetencyAssessments" table (TPT subtype).
    /// Only maps CompetencyAssessment-specific columns.
    /// Shared FK relationships (Teacher, AcademicYear, Term, etc.)
    /// are configured in AssessmentConfiguration — never repeat them here.
    /// </summary>
    public class CompetencyAssessmentConfiguration
        : IEntityTypeConfiguration<CompetencyAssessment>
>>>>>>> upstream/main
    {
        private readonly TenantContext _tenantContext;

        public CompetencyAssessmentConfiguration(TenantContext tenantContext)
<<<<<<< HEAD
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
=======
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
>>>>>>> upstream/main

            builder.Property(c => c.IsObservationBased)
                   .HasDefaultValue(true);

<<<<<<< HEAD
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
=======
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
>>>>>>> upstream/main
        }
    }
}