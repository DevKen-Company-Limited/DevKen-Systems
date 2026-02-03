using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Academic
{
    public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
    {
        private readonly TenantContext _tenantContext;

        public SubjectConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<Subject> builder)
        {
            builder.ToTable("Subjects");

            builder.HasKey(s => s.Id);

            builder.HasQueryFilter(s =>
                _tenantContext.TenantId == null ||
                s.TenantId == _tenantContext.TenantId);

            // Indexes
            builder.HasIndex(s => new { s.TenantId, s.Code }).IsUnique();
            builder.HasIndex(s => new { s.TenantId, s.Level });

            // Properties
            builder.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.Code)
                .IsRequired()
                .HasMaxLength(20);

            // Relationships
            builder.HasMany(s => s.Grades)
                .WithOne(g => g.Subject)
                .HasForeignKey(g => g.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}