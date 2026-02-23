using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Assessments
{
    // ═══════════════════════════════════════════════════════════════════
    // FORMATIVE ASSESSMENT — Repository Interface
    // ═══════════════════════════════════════════════════════════════════

    public interface IFormativeAssessmentRepository
    {
        // ── Read ────────────────────────────────────────────────────────

        Task<IEnumerable<FormativeAssessment>> GetAllAsync(bool trackChanges);

        Task<IEnumerable<FormativeAssessment>> GetBySchoolAsync(Guid schoolId, bool trackChanges);

        Task<FormativeAssessment?> GetByIdAsync(Guid id, bool trackChanges);

        /// <summary>Returns the assessment with its scores and student details.</summary>
        Task<FormativeAssessment?> GetWithScoresAsync(Guid id, bool trackChanges);

        Task<IEnumerable<FormativeAssessment>> GetByClassAsync(Guid classId, bool trackChanges);

        Task<IEnumerable<FormativeAssessment>> GetByTeacherAsync(Guid teacherId, bool trackChanges);

        Task<IEnumerable<FormativeAssessment>> GetByTermAsync(
            Guid termId, Guid academicYearId, bool trackChanges);

        Task<IEnumerable<FormativeAssessment>> GetByLearningOutcomeAsync(
            Guid learningOutcomeId, bool trackChanges);

        Task<IEnumerable<FormativeAssessment>> GetPublishedAsync(
            Guid classId, Guid termId, bool trackChanges);

        // ── Write ───────────────────────────────────────────────────────

        void Create(FormativeAssessment assessment);
        void Update(FormativeAssessment assessment);
        void Delete(FormativeAssessment assessment);
    }

    // ═══════════════════════════════════════════════════════════════════
    // FORMATIVE ASSESSMENT SCORE — Repository Interface
    // ═══════════════════════════════════════════════════════════════════

    public interface IFormativeAssessmentScoreRepository
    {
        // ── Read ────────────────────────────────────────────────────────

        Task<IEnumerable<FormativeAssessmentScore>> GetAllByAssessmentAsync(
            Guid assessmentId, bool trackChanges);

        Task<IEnumerable<FormativeAssessmentScore>> GetByStudentAsync(
            Guid studentId, bool trackChanges);

        Task<IEnumerable<FormativeAssessmentScore>> GetByStudentAndTermAsync(
            Guid studentId, Guid termId, bool trackChanges);

        Task<FormativeAssessmentScore?> GetByIdAsync(Guid id, bool trackChanges);

        /// <summary>Returns an existing score for a specific student + assessment pair.</summary>
        Task<FormativeAssessmentScore?> GetByStudentAndAssessmentAsync(
            Guid studentId, Guid assessmentId, bool trackChanges);

        Task<IEnumerable<FormativeAssessmentScore>> GetSubmittedByAssessmentAsync(
            Guid assessmentId, bool trackChanges);

        // ── Write ───────────────────────────────────────────────────────

        void Create(FormativeAssessmentScore score);
        void Update(FormativeAssessmentScore score);
        void Delete(FormativeAssessmentScore score);
        void DeleteRange(IEnumerable<FormativeAssessmentScore> scores);
    }
}