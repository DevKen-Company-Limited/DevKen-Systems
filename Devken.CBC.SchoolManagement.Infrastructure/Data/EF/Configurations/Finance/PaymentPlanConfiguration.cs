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
    public class PaymentPlanConfiguration : IEntityTypeConfiguration<PaymentPlan>
    {
        public void Configure(EntityTypeBuilder<PaymentPlan> builder)
        {
            builder.ToTable("PaymentPlans");
            builder.Property(x => x.PlanName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(500);
            builder.Ignore(x => x.TotalScheduled);
            builder.Ignore(x => x.TotalPaid);
            builder.Ignore(x => x.PendingInstallments);

            builder.HasOne(x => x.Invoice)
                   .WithOne(x => x.PaymentPlan)
                   .HasForeignKey<PaymentPlan>(x => x.InvoiceId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
