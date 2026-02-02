using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Identity
{
    public interface IRolePermissionRepository
        : IRepositoryBase<RolePermission, Guid>
    {
        Task<IEnumerable<RolePermission>> GetByRoleIdAsync(Guid roleId);
        void RemoveByRoleIdAndPermissionId(Guid roleId, Guid permissionId);
    }
}
