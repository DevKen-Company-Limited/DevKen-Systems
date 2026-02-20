using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Administration
{
    public class UserActivityConfiguration
        : IEntityTypeConfiguration<UserActivity>
    {
        private readonly TenantContext _tenantContext;

        public UserActivityConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<UserActivity> builder)
        {
            builder.ToTable("UserActivities");

            builder.HasKey(a => a.Id);

            // ── Multi-Tenant Global Filter ─────────────────────────────
            builder.HasQueryFilter(a =>
                _tenantContext.TenantId == null ||
                a.TenantId == _tenantContext.TenantId);

            // ── Indexes (Important for dashboard & paging performance) ─
            builder.HasIndex(a => a.UserId);
            builder.HasIndex(a => a.TenantId);
            builder.HasIndex(a => a.CreatedOn);
            builder.HasIndex(a => a.ActivityType);
            builder.HasIndex(a => new { a.TenantId, a.CreatedOn });
            builder.HasIndex(a => new { a.UserId, a.CreatedOn });

            // ── Properties ──────────────────────────────────────────────
            builder.Property(a => a.ActivityType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(a => a.ActivityDetails)
                .HasMaxLength(1000);

            builder.Property(a => a.CreatedOn)
                .IsRequired();

            // ── Relationships ───────────────────────────────────────────

            // User (Required)
            //builder.HasOne(a => a.User)
            //    //.WithMany(u => u.Activities)   // if you added navigation
            //    .HasForeignKey(a => a.UserId)
            //    .OnDelete(DeleteBehavior.Cascade);

            // School / Tenant (Optional)
            builder.HasOne(a => a.Tenant)
                .WithMany()
                .HasForeignKey(a => a.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
