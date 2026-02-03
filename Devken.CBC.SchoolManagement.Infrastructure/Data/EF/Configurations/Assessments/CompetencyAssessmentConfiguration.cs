using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    public class CompetencyAssessmentConfiguration : IEntityTypeConfiguration<CompetencyAssessment>
    {
        private readonly TenantContext _tenantContext;

        public CompetencyAssessmentConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<CompetencyAssessment> builder)
        {
            builder.ToTable("CompetencyAssessments");

            // ❌ Do NOT configure HasKey on derived type
            // builder.HasKey(ca => ca.Id);

            // Multi-tenant filter
            builder.HasQueryFilter(ca =>
                _tenantContext.TenantId == null ||
                ca.TenantId == _tenantContext.TenantId);

            // Properties
            builder.Property(ca => ca.CompetencyName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(ca => ca.Strand)
                .HasMaxLength(50);

            builder.Property(ca => ca.SubStrand)
                .HasMaxLength(50);

            builder.Property(ca => ca.RatingScale)
                .HasMaxLength(20);

            // Relationships
            builder.HasMany(ca => ca.Scores)
                .WithOne(cas => cas.CompetencyAssessment)
                .HasForeignKey(cas => cas.CompetencyAssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
