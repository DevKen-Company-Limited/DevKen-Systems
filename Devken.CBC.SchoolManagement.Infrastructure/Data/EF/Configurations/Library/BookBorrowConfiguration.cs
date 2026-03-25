using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Library
{
    public class BookBorrowConfiguration : IEntityTypeConfiguration<BookBorrow>
    {
        private readonly TenantContext _tenantContext;

        public BookBorrowConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<BookBorrow> builder)
        {
            builder.ToTable("BookBorrows");

            builder.HasKey(bb => bb.Id);

            builder.HasQueryFilter(bb =>
                _tenantContext.TenantId == null ||
                bb.TenantId == _tenantContext.TenantId);

            // Indexes for common lookups
            builder.HasIndex(bb => new { bb.TenantId, bb.MemberId });
            builder.HasIndex(bb => new { bb.TenantId, bb.BorrowDate });
            builder.HasIndex(bb => new { bb.TenantId, bb.BStatus });

            builder.Property(bb => bb.BStatus)
                   .HasConversion<string>()
                   .IsRequired();

            builder.Property(bb => bb.BorrowDate).IsRequired();
            builder.Property(bb => bb.DueDate).IsRequired();

            // Relationships
            builder.HasOne(bb => bb.Member)
                   .WithMany() // Assuming LibraryMember has many borrows
                   .HasForeignKey(bb => bb.MemberId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(bb => bb.Items)
                   .WithOne(bi => bi.Borrow)
                   .HasForeignKey(bi => bi.BorrowId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}