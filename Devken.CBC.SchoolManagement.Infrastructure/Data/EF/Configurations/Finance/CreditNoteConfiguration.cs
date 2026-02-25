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
    public class CreditNoteConfiguration : IEntityTypeConfiguration<CreditNote>
    {
        public void Configure(EntityTypeBuilder<CreditNote> builder)
        {
            builder.ToTable("CreditNotes");

            builder.Property(x => x.CreditNoteNumber).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.AmountApplied).HasColumnType("decimal(18,2)");
            builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(1000);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

            // Ignore computed
            builder.Ignore(x => x.RemainingBalance);

            builder.HasOne(x => x.Invoice)
                   .WithMany(x => x.CreditNotes)
                   .HasForeignKey(x => x.InvoiceId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.CreditNoteNumber }).IsUnique();
        }
    }
}
