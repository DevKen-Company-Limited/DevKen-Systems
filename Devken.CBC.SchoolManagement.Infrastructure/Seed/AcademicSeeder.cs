using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Seed
{
    /// <summary>
    /// Seeds academic data including academic years and classes
    /// </summary>
    public class AcademicSeeder
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger? _logger;

        public AcademicSeeder(AppDbContext dbContext, ILogger? logger = null)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Seeds academic year and classes for a specific school
        /// </summary>
        public async Task SeedAcademicDataAsync(Guid schoolId)
        {
            _logger?.LogInformation("=== Starting academic data seeding for school: {SchoolId} ===", schoolId);

            // Verify school exists
            var school = await _dbContext.Schools.FindAsync(schoolId);
            if (school == null)
            {
                _logger?.LogError("School with ID {SchoolId} not found", schoolId);
                throw new InvalidOperationException($"School with ID {schoolId} not found");
            }

            // Seed academic year first
            var academicYear = await SeedAcademicYearAsync(schoolId);

            // Then seed classes for that academic year
            await SeedClassesAsync(schoolId, academicYear.Id);

            _logger?.LogInformation("=== Academic data seeding completed for school: {SchoolId} ===", schoolId);
        }

        /// <summary>
        /// Seeds the current academic year for a school
        /// </summary>
        private async Task<AcademicYear> SeedAcademicYearAsync(Guid schoolId)
        {
            var currentYear = DateTime.UtcNow.Year;
            var code = $"{currentYear}/{currentYear + 1}";

            // Check if academic year already exists
            var existingYear = await _dbContext.AcademicYears
                .FirstOrDefaultAsync(ay => ay.TenantId == schoolId && ay.Code == code);

            if (existingYear != null)
            {
                _logger?.LogInformation("Academic year {Code} already exists for school {SchoolId}", code, schoolId);
                return existingYear;
            }

            var academicYear = new AcademicYear
            {
                Id = Guid.NewGuid(),
                TenantId = schoolId,
                Name = $"Academic Year {currentYear}/{currentYear + 1}",
                Code = code,
                StartDate = new DateTime(currentYear, 1, 1),
                EndDate = new DateTime(currentYear, 12, 31),
                IsCurrent = true,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow
            };

            _dbContext.AcademicYears.Add(academicYear);
            await _dbContext.SaveChangesAsync();

            _logger?.LogInformation("✅ Created academic year: {Code} for school {SchoolId}", code, schoolId);

            return academicYear;
        }

        /// <summary>
        /// Seeds classes for each CBC level
        /// </summary>
        private async Task SeedClassesAsync(Guid schoolId, Guid academicYearId)
        {
            var classesToSeed = new List<Class>();

            // Pre-Primary (PP1, PP2)
            classesToSeed.AddRange(CreateClassesForLevel(schoolId, academicYearId, CBCLevel.PP1, 2));
            classesToSeed.AddRange(CreateClassesForLevel(schoolId, academicYearId, CBCLevel.PP2, 2));

            // Lower Primary (Grade 1-3)
            classesToSeed.AddRange(CreateClassesForLevel(schoolId, academicYearId, CBCLevel.Grade1, 3));
            classesToSeed.AddRange(CreateClassesForLevel(schoolId, academicYearId, CBCLevel.Grade2, 3));
            classesToSeed.AddRange(CreateClassesForLevel(schoolId, academicYearId, CBCLevel.Grade3, 3));

            // Upper Primary (Grade 4-6)
            classesToSeed.AddRange(CreateClassesForLevel(schoolId, academicYearId, CBCLevel.Grade4, 3));
            classesToSeed.AddRange(CreateClassesForLevel(schoolId, academicYearId, CBCLevel.Grade5, 3));
            classesToSeed.AddRange(CreateClassesForLevel(schoolId, academicYearId, CBCLevel.Grade6, 3));

            // Junior Secondary (Grade 7-9)
            classesToSeed.AddRange(CreateClassesForLevel(schoolId, academicYearId, CBCLevel.Grade7, 2));
            classesToSeed.AddRange(CreateClassesForLevel(schoolId, academicYearId, CBCLevel.Grade8, 2));
            classesToSeed.AddRange(CreateClassesForLevel(schoolId, academicYearId, CBCLevel.Grade9, 2));

            // Filter out classes that already exist
            var existingClassCodes = await _dbContext.Classes
                .Where(c => c.TenantId == schoolId)
                .Select(c => c.Code)
                .ToListAsync();

            var newClasses = classesToSeed
                .Where(c => !existingClassCodes.Contains(c.Code))
                .ToList();

            if (newClasses.Any())
            {
                _dbContext.Classes.AddRange(newClasses);
                await _dbContext.SaveChangesAsync();

                _logger?.LogInformation("✅ Created {Count} classes for school {SchoolId}", newClasses.Count, schoolId);
            }
            else
            {
                _logger?.LogInformation("ℹ️ All classes already exist for school {SchoolId}", schoolId);
            }
        }

        /// <summary>
        /// Creates multiple classes for a specific CBC level
        /// </summary>
        private List<Class> CreateClassesForLevel(
            Guid schoolId,
            Guid academicYearId,
            CBCLevel level,
            int numberOfStreams)
        {
            var classes = new List<Class>();
            var streamNames = new[] { "A", "B", "C", "D", "E", "F" };

            for (int i = 0; i < numberOfStreams && i < streamNames.Length; i++)
            {
                var streamName = streamNames[i];
                var className = $"{GetLevelDisplayName(level)} {streamName}";
                var classCode = $"{GetLevelCode(level)}{streamName}";

                classes.Add(new Class
                {
                    Id = Guid.NewGuid(),
                    TenantId = schoolId,
                    AcademicYearId = academicYearId,
                    Name = className,
                    Code = classCode,
                    Level = level,
                    Capacity = 40,
                    TeacherId = null, // To be assigned later
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                });
            }

            return classes;
        }

        /// <summary>
        /// Gets the display name for a CBC level
        /// </summary>
        private string GetLevelDisplayName(CBCLevel level)
        {
            return level switch
            {
                CBCLevel.PP1 => "PP1",
                CBCLevel.PP2 => "PP2",
                CBCLevel.Grade1 => "Grade 1",
                CBCLevel.Grade2 => "Grade 2",
                CBCLevel.Grade3 => "Grade 3",
                CBCLevel.Grade4 => "Grade 4",
                CBCLevel.Grade5 => "Grade 5",
                CBCLevel.Grade6 => "Grade 6",
                CBCLevel.Grade7 => "Grade 7",
                CBCLevel.Grade8 => "Grade 8",
                CBCLevel.Grade9 => "Grade 9",
                _ => level.ToString()
            };
        }

        /// <summary>
        /// Gets the code prefix for a CBC level
        /// </summary>
        private string GetLevelCode(CBCLevel level)
        {
            return level switch
            {
                CBCLevel.PP1 => "PP1",
                CBCLevel.PP2 => "PP2",
                CBCLevel.Grade1 => "G1",
                CBCLevel.Grade2 => "G2",
                CBCLevel.Grade3 => "G3",
                CBCLevel.Grade4 => "G4",
                CBCLevel.Grade5 => "G5",
                CBCLevel.Grade6 => "G6",
                CBCLevel.Grade7 => "G7",
                CBCLevel.Grade8 => "G8",
                CBCLevel.Grade9 => "G9",
                _ => level.ToString()
            };
        }
    }

    /// <summary>
    /// Extension methods for academic seeding
    /// </summary>
    public static class AcademicSeedExtensions
    {
        /// <summary>
        /// Seeds academic data (academic year and classes) for a school
        /// </summary>
        public static async Task SeedAcademicDataAsync(
            this IServiceProvider services,
            Guid schoolId)
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetService<ILogger<AcademicSeeder>>();

            var seeder = new AcademicSeeder(dbContext, logger);
            await seeder.SeedAcademicDataAsync(schoolId);
        }

        /// <summary>
        /// Seeds academic data for the default school
        /// </summary>
        public static async Task SeedDefaultSchoolAcademicDataAsync(
            this IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetService<ILogger<AcademicSeeder>>();

            // Get default school
            var defaultSchool = await dbContext.Schools
                .FirstOrDefaultAsync(s => s.SlugName == "default-school");

            if (defaultSchool == null)
            {
                logger?.LogWarning("Default school not found. Cannot seed academic data.");
                return;
            }

            var seeder = new AcademicSeeder(dbContext, logger);
            await seeder.SeedAcademicDataAsync(defaultSchool.Id);
        }
    }
}