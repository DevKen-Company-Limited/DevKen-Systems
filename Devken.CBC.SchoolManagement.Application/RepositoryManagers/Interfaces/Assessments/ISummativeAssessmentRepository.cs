using Devken.CBC.SchoolManagement.Domain.Entities.Assessment;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Assessments
{
    // ═══════════════════════════════════════════════════════════════════
    // SUMMATIVE ASSESSMENT — Repository Interface
    // ═══════════════════════════════════════════════════════════════════

    public interface ISummativeAssessmentRepository
    {
        // ── Read ────────────────────────────────────────────────────────

        Task<IEnumerable<SummativeAssessment>> GetAllAsync(bool trackChanges);

        Task<IEnumerable<SummativeAssessment>> GetBySchoolAsync(Guid schoolId, bool trackChanges);

        Task<SummativeAssessment?> GetByIdAsync(Guid id, bool trackChanges);

        /// <summary>Returns the assessment with its scores and student details.</summary>
        Task<SummativeAssessment?> GetWithScoresAsync(Guid id, bool trackChanges);

        Task<IEnumerable<SummativeAssessment>> GetByClassAsync(Guid classId, bool trackChanges);

        Task<IEnumerable<SummativeAssessment>> GetByTeacherAsync(Guid teacherId, bool trackChanges);

        Task<IEnumerable<SummativeAssessment>> GetByTermAsync(
            Guid termId, Guid academicYearId, bool trackChanges);

        Task<IEnumerable<SummativeAssessment>> GetByExamTypeAsync(
            string examType, bool trackChanges);

        Task<IEnumerable<SummativeAssessment>> GetPublishedAsync(
            Guid classId, Guid termId, bool trackChanges);

        // ── Write ───────────────────────────────────────────────────────

        void Create(SummativeAssessment assessment);
        void Update(SummativeAssessment assessment);
        void Delete(SummativeAssessment assessment);
    }

    // ═══════════════════════════════════════════════════════════════════
    // SUMMATIVE ASSESSMENT SCORE — Repository Interface
    // ═══════════════════════════════════════════════════════════════════

    public interface ISummativeAssessmentScoreRepository
    {
        // ── Read ────────────────────────────────────────────────────────

        Task<IEnumerable<SummativeAssessmentScore>> GetAllByAssessmentAsync(
            Guid assessmentId, bool trackChanges);

        Task<IEnumerable<SummativeAssessmentScore>> GetByStudentAsync(
            Guid studentId, bool trackChanges);

        Task<IEnumerable<SummativeAssessmentScore>> GetByStudentAndTermAsync(
            Guid studentId, Guid termId, bool trackChanges);

        Task<SummativeAssessmentScore?> GetByIdAsync(Guid id, bool trackChanges);

        /// <summary>Returns an existing score for a specific student + assessment pair.</summary>
        Task<SummativeAssessmentScore?> GetByStudentAndAssessmentAsync(
            Guid studentId, Guid assessmentId, bool trackChanges);

        // ── Write ───────────────────────────────────────────────────────

        void Create(SummativeAssessmentScore score);
        void Update(SummativeAssessmentScore score);
        void Delete(SummativeAssessmentScore score);
        void DeleteRange(IEnumerable<SummativeAssessmentScore> scores);
    }
}