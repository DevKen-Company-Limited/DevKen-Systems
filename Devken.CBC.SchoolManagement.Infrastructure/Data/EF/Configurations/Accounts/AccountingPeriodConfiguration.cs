using Devken.CBC.SchoolManagement.Domain.Entities.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Configurations.Accounts
{
    public class AccountingPeriodConfiguration : IEntityTypeConfiguration<AccountingPeriod>
    {
        public void Configure(EntityTypeBuilder<AccountingPeriod> builder)
        {
            builder.ToTable("AccountingPeriods");
            builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.Notes).HasMaxLength(500);
            builder.HasIndex(x => new { x.TenantId, x.FiscalYear, x.PeriodNumber }).IsUnique();
        }
    }
}
