using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    /// <summary>
    /// Configures the "FormativeAssessments" TPT table.
    /// Only columns that exist exclusively on FormativeAssessment are mapped here.
    /// Shared columns (Title, TeacherId, etc.) are already in the "Assessments" table.
    /// </summary>
    public class FormativeAssessmentConfiguration : IEntityTypeConfiguration<FormativeAssessment>
    {
        private readonly TenantContext _tenantContext;

        public FormativeAssessmentConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<FormativeAssessment> builder)
        {
            // ── Subtype-only columns ─────────────────────────────────────

            builder.Property(f => f.FormativeType)
                   .HasMaxLength(50)
                   .IsRequired(false);

            builder.Property(f => f.CompetencyArea)
                   .HasMaxLength(100)
                   .IsRequired(false);

            builder.Property(f => f.Strand)
                   .HasMaxLength(50)
                   .IsRequired(false);

            builder.Property(f => f.SubStrand)
                   .HasMaxLength(50)
                   .IsRequired(false);

            builder.Property(f => f.Criteria)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.Property(f => f.Instructions)
                   .HasMaxLength(1000)
                   .IsRequired(false);

            builder.Property(f => f.FeedbackTemplate)
                   .HasMaxLength(1000)
                   .IsRequired(false);

            builder.Property(f => f.RequiresRubric)
                   .HasDefaultValue(false);

            builder.Property(f => f.AssessmentWeight)
                   .HasColumnType("decimal(5,2)")
                   .HasDefaultValue(100.0m);

            builder.Property(f => f.LearningOutcomeId)
                   .IsRequired(false);

            // ── Navigation: LearningOutcome ──────────────────────────────
            // The actual relationship is registered in AppDbContext because
            // LearningOutcome.FormativeAssessments collection lives on the
            // other side of the relationship.

            // ── Navigation: Scores ───────────────────────────────────────
            builder.HasMany(f => f.Scores)
                   .WithOne(s => s.FormativeAssessment)
                   .HasForeignKey(s => s.FormativeAssessmentId)
                   .OnDelete(DeleteBehavior.Cascade);

            // ── Indexes ──────────────────────────────────────────────────
            builder.HasIndex(f => f.LearningOutcomeId);
            builder.HasIndex(f => f.FormativeType);
        }
    }
}