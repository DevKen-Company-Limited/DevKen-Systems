using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Identity
{
    public interface IPermissionRepository
        : IRepositoryBase<Permission, Guid>
    {
        Task<Permission?> GetByKeyAsync(string key);
        Task<IEnumerable<Permission>> GetByGroupAsync(string groupName);
    }
}
