using Devken.CBC.SchoolManagement.Application.DTOs.Assessments;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Assessments
{
    // ═══════════════════════════════════════════════════════════════════
    // SUMMATIVE ASSESSMENT — Service Interface
    // ═══════════════════════════════════════════════════════════════════

    public interface ISummativeAssessmentService
    {
        Task<IEnumerable<SummativeAssessmentDto>> GetAllAsync(Guid? schoolId);
        Task<SummativeAssessmentDto?> GetByIdAsync(Guid id, Guid? schoolId);

        /// <summary>Returns the assessment plus all its scores.</summary>
        Task<SummativeAssessmentDto?> GetWithScoresAsync(Guid id, Guid? schoolId);

        Task<IEnumerable<SummativeAssessmentDto>> GetByClassAsync(Guid classId, Guid? schoolId);
        Task<IEnumerable<SummativeAssessmentDto>> GetByTeacherAsync(Guid teacherId, Guid? schoolId);
        Task<IEnumerable<SummativeAssessmentDto>> GetByTermAsync(
            Guid termId, Guid academicYearId, Guid? schoolId);
        Task<IEnumerable<SummativeAssessmentDto>> GetByExamTypeAsync(
            string examType, Guid? schoolId);
        Task<IEnumerable<SummativeAssessmentDto>> GetPublishedAsync(
            Guid classId, Guid termId, Guid? schoolId);

        Task<SummativeAssessmentDto> CreateAsync(
            CreateSummativeAssessmentRequest request, Guid schoolId);
        Task<SummativeAssessmentDto> UpdateAsync(
            Guid id, UpdateSummativeAssessmentRequest request, Guid? schoolId);
        Task PublishAsync(Guid id, bool publish, Guid? schoolId);
        Task DeleteAsync(Guid id, Guid? schoolId);
    }

    // ═══════════════════════════════════════════════════════════════════
    // SUMMATIVE ASSESSMENT SCORE — Service Interface
    // ═══════════════════════════════════════════════════════════════════

    public interface ISummativeAssessmentScoreService
    {
        Task<IEnumerable<SummativeAssessmentScoreDto>> GetByAssessmentAsync(
            Guid assessmentId, Guid? schoolId);
        Task<IEnumerable<SummativeAssessmentScoreDto>> GetByStudentAsync(
            Guid studentId, Guid? schoolId);
        Task<IEnumerable<SummativeAssessmentScoreDto>> GetByStudentAndTermAsync(
            Guid studentId, Guid termId, Guid? schoolId);
        Task<SummativeAssessmentScoreDto?> GetByIdAsync(Guid id, Guid? schoolId);

        Task<SummativeAssessmentScoreDto> CreateAsync(
            CreateSummativeAssessmentScoreRequest request, Guid? gradedById, Guid? schoolId);
        Task<SummativeAssessmentScoreDto> UpdateAsync(
            Guid id, UpdateSummativeAssessmentScoreRequest request, Guid? gradedById, Guid? schoolId);

        /// <summary>Recalculates class and stream positions for all scores of an assessment.</summary>
        Task RecalculatePositionsAsync(Guid assessmentId, Guid? schoolId);

        Task DeleteAsync(Guid id, Guid? schoolId);

        /// <summary>Removes all scores for a given assessment (used before deleting it).</summary>
        Task DeleteByAssessmentAsync(Guid assessmentId, Guid? schoolId);
    }
}