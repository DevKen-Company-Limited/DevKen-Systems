using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academics
{
    public interface IAcademicYearRepository : IRepositoryBase<AcademicYear, Guid>
    {
        /// <summary>
        /// Gets the current active academic year for a tenant
        /// </summary>
        Task<AcademicYear?> GetCurrentAcademicYearAsync(Guid tenantId);

        /// <summary>
        /// Gets all academic years for a tenant
        /// </summary>
        Task<IEnumerable<AcademicYear>> GetAllByTenantAsync(Guid tenantId, bool trackChanges = false);

        /// <summary>
        /// Gets academic year by code for a tenant
        /// </summary>
        Task<AcademicYear?> GetByCodeAsync(Guid tenantId, string code, bool trackChanges = false);

        /// <summary>
        /// Checks if an academic year code already exists for a tenant
        /// </summary>
        Task<bool> CodeExistsAsync(Guid tenantId, string code, Guid? excludeId = null);

        /// <summary>
        /// Gets academic years within a date range
        /// </summary>
        Task<IEnumerable<AcademicYear>> GetByDateRangeAsync(Guid tenantId, DateTime startDate, DateTime endDate, bool trackChanges = false);

        /// <summary>
        /// Sets a specific academic year as current and unsets all others for the tenant
        /// </summary>
        Task SetAsCurrentAsync(Guid tenantId, Guid academicYearId);

        /// <summary>
        /// Gets all open (not closed) academic years for a tenant
        /// </summary>
        Task<IEnumerable<AcademicYear>> GetOpenAcademicYearsAsync(Guid tenantId, bool trackChanges = false);

        /// <summary>
        /// Checks if there are any overlapping academic years
        /// </summary>
        Task<bool> HasOverlappingYearsAsync(Guid tenantId, DateTime startDate, DateTime endDate, Guid? excludeId = null);
    }
}
