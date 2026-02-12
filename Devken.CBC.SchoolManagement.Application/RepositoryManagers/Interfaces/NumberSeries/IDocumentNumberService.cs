using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.NumberSeries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.NumberSeries
{
    /// <summary>
    /// Repository for handling tenant-specific document number series.
    /// Extends the generic IRepositoryBase for CRUD.
    /// </summary>
    public interface IDocumentNumberSeriesRepository
        : IRepositoryBase<DocumentNumberSeries, Guid>
    {
        /// <summary>
        /// Get a series by entity name for a specific tenant.
        /// </summary>
        Task<DocumentNumberSeries?> GetByEntityAsync(string entityName, bool trackChanges);

        /// <summary>
        /// Get all series for a tenant (optional helper for UI).
        /// </summary>
        Task<IEnumerable<DocumentNumberSeries>> GetByTenantAsync(Guid tenantId);

        /// <summary>
        /// Generates the next document number for a given entity
        /// Increments the stored last number and saves changes
        /// </summary>
        Task<string> GenerateAsync(string entityName);

        /// <summary>
        /// Previews the next number without incrementing
        /// </summary>
        Task<string> PreviewAsync(string entityName);

        /// <summary>
        /// Creates a new number series configuration
        /// </summary>
        Task<DocumentNumberSeries> CreateSeriesAsync(string entityName, string prefix, int padding = 5, bool resetEveryYear = false, string? description = null);

        /// <summary>
        /// Resets a series counter (Admin only)
        /// </summary>
        Task ResetSeriesAsync(string entityName, int? startFrom = null);

        /// <summary>
        /// Checks if a series exists for an entity
        /// </summary>
        Task<bool> SeriesExistsAsync(string entityName);
    }
}