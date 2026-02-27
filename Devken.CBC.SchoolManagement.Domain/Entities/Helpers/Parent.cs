using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Helpers
{
    public class Parent : TenantBaseEntity<Guid>
    {
        // ───────────── Basic Information ─────────────

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = null!;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(20)]
        public string? AlternativePhoneNumber { get; set; }

        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        // ───────────── Identity Details ─────────────

        [MaxLength(20)]
        public string? NationalIdNumber { get; set; }

        [MaxLength(20)]
        public string? PassportNumber { get; set; }

        // ───────────── Employment Details ─────────────

        [MaxLength(100)]
        public string? Occupation { get; set; }

        [MaxLength(150)]
        public string? Employer { get; set; }

        [MaxLength(100)]
        public string? EmployerContact { get; set; }

        // ───────────── Relationship Info (ENUM) ─────────────

        [Required]
        public ParentRelationship Relationship { get; set; }

        public bool IsPrimaryContact { get; set; } = true;

        public bool IsEmergencyContact { get; set; } = true;

        // ───────────── Portal Access ─────────────

        public bool HasPortalAccess { get; set; } = false;

        [MaxLength(256)]
        public string? PortalUserId { get; set; }


        // ───────────── Navigation ─────────────

        public ICollection<Student> Students { get; set; } = new List<Student>();

        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}