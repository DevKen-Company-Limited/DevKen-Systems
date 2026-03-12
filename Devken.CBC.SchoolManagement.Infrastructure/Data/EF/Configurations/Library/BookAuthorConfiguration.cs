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
    public class BookAuthorConfiguration : IEntityTypeConfiguration<BookAuthor>
    {
        private readonly TenantContext _tenantContext;

        public BookAuthorConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<BookAuthor> builder)
        {
            builder.ToTable("BookAuthors");

            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.Property(x => x.Biography).HasMaxLength(1000);

            builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();

            if (_tenantContext?.TenantId != null)
                builder.HasQueryFilter(x => x.TenantId == _tenantContext.TenantId);
        }
    }
}
