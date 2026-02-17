using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Academics
{
    // ── Read DTO ──────────────────────────────────────────────────────────────
    public class TermDto
    {
        public Guid Id { get; set; }
        public Guid SchoolId { get; set; }
        public string SchoolName { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public int TermNumber { get; set; }
        public Guid AcademicYearId { get; set; }
        public string AcademicYearName { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsClosed { get; set; }
        public bool IsActive { get; set; }

        public string Notes { get; set; } = string.Empty;

        // Computed properties
        public int DurationDays { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    // ── Create DTO ────────────────────────────────────────────────────────────
    public class CreateTermRequest
    {
        /// <summary>Required for SuperAdmin only; ignored for school-scoped users.</summary>
        public Guid SchoolId { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = null!;

        [Required]
        [Range(1, 3, ErrorMessage = "Term number must be between 1 and 3")]
        public int TermNumber { get; set; }

        [Required]
        public Guid AcademicYearId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public bool IsCurrent { get; set; } = false;

        public bool IsClosed { get; set; } = false;

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    // ── Update DTO ────────────────────────────────────────────────────────────
    public class UpdateTermRequest
    {
        [Required, MaxLength(50)]
        public string Name { get; set; } = null!;

        [Required]
        [Range(1, 3, ErrorMessage = "Term number must be between 1 and 3")]
        public int TermNumber { get; set; }

        [Required]
        public Guid AcademicYearId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public bool IsCurrent { get; set; }

        public bool IsClosed { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    // ── Set Current Term Request ──────────────────────────────────────────────
    public class SetCurrentTermRequest
    {
        [Required]
        public Guid TermId { get; set; }
    }

    // ── Close Term Request ────────────────────────────────────────────────────
    public class CloseTermRequest
    {
        [Required]
        public Guid TermId { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }
    }
}
