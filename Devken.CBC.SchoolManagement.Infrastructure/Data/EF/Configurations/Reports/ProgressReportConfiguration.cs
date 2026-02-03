using Devken.CBC.SchoolManagement.Domain.Entities.Reports;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Reports
{
    public class ProgressReportConfiguration : IEntityTypeConfiguration<ProgressReport>
    {
        private readonly TenantContext _tenantContext;

        public ProgressReportConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<ProgressReport> builder)
        {
            builder.ToTable("ProgressReports");

            builder.HasKey(pr => pr.Id);

            builder.HasQueryFilter(pr =>
                _tenantContext.TenantId == null ||
                pr.TenantId == _tenantContext.TenantId);

            // Indexes
            builder.HasIndex(pr => new { pr.TenantId, pr.StudentId, pr.TermId });
            builder.HasIndex(pr => new { pr.TenantId, pr.ReportDate });

            // Properties
            builder.Property(pr => pr.ReportType)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(pr => pr.OverallGrade)
                .HasMaxLength(10);

            // Relationships
            builder.HasOne(pr => pr.Student)
                .WithMany(s => s.ProgressReports)
                .HasForeignKey(pr => pr.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pr => pr.Class)
                .WithMany()
                .HasForeignKey(pr => pr.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(pr => pr.Term)
                .WithMany(t => t.ProgressReports)
                .HasForeignKey(pr => pr.TermId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(pr => pr.AcademicYear)
                .WithMany()
                .HasForeignKey(pr => pr.AcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(pr => pr.SubjectReports)
                .WithOne(sr => sr.ProgressReport)
                .HasForeignKey(sr => sr.ProgressReportId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(pr => pr.Comments)
                .WithOne(prc => prc.ProgressReport)
                .HasForeignKey(prc => prc.ProgressReportId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}