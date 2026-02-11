using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Academic
{
    public class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
    {
        private readonly TenantContext _tenantContext;

        public TeacherConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<Teacher> builder)
        {
            builder.ToTable("Teachers");

            builder.HasKey(t => t.Id);

            builder.HasQueryFilter(t =>
                _tenantContext.TenantId == null || t.TenantId == _tenantContext.TenantId);

            // Indexes
            builder.HasIndex(t => new { t.TenantId, t.TeacherNumber }).IsUnique();
            builder.HasIndex(t => t.TscNumber).IsUnique().HasFilter("[TscNumber] IS NOT NULL");
            builder.HasIndex(t => new { t.TenantId, t.IsActive });

            // Properties
            builder.Property(t => t.FirstName).IsRequired().HasMaxLength(100);
            builder.Property(t => t.LastName).IsRequired().HasMaxLength(100);
            builder.Property(t => t.TeacherNumber).IsRequired().HasMaxLength(50);
            builder.Property(t => t.TscNumber).HasMaxLength(50);

            // Relationships
            builder.HasMany(t => t.Classes)
                   .WithOne(c => c.ClassTeacher)
                   .HasForeignKey(c => c.TeacherId)
                   .OnDelete(DeleteBehavior.SetNull);

            // CBC Levels
            builder.HasMany(t => t.CBCLevels)
                   .WithOne(c => c.Teacher)
                   .HasForeignKey(c => c.TeacherId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
