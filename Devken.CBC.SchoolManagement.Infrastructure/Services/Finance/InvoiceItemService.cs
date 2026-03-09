using Devken.CBC.SchoolManagement.Application.DTOs.Finance;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Finance;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
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

        /// <summary>
        /// Returns all invoice items, scoped by tenant for regular users.
        /// SuperAdmin may pass any schoolId or null to get all schools.
        /// Optionally filtered by invoiceId.
        /// </summary>
        public async Task<IEnumerable<InvoiceItemResponseDto>> GetAllInvoiceItemsAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin,
            Guid? invoiceId = null)
        {
            IEnumerable<InvoiceItem> items;

            if (isSuperAdmin)
            {
                items = schoolId.HasValue
                    ? await _repositories.InvoiceItem.GetByTenantAsync(schoolId.Value, trackChanges: false)
                    : await _repositories.InvoiceItem.GetAllAsync(trackChanges: false);
            }
            else
            {
                if (!userSchoolId.HasValue)
                    throw new UnauthorizedException("You must be assigned to a school to view invoice items.");

                items = await _repositories.InvoiceItem.GetByTenantAsync(userSchoolId.Value, trackChanges: false);
            }

            if (invoiceId.HasValue)
                items = items.Where(x => x.InvoiceId == invoiceId.Value);

            var itemList = items.OrderByDescending(x => x.CreatedOn).ToList();

            var schoolNameMap = await BuildSchoolNameMapAsync(
                itemList.Select(x => x.TenantId).Distinct());

            return itemList.Select(x => MapToResponse(x, schoolNameMap.GetValueOrDefault(x.TenantId)));
        }

        public async Task<InvoiceItemResponseDto> GetInvoiceItemByIdAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var item = await _repositories.InvoiceItem.GetDetailAsync(id, trackChanges: false)
                ?? throw new NotFoundException($"Invoice item with ID '{id}' not found.");

            ValidateAccess(item.TenantId, userSchoolId, isSuperAdmin);

            var schoolName = await ResolveSchoolNameAsync(item.TenantId);
            return MapToResponse(item, schoolName);
        }

        public async Task<IEnumerable<InvoiceItemResponseDto>> GetByInvoiceAsync(Guid invoiceId)
        {
            var items = await _repositories.InvoiceItem
                .GetByInvoiceAsync(invoiceId, trackChanges: false);

            return items.Select(x => MapToResponse(x));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Commands
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates an invoice item from the controller's standalone POST endpoint.
        /// Mirrors FeeItemService.CreateFeeItemAsync — resolves tenant, validates access.
        /// </summary>
        public async Task<InvoiceItemResponseDto> CreateInvoiceItemAsync(
            CreateInvoiceItemDto dto, Guid? userSchoolId, bool isSuperAdmin)
        {
            var tenantId = ResolveTenantId(dto.TenantId, userSchoolId, isSuperAdmin);

            var school = await _repositories.School.GetByIdAsync(tenantId, trackChanges: false)
                ?? throw new NotFoundException($"School with ID '{tenantId}' not found.");

            var invoice = await _repositories.Context
                .Set<Invoice>()
                .FindAsync(dto.InvoiceId)
                ?? throw new NotFoundException($"Invoice '{dto.InvoiceId}' not found.");

            if (invoice.TenantId != tenantId)
                throw new UnauthorizedException("Invoice does not belong to the resolved school.");

            return await CreateItemAsync(invoice, dto, school.Name);
        }

        /// <summary>
        /// Creates an invoice item scoped to a route-level invoiceId.
        /// Used by the nested /invoices/{invoiceId}/items route.
        /// </summary>
        public async Task<InvoiceItemResponseDto> CreateAsync(
            Guid invoiceId, CreateInvoiceItemDto dto)
        {
            var invoice = await _repositories.Context
                .Set<Invoice>()
                .FindAsync(invoiceId)
                ?? throw new KeyNotFoundException($"Invoice '{invoiceId}' not found.");

            return await CreateItemAsync(invoice, dto, schoolName: null);
        }

        public async Task<InvoiceItemResponseDto> UpdateAsync(
            Guid invoiceId, Guid id, UpdateInvoiceItemDto dto)
        {
            var item = await _repositories.InvoiceItem
                .GetDetailAsync(id, trackChanges: true)
                ?? throw new NotFoundException("Invoice item not found.");

            if (item.InvoiceId != invoiceId)
                throw new NotFoundException("Invoice item not found.");

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

            var schoolName = await ResolveSchoolNameAsync(item.TenantId);
            return MapToResponse(item, schoolName);
        }

        public async Task DeleteAsync(Guid invoiceId, Guid id)
        {
            var item = await _repositories.InvoiceItem
                .GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException("Invoice item not found.");

            if (item.InvoiceId != invoiceId)
                throw new NotFoundException("Invoice item not found.");

            _repositories.InvoiceItem.Delete(item);
            await _repositories.SaveAsync();
        }

        public async Task<InvoiceItemResponseDto> RecomputeAsync(
            Guid invoiceId, Guid id, decimal? discountOverride)
        {
            var item = await _repositories.InvoiceItem
                .GetDetailAsync(id, trackChanges: true)
                ?? throw new NotFoundException("Invoice item not found.");

            if (item.InvoiceId != invoiceId)
                throw new NotFoundException("Invoice item not found.");

            item.Compute(discountOverride);

            _repositories.InvoiceItem.Update(item);
            await _repositories.SaveAsync();

            var schoolName = await ResolveSchoolNameAsync(item.TenantId);
            return MapToResponse(item, schoolName);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Shared creation logic — validates FeeItem ownership, builds entity,
        /// calls Compute(), persists, and maps to response.
        /// </summary>
        private async Task<InvoiceItemResponseDto> CreateItemAsync(
            Invoice invoice, CreateInvoiceItemDto dto, string? schoolName)
        {
            if (dto.FeeItemId.HasValue)
            {
                var feeItem = await _repositories.Context
                    .Set<FeeItem>()
                    .FindAsync(dto.FeeItemId.Value)
                    ?? throw new NotFoundException($"FeeItem '{dto.FeeItemId}' not found.");

                if (feeItem.TenantId != invoice.TenantId)
                    throw new UnauthorizedException("FeeItem does not belong to your school.");
            }

            var item = new InvoiceItem
            {
                Id = Guid.NewGuid(),
                TenantId = invoice.TenantId,
                InvoiceId = invoice.Id,
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

            schoolName ??= await ResolveSchoolNameAsync(item.TenantId);
            return MapToResponse(item, schoolName);
        }

        private async Task<string?> ResolveSchoolNameAsync(Guid tenantId)
        {
            var school = await _repositories.School.GetByIdAsync(tenantId, trackChanges: false);
            return school?.Name;
        }

        private async Task<Dictionary<Guid, string>> BuildSchoolNameMapAsync(
            IEnumerable<Guid> tenantIds)
        {
            var map = new Dictionary<Guid, string>();
            foreach (var tid in tenantIds)
            {
                var school = await _repositories.School.GetByIdAsync(tid, trackChanges: false);
                if (school != null)
                    map[tid] = school.Name;
            }
            return map;
        }

        private static Guid ResolveTenantId(
            Guid? requestTenantId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin)
            {
                if (!requestTenantId.HasValue || requestTenantId == Guid.Empty)
                    throw new System.ComponentModel.DataAnnotations.ValidationException(
                        "TenantId is required for SuperAdmin when creating an invoice item.");
                return requestTenantId.Value;
            }

            if (!userSchoolId.HasValue || userSchoolId == Guid.Empty)
                throw new UnauthorizedException(
                    "You must be assigned to a school to create invoice items.");

            return userSchoolId.Value;
        }

        private static void ValidateAccess(
            Guid itemTenantId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin) return;
            if (!userSchoolId.HasValue || itemTenantId != userSchoolId.Value)
                throw new UnauthorizedException("You do not have access to this invoice item.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Mapping
        // ─────────────────────────────────────────────────────────────────────

        private static InvoiceItemResponseDto MapToResponse(
            InvoiceItem item, string? schoolName = null) => new()
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
                TenantId = item.TenantId,
                SchoolName = schoolName,
                CreatedOn = item.CreatedOn,
                UpdatedOn = item.UpdatedOn
            };
    }
}