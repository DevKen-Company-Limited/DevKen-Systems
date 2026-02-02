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
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        private readonly TenantContext _tenantContext;

        public UserRoleConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            // ── Unique index ─────────────────────────────────
            builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
                .IsUnique();

            // ── Relationships ────────────────────────────────
            // ⚠️ IMPORTANT: Use NoAction to prevent cascade cycles
            builder.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.NoAction);  // Changed from default Cascade

            builder.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.NoAction);  // Changed from default Cascade

            // ── Audit fields ─────────────────────────────────
            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(ur => ur.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(ur => ur.UpdatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            // ── Global query filter (Multi-Tenant Isolation) ─
            builder.HasQueryFilter(ur =>
                _tenantContext.TenantId == null ||
                ur.TenantId == _tenantContext.TenantId);
        }
    }
}
