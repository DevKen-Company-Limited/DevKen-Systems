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

            // ❌ Remove tenant filter from here

            // Indexes
            builder.HasIndex(cas => new { cas.TenantId, cas.CompetencyAssessmentId, cas.StudentId })
                   .IsUnique();

            builder.HasIndex(cas => new { cas.TenantId, cas.StudentId });

            // Properties
            builder.Property(cas => cas.Rating)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(cas => cas.AssessmentMethod)
                .HasMaxLength(20);

            // Relationships
            builder.HasOne(cas => cas.CompetencyAssessment)
                .WithMany(ca => ca.Scores)
                .HasForeignKey(cas => cas.CompetencyAssessmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(cas => cas.Student)
                .WithMany(s => s.CompetencyAssessmentScores)
                .HasForeignKey(cas => cas.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(cas => cas.Assessor)
                .WithMany()
                .HasForeignKey(cas => cas.AssessorId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
