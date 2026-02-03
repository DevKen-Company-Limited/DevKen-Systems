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

            builder.HasKey(ii => ii.Id);

            builder.HasQueryFilter(ii =>
                _tenantContext.TenantId == null ||
                ii.TenantId == _tenantContext.TenantId);

            // Indexes
            builder.HasIndex(ii => new { ii.TenantId, ii.InvoiceId });

            // Properties
            builder.Property(ii => ii.Description)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(ii => ii.ItemType)
                .HasMaxLength(50);

            builder.Property(ii => ii.UnitPrice)
                .HasPrecision(18, 2);

            builder.Property(ii => ii.Discount)
                .HasPrecision(18, 2)
                .HasDefaultValue(0.0m);

            // Computed Columns
            builder.Property(ii => ii.Total)
                .HasComputedColumnSql("[Quantity] * [UnitPrice]");

            builder.Property(ii => ii.NetAmount)
                .HasComputedColumnSql("([Quantity] * [UnitPrice]) - [Discount]");

            // Relationships
            builder.HasOne(ii => ii.Invoice)
                .WithMany(i => i.Items)
                .HasForeignKey(ii => ii.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ii => ii.Term)
                .WithMany()
                .HasForeignKey(ii => ii.TermId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ii => ii.FeeItem)
                .WithMany(fi => fi.InvoiceItems)
                .HasForeignKey(ii => ii.FeeItemId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}