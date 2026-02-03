using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Academic
{
    public class ClassConfiguration : IEntityTypeConfiguration<Class>
    {
        private readonly TenantContext _tenantContext;

        public ClassConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<Class> builder)
        {
            builder.ToTable("Classes");

            builder.HasKey(c => c.Id);

            builder.HasQueryFilter(c =>
                _tenantContext.TenantId == null ||
                c.TenantId == _tenantContext.TenantId);

            // Indexes
            builder.HasIndex(c => new { c.TenantId, c.Code }).IsUnique();
            builder.HasIndex(c => new { c.TenantId, c.Name });
            builder.HasIndex(c => new { c.TenantId, c.Level });

            // Properties
            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Code)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(c => c.Capacity)
                .HasDefaultValue(40);

            // Relationships
            builder.HasOne(c => c.ClassTeacher)
                .WithMany(t => t.Classes)
                .HasForeignKey(c => c.TeacherId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(c => c.AcademicYear)
                .WithMany(ay => ay.Classes)
                .HasForeignKey(c => c.AcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.Students)
                .WithOne(s => s.CurrentClass)
                .HasForeignKey(s => s.CurrentClassId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.Subjects)
                .WithMany(s => s.Classes)
                .UsingEntity(j => j.ToTable("ClassSubjects"));
        }
    }
}