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
    public class SubStrandRepository : RepositoryBase<SubStrand, Guid>, ISubStrandRepository
    {
        public SubStrandRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext)
        {
        }

        public async Task<IEnumerable<SubStrand>> GetAllAsync(bool trackChanges) =>
            await FindAll(trackChanges)
                .Include(ss => ss.Strand)
                    .ThenInclude(s => s.LearningArea)
                .ToListAsync();

        public async Task<IEnumerable<SubStrand>> GetByTenantIdAsync(Guid tenantId, bool trackChanges) =>
            await FindByCondition(ss => ss.TenantId == tenantId, trackChanges)
                .Include(ss => ss.Strand)
                    .ThenInclude(s => s.LearningArea)
                .ToListAsync();

        public async Task<IEnumerable<SubStrand>> GetByStrandAsync(Guid strandId, bool trackChanges) =>
            await FindByCondition(ss => ss.StrandId == strandId, trackChanges)
                .Include(ss => ss.Strand)
                    .ThenInclude(s => s.LearningArea)
                .ToListAsync();

        public async Task<bool> ExistsByNameAsync(string name, Guid strandId, Guid? excludeId = null) =>
            await FindByCondition(
                    ss => ss.Name.ToLower() == name.ToLower() &&
                          ss.StrandId == strandId &&
                          (excludeId == null || ss.Id != excludeId),
                    trackChanges: false)
                .AnyAsync();

        public async Task<SubStrand?> GetByIdWithDetailsAsync(Guid id, bool trackChanges) =>
            await FindByCondition(ss => ss.Id == id, trackChanges)
                .Include(ss => ss.Strand)
                    .ThenInclude(s => s.LearningArea)
                .Include(ss => ss.LearningOutcomes)
                .FirstOrDefaultAsync();
    }
}
