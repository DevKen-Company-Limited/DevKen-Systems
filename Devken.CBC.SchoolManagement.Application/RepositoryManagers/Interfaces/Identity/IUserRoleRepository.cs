using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Identity
{
    public interface IUserRoleRepository
        : IRepositoryBase<UserRole, Guid>
    {
        Task<IEnumerable<UserRole>> GetByUserIdAsync(Guid userId);
        void RemoveByUserIdAndRoleId(Guid userId, Guid roleId);
    }
}
