using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Identity
{
    public interface IUserRepository
        : IRepositoryBase<User, Guid>
    {
        /// <summary>Find by email WITHIN a specific tenant.</summary>
        Task<User?> GetByEmailAndTenantAsync(string email, Guid tenantId);

        /// <summary>
        /// Eager-load a user with roles → permissions for JWT claim building.
        /// </summary>
        Task<User?> GetWithRolesAndPermissionsAsync(Guid userId, Guid tenantId);

        /// <summary>Check whether an email is already taken in a tenant.</summary>
        Task<bool> EmailExistsInTenantAsync(string email, Guid tenantId);
    }
}
