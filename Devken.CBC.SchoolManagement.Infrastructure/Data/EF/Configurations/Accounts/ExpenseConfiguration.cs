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
    public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
    {
        public void Configure(EntityTypeBuilder<Expense> builder)
        {
            builder.ToTable("Expenses");
            builder.Property(x => x.ExpenseNumber).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
            builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TaxAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(30);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.ReceiptNumber).HasMaxLength(50);
            builder.Property(x => x.CostCentre).HasMaxLength(100);
            builder.Property(x => x.Notes).HasMaxLength(1000);
            builder.Ignore(x => x.TotalAmount);

            builder.HasIndex(x => new { x.TenantId, x.ExpenseNumber }).IsUnique();
        }
    }
}
