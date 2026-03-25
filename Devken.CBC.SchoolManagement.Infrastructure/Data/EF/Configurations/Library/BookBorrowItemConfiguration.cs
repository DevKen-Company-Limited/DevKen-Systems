using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Library
{
    public class BookBorrowItemConfiguration : IEntityTypeConfiguration<BookBorrowItem>
    {
        private readonly TenantContext _tenantContext;

        public BookBorrowItemConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<BookBorrowItem> builder)
        {
            builder.ToTable("BookBorrowItems");

            builder.HasKey(bi => bi.Id);

            builder.HasQueryFilter(bi =>
                _tenantContext.TenantId == null ||
                bi.TenantId == _tenantContext.TenantId);

            builder.HasIndex(bi => new { bi.TenantId, bi.BookCopyId });
            builder.HasIndex(bi => new { bi.TenantId, bi.IsOverdue });

            builder.Property(bi => bi.IsOverdue).HasDefaultValue(false);

            // Relationships
            builder.HasOne(bi => bi.BookCopy)
                   .WithMany()
                   .HasForeignKey(bi => bi.BookCopyId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}