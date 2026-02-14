using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.DTOs.Academics;

namespace Devken.CBC.SchoolManagement.Application.Service.Academics
{
    public interface ITermService
    {
        /// <summary>
        /// Get all terms based on user access level
        /// </summary>
        Task<IEnumerable<TermDto>> GetAllTermsAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin);

        /// <summary>
        /// Get terms for a specific academic year
        /// </summary>
        Task<IEnumerable<TermDto>> GetTermsByAcademicYearAsync(
            Guid academicYearId,
            Guid? userSchoolId,
            bool isSuperAdmin);

        /// <summary>
        /// Get a term by ID
        /// </summary>
        Task<TermDto> GetTermByIdAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin);

        /// <summary>
        /// Get the current active term for a school
        /// </summary>
        Task<TermDto?> GetCurrentTermAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin);

        /// <summary>
        /// Get all active (not closed) terms
        /// </summary>
        Task<IEnumerable<TermDto>> GetActiveTermsAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin);

        /// <summary>
        /// Create a new term
        /// </summary>
        Task<TermDto> CreateTermAsync(
            CreateTermRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin);

        /// <summary>
        /// Update an existing term
        /// </summary>
        Task<TermDto> UpdateTermAsync(
            Guid id,
            UpdateTermRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin);

        /// <summary>
        /// Delete a term
        /// </summary>
        Task DeleteTermAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin);

        /// <summary>
        /// Set a term as the current active term
        /// </summary>
        Task<TermDto> SetCurrentTermAsync(
            Guid termId,
            Guid? userSchoolId,
            bool isSuperAdmin);

        /// <summary>
        /// Close a term (mark as finished)
        /// </summary>
        Task<TermDto> CloseTermAsync(
            Guid termId,
            string? remarks,
            Guid? userSchoolId,
            bool isSuperAdmin);

        /// <summary>
        /// Reopen a closed term
        /// </summary>
        Task<TermDto> ReopenTermAsync(
            Guid termId,
            Guid? userSchoolId,
            bool isSuperAdmin);
    }
}
