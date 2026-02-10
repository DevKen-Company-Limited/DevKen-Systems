using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Payments
{
    public class MpesaPaymentRecord : TenantBaseEntity<Guid>
    {
        public string CheckoutRequestId { get; set; } = null!;
        public string MerchantRequestId { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;

        public decimal Amount { get; set; }

        public string AccountReference { get; set; } = null!;
        public string TransactionDesc { get; set; } = null!;

        /// <summary>
        /// Logical payment lifecycle status
        /// </summary>
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        /// <summary>
        /// Raw Mpesa result code from callback
        /// </summary>
        public MpesaResultCode? ResultCode { get; set; }

        public string? ResultDesc { get; set; }

        public string? MpesaReceiptNumber { get; set; }
        public DateTime? TransactionDate { get; set; }
    }
}
