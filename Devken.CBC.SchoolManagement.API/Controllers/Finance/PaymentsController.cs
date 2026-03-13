using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Payments;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.Finance;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Finance
{
    [Route("api/finance/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentsController(
        IPaymentService paymentService,
        IUserActivityService? activityService = null)   // primary constructor
        : BaseApiController(activityService)
    {
        // ─── GET ALL  →  GET /api/finance/payments ───────────────────────────────────

        [HttpGet]
        [Authorize(Policy = PermissionKeys.FeeRead)]
        public async Task<IActionResult> GetAll(
           [FromQuery] Guid? schoolId = null,
           [FromQuery] Guid? studentId = null,
           [FromQuery] Guid? invoiceId = null,
           [FromQuery] PaymentMethod? method = null,
           [FromQuery] PaymentStatus? status = null,
           [FromQuery] DateTime? from = null,
           [FromQuery] DateTime? to = null,
           [FromQuery] bool? isReversal = null,
           [FromQuery] string? search = null,
           [FromQuery] int page = 1,
           [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await paymentService.GetPagedAsync(
                    schoolId, studentId, invoiceId, method, status,
                    from, to, isReversal, search, page, pageSize,
                    GetUserSchoolIdOrNullWithValidation(), IsSuperAdmin);

                return SuccessResponse(result);
            }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(ex.Message); }
        }

        // ─── GET BY ID  →  GET /api/finance/payments/{id} ───────────────────────────

        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionKeys.FeeRead)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                return SuccessResponse(await paymentService.GetByIdAsync(
                    id, GetUserSchoolIdOrNullWithValidation(), IsSuperAdmin));
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(ex.Message); }
        }

        // ─── GET BY REFERENCE  →  GET /api/finance/payments/by-reference/{ref} ──────

        [HttpGet("by-reference/{reference}")]
        [Authorize(Policy = PermissionKeys.FeeRead)]
        public async Task<IActionResult> GetByReference(string reference)
        {
            try
            {
                return SuccessResponse(await paymentService.GetByReferenceAsync(
                    reference, GetUserSchoolIdOrNullWithValidation(), IsSuperAdmin));
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(ex.Message); }
        }

        // ─── GET BY STUDENT  →  GET /api/finance/payments/student/{studentId} ────────

        [HttpGet("student/{studentId:guid}")]
        [Authorize(Policy = PermissionKeys.FeeRead)]
        public async Task<IActionResult> GetByStudent(Guid studentId)
        {
            try
            {
                return SuccessResponse(await paymentService.GetByStudentAsync(
                    studentId, GetUserSchoolIdOrNullWithValidation(), IsSuperAdmin));
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(ex.Message); }
        }

        // ─── GET BY INVOICE  →  GET /api/finance/payments/invoice/{invoiceId} ────────

        [HttpGet("invoice/{invoiceId:guid}")]
        [Authorize(Policy = PermissionKeys.FeeRead)]
        public async Task<IActionResult> GetByInvoice(Guid invoiceId)
        {
            try
            {
                return SuccessResponse(await paymentService.GetByInvoiceAsync(
                    invoiceId, GetUserSchoolIdOrNullWithValidation(), IsSuperAdmin));
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(ex.Message); }
        }

        // ─── SUMMARY  →  GET /api/finance/payments/summary ──────────────────────────

        [HttpGet("summary")]
        [Authorize(Policy = PermissionKeys.FeeRead)]
        public async Task<IActionResult> GetSummary(
            [FromQuery] Guid? schoolId = null,
            [FromQuery] Guid? studentId = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            try
            {
                return SuccessResponse(await paymentService.GetSummaryAsync(
                    schoolId, studentId, from, to,
                    GetUserSchoolIdOrNullWithValidation(), IsSuperAdmin));
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(ex.Message); }
        }

        // ─── CREATE  →  POST /api/finance/payments ───────────────────────────────────

        [HttpPost]
        [Authorize(Policy = PermissionKeys.FeeWrite)]
        public async Task<IActionResult> Create([FromBody] CreatePaymentDto dto)
        {
            if (!ModelState.IsValid) return ValidationErrorResponse(ModelState);
            try
            {
                // CurrentUserId (from BaseApiController) throws UnauthorizedAccessException if claim missing
                var result = await paymentService.CreateAsync(
                    dto, GetUserSchoolIdOrNullWithValidation(), IsSuperAdmin, CurrentUserId);

                await LogUserActivityAsync("payment.create",
                    $"Created payment '{result.PaymentReference}' — {result.Amount:N2}");

                return CreatedResponse($"api/finance/payments/{result.Id}", result, "Payment created successfully.");
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(ex.Message); }
        }

        // ─── UPDATE  →  PUT /api/finance/payments/{id} ───────────────────────────────

        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionKeys.FeeWrite)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePaymentDto dto)
        {
            if (!ModelState.IsValid) return ValidationErrorResponse(ModelState);
            try
            {
                var result = await paymentService.UpdateAsync(
                    id, dto, GetUserSchoolIdOrNullWithValidation(), IsSuperAdmin, CurrentUserId);

                await LogUserActivityAsync("payment.update", $"Updated payment '{result.PaymentReference}'");

                return SuccessResponse(result, "Payment updated successfully.");
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(ex.Message); }
        }

        // ─── DELETE  →  DELETE /api/finance/payments/{id} ───────────────────────────

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionKeys.FeeWrite)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await paymentService.DeleteAsync(id, GetUserSchoolIdOrNullWithValidation(), IsSuperAdmin);

                await LogUserActivityAsync("payment.delete", $"Deleted payment ID: {id}");

                return SuccessResponse("Payment deleted successfully.");
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(ex.Message); }
        }

        // ─── REVERSE  →  POST /api/finance/payments/{id}/reverse ────────────────────

        [HttpPost("{id:guid}/reverse")]
        [Authorize(Policy = PermissionKeys.FeeWrite)]
        public async Task<IActionResult> Reverse(Guid id, [FromBody] ReversePaymentDto dto)
        {
            if (!ModelState.IsValid) return ValidationErrorResponse(ModelState);
            try
            {
                var result = await paymentService.ReverseAsync(
                    id, dto, GetUserSchoolIdOrNullWithValidation(), IsSuperAdmin, CurrentUserId);

                await LogUserActivityAsync("payment.reverse",
                    $"Reversed → '{result.PaymentReference}'. Reason: {dto.ReversalReason}");

                return CreatedResponse($"api/finance/payments/{result.Id}", result, "Payment reversed successfully.");
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(ex.Message); }
        }

        // ─── BULK CREATE  →  POST /api/finance/payments/bulk ────────────────────────

        [HttpPost("bulk")]
        [Authorize(Policy = PermissionKeys.FeeWrite)]
        public async Task<IActionResult> BulkCreate([FromBody] BulkPaymentDto dto)
        {
            if (!ModelState.IsValid) return ValidationErrorResponse(ModelState);
            try
            {
                var result = await paymentService.BulkCreateAsync(
                    dto, GetUserSchoolIdOrNullWithValidation(), IsSuperAdmin, CurrentUserId);

                await LogUserActivityAsync("payment.bulk-create",
                    $"Bulk: {result.Succeeded}/{result.TotalRequested} succeeded. " +
                    $"Total: {result.TotalAmountPosted:N2}. Failed: {result.Failed}");

                return SuccessResponse(result,
                    $"Bulk payment completed: {result.Succeeded} succeeded, {result.Failed} failed.");
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(ex.Message); }
        }
    }
}