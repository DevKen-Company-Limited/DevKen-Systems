using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.NumberSeries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.NumberSeries
{
    /// <summary>
    /// Repository interface for managing document number series
    /// Supports both tenant-context-based and explicit tenant ID operations
    /// </summary>
    public interface IDocumentNumberSeriesRepository
        : IRepositoryBase<DocumentNumberSeries, Guid>
    {
        // ─────────────────────────────────────────────────────────────────────
        // QUERY METHODS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Gets a series for the current tenant by entity name (uses TenantContext)
        /// </summary>
        Task<DocumentNumberSeries?> GetByEntityAsync(string entityName, bool trackChanges);

        /// <summary>
        /// Gets a series for a specific tenant by entity name (explicit tenantId)
        /// </summary>
        Task<DocumentNumberSeries?> GetByEntityAsync(string entityName, Guid tenantId, bool trackChanges);

        /// <summary>
        /// Gets all series for a specific tenant
        /// </summary>
        Task<IEnumerable<DocumentNumberSeries>> GetByTenantAsync(Guid tenantId);

        /// <summary>
        /// Checks if a series exists for an entity (uses TenantContext)
        /// </summary>
        Task<bool> SeriesExistsAsync(string entityName);

        /// <summary>
        /// Checks if a series exists for an entity (explicit tenantId)
        /// </summary>
        Task<bool> SeriesExistsAsync(string entityName, Guid tenantId);

        // ─────────────────────────────────────────────────────────────────────
        // NUMBER GENERATION
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Generates the next document number for a given entity (uses TenantContext)
        /// </summary>
        Task<string> GenerateAsync(string entityName);

        /// <summary>
        /// Generates the next document number for a given entity (explicit tenantId)
        /// </summary>
        Task<string> GenerateAsync(string entityName, Guid tenantId);

        /// <summary>
        /// Previews the next number without incrementing (uses TenantContext)
        /// </summary>
        Task<string> PreviewAsync(string entityName);

        /// <summary>
        /// Previews the next number without incrementing (explicit tenantId)
        /// </summary>
        Task<string> PreviewAsync(string entityName, Guid tenantId);

        /// <summary>
        /// Bulk generate multiple numbers (for imports)
        /// </summary>
        Task<List<string>> GenerateBulkAsync(string entityName, int count);

        /// <summary>
        /// Gets the current number without incrementing
        /// </summary>
        Task<string> GetCurrentAsync(string entityName);

        // ─────────────────────────────────────────────────────────────────────
        // SERIES MANAGEMENT
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new number series configuration (uses TenantContext)
        /// </summary>
        Task<DocumentNumberSeries> CreateSeriesAsync(
            string entityName,
            string prefix,
            int padding = 5,
            bool resetEveryYear = false,
            string? description = null);

        /// <summary>
        /// Creates a new number series configuration for a specific tenant (explicit tenantId)
        /// Use this when SuperAdmin creates series for a specific school
        /// </summary>
        Task<DocumentNumberSeries> CreateSeriesAsync(
            string entityName,
            Guid tenantId,
            string prefix,
            int padding = 5,
            bool resetEveryYear = false,
            string? description = null);

        /// <summary>
        /// Resets a series counter (Admin only)
        /// </summary>
        Task ResetSeriesAsync(string entityName, int? startFrom = null);

        /// <summary>
        /// Gets statistics for all series in current tenant
        /// </summary>
        Task<Dictionary<string, object>> GetStatisticsAsync();
    }
}