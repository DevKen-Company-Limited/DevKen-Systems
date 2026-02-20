using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Assessments
{
    public interface IAssessmentRepository : IRepositoryBase<Assessment1, Guid>
    {
        /// <summary>Returns all assessments (SuperAdmin use – not tenant-scoped).</summary>
        Task<IEnumerable<Assessment1>> GetAllAsync(bool trackChanges = false);

        /// <summary>Returns all assessments belonging to a specific school/tenant.</summary>
        Task<IEnumerable<Assessment1>> GetBySchoolAsync(Guid schoolId, bool trackChanges = false);

        /// <summary>Returns all assessments for a given class (no tracking).</summary>
        Task<IEnumerable<Assessment1>> GetByClassAsync(Guid classId, bool trackChanges = false);

        /// <summary>Returns all assessments for a given teacher (no tracking).</summary>
        Task<IEnumerable<Assessment1>> GetByTeacherAsync(Guid teacherId, bool trackChanges = false);

        /// <summary>Returns all assessments for a given term/academic year (no tracking).</summary>
        Task<IEnumerable<Assessment1>> GetByTermAsync(Guid termId, Guid academicYearId, bool trackChanges = false);

        /// <summary>Returns a single assessment with its grades eagerly loaded.</summary>
        Task<Assessment1?> GetWithGradesAsync(Guid assessmentId, bool trackChanges = false);

        /// <summary>Returns all published assessments visible to students.</summary>
        Task<IEnumerable<Assessment1>> GetPublishedAsync(Guid classId, Guid termId, bool trackChanges = false);
    }
}