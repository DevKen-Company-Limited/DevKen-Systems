using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    public class AssessmentConfiguration : IEntityTypeConfiguration<Assessment1>
    {
        private readonly TenantContext _tenantContext;

        public AssessmentConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<Assessment1> builder)
        {
            builder.ToTable("Assessments");

            builder.HasKey(a => a.Id);

            // ✅ Apply tenant filter at root level
            builder.HasQueryFilter(a =>
                _tenantContext.TenantId == null ||
                a.TenantId == _tenantContext.TenantId);

            // Indexes
            builder.HasIndex(a => new { a.TenantId, a.SubjectId, a.ClassId, a.AssessmentDate });
            builder.HasIndex(a => new { a.TenantId, a.AssessmentType });

            // Properties
            builder.Property(a => a.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(a => a.AssessmentType)
                .IsRequired()
                .HasMaxLength(20);

            // Relationships
            builder.HasOne(a => a.Subject)
                .WithMany()
                .HasForeignKey(a => a.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.Class)
                .WithMany()
                .HasForeignKey(a => a.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.Term)
                .WithMany(t => t.Assessments)
                .HasForeignKey(a => a.TermId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.AcademicYear)
                .WithMany(ay => ay.Assessments)
                .HasForeignKey(a => a.AcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
