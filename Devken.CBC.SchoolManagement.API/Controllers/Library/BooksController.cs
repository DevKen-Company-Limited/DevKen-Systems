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
    public class BooksController : BaseApiController
    {
        private readonly IBookService _bookService;

        public BooksController(
            IBookService bookService,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _bookService = bookService ?? throw new ArgumentNullException(nameof(bookService));
        }

        private string GetFullExceptionMessage(Exception ex)
        {
            var message = ex.Message;
            var inner = ex.InnerException;
            while (inner != null)
            {
                message += $" | Inner: {inner.Message}";
                inner = inner.InnerException;
            }
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

                var books = await _bookService.GetAllBooksAsync(
                    targetSchoolId, userSchoolId, IsSuperAdmin);

                //Response.Headers.Add("X-Access-Level", IsSuperAdmin ? "SuperAdmin" : "SchoolAdmin");
                //Response.Headers.Add("X-School-Filter", targetSchoolId?.ToString() ?? "All Schools");

                return SuccessResponse(books);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── GET BY CATEGORY ───────────────────────────────────────────────────

        [HttpGet("category/{categoryId:guid}")]
        [Authorize(Policy = PermissionKeys.BookRead)]
        public async Task<IActionResult> GetByCategory(Guid categoryId)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var books = await _bookService.GetBooksByCategoryAsync(
                    categoryId, userSchoolId, IsSuperAdmin);

                return SuccessResponse(books);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── GET BY AUTHOR ─────────────────────────────────────────────────────

        [HttpGet("author/{authorId:guid}")]
        [Authorize(Policy = PermissionKeys.BookRead)]
        public async Task<IActionResult> GetByAuthor(Guid authorId)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var books = await _bookService.GetBooksByAuthorAsync(
                    authorId, userSchoolId, IsSuperAdmin);

                return SuccessResponse(books);
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

                var book = await _bookService.GetBookByIdAsync(id, userSchoolId, IsSuperAdmin);

                return SuccessResponse(book);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        [HttpPost]
        [Authorize(Policy = PermissionKeys.BookWrite)]
        public async Task<IActionResult> Create([FromBody] CreateBookRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                if (!IsSuperAdmin)
                    request.SchoolId = userSchoolId!.Value;
                else if (request.SchoolId == null || request.SchoolId == Guid.Empty)
                    return ValidationErrorResponse("SchoolId is required for SuperAdmin.");

                var result = await _bookService.CreateBookAsync(request, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "book.create",
                    $"Created book '{result.Title}' (ISBN: {result.ISBN})");

                return CreatedResponse(result, "Book created successfully");
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
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBookRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var result = await _bookService.UpdateBookAsync(id, request, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "book.update",
                    $"Updated book '{result.Title}' (ISBN: {result.ISBN})");

                return SuccessResponse(result, "Book updated successfully");
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

                await _bookService.DeleteBookAsync(id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync("book.delete", $"Deleted book with ID: {id}");

                return SuccessResponse("Book deleted successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }
    }
}