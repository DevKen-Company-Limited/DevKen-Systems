using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Identity
{
    public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
    {
        private readonly TenantContext _tenantContext;

        public RolePermissionConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<RolePermission> builder)
        {
            builder.ToTable("RolePermissions");

            builder.HasKey(rp => rp.Id);

            builder.HasIndex(rp => new { rp.RoleId, rp.PermissionId })
                   .IsUnique();

            builder.HasOne(rp => rp.Role)
                   .WithMany(r => r.RolePermissions)
                   .HasForeignKey(rp => rp.RoleId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(rp => rp.Permission)
                   .WithMany(p => p.RolePermissions)
                   .HasForeignKey(rp => rp.PermissionId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(rp =>
                _tenantContext.TenantId == null ||
                rp.Role.TenantId == _tenantContext.TenantId);
        }
    }
}
