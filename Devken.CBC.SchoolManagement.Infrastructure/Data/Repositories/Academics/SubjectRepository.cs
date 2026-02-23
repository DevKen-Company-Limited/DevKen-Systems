using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academics;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Academics
{
    public class SubjectRepository : RepositoryBase<Subject, Guid>, ISubjectRepository
    {
        public SubjectRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext)
        {
        }

        public async Task<IEnumerable<Subject>> GetAllAsync(bool trackChanges) =>
            await FindAll(trackChanges)
                .ToListAsync();

        public async Task<IEnumerable<Subject>> GetByTenantIdAsync(Guid tenantId, bool trackChanges) =>
            await FindByCondition(s => s.TenantId == tenantId, trackChanges)
                .ToListAsync();

        public async Task<Subject?> GetByCodeAsync(string code, Guid tenantId) =>
            await FindByCondition(
                    s => s.Code == code && s.TenantId == tenantId,
                    trackChanges: false)
                .FirstOrDefaultAsync();

        public async Task<bool> ExistsByNameAsync(string name, Guid tenantId, Guid? excludeId = null) =>
            await FindByCondition(
                    s => s.Name.ToLower() == name.ToLower() &&
                         s.TenantId == tenantId &&
                         (excludeId == null || s.Id != excludeId),
                    trackChanges: false)
                .AnyAsync();

        public async Task<Subject?> GetByIdWithDetailsAsync(Guid id, bool trackChanges) =>
            await FindByCondition(s => s.Id == id, trackChanges)
                .Include(s => s.Classes)
                .Include(s => s.Teachers)
                .Include(s => s.Grades)
                .FirstOrDefaultAsync();
    }
}
