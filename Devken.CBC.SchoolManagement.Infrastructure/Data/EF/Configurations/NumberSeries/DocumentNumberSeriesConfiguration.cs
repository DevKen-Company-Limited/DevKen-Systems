using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration; // For School
using Devken.CBC.SchoolManagement.Domain.Entities.NumberSeries;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Domain.Entities.NumberSeries
{
    public class DocumentNumberSeriesConfiguration : IEntityTypeConfiguration<DocumentNumberSeries>
    {
        private readonly TenantContext _tenantContext;

        public DocumentNumberSeriesConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<DocumentNumberSeries> builder)
        {
            builder.ToTable("DocumentNumberSeries");

            // ── Primary Key ────────────────────────────────
            builder.HasKey(d => d.Id);

            // ── Global query filter for multi-tenancy ─────
            builder.HasQueryFilter(d =>
                _tenantContext.TenantId == null ||
                d.TenantId == _tenantContext.TenantId);

            // ── Unique Index ──────────────────────────────
            builder.HasIndex(d => new { d.TenantId, d.EntityName }).IsUnique();

            // ── Properties ───────────────────────────────
            builder.Property(d => d.EntityName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(d => d.Prefix)
                .HasMaxLength(10);

            builder.Property(d => d.Padding)
                .IsRequired()
                .HasDefaultValue(5);

            builder.Property(d => d.LastNumber)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(d => d.ResetEveryYear)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(d => d.LastGeneratedYear);

            // ── Relationships ────────────────────────────
            builder.HasOne(d => d.Tenant)               // Link to School entity
                   .WithMany()                          // School can have many DocumentNumberSeries
                   .HasForeignKey(d => d.TenantId)      // FK is TenantId
                   .OnDelete(DeleteBehavior.Restrict);  // Prevent cascade delete
        }
    }
}
