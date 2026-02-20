using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    /// <summary>
    /// Configures the "FormativeAssessments" table (TPT subtype).
    /// Only maps FormativeAssessment-specific columns.
    /// The LearningOutcome FK is declared here (not in AppDbContext).
    /// Shared FK relationships (Teacher, AcademicYear, Term) live in
    /// AssessmentConfiguration — never repeat them here.
    /// </summary>
    public class FormativeAssessmentConfiguration : IEntityTypeConfiguration<FormativeAssessment>
    {
        private readonly TenantContext _tenantContext;

        public FormativeAssessmentConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<FormativeAssessment> builder)
        {
            builder.ToTable("FormativeAssessments");

            // ── Subtype-specific Properties ──────────────────────────────
            builder.Property(f => f.FormativeType)
                   .HasMaxLength(50);

            builder.Property(f => f.CompetencyArea)
                   .HasMaxLength(100);

            builder.Property(f => f.Strand)
                   .HasMaxLength(50);

            builder.Property(f => f.SubStrand)
                   .HasMaxLength(50);

            builder.Property(f => f.Criteria)
                   .HasMaxLength(500);

            builder.Property(f => f.Instructions)
                   .HasMaxLength(1000);

            builder.Property(f => f.FeedbackTemplate)
                   .HasMaxLength(1000);

            builder.Property(f => f.AssessmentWeight)
                   .HasColumnType("decimal(5,2)")
                   .HasDefaultValue(100.0m);

            builder.Property(f => f.RequiresRubric)
                   .HasDefaultValue(false);

            // ── Relationships (subtype-specific only) ────────────────────
            // LearningOutcome is optional (nullable FK)
            builder.HasOne(f => f.LearningOutcome)
                   .WithMany(lo => lo.FormativeAssessments)
                   .HasForeignKey(f => f.LearningOutcomeId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.Restrict);

            // ── Scores collection configured in FormativeAssessmentScoreConfiguration ──
        }
    }
}