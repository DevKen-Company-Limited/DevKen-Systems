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
        // ── Search ────────────────────────────────────────────────────────────

        /// <summary>
        /// Free-text search across FirstName, LastName, Email, PhoneNumber.
        /// Maps to: query.SearchTerm in ParentService.GetAllAsync()
        /// </summary>
        public string? SearchTerm { get; set; }

        // ── Enum filter ───────────────────────────────────────────────────────

        /// <summary>
        /// Filter by relationship type. Nullable — omit to return all relationships.
        /// Maps to: query.Relationship in ParentService.GetAllAsync()
        /// </summary>
        public ParentRelationship? Relationship { get; set; }

        // ── Boolean filters ───────────────────────────────────────────────────

        /// <summary>
        /// true = primary contacts only | false = non-primary | null = all
        /// Maps to: query.IsPrimaryContact in ParentService.GetAllAsync()
        /// </summary>
        public bool? IsPrimaryContact { get; set; }

        /// <summary>
        /// true = emergency contacts only | false = non-emergency | null = all
        /// Maps to: query.IsEmergencyContact in ParentService.GetAllAsync()
        /// </summary>
        public bool? IsEmergencyContact { get; set; }

        /// <summary>
        /// true = has portal access | false = no portal access | null = all
        /// Maps to: query.HasPortalAccess in ParentService.GetAllAsync()
        /// </summary>
        public bool? HasPortalAccess { get; set; }

        /// <summary>
        /// true = Active only | false = Inactive only | null = all except Deleted
        /// Maps to: query.IsActive in ParentService.GetAllAsync()
        /// </summary>
        public bool? IsActive { get; set; }

        // ── Pagination ────────────────────────────────────────────────────────

        /// <summary>Page number (1-based). Defaults to 1.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Records per page. Defaults to 20.</summary>
        public int PageSize { get; set; } = 20;
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