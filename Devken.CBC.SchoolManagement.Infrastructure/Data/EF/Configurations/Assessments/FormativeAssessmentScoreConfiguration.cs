using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    public class FormativeAssessmentScoreConfiguration : IEntityTypeConfiguration<FormativeAssessmentScore>
    {
        public void Configure(EntityTypeBuilder<FormativeAssessmentScore> builder)
        {
            builder.ToTable("FormativeAssessmentScores");

            builder.HasKey(fas => fas.Id);

            // ❌ Removed tenant filter, already applied at Assessment1 level

            // Indexes
            builder.HasIndex(fas => new { fas.FormativeAssessmentId, fas.StudentId })
                .IsUnique();

            builder.HasIndex(fas => new { fas.StudentId });

            // Properties
            builder.Property(fas => fas.PerformanceLevel)
                .HasMaxLength(20);

            // Computed properties (optional)
            // builder.Property(fas => fas.Percentage)
            //     .HasComputedColumnSql("CASE WHEN [MaximumScore] > 0 THEN ([Score] / [MaximumScore]) * 100 ELSE 0 END", stored: true);

            // Relationships
            builder.HasOne(fas => fas.FormativeAssessment)
                .WithMany(fa => fa.Scores)
                .HasForeignKey(fas => fas.FormativeAssessmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(fas => fas.Student)
                .WithMany(s => s.FormativeAssessmentScores)
                .HasForeignKey(fas => fas.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(fas => fas.GradedBy)
                .WithMany()
                .HasForeignKey(fas => fas.GradedById)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
