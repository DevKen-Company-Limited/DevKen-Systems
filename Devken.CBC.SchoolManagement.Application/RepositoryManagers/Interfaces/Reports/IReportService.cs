using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Reports
{
    public interface IReportService
    {
        /// <summary>
        /// Generates a Students List PDF scoped to a single school.
        /// <paramref name="schoolId"/> is the target school (nullable so SuperAdmin can omit it).
        /// <paramref name="userSchoolId"/> is the calling user's own school (for access-control context).
        /// </summary>
        
        // ── Students Report methods ────────────────────────────────────────
        Task<byte[]> GenerateStudentsListReportAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin);

        /// <summary>
        /// Generates a Students List PDF covering ALL schools in the system.
        /// Only callable by a SuperAdmin — the controller enforces this constraint.
        /// </summary>
        Task<byte[]> GenerateAllSchoolsStudentsListReportAsync();
        // ── Subject Report methods ────────────────────────────────────────
        Task<byte[]> GenerateSubjectsListReportAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin);

        Task<byte[]> GenerateAllSchoolsSubjectsListReportAsync();

        /// <summary>
        /// Generates a Students List PDF covering ALL schools in the system.
        /// Only callable by a SuperAdmin — the controller enforces this constraint.
        /// </summary>
        Task<byte[]> GenerateAllSchoolsStudentsListReportAsync();
    }
}