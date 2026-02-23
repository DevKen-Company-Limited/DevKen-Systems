using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Curriculum
{
    public interface ILearningAreaRepository : IRepositoryBase<LearningArea, Guid>
    {
        Task<IEnumerable<LearningArea>> GetAllAsync(bool trackChanges);
        Task<IEnumerable<LearningArea>> GetByTenantIdAsync(Guid tenantId, bool trackChanges);
        Task<LearningArea?> GetByCodeAsync(string code, Guid tenantId);
        Task<bool> ExistsByNameAsync(string name, Guid tenantId, Guid? excludeId = null);
        Task<LearningArea?> GetByIdWithDetailsAsync(Guid id, bool trackChanges);
    }
}
