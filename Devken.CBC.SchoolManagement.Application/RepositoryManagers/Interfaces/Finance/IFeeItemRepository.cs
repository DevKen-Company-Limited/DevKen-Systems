using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Finance
{
    public interface IFeeItemRepository : IRepositoryBase<FeeItem, Guid>
    {
        Task<IEnumerable<FeeItem>> GetAllAsync(bool trackChanges);
        Task<IEnumerable<FeeItem>> GetByTenantIdAsync(Guid tenantId, bool trackChanges);
        Task<FeeItem?> GetByCodeAsync(string code, Guid tenantId);
        Task<bool> ExistsByNameAsync(string name, Guid tenantId, Guid? excludeId = null);
        Task<FeeItem?> GetByIdWithDetailsAsync(Guid id, bool trackChanges);
    }
}