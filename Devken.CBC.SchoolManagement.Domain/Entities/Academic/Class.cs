using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Domain.Enums.Students;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Academic
{
    public class Class : TenantBaseEntity<Guid>
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(10)]
        public string Code { get; set; } = null!;

        public CBCLevel Level { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public int Capacity { get; set; } = 40;

        public int CurrentEnrollment { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public Guid? TeacherId { get; set; }

        // Navigation Properties
        public Teacher? ClassTeacher { get; set; }
        public AcademicYear? AcademicYear { get; set; }
        public ICollection<Student> Students { get; set; } = new List<Student>();
        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();

        // Computed Properties
        public int AvailableSeats => Capacity - CurrentEnrollment;
        public bool IsFull => CurrentEnrollment >= Capacity;
    }
}