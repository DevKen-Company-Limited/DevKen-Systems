using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Devken.CBC.SchoolManagement.Domain.Enums;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Invoices
{
    // ─────────────────────────────────────────────────────────────
    // REQUEST DTOs
    // ─────────────────────────────────────────────────────────────

    public class CreateInvoiceDto
    {
        [Required]
        public Guid StudentId { get; set; }

        [Required]
        public Guid AcademicYearId { get; set; }

        public Guid? TermId { get; set; }

        public Guid? ParentId { get; set; }

        [Required]
        public DateTime InvoiceDate { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // TenantId required only when called by SuperAdmin
        public Guid? TenantId { get; set; }

        [Required, MinLength(1, ErrorMessage = "At least one invoice item is required.")]
        public List<CreateInvoiceItemDto> Items { get; set; } = new();
    }

    public class CreateInvoiceItemDto
    {
        [Required, MaxLength(200)]
        public string Description { get; set; } = null!;

        [MaxLength(50)]
        public string? ItemType { get; set; }

        public Guid? FeeItemId { get; set; }
        public Guid? TermId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; } = 1;

        [Range(0, double.MaxValue, ErrorMessage = "Unit price cannot be negative.")]
        public decimal UnitPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Discount cannot be negative.")]
        public decimal Discount { get; set; } = 0m;

        public bool IsTaxable { get; set; } = false;

        [Range(0, 100, ErrorMessage = "Tax rate must be between 0 and 100.")]
        public decimal? TaxRate { get; set; }

        [MaxLength(100)]
        public string? GlCode { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    public class UpdateInvoiceDto
    {
        public Guid? ParentId { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    public class ApplyDiscountDto
    {
        [Required, Range(0.01, double.MaxValue, ErrorMessage = "Discount must be positive.")]
        public decimal DiscountAmount { get; set; }
    }

    // ─────────────────────────────────────────────────────────────
    // RESPONSE DTOs
    // ─────────────────────────────────────────────────────────────

    public class InvoiceResponseDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string InvoiceNumber { get; set; } = null!;

        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = null!;

        public Guid AcademicYearId { get; set; }
        public string AcademicYearName { get; set; } = null!;

        public Guid? TermId { get; set; }
        public string? TermName { get; set; }

        public Guid? ParentId { get; set; }
        public string? ParentName { get; set; }

        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }

        public string? Description { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Balance { get; set; }

        public InvoiceStatus StatusInvoice { get; set; }
        public string StatusDisplay => StatusInvoice.ToString();
        public bool IsOverdue { get; set; }

        public string? Notes { get; set; }
        public string Status { get; set; } = null!;

        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }

        public List<InvoiceItemResponseDto> Items { get; set; } = new();
    }

    public class InvoiceItemResponseDto
    {
        public Guid Id { get; set; }
        public Guid? FeeItemId { get; set; }
        public string Description { get; set; } = null!;
        public string? ItemType { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public bool IsTaxable { get; set; }
        public decimal? TaxRate { get; set; }
        public decimal Total { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }
        public string? GlCode { get; set; }
        public string? Notes { get; set; }
    }

    public class InvoiceSummaryResponseDto
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; } = null!;
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = null!;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Balance { get; set; }
        public InvoiceStatus StatusInvoice { get; set; }
        public string StatusDisplay => StatusInvoice.ToString();
        public bool IsOverdue { get; set; }
        public string? TermName { get; set; }
        public string Status { get; set; } = null!;
    }

    // ─────────────────────────────────────────────────────────────
    // QUERY / FILTER DTO
    // ─────────────────────────────────────────────────────────────

    public class InvoiceQueryDto
    {
        public Guid? StudentId { get; set; }
        public Guid? ParentId { get; set; }
        public Guid? AcademicYearId { get; set; }
        public Guid? TermId { get; set; }
        public InvoiceStatus? InvoiceStatus { get; set; }
        public bool? IsOverdue { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public bool? IsActive { get; set; }
    }
}