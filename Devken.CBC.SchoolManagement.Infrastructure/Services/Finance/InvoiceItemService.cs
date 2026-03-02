using Devken.CBC.SchoolManagement.Application.DTOs.Finance;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Finance;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Finance
{
    public class InvoiceItemService : IInvoiceItemService
    {
        private readonly IRepositoryManager _repositories;

        public InvoiceItemService(IRepositoryManager repositories)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Queries
        // ─────────────────────────────────────────────────────────────────────

        public async Task<IEnumerable<InvoiceItemResponseDto>> GetByInvoiceAsync(Guid invoiceId)
        {
            var items = await _repositories.InvoiceItem
                .GetByInvoiceAsync(invoiceId, trackChanges: false);

            return items.Select(MapToResponse);
        }

        public async Task<InvoiceItemResponseDto?> GetByIdAsync(Guid invoiceId, Guid id)
        {
            var item = await _repositories.InvoiceItem
                .GetDetailAsync(id, trackChanges: false);

            if (item == null || item.InvoiceId != invoiceId)
                return null;

            return MapToResponse(item);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Commands
        // ─────────────────────────────────────────────────────────────────────

        public async Task<InvoiceItemResponseDto> CreateAsync(Guid invoiceId, CreateInvoiceItemDto dto)
        {
            var invoice = await _repositories.Context
                .Set<Invoice>()
                .FindAsync(invoiceId)
                ?? throw new KeyNotFoundException($"Invoice '{invoiceId}' not found.");

            if (dto.FeeItemId.HasValue)
            {
                var feeItem = await _repositories.FeeItem.GetByIdAsync(dto.FeeItemId.Value)
                    ?? throw new KeyNotFoundException($"FeeItem '{dto.FeeItemId}' not found.");

                if (feeItem.TenantId != invoice.TenantId)
                    throw new UnauthorizedAccessException("FeeItem does not belong to your school.");
            }

            var item = new InvoiceItem
            {
                Id = Guid.NewGuid(),
                TenantId = invoice.TenantId,
                InvoiceId = invoiceId,
                FeeItemId = dto.FeeItemId,
                TermId = dto.TermId,
                Description = dto.Description,
                ItemType = dto.ItemType,
                Quantity = dto.Quantity,
                UnitPrice = dto.UnitPrice,
                Discount = dto.Discount,
                IsTaxable = dto.IsTaxable,
                TaxRate = dto.TaxRate,
                GlCode = dto.GlCode,
                Notes = dto.Notes
            };

            item.Compute(dto.DiscountOverride);

            _repositories.InvoiceItem.Create(item);
            await _repositories.SaveAsync();

            return MapToResponse(item);
        }

        public async Task<InvoiceItemResponseDto> UpdateAsync(
            Guid invoiceId, Guid id, UpdateInvoiceItemDto dto)
        {
            var item = await _repositories.InvoiceItem
                .GetDetailAsync(id, trackChanges: true)
                ?? throw new KeyNotFoundException("Invoice item not found.");

            if (item.InvoiceId != invoiceId)
                throw new KeyNotFoundException("Invoice item not found.");

            item.Description = dto.Description;
            item.ItemType = dto.ItemType;
            item.Quantity = dto.Quantity;
            item.UnitPrice = dto.UnitPrice;
            item.Discount = dto.Discount;
            item.IsTaxable = dto.IsTaxable;
            item.TaxRate = dto.TaxRate;
            item.GlCode = dto.GlCode;
            item.Notes = dto.Notes;

            item.Compute(dto.DiscountOverride);

            _repositories.InvoiceItem.Update(item);
            await _repositories.SaveAsync();

            return MapToResponse(item);
        }

        public async Task DeleteAsync(Guid invoiceId, Guid id)
        {
            var item = await _repositories.InvoiceItem
                .GetByIdAsync(id, trackChanges: true)
                ?? throw new KeyNotFoundException("Invoice item not found.");

            if (item.InvoiceId != invoiceId)
                throw new KeyNotFoundException("Invoice item not found.");

            _repositories.InvoiceItem.Delete(item);
            await _repositories.SaveAsync();
        }

        public async Task<InvoiceItemResponseDto> RecomputeAsync(
            Guid invoiceId, Guid id, decimal? discountOverride)
        {
            var item = await _repositories.InvoiceItem
                .GetDetailAsync(id, trackChanges: true)
                ?? throw new KeyNotFoundException("Invoice item not found.");

            if (item.InvoiceId != invoiceId)
                throw new KeyNotFoundException("Invoice item not found.");

            item.Compute(discountOverride);

            _repositories.InvoiceItem.Update(item);
            await _repositories.SaveAsync();

            return MapToResponse(item);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Mapping
        // ─────────────────────────────────────────────────────────────────────

        private static InvoiceItemResponseDto MapToResponse(InvoiceItem item) =>
            new()
            {
                Id = item.Id!,
                InvoiceId = item.InvoiceId,
                FeeItemId = item.FeeItemId,
                TermId = item.TermId,
                Description = item.Description,
                ItemType = item.ItemType,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Discount = item.Discount,
                IsTaxable = item.IsTaxable,
                TaxRate = item.TaxRate,
                Total = item.Total,
                TaxAmount = item.TaxAmount,
                NetAmount = item.NetAmount,
                EffectiveUnitPrice = item.EffectiveUnitPrice,
                GlCode = item.GlCode,
                Notes = item.Notes,
                CreatedOn = item.CreatedOn,
                UpdatedOn = item.UpdatedOn
            };
    }
}