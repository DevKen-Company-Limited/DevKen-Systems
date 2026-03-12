using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Library
{
    public class BookCategoryConfiguration : IEntityTypeConfiguration<BookCategory>
    {
        private readonly TenantContext _tenantContext;

        public BookCategoryConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<BookCategory> builder)
        {
            builder.ToTable("BookCategories");

            builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500);

            builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();

            if (_tenantContext?.TenantId != null)
                builder.HasQueryFilter(x => x.TenantId == _tenantContext.TenantId);
        }
    }
}
