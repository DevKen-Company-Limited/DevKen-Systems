using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Finance
{
    public class Payment : TenantBaseEntity<Guid>
    {
        // ─── Identity ───────────────────────────────────────────────────────────────

        [Required]
        [MaxLength(50)]
        public string PaymentReference { get; set; } = null!;

        [MaxLength(30)]
        public string? ReceiptNumber { get; set; }

        // ─── Foreign Keys ────────────────────────────────────────────────────────────

        public Guid StudentId { get; set; }
        public Guid InvoiceId { get; set; }
        public Guid? ReceivedBy { get; set; } // FK → Staff.Id

        // ─── Core Fields ─────────────────────────────────────────────────────────────

        public DateTime PaymentDate { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        public PaymentStatus StatusPayment { get; set; } = PaymentStatus.Completed;

        [MaxLength(100)]
        public string? TransactionReference { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // ─── M-Pesa ──────────────────────────────────────────────────────────────────

        [MaxLength(20)]
        public string? MpesaCode { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        // ─── Bank Transfer / Cheque ──────────────────────────────────────────────────

        [MaxLength(100)]
        public string? BankName { get; set; }

        [MaxLength(50)]
        public string? AccountNumber { get; set; }

        [MaxLength(50)]
        public string? ChequeNumber { get; set; }

        public DateTime? ChequeClearanceDate { get; set; }

        // ─── Reversal Support ────────────────────────────────────────────────────────

        public Guid? ReversedFromPaymentId { get; set; }   // Points to original payment
        public bool IsReversal { get; set; } = false;

        [MaxLength(500)]
        public string? ReversalReason { get; set; }

        // ─── Navigation ──────────────────────────────────────────────────────────────

        public Student Student { get; set; } = null!;
        public Invoice Invoice { get; set; } = null!;
        public Staff? ReceivedByStaff { get; set; }

        // Self-referencing for reversals
        public Payment? ReversedFromPayment { get; set; }
        public Payment? ReversalPayment { get; set; }

        // ─── Domain Methods ──────────────────────────────────────────────────────────

        public bool IsCompleted => StatusPayment == PaymentStatus.Completed;
        public bool IsMpesa => PaymentMethod == PaymentMethod.Mpesa;
    }
}