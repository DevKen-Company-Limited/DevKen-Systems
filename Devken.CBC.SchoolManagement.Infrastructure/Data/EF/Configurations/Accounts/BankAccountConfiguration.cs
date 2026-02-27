using Devken.CBC.SchoolManagement.Domain.Entities.Accountings.Banks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Accounts
{
    public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
    {
        public void Configure(EntityTypeBuilder<BankAccount> builder)
        {
            builder.ToTable("BankAccounts");
            builder.Property(x => x.AccountName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.AccountNumber).HasMaxLength(50).IsRequired();
            builder.Property(x => x.BankName).HasMaxLength(100);
            builder.Property(x => x.BranchName).HasMaxLength(50);
            builder.Property(x => x.SwiftCode).HasMaxLength(20);
            builder.Property(x => x.CurrentBalance).HasColumnType("decimal(18,2)");
            builder.Property(x => x.LastReconciledBalance).HasColumnType("decimal(18,2)");
            builder.Property(x => x.Notes).HasMaxLength(500);

            builder.HasOne(x => x.GlAccount)
                   .WithMany()
                   .HasForeignKey(x => x.GlAccountId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.AccountNumber }).IsUnique();
        }
    }

}
