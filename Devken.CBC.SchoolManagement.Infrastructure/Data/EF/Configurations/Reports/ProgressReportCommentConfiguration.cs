using Devken.CBC.SchoolManagement.Domain.Entities.Reports;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Reports
{
    public class ProgressReportCommentConfiguration : IEntityTypeConfiguration<ProgressReportComment>
    {
        private readonly TenantContext _tenantContext;

        public ProgressReportCommentConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<ProgressReportComment> builder)
        {
            builder.ToTable("ProgressReportComments");

            builder.HasKey(prc => prc.Id);

            builder.HasQueryFilter(prc =>
                _tenantContext.TenantId == null ||
                prc.TenantId == _tenantContext.TenantId);

            // Indexes
            builder.HasIndex(prc => new { prc.TenantId, prc.ProgressReportId, prc.CommentDate });

            // Properties
            builder.Property(prc => prc.CommentType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(prc => prc.Comment)
                .IsRequired()
                .HasMaxLength(2000);
        }
    }
}