using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    public class FormativeAssessmentScoreConfiguration
        : IEntityTypeConfiguration<FormativeAssessmentScore>
    {
        private readonly TenantContext _tenantContext;

        public FormativeAssessmentScoreConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<FormativeAssessmentScore> builder)
        {
            builder.ToTable("FormativeAssessmentScores");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.StudentId).IsRequired();
            builder.Property(s => s.FormativeAssessmentId).IsRequired();
            builder.Property(s => s.Score).HasColumnType("decimal(18,2)");
            builder.Property(s => s.MaximumScore).HasColumnType("decimal(18,2)");
            builder.Property(s => s.Grade).HasMaxLength(10);
            builder.Property(s => s.PerformanceLevel).HasMaxLength(20);
            builder.Property(s => s.Feedback).HasMaxLength(2000);
            builder.Property(s => s.Strengths).HasMaxLength(500);
            builder.Property(s => s.AreasForImprovement).HasMaxLength(500);
            builder.Property(s => s.CompetencyArea).HasMaxLength(100);

            // Computed — not persisted
            builder.Ignore(s => s.Percentage);

            builder.HasOne(s => s.FormativeAssessment)
                   .WithMany(a => a.Scores)
                   .HasForeignKey(s => s.FormativeAssessmentId)
                   .OnDelete(DeleteBehavior.Cascade);

            //builder.HasOne(s => s.Student)
            //       .WithMany()
            //       .HasForeignKey(s => s.StudentId)
            //       .OnDelete(DeleteBehavior.Restrict);


            builder.HasOne(s => s.GradedBy)
                   .WithMany()
                   .HasForeignKey(s => s.GradedById)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => new { s.TenantId, s.FormativeAssessmentId });
            builder.HasIndex(s => new { s.TenantId, s.StudentId });

            builder.HasQueryFilter(s =>
                _tenantContext.TenantId == null ||
                s.TenantId == _tenantContext.TenantId);
        }
    }
}