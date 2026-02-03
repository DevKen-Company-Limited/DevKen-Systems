using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessment;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Devken.CBC.SchoolManagement.Domain.Entities.Reports;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Domain.Enums.Students;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using System.Security.Claims;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Academic
{
    /// <summary>
    /// Represents a student in the CBC school system
    /// </summary>
    public class Student : TenantBaseEntity<Guid>
    {
        #region Personal Information

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string AdmissionNumber { get; set; } = null!;

        [MaxLength(50)]
        public string? NemisNumber { get; set; }

        [MaxLength(50)]
        public string? BirthCertificateNumber { get; set; }

        public DateTime DateOfBirth { get; set; }

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

        #endregion

        #region Academic Information

        public DateTime DateOfAdmission { get; set; }

        public CBCLevel CurrentLevel { get; set; }

        public Guid CurrentClassId { get; set; }

        public Guid? CurrentAcademicYearId { get; set; }

        public StudentStatus Status { get; set; } = StudentStatus.Active;

        [MaxLength(500)]
        public string? PreviousSchool { get; set; }

        public DateTime? DateOfLeaving { get; set; }

        [MaxLength(500)]
        public string? LeavingReason { get; set; }

        #endregion

        #region Health Information

        [MaxLength(100)]
        public string? BloodGroup { get; set; }

        [MaxLength(1000)]
        public string? MedicalConditions { get; set; }

        [MaxLength(1000)]
        public string? Allergies { get; set; }

        [MaxLength(500)]
        public string? SpecialNeeds { get; set; }

        public bool RequiresSpecialSupport { get; set; } = false;

        #endregion

        #region Parent/Guardian Information

        // Primary Guardian
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

        // Secondary Guardian
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

        // Emergency Contact
        [MaxLength(200)]
        public string? EmergencyContactName { get; set; }

        [MaxLength(20)]
        public string? EmergencyContactPhone { get; set; }

        [MaxLength(50)]
        public string? EmergencyContactRelationship { get; set; }

        #endregion

        #region Additional Information

        [MaxLength(500)]
        public string? PhotoUrl { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;

        #endregion

        #region Navigation Properties

        public School? School { get; set; }

        public Class? CurrentClass { get; set; }

        public AcademicYear? CurrentAcademicYear { get; set; }

        public ICollection<Grade> Grades { get; set; } = new List<Grade>();

        public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();

        public ICollection<FormativeAssessment> FormativeAssessments { get; set; } = new List<FormativeAssessment>();

        public ICollection<SummativeAssessment> SummativeAssessments { get; set; } = new List<SummativeAssessment>();

        public ICollection<CompetencyAssessment> CompetencyAssessments { get; set; } = new List<CompetencyAssessment>();

        public ICollection<ProgressReport> ProgressReports { get; set; } = new List<ProgressReport>();

        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();

        #endregion

        #region Computed Properties

        public string FullName => $"{FirstName} {MiddleName} {LastName}".Replace("  ", " ").Trim();

        public int Age
        {
            get
            {
                var today = DateTime.Today;
                var age = today.Year - DateOfBirth.Year;
                if (DateOfBirth.Date > today.AddYears(-age)) age--;
                return age;
            }
        }

        public string DisplayName => $"{AdmissionNumber} - {FullName}";

        #endregion
    }
}