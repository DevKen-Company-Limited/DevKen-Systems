using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
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
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<Term> builder)
        {
            builder.ToTable("Terms");

            builder.HasKey(t => t.Id);

            // Tenant Filter
            builder.HasQueryFilter(t =>
                _tenantContext.TenantId == null ||
                t.TenantId == _tenantContext.TenantId);

            // Indexes
            builder.HasIndex(t => new { t.TenantId, t.AcademicYearId, t.TermNumber })
                .IsUnique();

            builder.HasIndex(t => new { t.TenantId, t.IsCurrent });
            builder.HasIndex(t => new { t.TenantId, t.StartDate, t.EndDate });

            // Properties Configuration
            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(t => t.TermNumber)
                .IsRequired();

            builder.Property(t => t.StartDate)
                .IsRequired();

            builder.Property(t => t.EndDate)
                .IsRequired();

            builder.Property(t => t.Notes)
                .HasMaxLength(1000);

            // Default Values
            builder.Property(t => t.IsCurrent)
                .HasDefaultValue(false);

            builder.Property(t => t.IsClosed)
                .HasDefaultValue(false);

            // Computed Property (if supported by your database)
            // Note: This might need to be handled in application logic instead
            // builder.Property(t => t.IsActive)
            //     .HasComputedColumnSql("CASE WHEN [IsClosed] = 0 AND GETDATE() BETWEEN [StartDate] AND [EndDate] THEN 1 ELSE 0 END", stored: true);

            // Relationships
            builder.HasOne(t => t.AcademicYear)
                .WithMany(ay => ay.Terms)
                .HasForeignKey(t => t.AcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);

            // Navigation Properties
            builder.HasMany(t => t.Assessments)
                .WithOne(a => a.Term)
                .HasForeignKey(a => a.TermId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(t => t.ProgressReports)
                .WithOne(pr => pr.Term)
                .HasForeignKey(pr => pr.TermId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ensure term dates are valid
            builder.HasCheckConstraint(
                "CK_Term_ValidDates",
                "[StartDate] < [EndDate]");

            builder.HasCheckConstraint(
                "CK_Term_ValidTermNumber",
                "[TermNumber] BETWEEN 1 AND 3");

            // Seed data for terms (optional - for initial setup)
            // This would typically be done in a seed service
            // builder.HasData(
            //     new Term
            //     {
            //         Id = Guid.NewGuid(),
            //         TenantId = _tenantContext.TenantId ?? Guid.Empty,
            //         Name = "Term 1",
            //         TermNumber = 1,
            //         AcademicYearId = academicYearId,
            //         StartDate = new DateTime(2024, 1, 10),
            //         EndDate = new DateTime(2024, 4, 5),
            //         IsCurrent = true,
            //         IsClosed = false
            //     },
            //     // ... other terms
            // );
        }
    }
}