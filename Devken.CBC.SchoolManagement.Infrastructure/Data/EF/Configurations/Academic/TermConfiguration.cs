using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Academic
{
    public class TermConfiguration : IEntityTypeConfiguration<Term>
    {
        private readonly TenantContext _tenantContext;

        public TermConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<Term> builder)
        {
            builder.ToTable("Terms", t =>
            {
                t.HasCheckConstraint("CK_Term_ValidDates", "[StartDate] < [EndDate]");
                t.HasCheckConstraint("CK_Term_ValidTermNumber", "[TermNumber] BETWEEN 1 AND 3");
            });

            builder.HasKey(t => t.Id);

            builder.HasQueryFilter(t =>
                _tenantContext.TenantId == null ||
                t.TenantId == _tenantContext.TenantId);

            builder.HasIndex(t => new { t.TenantId, t.AcademicYearId, t.TermNumber }).IsUnique();
            builder.HasIndex(t => new { t.TenantId, t.IsCurrent });
            builder.HasIndex(t => new { t.TenantId, t.StartDate, t.EndDate });

            builder.Property(t => t.Name).IsRequired().HasMaxLength(50);
            builder.Property(t => t.TermNumber).IsRequired();
            builder.Property(t => t.StartDate).IsRequired();
            builder.Property(t => t.EndDate).IsRequired();
            builder.Property(t => t.Notes).HasMaxLength(1000);
            builder.Property(t => t.IsCurrent).HasDefaultValue(false);
            builder.Property(t => t.IsClosed).HasDefaultValue(false);

            // Term → AcademicYear is owned by AcademicYearConfiguration
            // via HasMany(ay => ay.Terms).WithOne(t => t.AcademicYear).
            // Do NOT configure it here as well.

            builder.HasMany(t => t.ProgressReports)
                   .WithOne(pr => pr.Term)
                   .HasForeignKey(pr => pr.TermId)
                   .OnDelete(DeleteBehavior.Restrict);

            // ❌ REMOVED — HasMany(t => t.Assessments) was here before.
            // It is now handled exclusively in AssessmentConfiguration.cs
            // via HasOne(a => a.Term).WithMany(t => t.Assessments).
        }
    }
}