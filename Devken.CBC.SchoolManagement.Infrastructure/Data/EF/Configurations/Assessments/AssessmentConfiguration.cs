using Devken.CBC.SchoolManagement.Domain.Entities.Assessment;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    public class AssessmentConfiguration : IEntityTypeConfiguration<Assessment1>
    {
        private readonly TenantContext _tenantContext;

        public AssessmentConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<Assessment1> builder)
        {
            // Base table for all assessments (TPH)
            builder.ToTable("Assessments");

            // Primary Key (only on base type)
            builder.HasKey(a => a.Id);

            // Tenant Filter
            builder.HasQueryFilter(a =>
                _tenantContext.TenantId == null ||
                a.TenantId == _tenantContext.TenantId);

            // Indexes
            builder.HasIndex(a => new { a.TenantId, a.SubjectId, a.ClassId, a.AssessmentDate });
            builder.HasIndex(a => new { a.TenantId, a.AssessmentType });

            // Properties - Base properties only
            builder.Property(a => a.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(a => a.AssessmentType)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(a => a.Description)
                .HasMaxLength(1000);

            builder.Property(a => a.AssessmentDate)
                .IsRequired();

            // Relationships - Base relationships only
            builder.HasOne(a => a.Subject)
                .WithMany()
                .HasForeignKey(a => a.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.Class)
                .WithMany()
                .HasForeignKey(a => a.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.Term)
                .WithMany(t => t.Assessments)
                .HasForeignKey(a => a.TermId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.AcademicYear)
                .WithMany(ay => ay.Assessments)
                .HasForeignKey(a => a.AcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── TPH Discriminator ─────────────────────────────────────────────
            // This must be configured ONLY on the base type
            builder.HasDiscriminator<string>("AssessmentCategory")
                   .HasValue<Assessment1>("Base")
                   .HasValue<FormativeAssessment>("Formative")
                   .HasValue<SummativeAssessment>("Summative")
                   .HasValue<CompetencyAssessment>("Competency");
        }
    }
}