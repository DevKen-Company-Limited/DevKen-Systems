using Devken.CBC.SchoolManagement.Domain.Entities.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Accounts
{
    public class ChartOfAccountConfiguration : IEntityTypeConfiguration<ChartOfAccount>
    {
        public void Configure(EntityTypeBuilder<ChartOfAccount> builder)
        {
            builder.ToTable("ChartOfAccounts");

            builder.Property(x => x.AccountCode).HasMaxLength(20).IsRequired();
            builder.Property(x => x.AccountName).HasMaxLength(150).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500);
            builder.Property(x => x.CurrentBalance).HasColumnType("decimal(18,2)");
            builder.Property(x => x.AccountType).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.AccountSubType).HasConversion<string>().HasMaxLength(30);
            builder.Property(x => x.NormalBalance).HasConversion<string>().HasMaxLength(10);
            builder.Ignore(x => x.DisplayCode);

            builder.HasOne(x => x.ParentAccount)
                   .WithMany(x => x.ChildAccounts)
                   .HasForeignKey(x => x.ParentAccountId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.AccountCode }).IsUnique();
        }
    }
}
