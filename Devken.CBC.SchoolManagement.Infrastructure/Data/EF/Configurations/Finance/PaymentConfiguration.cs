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

            builder.Property(x => x.PaymentReference).HasMaxLength(50).IsRequired();
            builder.Property(x => x.ReceiptNumber).HasMaxLength(30);
            builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(30);
            builder.Property(x => x.StatusPayment).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.TransactionReference).HasMaxLength(100);
            builder.Property(x => x.Description).HasMaxLength(500);
            builder.Property(x => x.Notes).HasMaxLength(1000);
            builder.Property(x => x.MpesaCode).HasMaxLength(20);
            builder.Property(x => x.PhoneNumber).HasMaxLength(20);
            builder.Property(x => x.BankName).HasMaxLength(100);
            builder.Property(x => x.AccountNumber).HasMaxLength(50);
            builder.Property(x => x.ChequeNumber).HasMaxLength(50);
            builder.Property(x => x.ReversalReason).HasMaxLength(500);

            builder.HasOne(x => x.Student)
                   .WithMany()
                   .HasForeignKey(x => x.StudentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Invoice)
                   .WithMany(x => x.Payments)
                   .HasForeignKey(x => x.InvoiceId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Self-referencing for reversals
            builder.HasOne(x => x.ReversedFromPayment)
                   .WithOne(x => x.ReversalPayment)
                   .HasForeignKey<Payment>(x => x.ReversedFromPaymentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.PaymentReference }).IsUnique();
            builder.HasIndex(x => new { x.TenantId, x.StudentId });
            builder.HasIndex(x => x.MpesaCode).HasFilter("[MpesaCode] IS NOT NULL");
        }
    }
}