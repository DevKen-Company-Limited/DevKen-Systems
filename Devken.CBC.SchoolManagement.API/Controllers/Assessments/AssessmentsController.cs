//using Devken.CBC.SchoolManagement.Api.Controllers.Common;
//using Devken.CBC.SchoolManagement.Application.DTOs.Assessments;
//using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
//using Devken.CBC.SchoolManagement.Application.Service.Activities;
//using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Devken.CBC.SchoolManagement.Api.Controllers.Assessments
//{
//    /// <summary>
//    /// Manages assessments (formative, summative, competency-based).
//    ///
//    /// Access rules:
//    ///   SuperAdmin  – full read/write across all schools.
//    ///   SchoolAdmin – full read/write within their own school.
//    ///   Teacher     – create / update / publish their own assessments within their school.
//    ///   Student     – read published assessments for their class only.
//    /// </summary>
//    [Route("api/[controller]")]
//    [ApiController]
//    [Authorize]
//    public class AssessmentsController : BaseApiController
//    {
//        private readonly IRepositoryManager _repositories;

//        public AssessmentsController(
//            IRepositoryManager repositories,
//            IUserActivityService? activityService = null)
//            : base(activityService)
//        {
//            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
//        }

//        // ─────────────────────────────────────────────────────────────────────
//        #region Helpers

//        private static string GetFullExceptionMessage(Exception ex)
//        {
//            var message = ex.Message;
//            var inner = ex.InnerException;
//            while (inner != null)
//            {
//                message += $" | Detail: {inner.Message}";
//                inner = inner.InnerException;
//            }
//            return message;
//        }

//        /// <summary>Maps an Assessment1 entity to its response DTO.</summary>
//        private static AssessmentDto ToDto(Assessment1 a) => new()
//        {
//            Id = a.Id,
//            Title = a.Title,
//            Description = a.Description,
//            TeacherId = a.TeacherId,
//            TeacherName = a.Teacher != null
//                                   ? $"{a.Teacher.FirstName} {a.Teacher.LastName}".Trim()
//                                   : null,
//            SubjectId = a.SubjectId,
//            SubjectName = a.Subject?.Name,
//            ClassId = a.ClassId,
//            ClassName = a.Class?.Name,
//            TermId = a.TermId,
//            TermName = a.Term?.Name,
//            AcademicYearId = a.AcademicYearId,
//            AcademicYearName = a.AcademicYear?.Name,
//            AssessmentDate = a.AssessmentDate,
//            MaximumScore = a.MaximumScore,
//            AssessmentType = a.AssessmentType,
//            IsPublished = a.IsPublished,
//            PublishedDate = a.PublishedDate,
//            SchoolId = a.TenantId,
//            CreatedOn = a.CreatedOn
//        };

//        /// <summary>
//        /// Verifies the current user belongs to the same school as the target assessment
//        /// or is a SuperAdmin. Returns a ForbiddenResponse when access is denied.
//        /// </summary>
//        private IActionResult? ValidateAssessmentSchoolAccess(Assessment1 assessment)
//        {
//            if (IsSuperAdmin) return null;
//            return ValidateSchoolAccess(assessment.TenantId);
//        }

//        #endregion

//        // ─────────────────────────────────────────────────────────────────────
//        #region GET

//        /// <summary>
//        /// Returns all assessments.
//        /// SuperAdmin: all schools. Others: own school only.
//        /// </summary>
//        [HttpGet]
//        public async Task<IActionResult> GetAll()
//        {
//            if (!HasPermission("Assessment.Read"))
//                return ForbiddenResponse("You do not have permission to view assessments.");

//            try
//            {
//                IEnumerable<Assessment1> assessments;

//                if (IsSuperAdmin)
//                {
//                    // Not tenant-scoped — pull every record
//                    assessments = await _repositories.Assessment.GetAllAsync(trackChanges: false);
//                }
//                else
//                {
//                    // Scoped to the caller's school via TenantId
//                    var schoolId = GetUserSchoolIdOrNullWithValidation()!.Value;
//                    assessments = await _repositories.Assessment.GetBySchoolAsync(schoolId, trackChanges: false);
//                }

//                return SuccessResponse(assessments.Select(ToDto), "Assessments retrieved successfully");
//            }
//            catch (Exception ex)
//            {
//                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
//            }
//        }

//        /// <summary>Get a single assessment by ID.</summary>
//        [HttpGet("{id:guid}")]
//        public async Task<IActionResult> GetById(Guid id)
//        {
//            if (!HasPermission("Assessment.Read"))
//                return ForbiddenResponse("You do not have permission to view assessments.");

//            try
//            {
//                var assessment = await _repositories.Assessment.GetByIdAsync(id, trackChanges: false);
//                if (assessment == null)
//                    return NotFoundResponse("Assessment not found.");

//                var accessError = ValidateAssessmentSchoolAccess(assessment);
//                if (accessError != null) return accessError;

//                return SuccessResponse(ToDto(assessment));
//            }
//            catch (Exception ex)
//            {
//                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
//            }
//        }

//        /// <summary>Get an assessment including all its grades.</summary>
//        [HttpGet("{id:guid}/grades")]
//        public async Task<IActionResult> GetWithGrades(Guid id)
//        {
//            if (!HasPermission("Assessment.Read"))
//                return ForbiddenResponse("You do not have permission to view assessments.");

//            try
//            {
//                var assessment = await _repositories.Assessment.GetWithGradesAsync(id, trackChanges: false);
//                if (assessment == null)
//                    return NotFoundResponse("Assessment not found.");

//                var accessError = ValidateAssessmentSchoolAccess(assessment);
//                if (accessError != null) return accessError;

//                var result = new
//                {
//                    Assessment = ToDto(assessment),
//                    Grades = assessment.Grades.Select(g => new
//                    {
//                        g.Id,
//                        g.StudentId,
//                        g.Score,
//                        g.Remarks,
//                        g.CreatedOn
//                    })
//                };

//                return SuccessResponse(result, "Assessment with grades retrieved successfully");
//            }
//            catch (Exception ex)
//            {
//                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
//            }
//        }

//        /// <summary>All assessments for a specific class.</summary>
//        [HttpGet("class/{classId:guid}")]
//        public async Task<IActionResult> GetByClass(Guid classId)
//        {
//            if (!HasPermission("Assessment.Read"))
//                return ForbiddenResponse("You do not have permission to view assessments.");

//            try
//            {
//                var assessments = await _repositories.Assessment.GetByClassAsync(classId, trackChanges: false);

//                if (!IsSuperAdmin)
//                {
//                    var schoolId = GetUserSchoolIdOrNullWithValidation()!.Value;
//                    assessments = assessments.Where(a => a.TenantId == schoolId);
//                }

//                return SuccessResponse(assessments.Select(ToDto), "Assessments for class retrieved successfully");
//            }
//            catch (Exception ex)
//            {
//                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
//            }
//        }

//        /// <summary>All assessments assigned by a specific teacher.</summary>
//        [HttpGet("teacher/{teacherId:guid}")]
//        public async Task<IActionResult> GetByTeacher(Guid teacherId)
//        {
//            if (!HasPermission("Assessment.Read"))
//                return ForbiddenResponse("You do not have permission to view assessments.");

//            try
//            {
//                var assessments = await _repositories.Assessment.GetByTeacherAsync(teacherId, trackChanges: false);

//                if (!IsSuperAdmin)
//                {
//                    var schoolId = GetUserSchoolIdOrNullWithValidation()!.Value;
//                    assessments = assessments.Where(a => a.TenantId == schoolId);
//                }

//                return SuccessResponse(assessments.Select(ToDto), "Assessments for teacher retrieved successfully");
//            }
//            catch (Exception ex)
//            {
//                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
//            }
//        }

//        /// <summary>All assessments for a specific term / academic year.</summary>
//        [HttpGet("term/{termId:guid}/academic-year/{academicYearId:guid}")]
//        public async Task<IActionResult> GetByTerm(Guid termId, Guid academicYearId)
//        {
//            if (!HasPermission("Assessment.Read"))
//                return ForbiddenResponse("You do not have permission to view assessments.");

//            try
//            {
//                var assessments = await _repositories.Assessment.GetByTermAsync(termId, academicYearId, trackChanges: false);

//                if (!IsSuperAdmin)
//                {
//                    var schoolId = GetUserSchoolIdOrNullWithValidation()!.Value;
//                    assessments = assessments.Where(a => a.TenantId == schoolId);
//                }

//                return SuccessResponse(assessments.Select(ToDto), "Assessments for term retrieved successfully");
//            }
//            catch (Exception ex)
//            {
//                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
//            }
//        }

//        /// <summary>Published assessments visible to students for a given class + term.</summary>
//        [HttpGet("published/class/{classId:guid}/term/{termId:guid}")]
//        public async Task<IActionResult> GetPublished(Guid classId, Guid termId)
//        {
//            if (!HasPermission("Assessment.Read"))
//                return ForbiddenResponse("You do not have permission to view assessments.");

//            try
//            {
//                var assessments = await _repositories.Assessment.GetPublishedAsync(classId, termId, trackChanges: false);

//                if (!IsSuperAdmin)
//                {
//                    var schoolId = GetUserSchoolIdOrNullWithValidation()!.Value;
//                    assessments = assessments.Where(a => a.TenantId == schoolId);
//                }

//                return SuccessResponse(assessments.Select(ToDto), "Published assessments retrieved successfully");
//            }
//            catch (Exception ex)
//            {
//                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
//            }
//        }

//        #endregion

//        // ─────────────────────────────────────────────────────────────────────
//        #region CREATE

//        /// <summary>Create a new assessment. Teacher, SchoolAdmin, or SuperAdmin.</summary>
//        [HttpPost]
//        public async Task<IActionResult> Create([FromBody] CreateAssessmentRequest request)
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
//                        return ValidationErrorResponse("SuperAdmin must supply a SchoolId when creating an assessment.");
//                    schoolId = request.SchoolId.Value;
//                }
//                else
//                {
//                    schoolId = GetUserSchoolIdOrNullWithValidation()!.Value;
//                }

//                var assessment = new Assessment1
//                {
//                    Id = Guid.NewGuid(),
//                    Title = request.Title.Trim(),
//                    Description = request.Description?.Trim(),
//                    TeacherId = request.TeacherId,
//                    SubjectId = request.SubjectId,
//                    ClassId = request.ClassId,
//                    TermId = request.TermId,
//                    AcademicYearId = request.AcademicYearId,
//                    AssessmentDate = request.AssessmentDate,
//                    MaximumScore = request.MaximumScore,
//                    AssessmentType = request.AssessmentType.Trim(),
//                    IsPublished = false,   // always starts unpublished
//                    TenantId = schoolId,
//                    CreatedOn = DateTime.UtcNow
//                };

//                _repositories.Assessment.Create(assessment);
//                await _repositories.SaveAsync();

//                await LogUserActivityAsync(
//                    "assessment.create",
//                    $"Created assessment '{assessment.Title}' ({assessment.Id}) for class {assessment.ClassId}");

//                return CreatedResponse(ToDto(assessment), "Assessment created successfully");
//            }
//            catch (Exception ex)
//            {
//                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
//            }
//        }

//        #endregion

//        // ─────────────────────────────────────────────────────────────────────
//        #region UPDATE

//        /// <summary>Update an existing assessment (non-published fields).</summary>
//        [HttpPut("{id:guid}")]
//        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAssessmentRequest request)
//        {
//            if (!HasAnyPermission("Assessment.Write", "Assessment.Update"))
//                return ForbiddenResponse("You do not have permission to update assessments.");

//            if (!ModelState.IsValid)
//                return ValidationErrorResponse(ModelState);

//            try
//            {
//                var assessment = await _repositories.Assessment.GetByIdAsync(id, trackChanges: true);
//                if (assessment == null)
//                    return NotFoundResponse("Assessment not found.");

//                var accessError = ValidateAssessmentSchoolAccess(assessment);
//                if (accessError != null) return accessError;

//                // Published assessments are locked — only SuperAdmin / SchoolAdmin may force-edit
//                if (assessment.IsPublished
//                    && !IsSuperAdmin
//                    && !HasAnyPermission("Assessment.Override", "Assessment.Write"))
//                    return ForbiddenResponse("Published assessments cannot be edited. Contact an administrator.");

//                assessment.Title = request.Title.Trim();
//                assessment.Description = request.Description?.Trim();
//                assessment.TeacherId = request.TeacherId;
//                assessment.SubjectId = request.SubjectId;
//                assessment.ClassId = request.ClassId;
//                assessment.TermId = request.TermId;
//                assessment.AcademicYearId = request.AcademicYearId;
//                assessment.AssessmentDate = request.AssessmentDate;
//                assessment.MaximumScore = request.MaximumScore;
//                assessment.AssessmentType = request.AssessmentType.Trim();

//                _repositories.Assessment.Update(assessment);
//                await _repositories.SaveAsync();

//                await LogUserActivityAsync(
//                    "assessment.update",
//                    $"Updated assessment '{assessment.Title}' ({assessment.Id})");

//                return SuccessResponse(ToDto(assessment), "Assessment updated successfully");
//            }
//            catch (Exception ex)
//            {
//                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
//            }
//        }

//        /// <summary>Publish or un-publish an assessment.</summary>
//        [HttpPatch("{id:guid}/publish")]
//        public async Task<IActionResult> UpdatePublishStatus(Guid id, [FromBody] UpdateAssessmentPublishRequest request)
//        {
//            if (!HasAnyPermission("Assessment.Write", "Assessment.Publish"))
//                return ForbiddenResponse("You do not have permission to publish assessments.");

//            try
//            {
//                var assessment = await _repositories.Assessment.GetByIdAsync(id, trackChanges: true);
//                if (assessment == null)
//                    return NotFoundResponse("Assessment not found.");

//                var accessError = ValidateAssessmentSchoolAccess(assessment);
//                if (accessError != null) return accessError;

//                assessment.IsPublished = request.IsPublished;
//                assessment.PublishedDate = request.IsPublished ? DateTime.UtcNow : null;

//                _repositories.Assessment.Update(assessment);
//                await _repositories.SaveAsync();

//                var action = request.IsPublished ? "published" : "unpublished";

//                await LogUserActivityAsync(
//                    "assessment.publish",
//                    $"{char.ToUpper(action[0]) + action[1..]} assessment '{assessment.Title}' ({assessment.Id})");

//                return SuccessResponse(ToDto(assessment), $"Assessment {action} successfully");
//            }
//            catch (Exception ex)
//            {
//                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
//            }
//        }

//        #endregion

//        // ─────────────────────────────────────────────────────────────────────
//        #region DELETE

//        /// <summary>
//        /// Permanently delete an assessment.
//        /// SuperAdmin or SchoolAdmin only; teachers cannot delete assessments.
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
//                var assessment = await _repositories.Assessment.GetByIdAsync(id, trackChanges: true);
//                if (assessment == null)
//                    return NotFoundResponse("Assessment not found.");

//                var accessError = ValidateAssessmentSchoolAccess(assessment);
//                if (accessError != null) return accessError;

//                var title = assessment.Title;

//                _repositories.Assessment.Delete(assessment);
//                await _repositories.SaveAsync();

//                await LogUserActivityAsync(
//                    "assessment.delete",
//                    $"Deleted assessment '{title}' ({id})");

//                return SuccessResponse<object?>(null, "Assessment deleted successfully");
//            }
//            catch (Exception ex)
//            {
//                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
//            }
//        }

//        #endregion
//    }
//}