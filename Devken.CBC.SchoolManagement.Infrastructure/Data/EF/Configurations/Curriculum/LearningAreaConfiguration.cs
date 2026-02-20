using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Persistence.Configurations
{
    public class LearningAreaConfiguration : IEntityTypeConfiguration<LearningArea>
    {
        public void Configure(EntityTypeBuilder<LearningArea> builder)
        {
            builder.ToTable("LearningArea");

            builder.HasKey(la => la.Id);

            builder.Property(la => la.Name)
                   .IsRequired()
                   .HasMaxLength(150);

            builder.Property(la => la.Code)
                   .HasMaxLength(20);

            // ✅ LearningArea → Strands: cascade is fine here (no cycle)
            builder.HasMany(la => la.Strands)
                   .WithOne(s => s.LearningArea)
                   .HasForeignKey(s => s.LearningAreaId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}