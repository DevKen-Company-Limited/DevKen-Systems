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
    }
}
