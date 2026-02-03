using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Finance
{
    public class InvoiceItem : TenantBaseEntity<Guid>
    {
        public Guid InvoiceId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Description { get; set; } = null!;

        [MaxLength(50)]
        public string? ItemType { get; set; } // Tuition, Activity, Exam, Uniform, etc.

        public int Quantity { get; set; } = 1;

        public decimal UnitPrice { get; set; }

        public decimal Total => Quantity * UnitPrice;

        public decimal Discount { get; set; } = 0.0m;

        public decimal NetAmount => Total - Discount;

        public bool IsTaxable { get; set; } = false;

        public decimal? TaxRate { get; set; }

        public decimal? TaxAmount => IsTaxable && TaxRate.HasValue ? Total * (TaxRate.Value / 100) : 0;

        [MaxLength(100)]
        public string? GlCode { get; set; } // General Ledger Code

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Navigation Properties
        public Invoice Invoice { get; set; } = null!;

        // For term-based items
        public Guid? TermId { get; set; }
        public Term? Term { get; set; }

        // For specific fee items
        public Guid? FeeItemId { get; set; }
        public FeeItem? FeeItem { get; set; }
    }
}