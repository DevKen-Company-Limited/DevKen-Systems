using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Finance;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Finance
{
    public class FeeItemRepository : RepositoryBase<FeeItem, Guid>, IFeeItemRepository
    {
        public FeeItemRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext)
        {
        }

        /// <summary>
        /// Returns all fee items across all tenants (SuperAdmin only).
        /// School name is resolved separately in the service layer.
        /// </summary>
        public async Task<IEnumerable<FeeItem>> GetAllAsync(bool trackChanges) =>
            await FindAll(trackChanges)
                .OrderBy(f => f.Name)
                .ToListAsync();

        /// <summary>
        /// Returns all fee items belonging to a specific tenant/school.
        /// </summary>
        public async Task<IEnumerable<FeeItem>> GetByTenantIdAsync(
            Guid tenantId, bool trackChanges) =>
            await FindByCondition(f => f.TenantId == tenantId, trackChanges)
                .OrderBy(f => f.Name)
                .ToListAsync();

        /// <summary>
        /// Finds a fee item by its auto-generated code within a specific tenant.
        /// </summary>
        public async Task<FeeItem?> GetByCodeAsync(string code, Guid tenantId) =>
            await FindByCondition(
                    f => f.Code == code && f.TenantId == tenantId,
                    trackChanges: false)
                .FirstOrDefaultAsync();

        /// <summary>
        /// Checks whether a fee item with the given name already exists for a tenant,
        /// optionally excluding a specific fee item ID (used during update).
        /// </summary>
        public async Task<bool> ExistsByNameAsync(
            string name, Guid tenantId, Guid? excludeId = null) =>
            await FindByCondition(
                    f => f.Name.ToLower() == name.ToLower() &&
                         f.TenantId == tenantId &&
                         (excludeId == null || f.Id != excludeId),
                    trackChanges: false)
                .AnyAsync();

        /// <summary>
        /// Returns a fee item by ID with its related InvoiceItems eagerly loaded.
        /// School name is NOT included here — resolved separately via IRepositoryManager.School.
        /// </summary>
        public async Task<FeeItem?> GetByIdWithDetailsAsync(Guid id, bool trackChanges) =>
            await FindByCondition(f => f.Id == id, trackChanges)
                .Include(f => f.InvoiceItems)
                .FirstOrDefaultAsync();
    }
}