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
    public class SubStrandsController : BaseApiController
    {
        private readonly ISubStrandService _subStrandService;

        public SubStrandsController(
            ISubStrandService subStrandService,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _subStrandService = subStrandService ?? throw new ArgumentNullException(nameof(subStrandService));
        }

        private string GetFullExceptionMessage(Exception ex)
        {
            var message = ex.Message;
            var inner = ex.InnerException;
            while (inner != null) { message += $" | Inner: {inner.Message}"; inner = inner.InnerException; }
            return message;
        }

        // GET /api/curriculum/substrands
        [HttpGet]
        [Authorize(Policy = PermissionKeys.CurriculumRead)]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid? schoolId = null,
            [FromQuery] Guid? strandId = null)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var targetSchoolId = IsSuperAdmin ? schoolId : userSchoolId;

                var subStrands = await _subStrandService.GetAllSubStrandsAsync(
                    targetSchoolId, userSchoolId, IsSuperAdmin, strandId);

                return SuccessResponse(subStrands);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // GET /api/curriculum/substrands/{id}
        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionKeys.CurriculumRead)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var subStrand = await _subStrandService.GetSubStrandByIdAsync(id, userSchoolId, IsSuperAdmin);
                return SuccessResponse(subStrand);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // POST /api/curriculum/substrands
        [HttpPost]
        [Authorize(Policy = PermissionKeys.CurriculumWrite)]
        public async Task<IActionResult> Create([FromBody] CreateSubStrandDto dto)
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

                var result = await _subStrandService.CreateSubStrandAsync(dto, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "substrand.create",
                    $"Created sub-strand '{result.Name}' under strand '{result.StrandName}' in school {result.TenantId}");

                return CreatedResponse($"api/curriculum/substrands/{result.Id}", result, "Sub-strand created successfully.");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // PUT /api/curriculum/substrands/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionKeys.CurriculumWrite)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSubStrandDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var result = await _subStrandService.UpdateSubStrandAsync(id, dto, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync("substrand.update", $"Updated sub-strand '{result.Name}'");

                return SuccessResponse(result, "Sub-strand updated successfully.");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // DELETE /api/curriculum/substrands/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionKeys.CurriculumWrite)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                await _subStrandService.DeleteSubStrandAsync(id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync("substrand.delete", $"Deleted sub-strand ID: {id}");

                return SuccessResponse("Sub-strand deleted successfully.");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }
    }
}