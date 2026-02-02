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
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        private readonly TenantContext _tenantContext;

        public UserConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<User> builder)
        {
            // ── Unique index ─────────────────────────────────
            builder.HasIndex(u => new { u.TenantId, u.Email })
                .IsUnique();

            // ── Self-referencing audit fields ────────────────
            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(u => u.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(u => u.UpdatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            // ── Global query filter (Multi-Tenant Isolation) ─
            builder.HasQueryFilter(u =>
                _tenantContext.TenantId == null ||
                u.TenantId == _tenantContext.TenantId);
        }
    }
}
