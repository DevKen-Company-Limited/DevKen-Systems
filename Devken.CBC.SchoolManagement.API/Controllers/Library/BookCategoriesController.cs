using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Library;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.Library;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Library
{
    [Route("api/library/[controller]")]
    [ApiController]
    [Authorize]
    public class BookCategoriesController : BaseApiController
    {
        private readonly IBookCategoryService _service;

        public BookCategoriesController(
            IBookCategoryService service,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        private static string GetFullExceptionMessage(Exception ex)
        {
            var msg = ex.Message;
            var inner = ex.InnerException;
            while (inner != null) { msg += $" | Inner: {inner.Message}"; inner = inner.InnerException; }
            return msg;
        }

        // GET /api/library/bookcategories
        [HttpGet]
        [Authorize(Policy = PermissionKeys.LibraryRead)]
        public async Task<IActionResult> GetAll([FromQuery] Guid? schoolId = null)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var targetSchoolId = IsSuperAdmin ? schoolId : userSchoolId;
                var result = await _service.GetAllAsync(targetSchoolId, userSchoolId, IsSuperAdmin);
                return SuccessResponse(result);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // GET /api/library/bookcategories/{id}
        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionKeys.LibraryRead)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var result = await _service.GetByIdAsync(id, userSchoolId, IsSuperAdmin);
                return SuccessResponse(result);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // POST /api/library/bookcategories
        [HttpPost]
        [Authorize(Policy = PermissionKeys.LibraryWrite)]
        public async Task<IActionResult> Create([FromBody] CreateBookCategoryDto dto)
        {
            if (!ModelState.IsValid) return ValidationErrorResponse(ModelState);
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                if (!IsSuperAdmin) dto.TenantId = userSchoolId;
                else if (dto.TenantId == null || dto.TenantId == Guid.Empty)
                    return ValidationErrorResponse("TenantId is required for SuperAdmin.");

                var result = await _service.CreateAsync(dto, userSchoolId, IsSuperAdmin);
                await LogUserActivityAsync("bookcategory.create", $"Created book category '{result.Name}'");
                return CreatedResponse($"api/library/bookcategories/{result.Id}", result, "Book category created successfully.");
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // PUT /api/library/bookcategories/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionKeys.LibraryWrite)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBookCategoryDto dto)
        {
            if (!ModelState.IsValid) return ValidationErrorResponse(ModelState);
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var result = await _service.UpdateAsync(id, dto, userSchoolId, IsSuperAdmin);
                await LogUserActivityAsync("bookcategory.update", $"Updated book category '{result.Name}'");
                return SuccessResponse(result, "Book category updated successfully.");
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // DELETE /api/library/bookcategories/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionKeys.LibraryWrite)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                await _service.DeleteAsync(id, userSchoolId, IsSuperAdmin);
                await LogUserActivityAsync("bookcategory.delete", $"Deleted book category ID: {id}");
                return SuccessResponse("Book category deleted successfully.");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }
    }
}
