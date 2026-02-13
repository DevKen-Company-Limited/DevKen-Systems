using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Academic
{
    // ── Read DTO ──────────────────────────────────────────────────────────────
    public class TeacherDto
    {
        public Guid Id { get; set; }
        public Guid SchoolId { get; set; }

        // Personal
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string TeacherNumber { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public int? Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string TscNumber { get; set; } = string.Empty;
        public string Nationality { get; set; } = "Kenyan";
        public string IdNumber { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        // Professional
        public string EmploymentType { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Qualification { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public DateTime? DateOfEmployment { get; set; }
        public bool IsClassTeacher { get; set; }
        public Guid? CurrentClassId { get; set; }
        public string CurrentClassName { get; set; } = string.Empty;

        // Extra
        public string PhotoUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string? SchoolName { get; set; }
    }

    // ── Create DTO ────────────────────────────────────────────────────────────
    public class CreateTeacherRequest
    {
        /// <summary>Required for SuperAdmin only; ignored for school-scoped users.</summary>
        public Guid SchoolId { get; set; }

        [Required, MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [Required, MaxLength(100)]
        public string LastName { get; set; } = null!;

        [MaxLength(50)]
        public string? TeacherNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }
        public Gender Gender { get; set; }

        [MaxLength(50)]
        public string? TscNumber { get; set; }

        [MaxLength(100)]
        public string? Nationality { get; set; }

        [MaxLength(100)]
        public string? IdNumber { get; set; }

        [MaxLength(100)]
        public string? PhoneNumber { get; set; }

        [EmailAddress, MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        public EmploymentType EmploymentType { get; set; } = EmploymentType.Permanent;
        public Designation Designation { get; set; } = Designation.Teacher;

        [MaxLength(100)]
        public string? Qualification { get; set; }

        [MaxLength(100)]
        public string? Specialization { get; set; }

        public DateTime? DateOfEmployment { get; set; }
        public bool IsClassTeacher { get; set; } = false;
        public Guid? CurrentClassId { get; set; }

        [MaxLength(500)]
        public string? PhotoUrl { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(2000)]
        public string? Notes { get; set; }
    }

    // ── Update DTO ────────────────────────────────────────────────────────────
    public class UpdateTeacherRequest
    {
        [Required, MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [Required, MaxLength(100)]
        public string LastName { get; set; } = null!;

        public DateTime? DateOfBirth { get; set; }
        public Gender Gender { get; set; }

        [MaxLength(50)]
        public string? TscNumber { get; set; }

        [MaxLength(100)]
        public string? Nationality { get; set; }

        [MaxLength(100)]
        public string? IdNumber { get; set; }

        [MaxLength(100)]
        public string? PhoneNumber { get; set; }

        [EmailAddress, MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        public EmploymentType EmploymentType { get; set; }
        public Designation Designation { get; set; }

        [MaxLength(100)]
        public string? Qualification { get; set; }

        [MaxLength(100)]
        public string? Specialization { get; set; }

        public DateTime? DateOfEmployment { get; set; }
        public bool IsClassTeacher { get; set; }
        public Guid? CurrentClassId { get; set; }

        [MaxLength(500)]
        public string? PhotoUrl { get; set; }

        public bool IsActive { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }
        public Guid SchoolId { get; set; }
    }
}