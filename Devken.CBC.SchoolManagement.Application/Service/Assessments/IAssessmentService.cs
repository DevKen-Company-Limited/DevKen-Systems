using Devken.CBC.SchoolManagement.Application.DTOs.Assessments;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Assessments
{
    public interface IAssessmentService
    {
        // ── Queries ──────────────────────────────────────────────────────────
        Task<IEnumerable<AssessmentListItem>> GetAllAsync(
            AssessmentTypeDto? type,
            Guid? classId,
            Guid? termId,
            Guid? subjectId,
            Guid? teacherId,
            bool? isPublished,
            Guid? userSchoolId,
            bool isSuperAdmin);

        Task<AssessmentResponse> GetByIdAsync(
            Guid id,
            AssessmentTypeDto type,
            Guid? userSchoolId,
            bool isSuperAdmin);

        // ── Commands ─────────────────────────────────────────────────────────
        Task<AssessmentResponse> CreateAsync(
            CreateAssessmentRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin);

        Task<AssessmentResponse> UpdateAsync(
            Guid id,
            UpdateAssessmentRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin);

        Task PublishAsync(
            Guid id,
            AssessmentTypeDto type,
            Guid? userSchoolId,
            bool isSuperAdmin);

        Task DeleteAsync(
            Guid id,
            AssessmentTypeDto type,
            Guid? userSchoolId,
            bool isSuperAdmin);

        // ── Scores ───────────────────────────────────────────────────────────
        Task<IEnumerable<AssessmentScoreResponse>> GetScoresAsync(
            Guid assessmentId,
            AssessmentTypeDto type,
            Guid? userSchoolId,
            bool isSuperAdmin);

        Task<AssessmentScoreResponse> UpsertScoreAsync(
            UpsertScoreRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin);

        Task DeleteScoreAsync(
            Guid scoreId,
            AssessmentTypeDto type,
            Guid? userSchoolId,
            bool isSuperAdmin);
    }
}