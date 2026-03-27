// Infrastructure/Data/EF/Configurations/Library/LibrarySettingsConfiguration.cs
using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Library
{
    public class LibrarySettingsConfiguration : IEntityTypeConfiguration<LibrarySettings>
    {
        private readonly TenantContext _tenantContext;

        public LibrarySettingsConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<LibrarySettings> builder)
        {
            builder.ToTable("LibrarySettings");

            builder.HasKey(s => s.Id);

            // ── Global query filter (tenant isolation) ────────────────────────
            builder.HasQueryFilter(s =>
                _tenantContext.TenantId == null ||
                s.TenantId == _tenantContext.TenantId);

            // ── One settings record per school ────────────────────────────────
            builder.HasIndex(s => s.TenantId).IsUnique();

            // ── Properties ────────────────────────────────────────────────────
            builder.Property(s => s.FinePerDay)
                   .HasColumnType("decimal(10,2)");

            builder.Property(s => s.MaxBooksPerStudent).IsRequired();
            builder.Property(s => s.MaxBooksPerTeacher).IsRequired();
            builder.Property(s => s.BorrowDaysStudent).IsRequired();
            builder.Property(s => s.BorrowDaysTeacher).IsRequired();
            builder.Property(s => s.AllowBookReservation).IsRequired();
        }
    }
}