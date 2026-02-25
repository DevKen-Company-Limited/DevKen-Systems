using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Finance
{
    public class FeeStructureConfiguration : IEntityTypeConfiguration<FeeStructure>
    {
        public void Configure(EntityTypeBuilder<FeeStructure> builder)
        {
            builder.ToTable("FeeStructures");

            builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.MaxDiscountPercent).HasColumnType("decimal(5,2)");
            builder.Property(x => x.Level).HasConversion<string>().HasMaxLength(30);
            builder.Property(x => x.ApplicableTo).HasConversion<string>().HasMaxLength(20);

            builder.HasOne(x => x.FeeItem)
                   .WithMany(x => x.FeeStructures)
                   .HasForeignKey(x => x.FeeItemId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Composite unique: same fee cannot have two records for same year+term+level+applicableTo
            builder.HasIndex(x => new { x.TenantId, x.FeeItemId, x.AcademicYearId, x.TermId, x.Level, x.ApplicableTo })
                   .IsUnique();
        }
    }
}
