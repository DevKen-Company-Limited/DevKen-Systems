using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        // Computed property ignored by EF Core
        [NotMapped]
        public decimal Balance => TotalAmount - AmountPaid;

        public InvoiceStatus StatusInvoice { get; set; } = InvoiceStatus.Pending;

        [NotMapped]
        public bool IsOverdue => DateTime.Today > DueDate && StatusInvoice == InvoiceStatus.Pending;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public Guid? ParentId { get; set; }

        // Navigation properties
        public Student Student { get; set; } = null!;
        public AcademicYear AcademicYear { get; set; } = null!;
        public Term? Term { get; set; }
        public Parent? Parent { get; set; }
        public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }

    public enum InvoiceStatus
    {
        Draft = 0,           // ✅ Changed from 1 to 0 (CLR default)
        Pending = 1,         // ✅ Changed from 2 to 1
        PartiallyPaid = 2,   // ✅ Changed from 3 to 2
        Paid = 3,            // ✅ Changed from 4 to 3
        Overdue = 4,         // ✅ Changed from 5 to 4
        Cancelled = 5,       // ✅ Changed from 6 to 5
        Refunded = 6         // ✅ Changed from 7 to 6
    }
}
