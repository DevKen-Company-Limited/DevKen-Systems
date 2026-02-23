using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Academics;
using Devken.CBC.SchoolManagement.Application.DTOs.Curriculum;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.Service.Academics;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.Curriculum;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Academic
{
    [Route("api/curriculum/[controller]")]
    [ApiController]
    [Authorize]
    public class StrandsController : BaseApiController
    {
        private readonly IStrandService _strandService;

        public StrandsController(
            IStrandService strandService,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _strandService = strandService ?? throw new ArgumentNullException(nameof(strandService));
        }

        private string GetFullExceptionMessage(Exception ex)
        {
            var message = ex.Message;
            var inner = ex.InnerException;
            while (inner != null) { message += $" | Inner: {inner.Message}"; inner = inner.InnerException; }
            return message;
        }

        // GET /api/curriculum/strands
        [HttpGet]
        [Authorize(Policy = PermissionKeys.CurriculumRead)]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid? schoolId = null,
            [FromQuery] Guid? learningAreaId = null)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var targetSchoolId = IsSuperAdmin ? schoolId : userSchoolId;

                var strands = await _strandService.GetAllStrandsAsync(
                    targetSchoolId, userSchoolId, IsSuperAdmin, learningAreaId);

                return SuccessResponse(strands);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // GET /api/curriculum/strands/{id}
        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionKeys.CurriculumRead)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var strand = await _strandService.GetStrandByIdAsync(id, userSchoolId, IsSuperAdmin);
                return SuccessResponse(strand);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // POST /api/curriculum/strands
        [HttpPost]
        [Authorize(Policy = PermissionKeys.CurriculumWrite)]
        public async Task<IActionResult> Create([FromBody] CreateStrandDto dto)
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

                var result = await _strandService.CreateStrandAsync(dto, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "strand.create",
                    $"Created strand '{result.Name}' under learning area '{result.LearningAreaName}' in school {result.TenantId}");

                return CreatedResponse($"api/curriculum/strands/{result.Id}", result, "Strand created successfully.");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // PUT /api/curriculum/strands/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionKeys.CurriculumWrite)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStrandDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var result = await _strandService.UpdateStrandAsync(id, dto, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync("strand.update", $"Updated strand '{result.Name}'");

                return SuccessResponse(result, "Strand updated successfully.");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // DELETE /api/curriculum/strands/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionKeys.CurriculumWrite)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                await _strandService.DeleteStrandAsync(id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync("strand.delete", $"Deleted strand ID: {id}");

                return SuccessResponse("Strand deleted successfully.");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }
    }
}