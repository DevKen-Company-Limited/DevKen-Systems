using Devken.CBC.SchoolManagement.Domain.Entities.Accountings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Accounts
{
    public class BudgetLineConfiguration : IEntityTypeConfiguration<BudgetLine>
    {
        public void Configure(EntityTypeBuilder<BudgetLine> builder)
        {
            builder.ToTable("BudgetLines");
            builder.Property(x => x.BudgetedAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.Description).HasMaxLength(200);
            builder.Property(x => x.CostCentre).HasMaxLength(100);
            builder.Property(x => x.AccountType).HasConversion<string>().HasMaxLength(20);
            builder.Ignore(x => x.ActualAmount);
            builder.Ignore(x => x.Variance);
            builder.Ignore(x => x.VariancePercent);

            builder.HasOne(x => x.Budget)
                   .WithMany(x => x.Lines)
                   .HasForeignKey(x => x.BudgetId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
