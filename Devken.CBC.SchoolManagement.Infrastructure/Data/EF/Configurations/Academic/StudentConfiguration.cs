using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Academic
{
    public class StudentConfiguration : IEntityTypeConfiguration<Student>
    {
        private readonly TenantContext _tenantContext;

        public StudentConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<Student> builder)
        {
            // ✅ Modern approach: Configure table with check constraints
            builder.ToTable("Students", t =>
            {
                t.HasCheckConstraint(
                    "CK_Student_ValidDates",
                    "[DateOfAdmission] >= [DateOfBirth]");

                t.HasCheckConstraint(
                    "CK_Student_ValidAge",
                    "DATEDIFF(YEAR, [DateOfBirth], GETDATE()) BETWEEN 3 AND 25");

                t.HasCheckConstraint(
                    "CK_Student_ValidCBCLevel",
                    "[CurrentLevel] IN ('PP1','PP2','Grade1','Grade2','Grade3','Grade4','Grade5','Grade6','Grade7','Grade8','Grade9','Grade10','Grade11','Grade12')");
            });

            // Primary Key
            builder.HasKey(s => s.Id);

            // Tenant Filter
            builder.HasQueryFilter(s =>
                _tenantContext.TenantId == null ||
                s.TenantId == _tenantContext.TenantId);

            // Indexes
            builder.HasIndex(s => new { s.TenantId, s.AdmissionNumber }).IsUnique();
            builder.HasIndex(s => new { s.TenantId, s.NemisNumber })
                   .IsUnique()
                   .HasFilter("[NemisNumber] IS NOT NULL");

            builder.HasIndex(s => new { s.TenantId, s.CurrentClassId });
            builder.HasIndex(s => new { s.TenantId, s.CurrentLevel });
            builder.HasIndex(s => new { s.TenantId, s.Status });
            builder.HasIndex(s => new { s.TenantId, s.IsActive });
            builder.HasIndex(s => new { s.TenantId, s.ParentId });

            // Properties
            builder.Property(s => s.FirstName).IsRequired().HasMaxLength(100);
            builder.Property(s => s.LastName).IsRequired().HasMaxLength(100);
            builder.Property(s => s.AdmissionNumber).IsRequired().HasMaxLength(50);
            builder.Property(s => s.NemisNumber).HasMaxLength(50);
            builder.Property(s => s.DateOfBirth).IsRequired();
            builder.Property(s => s.DateOfAdmission).IsRequired();

            builder.Property(s => s.CurrentLevel)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(20);

            builder.Property(s => s.Status)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(20);

            // Relationships
            builder.HasOne(s => s.School)
                   .WithMany()
                   .HasForeignKey(s => s.TenantId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.CurrentClass)
                   .WithMany(c => c.Students)
                   .HasForeignKey(s => s.CurrentClassId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.CurrentAcademicYear)
                   .WithMany(ay => ay.Students)
                   .HasForeignKey(s => s.CurrentAcademicYearId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.Parent)
                   .WithMany(p => p.Students)
                   .HasForeignKey(s => s.ParentId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(s => s.Grades)
                   .WithOne(g => g.Student)
                   .HasForeignKey(g => g.StudentId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.FormativeAssessmentScores)
                   .WithOne(fas => fas.Student)
                   .HasForeignKey(fas => fas.StudentId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.SummativeAssessmentScores)
                   .WithOne(sas => sas.Student)
                   .HasForeignKey(sas => sas.StudentId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.CompetencyAssessmentScores)
                   .WithOne(cas => cas.Student)
                   .HasForeignKey(cas => cas.StudentId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.ProgressReports)
                   .WithOne(pr => pr.Student)
                   .HasForeignKey(pr => pr.StudentId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.Invoices)
                   .WithOne(i => i.Student)
                   .HasForeignKey(i => i.StudentId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.Payments)
                   .WithOne(p => p.Student)
                   .HasForeignKey(p => p.StudentId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}