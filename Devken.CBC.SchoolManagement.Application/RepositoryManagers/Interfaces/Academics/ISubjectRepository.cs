using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academics
{
    public interface ISubjectRepository : IRepositoryBase<Subject, Guid>
    {
        Task<IEnumerable<Subject>> GetAllAsync(bool trackChanges);
        Task<IEnumerable<Subject>> GetByTenantIdAsync(Guid tenantId, bool trackChanges);
        Task<Subject?> GetByCodeAsync(string code, Guid tenantId);
        Task<bool> ExistsByNameAsync(string name, Guid tenantId, Guid? excludeId = null);
        Task<Subject?> GetByIdWithDetailsAsync(Guid id, bool trackChanges);
    }
}
