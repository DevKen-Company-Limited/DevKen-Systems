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
    public class StudentDiscountConfiguration : IEntityTypeConfiguration<StudentDiscount>
    {
        public void Configure(EntityTypeBuilder<StudentDiscount> builder)
        {
            builder.ToTable("StudentDiscounts");

            builder.Property(x => x.Value).HasColumnType("decimal(10,2)");
            builder.Property(x => x.DiscountType).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.Reason).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.ReasonDescription).HasMaxLength(200);
            builder.Property(x => x.ApprovalNotes).HasMaxLength(500);

            builder.HasOne(x => x.Student)
                   .WithMany()
                   .HasForeignKey(x => x.StudentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.FeeItem)
                   .WithMany(x => x.StudentDiscounts)
                   .HasForeignKey(x => x.FeeItemId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

}
