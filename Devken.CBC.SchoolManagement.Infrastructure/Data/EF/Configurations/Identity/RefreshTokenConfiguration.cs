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
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        private readonly TenantContext _tenantContext;

        public RefreshTokenConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            // ── Unique index ─────────────────────────────────
            builder.HasIndex(rt => rt.Token)
                .IsUnique();

            // ── Relationships ────────────────────────────────
            // ⚠️ IMPORTANT: Use NoAction to prevent cascade cycles
            builder.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.NoAction);  // Changed from default Cascade

            builder.HasOne(rt => rt.ReplacedByToken)
                .WithOne()
                .HasForeignKey<RefreshToken>(rt => rt.ReplacedByTokenId)
                .OnDelete(DeleteBehavior.NoAction);

            // ── Audit fields ─────────────────────────────────
            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(rt => rt.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(rt => rt.UpdatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            // ── Global query filter (Multi-Tenant Isolation) ─
            builder.HasQueryFilter(rt =>
                _tenantContext.TenantId == null ||
                rt.TenantId == _tenantContext.TenantId);
        }
    }
}
