using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academics;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Academic
{
    public class TeacherRepository : RepositoryBase<Teacher, Guid>, ITeacherRepository
    {
        public TeacherRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        public async Task<IEnumerable<Teacher>> GetAllAsync(bool trackChanges) =>
            await FindAll(trackChanges)
                .Include(t => t.CurrentClass)
                .ToListAsync();

        public async Task<IEnumerable<Teacher>> GetBySchoolIdAsync(Guid schoolId, bool trackChanges) =>
            await FindByCondition(t => t.TenantId == schoolId, trackChanges)
                .Include(t => t.CurrentClass)
                .ToListAsync();

        public async Task<Teacher?> GetByTeacherNumberAsync(string teacherNumber, Guid schoolId) =>
            await FindByCondition(
                    t => t.TeacherNumber == teacherNumber && t.TenantId == schoolId,
                    trackChanges: false)
                .FirstOrDefaultAsync();

        public async Task<Teacher?> GetByIdWithDetailsAsync(Guid id, bool trackChanges) =>
            await FindByCondition(t => t.Id == id, trackChanges)
                .Include(t => t.CurrentClass)
                .Include(t => t.Subjects)
                .Include(t => t.CBCLevels)
                .FirstOrDefaultAsync();
    }
}