using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    /// <summary>
<<<<<<< HEAD
    /// Configures the "SummativeAssessments" TPT table.
    /// Only columns exclusive to SummativeAssessment are mapped here.
=======
    /// Configures the "SummativeAssessments" table (TPT subtype).
    /// Only maps SummativeAssessment-specific columns.
>>>>>>> upstream/main
    /// </summary>
    public class SummativeAssessmentConfiguration : IEntityTypeConfiguration<SummativeAssessment>
    {
        private readonly TenantContext _tenantContext;

        public SummativeAssessmentConfiguration(TenantContext tenantContext)
<<<<<<< HEAD
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<SummativeAssessment> builder)
        {
            // ── Subtype-only columns ─────────────────────────────────────

            builder.Property(s => s.ExamType)
                   .HasMaxLength(50)
                   .IsRequired(false);     // EndTerm | MidTerm | Final

            builder.Property(s => s.Duration)
                   .IsRequired(false);     // stored as TIME(7) by SQL Server

            builder.Property(s => s.NumberOfQuestions)
                   .HasDefaultValue(0);

            builder.Property(s => s.PassMark)
                   .HasColumnType("decimal(5,2)")
                   .HasDefaultValue(50.0m);

            builder.Property(s => s.HasPracticalComponent)
                   .HasDefaultValue(false);

            builder.Property(s => s.PracticalWeight)
                   .HasColumnType("decimal(5,2)")
                   .HasDefaultValue(0.0m);

            builder.Property(s => s.TheoryWeight)
                   .HasColumnType("decimal(5,2)")
                   .HasDefaultValue(100.0m);

            builder.Property(s => s.Instructions)
                   .HasMaxLength(1000)
                   .IsRequired(false);

            // ── Navigation: Scores ───────────────────────────────────────
            builder.HasMany(s => s.Scores)
                   .WithOne(sc => sc.SummativeAssessment)
                   .HasForeignKey(sc => sc.SummativeAssessmentId)
                   .OnDelete(DeleteBehavior.Cascade);

            // ── Indexes ──────────────────────────────────────────────────
            builder.HasIndex(s => s.ExamType);
=======
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<SummativeAssessment> builder)
        {
            builder.ToTable("SummativeAssessments");

            // ── Subtype-specific Properties ──────────────────────────────
            builder.Property(s => s.ExamType)
                   .HasMaxLength(50);

            builder.Property(s => s.Duration)
                   .HasColumnType("time");

            builder.Property(s => s.PassMark)
                   .HasColumnType("decimal(5,2)")
                   .HasDefaultValue(50.0m);

            builder.Property(s => s.PracticalWeight)
                   .HasColumnType("decimal(5,2)")
                   .HasDefaultValue(0.0m);

            builder.Property(s => s.TheoryWeight)
                   .HasColumnType("decimal(5,2)")
                   .HasDefaultValue(100.0m);

            builder.Property(s => s.Instructions)
                   .HasMaxLength(1000);

            builder.Property(s => s.HasPracticalComponent)
                   .HasDefaultValue(false);

            // ── Scores collection configured in SummativeAssessmentScoreConfiguration ──
>>>>>>> upstream/main
        }
    }
}