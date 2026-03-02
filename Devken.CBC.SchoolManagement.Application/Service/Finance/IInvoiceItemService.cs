using Devken.CBC.SchoolManagement.Application.DTOs.Finance;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Finance
{
    public interface IInvoiceItemService
    {
        Task<IEnumerable<InvoiceItemResponseDto>> GetByInvoiceAsync(Guid invoiceId);
        Task<InvoiceItemResponseDto?> GetByIdAsync(Guid invoiceId, Guid id);
        Task<InvoiceItemResponseDto> CreateAsync(Guid invoiceId, CreateInvoiceItemDto dto);
        Task<InvoiceItemResponseDto> UpdateAsync(Guid invoiceId, Guid id, UpdateInvoiceItemDto dto);
        Task DeleteAsync(Guid invoiceId, Guid id);
        Task<InvoiceItemResponseDto> RecomputeAsync(Guid invoiceId, Guid id, decimal? discountOverride);
    }
}