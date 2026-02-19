using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Assessments;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.Assessments;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Assessments
{
    /// <summary>
    /// Manages Competency-Based Assessments (CBC) and their student scores.
    ///
    /// Access rules:
    ///   SuperAdmin  — full read/write across all schools.
    ///   SchoolAdmin — full read/write within their school.
    ///   Teacher     — create / update / publish own assessments; record competency scores.
    ///   Student     — read published assessments and their own scores only.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CompetencyAssessmentsController : BaseApiController
    {
        private readonly ICompetencyAssessmentService _assessmentService;
        private readonly ICompetencyAssessmentScoreService _scoreService;

        public CompetencyAssessmentsController(
            ICompetencyAssessmentService assessmentService,
            ICompetencyAssessmentScoreService scoreService,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _assessmentService = assessmentService ?? throw new ArgumentNullException(nameof(assessmentService));
            _scoreService = scoreService ?? throw new ArgumentNullException(nameof(scoreService));
        }

        // ─────────────────────────────────────────────────────────────────────
        #region Helpers

        private static string BuildExceptionMessage(Exception ex)
        {
            var msg = ex.Message;
            var inner = ex.InnerException;
            while (inner != null) { msg += $" | {inner.Message}"; inner = inner.InnerException; }
            return msg;
        }

        /// <summary>
        /// Returns the caller's schoolId for non-SuperAdmin users,
        /// or null so the service pulls all schools for SuperAdmin.
        /// </summary>
        private Guid? CallerSchoolId()
            => IsSuperAdmin ? null : GetUserSchoolIdOrNullWithValidation();

        /// <summary>Resolves a mandatory schoolId — from token for regular users, from query for SuperAdmin.</summary>
        private Guid? ResolveSchoolId(Guid? overrideSchoolId = null)
        {
            if (IsSuperAdmin) return overrideSchoolId;
            return GetUserSchoolIdOrNullWithValidation();
        }

        #endregion

        // ═════════════════════════════════════════════════════════════════════
        // COMPETENCY ASSESSMENTS — CRUD
        // ═════════════════════════════════════════════════════════════════════

        #region GET — Assessments

        /// <summary>
        /// List all competency assessments.
        /// SuperAdmin: all schools. Others: own school only.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            if (!HasPermission("Assessment.Read"))
                return ForbiddenResponse("You do not have permission to view assessments.");

            try
            {
                var result = await _assessmentService.GetAllAsync(CallerSchoolId());
                return SuccessResponse(result, "Competency assessments retrieved successfully");
            }
            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
        }

        /// <summary>Get a single competency assessment by ID.</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!HasPermission("Assessment.Read"))
                return ForbiddenResponse("You do not have permission to view assessments.");

            try
            {
                var dto = await _assessmentService.GetByIdAsync(id, CallerSchoolId());
                if (dto == null) return NotFoundResponse("Competency assessment not found.");
                return SuccessResponse(dto);
            }
            catch (UnauthorizedAccessException) { return ForbiddenResponse("Access denied."); }
            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
        }

        /// <summary>Get a competency assessment with all its student scores.</summary>
        [HttpGet("{id:guid}/scores")]
        public async Task<IActionResult> GetWithScores(Guid id)
        {
            if (!HasPermission("Assessment.Read"))
                return ForbiddenResponse("You do not have permission to view assessments.");

            try
            {
                var dto = await _assessmentService.GetWithScoresAsync(id, CallerSchoolId());
                if (dto == null) return NotFoundResponse("Competency assessment not found.");
                return SuccessResponse(dto, "Assessment with scores retrieved successfully");
            }
            catch (UnauthorizedAccessException) { return ForbiddenResponse("Access denied."); }
            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
        }

        /// <summary>All competency assessments for a specific class.</summary>
        [HttpGet("class/{classId:guid}")]
        public async Task<IActionResult> GetByClass(Guid classId)
        {
            if (!HasPermission("Assessment.Read"))
                return ForbiddenResponse("You do not have permission to view assessments.");

            try
            {
                var result = await _assessmentService.GetByClassAsync(classId, CallerSchoolId());
                return SuccessResponse(result, "Competency assessments for class retrieved successfully");
            }
            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
        }

        /// <summary>All competency assessments assigned by a specific teacher.</summary>
        [HttpGet("teacher/{teacherId:guid}")]
        public async Task<IActionResult> GetByTeacher(Guid teacherId)
        {
            if (!HasPermission("Assessment.Read"))
                return ForbiddenResponse("You do not have permission to view assessments.");

            try
            {
                var result = await _assessmentService.GetByTeacherAsync(teacherId, CallerSchoolId());
                return SuccessResponse(result, "Competency assessments for teacher retrieved successfully");
            }
            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
        }

        /// <summary>All competency assessments for a given term and academic year.</summary>
        [HttpGet("term/{termId:guid}/academic-year/{academicYearId:guid}")]
        public async Task<IActionResult> GetByTerm(Guid termId, Guid academicYearId)
        {
            if (!HasPermission("Assessment.Read"))
                return ForbiddenResponse("You do not have permission to view assessments.");

            try
            {
                var result = await _assessmentService.GetByTermAsync(termId, academicYearId, CallerSchoolId());
                return SuccessResponse(result, "Competency assessments for term retrieved successfully");
            }
            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
        }

        /// <summary>
        /// Search competency assessments by competency name (partial match).
        /// Requires schoolId query param for SuperAdmin.
        /// </summary>
        [HttpGet("competency")]
        public async Task<IActionResult> GetByCompetencyName(
            [FromQuery] string name,
            [FromQuery] Guid? schoolId = null)
        {
            if (!HasPermission("Assessment.Read"))
                return ForbiddenResponse("You do not have permission to view assessments.");

            if (string.IsNullOrWhiteSpace(name))
                return ValidationErrorResponse("Competency name query parameter is required.");

            try
            {
                var resolvedSchoolId = ResolveSchoolId(schoolId);
                if (resolvedSchoolId == null)
                    return ValidationErrorResponse("SuperAdmin must supply a schoolId query parameter.");

                var result = await _assessmentService.GetByCompetencyNameAsync(name.Trim(), resolvedSchoolId.Value);
                return SuccessResponse(result, $"Competency assessments matching '{name}' retrieved successfully");
            }
            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
        }

        /// <summary>
        /// Filter competency assessments by CBC target level.
        /// Requires schoolId query param for SuperAdmin.
        /// </summary>
        [HttpGet("level/{level}")]
        public async Task<IActionResult> GetByTargetLevel(
            CBCLevel level,
            [FromQuery] Guid? schoolId = null)
        {
            if (!HasPermission("Assessment.Read"))
                return ForbiddenResponse("You do not have permission to view assessments.");

            try
            {
                var resolvedSchoolId = ResolveSchoolId(schoolId);
                if (resolvedSchoolId == null)
                    return ValidationErrorResponse("SuperAdmin must supply a schoolId query parameter.");

                var result = await _assessmentService.GetByTargetLevelAsync(level, resolvedSchoolId.Value);
                return SuccessResponse(result, $"Competency assessments for level '{level}' retrieved successfully");
            }
            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
        }

        /// <summary>
        /// Filter competency assessments by curriculum strand.
        /// Requires schoolId query param for SuperAdmin.
        /// </summary>
        [HttpGet("strand")]
        public async Task<IActionResult> GetByStrand(
            [FromQuery] string strand,
            [FromQuery] Guid? schoolId = null)
        {
            if (!HasPermission("Assessment.Read"))
                return ForbiddenResponse("You do not have permission to view assessments.");

            if (string.IsNullOrWhiteSpace(strand))
                return ValidationErrorResponse("Strand query parameter is required.");

            try
            {
                var resolvedSchoolId = ResolveSchoolId(schoolId);
                if (resolvedSchoolId == null)
                    return ValidationErrorResponse("SuperAdmin must supply a schoolId query parameter.");

                var result = await _assessmentService.GetByStrandAsync(strand.Trim(), resolvedSchoolId.Value);
                return SuccessResponse(result, $"Competency assessments for strand '{strand}' retrieved successfully");
            }
            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
        }

        /// <summary>Published competency assessments visible to students for a class + term.</summary>
        [HttpGet("published/class/{classId:guid}/term/{termId:guid}")]
        public async Task<IActionResult> GetPublished(Guid classId, Guid termId)
        {
            if (!HasPermission("Assessment.Read"))
                return ForbiddenResponse("You do not have permission to view assessments.");

            try
            {
                var result = await _assessmentService.GetPublishedAsync(classId, termId, CallerSchoolId());
                return SuccessResponse(result, "Published competency assessments retrieved successfully");
            }
            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
        }

        #endregion

        #region POST / PUT / PATCH / DELETE — Assessments

        /// <summary>Create a new competency assessment.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCompetencyAssessmentRequest request)
        {
            if (!HasAnyPermission("Assessment.Write", "Assessment.Create"))
                return ForbiddenResponse("You do not have permission to create assessments.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                Guid schoolId;
                if (IsSuperAdmin)
                {
                    if (request.SchoolId == null || request.SchoolId == Guid.Empty)
                        return ValidationErrorResponse("SuperAdmin must supply a SchoolId.");
                    schoolId = request.SchoolId.Value;
                }
                else
                {
                    schoolId = GetUserSchoolIdOrNullWithValidation()!.Value;
                }

                var dto = await _assessmentService.CreateAsync(request, schoolId);

                await LogUserActivityAsync(
                    "competency_assessment.create",
                    $"Created competency assessment '{dto.Title}' ({dto.Id}) — {dto.CompetencyName}");

                return CreatedResponse(dto, "Competency assessment created successfully");
            }
            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
        }

        /// <summary>Update an existing competency assessment.</summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(
            Guid id, [FromBody] UpdateCompetencyAssessmentRequest request)
        {
            if (!HasAnyPermission("Assessment.Write", "Assessment.Update"))
                return ForbiddenResponse("You do not have permission to update assessments.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var dto = await _assessmentService.UpdateAsync(id, request, CallerSchoolId());

                await LogUserActivityAsync(
                    "competency_assessment.update",
                    $"Updated competency assessment '{dto.Title}' ({dto.Id})");

                return SuccessResponse(dto, "Competency assessment updated successfully");
            }
            catch (KeyNotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException) { return ForbiddenResponse("Access denied."); }
            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
        }

        /// <summary>Publish or un-publish a competency assessment.</summary>
        [HttpPatch("{id:guid}/publish")]
        public async Task<IActionResult> Publish(
            Guid id, [FromBody] UpdateAssessmentPublishRequest request)
        {
            if (!HasAnyPermission("Assessment.Write", "Assessment.Publish"))
                return ForbiddenResponse("You do not have permission to publish assessments.");

            try
            {
                await _assessmentService.PublishAsync(id, request.IsPublished, CallerSchoolId());

                var action = request.IsPublished ? "published" : "unpublished";

                await LogUserActivityAsync(
                    "competency_assessment.publish",
                    $"{char.ToUpper(action[0]) + action[1..]} competency assessment {id}");

                return SuccessResponse<object?>(null, $"Assessment {action} successfully");
            }
            catch (KeyNotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException) { return ForbiddenResponse("Access denied."); }
            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
        }

        /// <summary>
        /// Permanently delete a competency assessment and all its scores.
        /// SchoolAdmin or SuperAdmin only.
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!HasPermission("Assessment.Delete"))
                return ForbiddenResponse("You do not have permission to delete assessments.");

            if (!IsSuperAdmin && !HasRole("SchoolAdmin"))
                return ForbiddenResponse("Only School Administrators or Super Administrators can delete assessments.");

            try
            {
                // Remove scores first to avoid FK violations
                await _scoreService.DeleteByAssessmentAsync(id, CallerSchoolId());
                await _assessmentService.DeleteAsync(id, CallerSchoolId());

                await LogUserActivityAsync(
                    "competency_assessment.delete",
                    $"Deleted competency assessment {id} and all its scores");

                return SuccessResponse<object?>(null, "Competency assessment deleted successfully");
            }
            catch (KeyNotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException) { return ForbiddenResponse("Access denied."); }
            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
        }

        #endregion

        // ═════════════════════════════════════════════════════════════════════
        // COMPETENCY ASSESSMENT SCORES
        // ═════════════════════════════════════════════════════════════════════

        #region GET — Scores

        /// <summary>All scores for a specific competency assessment.</summary>
        [HttpGet("{assessmentId:guid}/score-entries")]
        public async Task<IActionResult> GetScoresByAssessment(Guid assessmentId)
        {
            if (!HasPermission("Assessment.Read"))
                return ForbiddenResponse("You do not have permission to view scores.");

            try
            {
                var result = await _scoreService.GetByAssessmentAsync(assessmentId, CallerSchoolId());
                return SuccessResponse(result, "Scores retrieved successfully");
            }
            catch (KeyNotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException) { return ForbiddenResponse("Access denied."); }
            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
        }

        /// <summary>All competency scores for a specific student.</summary>
        [HttpGet("scores/student/{studentId:guid}")]
        public async Task<IActionResult> GetScoresByStudent(Guid studentId)
        {
            if (!HasPermission("Assessment.Read"))
                return ForbiddenResponse("You do not have permission to view scores.");

            try
            {
                var result = await _scoreService.GetByStudentAsync(studentId, CallerSchoolId());
                return SuccessResponse(result, "Student competency scores retrieved successfully");
            }
            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
        }

        /// <summary>All competency scores for a student within a specific term.</summary>
        [HttpGet("scores/student/{studentId:guid}/term/{termId:guid}")]
        public async Task<IActionResult> GetScoresByStudentAndTerm(Guid studentId, Guid termId)
        {
            if (!HasPermission("Assessment.Read"))
                return ForbiddenResponse("You do not have permission to view scores.");

            try
            {
                var result = await _scoreService.GetByStudentAndTermAsync(studentId, termId, CallerSchoolId());
                return SuccessResponse(result, "Student term competency scores retrieved successfully");
            }
            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
        }

        /// <summary>Get a single competency score entry by ID.</summary>
        [HttpGet("scores/{scoreId:guid}")]
        public async Task<IActionResult> GetScoreById(Guid scoreId)
        {
            if (!HasPermission("Assessment.Read"))
                return ForbiddenResponse("You do not have permission to view scores.");

            try
            {
                var dto = await _scoreService.GetByIdAsync(scoreId, CallerSchoolId());
                if (dto == null) return NotFoundResponse("Score entry not found.");
                return SuccessResponse(dto);
            }
            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
        }

        /// <summary>
        /// Filter scores for an assessment by rating band
        /// (Exceeds | Meets | Approaching | Below).
        /// </summary>
        [HttpGet("{assessmentId:guid}/scores/rating/{rating}")]
        public async Task<IActionResult> GetScoresByRating(Guid assessmentId, string rating)
        {
            if (!HasPermission("Assessment.Read"))
                return ForbiddenResponse("You do not have permission to view scores.");

            try
            {
                var result = await _scoreService.GetByRatingAsync(assessmentId, rating, CallerSchoolId());
                return SuccessResponse(result, $"Scores with rating '{rating}' retrieved successfully");
            }
            catch (KeyNotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException) { return ForbiddenResponse("Access denied."); }
            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
        }

        /// <summary>All scores recorded by a specific assessor (teacher).</summary>
        [HttpGet("scores/assessor/{assessorId:guid}")]
        public async Task<IActionResult> GetScoresByAssessor(Guid assessorId)
        {
            if (!HasPermission("Assessment.Read"))
                return ForbiddenResponse("You do not have permission to view scores.");

            try
            {
                var result = await _scoreService.GetByAssessorAsync(assessorId, CallerSchoolId());
                return SuccessResponse(result, "Assessor scores retrieved successfully");
            }
            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
        }

        #endregion

        #region POST / PUT / PATCH / DELETE — Scores

        /// <summary>
        /// Record a competency score for one student.
        /// Teacher, SchoolAdmin, or SuperAdmin.
        /// </summary>
        [HttpPost("scores")]
        public async Task<IActionResult> CreateScore(
            [FromBody] CreateCompetencyAssessmentScoreRequest request)
        {
            if (!HasAnyPermission("Assessment.Write", "Assessment.Grade"))
                return ForbiddenResponse("You do not have permission to record competency scores.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var assessorId = CurrentUserId;
                var dto = await _scoreService.CreateAsync(request, assessorId, CallerSchoolId());

                await LogUserActivityAsync(
                    "competency_score.create",
                    $"Recorded competency score for student {dto.StudentId} " +
                    $"on assessment {dto.CompetencyAssessmentId}: Rating={dto.Rating}");

                return CreatedResponse(dto, "Competency score recorded successfully");
            }
            catch (KeyNotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException) { return ForbiddenResponse("Access denied."); }
            catch (InvalidOperationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
        }

        /// <summary>Update an existing competency score entry.</summary>
        [HttpPut("scores/{scoreId:guid}")]
        public async Task<IActionResult> UpdateScore(
            Guid scoreId,
            [FromBody] UpdateCompetencyAssessmentScoreRequest request)
        {
            if (!HasAnyPermission("Assessment.Write", "Assessment.Grade"))
                return ForbiddenResponse("You do not have permission to update scores.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var assessorId = CurrentUserId;
                var dto = await _scoreService.UpdateAsync(scoreId, request, assessorId, CallerSchoolId());

                await LogUserActivityAsync(
                    "competency_score.update",
                    $"Updated competency score {scoreId} — Rating={dto.Rating}");

                return SuccessResponse(dto, "Competency score updated successfully");
            }
            catch (KeyNotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException) { return ForbiddenResponse("Access denied."); }
            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
        }

        /// <summary>
        /// Bulk finalize (or un-finalize) scores for a competency assessment.
        /// Typically called when the assessor has finished evaluating all students.
        /// </summary>
        [HttpPatch("scores/bulk-finalize")]
        public async Task<IActionResult> BulkFinalizeScores(
            [FromBody] BulkFinalizeCompetencyScoresRequest request)
        {
            if (!HasAnyPermission("Assessment.Write", "Assessment.Grade"))
                return ForbiddenResponse("You do not have permission to finalize scores.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                await _scoreService.BulkFinalizeAsync(request, CallerSchoolId());

                var finalized = request.Scores.Count(s => s.IsFinalized);
                var unfinalized = request.Scores.Count - finalized;

                await LogUserActivityAsync(
                    "competency_score.bulk_finalize",
                    $"Bulk finalize: {finalized} finalized, {unfinalized} un-finalized");

                return SuccessResponse<object?>(null,
                    $"Scores updated: {finalized} finalized, {unfinalized} un-finalized");
            }
            catch (KeyNotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException) { return ForbiddenResponse("Access denied."); }
            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
        }

        /// <summary>
        /// Delete a single competency score entry.
        /// SchoolAdmin or SuperAdmin only.
        /// </summary>
        [HttpDelete("scores/{scoreId:guid}")]
        public async Task<IActionResult> DeleteScore(Guid scoreId)
        {
            if (!HasPermission("Assessment.Delete"))
                return ForbiddenResponse("You do not have permission to delete scores.");

            if (!IsSuperAdmin && !HasRole("SchoolAdmin"))
                return ForbiddenResponse("Only School Administrators or Super Administrators can delete scores.");

            try
            {
                await _scoreService.DeleteAsync(scoreId, CallerSchoolId());

                await LogUserActivityAsync(
                    "competency_score.delete",
                    $"Deleted competency assessment score {scoreId}");

                return SuccessResponse<object?>(null, "Score deleted successfully");
            }
            catch (KeyNotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException) { return ForbiddenResponse("Access denied."); }
            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
        }

        #endregion
    }
}