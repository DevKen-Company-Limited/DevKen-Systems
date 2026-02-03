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

            // Primary Key
            builder.HasKey(i => i.Id);

            // Tenant Query Filter
            builder.HasQueryFilter(i =>
                _tenantContext.TenantId == null || i.TenantId == _tenantContext.TenantId);

            // Properties
            builder.Property(i => i.InvoiceNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(i => i.Description)
                .HasMaxLength(500);

            builder.Property(i => i.Notes)
                .HasMaxLength(1000);

            builder.Property(i => i.TotalAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(i => i.AmountPaid)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0.0m);

            builder.Property(i => i.StatusInvoice)
                .HasDefaultValue(InvoiceStatus.Pending);

            // Ignore computed properties
            builder.Ignore(i => i.Balance);
            builder.Ignore(i => i.IsOverdue);

            // Relationships
            builder.HasOne(i => i.Student)
                .WithMany()
                .HasForeignKey(i => i.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(i => i.AcademicYear)
                .WithMany()
                .HasForeignKey(i => i.AcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(i => i.Term)
                .WithMany()
                .HasForeignKey(i => i.TermId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(i => i.Parent)
                .WithMany()
                .HasForeignKey(i => i.ParentId)
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
