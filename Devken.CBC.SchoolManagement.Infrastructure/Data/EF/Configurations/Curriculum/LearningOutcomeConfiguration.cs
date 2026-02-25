using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Persistence.Configurations
{
    public class LearningOutcomeConfiguration : IEntityTypeConfiguration<LearningOutcome>
    {
        public void Configure(EntityTypeBuilder<LearningOutcome> builder)
        {
            builder.ToTable("LearningOutcome");

            builder.HasKey(lo => lo.Id);

            builder.Property(lo => lo.Outcome)
                   .IsRequired()
                   .HasMaxLength(250);

            builder.Property(lo => lo.Code)
                   .HasMaxLength(50);

            builder.Property(lo => lo.Description)
                   .HasMaxLength(1000);

            // ✅ LearningArea → NO cascade (breaks the cycle)
            builder.HasOne(lo => lo.LearningArea)
                   .WithMany()
                   .HasForeignKey(lo => lo.LearningAreaId)
                   .OnDelete(DeleteBehavior.Restrict);

            // ✅ Strand → NO cascade (breaks the cycle)
            builder.HasOne(lo => lo.Strand)
                   .WithMany()
                   .HasForeignKey(lo => lo.StrandId)
                   .OnDelete(DeleteBehavior.Restrict);

            // ✅ SubStrand → ONLY this one cascades (single path)
            builder.HasOne(lo => lo.SubStrand)
                   .WithMany(ss => ss.LearningOutcomes)
                   .HasForeignKey(lo => lo.SubStrandId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}