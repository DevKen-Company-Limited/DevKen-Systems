using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Library
{

 
    public class LibraryFineDto
    {
        public Guid Id { get; set; }
        public Guid BorrowItemId { get; set; }
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; }
        public bool IsWaived => IsPaid && Reason?.Contains("[WAIVED:") == true;
        public DateTime IssuedOn { get; set; }
        public DateTime? PaidOn { get; set; }
        public string? Reason { get; set; }

        // Multi-tenant fields
        public Guid? SchoolId { get; set; }
        public string? SchoolName { get; set; }
        public Guid? TenantId { get; set; }

        // Denormalized fields from related entities
        public string? MemberName { get; set; }
        public string? MemberNumber { get; set; }
        public string? BookTitle { get; set; }
        public string? ISBN { get; set; }
    }

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

        // SuperAdmin only - if provided, validates access
        public Guid? SchoolId { get; set; }
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