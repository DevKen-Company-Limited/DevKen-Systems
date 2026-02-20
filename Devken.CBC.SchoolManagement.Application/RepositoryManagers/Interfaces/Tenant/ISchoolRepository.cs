using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Tenant
{
    public interface ISchoolRepository : IRepositoryBase<School, Guid>
    {
        Task<School?> GetBySlugAsync(string slugName);

        Task<IEnumerable<School>> GetAllAsync(bool trackChanges = false);
        Task<IEnumerable<School>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges = false);
    }
}