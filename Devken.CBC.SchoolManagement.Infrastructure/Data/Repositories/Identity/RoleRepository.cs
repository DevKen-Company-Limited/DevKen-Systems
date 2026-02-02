using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Identity;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Enums;
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
    internal class RoleRepository
           : RepositoryBase<Role, Guid>, IRoleRepository
    {
        public RoleRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        public async Task<Role?> GetByNameAndTenantAsync(string name, Guid tenantId) =>
            await _context.Roles
                .FirstOrDefaultAsync(r =>
                    r.Name == name &&
                    r.TenantId == tenantId &&
                    r.Status != EntityStatus.Deleted);

        public async Task<Role?> GetWithPermissionsAsync(Guid roleId) =>
            await _context.Roles
                .Include(r => r.RolePermissions)
                    .Include(rp => rp.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == roleId);
    }

}
