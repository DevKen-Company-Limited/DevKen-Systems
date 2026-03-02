using System;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Finance
{
    // ──────────────────────────────────────────────────────────────────────────
    // CREATE
    // ──────────────────────────────────────────────────────────────────────────

    public class CreateInvoiceItemDto
    {
        [Required]
        public Guid InvoiceId { get; set; }

        public Guid? FeeItemId { get; set; }

        public Guid? TermId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Description { get; set; } = null!;

        [MaxLength(50)]
        public string? ItemType { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; } = 1;

        [Range(0, double.MaxValue, ErrorMessage = "Unit price must be non-negative.")]
        public decimal UnitPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Discount must be non-negative.")]
        public decimal Discount { get; set; } = 0;

        public bool IsTaxable { get; set; } = false;

        [Range(0, 100, ErrorMessage = "Tax rate must be between 0 and 100.")]
        public decimal? TaxRate { get; set; }

        [MaxLength(100)]
        public string? GlCode { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Optional discount override (e.g. student-level discount).
        /// If provided, overrides the Discount field during Compute().
        /// </summary>
        public decimal? DiscountOverride { get; set; }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UPDATE
    // ──────────────────────────────────────────────────────────────────────────

    public class UpdateInvoiceItemDto
    {
        [Required]
        [MaxLength(200)]
        public string Description { get; set; } = null!;

        [MaxLength(50)]
        public string? ItemType { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; } = 1;

        [Range(0, double.MaxValue, ErrorMessage = "Unit price must be non-negative.")]
        public decimal UnitPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Discount must be non-negative.")]
        public decimal Discount { get; set; } = 0;

        public bool IsTaxable { get; set; } = false;

        [Range(0, 100, ErrorMessage = "Tax rate must be between 0 and 100.")]
        public decimal? TaxRate { get; set; }

        [MaxLength(100)]
        public string? GlCode { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Optional discount override; replaces the Discount field during Compute().
        /// </summary>
        public decimal? DiscountOverride { get; set; }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // RESPONSE
    // ──────────────────────────────────────────────────────────────────────────

    public class InvoiceItemResponseDto
    {
        public Guid Id { get; set; }
        public Guid InvoiceId { get; set; }
        public Guid? FeeItemId { get; set; }
        public Guid? TermId { get; set; }
        public string Description { get; set; } = null!;
        public string? ItemType { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public bool IsTaxable { get; set; }
        public decimal? TaxRate { get; set; }

        // Computed financials
        public decimal Total { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal EffectiveUnitPrice { get; set; }

        public string? GlCode { get; set; }
        public string? Notes { get; set; }

        // Audit
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
    }
}