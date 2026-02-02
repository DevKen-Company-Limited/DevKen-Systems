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
    internal class PermissionRepository
            : RepositoryBase<Permission, Guid>, IPermissionRepository
    {
        public PermissionRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        public async Task<Permission?> GetByKeyAsync(string key) =>
            await _context.Permissions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Key == key);

        public async Task<IEnumerable<Permission>> GetByGroupAsync(string groupName) =>
            await _context.Permissions
                .AsNoTracking()
                .Where(p => p.GroupName == groupName)
                .ToListAsync();
    }
}
