using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Invoices;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.Finance;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Explicitly alias to avoid ambiguity with Application.Exceptions.ValidationException
using DataValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Finance
{
    [Route("api/finance/[controller]")]
    [ApiController]
    [Authorize]
    public class InvoicesController : BaseApiController
    {
        private readonly IInvoiceService _invoiceService;

        public InvoicesController(
            IInvoiceService invoiceService,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _invoiceService = invoiceService ?? throw new ArgumentNullException(nameof(invoiceService));
        }

        private string GetFullExceptionMessage(Exception ex)
        {
            var message = ex.Message;
            var inner = ex.InnerException;
            while (inner != null) { message += $" | Inner: {inner.Message}"; inner = inner.InnerException; }
            return message;
        }

        // GET /api/finance/invoices
        [HttpGet]
        [Authorize(Policy = PermissionKeys.InvoiceRead)]
        public async Task<IActionResult> GetAll(
            [FromQuery] InvoiceQueryDto query,
            [FromQuery] Guid? schoolId = null)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var result = await _invoiceService.GetAllInvoicesAsync(
                    schoolId: schoolId,
                    userSchoolId: userSchoolId,
                    isSuperAdmin: IsSuperAdmin,
                    query: query);

                return SuccessResponse(result);
            }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (DataValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // GET /api/finance/invoices/{id}
        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionKeys.InvoiceRead)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var result = await _invoiceService.GetInvoiceByIdAsync(
                    id: id,
                    userSchoolId: GetUserSchoolIdOrNullWithValidation(),
                    isSuperAdmin: IsSuperAdmin);

                return SuccessResponse(result);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // GET /api/finance/invoices/by-student/{studentId}
        [HttpGet("by-student/{studentId:guid}")]
        [Authorize(Policy = PermissionKeys.InvoiceRead)]
        public async Task<IActionResult> GetByStudent(Guid studentId)
        {
            try
            {
                var result = await _invoiceService.GetInvoicesByStudentAsync(
                    studentId: studentId,
                    userSchoolId: GetUserSchoolIdOrNullWithValidation(),
                    isSuperAdmin: IsSuperAdmin);

                return SuccessResponse(result);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // GET /api/finance/invoices/by-parent/{parentId}
        [HttpGet("by-parent/{parentId:guid}")]
        [Authorize(Policy = PermissionKeys.InvoiceRead)]
        public async Task<IActionResult> GetByParent(Guid parentId)
        {
            try
            {
                var result = await _invoiceService.GetInvoicesByParentAsync(
                    parentId: parentId,
                    userSchoolId: GetUserSchoolIdOrNullWithValidation(),
                    isSuperAdmin: IsSuperAdmin);

                return SuccessResponse(result);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // POST /api/finance/invoices
        [HttpPost]
        [Authorize(Policy = PermissionKeys.InvoiceWrite)]
        public async Task<IActionResult> Create([FromBody] CreateInvoiceDto dto)
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

                var invoice = await _invoiceService.CreateInvoiceAsync(
                    dto: dto,
                    userSchoolId: userSchoolId,
                    isSuperAdmin: IsSuperAdmin);

                await LogUserActivityAsync("invoice.create",
                    $"Created invoice: {invoice.InvoiceNumber} for student {invoice.StudentName} (ID: {invoice.Id})");

                return CreatedResponse(
                    $"api/finance/invoices/{invoice.Id}",
                    invoice,
                    "Invoice created successfully.");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (DataValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (InvalidOperationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // PUT /api/finance/invoices/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionKeys.InvoiceWrite)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInvoiceDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var result = await _invoiceService.UpdateInvoiceAsync(
                    id: id,
                    dto: dto,
                    userSchoolId: GetUserSchoolIdOrNullWithValidation(),
                    isSuperAdmin: IsSuperAdmin);

                await LogUserActivityAsync("invoice.update",
                    $"Updated invoice: {result.InvoiceNumber} (ID: {result.Id})");

                return SuccessResponse(result, "Invoice updated successfully.");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (DataValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (InvalidOperationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // PATCH /api/finance/invoices/{id}/apply-discount
        [HttpPatch("{id:guid}/apply-discount")]
        [Authorize(Policy = PermissionKeys.InvoiceWrite)]
        public async Task<IActionResult> ApplyDiscount(Guid id, [FromBody] ApplyDiscountDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var result = await _invoiceService.ApplyDiscountAsync(
                    id: id,
                    dto: dto,
                    userSchoolId: GetUserSchoolIdOrNullWithValidation(),
                    isSuperAdmin: IsSuperAdmin);

                await LogUserActivityAsync("invoice.discount",
                    $"Applied discount of {dto.DiscountAmount} to invoice {result.InvoiceNumber} (ID: {result.Id})");

                return SuccessResponse(result, "Discount applied successfully.");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (InvalidOperationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // PATCH /api/finance/invoices/{id}/cancel
        [HttpPatch("{id:guid}/cancel")]
        [Authorize(Policy = PermissionKeys.InvoiceWrite)]
        public async Task<IActionResult> Cancel(Guid id)
        {
            try
            {
                var result = await _invoiceService.CancelInvoiceAsync(
                    id: id,
                    userSchoolId: GetUserSchoolIdOrNullWithValidation(),
                    isSuperAdmin: IsSuperAdmin);

                await LogUserActivityAsync("invoice.cancel",
                    $"Cancelled invoice: {result.InvoiceNumber} (ID: {result.Id})");

                return SuccessResponse(result, "Invoice cancelled successfully.");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (InvalidOperationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // DELETE /api/finance/invoices/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionKeys.InvoiceWrite)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _invoiceService.DeleteInvoiceAsync(
                    id: id,
                    userSchoolId: GetUserSchoolIdOrNullWithValidation(),
                    isSuperAdmin: IsSuperAdmin);

                await LogUserActivityAsync("invoice.delete", $"Deleted invoice ID: {id}");

                return SuccessResponse("Invoice deleted successfully.");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (DataValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (InvalidOperationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }
    }
}