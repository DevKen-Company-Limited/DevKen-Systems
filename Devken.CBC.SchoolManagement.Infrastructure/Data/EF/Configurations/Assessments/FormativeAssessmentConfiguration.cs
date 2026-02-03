using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    public class FormativeAssessmentConfiguration : IEntityTypeConfiguration<FormativeAssessment>
    {
        public void Configure(EntityTypeBuilder<FormativeAssessment> builder)
        {
            builder.ToTable("FormativeAssessments");

            // ❌ Remove HasKey because it's defined on Assessment1
            // builder.HasKey(fa => fa.Id);

            // ❌ Remove tenant filter from derived type

            // Properties
            builder.Property(fa => fa.FormativeType)
                .HasMaxLength(50);

            builder.Property(fa => fa.CompetencyArea)
                .HasMaxLength(100);

            builder.Property(fa => fa.Strand)
                .HasMaxLength(50);

            builder.Property(fa => fa.SubStrand)
                .HasMaxLength(50);

            // Relationships
            builder.HasMany(fa => fa.Scores)
                .WithOne(fas => fas.FormativeAssessment)
                .HasForeignKey(fas => fas.FormativeAssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
