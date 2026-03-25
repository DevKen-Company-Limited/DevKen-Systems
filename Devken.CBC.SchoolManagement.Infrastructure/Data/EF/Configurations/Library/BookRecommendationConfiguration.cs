using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Library
{
    public class BookRecommendationConfiguration : IEntityTypeConfiguration<BookRecommendation>
    {
        private readonly TenantContext _tenantContext;

        public BookRecommendationConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<BookRecommendation> builder)
        {
            builder.ToTable("BookRecommendations");

            builder.HasKey(br => br.Id);

            builder.HasQueryFilter(br =>
                _tenantContext.TenantId == null ||
                br.TenantId == _tenantContext.TenantId);

            builder.HasIndex(br => new { br.TenantId, br.StudentId });
            builder.HasIndex(br => new { br.TenantId, br.BookId });

            builder.Property(br => br.Score).IsRequired();
            builder.Property(br => br.Reason).HasMaxLength(500);

            // Note: If you have a Book or Student entity, add relationships here
        }
    }
}