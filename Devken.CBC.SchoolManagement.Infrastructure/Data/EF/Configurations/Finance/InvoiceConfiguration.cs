using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Finance
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        private readonly TenantContext _tenantContext;

        public InvoiceConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.ToTable("Invoices");

            builder.HasKey(x => x.Id);

            // ───────────── Tenant Query Filter ─────────────
            builder.HasQueryFilter(i =>
                _tenantContext.TenantId == null ||
                i.TenantId == _tenantContext.TenantId);

            // ───────────── Identity ─────────────
            builder.Property(x => x.InvoiceNumber)
                   .IsRequired()
                   .HasMaxLength(50);

            // ───────────── Financial Precision (VERY IMPORTANT) ─────────────
            builder.Property(x => x.TotalAmount)
                   .HasPrecision(18, 2);

            builder.Property(x => x.DiscountAmount)
                   .HasPrecision(18, 2);

            // 🔥 AmountPaid removed — DO NOT configure it anymore

            // ───────────── Enum Configuration ─────────────
            // Store as string (readable) OR int (performance)
            builder.Property(x => x.StatusInvoice)
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .IsRequired();

            // ───────────── Text Fields ─────────────
            builder.Property(x => x.Description)
                   .HasMaxLength(500);

            builder.Property(x => x.Notes)
                   .HasMaxLength(1000);

            // ───────────── Required Dates ─────────────
            builder.Property(x => x.InvoiceDate)
                   .IsRequired();

            builder.Property(x => x.DueDate)
                   .IsRequired();

            // ───────────── Ignore Computed Properties ─────────────
            builder.Ignore(x => x.Balance);
            builder.Ignore(x => x.IsOverdue);
            builder.Ignore(x => x.AmountPaid); // Now computed from Payments

            // ───────────── Relationships ─────────────

            builder.HasOne(x => x.Student)
                   .WithMany()
                   .HasForeignKey(x => x.StudentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.AcademicYear)
                   .WithMany()
                   .HasForeignKey(x => x.AcademicYearId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Term)
                   .WithMany()
                   .HasForeignKey(x => x.TermId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Parent)
                   .WithMany()
                   .HasForeignKey(x => x.ParentId)
                   .OnDelete(DeleteBehavior.Restrict);

            // ───────────── Indexes (Enterprise-Level) ─────────────

            builder.HasIndex(x => new { x.TenantId, x.InvoiceNumber })
                   .IsUnique();

            builder.HasIndex(x => new { x.TenantId, x.StudentId, x.StatusInvoice });

            builder.HasIndex(x => new { x.TenantId, x.DueDate });

            builder.HasIndex(x => new { x.TenantId, x.ParentId });
        }
    }
}