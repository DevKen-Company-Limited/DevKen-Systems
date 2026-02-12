using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.NumberSeries;
using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.NumberSeries;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.NumberSeries
{
    /// <summary>
    /// Tenant-safe document number generator
    /// Inherits RepositoryBase for CRUD operations
    /// Fully transaction protected
    /// Implements IDocumentNumberSeriesRepository
    /// </summary>
    public class DocumentNumberService
        : RepositoryBase<DocumentNumberSeries, Guid>, IDocumentNumberSeriesRepository
    {
        private readonly IRepositoryManager _repository;

        public DocumentNumberService(
            AppDbContext context,
            TenantContext tenantContext,
            IRepositoryManager repository)
            : base(context, tenantContext)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        #region IDocumentNumberSeriesRepository Implementation

        /// <summary>
        /// Gets a series for the current tenant by entity name
        /// </summary>
        public async Task<DocumentNumberSeries?> GetByEntityAsync(string entityName, bool trackChanges)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be empty", nameof(entityName));

            // FIXED: Use _tenantContext.TenantId which is Guid, not Guid?
            var tenantId = _tenantContext.TenantId;

            return await FindByCondition(
                    x => x.EntityName == entityName && x.TenantId == tenantId,
                    trackChanges)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Gets all series for a specific tenant
        /// </summary>
        public async Task<IEnumerable<DocumentNumberSeries>> GetByTenantAsync(Guid tenantId)
        {
            if (tenantId == Guid.Empty)
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

            return await FindByCondition(x => x.TenantId == tenantId, false)
                .OrderBy(x => x.EntityName)
                .ToListAsync();
        }

        /// <summary>
        /// Generates the next document number for a given entity
        /// Increments the stored last number and saves changes
        /// </summary>
        public async Task<string> GenerateAsync(string entityName)
        {
            var series = await GetByEntityAsync(entityName, true)
                ?? throw new InvalidOperationException(
                    $"Number series not configured for '{entityName}'.");

            if (series.ResetEveryYear && series.LastGeneratedYear != DateTime.UtcNow.Year)
            {
                series.LastNumber = 0;
                series.LastGeneratedYear = DateTime.UtcNow.Year;
            }

            series.LastNumber++;

            Update(series);
            await _repository.SaveAsync();

            return Format(series);
        }


        /// <summary>
        /// Previews the next number without incrementing
        /// </summary>
        public async Task<string> PreviewAsync(string entityName)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be empty", nameof(entityName));

            var series = await GetByEntityAsync(entityName, false);

            if (series == null)
                throw new InvalidOperationException(
                    $"Number series not configured for '{entityName}'. Please contact system administrator.");

            var nextNumber = series.LastNumber + 1;
            if (series.ResetEveryYear && series.LastGeneratedYear != DateTime.UtcNow.Year)
                nextNumber = 1;

            return Format(series, nextNumber);
        }

        /// <summary>
        /// Creates a new number series configuration
        /// </summary>
        public async Task<DocumentNumberSeries> CreateSeriesAsync(
            string entityName,
            string prefix,
            int padding = 5,
            bool resetEveryYear = false,
            string? description = null)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be empty", nameof(entityName));
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Prefix cannot be empty", nameof(prefix));
            if (padding < 1 || padding > 10)
                throw new ArgumentException("Padding must be between 1 and 10", nameof(padding));

            await using var transaction = await _repository.BeginTransactionAsync();

            try
            {
                // Check if series already exists
                var existing = await GetByEntityAsync(entityName, true);
                if (existing != null)
                    throw new InvalidOperationException($"Number series for '{entityName}' already exists.");

                // FIXED: Use _tenantContext.TenantId which is Guid, not Guid?
                var tenantId = _tenantContext.TenantId;

                var series = new DocumentNumberSeries
                {
                    Id = Guid.NewGuid(),
                    TenantId = (Guid)tenantId, // Now this is Guid, not Guid?
                    EntityName = entityName,
                    Prefix = prefix.ToUpperInvariant(),
                    LastNumber = 0,
                    LastGeneratedYear = DateTime.UtcNow.Year,
                    Padding = padding,
                    ResetEveryYear = resetEveryYear,
                    Description = description, // This will work if property exists
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow,
                    Status = EntityStatus.Active // This requires EntityStatus enum
                };

                Create(series);
                await _repository.SaveAsync();
                await transaction.CommitAsync();

                return series;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Resets a series counter (Admin only)
        /// </summary>
        public async Task ResetSeriesAsync(string entityName, int? startFrom = null)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be empty", nameof(entityName));

            await using var transaction = await _repository.BeginTransactionAsync();

            try
            {
                var series = await GetByEntityAsync(entityName, true);
                if (series == null)
                    throw new InvalidOperationException($"Number series for '{entityName}' not found.");

                series.LastNumber = startFrom ?? 0;
                series.LastGeneratedYear = DateTime.UtcNow.Year;
                series.UpdatedOn = DateTime.UtcNow;

                Update(series);
                await _repository.SaveAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Checks if a series exists for an entity
        /// </summary>
        public async Task<bool> SeriesExistsAsync(string entityName)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be empty", nameof(entityName));

            var series = await GetByEntityAsync(entityName, false);
            return series != null;
        }

        #endregion

        #region Additional Helper Methods

        /// <summary>
        /// Bulk generate multiple numbers (for imports)
        /// </summary>
        public async Task<List<string>> GenerateBulkAsync(string entityName, int count)
        {
            if (count <= 0 || count > 1000)
                throw new ArgumentException("Count must be between 1 and 1000", nameof(count));

            var numbers = new List<string>();

            await using var transaction = await _repository.BeginTransactionAsync();

            try
            {
                for (int i = 0; i < count; i++)
                {
                    numbers.Add(await GenerateAsync(entityName));
                }

                await transaction.CommitAsync();
                return numbers;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Gets the current number without incrementing
        /// </summary>
        public async Task<string> GetCurrentAsync(string entityName)
        {
            var series = await GetByEntityAsync(entityName, false);
            if (series == null)
                throw new InvalidOperationException($"Number series for '{entityName}' not found.");

            return Format(series);
        }

        /// <summary>
        /// Gets statistics for all series in current tenant
        /// </summary>
        public async Task<Dictionary<string, object>> GetStatisticsAsync()
        {
            var series = await GetByTenantAsync((Guid)_tenantContext.TenantId);

            var stats = new Dictionary<string, object>
            {
                ["TotalSeries"] = series.Count(),
                ["Series"] = series.Select(s => new
                {
                    s.EntityName,
                    s.Prefix,
                    s.LastNumber,
                    s.LastGeneratedYear,
                    s.ResetEveryYear,
                    s.Padding,
                    NextNumber = s.LastNumber + 1,
                    FormattedNext = Format(s, s.LastNumber + 1)
                })
            };

            return stats;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Formats the document number with prefix, padding, and optional yearly reset
        /// </summary>
        private static string Format(DocumentNumberSeries series, int? overrideNumber = null)
        {
            var number = overrideNumber ?? series.LastNumber;
            var padded = number.ToString().PadLeft(series.Padding, '0');

            return series.ResetEveryYear
                ? $"{series.Prefix}-{DateTime.UtcNow.Year}-{padded}"
                : $"{series.Prefix}-{padded}";
        }

        #endregion
    }
}