using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Domain.Entities.Reports;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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
        public StudentStatus StudentStatus { get; set; }

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

        // School & Basic Relationships
        public School? School { get; set; }

        // Parent Relationship
        public Guid? ParentId { get; set; }
        public Parent? Parent { get; set; }

        // Academic Relationships
        public Class? CurrentClass { get; set; }
        public AcademicYear? CurrentAcademicYear { get; set; }

        // Assessment Relationships
        public ICollection<Grade> Grades { get; set; } = new List<Grade>();

        // Assessment Score Relationships (Student-specific results)
        public ICollection<FormativeAssessmentScore> FormativeAssessmentScores { get; set; } = new List<FormativeAssessmentScore>();
        public ICollection<SummativeAssessmentScore> SummativeAssessmentScores { get; set; } = new List<SummativeAssessmentScore>();
        public ICollection<CompetencyAssessmentScore> CompetencyAssessmentScores { get; set; } = new List<CompetencyAssessmentScore>();

        // Report Relationships
        public ICollection<ProgressReport> ProgressReports { get; set; } = new List<ProgressReport>();

        // Finance Relationships
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

        #region Helper Methods

        /// <summary>
        /// Gets the student's current academic performance summary
        /// </summary>
        public AcademicPerformance GetAcademicPerformance()
        {
            return new AcademicPerformance
            {
                StudentId = Id,
                StudentName = FullName,
                AdmissionNumber = AdmissionNumber,
                CurrentLevel = CurrentLevel,
                ClassName = CurrentClass?.Name ?? "Not Assigned"
            };
        }

        /// <summary>
        /// Checks if student has any pending fees
        /// </summary>
        public bool HasPendingFees()
        {
            // This would typically query the database
            // For now, return a placeholder logic
            return false;
        }

        /// <summary>
        /// Gets student's guardian information for emergency contacts
        /// </summary>
        public GuardianInfo GetGuardianInfo()
        {
            return new GuardianInfo
            {
                PrimaryGuardian = PrimaryGuardianName,
                PrimaryGuardianPhone = PrimaryGuardianPhone,
                PrimaryGuardianRelationship = PrimaryGuardianRelationship,
                SecondaryGuardian = SecondaryGuardianName,
                SecondaryGuardianPhone = SecondaryGuardianPhone,
                EmergencyContact = EmergencyContactName,
                EmergencyContactPhone = EmergencyContactPhone
            };
        }

        #endregion
    }

    #region Helper Classes

    /// <summary>
    /// Represents student's academic performance summary
    /// </summary>
    public class AcademicPerformance
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = null!;
        public string AdmissionNumber { get; set; } = null!;
        public CBCLevel CurrentLevel { get; set; }
        public string ClassName { get; set; } = null!;
        public decimal? AverageScore { get; set; }
        public string? OverallGrade { get; set; }
        public int? ClassRank { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents student's guardian/contact information
    /// </summary>
    public class GuardianInfo
    {
        public string? PrimaryGuardian { get; set; }
        public string? PrimaryGuardianPhone { get; set; }
        public string? PrimaryGuardianRelationship { get; set; }
        public string? SecondaryGuardian { get; set; }
        public string? SecondaryGuardianPhone { get; set; }
        public string? EmergencyContact { get; set; }
        public string? EmergencyContactPhone { get; set; }
    }

    #endregion
}