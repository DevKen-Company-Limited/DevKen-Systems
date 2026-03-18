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
    public class BookInventoryController : BaseApiController
    {
        private readonly IBookInventoryService _inventoryService;

        public BookInventoryController(
            IBookInventoryService inventoryService,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
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
        [Authorize(Policy = PermissionKeys.LibraryRead)]
        public async Task<IActionResult> GetAll([FromQuery] Guid? schoolId = null)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var targetSchoolId = IsSuperAdmin ? schoolId : userSchoolId;

                var inventory = await _inventoryService.GetAllInventoryAsync(
                    targetSchoolId, userSchoolId, IsSuperAdmin);

                //Response.Headers.Add("X-Access-Level", IsSuperAdmin ? "SuperAdmin" : "SchoolAdmin");
                //Response.Headers.Add("X-School-Filter", targetSchoolId?.ToString() ?? "All Schools");

                return SuccessResponse(inventory);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── GET BY BOOK ───────────────────────────────────────────────────────

        [HttpGet("book/{bookId:guid}")]
        [Authorize(Policy = PermissionKeys.LibraryRead)]
        public async Task<IActionResult> GetByBook(Guid bookId)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var inventory = await _inventoryService.GetInventoryByBookAsync(bookId, userSchoolId, IsSuperAdmin);
                return SuccessResponse(inventory);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── GET BY ID ─────────────────────────────────────────────────────────

        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionKeys.LibraryRead)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var inventory = await _inventoryService.GetInventoryByIdAsync(id, userSchoolId, IsSuperAdmin);
                return SuccessResponse(inventory);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        [HttpPost]
        [Authorize(Policy = PermissionKeys.LibraryWrite)]
        public async Task<IActionResult> Create([FromBody] CreateBookInventoryRequest request)
        {
            if (!ModelState.IsValid) return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                if (!IsSuperAdmin)
                    request.SchoolId = userSchoolId!.Value;
                else if (request.SchoolId == null || request.SchoolId == Guid.Empty)
                    return ValidationErrorResponse("SchoolId is required for SuperAdmin.");

                var result = await _inventoryService.CreateInventoryAsync(request, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "book-inventory.create",
                    $"Created inventory for book ID: {result.BookId} ({result.BookTitle})");

                return CreatedResponse(result, "Book inventory created successfully");
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
        [Authorize(Policy = PermissionKeys.LibraryWrite)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBookInventoryRequest request)
        {
            if (!ModelState.IsValid) return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var result = await _inventoryService.UpdateInventoryAsync(id, request, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "book-inventory.update",
                    $"Updated inventory for book '{result.BookTitle}'");

                return SuccessResponse(result, "Book inventory updated successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── DELETE ────────────────────────────────────────────────────────────

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionKeys.LibraryDelete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                await _inventoryService.DeleteInventoryAsync(id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync("book-inventory.delete", $"Deleted inventory record ID: {id}");

                return SuccessResponse("Book inventory record deleted successfully");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── RECALCULATE ───────────────────────────────────────────────────────

        [HttpPost("book/{bookId:guid}/recalculate")]
        [Authorize(Policy = PermissionKeys.LibraryWrite)]
        public async Task<IActionResult> Recalculate(Guid bookId)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var result = await _inventoryService.RecalculateAsync(bookId, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "book-inventory.recalculate",
                    $"Recalculated inventory for book '{result.BookTitle}'");

                return SuccessResponse(result, "Inventory recalculated successfully");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }
    }
}