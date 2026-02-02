using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            // ── Unique index ─────────────────────────────────
            builder.HasIndex(rp => new { rp.RoleId, rp.PermissionId })
                .IsUnique();

            // ── Relationships ────────────────────────────────
            // ⚠️ IMPORTANT: Use NoAction to prevent cascade cycles
            builder.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.NoAction);  // Changed from default Cascade

            builder.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.NoAction);  // Changed from default Cascade

            // ── Global query filter (Multi-Tenant Isolation) ─
            // Apply filter matching the Role entity to prevent the warning
            builder.HasQueryFilter(rp =>
                _tenantContext.TenantId == null ||
                rp.Role.TenantId == _tenantContext.TenantId);
        }
    }
}
