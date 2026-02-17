using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Academic
{
    /// <summary>
    /// Student Data Transfer Object
    /// </summary>
    public class StudentDto
    {
        public Guid Id { get; set; }
        public Guid SchoolId { get; set; }
        public string SchoolName { get; set; } = string.Empty;

        // Personal Information
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public string NemisNumber { get; set; } = string.Empty;
        public string BirthCertificateNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string PlaceOfBirth { get; set; } = string.Empty;
        public string Nationality { get; set; } = "Kenyan";
        public string County { get; set; } = string.Empty;
        public string SubCounty { get; set; } = string.Empty;
        public string HomeAddress { get; set; } = string.Empty;
        public string Religion { get; set; } = string.Empty;

        // Academic Details
        public DateTime DateOfAdmission { get; set; }
        public string StudentStatus { get; set; } = string.Empty;
        public string CBCLevel { get; set; } = string.Empty;
        public string CurrentLevel { get; set; } = string.Empty;
        public Guid? CurrentClassId { get; set; }
        public string CurrentClassName { get; set; } = string.Empty;
        public Guid? CurrentAcademicYearId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PreviousSchool { get; set; } = string.Empty;

        // Medical Information
        public string BloodGroup { get; set; } = string.Empty;
        public string MedicalConditions { get; set; } = string.Empty;
        public string Allergies { get; set; } = string.Empty;
        public string SpecialNeeds { get; set; } = string.Empty;
        public bool RequiresSpecialSupport { get; set; }

        // Guardian Information
        public string PrimaryGuardianName { get; set; } = string.Empty;
        public string PrimaryGuardianRelationship { get; set; } = string.Empty;
        public string PrimaryGuardianPhone { get; set; } = string.Empty;
        public string PrimaryGuardianEmail { get; set; } = string.Empty;
        public string PrimaryGuardianOccupation { get; set; } = string.Empty;
        public string PrimaryGuardianAddress { get; set; } = string.Empty;

        public string SecondaryGuardianName { get; set; } = string.Empty;
        public string SecondaryGuardianRelationship { get; set; } = string.Empty;
        public string SecondaryGuardianPhone { get; set; } = string.Empty;
        public string SecondaryGuardianEmail { get; set; } = string.Empty;
        public string SecondaryGuardianOccupation { get; set; } = string.Empty;

        public string EmergencyContactName { get; set; } = string.Empty;
        public string EmergencyContactPhone { get; set; } = string.Empty;
        public string EmergencyContactRelationship { get; set; } = string.Empty;

        // Additional
        public string PhotoUrl { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? AcademicYearName { get; set; }
    }

    /// <summary>
    /// Create Student Request
    /// </summary>
    public class CreateStudentRequest
    {
        [Required]
        public Guid SchoolId { get; set; }

        // Personal Information
        [Required, MinLength(2), MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MinLength(2), MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        /// <summary>
        /// Optional - will be auto-generated if not provided
        /// </summary>
        [MaxLength(50)]
        public string? AdmissionNumber { get; set; }

        [MaxLength(50)]
        public string? NemisNumber { get; set; }

        [MaxLength(50)]
        public string? BirthCertificateNumber { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public Gender Gender { get; set; }

        [MaxLength(100)]
        public string? PlaceOfBirth { get; set; }

        [MaxLength(50)]
        public string? Nationality { get; set; }

        [MaxLength(50)]
        public string? County { get; set; }

        [MaxLength(50)]
        public string? SubCounty { get; set; }

        [MaxLength(500)]
        public string? HomeAddress { get; set; }

        [MaxLength(50)]
        public string? Religion { get; set; }

        // Academic Details
        public DateTime? DateOfAdmission { get; set; }

        [Required]
        public StudentStatus StudentStatus { get; set; }

        [Required]
        public CBCLevel CBCLevel { get; set; }

        [Required]
        public CBCLevel CurrentLevel { get; set; }

        public Guid? CurrentClassId { get; set; }
        public Guid? CurrentAcademicYearId { get; set; }

        [MaxLength(200)]
        public string? PreviousSchool { get; set; }

        // Medical Information
        [MaxLength(10)]
        public string? BloodGroup { get; set; }

        public string? MedicalConditions { get; set; }
        public string? Allergies { get; set; }
        public string? SpecialNeeds { get; set; }
        public bool RequiresSpecialSupport { get; set; }

        // Guardian Information
        [Required, MaxLength(200)]
        public string PrimaryGuardianName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string PrimaryGuardianRelationship { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string PrimaryGuardianPhone { get; set; } = string.Empty;

        [MaxLength(100), EmailAddress]
        public string? PrimaryGuardianEmail { get; set; }

        [MaxLength(100)]
        public string? PrimaryGuardianOccupation { get; set; }

        [MaxLength(500)]
        public string? PrimaryGuardianAddress { get; set; }

        [MaxLength(200)]
        public string? SecondaryGuardianName { get; set; }

        [MaxLength(50)]
        public string? SecondaryGuardianRelationship { get; set; }

        [MaxLength(20)]
        public string? SecondaryGuardianPhone { get; set; }

        [MaxLength(100), EmailAddress]
        public string? SecondaryGuardianEmail { get; set; }

        [MaxLength(100)]
        public string? SecondaryGuardianOccupation { get; set; }

        [MaxLength(200)]
        public string? EmergencyContactName { get; set; }

        [MaxLength(20)]
        public string? EmergencyContactPhone { get; set; }

        [MaxLength(50)]
        public string? EmergencyContactRelationship { get; set; }

        // Additional
        [MaxLength(500)]
        public string? PhotoUrl { get; set; }

        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Update Student Request
    /// </summary>
    public class UpdateStudentRequest
    {
        // Personal Information
        [Required, MinLength(2), MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MinLength(2), MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? MiddleName { get; set; }
        public DateTime? DateOfAdmission { get; set; }

        // Note: AdmissionNumber is immutable after creation

        [MaxLength(50)]
        public string? NemisNumber { get; set; }

        [MaxLength(50)]
        public string? BirthCertificateNumber { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public Gender Gender { get; set; }

        [MaxLength(100)]
        public string? PlaceOfBirth { get; set; }

        [MaxLength(50)]
        public string? Nationality { get; set; }

        [MaxLength(50)]
        public string? County { get; set; }

        [MaxLength(50)]
        public string? SubCounty { get; set; }

        [MaxLength(500)]
        public string? HomeAddress { get; set; }

        [MaxLength(50)]
        public string? Religion { get; set; }

        // Academic Details
        [Required]
        public StudentStatus StudentStatus { get; set; }

        [Required]
        public CBCLevel CBCLevel { get; set; }

        [Required]
        public CBCLevel CurrentLevel { get; set; }

        public Guid? CurrentClassId { get; set; }
        public Guid? CurrentAcademicYearId { get; set; }

        [MaxLength(200)]
        public string? PreviousSchool { get; set; }

        // Medical Information
        [MaxLength(10)]
        public string? BloodGroup { get; set; }

        public string? MedicalConditions { get; set; }
        public string? Allergies { get; set; }
        public string? SpecialNeeds { get; set; }
        public bool RequiresSpecialSupport { get; set; }

        // Guardian Information
        [Required, MaxLength(200)]
        public string PrimaryGuardianName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string PrimaryGuardianRelationship { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string PrimaryGuardianPhone { get; set; } = string.Empty;

        [MaxLength(100), EmailAddress]
        public string? PrimaryGuardianEmail { get; set; }

        [MaxLength(100)]
        public string? PrimaryGuardianOccupation { get; set; }

        [MaxLength(500)]
        public string? PrimaryGuardianAddress { get; set; }

        [MaxLength(200)]
        public string? SecondaryGuardianName { get; set; }

        [MaxLength(50)]
        public string? SecondaryGuardianRelationship { get; set; }

        [MaxLength(20)]
        public string? SecondaryGuardianPhone { get; set; }

        [MaxLength(100), EmailAddress]
        public string? SecondaryGuardianEmail { get; set; }

        [MaxLength(100)]
        public string? SecondaryGuardianOccupation { get; set; }

        [MaxLength(200)]
        public string? EmergencyContactName { get; set; }

        [MaxLength(20)]
        public string? EmergencyContactPhone { get; set; }

        [MaxLength(50)]
        public string? EmergencyContactRelationship { get; set; }

        // Additional
        [MaxLength(500)]
        public string? PhotoUrl { get; set; }

        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
    }
}