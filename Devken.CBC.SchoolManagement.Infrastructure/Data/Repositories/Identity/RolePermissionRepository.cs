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
    internal class RolePermissionRepository
        : RepositoryBase<RolePermission, Guid>, IRolePermissionRepository
    {
        public RolePermissionRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        public async Task<IEnumerable<RolePermission>> GetByRoleIdAsync(Guid roleId) =>
            await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

        public void RemoveByRoleIdAndPermissionId(Guid roleId, Guid permissionId)
        {
            var entity = _context.RolePermissions
                .FirstOrDefault(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
            if (entity != null)
                _context.RolePermissions.Remove(entity);
        }
    }
}
