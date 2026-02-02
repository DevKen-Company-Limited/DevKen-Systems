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
    internal class SuperAdminRepository
        : RepositoryBase<SuperAdmin, Guid>, ISuperAdminRepository
    {
        public SuperAdminRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        public async Task<SuperAdmin?> GetByEmailAsync(string email) =>
            await _context.SuperAdmins
                .FirstOrDefaultAsync(sa => sa.Email == email);

        public Task<bool> AnyExistsAsync() =>
            _context.SuperAdmins.AnyAsync();
    }
}
