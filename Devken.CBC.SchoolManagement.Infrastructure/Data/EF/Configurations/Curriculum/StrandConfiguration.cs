using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Persistence.Configurations
{
    public class StrandConfiguration : IEntityTypeConfiguration<Strand>
    {
        public void Configure(EntityTypeBuilder<Strand> builder)
        {
            builder.ToTable("Strand");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Name)
                   .IsRequired()
                   .HasMaxLength(150);

            // ✅ Strand → SubStrands: cascade is fine here (no cycle)
            builder.HasMany(s => s.SubStrands)
                   .WithOne(ss => ss.Strand)
                   .HasForeignKey(ss => ss.StrandId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}