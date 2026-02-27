using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Academic
{
    public class AcademicYearConfiguration : IEntityTypeConfiguration<AcademicYear>
    {
        private readonly TenantContext _tenantContext;

        public AcademicYearConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<AcademicYear> builder)
        {
            builder.ToTable("AcademicYears");
            builder.HasKey(ay => ay.Id);

            builder.Ignore(ay => ay.IsActive);

            builder.HasQueryFilter(ay =>
                _tenantContext.TenantId == null ||
                ay.TenantId == _tenantContext.TenantId);

            builder.HasIndex(ay => new { ay.TenantId, ay.Code }).IsUnique();
            builder.HasIndex(ay => new { ay.TenantId, ay.IsCurrent });

            builder.Property(ay => ay.Name).IsRequired().HasMaxLength(50);
            builder.Property(ay => ay.Code).IsRequired().HasMaxLength(20);
            builder.Property(ay => ay.StartDate).IsRequired();
            builder.Property(ay => ay.EndDate).IsRequired();

            builder.HasOne(ay => ay.School)
                   .WithMany(s => s.AcademicYears)
                   .HasForeignKey(ay => ay.TenantId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(ay => ay.Classes)
                   .WithOne(c => c.AcademicYear)
                   .HasForeignKey(c => c.AcademicYearId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(ay => ay.Students)
                   .WithOne(s => s.CurrentAcademicYear)
                   .HasForeignKey(s => s.CurrentAcademicYearId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(ay => ay.Terms)
                   .WithOne(t => t.AcademicYear)
                   .HasForeignKey(t => t.AcademicYearId)
                   .OnDelete(DeleteBehavior.Restrict);

            // ❌ REMOVED — HasMany(ay => ay.Assessments) was here before.
            // It is now handled exclusively in AssessmentConfiguration.cs
            // via HasOne(a => a.AcademicYear).WithMany(ay => ay.Assessments).
        }
    }
}