using Devken.CBC.SchoolManagement.Application.DTOs.Assessments;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Assessments
{
    // ═══════════════════════════════════════════════════════════════════
    // FORMATIVE ASSESSMENT — Service Interface
    // ═══════════════════════════════════════════════════════════════════

    public interface IFormativeAssessmentService
    {
        Task<IEnumerable<FormativeAssessmentDto>> GetAllAsync(Guid? schoolId);

        Task<FormativeAssessmentDto?> GetByIdAsync(Guid id, Guid? schoolId);

        /// <summary>Returns the assessment plus all its scores.</summary>
        Task<FormativeAssessmentDto?> GetWithScoresAsync(Guid id, Guid? schoolId);

        Task<IEnumerable<FormativeAssessmentDto>> GetByClassAsync(Guid classId, Guid? schoolId);

        Task<IEnumerable<FormativeAssessmentDto>> GetByTeacherAsync(Guid teacherId, Guid? schoolId);

        Task<IEnumerable<FormativeAssessmentDto>> GetByTermAsync(
            Guid termId, Guid academicYearId, Guid? schoolId);

        Task<IEnumerable<FormativeAssessmentDto>> GetByLearningOutcomeAsync(
            Guid learningOutcomeId, Guid? schoolId);

        Task<IEnumerable<FormativeAssessmentDto>> GetPublishedAsync(
            Guid classId, Guid termId, Guid? schoolId);

        Task<FormativeAssessmentDto> CreateAsync(
            CreateFormativeAssessmentRequest request, Guid schoolId);

        Task<FormativeAssessmentDto> UpdateAsync(
            Guid id, UpdateFormativeAssessmentRequest request, Guid? schoolId);

        Task PublishAsync(Guid id, bool publish, Guid? schoolId);

        Task DeleteAsync(Guid id, Guid? schoolId);
    }

    // ═══════════════════════════════════════════════════════════════════
    // FORMATIVE ASSESSMENT SCORE — Service Interface
    // ═══════════════════════════════════════════════════════════════════

    public interface IFormativeAssessmentScoreService
    {
        Task<IEnumerable<FormativeAssessmentScoreDto>> GetByAssessmentAsync(
            Guid assessmentId, Guid? schoolId);

        Task<IEnumerable<FormativeAssessmentScoreDto>> GetByStudentAsync(
            Guid studentId, Guid? schoolId);

        Task<IEnumerable<FormativeAssessmentScoreDto>> GetByStudentAndTermAsync(
            Guid studentId, Guid termId, Guid? schoolId);

        Task<FormativeAssessmentScoreDto?> GetByIdAsync(Guid id, Guid? schoolId);

        Task<FormativeAssessmentScoreDto> CreateAsync(
            CreateFormativeAssessmentScoreRequest request, Guid? gradedById, Guid? schoolId);

        Task<FormativeAssessmentScoreDto> UpdateAsync(
            Guid id, UpdateFormativeAssessmentScoreRequest request, Guid? gradedById, Guid? schoolId);

        /// <summary>
        /// Submits or retracts one or many scores in a single call.
        /// </summary>
        Task BulkSubmitAsync(
            BulkSubmitFormativeScoresRequest request, Guid? schoolId);

        Task DeleteAsync(Guid id, Guid? schoolId);

        /// <summary>Removes all scores for a given assessment (used before deleting it).</summary>
        Task DeleteByAssessmentAsync(Guid assessmentId, Guid? schoolId);
    }
}