using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Academic
{
    public class LearningOutcomeConfiguration : IEntityTypeConfiguration<LearningOutcome>
    {
        private readonly TenantContext _tenantContext;

        public LearningOutcomeConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<LearningOutcome> builder)
        {
            builder.ToTable("LearningOutcomes");

            builder.HasKey(lo => lo.Id);

            builder.HasQueryFilter(lo =>
                _tenantContext.TenantId == null ||
                lo.TenantId == _tenantContext.TenantId);

            // Indexes
            builder.HasIndex(lo => new { lo.TenantId, lo.Code }).IsUnique();
            builder.HasIndex(lo => new { lo.TenantId, lo.Level, lo.Strand, lo.SubStrand });

            // Properties
            builder.Property(lo => lo.Outcome)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(lo => lo.Code)
                .HasMaxLength(50);

            builder.Property(lo => lo.Strand)
                .HasMaxLength(100);

            builder.Property(lo => lo.SubStrand)
                .HasMaxLength(100);
        }
    }
}