using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    public class SummativeAssessmentScoreConfiguration
        : IEntityTypeConfiguration<SummativeAssessmentScore>
    {
        private readonly TenantContext _tenantContext;

        public SummativeAssessmentScoreConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<SummativeAssessmentScore> builder)
        {
            builder.ToTable("SummativeAssessmentScores");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.StudentId).IsRequired();
            builder.Property(s => s.SummativeAssessmentId).IsRequired();
            builder.Property(s => s.TheoryScore).HasColumnType("decimal(18,2)");
            builder.Property(s => s.PracticalScore).HasColumnType("decimal(18,2)");
            builder.Property(s => s.MaximumTheoryScore).HasColumnType("decimal(18,2)");
            builder.Property(s => s.MaximumPracticalScore).HasColumnType("decimal(18,2)");
            builder.Property(s => s.Grade).HasMaxLength(10);
            builder.Property(s => s.Remarks).HasMaxLength(20);
            builder.Property(s => s.Comments).HasMaxLength(1000);

            // Computed — not persisted
            builder.Ignore(s => s.TotalScore);
            builder.Ignore(s => s.MaximumTotalScore);
            builder.Ignore(s => s.Percentage);
            builder.Ignore(s => s.PerformanceStatus);

            builder.HasOne(s => s.SummativeAssessment)
                   .WithMany(a => a.Scores)
                   .HasForeignKey(s => s.SummativeAssessmentId)
                   .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(s => s.Student)
           .WithMany(st => st.SummativeAssessmentScores)
           .HasForeignKey(s => s.StudentId)
           .OnDelete(DeleteBehavior.Restrict);


            builder.HasOne(s => s.GradedBy)
                   .WithMany()
                   .HasForeignKey(s => s.GradedById)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => new { s.TenantId, s.SummativeAssessmentId });
            builder.HasIndex(s => new { s.TenantId, s.StudentId });

            builder.HasQueryFilter(s =>
                _tenantContext.TenantId == null ||
                s.TenantId == _tenantContext.TenantId);
        }
    }
}