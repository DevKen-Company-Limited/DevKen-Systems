using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
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

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Tenant
{
    internal class SchoolRepository
        : RepositoryBase<School, Guid>, ISchoolRepository
    {
        public SchoolRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        public async Task<School?> GetBySlugAsync(string slug) =>
            await _context.Schools
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SlugName == slug && s.Status != EntityStatus.Deleted);
    }
}
