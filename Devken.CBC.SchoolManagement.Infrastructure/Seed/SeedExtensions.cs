using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Seed
{
    public static class SeedExtensions
    {
        public static async Task SeedDefaultDataAsync(this IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
            var permissionSeeder = serviceProvider.GetRequiredService<IPermissionSeedService>();

            // Use a default tenant (school) ID or fetch existing school
            var defaultSchool = await dbContext.Schools.FirstOrDefaultAsync();
            if (defaultSchool == null)
            {
                logger.LogInformation("No default school found. Creating one.");
                defaultSchool = new School
                {
                    Id = Guid.NewGuid(),
                    Name = "Default School",
                    SlugName = "default-school",
                    Email = "info@defaultschool.com",
                    PhoneNumber = "0000000000",
                    Address = "Default Address",
                    IsActive = true,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                };
                dbContext.Schools.Add(defaultSchool);
                await dbContext.SaveChangesAsync();
            }

            // Seed permissions and roles
            await permissionSeeder.SeedPermissionsAndRolesAsync(defaultSchool.Id);
        }
    }
}