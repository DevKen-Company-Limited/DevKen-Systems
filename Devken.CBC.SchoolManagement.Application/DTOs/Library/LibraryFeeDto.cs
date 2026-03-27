using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Library
{
    // ── Read DTO ──────────────────────────────────────────────────────────────
    public class LibraryFeeDto
    {
        public Guid Id { get; set; }
        public Guid SchoolId { get; set; }
        public string SchoolName { get; set; } = string.Empty;
        public Guid MemberId { get; set; }
        public string MemberNumber { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty; 
        public Guid? BookBorrowId { get; set; }
        public LibraryFeeType FeeType { get; set; }
        public string FeeTypeDisplay { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Balance { get; set; }
        public LibraryFeeStatus FeeStatus { get; set; }
        public string FeeStatusDisplay { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime FeeDate { get; set; }
        public DateTime? PaidOn { get; set; }
        public string? WaivedReason { get; set; }
    }

    // ── Create DTO ────────────────────────────────────────────────────────────
    public class CreateLibraryFeeRequest
    {
        /// <summary>Required for SuperAdmin only.</summary>
        public Guid? SchoolId { get; set; }

        [Required]
        public Guid MemberId { get; set; }

        public Guid? BookBorrowId { get; set; }

        [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LibraryFeeType FeeType { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public DateTime? FeeDate { get; set; }
    }

    // ── Update DTO ────────────────────────────────────────────────────────────
    public class UpdateLibraryFeeRequest
    {
        [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LibraryFeeType FeeType { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public DateTime? FeeDate { get; set; }
    }

    // ── Payment DTO ───────────────────────────────────────────────────────────
    public class RecordLibraryFeePaymentRequest
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than zero.")]
        public decimal AmountPaid { get; set; }

        public DateTime? PaidOn { get; set; }
    }

    // ── Waive DTO ─────────────────────────────────────────────────────────────
    public class WaiveLibraryFeeRequest
    {
        [Required, MaxLength(500)]
        public string Reason { get; set; } = null!;
    }

    // ── Query/Filter DTO ──────────────────────────────────────────────────────
    public class LibraryFeeFilterRequest
    {
        public Guid? SchoolId { get; set; }
        public Guid? MemberId { get; set; }
        public LibraryFeeStatus? FeeStatus { get; set; }
        public LibraryFeeType? FeeType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}