using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Finance
{
    public class FeeItem : TenantBaseEntity<Guid>
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(20)]
        public string Code { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        public decimal DefaultAmount { get; set; }

        [MaxLength(50)]
        public string FeeType { get; set; } = null!; // Tuition, Activity, Exam, Uniform, Other

        public bool IsMandatory { get; set; } = true;

        public bool IsRecurring { get; set; } = false; // Monthly, Termly, Yearly

        [MaxLength(20)]
        public string? Recurrence { get; set; } // Monthly, Termly, Yearly

        public bool IsTaxable { get; set; } = false;

        public decimal? TaxRate { get; set; }

        [MaxLength(100)]
        public string? GlCode { get; set; }

        public bool IsActive { get; set; } = true;

        // For CBC specific fees
        public CBCLevel? ApplicableLevel { get; set; } // Null for all levels

        [MaxLength(100)]
        public string? ApplicableTo { get; set; } // "All", "Boarding", "Day", "Special"

        // Navigation Properties
        public ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();

        // Computed Properties
        public string DisplayName => $"{Code} - {Name}";
    }
}