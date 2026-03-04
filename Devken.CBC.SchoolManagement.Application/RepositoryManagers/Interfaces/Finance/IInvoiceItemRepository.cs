using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Finance
{
    /// <summary>
    /// Repository contract for InvoiceItem.
    /// Inherits generic CRUD from IRepositoryBase.
    /// </summary>
    public interface IInvoiceItemRepository : IRepositoryBase<InvoiceItem, Guid>
    {
        /// <summary>Returns all items belonging to a specific invoice.</summary>
        Task<IEnumerable<InvoiceItem>> GetByInvoiceAsync(Guid invoiceId, bool trackChanges = false);

        /// <summary>Returns a single item by ID with optional navigation includes.</summary>
        Task<InvoiceItem?> GetDetailAsync(Guid id, bool trackChanges = false);

        /// <summary>Returns all items across all invoices for a given tenant.</summary>
        Task<IEnumerable<InvoiceItem>> GetByTenantAsync(Guid tenantId, bool trackChanges = false);
    }
}