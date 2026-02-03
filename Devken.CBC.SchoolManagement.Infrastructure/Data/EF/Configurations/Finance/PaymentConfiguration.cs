using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Finance
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        private readonly TenantContext _tenantContext;

        public PaymentConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments");

            builder.HasKey(p => p.Id);

            builder.HasQueryFilter(p =>
                _tenantContext.TenantId == null ||
                p.TenantId == _tenantContext.TenantId);

            // Indexes
            builder.HasIndex(p => new { p.TenantId, p.PaymentReference }).IsUnique();
            builder.HasIndex(p => new { p.TenantId, p.TransactionReference }).IsUnique().HasFilter("[TransactionReference] IS NOT NULL");
            builder.HasIndex(p => new { p.TenantId, p.StudentId, p.PaymentDate });

            // Properties
            builder.Property(p => p.PaymentReference)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.PaymentMethod)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.Amount)
                .HasPrecision(18, 2);

            builder.Property(p => p.MpesaCode)
                .HasMaxLength(20);

            // Relationships
            builder.HasOne(p => p.Student)
                .WithMany(s => s.Payments)
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.ReceivedByStaff)
                .WithMany()
                .HasForeignKey(p => p.ReceivedBy)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}