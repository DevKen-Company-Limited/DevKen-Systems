using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    public class SummativeAssessmentScoreConfiguration : IEntityTypeConfiguration<SummativeAssessmentScore>
    {
        private readonly TenantContext _tenantContext;

        public SummativeAssessmentScoreConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<SummativeAssessmentScore> builder)
        {
            builder.ToTable("SummativeAssessmentScores");

            builder.HasKey(sas => sas.Id);

            //builder.HasQueryFilter(sas =>
            //    _tenantContext.TenantId == null ||
            //    sas.TenantId == _tenantContext.TenantId);

            // Indexes
            builder.HasIndex(sas => new { sas.TenantId, sas.SummativeAssessmentId, sas.StudentId })
                .IsUnique();

            builder.HasIndex(sas => new { sas.TenantId, sas.StudentId });
            builder.HasIndex(sas => new { sas.TenantId, sas.PositionInClass });

            // Properties
            builder.Property(sas => sas.TheoryScore)
                .HasPrecision(6, 2);

            builder.Property(sas => sas.PracticalScore)
                .HasPrecision(6, 2);

            builder.Property(sas => sas.MaximumTheoryScore)
                .HasPrecision(6, 2);

            builder.Property(sas => sas.MaximumPracticalScore)
                .HasPrecision(6, 2);

            builder.Property(sas => sas.Grade)
                .HasMaxLength(10);

            builder.Property(sas => sas.Remarks)
                .HasMaxLength(20);

            // Relationships
            builder.HasOne(sas => sas.SummativeAssessment)
                .WithMany(sa => sa.Scores)
                .HasForeignKey(sas => sas.SummativeAssessmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(sas => sas.Student)
                .WithMany(s => s.SummativeAssessmentScores)
                .HasForeignKey(sas => sas.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(sas => sas.GradedBy)
                .WithMany()
                .HasForeignKey(sas => sas.GradedById)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}