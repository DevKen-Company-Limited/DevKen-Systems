using System;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Library
{
    // ── Read DTO ──────────────────────────────────────────────────────────────

    public class BookCopyDto
    {
        public Guid Id { get; set; }
        public Guid SchoolId { get; set; }
        public Guid BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string BookISBN { get; set; } = string.Empty;
        public Guid LibraryBranchId { get; set; }
        public string LibraryBranchName { get; set; } = string.Empty;
        public string AccessionNumber { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string? QRCode { get; set; }
        public string Condition { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public bool IsLost { get; set; }
        public bool IsDamaged { get; set; }
        public DateTime? AcquiredOn { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    // ── Create DTO ────────────────────────────────────────────────────────────

    public class CreateBookCopyRequest
    {
        public Guid? SchoolId { get; set; }

        [Required]
        public Guid BookId { get; set; }

        [Required]
        public Guid LibraryBranchId { get; set; }

        /// <summary>Optional — auto-generated if not provided.</summary>
        [MaxLength(50)]
        public string? AccessionNumber { get; set; }

        /// <summary>Optional — auto-generated if not provided.</summary>
        [MaxLength(50)]
        public string? Barcode { get; set; }

        [MaxLength(100)]
        public string? QRCode { get; set; }

        [Required]
        public string Condition { get; set; } = "Good";

        public bool IsAvailable { get; set; } = true;
        public bool IsLost { get; set; } = false;
        public bool IsDamaged { get; set; } = false;

        public DateTime? AcquiredOn { get; set; }
    }

    // ── Update DTO ────────────────────────────────────────────────────────────

    public class UpdateBookCopyRequest
    {
        [Required]
        public Guid LibraryBranchId { get; set; }

        [Required, MaxLength(50)]
        public string AccessionNumber { get; set; } = null!;

        [Required, MaxLength(50)]
        public string Barcode { get; set; } = null!;

        [MaxLength(100)]
        public string? QRCode { get; set; }

        [Required]
        public string Condition { get; set; } = "Good";

        public bool IsAvailable { get; set; }
        public bool IsLost { get; set; }
        public bool IsDamaged { get; set; }

        public DateTime? AcquiredOn { get; set; }
    }

    // ── Mark Lost/Damaged Request ─────────────────────────────────────────────

    public class MarkBookCopyStatusRequest
    {
        [MaxLength(500)]
        public string? Remarks { get; set; }
    }
}