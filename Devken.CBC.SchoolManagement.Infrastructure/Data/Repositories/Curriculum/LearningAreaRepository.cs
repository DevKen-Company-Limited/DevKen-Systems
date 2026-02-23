using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Curriculum;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Curriculum    
{
    public class LearningAreaRepository : RepositoryBase<LearningArea, Guid>, ILearningAreaRepository
    {
        public LearningAreaRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext)
        {
        }

        public async Task<IEnumerable<LearningArea>> GetAllAsync(bool trackChanges) =>
            await FindAll(trackChanges)
                .ToListAsync();

        public async Task<IEnumerable<LearningArea>> GetByTenantIdAsync(Guid tenantId, bool trackChanges) =>
            await FindByCondition(la => la.TenantId == tenantId, trackChanges)
                .ToListAsync();

        public async Task<LearningArea?> GetByCodeAsync(string code, Guid tenantId) =>
            await FindByCondition(
                    la => la.Code == code && la.TenantId == tenantId,
                    trackChanges: false)
                .FirstOrDefaultAsync();

        public async Task<bool> ExistsByNameAsync(string name, Guid tenantId, Guid? excludeId = null) =>
            await FindByCondition(
                    la => la.Name.ToLower() == name.ToLower() &&
                          la.TenantId == tenantId &&
                          (excludeId == null || la.Id != excludeId),
                    trackChanges: false)
                .AnyAsync();

        public async Task<LearningArea?> GetByIdWithDetailsAsync(Guid id, bool trackChanges) =>
            await FindByCondition(la => la.Id == id, trackChanges)
                .Include(la => la.Strands)
                    .ThenInclude(s => s.SubStrands)
                .FirstOrDefaultAsync();
    }
}