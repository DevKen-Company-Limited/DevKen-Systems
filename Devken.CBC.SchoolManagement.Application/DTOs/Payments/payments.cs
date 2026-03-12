using Devken.CBC.SchoolManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Payments
{
    // ══════════════════════════════════════════════════════════════════════════
    //  RESPONSE
    // ══════════════════════════════════════════════════════════════════════════

    public class PaymentResponseDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public string PaymentReference { get; set; } = null!;
        public string? ReceiptNumber { get; set; }

        public Guid StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? AdmissionNumber { get; set; }

        public Guid InvoiceId { get; set; }
        public string? InvoiceNumber { get; set; }

        public Guid? ReceivedBy { get; set; }
        public string? ReceivedByName { get; set; }

        public DateTime PaymentDate { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = null!;
        public string StatusPayment { get; set; } = null!;
        public string? TransactionReference { get; set; }
        public string? Description { get; set; }
        public string? Notes { get; set; }

        public string? MpesaCode { get; set; }
        public string? PhoneNumber { get; set; }

        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? ChequeNumber { get; set; }
        public DateTime? ChequeClearanceDate { get; set; }

        public Guid? ReversedFromPaymentId { get; set; }
        public bool IsReversal { get; set; }
        public string? ReversalReason { get; set; }

        public bool IsCompleted { get; set; }
        public bool IsMpesa { get; set; }

        // BaseEntity<TId> audit fields
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  CREATE
    // ══════════════════════════════════════════════════════════════════════════

    public class CreatePaymentDto
    {
        public Guid? TenantId { get; set; }   // resolved server-side for non-SuperAdmin

        [Required] public Guid StudentId { get; set; }
        [Required] public Guid InvoiceId { get; set; }
        public Guid? ReceivedBy { get; set; }

        [Required] public DateTime PaymentDate { get; set; }
        public DateTime? ReceivedDate { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Required] public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        public PaymentStatus StatusPayment { get; set; } = PaymentStatus.Completed;

        [MaxLength(100)] public string? TransactionReference { get; set; }
        [MaxLength(500)] public string? Description { get; set; }
        [MaxLength(1000)] public string? Notes { get; set; }

        [MaxLength(20)] public string? MpesaCode { get; set; }
        [MaxLength(20)] public string? PhoneNumber { get; set; }

        [MaxLength(100)] public string? BankName { get; set; }
        [MaxLength(50)] public string? AccountNumber { get; set; }
        [MaxLength(50)] public string? ChequeNumber { get; set; }
        public DateTime? ChequeClearanceDate { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  UPDATE  (partial — null = leave unchanged)
    // ══════════════════════════════════════════════════════════════════════════

    public class UpdatePaymentDto
    {
        public DateTime? PaymentDate { get; set; }
        public DateTime? ReceivedDate { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal? Amount { get; set; }

        public PaymentMethod? PaymentMethod { get; set; }
        public PaymentStatus? StatusPayment { get; set; }
        public Guid? ReceivedBy { get; set; }

        [MaxLength(100)] public string? TransactionReference { get; set; }
        [MaxLength(500)] public string? Description { get; set; }
        [MaxLength(1000)] public string? Notes { get; set; }

        [MaxLength(20)] public string? MpesaCode { get; set; }
        [MaxLength(20)] public string? PhoneNumber { get; set; }

        [MaxLength(100)] public string? BankName { get; set; }
        [MaxLength(50)] public string? AccountNumber { get; set; }
        [MaxLength(50)] public string? ChequeNumber { get; set; }
        public DateTime? ChequeClearanceDate { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  REVERSAL
    // ══════════════════════════════════════════════════════════════════════════

    public class ReversePaymentDto
    {
        [Required]
        [MaxLength(500)]
        public string ReversalReason { get; set; } = null!;

        public Guid? ReceivedBy { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  BULK  (CBC — whole class / grade payment run)
    // ══════════════════════════════════════════════════════════════════════════

    public class BulkPaymentItemDto
    {
        [Required] public Guid StudentId { get; set; }
        [Required] public Guid InvoiceId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [MaxLength(20)] public string? MpesaCode { get; set; }
        [MaxLength(20)] public string? PhoneNumber { get; set; }
        [MaxLength(100)] public string? TransactionReference { get; set; }
        [MaxLength(500)] public string? Notes { get; set; }
    }

    public class BulkPaymentDto
    {
        public Guid? TenantId { get; set; }   // resolved server-side for non-SuperAdmin

        [Required] public DateTime PaymentDate { get; set; }
        [Required] public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        public PaymentStatus StatusPayment { get; set; } = PaymentStatus.Completed;

        public Guid? ReceivedBy { get; set; }

        [MaxLength(500)] public string? Description { get; set; }
        [MaxLength(100)] public string? BankName { get; set; }
        [MaxLength(50)] public string? AccountNumber { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one payment item is required.")]
        public List<BulkPaymentItemDto> Payments { get; set; } = new();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  BULK RESULT
    // ══════════════════════════════════════════════════════════════════════════

    public class BulkPaymentResultDto
    {
        public int TotalRequested { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }
        public decimal TotalAmountPosted { get; set; }

        public List<PaymentResponseDto> CreatedPayments { get; set; } = new();
        public List<BulkPaymentErrorDto> Errors { get; set; } = new();
    }

    public class BulkPaymentErrorDto
    {
        public Guid StudentId { get; set; }
        public Guid InvoiceId { get; set; }
        public string Reason { get; set; } = null!;
    }
}