using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Library
{
    public interface IBookPublisherRepository : IRepositoryBase<BookPublisher, Guid>
    {
        Task<IEnumerable<BookPublisher>> GetAllAsync(bool trackChanges);
        Task<IEnumerable<BookPublisher>> GetByTenantIdAsync(Guid tenantId, bool trackChanges);
        Task<bool> ExistsByNameAsync(string name, Guid tenantId, Guid? excludeId = null);
    }
}
