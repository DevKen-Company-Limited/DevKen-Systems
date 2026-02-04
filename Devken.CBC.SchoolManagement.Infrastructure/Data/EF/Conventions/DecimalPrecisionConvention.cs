using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.EF.Conventions
{
    public static class DecimalPrecisionConvention
    {
        public static void Apply(ModelBuilder modelBuilder)
        {
            foreach (IMutableProperty property in modelBuilder.Model
                         .GetEntityTypes()
                         .SelectMany(e => e.GetProperties())
                         .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                // Skip if explicitly configured
                if (property.GetPrecision() != null)
                    continue;

                property.SetPrecision(18);
                property.SetScale(2);
            }
        }
    }
}
