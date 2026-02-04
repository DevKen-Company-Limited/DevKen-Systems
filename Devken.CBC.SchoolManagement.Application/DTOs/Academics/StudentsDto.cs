using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Academics
{
    /// <summary>
    /// DTO for creating a new student
    /// </summary>
    public class CreateStudentRequest
    {
        // Personal Information
        [Required(ErrorMessage = "First name is required")]
        [MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [MaxLength(100)]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "Admission number is required")]
        [MaxLength(50)]
        public string AdmissionNumber { get; set; } = null!;

        [MaxLength(50)]
        public string? NemisNumber { get; set; }

        [MaxLength(50)]
        public string? BirthCertificateNumber { get; set; }

        [Required(ErrorMessage = "Date of birth is required")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public Gender Gender { get; set; }

        [MaxLength(500)]
        public string? PlaceOfBirth { get; set; }

        [MaxLength(100)]
        public string? Nationality { get; set; } = "Kenyan";

        [MaxLength(100)]
        public string? County { get; set; }

        [MaxLength(100)]
        public string? SubCounty { get; set; }

        [MaxLength(500)]
        public string? HomeAddress { get; set; }

        [MaxLength(50)]
        public string? Religion { get; set; }

        // Academic Information
        [Required(ErrorMessage = "Date of admission is required")]
        public DateTime DateOfAdmission { get; set; }

        [Required(ErrorMessage = "Current level is required")]
        public CBCLevel CurrentLevel { get; set; }

        [Required(ErrorMessage = "Current class is required")]
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

        public bool RequiresSpecialSupport { get; set; } = false;

        // Primary Guardian
        [MaxLength(200)]
        public string? PrimaryGuardianName { get; set; }

        [MaxLength(50)]
        public string? PrimaryGuardianRelationship { get; set; }

        [MaxLength(20)]
        public string? PrimaryGuardianPhone { get; set; }

        [MaxLength(100)]
        [EmailAddress]
        public string? PrimaryGuardianEmail { get; set; }

        [MaxLength(200)]
        public string? PrimaryGuardianOccupation { get; set; }

        [MaxLength(500)]
        public string? PrimaryGuardianAddress { get; set; }

        // Secondary Guardian
        [MaxLength(200)]
        public string? SecondaryGuardianName { get; set; }

        [MaxLength(50)]
        public string? SecondaryGuardianRelationship { get; set; }

        [MaxLength(20)]
        public string? SecondaryGuardianPhone { get; set; }

        [MaxLength(100)]
        [EmailAddress]
        public string? SecondaryGuardianEmail { get; set; }

        [MaxLength(200)]
        public string? SecondaryGuardianOccupation { get; set; }

        // Emergency Contact
        [MaxLength(200)]
        public string? EmergencyContactName { get; set; }

        [MaxLength(20)]
        public string? EmergencyContactPhone { get; set; }

        [MaxLength(50)]
        public string? EmergencyContactRelationship { get; set; }

        // Additional
        [MaxLength(500)]
        public string? PhotoUrl { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO for updating student information
    /// </summary>
    public class UpdateStudentRequest
    {
        [Required]
        public Guid Id { get; set; }

        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        [MaxLength(50)]
        public string? NemisNumber { get; set; }

        [MaxLength(50)]
        public string? BirthCertificateNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public Gender? Gender { get; set; }

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

        public CBCLevel? CurrentLevel { get; set; }

        public Guid? CurrentClassId { get; set; }

        public Guid? CurrentAcademicYearId { get; set; }

        public StudentStatus? StudentStatus { get; set; }

        [MaxLength(500)]
        public string? PreviousSchool { get; set; }

        [MaxLength(100)]
        public string? BloodGroup { get; set; }

        [MaxLength(1000)]
        public string? MedicalConditions { get; set; }

        [MaxLength(1000)]
        public string? Allergies { get; set; }

        [MaxLength(500)]
        public string? SpecialNeeds { get; set; }

        public bool? RequiresSpecialSupport { get; set; }

        [MaxLength(200)]
        public string? PrimaryGuardianName { get; set; }

        [MaxLength(50)]
        public string? PrimaryGuardianRelationship { get; set; }

        [MaxLength(20)]
        public string? PrimaryGuardianPhone { get; set; }

        [MaxLength(100)]
        [EmailAddress]
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
        [EmailAddress]
        public string? SecondaryGuardianEmail { get; set; }

        [MaxLength(200)]
        public string? SecondaryGuardianOccupation { get; set; }

        [MaxLength(200)]
        public string? EmergencyContactName { get; set; }

        [MaxLength(20)]
        public string? EmergencyContactPhone { get; set; }

        [MaxLength(50)]
        public string? EmergencyContactRelationship { get; set; }

        [MaxLength(500)]
        public string? PhotoUrl { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// DTO for student response
    /// </summary>
    public class StudentResponse
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string AdmissionNumber { get; set; } = null!;
        public string? NemisNumber { get; set; }
        public string? BirthCertificateNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; }
        public Gender Gender { get; set; }
        public string? PlaceOfBirth { get; set; }
        public string? Nationality { get; set; }
        public string? County { get; set; }
        public string? SubCounty { get; set; }
        public string? HomeAddress { get; set; }
        public string? Religion { get; set; }
        public DateTime DateOfAdmission { get; set; }
        public CBCLevel CurrentLevel { get; set; }
        public string CurrentLevelName { get; set; } = null!;
        public Guid CurrentClassId { get; set; }
        public string? CurrentClassName { get; set; }
        public Guid? CurrentAcademicYearId { get; set; }
        public StudentStatus StudentStatus { get; set; }
        public string? PreviousSchool { get; set; }
        public DateTime? DateOfLeaving { get; set; }
        public string? LeavingReason { get; set; }
        public string? BloodGroup { get; set; }
        public string? MedicalConditions { get; set; }
        public string? Allergies { get; set; }
        public string? SpecialNeeds { get; set; }
        public bool RequiresSpecialSupport { get; set; }
        public string? PrimaryGuardianName { get; set; }
        public string? PrimaryGuardianRelationship { get; set; }
        public string? PrimaryGuardianPhone { get; set; }
        public string? PrimaryGuardianEmail { get; set; }
        public string? PrimaryGuardianOccupation { get; set; }
        public string? SecondaryGuardianName { get; set; }
        public string? SecondaryGuardianPhone { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
    }

    /// <summary>
    /// DTO for student list item (lightweight)
    /// </summary>
    public class StudentListItemResponse
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = null!;
        public string AdmissionNumber { get; set; } = null!;
        public Gender Gender { get; set; }
        public int Age { get; set; }
        public CBCLevel CurrentLevel { get; set; }
        public string CurrentLevelName { get; set; } = null!;
        public string? CurrentClassName { get; set; }
        public StudentStatus StudentStatus { get; set; }
        public bool IsActive { get; set; }
        public string? PhotoUrl { get; set; }
    }

    /// <summary>
    /// DTO for paginated student list
    /// </summary>
    public class StudentPagedResponse
    {
        public List<StudentListItemResponse> Students { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    /// <summary>
    /// DTO for student search request
    /// </summary>
    public class StudentSearchRequest
    {
        public string? SearchTerm { get; set; }
        public CBCLevel? Level { get; set; }
        public Guid? ClassId { get; set; }
        public StudentStatus? Status { get; set; }
        public Gender? Gender { get; set; }
        public bool IncludeInactive { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// DTO for transfer student to another class
    /// </summary>
    public class TransferStudentRequest
    {
        [Required]
        public Guid StudentId { get; set; }

        [Required]
        public Guid NewClassId { get; set; }

        public CBCLevel? NewLevel { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        public DateTime? EffectiveDate { get; set; }
    }

    /// <summary>
    /// DTO for withdrawing a student
    /// </summary>
    public class WithdrawStudentRequest
    {
        [Required]
        public Guid StudentId { get; set; }

        [Required]
        public DateTime DateOfLeaving { get; set; }

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = null!;

        public StudentStatus NewStatus { get; set; } = StudentStatus.Withdrawn;
    }

    /// <summary>
    /// DTO for student statistics
    /// </summary>
    public class StudentStatisticsResponse
    {
        public int TotalStudents { get; set; }
        public int ActiveStudents { get; set; }
        public int InactiveStudents { get; set; }
        public int MaleStudents { get; set; }
        public int FemaleStudents { get; set; }
        public int StudentsWithSpecialNeeds { get; set; }
        public Dictionary<string, int> StudentsByLevel { get; set; } = new();
        public Dictionary<string, int> StudentsByClass { get; set; } = new();
        public Dictionary<string, int> StudentsByStatus { get; set; } = new();
    }
}
