using System;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Library
{
    // ── Read DTO ──────────────────────────────────────────────────────────────

    public class BookInventoryDto
    {
        public Guid Id { get; set; }
        public Guid SchoolId { get; set; }
        public Guid BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string BookISBN { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public int BorrowedCopies { get; set; }
        public int LostCopies { get; set; }
        public int DamagedCopies { get; set; }
        public double AvailabilityPercentage { get; set; }
    }

    // ── Create DTO ────────────────────────────────────────────────────────────

    public class CreateBookInventoryRequest
    {
        /// <summary>Required for SuperAdmin only.</summary>
        public Guid? SchoolId { get; set; }

        [Required]
        public Guid BookId { get; set; }

        [Range(0, int.MaxValue)]
        public int TotalCopies { get; set; } = 0;

        [Range(0, int.MaxValue)]
        public int AvailableCopies { get; set; } = 0;

        [Range(0, int.MaxValue)]
        public int BorrowedCopies { get; set; } = 0;

        [Range(0, int.MaxValue)]
        public int LostCopies { get; set; } = 0;

        [Range(0, int.MaxValue)]
        public int DamagedCopies { get; set; } = 0;
    }

    // ── Update DTO ────────────────────────────────────────────────────────────

    public class UpdateBookInventoryRequest
    {
        [Range(0, int.MaxValue)]
        public int TotalCopies { get; set; }

        [Range(0, int.MaxValue)]
        public int AvailableCopies { get; set; }

        [Range(0, int.MaxValue)]
        public int BorrowedCopies { get; set; }

        [Range(0, int.MaxValue)]
        public int LostCopies { get; set; }

        [Range(0, int.MaxValue)]
        public int DamagedCopies { get; set; }
    }

    // ── Recalculate Request ───────────────────────────────────────────────────

    public class RecalculateInventoryRequest
    {
        /// <summary>
        /// If true, rebuilds inventory counts from actual BookCopy records.
        /// </summary>
        public bool ForceRecalculate { get; set; } = true;
    }
}