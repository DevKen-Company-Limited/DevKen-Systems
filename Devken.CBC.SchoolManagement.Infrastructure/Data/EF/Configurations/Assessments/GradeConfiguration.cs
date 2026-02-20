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

            // Tenant Filter
            builder.HasQueryFilter(g =>
                _tenantContext.TenantId == null ||
                g.TenantId == _tenantContext.TenantId);

            // Indexes
            builder.HasIndex(g => new { g.TenantId, g.StudentId });
            builder.HasIndex(g => new { g.TenantId, g.SubjectId });
            builder.HasIndex(g => new { g.TenantId, g.TermId });
            builder.HasIndex(g => new { g.TenantId, g.AssessmentId });

            // Properties
            builder.Property(g => g.GradeLetter)
                   .HasMaxLength(10);

            builder.Property(g => g.GradeType)
                   .HasMaxLength(20);

            builder.Property(g => g.Score)
                   .HasPrecision(6, 2);

            builder.Property(g => g.MaximumScore)
                   .HasPrecision(6, 2);

            builder.Property(g => g.Remarks)
                   .HasMaxLength(500);

            builder.Property(g => g.AssessmentDate)
                   .IsRequired();

            // Relationships
            builder.HasOne(g => g.Student)
                   .WithMany(s => s.Grades)  // ✅ Points to Student.Grades collection
                   .HasForeignKey(g => g.StudentId)
                   .IsRequired()  // Changed to required since StudentId is not nullable
                   .OnDelete(DeleteBehavior.Cascade);  // Changed to Cascade as per StudentConfiguration

            builder.HasOne(g => g.Subject)
                   .WithMany(s => s.Grades)  // ✅ FIXED: Points to Subject.Grades collection
                   .HasForeignKey(g => g.SubjectId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(g => g.Term)
                   .WithMany(t => t.Grades)
                   .HasForeignKey(g => g.TermId)
                   .OnDelete(DeleteBehavior.Restrict);

            //builder.HasOne(g => g.Assessment)
            //       //.WithMany(a => a.Grades)
            //       .HasForeignKey(g => g.AssessmentId)
            //       .IsRequired(false)
            //       .OnDelete(DeleteBehavior.Restrict);
        }
    }
}