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
    public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
    {
        public void Configure(EntityTypeBuilder<JournalEntry> builder)
        {
            builder.ToTable("JournalEntries");

            builder.Property(x => x.JournalNumber).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(1000);
            builder.Property(x => x.SourceType).HasMaxLength(50);
            builder.Property(x => x.EntryType).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

            builder.Ignore(x => x.TotalDebits);
            builder.Ignore(x => x.TotalCredits);

            builder.HasOne(x => x.AccountingPeriod)
                   .WithMany(x => x.JournalEntries)
                   .HasForeignKey(x => x.AccountingPeriodId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ReversesJournal)
                   .WithOne()
                   .HasForeignKey<JournalEntry>(x => x.ReversesJournalId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.JournalNumber }).IsUnique();
            builder.HasIndex(x => new { x.TenantId, x.SourceType, x.SourceId });
        }
    }
}
