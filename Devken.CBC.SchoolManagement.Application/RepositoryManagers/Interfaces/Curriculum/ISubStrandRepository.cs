using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Curriculum
{
    public interface ISubStrandRepository : IRepositoryBase<SubStrand, Guid>
    {
        Task<IEnumerable<SubStrand>> GetAllAsync(bool trackChanges);
        Task<IEnumerable<SubStrand>> GetByTenantIdAsync(Guid tenantId, bool trackChanges);
        Task<IEnumerable<SubStrand>> GetByStrandAsync(Guid strandId, bool trackChanges);
        Task<bool> ExistsByNameAsync(string name, Guid strandId, Guid? excludeId = null);
        Task<SubStrand?> GetByIdWithDetailsAsync(Guid id, bool trackChanges);
    }
}