using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Domain.Entities.Reports;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Domain.Enums.Students;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Academic
{
    public class Teacher : TenantBaseEntity<Guid>
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
        public string TeacherNumber { get; set; } = null!;

        public DateTime? DateOfBirth { get; set; }

        public Gender Gender { get; set; }

        [MaxLength(50)]
        public string? TscNumber { get; set; } // Teachers Service Commission Number

        [MaxLength(100)]
        public string? Nationality { get; set; } = "Kenyan";

        [MaxLength(100)]
        public string? IdNumber { get; set; }

        [MaxLength(100)]
        public string? PhoneNumber { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        #endregion

        #region Professional Information

        [MaxLength(100)]
        public string? Qualification { get; set; }

        [MaxLength(100)]
        public string? Specialization { get; set; }

        public DateTime? DateOfEmployment { get; set; }

        [MaxLength(50)]
        public string? EmploymentType { get; set; } // Permanent, Contract, Probation

        [MaxLength(50)]
        public string? Designation { get; set; } // Head Teacher, Deputy, Senior Teacher, etc.

        [MaxLength(200)]
        public string? SubjectsTaught { get; set; } // Comma-separated subject names

        [MaxLength(100)]
        public string? CBCLevelsHandled { get; set; } // Comma-separated CBC levels

        public bool IsClassTeacher { get; set; } = false;

        public Guid? CurrentClassId { get; set; }

        #endregion

        #region Additional Information

        [MaxLength(500)]
        public string? PhotoUrl { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(2000)]
        public string? Notes { get; set; }

        #endregion

        #region Navigation Properties

        public Class? CurrentClass { get; set; }
        public ICollection<Class> Classes { get; set; } = new List<Class>();
        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
        public ICollection<Assessment> AssessmentsCreated { get; set; } = new List<Assessment>();
        public ICollection<ProgressReport> ProgressReportsReviewed { get; set; } = new List<ProgressReport>();

        #endregion

        #region Computed Properties

        public string FullName => $"{FirstName} {MiddleName} {LastName}".Replace("  ", " ").Trim();

        public string DisplayName => $"{TeacherNumber} - {FullName}";

        public int? Age
        {
            get
            {
                if (!DateOfBirth.HasValue) return null;
                var today = DateTime.Today;
                var age = today.Year - DateOfBirth.Value.Year;
                if (DateOfBirth.Value.Date > today.AddYears(-age)) age--;
                return age;
            }
        }

        #endregion
    }
}