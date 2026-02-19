//using Devken.CBC.SchoolManagement.Api.Controllers.Common;
//using Devken.CBC.SchoolManagement.Application.DTOs.Assessments;
//using Devken.CBC.SchoolManagement.Application.Service.Activities;
//using Devken.CBC.SchoolManagement.Application.Service.Assessments;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using System;
//using System.Threading.Tasks;

//namespace Devken.CBC.SchoolManagement.Api.Controllers.Assessments
//{
//    /// <summary>
//    /// Manages Summative Assessments (exams) and their student scores.
//    ///
//    /// Access rules:
//    ///   SuperAdmin  — full read/write across all schools.
//    ///   SchoolAdmin — full read/write within their school.
//    ///   Teacher     — create / update / publish own assessments; grade students.
//    ///   Student     — read published assessments and their own scores only.
//    /// </summary>
//    [Route("api/[controller]")]
//    [ApiController]
//    [Authorize]
//    public class SummativeAssessmentsController : BaseApiController
//    {
//        private readonly ISummativeAssessmentService _assessmentService;
//        private readonly ISummativeAssessmentScoreService _scoreService;

//        public SummativeAssessmentsController(
//            ISummativeAssessmentService assessmentService,
//            ISummativeAssessmentScoreService scoreService,
//            IUserActivityService? activityService = null)
//            : base(activityService)
//        {
//            _assessmentService = assessmentService ?? throw new ArgumentNullException(nameof(assessmentService));
//            _scoreService = scoreService ?? throw new ArgumentNullException(nameof(scoreService));
//        }

//        // ─────────────────────────────────────────────────────────────────────
//        #region Helpers

//        private static string BuildExceptionMessage(Exception ex)
//        {
//            var msg = ex.Message;
//            var inner = ex.InnerException;
//            while (inner != null) { msg += $" | {inner.Message}"; inner = inner.InnerException; }
//            return msg;
//        }

//        /// <summary>
//        /// Returns the caller's schoolId for non-SuperAdmin users,
//        /// or null so the service layer pulls all schools for SuperAdmin.
//        /// </summary>
//        private Guid? CallerSchoolId()
//            => IsSuperAdmin ? null : GetUserSchoolIdOrNullWithValidation();

//        #endregion

//        // ═════════════════════════════════════════════════════════════════════
//        // SUMMATIVE ASSESSMENTS — CRUD
//        // ═════════════════════════════════════════════════════════════════════

//        #region GET — Assessments

//        /// <summary>
//        /// List all summative assessments.
//        /// SuperAdmin: all schools. Others: own school only.
//        /// </summary>
//        [HttpGet]
//        public async Task<IActionResult> GetAll()
//        {
//            if (!HasPermission("Assessment.Read"))
//                return ForbiddenResponse("You do not have permission to view assessments.");

//            try
//            {
//                var result = await _assessmentService.GetAllAsync(CallerSchoolId());
//                return SuccessResponse(result, "Summative assessments retrieved successfully");
//            }
//            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
//        }

//        /// <summary>Get a single summative assessment by ID.</summary>
//        [HttpGet("{id:guid}")]
//        public async Task<IActionResult> GetById(Guid id)
//        {
//            if (!HasPermission("Assessment.Read"))
//                return ForbiddenResponse("You do not have permission to view assessments.");

//            try
//            {
//                var dto = await _assessmentService.GetByIdAsync(id, CallerSchoolId());
//                if (dto == null) return NotFoundResponse("Summative assessment not found.");
//                return SuccessResponse(dto);
//            }
//            catch (UnauthorizedAccessException) { return ForbiddenResponse("Access denied."); }
//            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
//        }

//        /// <summary>Get a summative assessment with all its student scores.</summary>
//        [HttpGet("{id:guid}/scores")]
//        public async Task<IActionResult> GetWithScores(Guid id)
//        {
//            if (!HasPermission("Assessment.Read"))
//                return ForbiddenResponse("You do not have permission to view assessments.");

//            try
//            {
//                var dto = await _assessmentService.GetWithScoresAsync(id, CallerSchoolId());
//                if (dto == null) return NotFoundResponse("Summative assessment not found.");
//                return SuccessResponse(dto, "Assessment with scores retrieved successfully");
//            }
//            catch (UnauthorizedAccessException) { return ForbiddenResponse("Access denied."); }
//            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
//        }

//        /// <summary>All summative assessments for a specific class.</summary>
//        [HttpGet("class/{classId:guid}")]
//        public async Task<IActionResult> GetByClass(Guid classId)
//        {
//            if (!HasPermission("Assessment.Read"))
//                return ForbiddenResponse("You do not have permission to view assessments.");

//            try
//            {
//                var result = await _assessmentService.GetByClassAsync(classId, CallerSchoolId());
//                return SuccessResponse(result, "Summative assessments for class retrieved successfully");
//            }
//            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
//        }

//        /// <summary>All summative assessments assigned by a specific teacher.</summary>
//        [HttpGet("teacher/{teacherId:guid}")]
//        public async Task<IActionResult> GetByTeacher(Guid teacherId)
//        {
//            if (!HasPermission("Assessment.Read"))
//                return ForbiddenResponse("You do not have permission to view assessments.");

//            try
//            {
//                var result = await _assessmentService.GetByTeacherAsync(teacherId, CallerSchoolId());
//                return SuccessResponse(result, "Summative assessments for teacher retrieved successfully");
//            }
//            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
//        }

//        /// <summary>All summative assessments for a given term and academic year.</summary>
//        [HttpGet("term/{termId:guid}/academic-year/{academicYearId:guid}")]
//        public async Task<IActionResult> GetByTerm(Guid termId, Guid academicYearId)
//        {
//            if (!HasPermission("Assessment.Read"))
//                return ForbiddenResponse("You do not have permission to view assessments.");

//            try
//            {
//                var result = await _assessmentService.GetByTermAsync(termId, academicYearId, CallerSchoolId());
//                return SuccessResponse(result, "Summative assessments for term retrieved successfully");
//            }
//            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
//        }

//        /// <summary>All summative assessments of a specific exam type (e.g. EndTerm, MidTerm, Final).</summary>
//        [HttpGet("exam-type/{examType}")]
//        public async Task<IActionResult> GetByExamType(string examType)
//        {
//            if (!HasPermission("Assessment.Read"))
//                return ForbiddenResponse("You do not have permission to view assessments.");

//            try
//            {
//                var result = await _assessmentService.GetByExamTypeAsync(examType, CallerSchoolId());
//                return SuccessResponse(result, "Summative assessments for exam type retrieved successfully");
//            }
//            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
//        }

//        /// <summary>Published summative assessments visible to students for a class + term.</summary>
//        [HttpGet("published/class/{classId:guid}/term/{termId:guid}")]
//        public async Task<IActionResult> GetPublished(Guid classId, Guid termId)
//        {
//            if (!HasPermission("Assessment.Read"))
//                return ForbiddenResponse("You do not have permission to view assessments.");

//            try
//            {
//                var result = await _assessmentService.GetPublishedAsync(classId, termId, CallerSchoolId());
//                return SuccessResponse(result, "Published summative assessments retrieved successfully");
//            }
//            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
//        }

//        #endregion

//        #region POST / PUT / PATCH / DELETE — Assessments

//        /// <summary>Create a new summative assessment.</summary>
//        [HttpPost]
//        public async Task<IActionResult> Create([FromBody] CreateSummativeAssessmentRequest request)
//        {
//            if (!HasAnyPermission("Assessment.Write", "Assessment.Create"))
//                return ForbiddenResponse("You do not have permission to create assessments.");

//            if (!ModelState.IsValid)
//                return ValidationErrorResponse(ModelState);

//            try
//            {
//                Guid schoolId;
//                if (IsSuperAdmin)
//                {
//                    if (request.SchoolId == null || request.SchoolId == Guid.Empty)
//                        return ValidationErrorResponse("SuperAdmin must supply a SchoolId.");
//                    schoolId = request.SchoolId.Value;
//                }
//                else
//                {
//                    schoolId = GetUserSchoolIdOrNullWithValidation()!.Value;
//                }

//                var dto = await _assessmentService.CreateAsync(request, schoolId);

//                await LogUserActivityAsync(
//                    "summative_assessment.create",
//                    $"Created summative assessment '{dto.Title}' ({dto.Id})");

//                return CreatedResponse(dto, "Summative assessment created successfully");
//            }
//            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
//        }

//        /// <summary>Update an existing summative assessment.</summary>
//        [HttpPut("{id:guid}")]
//        public async Task<IActionResult> Update(
//            Guid id, [FromBody] UpdateSummativeAssessmentRequest request)
//        {
//            if (!HasAnyPermission("Assessment.Write", "Assessment.Update"))
//                return ForbiddenResponse("You do not have permission to update assessments.");

//            if (!ModelState.IsValid)
//                return ValidationErrorResponse(ModelState);

//            try
//            {
//                var dto = await _assessmentService.UpdateAsync(id, request, CallerSchoolId());

//                await LogUserActivityAsync(
//                    "summative_assessment.update",
//                    $"Updated summative assessment '{dto.Title}' ({dto.Id})");

//                return SuccessResponse(dto, "Summative assessment updated successfully");
//            }
//            catch (KeyNotFoundException ex) { return NotFoundResponse(ex.Message); }
//            catch (UnauthorizedAccessException) { return ForbiddenResponse("Access denied."); }
//            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
//        }

//        /// <summary>Publish or un-publish a summative assessment.</summary>
//        [HttpPatch("{id:guid}/publish")]
//        public async Task<IActionResult> Publish(
//            Guid id, [FromBody] UpdateAssessmentPublishRequest request)
//        {
//            if (!HasAnyPermission("Assessment.Write", "Assessment.Publish"))
//                return ForbiddenResponse("You do not have permission to publish assessments.");

//            try
//            {
//                await _assessmentService.PublishAsync(id, request.IsPublished, CallerSchoolId());

//                var action = request.IsPublished ? "published" : "unpublished";

//                await LogUserActivityAsync(
//                    "summative_assessment.publish",
//                    $"{char.ToUpper(action[0]) + action[1..]} summative assessment {id}");

//                return SuccessResponse<object?>(null, $"Assessment {action} successfully");
//            }
//            catch (KeyNotFoundException ex) { return NotFoundResponse(ex.Message); }
//            catch (UnauthorizedAccessException) { return ForbiddenResponse("Access denied."); }
//            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
//        }

//        /// <summary>
//        /// Permanently delete a summative assessment.
//        /// SchoolAdmin or SuperAdmin only.
//        /// </summary>
//        [HttpDelete("{id:guid}")]
//        public async Task<IActionResult> Delete(Guid id)
//        {
//            if (!HasPermission("Assessment.Delete"))
//                return ForbiddenResponse("You do not have permission to delete assessments.");

//            if (!IsSuperAdmin && !HasRole("SchoolAdmin"))
//                return ForbiddenResponse("Only School Administrators or Super Administrators can delete assessments.");

//            try
//            {
//                // Remove scores first to avoid FK violation
//                await _scoreService.DeleteByAssessmentAsync(id, CallerSchoolId());
//                await _assessmentService.DeleteAsync(id, CallerSchoolId());

//                await LogUserActivityAsync(
//                    "summative_assessment.delete",
//                    $"Deleted summative assessment {id} and all its scores");

//                return SuccessResponse<object?>(null, "Summative assessment deleted successfully");
//            }
//            catch (KeyNotFoundException ex) { return NotFoundResponse(ex.Message); }
//            catch (UnauthorizedAccessException) { return ForbiddenResponse("Access denied."); }
//            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
//        }

//        #endregion

//        // ═════════════════════════════════════════════════════════════════════
//        // SUMMATIVE ASSESSMENT SCORES
//        // ═════════════════════════════════════════════════════════════════════

//        #region GET — Scores

//        /// <summary>All scores for a specific summative assessment.</summary>
//        [HttpGet("{assessmentId:guid}/score-entries")]
//        public async Task<IActionResult> GetScoresByAssessment(Guid assessmentId)
//        {
//            if (!HasPermission("Assessment.Read"))
//                return ForbiddenResponse("You do not have permission to view scores.");

//            try
//            {
//                var result = await _scoreService.GetByAssessmentAsync(assessmentId, CallerSchoolId());
//                return SuccessResponse(result, "Scores retrieved successfully");
//            }
//            catch (KeyNotFoundException ex) { return NotFoundResponse(ex.Message); }
//            catch (UnauthorizedAccessException) { return ForbiddenResponse("Access denied."); }
//            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
//        }

//        /// <summary>All summative scores for a specific student.</summary>
//        [HttpGet("scores/student/{studentId:guid}")]
//        public async Task<IActionResult> GetScoresByStudent(Guid studentId)
//        {
//            if (!HasPermission("Assessment.Read"))
//                return ForbiddenResponse("You do not have permission to view scores.");

//            try
//            {
//                var result = await _scoreService.GetByStudentAsync(studentId, CallerSchoolId());
//                return SuccessResponse(result, "Student scores retrieved successfully");
//            }
//            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
//        }

//        /// <summary>All summative scores for a student within a specific term.</summary>
//        [HttpGet("scores/student/{studentId:guid}/term/{termId:guid}")]
//        public async Task<IActionResult> GetScoresByStudentAndTerm(Guid studentId, Guid termId)
//        {
//            if (!HasPermission("Assessment.Read"))
//                return ForbiddenResponse("You do not have permission to view scores.");

//            try
//            {
//                var result = await _scoreService.GetByStudentAndTermAsync(studentId, termId, CallerSchoolId());
//                return SuccessResponse(result, "Student term scores retrieved successfully");
//            }
//            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
//        }

//        /// <summary>Get a single score entry by ID.</summary>
//        [HttpGet("scores/{scoreId:guid}")]
//        public async Task<IActionResult> GetScoreById(Guid scoreId)
//        {
//            if (!HasPermission("Assessment.Read"))
//                return ForbiddenResponse("You do not have permission to view scores.");

//            try
//            {
//                var dto = await _scoreService.GetByIdAsync(scoreId, CallerSchoolId());
//                if (dto == null) return NotFoundResponse("Score entry not found.");
//                return SuccessResponse(dto);
//            }
//            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
//        }

//        #endregion

//        #region POST / PUT / PATCH / DELETE — Scores

//        /// <summary>
//        /// Record a score for one student on a summative assessment.
//        /// Teacher, SchoolAdmin, or SuperAdmin.
//        /// </summary>
//        [HttpPost("scores")]
//        public async Task<IActionResult> CreateScore(
//            [FromBody] CreateSummativeAssessmentScoreRequest request)
//        {
//            if (!HasAnyPermission("Assessment.Write", "Assessment.Grade"))
//                return ForbiddenResponse("You do not have permission to grade assessments.");

//            if (!ModelState.IsValid)
//                return ValidationErrorResponse(ModelState);

//            try
//            {
//                var gradedById = CurrentUserId;
//                var dto = await _scoreService.CreateAsync(request, gradedById, CallerSchoolId());

//                await LogUserActivityAsync(
//                    "summative_score.create",
//                    $"Graded student {dto.StudentId} on assessment {dto.SummativeAssessmentId}: " +
//                    $"{dto.TotalScore}/{dto.MaximumTotalScore} ({dto.Percentage:F1}%)");

//                return CreatedResponse(dto, "Score recorded successfully");
//            }
//            catch (KeyNotFoundException ex) { return NotFoundResponse(ex.Message); }
//            catch (UnauthorizedAccessException) { return ForbiddenResponse("Access denied."); }
//            catch (InvalidOperationException ex) { return ValidationErrorResponse(ex.Message); }
//            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
//        }

//        /// <summary>Update an existing score entry.</summary>
//        [HttpPut("scores/{scoreId:guid}")]
//        public async Task<IActionResult> UpdateScore(
//            Guid scoreId,
//            [FromBody] UpdateSummativeAssessmentScoreRequest request)
//        {
//            if (!HasAnyPermission("Assessment.Write", "Assessment.Grade"))
//                return ForbiddenResponse("You do not have permission to update scores.");

//            if (!ModelState.IsValid)
//                return ValidationErrorResponse(ModelState);

//            try
//            {
//                var gradedById = CurrentUserId;
//                var dto = await _scoreService.UpdateAsync(scoreId, request, gradedById, CallerSchoolId());

//                await LogUserActivityAsync(
//                    "summative_score.update",
//                    $"Updated score {scoreId} — {dto.TotalScore}/{dto.MaximumTotalScore}");

//                return SuccessResponse(dto, "Score updated successfully");
//            }
//            catch (KeyNotFoundException ex) { return NotFoundResponse(ex.Message); }
//            catch (UnauthorizedAccessException) { return ForbiddenResponse("Access denied."); }
//            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
//        }

//        /// <summary>
//        /// Recalculate class positions for all scores of a summative assessment,
//        /// ranked by total score descending.
//        /// Teacher, SchoolAdmin, or SuperAdmin.
//        /// </summary>
//        [HttpPatch("{assessmentId:guid}/recalculate-positions")]
//        public async Task<IActionResult> RecalculatePositions(Guid assessmentId)
//        {
//            if (!HasAnyPermission("Assessment.Write", "Assessment.Grade"))
//                return ForbiddenResponse("You do not have permission to update scores.");

//            try
//            {
//                await _scoreService.RecalculatePositionsAsync(assessmentId, CallerSchoolId());

//                await LogUserActivityAsync(
//                    "summative_score.recalculate_positions",
//                    $"Recalculated class positions for assessment {assessmentId}");

//                return SuccessResponse<object?>(null, "Class positions recalculated successfully");
//            }
//            catch (KeyNotFoundException ex) { return NotFoundResponse(ex.Message); }
//            catch (UnauthorizedAccessException) { return ForbiddenResponse("Access denied."); }
//            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
//        }

//        /// <summary>
//        /// Delete a single score entry.
//        /// SchoolAdmin or SuperAdmin only.
//        /// </summary>
//        [HttpDelete("scores/{scoreId:guid}")]
//        public async Task<IActionResult> DeleteScore(Guid scoreId)
//        {
//            if (!HasPermission("Assessment.Delete"))
//                return ForbiddenResponse("You do not have permission to delete scores.");

//            if (!IsSuperAdmin && !HasRole("SchoolAdmin"))
//                return ForbiddenResponse("Only School Administrators or Super Administrators can delete scores.");

//            try
//            {
//                await _scoreService.DeleteAsync(scoreId, CallerSchoolId());

//                await LogUserActivityAsync(
//                    "summative_score.delete",
//                    $"Deleted summative assessment score {scoreId}");

//                return SuccessResponse<object?>(null, "Score deleted successfully");
//            }
//            catch (KeyNotFoundException ex) { return NotFoundResponse(ex.Message); }
//            catch (UnauthorizedAccessException) { return ForbiddenResponse("Access denied."); }
//            catch (Exception ex) { return InternalServerErrorResponse(BuildExceptionMessage(ex)); }
//        }

//        #endregion
//    }
//}