using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
<<<<<<< HEAD
=======

>>>>>>> upstream/main
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Assessments
{
<<<<<<< HEAD
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
=======
    // ─────────────────────────────────────────────────────────────────────────
    // FORMATIVE
    // ─────────────────────────────────────────────────────────────────────────
    public interface IFormativeAssessmentRepository : IRepositoryBase<FormativeAssessment, Guid>
    {
        Task<IEnumerable<FormativeAssessment>> GetAllAsync(
            Guid? classId, Guid? termId, Guid? subjectId, Guid? teacherId,
            bool? isPublished, bool trackChanges = false);

        /// <summary>
        /// Tenant-filtered fetch. Use for regular tenant users.
        /// </summary>
        Task<FormativeAssessment?> GetByIdWithDetailsAsync(Guid id, bool trackChanges = false);

        /// <summary>
        /// Bypasses the global tenant query filter.
        /// Use for SuperAdmin operations where TenantContext != entity.TenantId.
        /// </summary>
        Task<FormativeAssessment?> GetByIdIgnoringTenantAsync(Guid id, bool trackChanges = false);

        Task<IEnumerable<FormativeAssessment>> GetByClassAndTermAsync(
            Guid classId, Guid termId, bool trackChanges = false);

        Task<bool> IsPublishedAsync(Guid id);

        /// <summary>
        /// Loads all navigation properties on a tracked entity, bypassing the
        /// tenant query filter. Call after Create + SaveAsync for SuperAdmin creates.
        /// </summary>
        Task LoadNavigationsAsync(FormativeAssessment entity);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SUMMATIVE
    // ─────────────────────────────────────────────────────────────────────────
    public interface ISummativeAssessmentRepository : IRepositoryBase<SummativeAssessment, Guid>
    {
        Task<IEnumerable<SummativeAssessment>> GetAllAsync(
            Guid? classId, Guid? termId, Guid? subjectId, Guid? teacherId,
            bool? isPublished, bool trackChanges = false);

        /// <summary>
        /// Tenant-filtered fetch. Use for regular tenant users.
        /// </summary>
        Task<SummativeAssessment?> GetByIdWithDetailsAsync(Guid id, bool trackChanges = false);

        /// <summary>
        /// Bypasses the global tenant query filter.
        /// Use for SuperAdmin operations where TenantContext != entity.TenantId.
        /// </summary>
        Task<SummativeAssessment?> GetByIdIgnoringTenantAsync(Guid id, bool trackChanges = false);

        Task<IEnumerable<SummativeAssessment>> GetByClassAndTermAsync(
            Guid classId, Guid termId, bool trackChanges = false);

        Task<bool> IsPublishedAsync(Guid id);

        /// <summary>
        /// Loads all navigation properties on a tracked entity, bypassing the
        /// tenant query filter. Call after Create + SaveAsync for SuperAdmin creates.
        /// </summary>
        Task LoadNavigationsAsync(SummativeAssessment entity);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // COMPETENCY
    // ─────────────────────────────────────────────────────────────────────────
    public interface ICompetencyAssessmentRepository : IRepositoryBase<CompetencyAssessment, Guid>
    {
        Task<IEnumerable<CompetencyAssessment>> GetAllAsync(
            Guid? classId, Guid? termId, Guid? subjectId, Guid? teacherId,
            bool? isPublished, bool trackChanges = false);

        /// <summary>
        /// Tenant-filtered fetch. Use for regular tenant users.
        /// </summary>
        Task<CompetencyAssessment?> GetByIdWithDetailsAsync(Guid id, bool trackChanges = false);

        /// <summary>
        /// Bypasses the global tenant query filter.
        /// Use for SuperAdmin operations where TenantContext != entity.TenantId.
        /// </summary>
        Task<CompetencyAssessment?> GetByIdIgnoringTenantAsync(Guid id, bool trackChanges = false);

        Task<IEnumerable<CompetencyAssessment>> GetByClassAndTermAsync(
            Guid classId, Guid termId, bool trackChanges = false);

        Task<bool> IsPublishedAsync(Guid id);

        /// <summary>
        /// Loads all navigation properties on a tracked entity, bypassing the
        /// tenant query filter. Call after Create + SaveAsync for SuperAdmin creates.
        /// </summary>
        Task LoadNavigationsAsync(CompetencyAssessment entity);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SCORE REPOSITORIES
    // ─────────────────────────────────────────────────────────────────────────
    public interface IFormativeAssessmentScoreRepository : IRepositoryBase<FormativeAssessmentScore, Guid>
    {
        Task<IEnumerable<FormativeAssessmentScore>> GetByAssessmentAsync(
            Guid assessmentId, bool trackChanges = false);

        Task<IEnumerable<FormativeAssessmentScore>> GetByStudentAsync(
            Guid studentId, Guid? termId = null, bool trackChanges = false);

        Task<FormativeAssessmentScore?> GetByAssessmentAndStudentAsync(
            Guid assessmentId, Guid studentId, bool trackChanges = false);
    }

    public interface ISummativeAssessmentScoreRepository : IRepositoryBase<SummativeAssessmentScore, Guid>
    {
        Task<IEnumerable<SummativeAssessmentScore>> GetByAssessmentAsync(
            Guid assessmentId, bool trackChanges = false);

        Task<IEnumerable<SummativeAssessmentScore>> GetByStudentAsync(
            Guid studentId, Guid? termId = null, bool trackChanges = false);

        Task<SummativeAssessmentScore?> GetByAssessmentAndStudentAsync(
            Guid assessmentId, Guid studentId, bool trackChanges = false);
    }

    public interface ICompetencyAssessmentScoreRepository : IRepositoryBase<CompetencyAssessmentScore, Guid>
    {
        Task<IEnumerable<CompetencyAssessmentScore>> GetByAssessmentAsync(
            Guid assessmentId, bool trackChanges = false);

        Task<IEnumerable<CompetencyAssessmentScore>> GetByStudentAsync(
            Guid studentId, Guid? termId = null, bool trackChanges = false);

        Task<CompetencyAssessmentScore?> GetByAssessmentAndStudentAsync(
            Guid assessmentId, Guid studentId, bool trackChanges = false);
>>>>>>> upstream/main
    }
}