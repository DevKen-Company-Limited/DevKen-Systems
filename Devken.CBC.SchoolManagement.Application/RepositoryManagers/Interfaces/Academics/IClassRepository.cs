using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Enums;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academics
{
    public interface IClassRepository : IRepositoryBase<Class, Guid>
    {
        /// <summary>
        /// Gets all classes for a tenant
        /// </summary>
        Task<IEnumerable<Class>> GetAllByTenantAsync(Guid tenantId, bool trackChanges = false);

        /// <summary>
        /// Gets all classes for a specific academic year
        /// </summary>
        Task<IEnumerable<Class>> GetByAcademicYearAsync(Guid tenantId, Guid academicYearId, bool trackChanges = false);

        /// <summary>
        /// Gets class by code for a tenant
        /// </summary>
        Task<Class?> GetByCodeAsync(Guid tenantId, string code, bool trackChanges = false);

        /// <summary>
        /// Checks if a class code already exists for a tenant
        /// </summary>
        Task<bool> CodeExistsAsync(Guid tenantId, string code, Guid? excludeId = null);

        /// <summary>
        /// Gets all classes for a specific CBC level
        /// </summary>
        Task<IEnumerable<Class>> GetByLevelAsync(Guid tenantId, CBCLevel level, bool trackChanges = false);

        /// <summary>
        /// Gets all active classes for a tenant
        /// </summary>
        Task<IEnumerable<Class>> GetActiveClassesAsync(Guid tenantId, bool trackChanges = false);

        /// <summary>
        /// Gets classes assigned to a specific teacher
        /// </summary>
        Task<IEnumerable<Class>> GetByTeacherAsync(Guid tenantId, Guid teacherId, bool trackChanges = false);

        /// <summary>
        /// Gets class with students included
        /// </summary>
        Task<Class?> GetWithStudentsAsync(Guid id, bool trackChanges = false);

        /// <summary>
        /// Gets class with full details (students, subjects, teacher)
        /// </summary>
        Task<Class?> GetWithDetailsAsync(Guid id, bool trackChanges = false);

        /// <summary>
        /// Checks if class has available seats
        /// </summary>
        Task<bool> HasAvailableSeatsAsync(Guid classId);

        /// <summary>
        /// Updates class enrollment count
        /// </summary>
        Task UpdateEnrollmentAsync(Guid classId, int newEnrollment);
    }
}
