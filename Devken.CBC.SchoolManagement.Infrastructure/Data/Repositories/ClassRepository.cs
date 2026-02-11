using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academics;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories
{
    public class ClassRepository : RepositoryBase<Class, Guid>, IClassRepository
    {
        public ClassRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext)
        {
        }

        public async Task<IEnumerable<Class>> GetAllByTenantAsync(Guid tenantId, bool trackChanges = false)
        {
            return await FindByCondition(
                c => c.TenantId == tenantId,
                trackChanges)
                .Include(c => c.ClassTeacher)
                .Include(c => c.AcademicYear)
                .OrderBy(c => c.Level)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Class>> GetByAcademicYearAsync(Guid tenantId, Guid academicYearId, bool trackChanges = false)
        {
            return await FindByCondition(
                c => c.TenantId == tenantId && c.AcademicYearId == academicYearId,
                trackChanges)
                .Include(c => c.ClassTeacher)
                .OrderBy(c => c.Level)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Class?> GetByCodeAsync(Guid tenantId, string code, bool trackChanges = false)
        {
            return await FindByCondition(
                c => c.TenantId == tenantId && c.Code == code,
                trackChanges)
                .Include(c => c.ClassTeacher)
                .Include(c => c.AcademicYear)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> CodeExistsAsync(Guid tenantId, string code, Guid? excludeId = null)
        {
            var query = FindByCondition(
                c => c.TenantId == tenantId && c.Code == code,
                trackChanges: false);

            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<IEnumerable<Class>> GetByLevelAsync(Guid tenantId, CBCLevel level, bool trackChanges = false)
        {
            return await FindByCondition(
                c => c.TenantId == tenantId && c.Level == level,
                trackChanges)
                .Include(c => c.ClassTeacher)
                .Include(c => c.AcademicYear)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Class>> GetActiveClassesAsync(Guid tenantId, bool trackChanges = false)
        {
            return await FindByCondition(
                c => c.TenantId == tenantId && c.IsActive,
                trackChanges)
                .Include(c => c.ClassTeacher)
                .Include(c => c.AcademicYear)
                .OrderBy(c => c.Level)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Class>> GetByTeacherAsync(Guid tenantId, Guid teacherId, bool trackChanges = false)
        {
            return await FindByCondition(
                c => c.TenantId == tenantId && c.TeacherId == teacherId,
                trackChanges)
                .Include(c => c.AcademicYear)
                .OrderBy(c => c.Level)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Class?> GetWithStudentsAsync(Guid id, bool trackChanges = false)
        {
            var query = FindByCondition(c => c.Id == id, trackChanges)
                .Include(c => c.Students)
                .Include(c => c.ClassTeacher)
                .Include(c => c.AcademicYear);

            return await query.FirstOrDefaultAsync();
        }

        public async Task<Class?> GetWithDetailsAsync(Guid id, bool trackChanges = false)
        {
            var query = FindByCondition(c => c.Id == id, trackChanges)
                .Include(c => c.Students)
                .Include(c => c.Subjects)
                .Include(c => c.ClassTeacher)
                .Include(c => c.AcademicYear);

            return await query.FirstOrDefaultAsync();
        }

        public async Task<bool> HasAvailableSeatsAsync(Guid classId)
        {
            var classEntity = await FindByCondition(
                c => c.Id == classId,
                trackChanges: false)
                .FirstOrDefaultAsync();

            if (classEntity == null)
                return false;

            return classEntity.CurrentEnrollment < classEntity.Capacity;
        }

        public async Task UpdateEnrollmentAsync(Guid classId, int newEnrollment)
        {
            var classEntity = await FindByCondition(
                c => c.Id == classId,
                trackChanges: true)
                .FirstOrDefaultAsync();

            if (classEntity != null)
            {
                classEntity.CurrentEnrollment = newEnrollment;
                Update(classEntity);
            }
        }
    }
}
