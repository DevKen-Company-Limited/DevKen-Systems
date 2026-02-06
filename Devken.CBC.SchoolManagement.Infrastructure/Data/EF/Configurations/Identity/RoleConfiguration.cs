using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Identity
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        private readonly TenantContext _tenantContext;

        public RoleConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Roles");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Name)
                   .IsRequired()
                   .HasMaxLength(150);

            builder.HasIndex(r => new { r.TenantId, r.Name })
                   .IsUnique();

            builder.HasMany(r => r.UserRoles)
                   .WithOne(ur => ur.Role)
                   .HasForeignKey(ur => ur.RoleId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(r => r.RolePermissions)
                   .WithOne(rp => rp.Role)
                   .HasForeignKey(rp => rp.RoleId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(r =>
                _tenantContext.TenantId == null ||
                r.TenantId == _tenantContext.TenantId);
        }
    }
}
