using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Library
{
    public class BookCopyConfiguration : IEntityTypeConfiguration<BookCopy>
    {
        private readonly TenantContext _tenantContext;

        public BookCopyConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<BookCopy> builder)
        {
            builder.ToTable("BookCopies");

            builder.HasKey(bc => bc.Id);

            builder.HasQueryFilter(bc =>
                _tenantContext.TenantId == null ||
                bc.TenantId == _tenantContext.TenantId);

            builder.HasIndex(bc => new { bc.TenantId, bc.AccessionNumber }).IsUnique();
            builder.HasIndex(bc => new { bc.TenantId, bc.Barcode }).IsUnique();
            builder.HasIndex(bc => new { bc.TenantId, bc.BookId });
            builder.HasIndex(bc => new { bc.TenantId, bc.LibraryBranchId });
            builder.HasIndex(bc => new { bc.TenantId, bc.IsAvailable });

            builder.Property(bc => bc.AccessionNumber).IsRequired().HasMaxLength(50);
            builder.Property(bc => bc.Barcode).IsRequired().HasMaxLength(50);
            builder.Property(bc => bc.QRCode).HasMaxLength(100);
            builder.Property(bc => bc.Condition)
                   .HasConversion<string>()
                   .HasDefaultValue(BookCondition.Good);
            builder.Property(bc => bc.IsAvailable).HasDefaultValue(true);
            builder.Property(bc => bc.IsLost).HasDefaultValue(false);
            builder.Property(bc => bc.IsDamaged).HasDefaultValue(false);

            // Book → BookCopy handled by BookConfiguration cascade
            builder.HasOne(bc => bc.LibraryBranch)
                   .WithMany(lb => lb.BookCopies)
                   .HasForeignKey(bc => bc.LibraryBranchId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}