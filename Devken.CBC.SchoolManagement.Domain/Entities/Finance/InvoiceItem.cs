using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Finance
{
    public class InvoiceItem : TenantBaseEntity<Guid>
    {
        // ─── Foreign Keys ────────────────────────────────────────────────────────────

        public Guid InvoiceId { get; set; }
        public Guid? FeeItemId { get; set; }
        public Guid? TermId { get; set; }

        // ─── Item Details ────────────────────────────────────────────────────────────

        [Required]
        [MaxLength(200)]
        public string Description { get; set; } = null!;

        [MaxLength(50)]
        public string? ItemType { get; set; } // Maps to FeeType display name

        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; } = 0.0m;

        // ─── Tax ────────────────────────────────────────────────────────────────────

        public bool IsTaxable { get; set; } = false;

        /// <summary>Tax rate as a percentage, e.g. 16 for 16%.</summary>
        public decimal? TaxRate { get; set; }

        // ─── Computed Financials (persisted) ─────────────────────────────────────────
        // These use private setters so EF Core can map them.
        // Always call Compute() before saving this entity.

        public decimal Total { get; private set; }         // Quantity * UnitPrice
        public decimal TaxAmount { get; private set; }     // Tax applied after discount
        public decimal NetAmount { get; private set; }     // Final payable amount

        // ─── GL / Notes ──────────────────────────────────────────────────────────────

        [MaxLength(100)]
        public string? GlCode { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // ─── Navigation ──────────────────────────────────────────────────────────────

        public Invoice Invoice { get; set; } = null!;
        public FeeItem? FeeItem { get; set; }
        public Term? Term { get; set; }

        // ─── Domain Methods ──────────────────────────────────────────────────────────

        /// <summary>
        /// Calculates Total, TaxAmount, and NetAmount.
        /// Must be called before persisting or adding to invoice.
        /// Optionally pass a discount override (e.g. from StudentDiscount).
        /// </summary>
        public void Compute(decimal? discountOverride = null)
        {
            if (discountOverride.HasValue)
                Discount = discountOverride.Value;

            Total = Quantity * UnitPrice;
            var discountedTotal = Total - Discount;

            TaxAmount = IsTaxable && TaxRate.HasValue
                ? discountedTotal * (TaxRate.Value / 100)
                : 0;

            NetAmount = discountedTotal + TaxAmount;
        }

        // ─── Computed (not stored) ────────────────────────────────────────────────────

        [NotMapped]
        public decimal EffectiveUnitPrice => Quantity > 0 ? NetAmount / Quantity : 0;
    }
}