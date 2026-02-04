using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    public class CompetencyAssessmentScoreConfiguration : IEntityTypeConfiguration<CompetencyAssessmentScore>
    {
        public void Configure(EntityTypeBuilder<CompetencyAssessmentScore> builder)
        {
            builder.ToTable("CompetencyAssessmentScores");

            builder.HasKey(cas => cas.Id);

            // Indexes
            builder.HasIndex(cas => new { cas.StudentId, cas.CompetencyAssessmentId });

            // Properties
            builder.Property(cas => cas.Rating)
                .HasMaxLength(20);

          
            builder.Property(cas => cas.Feedback)
                .HasMaxLength(500);

            builder.HasOne(cas => cas.Student)
                .WithMany(s => s.CompetencyAssessmentScores)
                .HasForeignKey(cas => cas.StudentId)
                .IsRequired(false)  // ✅ Make optional to match query filter warning
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(cas => cas.CompetencyAssessment)
                .WithMany(ca => ca.Scores)
                .HasForeignKey(cas => cas.CompetencyAssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}