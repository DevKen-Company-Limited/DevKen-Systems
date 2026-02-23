using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    /// <summary>
    /// Configures the "CompetencyAssessmentScores" table.
<<<<<<< HEAD
    /// This is a standalone entity — not part of the assessment TPT hierarchy.
=======
    /// ✅ Fixes warning [10622]: matching HasQueryFilter added.
>>>>>>> upstream/main
    /// </summary>
    public class CompetencyAssessmentScoreConfiguration : IEntityTypeConfiguration<CompetencyAssessmentScore>
    {
        private readonly TenantContext _tenantContext;

        public CompetencyAssessmentScoreConfiguration(TenantContext tenantContext)
<<<<<<< HEAD
        {
            _tenantContext = tenantContext;
        }
=======
            => _tenantContext = tenantContext;
>>>>>>> upstream/main

        public void Configure(EntityTypeBuilder<CompetencyAssessmentScore> builder)
        {
            builder.ToTable("CompetencyAssessmentScores");

<<<<<<< HEAD
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
=======
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
>>>>>>> upstream/main

            builder.Property(s => s.IsFinalized)
                   .HasDefaultValue(false);

<<<<<<< HEAD
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
=======
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
>>>>>>> upstream/main
        }
    }
}