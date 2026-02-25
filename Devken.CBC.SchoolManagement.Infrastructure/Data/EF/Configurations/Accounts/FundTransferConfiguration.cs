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
    public class FundTransferConfiguration : IEntityTypeConfiguration<FundTransfer>
    {
        public void Configure(EntityTypeBuilder<FundTransfer> builder)
        {
            builder.ToTable("FundTransfers");
            builder.Property(x => x.TransferReference).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.Purpose).HasMaxLength(500).IsRequired();
            builder.Property(x => x.TransactionReference).HasMaxLength(100);
            builder.Property(x => x.Notes).HasMaxLength(1000);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

            builder.HasOne(x => x.FromAccount)
                   .WithMany(x => x.TransfersFrom)
                   .HasForeignKey(x => x.FromAccountId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ToAccount)
                   .WithMany(x => x.TransfersTo)
                   .HasForeignKey(x => x.ToAccountId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
