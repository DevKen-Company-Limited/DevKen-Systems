using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Academic
{
    public class ParentConfiguration : IEntityTypeConfiguration<Parent>
    {
        private readonly TenantContext _tenantContext;

        public ParentConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<Parent> builder)
        {
            builder.ToTable("Parents");

            builder.HasKey(p => p.Id);

            // ───────────── Tenant Query Filter ─────────────
            builder.HasQueryFilter(p =>
                _tenantContext.TenantId == null ||
                p.TenantId == _tenantContext.TenantId);

            // ───────────── Indexes (Important for CBC Performance) ─────────────

            builder.HasIndex(p => new { p.TenantId, p.Email });
            builder.HasIndex(p => new { p.TenantId, p.PhoneNumber });
            builder.HasIndex(p => new { p.TenantId, p.NationalIdNumber });

            // ───────────── Property Configurations ─────────────

            builder.Property(p => p.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.MiddleName)
                .HasMaxLength(100);

            builder.Property(p => p.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.PhoneNumber)
                .HasMaxLength(20);

            builder.Property(p => p.AlternativePhoneNumber)
                .HasMaxLength(20);

            builder.Property(p => p.Email)
                .HasMaxLength(150);

            builder.Property(p => p.Address)
                .HasMaxLength(500);

            builder.Property(p => p.NationalIdNumber)
                .HasMaxLength(20);

            builder.Property(p => p.PassportNumber)
                .HasMaxLength(20);

            builder.Property(p => p.Occupation)
                .HasMaxLength(100);

            builder.Property(p => p.Employer)
                .HasMaxLength(150);

            builder.Property(p => p.EmployerContact)
                .HasMaxLength(100);

            builder.Property(p => p.RelationshipToStudent)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.PortalUserId)
                .HasMaxLength(256);

            // ───────────── Boolean Defaults ─────────────

            builder.Property(p => p.IsPrimaryContact)
                .HasDefaultValue(true);

            builder.Property(p => p.IsEmergencyContact)
                .HasDefaultValue(true);

            builder.Property(p => p.HasPortalAccess)
                .HasDefaultValue(false);

            builder.Property(p => p.IsActive)
                .HasDefaultValue(true);

            // ───────────── Relationships ─────────────
            // DO NOT configure Student relationship here
            // DO NOT configure Invoice relationship here
            // They should be configured in their respective configurations
        }
    }
}