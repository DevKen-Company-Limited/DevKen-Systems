using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Finance
{
    public class FeeItemConfiguration : IEntityTypeConfiguration<FeeItem>
    {
        private readonly TenantContext _tenantContext;

        public FeeItemConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<FeeItem> builder)
        {
            builder.ToTable("FeeItems");

            builder.HasKey(fi => fi.Id);

            builder.HasQueryFilter(fi =>
                _tenantContext.TenantId == null ||
                fi.TenantId == _tenantContext.TenantId);

            // Indexes
            builder.HasIndex(fi => new { fi.TenantId, fi.Code }).IsUnique();
            builder.HasIndex(fi => new { fi.TenantId, fi.FeeType });

            // Properties
            builder.Property(fi => fi.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(fi => fi.Code)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(fi => fi.DefaultAmount)
                .HasPrecision(18, 2);

            builder.Property(fi => fi.FeeType)
                .IsRequired()
                .HasMaxLength(50);

            // Relationships
            builder.HasMany(fi => fi.InvoiceItems)
                .WithOne(ii => ii.FeeItem)
                .HasForeignKey(ii => ii.FeeItemId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}