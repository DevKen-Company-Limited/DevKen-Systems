using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Library
{
    public class LibraryBranchConfiguration : IEntityTypeConfiguration<LibraryBranch>
    {
        private readonly TenantContext _tenantContext;

        public LibraryBranchConfiguration(TenantContext tenantContext)
            => _tenantContext = tenantContext;

        public void Configure(EntityTypeBuilder<LibraryBranch> builder)
        {
            builder.ToTable("LibraryBranches");

            builder.HasKey(lb => lb.Id);

            builder.HasQueryFilter(lb =>
                _tenantContext.TenantId == null ||
                lb.TenantId == _tenantContext.TenantId);

            builder.HasIndex(lb => new { lb.TenantId, lb.Name }).IsUnique();

            builder.Property(lb => lb.Name).IsRequired().HasMaxLength(150);
            builder.Property(lb => lb.Location).HasMaxLength(300);

            builder.HasMany(lb => lb.BookCopies)
                   .WithOne(bc => bc.LibraryBranch)
                   .HasForeignKey(bc => bc.LibraryBranchId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}