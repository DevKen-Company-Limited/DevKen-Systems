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
    public class BookPublisherConfiguration : IEntityTypeConfiguration<BookPublisher>
    {
        private readonly TenantContext _tenantContext;

        public BookPublisherConfiguration(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Configure(EntityTypeBuilder<BookPublisher> builder)
        {
            builder.ToTable("BookPublishers");

            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.Property(x => x.Address).HasMaxLength(500);

            builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();

            if (_tenantContext?.TenantId != null)
                builder.HasQueryFilter(x => x.TenantId == _tenantContext.TenantId);
        }
    }
}
