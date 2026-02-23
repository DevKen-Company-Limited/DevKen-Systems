// Api/Controllers/Academic/GradesController.cs
using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Academics;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.Service.Academics;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Academic
{
    [Route("api/academic/[controller]")]
    [ApiController]
    [Authorize]
    public class GradesController : BaseApiController
    {
        private readonly IGradeService _gradeService;

        public GradesController(
            IGradeService gradeService,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _gradeService = gradeService ?? throw new ArgumentNullException(nameof(gradeService));
        }

        private string GetFullExceptionMessage(Exception ex)
        {
            var message = ex.Message;
            var inner = ex.InnerException;
            while (inner != null) { message += $" | Inner: {inner.Message}"; inner = inner.InnerException; }
            return message;
        }

        // GET /api/academic/grades
        [HttpGet]
        [Authorize(Policy = PermissionKeys.GradeRead)]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid? schoolId = null,
            [FromQuery] Guid? studentId = null,
            [FromQuery] Guid? subjectId = null,
            [FromQuery] Guid? termId = null)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var targetSchoolId = IsSuperAdmin ? schoolId : userSchoolId;

                var grades = await _gradeService.GetAllGradesAsync(
                    targetSchoolId, userSchoolId, IsSuperAdmin,
                    studentId, subjectId, termId);

                return SuccessResponse(grades);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // GET /api/academic/grades/{id}
        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionKeys.GradeRead)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var grade = await _gradeService.GetGradeByIdAsync(id, userSchoolId, IsSuperAdmin);
                return SuccessResponse(grade);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // POST /api/academic/grades
        [HttpPost]
        [Authorize(Policy = PermissionKeys.GradeWrite)]
        public async Task<IActionResult> Create([FromBody] CreateGradeDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                if (!IsSuperAdmin)
                    dto.TenantId = userSchoolId;
                else if (dto.TenantId == null || dto.TenantId == Guid.Empty)
                    return ValidationErrorResponse("TenantId is required for SuperAdmin.");

                var result = await _gradeService.CreateGradeAsync(dto, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "grade.create",
                    $"Created grade for student '{result.StudentId}', subject '{result.SubjectId}' in school {result.TenantId}");

                return CreatedResponse($"api/academic/grades/{result.Id}", result, "Grade created successfully.");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // PUT /api/academic/grades/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionKeys.GradeWrite)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGradeDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var result = await _gradeService.UpdateGradeAsync(id, dto, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync("grade.update",
                    $"Updated grade ID '{id}' — score: {result.Score}/{result.MaximumScore}");

                return SuccessResponse(result, "Grade updated successfully.");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // DELETE /api/academic/grades/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionKeys.GradeWrite)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                await _gradeService.DeleteGradeAsync(id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync("grade.delete", $"Deleted grade ID: {id}");

                return SuccessResponse("Grade deleted successfully.");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // PATCH /api/academic/grades/{id}/finalize
        [HttpPatch("{id:guid}/finalize")]
        [Authorize(Policy = PermissionKeys.GradeWrite)]
        public async Task<IActionResult> Finalize(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var result = await _gradeService.FinalizeGradeAsync(id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync("grade.finalize",
                    $"Finalized grade ID '{id}' for student '{result.StudentId}'");

                return SuccessResponse(result, "Grade finalized successfully.");
            }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }
    }
}