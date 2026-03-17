using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Library
{
    public class BookConfiguration : IEntityTypeConfiguration<Book>
    {
        private readonly TenantContext _tenantContext;

        public BookConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<Book> builder)
        {
            builder.ToTable("Books");

            builder.HasKey(b => b.Id);

            builder.HasQueryFilter(b =>
                _tenantContext.TenantId == null ||
                b.TenantId == _tenantContext.TenantId);

            builder.HasIndex(b => new { b.TenantId, b.ISBN }).IsUnique();
            builder.HasIndex(b => new { b.TenantId, b.CategoryId });
            builder.HasIndex(b => new { b.TenantId, b.AuthorId });

            builder.Property(b => b.Title).IsRequired().HasMaxLength(255);
            builder.Property(b => b.ISBN).IsRequired().HasMaxLength(20);
            builder.Property(b => b.Language).HasMaxLength(50);
            builder.Property(b => b.Description).HasMaxLength(2000);

            builder.HasOne(b => b.Category)
                   .WithMany()
                   .HasForeignKey(b => b.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(b => b.Author)
                   .WithMany()
                   .HasForeignKey(b => b.AuthorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(b => b.Publisher)
                   .WithMany()
                   .HasForeignKey(b => b.PublisherId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(b => b.Copies)
                   .WithOne(bc => bc.Book)
                   .HasForeignKey(bc => bc.BookId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}