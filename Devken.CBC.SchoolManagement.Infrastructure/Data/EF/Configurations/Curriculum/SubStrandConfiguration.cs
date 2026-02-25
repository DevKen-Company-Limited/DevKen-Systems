using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Persistence.Configurations
{
    public class SubStrandConfiguration : IEntityTypeConfiguration<SubStrand>
    {
        public void Configure(EntityTypeBuilder<SubStrand> builder)
        {
            builder.ToTable("SubStrand");

            builder.HasKey(ss => ss.Id);

            builder.Property(ss => ss.Name)
                   .IsRequired()
                   .HasMaxLength(150);

            // LearningOutcomes configured from LearningOutcomeConfiguration
        }
    }
}