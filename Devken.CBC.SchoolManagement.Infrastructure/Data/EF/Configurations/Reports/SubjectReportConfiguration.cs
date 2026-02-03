using Devken.CBC.SchoolManagement.Domain.Entities.Reports;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Reports
{
    public class SubjectReportConfiguration : IEntityTypeConfiguration<SubjectReport>
    {
        private readonly TenantContext _tenantContext;

        public SubjectReportConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<SubjectReport> builder)
        {
            builder.ToTable("SubjectReports");

            builder.HasKey(sr => sr.Id);

            builder.HasQueryFilter(sr =>
                _tenantContext.TenantId == null ||
                sr.TenantId == _tenantContext.TenantId);

            // Indexes
            builder.HasIndex(sr => new { sr.TenantId, sr.ProgressReportId, sr.SubjectId }).IsUnique();

            // Properties
            builder.Property(sr => sr.Grade)
                .HasMaxLength(10);

            // Relationships
            builder.HasOne(sr => sr.ProgressReport)
                .WithMany(pr => pr.SubjectReports)
                .HasForeignKey(sr => sr.ProgressReportId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(sr => sr.Subject)
                .WithMany()
                .HasForeignKey(sr => sr.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(sr => sr.SubjectTeacher)
                .WithMany()
                .HasForeignKey(sr => sr.TeacherId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}