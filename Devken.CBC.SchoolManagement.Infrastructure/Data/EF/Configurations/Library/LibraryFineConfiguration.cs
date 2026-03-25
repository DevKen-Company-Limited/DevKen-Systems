using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Library
{
    public class LibraryFineConfiguration : IEntityTypeConfiguration<LibraryFine>
    {
        private readonly TenantContext _tenantContext;

        public LibraryFineConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<LibraryFine> builder)
        {
            builder.ToTable("LibraryFines");

            builder.HasKey(lf => lf.Id);

            builder.HasQueryFilter(lf =>
                _tenantContext.TenantId == null ||
                lf.TenantId == _tenantContext.TenantId);

            builder.HasIndex(lf => new { lf.TenantId, lf.IsPaid });

            builder.Property(lf => lf.Amount)
                   .HasPrecision(18, 2)
                   .IsRequired();

            builder.Property(lf => lf.Reason)
                   .HasMaxLength(500);

            // Relationship
            builder.HasOne(lf => lf.BorrowItem)
                   .WithMany(bi => bi.Fines)
                   .HasForeignKey(lf => lf.BorrowItemId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}