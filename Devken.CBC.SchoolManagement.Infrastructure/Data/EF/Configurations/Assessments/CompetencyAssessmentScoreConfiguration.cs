using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Assessments
{
    public class CompetencyAssessmentScoreConfiguration : IEntityTypeConfiguration<CompetencyAssessmentScore>
    {
        private readonly TenantContext _tenantContext;

        public CompetencyAssessmentScoreConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<CompetencyAssessmentScore> builder)
        {
            builder.ToTable("CompetencyAssessmentScores");

            builder.HasKey(cas => cas.Id);

            builder.HasQueryFilter(cas =>
                _tenantContext.TenantId == null ||
                cas.TenantId == _tenantContext.TenantId);

            // Indexes
            builder.HasIndex(cas => new { cas.TenantId, cas.CompetencyAssessmentId, cas.StudentId })
                .IsUnique();

            builder.HasIndex(cas => new { cas.TenantId, cas.StudentId });

            // Properties
            builder.Property(cas => cas.Rating)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(cas => cas.AssessmentMethod)
                .HasMaxLength(20);

            // Relationships
            builder.HasOne(cas => cas.CompetencyAssessment)
                .WithMany(ca => ca.Scores)
                .HasForeignKey(cas => cas.CompetencyAssessmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(cas => cas.Student)
                .WithMany(s => s.CompetencyAssessmentScores)
                .HasForeignKey(cas => cas.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(cas => cas.Assessor)
                .WithMany()
                .HasForeignKey(cas => cas.AssessorId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}