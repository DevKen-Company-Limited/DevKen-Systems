using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academics
{
    public interface IUserRepository : IRepositoryBase<User, Guid>
    {
        Task<User?> GetByEmailAsync(string email, Guid tenantId);
        Task<User?> GetByEmailWithRolesAsync(string email, Guid tenantId);
        Task<bool> EmailExistsAsync(string email, Guid tenantId);
    }
}
