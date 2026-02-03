using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
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

            builder.HasQueryFilter(p =>
                _tenantContext.TenantId == null ||
                p.TenantId == _tenantContext.TenantId);

            // Indexes
            builder.HasIndex(p => new { p.TenantId, p.PhoneNumber }).IsUnique().HasFilter("[PhoneNumber] IS NOT NULL");
            builder.HasIndex(p => new { p.TenantId, p.Email }).IsUnique().HasFilter("[Email] IS NOT NULL");

            // Properties
            builder.Property(p => p.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.PhoneNumber)
                .HasMaxLength(20);

            // Relationships
            builder.HasMany(p => p.Students)
                .WithOne()
                .HasForeignKey(s => s.ParentId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(p => p.Invoices)
                .WithOne(i => i.Parent)
                .HasForeignKey(i => i.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}