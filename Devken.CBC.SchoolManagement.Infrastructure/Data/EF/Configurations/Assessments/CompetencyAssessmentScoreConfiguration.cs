using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    /// <summary>
    /// Configures the "CompetencyAssessmentScores" table.
    /// ✅ Fixes warning [10622]: matching HasQueryFilter added.
    /// </summary>
    public class CompetencyAssessmentScoreConfiguration : IEntityTypeConfiguration<CompetencyAssessmentScore>
    {
        private readonly TenantContext _tenantContext;

        public CompetencyAssessmentScoreConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<CompetencyAssessmentScore> builder)
        {
            builder.ToTable("CompetencyAssessmentScores");

            builder.HasKey(s => s.Id);

            // ── Properties ──────────────────────────────────────────────
            builder.Property(s => s.Rating)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(s => s.Evidence)
                   .HasMaxLength(1000);

            builder.Property(s => s.AssessmentMethod)
                   .HasMaxLength(20);

            builder.Property(s => s.ToolsUsed)
                   .HasMaxLength(500);

            builder.Property(s => s.Feedback)
                   .HasMaxLength(2000);

            builder.Property(s => s.AreasForImprovement)
                   .HasMaxLength(500);

            builder.Property(s => s.Strand)
                   .HasMaxLength(100);

            builder.Property(s => s.SubStrand)
                   .HasMaxLength(100);

            builder.Property(s => s.SpecificLearningOutcome)
                   .HasMaxLength(100);

            builder.Property(s => s.IsFinalized)
                   .HasDefaultValue(false);

            // ── Computed properties — never persisted ────────────────────
            builder.Ignore(s => s.CompetencyLevel);

            // ── Relationships ────────────────────────────────────────────
            builder.HasOne(s => s.CompetencyAssessment)
                   .WithMany(a => a.Scores)
                   .HasForeignKey(s => s.CompetencyAssessmentId)
                   .OnDelete(DeleteBehavior.Cascade);

            //builder.HasOne(s => s.Student)
            //       .WithMany()
            //       .HasForeignKey(s => s.StudentId)
            //       .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(s => s.Student)
           .WithMany(st => st.CompetencyAssessmentScores)
           .HasForeignKey(s => s.StudentId)
           .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(s => s.Assessor)
                   .WithMany()
                   .HasForeignKey(s => s.AssessorId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.Restrict);

            // ── Indexes ──────────────────────────────────────────────────
            builder.HasIndex(s => new { s.TenantId, s.CompetencyAssessmentId });
            builder.HasIndex(s => new { s.TenantId, s.StudentId });

            // ✅ Fix warning [10622]
            builder.HasQueryFilter(s => s.TenantId == _tenantContext.TenantId);
        }
    }
}