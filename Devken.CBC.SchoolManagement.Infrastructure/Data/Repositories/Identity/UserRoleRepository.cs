using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Identity;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Identity
{
    internal class UserRoleRepository
        : RepositoryBase<UserRole, Guid>, IUserRoleRepository
    {
        public UserRoleRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        public async Task<IEnumerable<UserRole>> GetByUserIdAsync(Guid userId) =>
            await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .ToListAsync();

        public void RemoveByUserIdAndRoleId(Guid userId, Guid roleId)
        {
            var entity = _context.UserRoles
                .FirstOrDefault(ur => ur.UserId == userId && ur.RoleId == roleId);
            if (entity != null)
                _context.UserRoles.Remove(entity);
        }
    }
}
