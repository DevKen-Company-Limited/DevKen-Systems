using Devken.CBC.SchoolManagement.Application.DTOs.Payments;
using Devken.CBC.SchoolManagement.Domain.Enums;

namespace Devken.CBC.SchoolManagement.Application.Service.Finance
{
    public interface IPaymentService
    {
        Task<IEnumerable<PaymentResponseDto>> GetAllAsync(
            Guid? schoolId,
            Guid? studentId,
            Guid? invoiceId,
            PaymentMethod? method,
            PaymentStatus? status,
            DateTime? from,
            DateTime? to,
            bool? isReversal,
            Guid? userSchoolId,
            bool isSuperAdmin);

        Task<PaymentResponseDto> GetByIdAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);
        Task<PaymentResponseDto> GetByReferenceAsync(string reference, Guid? userSchoolId, bool isSuperAdmin);
        Task<IEnumerable<PaymentResponseDto>> GetByStudentAsync(Guid studentId, Guid? userSchoolId, bool isSuperAdmin);
        Task<IEnumerable<PaymentResponseDto>> GetByInvoiceAsync(Guid invoiceId, Guid? userSchoolId, bool isSuperAdmin);
        Task<object> GetSummaryAsync(Guid? schoolId, Guid? studentId, DateTime? from, DateTime? to, Guid? userSchoolId, bool isSuperAdmin);

        Task<PaymentResponseDto> CreateAsync(CreatePaymentDto dto, Guid? userSchoolId, bool isSuperAdmin, Guid currentUserId);
        Task<PaymentResponseDto> UpdateAsync(Guid id, UpdatePaymentDto dto, Guid? userSchoolId, bool isSuperAdmin, Guid currentUserId);
        Task DeleteAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);
        Task<PaymentResponseDto> ReverseAsync(Guid id, ReversePaymentDto dto, Guid? userSchoolId, bool isSuperAdmin, Guid currentUserId);
        Task<BulkPaymentResultDto> BulkCreateAsync(BulkPaymentDto dto, Guid? userSchoolId, bool isSuperAdmin, Guid currentUserId);
    }
}