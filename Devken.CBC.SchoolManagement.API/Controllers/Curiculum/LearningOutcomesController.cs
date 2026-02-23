using DevExpress.XtraRichEdit.Model;
using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Curriculum;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.Curriculum;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Devken.CBC.SchoolManagement.API.Controllers.Curiculum
{
    [Route("api/curriculum/[controller]")]
    [ApiController]
    [Authorize]
    public class LearningOutcomesController : BaseApiController
    {
        private readonly ILearningOutcomeService _learningOutcomeService;

        public LearningOutcomesController(
            ILearningOutcomeService learningOutcomeService,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _learningOutcomeService = learningOutcomeService ?? throw new ArgumentNullException(nameof(learningOutcomeService));
        }

        private string GetFullExceptionMessage(Exception ex)
        {
            var message = ex.Message;
            var inner = ex.InnerException;
            while (inner != null) { message += $" | Inner: {inner.Message}"; inner = inner.InnerException; }
            return message;
        }

        // GET /api/curriculum/learningoutcomes
        [HttpGet]
        [Authorize(Policy = PermissionKeys.CurriculumRead)]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid? schoolId = null,
        [FromQuery] Guid? subStrandId = null,
            [FromQuery] Guid? strandId = null,
            [FromQuery] Guid? learningAreaId = null,
            [FromQuery] CBCLevel? level = null,
            [FromQuery] bool? isCore = null)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var targetSchoolId = IsSuperAdmin ? schoolId : userSchoolId;

                var outcomes = await _learningOutcomeService.GetAllLearningOutcomesAsync(
                    targetSchoolId, userSchoolId, IsSuperAdmin,
                    subStrandId, strandId, learningAreaId, level, isCore);

                return SuccessResponse(outcomes);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // GET /api/curriculum/learningoutcomes/{id}
        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionKeys.CurriculumRead)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var outcome = await _learningOutcomeService.GetLearningOutcomeByIdAsync(id, userSchoolId, IsSuperAdmin);
                return SuccessResponse(outcome);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // GET /api/curriculum/learningoutcomes/by-code/{code}
        [HttpGet("by-code/{code}")]
        [Authorize(Policy = PermissionKeys.CurriculumRead)]
        public async Task<IActionResult> GetByCode(string code)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var outcome = await _learningOutcomeService.GetLearningOutcomeByCodeAsync(code, userSchoolId, IsSuperAdmin);
                return SuccessResponse(outcome);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // POST /api/curriculum/learningoutcomes
        [HttpPost]
        [Authorize(Policy = PermissionKeys.CurriculumWrite)]
        public async Task<IActionResult> Create([FromBody] CreateLearningOutcomeDto dto)
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

                var result = await _learningOutcomeService.CreateLearningOutcomeAsync(dto, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "learning_outcome.create",
                    $"Created learning outcome '{result.Code ?? result.Outcome}' in school {result.TenantId}");

                return CreatedResponse($"api/curriculum/learningoutcomes/{result.Id}", result, "Learning outcome created successfully.");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // PUT /api/curriculum/learningoutcomes/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionKeys.CurriculumWrite)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLearningOutcomeDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var result = await _learningOutcomeService.UpdateLearningOutcomeAsync(id, dto, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync("learning_outcome.update", $"Updated learning outcome '{result.Code ?? result.Outcome}'");

                return SuccessResponse(result, "Learning outcome updated successfully.");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // DELETE /api/curriculum/learningoutcomes/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionKeys.CurriculumWrite)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                await _learningOutcomeService.DeleteLearningOutcomeAsync(id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync("learning_outcome.delete", $"Deleted learning outcome ID: {id}");

                return SuccessResponse("Learning outcome deleted successfully.");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }
    }
}
