using Devken.CBC.SchoolManagement.Application.DTOs.Finance;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Finance
{
    public interface IInvoiceItemService
    {
        // ─────────────────────────────────────────────────────────────────────
        // Queries
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns all invoice items, optionally filtered by tenant.
        /// SuperAdmin may pass any schoolId or null (all schools).
        /// Regular users are scoped to their own school automatically.
        /// </summary>
        Task<IEnumerable<InvoiceItemResponseDto>> GetAllInvoiceItemsAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin,
            Guid? invoiceId = null);

        Task<InvoiceItemResponseDto> GetInvoiceItemByIdAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin);

        Task<IEnumerable<InvoiceItemResponseDto>> GetByInvoiceAsync(Guid invoiceId);

        // ─────────────────────────────────────────────────────────────────────
        // Commands
        // ─────────────────────────────────────────────────────────────────────

        Task<InvoiceItemResponseDto> CreateAsync(
            Guid invoiceId, CreateInvoiceItemDto dto);

        Task<InvoiceItemResponseDto> UpdateAsync(
            Guid invoiceId, Guid id, UpdateInvoiceItemDto dto);

        Task DeleteAsync(Guid invoiceId, Guid id);

        Task<InvoiceItemResponseDto> RecomputeAsync(
            Guid invoiceId, Guid id, decimal? discountOverride);

        Task<InvoiceItemResponseDto> CreateInvoiceItemAsync(
            CreateInvoiceItemDto dto, Guid? userSchoolId, bool isSuperAdmin);
    }
}