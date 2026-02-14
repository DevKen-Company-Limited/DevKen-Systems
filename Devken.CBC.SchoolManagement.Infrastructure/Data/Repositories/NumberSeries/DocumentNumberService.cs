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
    /// Uses EF Core's built-in transaction handling (compatible with retry strategy)
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
        /// Gets a series for the current tenant by entity name (uses TenantContext)
        /// </summary>
        public async Task<DocumentNumberSeries?> GetByEntityAsync(string entityName, bool trackChanges)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be empty", nameof(entityName));

            var tenantId = _tenantContext.TenantId;

            return await FindByCondition(
                    x => x.EntityName == entityName && x.TenantId == tenantId,
                    trackChanges)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Gets a series for a specific tenant by entity name (explicit tenantId)
        /// Use this when SuperAdmin needs to work with a specific school's series
        /// </summary>
        public async Task<DocumentNumberSeries?> GetByEntityAsync(string entityName, Guid tenantId, bool trackChanges)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be empty", nameof(entityName));

            if (tenantId == Guid.Empty)
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

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
        /// Generates the next document number for a given entity (uses TenantContext)
        /// Increments the stored last number and saves changes
        /// EF Core handles the transaction automatically with SaveAsync
        /// </summary>
        public async Task<string> GenerateAsync(string entityName)
        {
            var tenantId = _tenantContext.TenantId;
            return await GenerateAsync(entityName, (Guid)tenantId);
        }

        /// <summary>
        /// Generates the next document number for a given entity (explicit tenantId)
        /// Use this when SuperAdmin creates resources for a specific school
        /// Increments the stored last number and saves changes
        /// EF Core handles the transaction automatically with SaveAsync
        /// </summary>
        public async Task<string> GenerateAsync(string entityName, Guid tenantId)
        {
            if (tenantId == Guid.Empty)
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

            var series = await GetByEntityAsync(entityName, tenantId, true)
                ?? throw new InvalidOperationException(
                    $"Number series not configured for '{entityName}' in tenant '{tenantId}'.");

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
        /// Previews the next number without incrementing (uses TenantContext)
        /// </summary>
        public async Task<string> PreviewAsync(string entityName)
        {
            var tenantId = _tenantContext.TenantId;
            return await PreviewAsync(entityName, (Guid)tenantId);
        }

        /// <summary>
        /// Previews the next number without incrementing (explicit tenantId)
        /// </summary>
        public async Task<string> PreviewAsync(string entityName, Guid tenantId)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be empty", nameof(entityName));

            if (tenantId == Guid.Empty)
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

            var series = await GetByEntityAsync(entityName, tenantId, false);

            if (series == null)
                throw new InvalidOperationException(
                    $"Number series not configured for '{entityName}' in tenant '{tenantId}'. Please contact system administrator.");

            var nextNumber = series.LastNumber + 1;
            if (series.ResetEveryYear && series.LastGeneratedYear != DateTime.UtcNow.Year)
                nextNumber = 1;

            return Format(series, nextNumber);
        }

        /// <summary>
        /// Creates a new number series configuration (uses TenantContext)
        /// EF Core handles the transaction automatically with SaveAsync
        /// FIXED: Removed explicit transaction to work with retry strategy
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

            // Check if series already exists
            var existing = await GetByEntityAsync(entityName, true);
            if (existing != null)
                throw new InvalidOperationException($"Number series for '{entityName}' already exists.");

            var tenantId = _tenantContext.TenantId;

            var series = new DocumentNumberSeries
            {
                Id = Guid.NewGuid(),
                TenantId = (Guid)tenantId,
                EntityName = entityName,
                Prefix = prefix.ToUpperInvariant(),
                LastNumber = 0,
                LastGeneratedYear = DateTime.UtcNow.Year,
                Padding = padding,
                ResetEveryYear = resetEveryYear,
                Description = description,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow,
                Status = EntityStatus.Active
            };

            Create(series);
            await _repository.SaveAsync();

            return series;
        }

        /// <summary>
        /// Creates a new number series configuration for a specific tenant (explicit tenantId)
        /// Use this when SuperAdmin creates series for a specific school
        /// EF Core handles the transaction automatically with SaveAsync
        /// </summary>
        public async Task<DocumentNumberSeries> CreateSeriesAsync(
            string entityName,
            Guid tenantId,
            string prefix,
            int padding = 5,
            bool resetEveryYear = false,
            string? description = null)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be empty", nameof(entityName));
            if (tenantId == Guid.Empty)
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Prefix cannot be empty", nameof(prefix));
            if (padding < 1 || padding > 10)
                throw new ArgumentException("Padding must be between 1 and 10", nameof(padding));

            // Check if series already exists for this tenant
            var existing = await GetByEntityAsync(entityName, tenantId, true);
            if (existing != null)
                throw new InvalidOperationException($"Number series for '{entityName}' already exists in tenant '{tenantId}'.");

            var series = new DocumentNumberSeries
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                EntityName = entityName,
                Prefix = prefix.ToUpperInvariant(),
                LastNumber = 0,
                LastGeneratedYear = DateTime.UtcNow.Year,
                Padding = padding,
                ResetEveryYear = resetEveryYear,
                Description = description,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow,
                Status = EntityStatus.Active
            };

            Create(series);
            await _repository.SaveAsync();

            return series;
        }

        /// <summary>
        /// Resets a series counter (Admin only)
        /// EF Core handles the transaction automatically with SaveAsync
        /// FIXED: Removed explicit transaction to work with retry strategy
        /// </summary>
        public async Task ResetSeriesAsync(string entityName, int? startFrom = null)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be empty", nameof(entityName));

            var series = await GetByEntityAsync(entityName, true);
            if (series == null)
                throw new InvalidOperationException($"Number series for '{entityName}' not found.");

            series.LastNumber = startFrom ?? 0;
            series.LastGeneratedYear = DateTime.UtcNow.Year;
            series.UpdatedOn = DateTime.UtcNow;

            Update(series);
            await _repository.SaveAsync();
        }

        /// <summary>
        /// Checks if a series exists for an entity (uses TenantContext)
        /// </summary>
        public async Task<bool> SeriesExistsAsync(string entityName)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be empty", nameof(entityName));

            var series = await GetByEntityAsync(entityName, false);
            return series != null;
        }

        /// <summary>
        /// Checks if a series exists for an entity (explicit tenantId)
        /// </summary>
        public async Task<bool> SeriesExistsAsync(string entityName, Guid tenantId)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be empty", nameof(entityName));

            if (tenantId == Guid.Empty)
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

            var series = await GetByEntityAsync(entityName, tenantId, false);
            return series != null;
        }

        #endregion

        #region Additional Helper Methods

        /// <summary>
        /// Bulk generate multiple numbers (for imports)
        /// Uses execution strategy to handle retries properly
        /// </summary>
        public async Task<List<string>> GenerateBulkAsync(string entityName, int count)
        {
            if (count <= 0 || count > 1000)
                throw new ArgumentException("Count must be between 1 and 1000", nameof(count));

            var numbers = new List<string>();

            // Use execution strategy for bulk operations
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                // Begin explicit transaction within execution strategy
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    for (int i = 0; i < count; i++)
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

                        numbers.Add(Format(series));
                    }

                    await _repository.SaveAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            return numbers;
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