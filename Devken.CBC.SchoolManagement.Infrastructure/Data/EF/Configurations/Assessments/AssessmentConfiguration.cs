using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    /// <summary>
    /// Configures the root TPT table "Assessments" which holds only the columns
    /// shared by all assessment subtypes. Subtype-specific columns live in their
    /// own dedicated tables (FormativeAssessments, SummativeAssessments,
    /// CompetencyAssessments) and are configured in their own IEntityTypeConfiguration.
    /// </summary>
    public class AssessmentConfiguration : IEntityTypeConfiguration<Assessment1>
    {
        private readonly TenantContext _tenantContext;

        public AssessmentConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<Assessment1> builder)
        {
<<<<<<< HEAD
            // Table name is declared in AppDbContext via UseTptMappingStrategy().
            // We only configure columns and indexes here.

            builder.Property(a => a.Title)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(a => a.Description)
                   .HasMaxLength(500);

            builder.Property(a => a.AssessmentType)
                   .IsRequired()
                   .HasMaxLength(20);

            builder.Property(a => a.MaximumScore)
                   .HasColumnType("decimal(18,2)");

            builder.Property(a => a.IsPublished)
                   .HasDefaultValue(false);

            builder.Property(a => a.PublishedDate)
                   .IsRequired(false);

            // ── Foreign Keys ────────────────────────────────────────────

=======
            builder.ToTable("Assessments");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Title).IsRequired().HasMaxLength(200);
            builder.Property(a => a.Description).HasMaxLength(500);
            builder.Property(a => a.AssessmentType).IsRequired().HasMaxLength(20);
            builder.Property(a => a.MaximumScore).HasColumnType("decimal(18,2)");
            builder.Property(a => a.AssessmentDate).IsRequired();
            builder.Property(a => a.IsPublished).HasDefaultValue(false);
            builder.Property(a => a.TeacherId).IsRequired();
            builder.Property(a => a.SubjectId).IsRequired();
            builder.Property(a => a.ClassId).IsRequired();
            builder.Property(a => a.TermId).IsRequired();
            builder.Property(a => a.AcademicYearId).IsRequired();

            // Each relationship is owned HERE and nowhere else.
            // WithMany(collection) tells EF exactly which inverse nav to use,
            // preventing it from auto-discovering a second relationship.
>>>>>>> upstream/main
            builder.HasOne(a => a.Teacher)
                   .WithMany()
                   .HasForeignKey(a => a.TeacherId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.Subject)
                   .WithMany()
                   .HasForeignKey(a => a.SubjectId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.Class)
                   .WithMany()
                   .HasForeignKey(a => a.ClassId)
                   .OnDelete(DeleteBehavior.Restrict);

            // WithMany(t => t.Assessments) collapses the relationship that
            // TermConfiguration was also defining via HasMany(t => t.Assessments)
            // — having both caused the TermId1 shadow property.
            builder.HasOne(a => a.Term)
                   .WithMany()
                   .HasForeignKey(a => a.TermId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Same fix for AcademicYearId1 — AcademicYearConfiguration was also
            // defining HasMany(ay => ay.Assessments) causing the duplicate.
            builder.HasOne(a => a.AcademicYear)
                   .WithMany()
                   .HasForeignKey(a => a.AcademicYearId)
                   .OnDelete(DeleteBehavior.Restrict);

<<<<<<< HEAD
            // ── Indexes ──────────────────────────────────────────────────

            builder.HasIndex(a => a.TeacherId);
            builder.HasIndex(a => a.SubjectId);
            builder.HasIndex(a => a.ClassId);
            builder.HasIndex(a => a.TermId);
            builder.HasIndex(a => a.AcademicYearId);
            builder.HasIndex(a => a.AssessmentType);

            // ── Global Query Filter (multi-tenant) ───────────────────────
            if (_tenantContext?.TenantId != null)
            {
                builder.HasQueryFilter(a => a.TenantId == _tenantContext.TenantId);
            }
=======
            builder.HasQueryFilter(a =>
                _tenantContext.TenantId == null ||
                a.TenantId == _tenantContext.TenantId);
>>>>>>> upstream/main
        }
    }
}