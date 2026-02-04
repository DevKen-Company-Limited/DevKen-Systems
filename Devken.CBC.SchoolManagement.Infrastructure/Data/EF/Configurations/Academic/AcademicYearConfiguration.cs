using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Academic
{
    public class AcademicYearConfiguration : IEntityTypeConfiguration<AcademicYear>
    {
        private readonly TenantContext _tenantContext;

        public AcademicYearConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<AcademicYear> builder)
        {
            builder.ToTable("AcademicYears");

            builder.HasKey(ay => ay.Id);

            builder.HasQueryFilter(ay =>
                _tenantContext.TenantId == null ||
                ay.TenantId == _tenantContext.TenantId);

            // Indexes
            builder.HasIndex(ay => new { ay.TenantId, ay.Code }).IsUnique();
            builder.HasIndex(ay => new { ay.TenantId, ay.IsCurrent });

            // Properties
            builder.Property(ay => ay.Name)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(ay => ay.Code)
                .IsRequired()
                .HasMaxLength(9);

            builder.Property(ay => ay.StartDate)
                .IsRequired();

            builder.Property(ay => ay.EndDate)
                .IsRequired();

            // Relationships
            builder.HasMany(ay => ay.Classes)
                .WithOne(c => c.AcademicYear)
                .HasForeignKey(c => c.AcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(ay => ay.Students)
                .WithOne(s => s.CurrentAcademicYear)
                .HasForeignKey(s => s.CurrentAcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(ay => ay.Assessments)
                .WithOne(a => a.AcademicYear)
                .HasForeignKey(a => a.AcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}