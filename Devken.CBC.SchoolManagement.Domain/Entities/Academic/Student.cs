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
    /// Includes personal, academic, medical, and guardian information
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

        /// <summary>
        /// Unique admission number - auto-generated if not provided
        /// Format: ADM-YYYY-XXXXX (e.g., ADM-2026-00001)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string AdmissionNumber { get; set; } = null!;

        /// <summary>
        /// National Education Management Information System number
        /// </summary>
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
        public string? Nationality { get; set; } = "Kenyan";

        [MaxLength(50)]
        public string? County { get; set; }

        [MaxLength(50)]
        public string? SubCounty { get; set; }

        [MaxLength(500)]
        public string? HomeAddress { get; set; }

        [MaxLength(50)]
        public string? Religion { get; set; }

        #endregion

        #region Academic Information

        [Required]
        public DateTime DateOfAdmission { get; set; }

        /// <summary>
        /// Student's enrollment status (New, Continuing, Transferred, etc.)
        /// </summary>
        [Required]
        public StudentStatus StudentStatus { get; set; }

        /// <summary>
        /// Student's current CBC level (Grade 1-9, PP1, PP2, etc.)
        /// </summary>
        [Required]
        public CBCLevel CBCLevel { get; set; }

        /// <summary>
        /// Current level - mirrors CBCLevel for backward compatibility
        /// </summary>
        [Required]
        public CBCLevel CurrentLevel { get; set; }

        public Guid? CurrentClassId { get; set; }

        public Guid? CurrentAcademicYearId { get; set; }

        /// <summary>
        /// Overall student status (Active, Suspended, Graduated, etc.)
        /// </summary>
        [Required]
        public StudentStatus Status { get; set; } = StudentStatus.Active;

        [MaxLength(200)]
        public string? PreviousSchool { get; set; }

        public DateTime? DateOfLeaving { get; set; }

        [MaxLength(500)]
        public string? LeavingReason { get; set; }

        #endregion

        #region Medical & Health Information

        [MaxLength(10)]
        public string? BloodGroup { get; set; }

        public string? MedicalConditions { get; set; }

        public string? Allergies { get; set; }

        public string? SpecialNeeds { get; set; }

        public bool RequiresSpecialSupport { get; set; } = false;

        #endregion

        #region Guardian Information

        // ═══════════════════════════════════════════════════════════════
        // Primary Guardian (Required)
        // ═══════════════════════════════════════════════════════════════

        [Required]
        [MaxLength(200)]
        public string PrimaryGuardianName { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string PrimaryGuardianRelationship { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string PrimaryGuardianPhone { get; set; } = null!;

        [MaxLength(100)]
        public string? PrimaryGuardianEmail { get; set; }

        [MaxLength(100)]
        public string? PrimaryGuardianOccupation { get; set; }

        [MaxLength(500)]
        public string? PrimaryGuardianAddress { get; set; }

        // ═══════════════════════════════════════════════════════════════
        // Secondary Guardian (Optional)
        // ═══════════════════════════════════════════════════════════════

        [MaxLength(200)]
        public string? SecondaryGuardianName { get; set; }

        [MaxLength(50)]
        public string? SecondaryGuardianRelationship { get; set; }

        [MaxLength(20)]
        public string? SecondaryGuardianPhone { get; set; }

        [MaxLength(100)]
        public string? SecondaryGuardianEmail { get; set; }

        [MaxLength(100)]
        public string? SecondaryGuardianOccupation { get; set; }

        // ═══════════════════════════════════════════════════════════════
        // Emergency Contact (Optional)
        // ═══════════════════════════════════════════════════════════════

        [MaxLength(200)]
        public string? EmergencyContactName { get; set; }

        [MaxLength(20)]
        public string? EmergencyContactPhone { get; set; }

        [MaxLength(50)]
        public string? EmergencyContactRelationship { get; set; }

        #endregion

        #region Additional Information

        /// <summary>
        /// Photo URL - stored in /uploads/students/
        /// Managed by ImageUploadService
        /// </summary>
        [MaxLength(500)]
        public string? PhotoUrl { get; set; }

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

        /// <summary>
        /// Full name: FirstName MiddleName LastName
        /// </summary>
        public string FullName => $"{FirstName} {MiddleName} {LastName}".Replace("  ", " ").Trim();

        /// <summary>
        /// Calculates current age based on date of birth
        /// </summary>
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

        /// <summary>
        /// Display name with admission number
        /// Format: ADM-2026-00001 - John Doe
        /// </summary>
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
                CBCLevel = CBCLevel,
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
                PrimaryGuardianEmail = PrimaryGuardianEmail,
                PrimaryGuardianRelationship = PrimaryGuardianRelationship,
                SecondaryGuardian = SecondaryGuardianName,
                SecondaryGuardianPhone = SecondaryGuardianPhone,
                SecondaryGuardianEmail = SecondaryGuardianEmail,
                EmergencyContact = EmergencyContactName,
                EmergencyContactPhone = EmergencyContactPhone,
                EmergencyContactRelationship = EmergencyContactRelationship
            };
        }

        /// <summary>
        /// Validates if student is eligible for promotion to next level
        /// </summary>
        public bool IsEligibleForPromotion()
        {
            // Business logic for promotion eligibility
            // - Must be active
            // - No pending fees
            // - Meets academic requirements
            return IsActive && !HasPendingFees();
        }

        /// <summary>
        /// Gets student's medical summary for quick reference
        /// </summary>
        public MedicalSummary GetMedicalSummary()
        {
            return new MedicalSummary
            {
                StudentId = Id,
                StudentName = FullName,
                BloodGroup = BloodGroup,
                HasMedicalConditions = !string.IsNullOrWhiteSpace(MedicalConditions),
                HasAllergies = !string.IsNullOrWhiteSpace(Allergies),
                HasSpecialNeeds = !string.IsNullOrWhiteSpace(SpecialNeeds),
                RequiresSpecialSupport = RequiresSpecialSupport
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
        public CBCLevel CBCLevel { get; set; }
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
        public string PrimaryGuardian { get; set; } = null!;
        public string PrimaryGuardianPhone { get; set; } = null!;
        public string? PrimaryGuardianEmail { get; set; }
        public string PrimaryGuardianRelationship { get; set; } = null!;
        public string? SecondaryGuardian { get; set; }
        public string? SecondaryGuardianPhone { get; set; }
        public string? SecondaryGuardianEmail { get; set; }
        public string? EmergencyContact { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelationship { get; set; }
    }

    /// <summary>
    /// Represents student's medical summary for quick reference
    /// </summary>
    public class MedicalSummary
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = null!;
        public string? BloodGroup { get; set; }
        public bool HasMedicalConditions { get; set; }
        public bool HasAllergies { get; set; }
        public bool HasSpecialNeeds { get; set; }
        public bool RequiresSpecialSupport { get; set; }

        /// <summary>
        /// Returns true if student has any medical concerns
        /// </summary>
        public bool HasMedicalConcerns =>
            HasMedicalConditions || HasAllergies || HasSpecialNeeds || RequiresSpecialSupport;
    }

    #endregion
}