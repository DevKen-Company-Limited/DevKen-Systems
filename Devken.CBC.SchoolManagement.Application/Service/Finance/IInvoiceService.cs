using Devken.CBC.SchoolManagement.Application.DTOs.Invoices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Finance
{
    public interface IInvoiceService
    {
        Task<IEnumerable<InvoiceSummaryResponseDto>> GetAllInvoicesAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin,
            InvoiceQueryDto query);

        Task<InvoiceResponseDto> GetInvoiceByIdAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin);

        Task<IEnumerable<InvoiceSummaryResponseDto>> GetInvoicesByStudentAsync(
            Guid studentId,
            Guid? userSchoolId,
            bool isSuperAdmin);

        Task<IEnumerable<InvoiceSummaryResponseDto>> GetInvoicesByParentAsync(
            Guid parentId,
            Guid? userSchoolId,
            bool isSuperAdmin);

        Task<InvoiceResponseDto> CreateInvoiceAsync(
            CreateInvoiceDto dto,
            Guid? userSchoolId,
            bool isSuperAdmin);

        Task<InvoiceResponseDto> UpdateInvoiceAsync(
            Guid id,
            UpdateInvoiceDto dto,
            Guid? userSchoolId,
            bool isSuperAdmin);

        Task<InvoiceResponseDto> ApplyDiscountAsync(
            Guid id,
            ApplyDiscountDto dto,
            Guid? userSchoolId,
            bool isSuperAdmin);

        Task<InvoiceResponseDto> CancelInvoiceAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin);

        Task DeleteInvoiceAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin);
    }
}