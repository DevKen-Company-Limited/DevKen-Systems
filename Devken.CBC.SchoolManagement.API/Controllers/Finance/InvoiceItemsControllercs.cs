using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Finance;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.Finance;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Finance
{
    [Route("api/finance/[controller]")]
    [ApiController]
    [Authorize]
    public class InvoiceItemsController : BaseApiController
    {
        private readonly IInvoiceItemService _service;
        private readonly IRepositoryManager _repositories;

        public InvoiceItemsController(
            IInvoiceItemService service,
            IRepositoryManager repositories,
            IUserActivityService? activityService = null,
            ILogger<InvoiceItemsController>? logger = null)
            : base(activityService, logger)
        {
            _service      = service      ?? throw new ArgumentNullException(nameof(service));
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
        }

        // ───────────────────────────────────────────────────────────────────
        // GET  /api/invoices/{invoiceId}/items
        // ───────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetByInvoice(Guid invoiceId)
        {
            if (!HasPermission(PermissionKeys.InvoiceItemRead))
                return ForbiddenResponse("You do not have permission to view invoice items.");

            var accessError = await ValidateInvoiceAccessAsync(invoiceId);
            if (accessError != null) return accessError;

            var items = await _service.GetByInvoiceAsync(invoiceId);
            return SuccessResponse(items);
        }

        // ───────────────────────────────────────────────────────────────────
        // GET  /api/invoices/{invoiceId}/items/{id}
        // ───────────────────────────────────────────────────────────────────

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid invoiceId, Guid id)
        {
            if (!HasPermission(PermissionKeys.InvoiceItemRead))
                return ForbiddenResponse("You do not have permission to view invoice items.");

            var accessError = await ValidateInvoiceAccessAsync(invoiceId);
            if (accessError != null) return accessError;

            var item = await _service.GetByIdAsync(invoiceId, id);
            if (item == null) return NotFoundResponse("Invoice item not found.");

            return SuccessResponse(item);
        }

        // ───────────────────────────────────────────────────────────────────
        // POST  /api/invoices/{invoiceId}/items
        // ───────────────────────────────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> Create(Guid invoiceId, [FromBody] CreateInvoiceItemDto dto)
        {
            if (!HasPermission(PermissionKeys.InvoiceItemWrite))
                return ForbiddenResponse("You do not have permission to create invoice items.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState.ToDictionary(
                    k => k.Key,
                    v => v.Value!.Errors.Select(e => e.ErrorMessage).ToArray()));

            if (dto.InvoiceId != invoiceId)
                return ValidationErrorResponse("InvoiceId in body does not match the route.");

            var accessError = await ValidateInvoiceAccessAsync(invoiceId);
            if (accessError != null) return accessError;

            var item = await _service.CreateAsync(invoiceId, dto);

            await LogUserActivityAsync(
                "invoiceitem.create",
                $"Added item '{item.Description}' to invoice {invoiceId}");

            return CreatedResponse(
                $"api/invoices/{invoiceId}/items/{item.Id}",
                item,
                "Invoice item created successfully.");
        }

        // ───────────────────────────────────────────────────────────────────
        // PUT  /api/invoices/{invoiceId}/items/{id}
        // ───────────────────────────────────────────────────────────────────

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(
            Guid invoiceId, Guid id, [FromBody] UpdateInvoiceItemDto dto)
        {
            if (!HasPermission(PermissionKeys.InvoiceItemWrite))
                return ForbiddenResponse("You do not have permission to update invoice items.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState.ToDictionary(
                    k => k.Key,
                    v => v.Value!.Errors.Select(e => e.ErrorMessage).ToArray()));

            var accessError = await ValidateInvoiceAccessAsync(invoiceId);
            if (accessError != null) return accessError;

            var item = await _service.UpdateAsync(invoiceId, id, dto);

            await LogUserActivityAsync(
                "invoiceitem.update",
                $"Updated item '{item.Description}' on invoice {invoiceId}");

            return SuccessResponse(item, "Invoice item updated successfully.");
        }

        // ───────────────────────────────────────────────────────────────────
        // DELETE  /api/invoices/{invoiceId}/items/{id}
        // ───────────────────────────────────────────────────────────────────

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid invoiceId, Guid id)
        {
            if (!HasPermission(PermissionKeys.InvoiceItemWrite))
                return ForbiddenResponse("You do not have permission to delete invoice items.");

            var accessError = await ValidateInvoiceAccessAsync(invoiceId);
            if (accessError != null) return accessError;

            await _service.DeleteAsync(invoiceId, id);

            await LogUserActivityAsync(
                "invoiceitem.delete",
                $"Deleted item '{id}' from invoice {invoiceId}");

            return SuccessResponse<object?>(null, "Invoice item deleted successfully.");
        }

        // ───────────────────────────────────────────────────────────────────
        // PATCH  /api/invoices/{invoiceId}/items/{id}/recompute
        // ───────────────────────────────────────────────────────────────────

        [HttpPatch("{id:guid}/recompute")]
        public async Task<IActionResult> Recompute(
            Guid invoiceId, Guid id, [FromQuery] decimal? discountOverride = null)
        {
            if (!HasPermission(PermissionKeys.InvoiceItemWrite))
                return ForbiddenResponse("You do not have permission to recompute invoice items.");

            var accessError = await ValidateInvoiceAccessAsync(invoiceId);
            if (accessError != null) return accessError;

            var item = await _service.RecomputeAsync(invoiceId, id, discountOverride);

            await LogUserActivityAsync(
                "invoiceitem.recompute",
                $"Recomputed financials for item '{id}' on invoice {invoiceId}");

            return SuccessResponse(item, "Invoice item financials recomputed.");
        }

        // ───────────────────────────────────────────────────────────────────
        // Private — HTTP-level guard only (no business logic)
        // ───────────────────────────────────────────────────────────────────

        private async Task<IActionResult?> ValidateInvoiceAccessAsync(Guid invoiceId)
        {
            var invoice = await _repositories.Context
                .Set<Domain.Entities.Finance.Invoice>()
                .FindAsync(invoiceId);

            if (invoice == null)
                return NotFoundResponse("Invoice not found.");

            return ValidateTenantAccess(invoice.TenantId);
        }
    }
}