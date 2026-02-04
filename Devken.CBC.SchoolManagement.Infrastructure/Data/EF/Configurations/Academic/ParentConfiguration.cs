using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Academic
{
    public class ParentConfiguration : IEntityTypeConfiguration<Parent>
    {
        private readonly TenantContext _tenantContext;

        public ParentConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<Parent> builder)
        {
            builder.ToTable("Parents");

            builder.HasKey(p => p.Id);

            // Tenant Filter
            builder.HasQueryFilter(p =>
                _tenantContext.TenantId == null ||
                p.TenantId == _tenantContext.TenantId);

            // Indexes
            builder.HasIndex(p => new { p.TenantId, p.Email });
            builder.HasIndex(p => new { p.TenantId, p.PhoneNumber });

            // Properties
            builder.Property(p => p.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.MiddleName)
                .HasMaxLength(100);

            builder.Property(p => p.PhoneNumber)
                .HasMaxLength(20);

            builder.Property(p => p.Email)
                .HasMaxLength(100);

            builder.Property(p => p.Address)
                .HasMaxLength(500);

            builder.Property(p => p.Occupation)
                .HasMaxLength(50);

            builder.Property(p => p.Employer)
                .HasMaxLength(100);

            // Relationships
            // DO NOT configure Student relationship here - it's configured in StudentConfiguration
            // DO NOT configure Invoice relationship here - it's configured in InvoiceConfiguration

            // If you have these, REMOVE them:
            // builder.HasMany(p => p.Students)...
            // builder.HasMany(p => p.Invoices)...
        }
    }
}