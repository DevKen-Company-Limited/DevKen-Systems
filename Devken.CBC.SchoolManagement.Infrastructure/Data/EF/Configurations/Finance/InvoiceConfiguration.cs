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

            builder.Property(x => x.InvoiceNumber).HasMaxLength(50).IsRequired();
            builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.AmountPaid).HasColumnType("decimal(18,2)");
            builder.Property(x => x.StatusInvoice).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.Description).HasMaxLength(500);
            builder.Property(x => x.Notes).HasMaxLength(1000);

            // Ignore runtime-computed properties
            builder.Ignore(x => x.Balance);
            builder.Ignore(x => x.IsOverdue);

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

            builder.HasIndex(x => new { x.TenantId, x.InvoiceNumber }).IsUnique();
            builder.HasIndex(x => new { x.TenantId, x.StudentId, x.StatusInvoice });
            builder.HasIndex(x => x.DueDate);
        }
    }
}