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
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        private readonly TenantContext _tenantContext;

        public RoleConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<Role> builder)
        {
            // ── Unique index ─────────────────────────────────
            builder.HasIndex(r => new { r.TenantId, r.Name })
                .IsUnique();

            // ── Audit fields ─────────────────────────────────
            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(r => r.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(r => r.UpdatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            // ── Global query filter (Multi-Tenant Isolation) ─
            builder.HasQueryFilter(r =>
                _tenantContext.TenantId == null ||
                r.TenantId == _tenantContext.TenantId);
        }
    }
}
