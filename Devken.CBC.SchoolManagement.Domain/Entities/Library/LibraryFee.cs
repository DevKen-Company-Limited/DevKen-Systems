// Domain/Entities/Library/LibraryFee.cs
using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Library
{
    public class LibraryFee : TenantBaseEntity<Guid>
    {
        public Guid MemberId { get; set; }

        /// <summary>Optional — links this fee to a specific borrow transaction.</summary>
        public Guid? BookBorrowId { get; set; }

        public LibraryFeeType FeeType { get; set; }

        public decimal Amount { get; set; }

        /// <summary>Amount already collected (supports partial payments).</summary>
        public decimal AmountPaid { get; set; } = 0;

        public LibraryFeeStatus FeeStatus { get; set; } = LibraryFeeStatus.Unpaid;

        public string Description { get; set; } = string.Empty;

        public DateTime FeeDate { get; set; }

        public DateTime? PaidOn { get; set; }

        /// <summary>Free-text reason if fee was waived by an admin.</summary>
        public string? WaivedReason { get; set; }

        // ── Navigation Properties ─────────────────────────────────────────────
        public LibraryMember Member { get; set; } = null!;
        public BookBorrow? BookBorrow { get; set; }
        public Guid TenantId { get; set; }
        [ForeignKey("TenantId")] // Explicitly link TenantId to School
        public virtual School? School { get; set; }


    }
}