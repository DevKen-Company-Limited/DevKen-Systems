using Devken.CBC.SchoolManagement.Domain.Entities.Assessment;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    public class SummativeAssessmentConfiguration : IEntityTypeConfiguration<SummativeAssessment>
    {
        private readonly TenantContext _tenantContext;

        public SummativeAssessmentConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<SummativeAssessment> builder)
        {
            builder.ToTable("SummativeAssessments");

            // ❌ Do NOT set HasKey on derived type
            // builder.HasKey(sa => sa.Id);

            // Multi-tenant filter
            builder.HasQueryFilter(sa =>
                _tenantContext.TenantId == null ||
                sa.TenantId == _tenantContext.TenantId);

            // Properties
            builder.Property(sa => sa.ExamType)
                .HasMaxLength(50);

            builder.Property(sa => sa.PassMark)
                .HasDefaultValue(50.0m);

            // Relationships
            builder.HasMany(sa => sa.Scores)
                .WithOne(sas => sas.SummativeAssessment)
                .HasForeignKey(sas => sas.SummativeAssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
