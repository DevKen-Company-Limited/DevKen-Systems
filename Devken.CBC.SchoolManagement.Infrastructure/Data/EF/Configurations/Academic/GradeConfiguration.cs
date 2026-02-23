using Devken.CBC.SchoolManagement.Domain.Entities.Assessments.Academic;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Academic
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

            // Prevent duplicate grade for same student+subject+term
            builder.HasIndex(g => new { g.TenantId, g.StudentId, g.SubjectId, g.TermId })
                   .IsUnique()
                   .HasFilter("[TermId] IS NOT NULL");

            builder.Property(g => g.Score).HasPrecision(8, 2);
            builder.Property(g => g.MaximumScore).HasPrecision(8, 2);
            builder.Property(g => g.Remarks).HasMaxLength(500);

            // Relationships — already defined on Grade entity navigation props
            // Student / Subject / Term / Assessment are configured on their own side
        }
    }
}
