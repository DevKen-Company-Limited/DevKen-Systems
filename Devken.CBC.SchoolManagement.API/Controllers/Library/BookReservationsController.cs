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
    public class BookReservationsController : BaseApiController
    {
        private readonly IBookReservationService _reservationService;

        public BookReservationsController(
            IBookReservationService reservationService,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _reservationService = reservationService
                ?? throw new ArgumentNullException(nameof(reservationService));
        }

        // ── HELPER ────────────────────────────────────────────────────────────

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

        /// <summary>
        /// Returns all book reservations.
        /// SuperAdmin may optionally filter by schoolId query param.
        /// School-scoped users always see only their school's reservations.
        /// </summary>
        [HttpGet]
        [Authorize(Policy = PermissionKeys.BookIssueRead)]
        public async Task<IActionResult> GetAll([FromQuery] Guid? schoolId = null)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var targetSchoolId = IsSuperAdmin ? schoolId : userSchoolId;

                var reservations = await _reservationService.GetAllReservationsAsync(
                    targetSchoolId, userSchoolId, IsSuperAdmin);

                Response.Headers.Append("X-Access-Level",
                    IsSuperAdmin ? "SuperAdmin" : "SchoolAdmin");
                Response.Headers.Append("X-School-Filter",
                    targetSchoolId?.ToString() ?? "All Schools");

                return SuccessResponse(reservations);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── GET BY ID ─────────────────────────────────────────────────────────

        /// <summary>Returns a single reservation by its ID.</summary>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionKeys.BookIssueRead)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var reservation = await _reservationService.GetReservationByIdAsync(
                    id, userSchoolId, IsSuperAdmin);

                return SuccessResponse(reservation);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── GET BY BOOK ───────────────────────────────────────────────────────

        /// <summary>Returns all reservations for a specific book.</summary>
        [HttpGet("by-book/{bookId:guid}")]
        [Authorize(Policy = PermissionKeys.BookIssueRead)]
        public async Task<IActionResult> GetByBook(Guid bookId)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var reservations = await _reservationService.GetReservationsByBookAsync(
                    bookId, userSchoolId, IsSuperAdmin);

                return SuccessResponse(reservations);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── GET BY MEMBER ─────────────────────────────────────────────────────

        /// <summary>Returns all reservations for a specific library member.</summary>
        [HttpGet("by-member/{memberId:guid}")]
        [Authorize(Policy = PermissionKeys.BookIssueRead)]
        public async Task<IActionResult> GetByMember(Guid memberId)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var reservations = await _reservationService.GetReservationsByMemberAsync(
                    memberId, userSchoolId, IsSuperAdmin);

                return SuccessResponse(reservations);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        /// <summary>Creates a new book reservation.</summary>
        [HttpPost]
        [Authorize(Policy = PermissionKeys.BookIssueWrite)]
        public async Task<IActionResult> Create([FromBody] CreateBookReservationRequest request)
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

                var result = await _reservationService.CreateReservationAsync(
                    request, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "book-reservation.create",
                    $"Created reservation for book '{result.BookTitle}' by member '{result.MemberName}'");

                return CreatedResponse(result, "Book reservation created successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── UPDATE ────────────────────────────────────────────────────────────

        /// <summary>Updates an existing reservation (book, member, fulfilled flag).</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionKeys.BookIssueWrite)]
        public async Task<IActionResult> Update(
            Guid id, [FromBody] UpdateBookReservationRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var result = await _reservationService.UpdateReservationAsync(
                    id, request, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "book-reservation.update",
                    $"Updated reservation '{id}' for book '{result.BookTitle}'");

                return SuccessResponse(result, "Book reservation updated successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── FULFILL ───────────────────────────────────────────────────────────

        /// <summary>
        /// Marks a reservation as fulfilled.
        /// Use this dedicated endpoint instead of PUT when you only want to flip the
        /// fulfilled flag (e.g. when handing a book copy to the member).
        /// </summary>
        [HttpPatch("{id:guid}/fulfill")]
        [Authorize(Policy = PermissionKeys.BookIssueWrite)]
        public async Task<IActionResult> Fulfill(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var result = await _reservationService.FulfillReservationAsync(
                    id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "book-reservation.fulfill",
                    $"Fulfilled reservation '{id}' for book '{result.BookTitle}'");

                return SuccessResponse(result, "Book reservation fulfilled successfully");
            }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── DELETE ────────────────────────────────────────────────────────────

        /// <summary>Deletes an unfulfilled reservation. Fulfilled reservations cannot be deleted.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionKeys.BookIssueWrite)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                await _reservationService.DeleteReservationAsync(id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "book-reservation.delete",
                    $"Deleted reservation with ID: {id}");

                return SuccessResponse("Book reservation deleted successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }
    }
}