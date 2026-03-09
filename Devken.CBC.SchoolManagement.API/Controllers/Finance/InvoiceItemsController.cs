//using Devken.CBC.SchoolManagement.Api.Controllers.Common;
//using Devken.CBC.SchoolManagement.Application.DTOs.Finance;
//using Devken.CBC.SchoolManagement.Application.Exceptions;
//using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
//using Devken.CBC.SchoolManagement.Application.Service.Activities;
//using Devken.CBC.SchoolManagement.Application.Service.Finance;
//using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Threading.Tasks;

//namespace Devken.CBC.SchoolManagement.Api.Controllers.Finance
//{
//    [Route("api/finance/invoiceitems")]
//    [ApiController]
//    [Authorize]
//    public class InvoiceItemsController : BaseApiController
//    {
//        private readonly IInvoiceItemService _invoiceItemService;
//        private readonly IRepositoryManager _repositories;

//        public InvoiceItemsController(
//            IInvoiceItemService invoiceItemService,
//            IRepositoryManager repositories,
//            IUserActivityService? activityService = null,
//            ILogger<InvoiceItemsController>? logger = null)
//            : base(activityService, logger)
//        {
//            _invoiceItemService = invoiceItemService ?? throw new ArgumentNullException(nameof(invoiceItemService));
//            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
//        }

//        private static string GetFullExceptionMessage(Exception ex)
//        {
//            var message = ex.Message;
//            var inner = ex.InnerException;
//            while (inner != null) { message += $" | Inner: {inner.Message}"; inner = inner.InnerException; }
//            return message;
//        }

//        // GET /api/finance/invoiceitems
//        [HttpGet]
//        [Authorize(Policy = PermissionKeys.InvoiceItemRead)]
//        public async Task<IActionResult> GetAll(
//            [FromQuery] Guid? schoolId = null,
//            [FromQuery] Guid? invoiceId = null)
//        {
//            try
//            {
//                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
//                var targetSchoolId = IsSuperAdmin ? schoolId : userSchoolId;

//                var items = await _invoiceItemService.GetAllInvoiceItemsAsync(
//                    targetSchoolId, userSchoolId, IsSuperAdmin, invoiceId);

//                return SuccessResponse(items);
//            }
//            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
//            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
//            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
//        }

//        // GET /api/finance/invoiceitems/{id}
//        [HttpGet("{id:guid}")]
//        [Authorize(Policy = PermissionKeys.InvoiceItemRead)]
//        public async Task<IActionResult> GetById(Guid id)
//        {
//            try
//            {
//                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
//                var item = await _invoiceItemService.GetInvoiceItemByIdAsync(id, userSchoolId, IsSuperAdmin);
//                return SuccessResponse(item);
//            }
//            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
//            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
//            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
//            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
//        }

//        // GET /api/finance/invoiceitems/by-invoice/{invoiceId}
//        [HttpGet("by-invoice/{invoiceId:guid}")]
//        [Authorize(Policy = PermissionKeys.InvoiceItemRead)]
//        public async Task<IActionResult> GetByInvoice(Guid invoiceId)
//        {
//            try
//            {
//                var accessError = await ValidateInvoiceAccessAsync(invoiceId);
//                if (accessError != null) return accessError;

//                var items = await _invoiceItemService.GetByInvoiceAsync(invoiceId);
//                return SuccessResponse(items);
//            }
//            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
//            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
//            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
//        }

//        // POST /api/finance/invoiceitems
//        [HttpPost]
//        [Authorize(Policy = PermissionKeys.InvoiceItemWrite)]
//        public async Task<IActionResult> Create([FromBody] CreateInvoiceItemDto dto)
//        {
//            if (!ModelState.IsValid)
//                return ValidationErrorResponse(ModelState);

//            try
//            {
//                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

//                if (!IsSuperAdmin)
//                    dto.TenantId = userSchoolId;
//                else if (dto.TenantId == null || dto.TenantId == Guid.Empty)
//                    return ValidationErrorResponse("TenantId is required for SuperAdmin.");

//                var result = await _invoiceItemService.CreateInvoiceItemAsync(dto, userSchoolId, IsSuperAdmin);

//                await LogUserActivityAsync(
//                    "invoiceitem.create",
//                    $"Created invoice item '{result.Description}' on invoice {result.InvoiceId} in school {result.TenantId}");

//                return CreatedResponse($"api/finance/invoiceitems/{result.Id}", result, "Invoice item created successfully.");
//            }
//            catch (System.ComponentModel.DataAnnotations.ValidationException ex) { return ValidationErrorResponse(ex.Message); }
//            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
//            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
//            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
//            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
//            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
//        }

//        // PUT /api/finance/invoiceitems/{id}
//        [HttpPut("{id:guid}")]
//        [Authorize(Policy = PermissionKeys.InvoiceItemWrite)]
//        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInvoiceItemDto dto)
//        {
//            if (!ModelState.IsValid)
//                return ValidationErrorResponse(ModelState);

//            try
//            {
//                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

//                // Verify access before mutating — does NOT load the entity into the tracker,
//                // so UpdateAsync's own GetDetailAsync(trackChanges:true) fetch is clean.
//                var existing = await _invoiceItemService.GetInvoiceItemByIdAsync(id, userSchoolId, IsSuperAdmin);

//                var result = await _invoiceItemService.UpdateAsync(existing.InvoiceId, id, dto);

//                await LogUserActivityAsync(
//                    "invoiceitem.update",
//                    $"Updated invoice item '{result.Description}' [{id}]");

//                return SuccessResponse(result, "Invoice item updated successfully.");
//            }
//            catch (System.ComponentModel.DataAnnotations.ValidationException ex) { return ValidationErrorResponse(ex.Message); }
//            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
//            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
//            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
//            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
//        }

//        // DELETE /api/finance/invoiceitems/{id}
//        [HttpDelete("{id:guid}")]
//        [Authorize(Policy = PermissionKeys.InvoiceItemWrite)]
//        public async Task<IActionResult> Delete(Guid id)
//        {
//            try
//            {
//                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

//                // Verify access + resolve invoiceId without loading into tracker
//                var existing = await _invoiceItemService.GetInvoiceItemByIdAsync(id, userSchoolId, IsSuperAdmin);

//                await _invoiceItemService.DeleteAsync(existing.InvoiceId, id);

//                await LogUserActivityAsync("invoiceitem.delete", $"Deleted invoice item ID: {id}");

//                return SuccessResponse("Invoice item deleted successfully.");
//            }
//            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
//            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
//            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
//            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
//        }

//        // PATCH /api/finance/invoiceitems/{id}/recompute
//        [HttpPatch("{id:guid}/recompute")]
//        [Authorize(Policy = PermissionKeys.InvoiceItemWrite)]
//        public async Task<IActionResult> Recompute(
//            Guid id, [FromQuery] decimal? discountOverride = null)
//        {
//            try
//            {
//                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

//                // Verify access + resolve invoiceId without loading into tracker
//                var existing = await _invoiceItemService.GetInvoiceItemByIdAsync(id, userSchoolId, IsSuperAdmin);

//                var result = await _invoiceItemService.RecomputeAsync(existing.InvoiceId, id, discountOverride);

//                await LogUserActivityAsync(
//                    "invoiceitem.recompute",
//                    $"Recomputed financials for invoice item '{id}'");

//                return SuccessResponse(result, "Invoice item financials recomputed.");
//            }
//            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
//            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
//            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
//            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
//        }

//        // ── Private ───────────────────────────────────────────────────────────

//        private async Task<IActionResult?> ValidateInvoiceAccessAsync(Guid invoiceId)
//        {
//            var invoice = await _repositories.Context
//                .Set<Domain.Entities.Finance.Invoice>()
//                .FindAsync(invoiceId);

//            if (invoice == null)
//                return NotFoundResponse("Invoice not found.");

//            return ValidateTenantAccess(invoice.TenantId);
//        }
//    }
//}