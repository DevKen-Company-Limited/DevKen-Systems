using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Domain.Enums;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Academics
{
    public class CreateClassRequest
    {
        public Guid SchoolId { get; set; }

        [Required]
        [MaxLength(100)]
        public string? Name { get; set; }

        [Required]
        [MaxLength(10)]
        public string? Code { get; set; }

        [Required]
        public CBCLevel Level { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Range(1, 100)]
        public int Capacity { get; set; } = 40;

        [Required]
        public Guid AcademicYearId { get; set; }

        public Guid? TeacherId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdateClassRequest
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(10)]
        public string? Code { get; set; }

        public CBCLevel? Level { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Range(1, 100)]
        public int? Capacity { get; set; }

        public Guid? AcademicYearId { get; set; }

        public Guid? TeacherId { get; set; }

        public bool? IsActive { get; set; }
    }

    public class ClassDto
    {
        public Guid Id { get; set; }
        public Guid SchoolId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public CBCLevel Level { get; set; }
        public string LevelName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Capacity { get; set; }
        public int CurrentEnrollment { get; set; }
        public int AvailableSeats { get; set; }
        public bool IsFull { get; set; }
        public bool IsActive { get; set; }
        public Guid AcademicYearId { get; set; }
        public string? AcademicYearName { get; set; }
        public string? AcademicYearCode { get; set; }
        public Guid? TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public DateTime CreatedOn { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public Guid? UpdatedBy { get; set; }
    }

    public class ClassDetailDto : ClassDto
    {
        public int StudentCount { get; set; }
        public int SubjectCount { get; set; }
    }
}
