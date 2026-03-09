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
    public class BookAuthorsController : BaseApiController
    {
        private readonly IBookAuthorService _service;

        public BookAuthorsController(
            IBookAuthorService service,
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

        // GET /api/library/bookauthors
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

        // GET /api/library/bookauthors/{id}
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

        // POST /api/library/bookauthors
        [HttpPost]
        [Authorize(Policy = PermissionKeys.LibraryWrite)]
        public async Task<IActionResult> Create([FromBody] CreateBookAuthorDto dto)
        {
            if (!ModelState.IsValid) return ValidationErrorResponse(ModelState);
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                if (!IsSuperAdmin) dto.TenantId = userSchoolId;
                else if (dto.TenantId == null || dto.TenantId == Guid.Empty)
                    return ValidationErrorResponse("TenantId is required for SuperAdmin.");

                var result = await _service.CreateAsync(dto, userSchoolId, IsSuperAdmin);
                await LogUserActivityAsync("bookauthor.create", $"Created book author '{result.Name}'");
                return CreatedResponse($"api/library/bookauthors/{result.Id}", result, "Book author created successfully.");
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // PUT /api/library/bookauthors/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionKeys.LibraryWrite)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBookAuthorDto dto)
        {
            if (!ModelState.IsValid) return ValidationErrorResponse(ModelState);
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var result = await _service.UpdateAsync(id, dto, userSchoolId, IsSuperAdmin);
                await LogUserActivityAsync("bookauthor.update", $"Updated book author '{result.Name}'");
                return SuccessResponse(result, "Book author updated successfully.");
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // DELETE /api/library/bookauthors/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionKeys.LibraryWrite)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                await _service.DeleteAsync(id, userSchoolId, IsSuperAdmin);
                await LogUserActivityAsync("bookauthor.delete", $"Deleted book author ID: {id}");
                return SuccessResponse("Book author deleted successfully.");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }
    }
}
