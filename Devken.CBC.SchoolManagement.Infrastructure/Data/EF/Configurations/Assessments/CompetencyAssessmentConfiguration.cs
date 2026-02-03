using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    public class CompetencyAssessmentConfiguration : IEntityTypeConfiguration<CompetencyAssessment>
    {
        public void Configure(EntityTypeBuilder<CompetencyAssessment> builder)
        {
            builder.ToTable("CompetencyAssessments");

            // ❌ Do NOT configure HasKey on derived type
            // builder.HasKey(ca => ca.Id);

            // ❌ Remove tenant filter from here

            // Properties
            builder.Property(ca => ca.CompetencyName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(ca => ca.Strand)
                .HasMaxLength(50);

            builder.Property(ca => ca.SubStrand)
                .HasMaxLength(50);

            builder.Property(ca => ca.RatingScale)
                .HasMaxLength(20);

            // Relationships
            builder.HasMany(ca => ca.Scores)
                .WithOne(cas => cas.CompetencyAssessment)
                .HasForeignKey(cas => cas.CompetencyAssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
