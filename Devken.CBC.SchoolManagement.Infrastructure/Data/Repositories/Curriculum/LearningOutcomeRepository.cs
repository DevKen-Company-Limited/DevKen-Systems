using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Curriculum;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Curriculum
{
    public class LearningOutcomeRepository : RepositoryBase<LearningOutcome, Guid>, ILearningOutcomeRepository
    {
        public LearningOutcomeRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext)
        {
        }

        public async Task<IEnumerable<LearningOutcome>> GetAllAsync(bool trackChanges) =>
            await FindAll(trackChanges)
                .Include(lo => lo.LearningArea)
                .Include(lo => lo.Strand)
                .Include(lo => lo.SubStrand)
                .ToListAsync();

        public async Task<IEnumerable<LearningOutcome>> GetByTenantIdAsync(Guid tenantId, bool trackChanges) =>
            await FindByCondition(lo => lo.TenantId == tenantId, trackChanges)
                .Include(lo => lo.LearningArea)
                .Include(lo => lo.Strand)
                .Include(lo => lo.SubStrand)
                .ToListAsync();

        public async Task<IEnumerable<LearningOutcome>> GetBySubStrandAsync(Guid subStrandId, bool trackChanges) =>
            await FindByCondition(lo => lo.SubStrandId == subStrandId, trackChanges)
                .Include(lo => lo.LearningArea)
                .Include(lo => lo.Strand)
                .Include(lo => lo.SubStrand)
                .ToListAsync();

        public async Task<IEnumerable<LearningOutcome>> GetByStrandAsync(Guid strandId, bool trackChanges) =>
            await FindByCondition(lo => lo.StrandId == strandId, trackChanges)
                .Include(lo => lo.LearningArea)
                .Include(lo => lo.Strand)
                .Include(lo => lo.SubStrand)
                .ToListAsync();

        public async Task<IEnumerable<LearningOutcome>> GetByLearningAreaAsync(Guid learningAreaId, bool trackChanges) =>
            await FindByCondition(lo => lo.LearningAreaId == learningAreaId, trackChanges)
                .Include(lo => lo.LearningArea)
                .Include(lo => lo.Strand)
                .Include(lo => lo.SubStrand)
                .ToListAsync();

        public async Task<LearningOutcome?> GetByCodeAsync(string code, Guid tenantId) =>
            await FindByCondition(
                    lo => lo.Code == code && lo.TenantId == tenantId,
                    trackChanges: false)
                .Include(lo => lo.LearningArea)
                .Include(lo => lo.Strand)
                .Include(lo => lo.SubStrand)
                .FirstOrDefaultAsync();

        public async Task<bool> ExistsByCodeAsync(string code, Guid tenantId, Guid? excludeId = null) =>
            await FindByCondition(
                    lo => lo.Code == code &&
                          lo.TenantId == tenantId &&
                          (excludeId == null || lo.Id != excludeId),
                    trackChanges: false)
                .AnyAsync();

        public async Task<LearningOutcome?> GetByIdWithDetailsAsync(Guid id, bool trackChanges) =>
            await FindByCondition(lo => lo.Id == id, trackChanges)
                .Include(lo => lo.LearningArea)
                .Include(lo => lo.Strand)
                .Include(lo => lo.SubStrand)
                .FirstOrDefaultAsync();
    }
}
