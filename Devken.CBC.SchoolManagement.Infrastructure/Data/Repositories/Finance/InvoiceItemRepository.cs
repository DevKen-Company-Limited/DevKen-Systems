using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Finance;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Finance
{
    public class InvoiceItemRepository
        : RepositoryBase<InvoiceItem, Guid>, IInvoiceItemRepository
    {
        public InvoiceItemRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        /// <summary>
        /// Returns all invoice items across all tenants (SuperAdmin only).
        /// Ordered by CreatedOn descending — consistent with GetByTenantAsync
        /// and what the service layer expects.
        /// </summary>
        public async Task<IEnumerable<InvoiceItem>> GetAllAsync(bool trackChanges) =>
            await FindAll(trackChanges)
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();

        public async Task<InvoiceItem?> GetByCodeAsync(string code, Guid tenantId) =>
            await FindByCondition(
                    x => x.GlCode == code && x.TenantId == tenantId,
                    trackChanges: false)
                .FirstOrDefaultAsync();

        public async Task<bool> ExistsByNameAsync(
            string name, Guid tenantId, Guid? excludeId = null) =>
            await FindByCondition(
                    x => x.Description.ToLower() == name.ToLower() &&
                         x.TenantId == tenantId &&
                         (excludeId == null || x.Id != excludeId),
                    trackChanges: false)
                .AnyAsync();

        public async Task<IEnumerable<InvoiceItem>> GetByInvoiceAsync(
            Guid invoiceId, bool trackChanges = false) =>
            await FindByCondition(x => x.InvoiceId == invoiceId, trackChanges)
                .Include(x => x.FeeItem)
                .OrderBy(x => x.CreatedOn)
                .ToListAsync();

        public async Task<InvoiceItem?> GetDetailAsync(Guid id, bool trackChanges = false) =>
            await FindByCondition(x => x.Id == id, trackChanges)
                .Include(x => x.FeeItem)
                .Include(x => x.Term)
                .FirstOrDefaultAsync();

        public async Task<IEnumerable<InvoiceItem>> GetByTenantAsync(
            Guid tenantId, bool trackChanges = false) =>
            await FindByCondition(x => x.TenantId == tenantId, trackChanges)
                .Include(x => x.FeeItem)
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();
    }
}