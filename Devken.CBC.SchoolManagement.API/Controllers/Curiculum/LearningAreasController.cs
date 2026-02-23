using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Academics;
using Devken.CBC.SchoolManagement.Application.DTOs.Curriculum;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.Service.Academics;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.Curriculum;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Academic
{
    [Route("api/curriculum/[controller]")]
    [ApiController]
    [Authorize]
    public class LearningAreasController : BaseApiController
    {
        private readonly ILearningAreaService _learningAreaService;

        public LearningAreasController(
            ILearningAreaService learningAreaService,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _learningAreaService = learningAreaService ?? throw new ArgumentNullException(nameof(learningAreaService));
        }

        private string GetFullExceptionMessage(Exception ex)
        {
            var message = ex.Message;
            var inner = ex.InnerException;
            while (inner != null) { message += $" | Inner: {inner.Message}"; inner = inner.InnerException; }
            return message;
        }

        // GET /api/curriculum/learningareas
        [HttpGet]
        [Authorize(Policy = PermissionKeys.CurriculumRead)]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid? schoolId = null,
            [FromQuery] CBCLevel? level = null)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var targetSchoolId = IsSuperAdmin ? schoolId : userSchoolId;

                var areas = await _learningAreaService.GetAllLearningAreasAsync(
                    targetSchoolId, userSchoolId, IsSuperAdmin, level);

                return SuccessResponse(areas);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // GET /api/curriculum/learningareas/{id}
        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionKeys.CurriculumRead)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var area = await _learningAreaService.GetLearningAreaByIdAsync(id, userSchoolId, IsSuperAdmin);
                return SuccessResponse(area);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // GET /api/curriculum/learningareas/by-code/{code}
        [HttpGet("by-code/{code}")]
        [Authorize(Policy = PermissionKeys.CurriculumRead)]
        public async Task<IActionResult> GetByCode(string code)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var area = await _learningAreaService.GetLearningAreaByCodeAsync(code, userSchoolId, IsSuperAdmin);
                return SuccessResponse(area);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // POST /api/curriculum/learningareas
        [HttpPost]
        [Authorize(Policy = PermissionKeys.CurriculumWrite)]
        public async Task<IActionResult> Create([FromBody] CreateLearningAreaDto dto)
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

                var result = await _learningAreaService.CreateLearningAreaAsync(dto, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "learning_area.create",
                    $"Created learning area '{result.Name}' in school {result.TenantId}");

                return CreatedResponse($"api/curriculum/learningareas/{result.Id}", result, "Learning area created successfully.");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // PUT /api/curriculum/learningareas/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionKeys.CurriculumWrite)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLearningAreaDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var result = await _learningAreaService.UpdateLearningAreaAsync(id, dto, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync("learning_area.update", $"Updated learning area '{result.Name}'");

                return SuccessResponse(result, "Learning area updated successfully.");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // DELETE /api/curriculum/learningareas/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionKeys.CurriculumWrite)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                await _learningAreaService.DeleteLearningAreaAsync(id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync("learning_area.delete", $"Deleted learning area ID: {id}");

                return SuccessResponse("Learning area deleted successfully.");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }
    }
}