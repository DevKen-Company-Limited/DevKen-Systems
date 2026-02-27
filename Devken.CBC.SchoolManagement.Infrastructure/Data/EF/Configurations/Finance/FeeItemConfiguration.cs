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

            builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500);
            builder.Property(x => x.DefaultAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TaxRate).HasColumnType("decimal(5,2)");
            builder.Property(x => x.GlCode).HasMaxLength(100);
            builder.Property(x => x.ApplicableTo).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.FeeType).HasConversion<string>().HasMaxLength(30);
            builder.Property(x => x.Recurrence).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.ApplicableLevel).HasConversion<string>().HasMaxLength(30);

            // Ignore computed property
            builder.Ignore(x => x.DisplayName);

            // Indexes
            builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            builder.HasIndex(x => new { x.TenantId, x.FeeType });
            builder.HasIndex(x => x.IsActive);
        }
    }
}