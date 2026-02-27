using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Parents
{
    // ─────────────────────────────────────────────────────────────
    // REQUEST DTOs
    // ─────────────────────────────────────────────────────────────

    public class CreateParentDto
    {
        // Basic Information
        [Required(ErrorMessage = "First name is required.")]
        [MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [MaxLength(100)]
        public string LastName { get; set; } = null!;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(20)]
        public string? AlternativePhoneNumber { get; set; }

        [MaxLength(150)]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        // Identity
        [MaxLength(20)]
        public string? NationalIdNumber { get; set; }

        [MaxLength(20)]
        public string? PassportNumber { get; set; }

        // Employment
        [MaxLength(100)]
        public string? Occupation { get; set; }

        [MaxLength(150)]
        public string? Employer { get; set; }

        [MaxLength(100)]
        public string? EmployerContact { get; set; }

        // Relationship
        [Required(ErrorMessage = "Relationship is required.")]
        public ParentRelationship Relationship { get; set; }

        public bool IsPrimaryContact { get; set; } = true;
        public bool IsEmergencyContact { get; set; } = true;

        // Portal Access
        public bool HasPortalAccess { get; set; } = false;

        [MaxLength(256)]
        public string? PortalUserId { get; set; }

        /// <summary>Required only when called by SuperAdmin.</summary>
        public Guid? TenantId { get; set; }
    }

    public class UpdateParentDto
    {
        // Basic Information
        [Required(ErrorMessage = "First name is required.")]
        [MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [MaxLength(100)]
        public string LastName { get; set; } = null!;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(20)]
        public string? AlternativePhoneNumber { get; set; }

        [MaxLength(150)]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        // Identity
        [MaxLength(20)]
        public string? NationalIdNumber { get; set; }

        [MaxLength(20)]
        public string? PassportNumber { get; set; }

        // Employment
        [MaxLength(100)]
        public string? Occupation { get; set; }

        [MaxLength(150)]
        public string? Employer { get; set; }

        [MaxLength(100)]
        public string? EmployerContact { get; set; }

        // Relationship
        [Required(ErrorMessage = "Relationship is required.")]
        public ParentRelationship Relationship { get; set; }

        public bool IsPrimaryContact { get; set; }
        public bool IsEmergencyContact { get; set; }

        // Portal Access
        public bool HasPortalAccess { get; set; }

        [MaxLength(256)]
        public string? PortalUserId { get; set; }
    }

    // ─────────────────────────────────────────────────────────────
    // RESPONSE DTOs
    // ─────────────────────────────────────────────────────────────

    public class ParentDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        // Basic Information
        public string FirstName { get; set; } = null!;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = null!;

        // Computed from the three name parts — no entity change needed
        public string FullName => string.Join(" ",
            new[] { FirstName, MiddleName, LastName }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        public string? PhoneNumber { get; set; }
        public string? AlternativePhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }

        // Identity
        public string? NationalIdNumber { get; set; }
        public string? PassportNumber { get; set; }

        // Employment
        public string? Occupation { get; set; }
        public string? Employer { get; set; }
        public string? EmployerContact { get; set; }

        // Relationship
        public ParentRelationship Relationship { get; set; }

        /// <summary>
        /// Human-readable label for the frontend — matches the SubjectResponseDto
        /// pattern of serialising enums as strings.
        /// </summary>
        public string RelationshipDisplay => Relationship.ToString();

        public bool IsPrimaryContact { get; set; }
        public bool IsEmergencyContact { get; set; }

        // Portal Access
        public bool HasPortalAccess { get; set; }
        public string? PortalUserId { get; set; }

        // Metadata — serialised as string, same pattern as SubjectResponseDto.Status
        public string Status { get; set; } = null!;
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }

        // Linked students count — populated from Students nav property
        public int StudentCount { get; set; }
    }

    public class ParentSummaryDto
    {
        public Guid Id { get; set; }

        // FullName cannot be expression-bodied here because the summary
        // mapper only sets this one property (not the three name parts).
        public string FullName { get; set; } = null!;

        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }

        public ParentRelationship Relationship { get; set; }

        /// <summary>Consistent with ParentDto.RelationshipDisplay.</summary>
        public string RelationshipDisplay => Relationship.ToString();

        public bool IsPrimaryContact { get; set; }
        public bool IsEmergencyContact { get; set; }
        public bool HasPortalAccess { get; set; }
        public int StudentCount { get; set; }

        // Serialised as string — consistent with ParentDto and SubjectResponseDto
        public string Status { get; set; } = null!;
    }

    // ─────────────────────────────────────────────────────────────
    // QUERY / FILTER DTO
    // ─────────────────────────────────────────────────────────────

    public class ParentQueryDto
    {
        /// <summary>Searches across FirstName, LastName, Email and PhoneNumber.</summary>
        public string? SearchTerm { get; set; }

        public ParentRelationship? Relationship { get; set; }
        public bool? IsPrimaryContact { get; set; }
        public bool? IsEmergencyContact { get; set; }
        public bool? HasPortalAccess { get; set; }

        /// <summary>
        /// Filter by active/inactive state. Null returns all non-deleted records.
        /// Uses bool rather than EntityStatus so the frontend doesn't need to
        /// know about internal enum values.
        /// </summary>
        public bool? IsActive { get; set; }
    }

    // ─────────────────────────────────────────────────────────────
    // PAGINATED RESPONSE
    // ─────────────────────────────────────────────────────────────

    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => PageNumber < TotalPages;
        public bool HasPreviousPage => PageNumber > 1;
    }
}