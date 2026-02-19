using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Assessments
{
    // ═══════════════════════════════════════════════════════════════════
    // COMPETENCY ASSESSMENT — Repository Interface
    // ═══════════════════════════════════════════════════════════════════

    public interface ICompetencyAssessmentRepository
    {
        // ── Read ────────────────────────────────────────────────────────

        Task<IEnumerable<CompetencyAssessment>> GetAllAsync(bool trackChanges);

        Task<IEnumerable<CompetencyAssessment>> GetBySchoolAsync(Guid schoolId, bool trackChanges);

        Task<CompetencyAssessment?> GetByIdAsync(Guid id, bool trackChanges);

        /// <summary>Returns the assessment with all its scores, students, and assessors.</summary>
        Task<CompetencyAssessment?> GetWithScoresAsync(Guid id, bool trackChanges);

        Task<IEnumerable<CompetencyAssessment>> GetByClassAsync(Guid classId, bool trackChanges);

        Task<IEnumerable<CompetencyAssessment>> GetByTeacherAsync(Guid teacherId, bool trackChanges);

        Task<IEnumerable<CompetencyAssessment>> GetByTermAsync(
            Guid termId, Guid academicYearId, bool trackChanges);

        Task<IEnumerable<CompetencyAssessment>> GetByCompetencyNameAsync(
            string competencyName, Guid schoolId, bool trackChanges);

        Task<IEnumerable<CompetencyAssessment>> GetByTargetLevelAsync(
            CBCLevel level, Guid schoolId, bool trackChanges);

        Task<IEnumerable<CompetencyAssessment>> GetByStrandAsync(
            string strand, Guid schoolId, bool trackChanges);

        Task<IEnumerable<CompetencyAssessment>> GetPublishedAsync(
            Guid classId, Guid termId, bool trackChanges);

        // ── Write ───────────────────────────────────────────────────────

        void Create(CompetencyAssessment assessment);
        void Update(CompetencyAssessment assessment);
        void Delete(CompetencyAssessment assessment);
    }

    // ═══════════════════════════════════════════════════════════════════
    // COMPETENCY ASSESSMENT SCORE — Repository Interface
    // ═══════════════════════════════════════════════════════════════════

    public interface ICompetencyAssessmentScoreRepository
    {
        // ── Read ────────────────────────────────────────────────────────

        Task<IEnumerable<CompetencyAssessmentScore>> GetAllByAssessmentAsync(
            Guid assessmentId, bool trackChanges);

        Task<IEnumerable<CompetencyAssessmentScore>> GetByStudentAsync(
            Guid studentId, bool trackChanges);

        Task<IEnumerable<CompetencyAssessmentScore>> GetByStudentAndTermAsync(
            Guid studentId, Guid termId, bool trackChanges);

        Task<CompetencyAssessmentScore?> GetByIdAsync(Guid id, bool trackChanges);

        /// <summary>Returns an existing score for a student + assessment pair (for duplicate check).</summary>
        Task<CompetencyAssessmentScore?> GetByStudentAndAssessmentAsync(
            Guid studentId, Guid assessmentId, bool trackChanges);

        Task<IEnumerable<CompetencyAssessmentScore>> GetFinalizedByAssessmentAsync(
            Guid assessmentId, bool trackChanges);

        Task<IEnumerable<CompetencyAssessmentScore>> GetByRatingAsync(
            Guid assessmentId, string rating, bool trackChanges);

        Task<IEnumerable<CompetencyAssessmentScore>> GetByAssessorAsync(
            Guid assessorId, bool trackChanges);

        // ── Write ───────────────────────────────────────────────────────

        void Create(CompetencyAssessmentScore score);
        void Update(CompetencyAssessmentScore score);
        void Delete(CompetencyAssessmentScore score);
        void DeleteRange(IEnumerable<CompetencyAssessmentScore> scores);
    }
}