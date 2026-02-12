using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.NumberSeries;
using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.NumberSeries;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
            _repository = repository;
        }

        /// <summary>
        /// Generates the next document number for a given entity
        /// Increments the stored last number and saves changes
        /// </summary>
        public async Task<string> GenerateAsync(string entityName)
        {
            var tenantId = _tenantContext.TenantId;

            await using var transaction = await _repository.BeginTransactionAsync();

            try
            {
                var series = await GetByEntityAsync(entityName, true);

                if (series == null)
                    throw new InvalidOperationException(
                        $"Number series not configured for {entityName}.");

                // Reset yearly if required
                if (series.ResetEveryYear && series.LastGeneratedYear != DateTime.UtcNow.Year)
                {
                    series.LastNumber = 0;
                    series.LastGeneratedYear = DateTime.UtcNow.Year;
                }

                series.LastNumber++;
                var formatted = Format(series);

                Update(series);
                await _repository.SaveAsync();
                await transaction.CommitAsync();

                return formatted;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Previews the next number without incrementing
        /// </summary>
        public async Task<string> PreviewAsync(string entityName)
        {
            var series = await GetByEntityAsync(entityName, false);

            if (series == null)
                throw new InvalidOperationException(
                    $"Number series not configured for {entityName}.");

            var nextNumber = series.LastNumber + 1;
            if (series.ResetEveryYear && series.LastGeneratedYear != DateTime.UtcNow.Year)
                nextNumber = 1;

            return Format(series, nextNumber);
        }

        /// <summary>
        /// Gets a series for the current tenant by entity name
        /// </summary>
        public async Task<DocumentNumberSeries?> GetByEntityAsync(string entityName, bool trackChanges)
        {
            return await FindByCondition(
                    x => x.EntityName == entityName && x.TenantId == _tenantContext.TenantId,
                    trackChanges)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Gets all series for a specific tenant
        /// </summary>
        public async Task<IEnumerable<DocumentNumberSeries>> GetByTenantAsync(Guid tenantId)
        {
            return await FindByCondition(x => x.TenantId == tenantId, false)
                .ToListAsync();
        }

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
    }
}
