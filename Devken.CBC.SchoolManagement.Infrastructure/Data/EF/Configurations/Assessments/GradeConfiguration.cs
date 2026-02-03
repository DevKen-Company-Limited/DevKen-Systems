using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    public class GradeConfiguration : IEntityTypeConfiguration<Grade>
    {
        private readonly TenantContext _tenantContext;

        public GradeConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<Grade> builder)
        {
            builder.ToTable("Grades");

            builder.HasKey(g => g.Id);

            builder.HasQueryFilter(g =>
                _tenantContext.TenantId == null ||
                g.TenantId == _tenantContext.TenantId);

            // Indexes
            builder.HasIndex(g => new { g.TenantId, g.StudentId, g.SubjectId, g.TermId });
            builder.HasIndex(g => new { g.TenantId, g.AssessmentDate });

            // Properties
            builder.Property(g => g.GradeLetter)
                .HasMaxLength(10);

            builder.Property(g => g.GradeType)
                .HasMaxLength(20);

            // Relationships
            builder.HasOne(g => g.Student)
                .WithMany(s => s.Grades)
                .HasForeignKey(g => g.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(g => g.Subject)
                .WithMany(s => s.Grades)
                .HasForeignKey(g => g.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(g => g.Term)
                .WithMany()
                .HasForeignKey(g => g.TermId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(g => g.Assessment)
                .WithMany(a => a.Grades)
                .HasForeignKey(g => g.AssessmentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}