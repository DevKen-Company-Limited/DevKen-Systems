using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Curriculum
{
    public interface IStrandRepository : IRepositoryBase<Strand, Guid>
    {
        Task<IEnumerable<Strand>> GetAllAsync(bool trackChanges);
        Task<IEnumerable<Strand>> GetByTenantIdAsync(Guid tenantId, bool trackChanges);
        Task<IEnumerable<Strand>> GetByLearningAreaAsync(Guid learningAreaId, bool trackChanges);
        Task<bool> ExistsByNameAsync(string name, Guid learningAreaId, Guid? excludeId = null);
        Task<Strand?> GetByIdWithDetailsAsync(Guid id, bool trackChanges);
    }
}