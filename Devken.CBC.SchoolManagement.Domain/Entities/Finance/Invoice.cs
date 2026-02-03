using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Finance
{
    public class Invoice : TenantBaseEntity<Guid>
    {
        [Required]
        [MaxLength(50)]
        public string InvoiceNumber { get; set; } = null!;

        public Guid StudentId { get; set; }

        public Guid AcademicYearId { get; set; }

        public Guid? TermId { get; set; }

        public DateTime InvoiceDate { get; set; }

        public DateTime DueDate { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal AmountPaid { get; set; } = 0.0m;

        public decimal Balance => TotalAmount - AmountPaid;

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;

        public bool IsOverdue => DateTime.Today > DueDate && Status == InvoiceStatus.Pending;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public Guid? ParentId { get; set; }

        // Navigation Properties
        public Student Student { get; set; } = null!;
        public AcademicYear AcademicYear { get; set; } = null!;
        public Term? Term { get; set; }
        public Parent? Parent { get; set; }
        public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}

public enum InvoiceStatus
{
    Draft = 1,
    Pending = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Overdue = 5,
    Cancelled = 6,
    Refunded = 7
}