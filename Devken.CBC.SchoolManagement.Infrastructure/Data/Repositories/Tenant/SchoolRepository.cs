using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Tenant;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Tenant
{
    public class SchoolRepository : RepositoryBase<School, Guid>, ISchoolRepository
    {
        public SchoolRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext)
        {
        }

        public async Task<School?> GetBySlugAsync(string slugName)
        {
            if (string.IsNullOrWhiteSpace(slugName))
                return null;

            // Normalize search to avoid case mismatches
            var normalized = slugName.Trim();

            return await FindByCondition(
                    s => s.SlugName == normalized,
                    trackChanges: false)
                .FirstOrDefaultAsync();
        }

        public async Task<System.Collections.Generic.IEnumerable<School>> GetAllAsync(bool trackChanges = false)
        {
            return await FindAll(trackChanges)
                .ToListAsync();
        }

        public async Task<IEnumerable<School>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges = false)
        {
            if (ids == null || !ids.Any())
                return Enumerable.Empty<School>();

            return await FindByCondition(s => ids.Contains(s.Id), trackChanges)
                .ToListAsync();
        }

    }
}