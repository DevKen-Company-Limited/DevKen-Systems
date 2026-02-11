using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Academic
{
    public class TeacherCBCLevelConfiguration : IEntityTypeConfiguration<TeacherCBCLevel>
    {
        private readonly TenantContext _tenantContext;

        public TeacherCBCLevelConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<TeacherCBCLevel> builder)
        {
            builder.ToTable("TeacherCBCLevels");

            // Primary Key
            builder.HasKey(t => t.Id);

            // Query filter for multi-tenancy
            builder.HasQueryFilter(t =>
                _tenantContext.TenantId == null || t.TenantId == _tenantContext.TenantId);

            // Relationships
            builder.HasOne(t => t.Teacher)
                   .WithMany(t => t.CBCLevels)
                   .HasForeignKey(t => t.TeacherId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Index for quick lookups
            builder.HasIndex(t => new { t.TenantId, t.TeacherId, t.Level }).IsUnique();

            // Properties
            builder.Property(t => t.Level)
                   .IsRequired();
        }
    }
}
