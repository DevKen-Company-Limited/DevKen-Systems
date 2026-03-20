using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Library
{
    // ── Read DTOs ─────────────────────────────────────────────────────────────
    // NOTE: BookCopyDto is defined in BookCopyDtos.cs — do NOT redefine here.

    public class BookDto
    {
        public Guid Id { get; set; }
        public Guid SchoolId { get; set; }
        public string SchoolName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public Guid AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public Guid PublisherId { get; set; }
        public string PublisherName { get; set; } = string.Empty;
        public int PublicationYear { get; set; }
        public string? Language { get; set; }
        public string? Description { get; set; }
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public ICollection<BookCopyDto> Copies { get; set; } = new List<BookCopyDto>();
    }

    // ── Create DTO ────────────────────────────────────────────────────────────

    public class CreateBookRequest
    {
        /// <summary>Required for SuperAdmin only; ignored for school-scoped users.</summary>
        public Guid? SchoolId { get; set; }

        [Required, MaxLength(255)]
        public string Title { get; set; } = null!;

        [Required, MaxLength(20)]
        public string ISBN { get; set; } = null!;

        [Required]
        public Guid CategoryId { get; set; }

        [Required]
        public Guid AuthorId { get; set; }

        [Required]
        public Guid PublisherId { get; set; }

        [Required]
        [Range(1000, 9999, ErrorMessage = "Publication year must be a valid 4-digit year.")]
        public int PublicationYear { get; set; }

        [MaxLength(50)]
        public string? Language { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }
    }

    // ── Update DTO ────────────────────────────────────────────────────────────

    public class UpdateBookRequest
    {
        [Required, MaxLength(255)]
        public string Title { get; set; } = null!;

        [Required, MaxLength(20)]
        public string ISBN { get; set; } = null!;

        [Required]
        public Guid CategoryId { get; set; }

        [Required]
        public Guid AuthorId { get; set; }

        [Required]
        public Guid PublisherId { get; set; }

        [Required]
        [Range(1000, 9999, ErrorMessage = "Publication year must be a valid 4-digit year.")]
        public int PublicationYear { get; set; }

        [MaxLength(50)]
        public string? Language { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }
    }
}