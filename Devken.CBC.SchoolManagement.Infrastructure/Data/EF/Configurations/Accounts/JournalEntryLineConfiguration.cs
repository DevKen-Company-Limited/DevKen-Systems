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
    public class JournalEntryLineConfiguration : IEntityTypeConfiguration<JournalEntryLine>
    {
        public void Configure(EntityTypeBuilder<JournalEntryLine> builder)
        {
            builder.ToTable("JournalEntryLines");
            builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.Description).HasMaxLength(300);
            builder.Property(x => x.CostCentre).HasMaxLength(100);
            builder.Property(x => x.Side).HasConversion<string>().HasMaxLength(10);

            builder.HasOne(x => x.JournalEntry)
                   .WithMany(x => x.Lines)
                   .HasForeignKey(x => x.JournalEntryId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Account)
                   .WithMany(x => x.JournalLines)
                   .HasForeignKey(x => x.AccountId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
