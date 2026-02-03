using Devken.CBC.SchoolManagement.Domain.Entities.Subscription;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Subscription
{
    public class SubscriptionConfiguration : IEntityTypeConfiguration<Domain.Entities.Subscription.Subscription>
    {
        private readonly TenantContext _tenantContext;

        public SubscriptionConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<Domain.Entities.Subscription.Subscription> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Currency)
                .HasMaxLength(3)
                .IsRequired();

            builder.Property(s => s.Amount)
                .HasPrecision(18, 2);

            builder.Property(s => s.MaxStorageGB)
                .HasPrecision(18, 2);

            builder.Property(s => s.EnabledFeatures)
                .HasMaxLength(500);

            builder.Property(s => s.AdminNotes)
                .HasMaxLength(1000);

            builder.HasIndex(s => new { s.TenantId, s.SchoolId });

            builder.HasIndex(s => new { s.SchoolId, s.ExpiryDate });

            builder.HasOne(s => s.School)
                .WithMany()
                .HasForeignKey(s => s.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(s =>
                _tenantContext.TenantId == null ||
                s.TenantId == _tenantContext.TenantId);
        }
    }
}
