using System;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Library
{
    public class CreateLibraryFineDto
    {
        [Required]
        public Guid BorrowItemId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;

        public DateTime? IssuedOn { get; set; }
    }

    public class PayFineDto
    {
        [Required]
        public Guid FineId { get; set; }

        public DateTime? PaymentDate { get; set; }
    }

    public class PayMultipleFinesDto
    {
        [Required]
        [MinLength(1)]
        public List<Guid> FineIds { get; set; } = new List<Guid>();

        public DateTime? PaymentDate { get; set; }
    }

    public class WaiveFineDto
    {
        [Required]
        public Guid FineId { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;
    }
}