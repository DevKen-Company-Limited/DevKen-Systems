using Devken.CBC.SchoolManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Finance
{
    public class CreateFeeItemDto
    {
        [Required(ErrorMessage = "Fee item name is required.")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = default!;

        /// <summary>
        /// Leave empty — auto-generated via DocumentNumberSeries.
        /// </summary>
        public string? Code { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Default amount is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Amount must be non-negative.")]
        public decimal DefaultAmount { get; set; }

        [Required(ErrorMessage = "Fee type is required.")]
        public FeeType FeeType { get; set; }

        public bool IsMandatory { get; set; } = true;
        public bool IsRecurring { get; set; } = false;

        public RecurrenceType? Recurrence { get; set; }

        public bool IsTaxable { get; set; } = false;

        [Range(0, 100, ErrorMessage = "Tax rate must be between 0 and 100.")]
        public decimal? TaxRate { get; set; }

        [MaxLength(100)]
        public string? GlCode { get; set; }

        public bool IsActive { get; set; } = true;

        public CBCLevel? ApplicableLevel { get; set; }

        public ApplicableTo? ApplicableTo { get; set; }

        /// <summary>Required only when called by SuperAdmin.</summary>
        public Guid? TenantId { get; set; }
    }

    public class UpdateFeeItemDto
    {
        [Required(ErrorMessage = "Fee item name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = default!;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Default amount is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Amount must be non-negative.")]
        public decimal DefaultAmount { get; set; }

        [Required(ErrorMessage = "Fee type is required.")]
        public FeeType FeeType { get; set; }

        public bool IsMandatory { get; set; } = true;
        public bool IsRecurring { get; set; } = false;

        public RecurrenceType? Recurrence { get; set; }

        public bool IsTaxable { get; set; } = false;

        [Range(0, 100)]
        public decimal? TaxRate { get; set; }

        [MaxLength(100)]
        public string? GlCode { get; set; }

        public bool IsActive { get; set; } = true;

        public CBCLevel? ApplicableLevel { get; set; }

        public ApplicableTo? ApplicableTo { get; set; }
    }

    public class FeeItemResponseDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public decimal DefaultAmount { get; set; }
        public string FeeType { get; set; } = default!;       // numeric string e.g. "0"
        public bool IsMandatory { get; set; }
        public bool IsRecurring { get; set; }
        public string? Recurrence { get; set; }               // numeric string e.g. "2"
        public bool IsTaxable { get; set; }
        public decimal? TaxRate { get; set; }
        public string? GlCode { get; set; }
        public bool IsActive { get; set; }
        public string? ApplicableLevel { get; set; }          // numeric string e.g. "3"
        public string? ApplicableTo { get; set; }             // numeric string e.g. "1"
        public string DisplayName { get; set; } = default!;
        public Guid TenantId { get; set; }
        public Guid SchoolId => TenantId;
        public string? SchoolName { get; set; }
        public string Status { get; set; } = default!;
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
    }
}