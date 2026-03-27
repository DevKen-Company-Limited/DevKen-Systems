using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Library;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.Library;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Library
{
    [Route("api/library/[controller]")]
    [ApiController]
    [Authorize]
    public class LibraryFeesController : BaseApiController
    {
        private readonly ILibraryFeeService _feeService;

        public LibraryFeesController(
            ILibraryFeeService feeService,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _feeService = feeService
                ?? throw new ArgumentNullException(nameof(feeService));
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
        [Authorize(Policy = PermissionKeys.LibraryRead)]
        public async Task<IActionResult> GetAll([FromQuery] Guid? schoolId = null)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var targetSchoolId = IsSuperAdmin ? schoolId : userSchoolId;

                var fees = await _feeService.GetAllFeesAsync(
                    targetSchoolId, userSchoolId, IsSuperAdmin);

                Response.Headers.Append("X-Access-Level",
                    IsSuperAdmin ? "SuperAdmin" : "SchoolAdmin");
                Response.Headers.Append("X-School-Filter",
                    targetSchoolId?.ToString() ?? "All Schools");

                return SuccessResponse(fees);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── GET FILTERED ──────────────────────────────────────────────────────

        [HttpGet("filter")]
        [Authorize(Policy = PermissionKeys.LibraryRead)]
        public async Task<IActionResult> GetFiltered([FromQuery] LibraryFeeFilterRequest filter)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var fees = await _feeService.GetFilteredFeesAsync(
                    filter, userSchoolId, IsSuperAdmin);

                return SuccessResponse(fees);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── GET BY MEMBER ─────────────────────────────────────────────────────

        [HttpGet("member/{memberId:guid}")]
        [Authorize(Policy = PermissionKeys.LibraryRead)]
        public async Task<IActionResult> GetByMember(Guid memberId)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var fees = await _feeService.GetFeesByMemberAsync(
                    memberId, userSchoolId, IsSuperAdmin);

                return SuccessResponse(fees);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── GET OUTSTANDING BALANCE ───────────────────────────────────────────

        [HttpGet("member/{memberId:guid}/balance")]
        [Authorize(Policy = PermissionKeys.LibraryRead)]
        public async Task<IActionResult> GetOutstandingBalance(Guid memberId)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var balance = await _feeService.GetOutstandingBalanceAsync(
                    memberId, userSchoolId, IsSuperAdmin);

                return SuccessResponse(new { MemberId = memberId, OutstandingBalance = balance });
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

                var fee = await _feeService.GetFeeByIdAsync(id, userSchoolId, IsSuperAdmin);

                return SuccessResponse(fee);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        [HttpPost]
        [Authorize(Policy = PermissionKeys.LibraryWrite)]
        public async Task<IActionResult> Create([FromBody] CreateLibraryFeeRequest request)
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

                var result = await _feeService.CreateFeeAsync(request, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "library-fee.create",
                    $"Created {result.FeeType} fee of {result.Amount} for member '{result.MemberNumber}'");

                return CreatedResponse(result, "Library fee created successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── UPDATE ────────────────────────────────────────────────────────────

        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionKeys.LibraryWrite)]
        public async Task<IActionResult> Update(
            Guid id, [FromBody] UpdateLibraryFeeRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var result = await _feeService.UpdateFeeAsync(id, request, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "library-fee.update",
                    $"Updated library fee '{id}' — new amount: {result.Amount}");

                return SuccessResponse(result, "Library fee updated successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── RECORD PAYMENT ────────────────────────────────────────────────────

        [HttpPost("{id:guid}/pay")]
        [Authorize(Policy = PermissionKeys.LibraryWrite)]
        public async Task<IActionResult> RecordPayment(
            Guid id, [FromBody] RecordLibraryFeePaymentRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var result = await _feeService.RecordPaymentAsync(
                    id, request, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "library-fee.payment",
                    $"Recorded payment of {request.AmountPaid} for fee '{id}' — status: {result.FeeStatus}");

                return SuccessResponse(result, "Payment recorded successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── WAIVE ─────────────────────────────────────────────────────────────

        [HttpPost("{id:guid}/waive")]
        [Authorize(Policy = PermissionKeys.LibraryWrite)]
        public async Task<IActionResult> Waive(
            Guid id, [FromBody] WaiveLibraryFeeRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var result = await _feeService.WaiveFeeAsync(
                    id, request, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "library-fee.waive",
                    $"Waived library fee '{id}'. Reason: {request.Reason}");

                return SuccessResponse(result, "Library fee waived successfully");
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

                await _feeService.DeleteFeeAsync(id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "library-fee.delete",
                    $"Deleted library fee with ID: {id}");

                return SuccessResponse("Library fee deleted successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }
    }
}