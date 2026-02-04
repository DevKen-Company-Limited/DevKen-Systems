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
            builder.HasKey(i => i.Id);

            builder.HasQueryFilter(i =>
                _tenantContext.TenantId == null ||
                i.TenantId == _tenantContext.TenantId);

            builder.HasIndex(i => new { i.TenantId, i.StudentId });
            builder.HasIndex(i => new { i.TenantId, i.ParentId });
            builder.HasIndex(i => new { i.TenantId, i.InvoiceNumber }).IsUnique();
            builder.HasIndex(i => new { i.TenantId, i.StatusInvoice });

            builder.Property(i => i.InvoiceNumber)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(i => i.Description)
                   .HasMaxLength(500);

            builder.Property(i => i.Notes)
                   .HasMaxLength(1000);

            builder.Property(i => i.TotalAmount)
                   .HasPrecision(18, 2);

            builder.Property(i => i.AmountPaid)
                   .HasPrecision(18, 2)
                   .HasDefaultValue(0m);

            // ✅ OPTION 1: Remove HasDefaultValue - let the C# default handle it
            builder.Property(i => i.StatusInvoice)
                   .HasConversion<string>()
                   .IsRequired();
            // The Invoice entity already sets = InvoiceStatus.Pending as default

            builder.Ignore(i => i.Balance);
            builder.Ignore(i => i.IsOverdue);

            builder.HasOne(i => i.Student)
                   .WithMany(s => s.Invoices)
                   .HasForeignKey(i => i.StudentId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(i => i.Parent)
                   .WithMany(p => p.Invoices)
                   .HasForeignKey(i => i.ParentId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(i => i.AcademicYear)
                   .WithMany()
                   .HasForeignKey(i => i.AcademicYearId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(i => i.Term)
                   .WithMany()
                   .HasForeignKey(i => i.TermId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(i => i.Items)
                   .WithOne(ii => ii.Invoice)
                   .HasForeignKey(ii => ii.InvoiceId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(i => i.Payments)
                   .WithOne(p => p.Invoice)
                   .HasForeignKey(p => p.InvoiceId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}