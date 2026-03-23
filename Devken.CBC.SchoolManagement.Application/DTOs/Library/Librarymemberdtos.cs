using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Library
{
    // ── Read DTO ──────────────────────────────────────────────────────────────
    public class LibraryMemberDto
    {
        public Guid Id { get; set; }
        public Guid SchoolId { get; set; }
        public Guid UserId { get; set; }

        /// <summary>Full name resolved from the linked User record.</summary>
        public string UserFullName { get; set; } = string.Empty;

        public string UserEmail { get; set; } = string.Empty;
        public string MemberNumber { get; set; } = string.Empty;
        public LibraryMemberType MemberType { get; set; }
        public DateTime JoinedOn { get; set; }
        public bool IsActive { get; set; }
        public int TotalBorrows { get; set; }
    }

    // ── Create DTO ────────────────────────────────────────────────────────────
    public class CreateLibraryMemberRequest
    {
        /// <summary>Required for SuperAdmin only; ignored for school-scoped users.</summary>
        public Guid? SchoolId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [MaxLength(50)]
        public string? MemberNumber { get; set; } = null!;

        [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))]  // ← add this
        public LibraryMemberType MemberType { get; set; }

        public DateTime? JoinedOn { get; set; }
    }

    // ── Update DTO ────────────────────────────────────────────────────────────
    public class UpdateLibraryMemberRequest
    {
        [Required, MaxLength(50)]
        public string MemberNumber { get; set; } = null!;

        [Required]
        public LibraryMemberType MemberType { get; set; }

        public bool IsActive { get; set; }
    }
}