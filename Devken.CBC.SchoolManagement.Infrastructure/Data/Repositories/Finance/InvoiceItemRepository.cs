using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Finance;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Finance
{
    /// <summary>
    /// Concrete repository for InvoiceItem.
    /// Inherits all generic CRUD from RepositoryBase.
    /// </summary>
    public class InvoiceItemRepository
        : RepositoryBase<InvoiceItem, Guid>, IInvoiceItemRepository
    {
        public InvoiceItemRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        /// <inheritdoc/>
        public async Task<IEnumerable<InvoiceItem>> GetByInvoiceAsync(
            Guid invoiceId, bool trackChanges = false)
        {
            return await FindByCondition(
                    x => x.InvoiceId == invoiceId,
                    trackChanges)
                .Include(x => x.FeeItem)
                .OrderBy(x => x.CreatedOn)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<InvoiceItem?> GetDetailAsync(Guid id, bool trackChanges = false)
        {
            return await FindByCondition(x => x.Id == id, trackChanges)
                .Include(x => x.FeeItem)
                .Include(x => x.Term)
                .FirstOrDefaultAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<InvoiceItem>> GetByTenantAsync(
            Guid tenantId, bool trackChanges = false)
        {
            return await FindByCondition(
                    x => x.TenantId == tenantId,
                    trackChanges)
                .Include(x => x.FeeItem)
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();
        }
    }
}