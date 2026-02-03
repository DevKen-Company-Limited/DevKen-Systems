using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Finance
{
    public class Payment : TenantBaseEntity<Guid>
    {
        [Required]
        [MaxLength(50)]
        public string PaymentReference { get; set; } = null!;

        public Guid StudentId { get; set; }

        public Guid InvoiceId { get; set; }

        public DateTime PaymentDate { get; set; }

        public decimal Amount { get; set; }

        [MaxLength(50)]
        public string PaymentMethod { get; set; } = null!; // Cash, Mpesa, BankTransfer, Cheque

        [MaxLength(100)]
        public string? TransactionReference { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Completed;

        public DateTime? ReceivedDate { get; set; }

        public Guid? ReceivedBy { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public string? ReceiptNumber { get; set; }

        // For Mpesa
        [MaxLength(20)]
        public string? MpesaCode { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        // For Bank Transfer
        [MaxLength(100)]
        public string? BankName { get; set; }

        [MaxLength(50)]
        public string? AccountNumber { get; set; }

        // Navigation Properties
        public Student Student { get; set; } = null!;
        public Invoice Invoice { get; set; } = null!;
        public Staff? ReceivedByStaff { get; set; }
    }
}

public enum PaymentStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3,
    Refunded = 4,
    Cancelled = 5
}