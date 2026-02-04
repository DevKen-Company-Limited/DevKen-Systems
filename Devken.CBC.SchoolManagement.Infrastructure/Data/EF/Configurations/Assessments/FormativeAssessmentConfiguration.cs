using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    public class FormativeAssessmentConfiguration : IEntityTypeConfiguration<FormativeAssessment>
    {
        public void Configure(EntityTypeBuilder<FormativeAssessment> builder)
        {
            // DO NOT call ToTable() for derived types in TPH
            // DO NOT call HasKey() for derived types in TPH

            // Derived-specific properties only

            builder.Property(fa => fa.AssessmentWeight)
                .HasPrecision(5, 2)
                .HasDefaultValue(100.0m);

            builder.Property(fa => fa.Criteria)
                .HasMaxLength(500);

            builder.Property(fa => fa.Instructions)
                .HasMaxLength(1000);

            // Relationships
            builder.HasMany(fa => fa.Scores)
                .WithOne(fas => fas.FormativeAssessment)
                .HasForeignKey(fas => fas.FormativeAssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}