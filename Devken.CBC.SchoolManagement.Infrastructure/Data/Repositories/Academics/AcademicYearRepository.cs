using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academics;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Academics
{
    public class AcademicYearRepository : RepositoryBase<AcademicYear, Guid>, IAcademicYearRepository
    {
        public AcademicYearRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext)
        {
        }

        public async Task<AcademicYear?> GetCurrentAcademicYearAsync(Guid tenantId)
        {
            return await FindByCondition(
                ay => ay.TenantId == tenantId && ay.IsCurrent,
                trackChanges: false)
                .Include(ay => ay.Terms)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<AcademicYear>> GetAllByTenantAsync(Guid tenantId, bool trackChanges = false)
        {
            return await FindByCondition(
                ay => ay.TenantId == tenantId,
                trackChanges)
                .OrderByDescending(ay => ay.StartDate)
                .ToListAsync();
        }

        public async Task<AcademicYear?> GetByCodeAsync(Guid tenantId, string code, bool trackChanges = false)
        {
            return await FindByCondition(
                ay => ay.TenantId == tenantId && ay.Code == code,
                trackChanges)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> CodeExistsAsync(Guid tenantId, string code, Guid? excludeId = null)
        {
            var query = FindByCondition(
                ay => ay.TenantId == tenantId && ay.Code == code,
                trackChanges: false);

            if (excludeId.HasValue)
            {
                query = query.Where(ay => ay.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<IEnumerable<AcademicYear>> GetByDateRangeAsync(
            Guid tenantId,
            DateTime startDate,
            DateTime endDate,
            bool trackChanges = false)
        {
            return await FindByCondition(
                ay => ay.TenantId == tenantId &&
                      ay.StartDate <= endDate &&
                      ay.EndDate >= startDate,
                trackChanges)
                .OrderBy(ay => ay.StartDate)
                .ToListAsync();
        }

        public async Task SetAsCurrentAsync(Guid tenantId, Guid academicYearId)
        {
            // Unset all current academic years for this tenant
            var currentYears = await FindByCondition(
                ay => ay.TenantId == tenantId && ay.IsCurrent,
                trackChanges: true)
                .ToListAsync();

            foreach (var year in currentYears)
            {
                year.IsCurrent = false;
            }

            // Set the specified year as current
            var targetYear = await GetByIdAsync(academicYearId, trackChanges: true);
            if (targetYear != null && targetYear.TenantId == tenantId)
            {
                targetYear.IsCurrent = true;
            }
        }

        public async Task<IEnumerable<AcademicYear>> GetOpenAcademicYearsAsync(Guid tenantId, bool trackChanges = false)
        {
            return await FindByCondition(
                ay => ay.TenantId == tenantId && !ay.IsClosed,
                trackChanges)
                .OrderByDescending(ay => ay.StartDate)
                .ToListAsync();
        }

        public async Task<bool> HasOverlappingYearsAsync(
            Guid tenantId,
            DateTime startDate,
            DateTime endDate,
            Guid? excludeId = null)
        {
            var query = FindByCondition(
                ay => ay.TenantId == tenantId &&
                      ((ay.StartDate <= endDate && ay.EndDate >= startDate)),
                trackChanges: false);

            if (excludeId.HasValue)
            {
                query = query.Where(ay => ay.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }
    }
}
