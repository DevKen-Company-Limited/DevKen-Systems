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
    public class TermRepository : RepositoryBase<Term, Guid>, ITermRepository
    {
        public TermRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        public async Task<IEnumerable<Term>> GetAllAsync(bool trackChanges) =>
            await FindAll(trackChanges)
                .Include(t => t.AcademicYear)
                    .ThenInclude(ay => ay.School)
                .OrderByDescending(t => t.AcademicYear.StartDate)
                .ThenBy(t => t.TermNumber)
                .ToListAsync();

        public async Task<IEnumerable<Term>> GetBySchoolIdAsync(Guid schoolId, bool trackChanges) =>
            await FindByCondition(t => t.TenantId == schoolId, trackChanges)
                .Include(t => t.AcademicYear)
                .OrderByDescending(t => t.AcademicYear.StartDate)
                .ThenBy(t => t.TermNumber)
                .ToListAsync();

        public async Task<IEnumerable<Term>> GetByAcademicYearIdAsync(Guid academicYearId, bool trackChanges) =>
            await FindByCondition(t => t.AcademicYearId == academicYearId, trackChanges)
                .Include(t => t.AcademicYear)
                    .ThenInclude(ay => ay.School)
                .OrderBy(t => t.TermNumber)
                .ToListAsync();

        public async Task<Term?> GetByIdWithDetailsAsync(Guid id, bool trackChanges) =>
            await FindByCondition(t => t.Id == id, trackChanges)
                .Include(t => t.AcademicYear)
                    .ThenInclude(ay => ay.School)
                .Include(t => t.Assessments)
                .Include(t => t.ProgressReports)
                .Include(t => t.Grades)
                .FirstOrDefaultAsync();

        public async Task<Term?> GetCurrentTermAsync(Guid schoolId) =>
            await FindByCondition(
                    t => t.TenantId == schoolId && t.IsCurrent && !t.IsClosed,
                    trackChanges: false)
                .Include(t => t.AcademicYear)
                .FirstOrDefaultAsync();

        public async Task<Term?> GetByTermNumberAsync(int termNumber, Guid academicYearId) =>
            await FindByCondition(
                    t => t.TermNumber == termNumber && t.AcademicYearId == academicYearId,
                    trackChanges: false)
                .FirstOrDefaultAsync();

        public async Task<IEnumerable<Term>> GetActiveTermsAsync(Guid schoolId, bool trackChanges) =>
            await FindByCondition(
                    t => t.TenantId == schoolId && !t.IsClosed,
                    trackChanges)
                .Include(t => t.AcademicYear)
                .OrderByDescending(t => t.AcademicYear.StartDate)
                .ThenBy(t => t.TermNumber)
                .ToListAsync();

        public async Task<bool> HasDateOverlapAsync(
            Guid academicYearId,
            DateTime startDate,
            DateTime endDate,
            Guid? excludeTermId = null)
        {
            var query = FindByCondition(
                t => t.AcademicYearId == academicYearId &&
                     ((t.StartDate <= endDate && t.EndDate >= startDate)),
                trackChanges: false);

            if (excludeTermId.HasValue)
            {
                query = query.Where(t => t.Id != excludeTermId.Value);
            }

            return await query.AnyAsync();
        }
    }
}