using System;
using System.Threading.Tasks;
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
    public class BookCopiesController : BaseApiController
    {
        private readonly IBookCopyService _copyService;

        public BookCopiesController(
            IBookCopyService copyService,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _copyService = copyService ?? throw new ArgumentNullException(nameof(copyService));
        }

        private string GetFullExceptionMessage(Exception ex)
        {
            var message = ex.Message;
            var inner = ex.InnerException;
            while (inner != null) { message += $" | Inner: {inner.Message}"; inner = inner.InnerException; }
            return message;
        }

        // ── GET ALL ───────────────────────────────────────────────────────────

        [HttpGet]
        [Authorize(Policy = PermissionKeys.BookRead)]
        public async Task<IActionResult> GetAll([FromQuery] Guid? schoolId = null)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var targetSchoolId = IsSuperAdmin ? schoolId : userSchoolId;

                var copies = await _copyService.GetAllCopiesAsync(targetSchoolId, userSchoolId, IsSuperAdmin);

                //Response.Headers.Append("X-Access-Level", IsSuperAdmin ? "SuperAdmin" : "SchoolAdmin");
                //Response.Headers.Append("X-School-Filter", targetSchoolId?.ToString() ?? "All Schools");

                return SuccessResponse(copies);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── GET BY BOOK ───────────────────────────────────────────────────────

        [HttpGet("book/{bookId:guid}")]
        [Authorize(Policy = PermissionKeys.BookRead)]
        public async Task<IActionResult> GetByBook(Guid bookId)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var copies = await _copyService.GetCopiesByBookAsync(bookId, userSchoolId, IsSuperAdmin);
                return SuccessResponse(copies);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── GET BY BRANCH ─────────────────────────────────────────────────────

        [HttpGet("branch/{branchId:guid}")]
        [Authorize(Policy = PermissionKeys.BookRead)]
        public async Task<IActionResult> GetByBranch(Guid branchId)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var copies = await _copyService.GetCopiesByBranchAsync(branchId, userSchoolId, IsSuperAdmin);
                return SuccessResponse(copies);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── GET BY ID ─────────────────────────────────────────────────────────

        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionKeys.BookRead)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var copy = await _copyService.GetCopyByIdAsync(id, userSchoolId, IsSuperAdmin);
                return SuccessResponse(copy);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        [HttpPost]
        [Authorize(Policy = PermissionKeys.BookWrite)]
        public async Task<IActionResult> Create([FromBody] CreateBookCopyRequest request)
        {
            if (!ModelState.IsValid) return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                if (!IsSuperAdmin)
                    request.SchoolId = userSchoolId!.Value;
                else if (request.SchoolId == null || request.SchoolId == Guid.Empty)
                    return ValidationErrorResponse("SchoolId is required for SuperAdmin.");

                var result = await _copyService.CreateCopyAsync(request, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "book-copy.create",
                    $"Added copy (Accession: {result.AccessionNumber}) for book '{result.BookTitle}'");

                return CreatedResponse(result, "Book copy created successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── UPDATE ────────────────────────────────────────────────────────────

        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionKeys.BookWrite)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBookCopyRequest request)
        {
            if (!ModelState.IsValid) return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var result = await _copyService.UpdateCopyAsync(id, request, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "book-copy.update",
                    $"Updated copy (Accession: {result.AccessionNumber}) for book '{result.BookTitle}'");

                return SuccessResponse(result, "Book copy updated successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── DELETE ────────────────────────────────────────────────────────────

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionKeys.BookDelete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                await _copyService.DeleteCopyAsync(id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync("book-copy.delete", $"Deleted book copy with ID: {id}");

                return SuccessResponse("Book copy deleted successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── STATUS MANAGEMENT ─────────────────────────────────────────────────

        [HttpPatch("{id:guid}/mark-lost")]
        [Authorize(Policy = PermissionKeys.BookWrite)]
        public async Task<IActionResult> MarkLost(Guid id, [FromBody] MarkBookCopyStatusRequest request)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var result = await _copyService.MarkAsLostAsync(id, request.Remarks, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "book-copy.mark-lost",
                    $"Marked copy (Accession: {result.AccessionNumber}) as lost");

                return SuccessResponse(result, "Book copy marked as lost");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        [HttpPatch("{id:guid}/mark-damaged")]
        [Authorize(Policy = PermissionKeys.BookWrite)]
        public async Task<IActionResult> MarkDamaged(Guid id, [FromBody] MarkBookCopyStatusRequest request)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var result = await _copyService.MarkAsDamagedAsync(id, request.Remarks, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "book-copy.mark-damaged",
                    $"Marked copy (Accession: {result.AccessionNumber}) as damaged");

                return SuccessResponse(result, "Book copy marked as damaged");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        [HttpPatch("{id:guid}/mark-available")]
        [Authorize(Policy = PermissionKeys.BookWrite)]
        public async Task<IActionResult> MarkAvailable(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var result = await _copyService.MarkAsAvailableAsync(id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "book-copy.mark-available",
                    $"Marked copy (Accession: {result.AccessionNumber}) as available");

                return SuccessResponse(result, "Book copy marked as available");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }
    }
}