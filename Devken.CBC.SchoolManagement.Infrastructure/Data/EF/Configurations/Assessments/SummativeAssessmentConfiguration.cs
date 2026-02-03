using Devken.CBC.SchoolManagement.Domain.Entities.Assessment;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    public class SummativeAssessmentConfiguration : IEntityTypeConfiguration<SummativeAssessment>
    {
        public void Configure(EntityTypeBuilder<SummativeAssessment> builder)
        {
            builder.ToTable("SummativeAssessments");

            // Configure only derived-specific properties
            builder.Property(sa => sa.ExamType)
                .HasMaxLength(50);

            builder.Property(sa => sa.PassMark)
                .HasDefaultValue(50.0m);

            builder.Property(sa => sa.NumberOfQuestions)
                .HasDefaultValue(0);

            builder.Property(sa => sa.HasPracticalComponent)
                .HasDefaultValue(false);

            builder.Property(sa => sa.PracticalWeight)
                .HasPrecision(5, 2)
                .HasDefaultValue(0.0m);

            builder.Property(sa => sa.TheoryWeight)
                .HasPrecision(5, 2)
                .HasDefaultValue(100.0m);

            builder.Property(sa => sa.Duration)
                .HasColumnType("time");

            builder.Property(sa => sa.Instructions)
                .HasMaxLength(1000);

            // Relationships
            builder.HasMany(sa => sa.Scores)
                .WithOne(sas => sas.SummativeAssessment)
                .HasForeignKey(sas => sas.SummativeAssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
