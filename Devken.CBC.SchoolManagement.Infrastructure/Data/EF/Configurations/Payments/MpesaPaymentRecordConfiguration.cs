using Devken.CBC.SchoolManagement.Domain.Entities.Payments;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Payments
{
    public sealed class MpesaPaymentRecordConfiguration
        : IEntityTypeConfiguration<MpesaPaymentRecord>
    {
        private readonly TenantContext _tenantContext;

        public MpesaPaymentRecordConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<MpesaPaymentRecord> builder)
        {
            // ---------------- TABLE ----------------
            builder.ToTable("MpesaPayments");

            // ---------------- KEY ----------------
            builder.HasKey(p => p.Id);

            // ---------------- MULTI-TENANCY ----------------
            builder.HasQueryFilter(p =>
                _tenantContext.TenantId == null ||
                p.TenantId == _tenantContext.TenantId);

            // ---------------- INDEXES ----------------
            builder.HasIndex(p => p.CheckoutRequestId).IsUnique();
            builder.HasIndex(p => p.MerchantRequestId);
            builder.HasIndex(p => p.AccountReference);
            builder.HasIndex(p => p.PaymentStatus);
            builder.HasIndex(p => p.CreatedOn);
            builder.HasIndex(p => new { p.TenantId, p.CheckoutRequestId });

            // ---------------- PROPERTIES ----------------
            builder.Property(p => p.CheckoutRequestId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.MerchantRequestId)
                .HasMaxLength(100);

            builder.Property(p => p.PhoneNumber)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(p => p.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(p => p.AccountReference)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.TransactionDesc)
                .HasMaxLength(200);

            builder.Property(p => p.PaymentStatus)
                .IsRequired();

            builder.Property(p => p.ResultCode)
                .HasConversion<int>();

            builder.Property(p => p.ResultDesc)
                .HasMaxLength(500);

            builder.Property(p => p.MpesaReceiptNumber)
                .HasMaxLength(100);

            builder.Property(p => p.CreatedOn)
                .IsRequired();

            builder.Property(p => p.UpdatedOn)
                .IsRequired();

            builder.Property(p => p.CreatedBy)
                .IsRequired();

            builder.Property(p => p.UpdatedBy)
                .IsRequired();

            builder.Property(p => p.TenantId)
                .IsRequired();
        }
    }
}