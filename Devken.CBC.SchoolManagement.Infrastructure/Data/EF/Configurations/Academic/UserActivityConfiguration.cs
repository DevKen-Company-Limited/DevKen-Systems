using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Administration
{
    public class UserActivityConfiguration : IEntityTypeConfiguration<UserActivity>
    {
        private readonly TenantContext _tenantContext;

        public UserActivityConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<UserActivity> builder)
        {
            builder.ToTable("UserActivities");
            builder.HasKey(a => a.Id);

            // ── Properties ───────────────────────────────────────────────
            builder.Property(a => a.UserId).IsRequired();
            builder.Property(a => a.ActivityType).IsRequired().HasMaxLength(100);
            builder.Property(a => a.ActivityDetails).HasMaxLength(1000);
            builder.Property(a => a.CreatedOn).IsRequired();

            // ── Indexes ──────────────────────────────────────────────────
            builder.HasIndex(a => a.UserId);
            builder.HasIndex(a => a.TenantId);
            builder.HasIndex(a => a.CreatedOn);
            builder.HasIndex(a => a.ActivityType);
            builder.HasIndex(a => new { a.TenantId, a.CreatedOn });
            builder.HasIndex(a => new { a.UserId, a.CreatedOn });

            // ── Relationships ────────────────────────────────────────────
            // User navigation removed from entity — no HasOne(User) here.
            // UserId is kept as a plain indexed FK column for querying,
            // but EF will not track or join User automatically. This
            // eliminates warning [10622] entirely without any schema change.

            builder.HasOne(a => a.Tenant)
                   .WithMany()
                   .HasForeignKey(a => a.TenantId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.Restrict);

            // ── Query Filter ─────────────────────────────────────────────
            builder.HasQueryFilter(a =>
                _tenantContext.TenantId == null ||
                a.TenantId == _tenantContext.TenantId);
        }
    }
}
