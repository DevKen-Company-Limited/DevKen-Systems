using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Academic
{
    public class AcademicYear : TenantBaseEntity<Guid>
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = null!;

        [Required]
        [MaxLength(9)]
        public string Code { get; set; } = null!; // Format: "2024-2025"

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool IsCurrent { get; set; } = false;

        public bool IsClosed { get; set; } = false;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Navigation Properties
        public ICollection<Class> Classes { get; set; } = new List<Class>();
        public ICollection<Student> Students { get; set; } = new List<Student>();
        public ICollection<Assessment1> Assessments { get; set; } = new List<Assessment1>();
        public ICollection<Term> Terms { get; set; } = new List<Term>();

        // Computed Properties
        public bool IsActive => !IsClosed && DateTime.Today >= StartDate && DateTime.Today <= EndDate;
    }
}