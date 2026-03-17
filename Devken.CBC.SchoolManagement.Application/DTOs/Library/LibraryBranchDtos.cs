using System;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Library
{
    // ── Read DTO ──────────────────────────────────────────────────────────────

    public class LibraryBranchDto
    {
        public Guid Id { get; set; }
        public Guid SchoolId { get; set; }
        public string SchoolName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
    }

    // ── Create DTO ────────────────────────────────────────────────────────────

    public class CreateLibraryBranchRequest
    {
        /// <summary>Required for SuperAdmin only; ignored for school-scoped users.</summary>
        public Guid? SchoolId { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = null!;

        [MaxLength(300)]
        public string? Location { get; set; }
    }

    // ── Update DTO ────────────────────────────────────────────────────────────

    public class UpdateLibraryBranchRequest
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = null!;

        [MaxLength(300)]
        public string? Location { get; set; }
    }
}