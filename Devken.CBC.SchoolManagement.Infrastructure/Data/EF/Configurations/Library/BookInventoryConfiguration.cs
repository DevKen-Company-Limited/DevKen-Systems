using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Library
{
    public class BookInventoryConfiguration : IEntityTypeConfiguration<BookInventory>
    {
        private readonly TenantContext _tenantContext;

        public BookInventoryConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<BookInventory> builder)
        {
            builder.ToTable("BookInventories");

            builder.HasKey(i => i.Id);

            builder.HasQueryFilter(i =>
                _tenantContext.TenantId == null ||
                i.TenantId == _tenantContext.TenantId);

            // One inventory record per book per school
            builder.HasIndex(i => new { i.TenantId, i.BookId }).IsUnique();

            builder.Property(i => i.TotalCopies).HasDefaultValue(0);
            builder.Property(i => i.AvailableCopies).HasDefaultValue(0);
            builder.Property(i => i.BorrowedCopies).HasDefaultValue(0);
            builder.Property(i => i.LostCopies).HasDefaultValue(0);
            builder.Property(i => i.DamagedCopies).HasDefaultValue(0);

            builder.HasOne(i => i.Book)
                   .WithMany()
                   .HasForeignKey(i => i.BookId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}