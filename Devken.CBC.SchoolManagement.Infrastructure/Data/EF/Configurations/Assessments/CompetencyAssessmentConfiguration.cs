using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    public class CompetencyAssessmentConfiguration : IEntityTypeConfiguration<CompetencyAssessment>
    {
        public void Configure(EntityTypeBuilder<CompetencyAssessment> builder)
        {
            // DO NOT call ToTable() for derived types in TPH
            // DO NOT call HasKey() for derived types in TPH

            // Derived-specific properties only
            builder.Property(ca => ca.Strand)
                .HasMaxLength(100);

            builder.Property(ca => ca.SubStrand)
                .HasMaxLength(100);

            builder.Property(ca => ca.SpecificLearningOutcome)
                .HasMaxLength(100);

            builder.Property(ca => ca.Instructions)
                .HasMaxLength(1000);

            // Relationships
            builder.HasMany(ca => ca.Scores)
                .WithOne(cas => cas.CompetencyAssessment)
                .HasForeignKey(cas => cas.CompetencyAssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}