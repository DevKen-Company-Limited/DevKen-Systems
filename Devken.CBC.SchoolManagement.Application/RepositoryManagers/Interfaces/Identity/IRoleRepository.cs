using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Identity
{
    public interface IRoleRepository
        : IRepositoryBase<Role, Guid>
    {
        Task<Role?> GetByNameAndTenantAsync(string name, Guid tenantId);

        /// <summary>
        /// Get a role with its permissions eagerly loaded.
        /// </summary>
        Task<Role?> GetWithPermissionsAsync(Guid roleId);
    }
}
