using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Domain.Entities.Reports;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Academic
{
    public class Teacher : TenantBaseEntity<Guid>
    {
        #region Personal Information
        [Required, MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [Required, MaxLength(100)]
        public string LastName { get; set; } = null!;

        [Required, MaxLength(50)]
        public string TeacherNumber { get; set; } = null!;

        public DateTime? DateOfBirth { get; set; }
        public Gender Gender { get; set; }

        [MaxLength(50)]
        public string? TscNumber { get; set; }

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
        public EmploymentType EmploymentType { get; set; } = EmploymentType.Permanent;
        public Designation Designation { get; set; } = Designation.Teacher;

        [MaxLength(100)]
        public string? Qualification { get; set; }

        [MaxLength(100)]
        public string? Specialization { get; set; }

        public DateTime? DateOfEmployment { get; set; }
        public bool IsClassTeacher { get; set; } = false;
        public Guid? CurrentClassId { get; set; }
        #endregion

        #region Navigation Properties
        public Class? CurrentClass { get; set; }
        public ICollection<Class> Classes { get; set; } = new HashSet<Class>();
        public ICollection<Subject> Subjects { get; set; } = new HashSet<Subject>();

        /// <summary>
        /// CBC levels handled by the teacher
        /// </summary>
        public ICollection<TeacherCBCLevel> CBCLevels { get; set; } = new HashSet<TeacherCBCLevel>();

        public ICollection<Assessment1> AssessmentsCreated { get; set; } = new HashSet<Assessment1>();
        public ICollection<ProgressReport> ProgressReportsReviewed { get; set; } = new HashSet<ProgressReport>();
        #endregion

        #region Additional Information
        [MaxLength(500)]
        public string? PhotoUrl { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(2000)]
        public string? Notes { get; set; }
        #endregion

        #region Computed Properties
        public string FullName =>
            string.Join(" ", new[] { FirstName, MiddleName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

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
