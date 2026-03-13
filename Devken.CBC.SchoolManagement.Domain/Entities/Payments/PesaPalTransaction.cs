using System;
using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Enums;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Payments
{
    public sealed class PesaPalTransaction : TenantBaseEntity<Guid>
    {
        // ── PesaPal identifiers ──────────────────────────────────────

        /// <summary>
        /// Unique identifier assigned by PesaPal when the order is submitted.
        /// Used for status polling and IPN reconciliation.
        /// </summary>
        public string OrderTrackingId { get; set; } = string.Empty;

        /// <summary>
        /// Our own internal reference (e.g. "BULK-1717000000-ABC12").
        /// </summary>
        public string MerchantReference { get; set; } = string.Empty;

        // ── Payment details ──────────────────────────────────────────

        public decimal Amount { get; set; }

        public string Currency { get; set; } = "KES";

        /// <summary>Human-readable description sent to PesaPal.</summary>
        public string? Description { get; set; }

        // ── Status ───────────────────────────────────────────────────

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        public PaymentMethod? PaymentMethod { get; set; }

        public string? ConfirmationCode { get; set; }

        public string? PaymentAccount { get; set; }

        public string? ErrorMessage { get; set; }

        // ── Billing snapshot ─────────────────────────────────────────

        public string? PayerFirstName { get; set; }

        public string? PayerLastName { get; set; }

        public string? PayerEmail { get; set; }

        public string? PayerPhone { get; set; }

        // ── Timestamps ───────────────────────────────────────────────

        public DateTime? CompletedOn { get; set; }
    }
}