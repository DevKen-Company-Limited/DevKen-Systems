using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academics;
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
    public class StrandRepository : RepositoryBase<Strand, Guid>, IStrandRepository
    {
        public StrandRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext)
        {
        }

        public async Task<IEnumerable<Strand>> GetAllAsync(bool trackChanges) =>
            await FindAll(trackChanges)
                .Include(s => s.LearningArea)
                .ToListAsync();

        public async Task<IEnumerable<Strand>> GetByTenantIdAsync(Guid tenantId, bool trackChanges) =>
            await FindByCondition(s => s.TenantId == tenantId, trackChanges)
                .Include(s => s.LearningArea)
                .ToListAsync();

        public async Task<IEnumerable<Strand>> GetByLearningAreaAsync(Guid learningAreaId, bool trackChanges) =>
            await FindByCondition(s => s.LearningAreaId == learningAreaId, trackChanges)
                .Include(s => s.LearningArea)
                .ToListAsync();

        public async Task<bool> ExistsByNameAsync(string name, Guid learningAreaId, Guid? excludeId = null) =>
            await FindByCondition(
                    s => s.Name.ToLower() == name.ToLower() &&
                         s.LearningAreaId == learningAreaId &&
                         (excludeId == null || s.Id != excludeId),
                    trackChanges: false)
                .AnyAsync();

        public async Task<Strand?> GetByIdWithDetailsAsync(Guid id, bool trackChanges) =>
            await FindByCondition(s => s.Id == id, trackChanges)
                .Include(s => s.LearningArea)
                .Include(s => s.SubStrands)
                .FirstOrDefaultAsync();
    }
}