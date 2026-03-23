using System;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Library
{
    // ── Read DTO ──────────────────────────────────────────────────────────────
    public class BookReservationDto
    {
        public Guid Id { get; set; }
        public Guid SchoolId { get; set; }
        public Guid BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public Guid MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public DateTime ReservedOn { get; set; }
        public bool IsFulfilled { get; set; }
    }

    // ── Create DTO ────────────────────────────────────────────────────────────
    public class CreateBookReservationRequest
    {
        /// <summary>Required for SuperAdmin only; ignored for school-scoped users.</summary>
        public Guid? SchoolId { get; set; }

        [Required]
        public Guid BookId { get; set; }

        [Required]
        public Guid MemberId { get; set; }
    }

    // ── Update DTO ────────────────────────────────────────────────────────────
    public class UpdateBookReservationRequest
    {
        [Required]
        public Guid BookId { get; set; }

        [Required]
        public Guid MemberId { get; set; }

        public bool IsFulfilled { get; set; }
    }

    // ── Fulfill DTO ───────────────────────────────────────────────────────────
    public class FulfillBookReservationRequest
    {
        // Intentionally empty — fulfilling only flips IsFulfilled = true.
        // Extend here if you need to capture fulfillment notes, copy ID, etc.
    }
}