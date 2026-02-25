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
    public class PaymentPlanInstallmentConfiguration : IEntityTypeConfiguration<PaymentPlanInstallment>
    {
        public void Configure(EntityTypeBuilder<PaymentPlanInstallment> builder)
        {
            builder.ToTable("PaymentPlanInstallments");
            builder.Property(x => x.AmountDue).HasColumnType("decimal(18,2)");
            builder.Property(x => x.AmountPaid).HasColumnType("decimal(18,2)");
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.Notes).HasMaxLength(500);

            builder.HasOne(x => x.PaymentPlan)
                   .WithMany(x => x.Installments)
                   .HasForeignKey(x => x.PaymentPlanId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
