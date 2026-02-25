// Devken.CBC.SchoolManagement.Infrastructure/Data/EF/Configurations/Finance/CreditNoteConfiguration.cs
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Finance
{
    public class CreditNoteConfiguration : IEntityTypeConfiguration<CreditNote>
    {

        public void Configure(EntityTypeBuilder<CreditNote> builder)
        {
            builder.ToTable("CreditNotes");

            // ── Columns ───────────────────────────────────────────────────────
            builder.Property(x => x.CreditNoteNumber)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(x => x.Amount)
                   .HasColumnType("decimal(18,2)");

            builder.Property(x => x.AmountApplied)
                   .HasColumnType("decimal(18,2)")
                   .HasDefaultValue(0.0m);

            builder.Property(x => x.Reason)
                   .HasMaxLength(500)
                   .IsRequired();

            builder.Property(x => x.Notes)
                   .HasMaxLength(1000);

            builder.Property(x => x.Status)
                   .HasConversion<string>()
                   .HasMaxLength(20);

            // FK columns — nullable for AppliedToInvoiceId
            builder.Property(x => x.AppliedToInvoiceId).IsRequired(false);
            builder.Property(x => x.IssuedBy).IsRequired(false);

            // ── Computed — never persisted ────────────────────────────────────
            builder.Ignore(x => x.RemainingBalance);

            // ── Relationships ─────────────────────────────────────────────────

            // The invoice that originated this credit note (required)
            // Restrict: do not cascade-delete the credit note when the invoice
            // is deleted — the credit note is a financial record that must persist.
            builder.HasOne(x => x.Invoice)
                   .WithMany(x => x.CreditNotes)
                   .HasForeignKey(x => x.InvoiceId)
                   .IsRequired(true)
                   .OnDelete(DeleteBehavior.Restrict);

            // The invoice this credit note was applied against (optional — null until applied).
            // WithMany(null): Invoice does not need a back-collection for applied credit notes,
            // so we use the parameterless overload to avoid requiring a second collection on Invoice.
            // Restrict: same reasoning — preserve the financial audit trail.
            builder.HasOne(x => x.AppliedToInvoice)
                   .WithMany()
                   .HasForeignKey(x => x.AppliedToInvoiceId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.Restrict);

            // ── Indexes ───────────────────────────────────────────────────────
            // Unique credit note number per tenant
            builder.HasIndex(x => new { x.TenantId, x.CreditNoteNumber }).IsUnique();

            // Speed up lookups by student and by applied-to invoice
            builder.HasIndex(x => x.StudentId);
            builder.HasIndex(x => x.AppliedToInvoiceId);
        }
    }
}