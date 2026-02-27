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
    public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
    {
        public void Configure(EntityTypeBuilder<Budget> builder)
        {
            builder.ToTable("Budgets");
            builder.Property(x => x.Title).HasMaxLength(150).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500);
            builder.Property(x => x.Notes).HasMaxLength(1000);
            builder.Property(x => x.TotalRevenueBudget).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TotalExpenseBudget).HasColumnType("decimal(18,2)");
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            builder.Ignore(x => x.BudgetedSurplusDeficit);
        }
    }
}
