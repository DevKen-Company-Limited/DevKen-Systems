using Devken.CBC.SchoolManagement.Application.DTOs.Assessments;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Assessments
{
    // ═══════════════════════════════════════════════════════════════════
    // COMPETENCY ASSESSMENT — Service Interface
    // ═══════════════════════════════════════════════════════════════════

    public interface ICompetencyAssessmentService
    {
        Task<IEnumerable<CompetencyAssessmentDto>> GetAllAsync(Guid? schoolId);

        Task<CompetencyAssessmentDto?> GetByIdAsync(Guid id, Guid? schoolId);

        /// <summary>Returns the assessment plus all its scores.</summary>
        Task<CompetencyAssessmentDto?> GetWithScoresAsync(Guid id, Guid? schoolId);

        Task<IEnumerable<CompetencyAssessmentDto>> GetByClassAsync(Guid classId, Guid? schoolId);

        Task<IEnumerable<CompetencyAssessmentDto>> GetByTeacherAsync(Guid teacherId, Guid? schoolId);

        Task<IEnumerable<CompetencyAssessmentDto>> GetByTermAsync(
            Guid termId, Guid academicYearId, Guid? schoolId);

        Task<IEnumerable<CompetencyAssessmentDto>> GetByCompetencyNameAsync(
            string competencyName, Guid schoolId);

        Task<IEnumerable<CompetencyAssessmentDto>> GetByTargetLevelAsync(
            CBCLevel level, Guid schoolId);

        Task<IEnumerable<CompetencyAssessmentDto>> GetByStrandAsync(
            string strand, Guid schoolId);

        Task<IEnumerable<CompetencyAssessmentDto>> GetPublishedAsync(
            Guid classId, Guid termId, Guid? schoolId);

        Task<CompetencyAssessmentDto> CreateAsync(
            CreateCompetencyAssessmentRequest request, Guid schoolId);

        Task<CompetencyAssessmentDto> UpdateAsync(
            Guid id, UpdateCompetencyAssessmentRequest request, Guid? schoolId);

        Task PublishAsync(Guid id, bool publish, Guid? schoolId);

        Task DeleteAsync(Guid id, Guid? schoolId);
    }

    // ═══════════════════════════════════════════════════════════════════
    // COMPETENCY ASSESSMENT SCORE — Service Interface
    // ═══════════════════════════════════════════════════════════════════

    public interface ICompetencyAssessmentScoreService
    {
        Task<IEnumerable<CompetencyAssessmentScoreDto>> GetByAssessmentAsync(
            Guid assessmentId, Guid? schoolId);

        Task<IEnumerable<CompetencyAssessmentScoreDto>> GetByStudentAsync(
            Guid studentId, Guid? schoolId);

        Task<IEnumerable<CompetencyAssessmentScoreDto>> GetByStudentAndTermAsync(
            Guid studentId, Guid termId, Guid? schoolId);

        Task<CompetencyAssessmentScoreDto?> GetByIdAsync(Guid id, Guid? schoolId);

        Task<IEnumerable<CompetencyAssessmentScoreDto>> GetByRatingAsync(
            Guid assessmentId, string rating, Guid? schoolId);

        Task<IEnumerable<CompetencyAssessmentScoreDto>> GetByAssessorAsync(
            Guid assessorId, Guid? schoolId);

        Task<CompetencyAssessmentScoreDto> CreateAsync(
            CreateCompetencyAssessmentScoreRequest request, Guid? assessorId, Guid? schoolId);

        Task<CompetencyAssessmentScoreDto> UpdateAsync(
            Guid id, UpdateCompetencyAssessmentScoreRequest request, Guid? assessorId, Guid? schoolId);

        /// <summary>Finalizes or un-finalizes one or many scores in a single call.</summary>
        Task BulkFinalizeAsync(BulkFinalizeCompetencyScoresRequest request, Guid? schoolId);

        Task DeleteAsync(Guid id, Guid? schoolId);

        /// <summary>Removes all scores for a given assessment (called before deleting the assessment).</summary>
        Task DeleteByAssessmentAsync(Guid assessmentId, Guid? schoolId);
    }
}