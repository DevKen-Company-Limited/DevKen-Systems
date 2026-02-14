using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academics
{
    public interface ITermRepository : IRepositoryBase<Term, Guid>
    {
        /// <summary>
        /// Get all terms with related entities
        /// </summary>
        Task<IEnumerable<Term>> GetAllAsync(bool trackChanges);

        /// <summary>
        /// Get all terms for a specific school
        /// </summary>
        Task<IEnumerable<Term>> GetBySchoolIdAsync(Guid schoolId, bool trackChanges);

        /// <summary>
        /// Get all terms for a specific academic year
        /// </summary>
        Task<IEnumerable<Term>> GetByAcademicYearIdAsync(Guid academicYearId, bool trackChanges);

        /// <summary>
        /// Get a term by ID with all related entities
        /// </summary>
        Task<Term?> GetByIdWithDetailsAsync(Guid id, bool trackChanges);

        /// <summary>
        /// Get the current active term for a school
        /// </summary>
        Task<Term?> GetCurrentTermAsync(Guid schoolId);

        /// <summary>
        /// Check if a term number already exists for an academic year
        /// </summary>
        Task<Term?> GetByTermNumberAsync(int termNumber, Guid academicYearId);

        /// <summary>
        /// Get all active (not closed) terms for a school
        /// </summary>
        Task<IEnumerable<Term>> GetActiveTermsAsync(Guid schoolId, bool trackChanges);

        /// <summary>
        /// Check if there are any overlapping terms in the academic year
        /// </summary>
        Task<bool> HasDateOverlapAsync(Guid academicYearId, DateTime startDate, DateTime endDate, Guid? excludeTermId = null);
    }
}
