using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Finance
{
    public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
    {
        private readonly TenantContext _tenantContext;

        public InvoiceItemConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<InvoiceItem> builder)
        {
            builder.ToTable("InvoiceItems");

            builder.Property(x => x.Description).HasMaxLength(200).IsRequired();
            builder.Property(x => x.ItemType).HasMaxLength(50);
            builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            builder.Property(x => x.Discount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TaxRate).HasColumnType("decimal(5,2)");
            builder.Property(x => x.Total).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TaxAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.NetAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.GlCode).HasMaxLength(100);
            builder.Property(x => x.Notes).HasMaxLength(500);

            // Ignore runtime-only properties
            builder.Ignore(x => x.EffectiveUnitPrice);

            builder.HasOne(x => x.Invoice)
                   .WithMany(x => x.Items)
                   .HasForeignKey(x => x.InvoiceId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.FeeItem)
                   .WithMany(x => x.InvoiceItems)
                   .HasForeignKey(x => x.FeeItemId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}