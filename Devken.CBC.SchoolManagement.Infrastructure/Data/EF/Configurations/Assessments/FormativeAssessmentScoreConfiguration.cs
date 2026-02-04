using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    public class FormativeAssessmentScoreConfiguration
        : IEntityTypeConfiguration<FormativeAssessmentScore>
    {
        public void Configure(EntityTypeBuilder<FormativeAssessmentScore> builder)
        {
            builder.ToTable("FormativeAssessmentScores");

            builder.HasKey(fas => fas.Id);

            // Indexes
            builder.HasIndex(fas => new { fas.FormativeAssessmentId, fas.StudentId })
                   .IsUnique();

            builder.HasIndex(fas => fas.StudentId);

            // Properties
            builder.Property(fas => fas.Score)
                   .HasPrecision(6, 2);

            builder.Property(fas => fas.MaximumScore)
                   .HasPrecision(6, 2);

            builder.Property(fas => fas.Grade)
                   .HasMaxLength(10);

            builder.Property(fas => fas.PerformanceLevel)
                   .HasMaxLength(20);

            builder.Property(fas => fas.Feedback)
                   .HasMaxLength(2000);

            builder.Property(fas => fas.Strengths)
                   .HasMaxLength(500);

            builder.Property(fas => fas.AreasForImprovement)
                   .HasMaxLength(500);

            builder.Property(fas => fas.CompetencyArea)
                   .HasMaxLength(100);

            // Ignore computed properties
            builder.Ignore(fas => fas.Percentage);

            // Relationships
            builder.HasOne(fas => fas.FormativeAssessment)
                   .WithMany(fa => fa.Scores)
                   .HasForeignKey(fas => fas.FormativeAssessmentId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(fas => fas.Student)
                .WithMany(s => s.FormativeAssessmentScores)
                .HasForeignKey(fas => fas.StudentId)
                .IsRequired(false)  // ✅ Make optional to match query filter warning
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(fas => fas.GradedBy)
                   .WithMany()
                   .HasForeignKey(fas => fas.GradedById)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
