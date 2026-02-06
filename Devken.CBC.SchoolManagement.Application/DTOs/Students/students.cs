// File: Application/DTOs/Academic/StudentDto.cs
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Academic
{
    public class StudentDto
    {
        public Guid Id { get; set; }
        public Guid SchoolId { get; set; }

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
        public string Nationality { get; set; } = string.Empty;
        public string County { get; set; } = string.Empty;
        public string SubCounty { get; set; } = string.Empty;
        public string HomeAddress { get; set; } = string.Empty;
        public string Religion { get; set; } = string.Empty;

        // Academic Information
        public DateTime DateOfAdmission { get; set; }
        public string CurrentLevel { get; set; } = string.Empty;
        public Guid CurrentClassId { get; set; }
        public string CurrentClassName { get; set; } = string.Empty;
        public Guid? CurrentAcademicYearId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PreviousSchool { get; set; } = string.Empty;

        // Health Information
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

        // Additional Information
        public string PhotoUrl { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CreateStudentRequest
    {
        [Required]
        public Guid SchoolId { get; set; }

        // Personal Information
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [Required]
        [MaxLength(50)]
        public string AdmissionNumber { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? NemisNumber { get; set; }

        [MaxLength(50)]
        public string? BirthCertificateNumber { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public Gender Gender { get; set; }

        [MaxLength(500)]
        public string? PlaceOfBirth { get; set; }

        [MaxLength(100)]
        public string? Nationality { get; set; }

        [MaxLength(100)]
        public string? County { get; set; }

        [MaxLength(100)]
        public string? SubCounty { get; set; }

        [MaxLength(500)]
        public string? HomeAddress { get; set; }

        [MaxLength(50)]
        public string? Religion { get; set; }

        // Academic Information
        public DateTime? DateOfAdmission { get; set; }

        [Required]
        public CBCLevel CurrentLevel { get; set; }

        [Required]
        public Guid CurrentClassId { get; set; }

        public Guid? CurrentAcademicYearId { get; set; }

        [MaxLength(500)]
        public string? PreviousSchool { get; set; }

        // Health Information
        [MaxLength(100)]
        public string? BloodGroup { get; set; }

        [MaxLength(1000)]
        public string? MedicalConditions { get; set; }

        [MaxLength(1000)]
        public string? Allergies { get; set; }

        [MaxLength(500)]
        public string? SpecialNeeds { get; set; }

        public bool RequiresSpecialSupport { get; set; }

        // Guardian Information
        [MaxLength(200)]
        public string? PrimaryGuardianName { get; set; }

        [MaxLength(50)]
        public string? PrimaryGuardianRelationship { get; set; }

        [MaxLength(20)]
        public string? PrimaryGuardianPhone { get; set; }

        [MaxLength(100)]
        public string? PrimaryGuardianEmail { get; set; }

        [MaxLength(200)]
        public string? PrimaryGuardianOccupation { get; set; }

        [MaxLength(500)]
        public string? PrimaryGuardianAddress { get; set; }

        [MaxLength(200)]
        public string? SecondaryGuardianName { get; set; }

        [MaxLength(50)]
        public string? SecondaryGuardianRelationship { get; set; }

        [MaxLength(20)]
        public string? SecondaryGuardianPhone { get; set; }

        [MaxLength(100)]
        public string? SecondaryGuardianEmail { get; set; }

        [MaxLength(200)]
        public string? SecondaryGuardianOccupation { get; set; }

        [MaxLength(200)]
        public string? EmergencyContactName { get; set; }

        [MaxLength(20)]
        public string? EmergencyContactPhone { get; set; }

        [MaxLength(50)]
        public string? EmergencyContactRelationship { get; set; }

        // Additional Information
        [MaxLength(500)]
        public string? PhotoUrl { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdateStudentRequest
    {
        // Personal Information
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [MaxLength(50)]
        public string? NemisNumber { get; set; }

        [MaxLength(50)]
        public string? BirthCertificateNumber { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public Gender Gender { get; set; }

        [MaxLength(500)]
        public string? PlaceOfBirth { get; set; }

        [MaxLength(100)]
        public string? Nationality { get; set; }

        [MaxLength(100)]
        public string? County { get; set; }

        [MaxLength(100)]
        public string? SubCounty { get; set; }

        [MaxLength(500)]
        public string? HomeAddress { get; set; }

        [MaxLength(50)]
        public string? Religion { get; set; }

        // Academic Information
        [Required]
        public CBCLevel CurrentLevel { get; set; }

        [Required]
        public Guid CurrentClassId { get; set; }

        public Guid? CurrentAcademicYearId { get; set; }

        [MaxLength(500)]
        public string? PreviousSchool { get; set; }

        // Health Information
        [MaxLength(100)]
        public string? BloodGroup { get; set; }

        [MaxLength(1000)]
        public string? MedicalConditions { get; set; }

        [MaxLength(1000)]
        public string? Allergies { get; set; }

        [MaxLength(500)]
        public string? SpecialNeeds { get; set; }

        public bool RequiresSpecialSupport { get; set; }

        // Guardian Information
        [MaxLength(200)]
        public string? PrimaryGuardianName { get; set; }

        [MaxLength(50)]
        public string? PrimaryGuardianRelationship { get; set; }

        [MaxLength(20)]
        public string? PrimaryGuardianPhone { get; set; }

        [MaxLength(100)]
        public string? PrimaryGuardianEmail { get; set; }

        [MaxLength(200)]
        public string? PrimaryGuardianOccupation { get; set; }

        [MaxLength(500)]
        public string? PrimaryGuardianAddress { get; set; }

        [MaxLength(200)]
        public string? SecondaryGuardianName { get; set; }

        [MaxLength(50)]
        public string? SecondaryGuardianRelationship { get; set; }

        [MaxLength(20)]
        public string? SecondaryGuardianPhone { get; set; }

        [MaxLength(100)]
        public string? SecondaryGuardianEmail { get; set; }

        [MaxLength(200)]
        public string? SecondaryGuardianOccupation { get; set; }

        [MaxLength(200)]
        public string? EmergencyContactName { get; set; }

        [MaxLength(20)]
        public string? EmergencyContactPhone { get; set; }

        [MaxLength(50)]
        public string? EmergencyContactRelationship { get; set; }

        // Additional Information
        [MaxLength(500)]
        public string? PhotoUrl { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; }
    }
}