using System;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Academics
{
    public class CreateAcademicYearRequest
    {
        public Guid SchoolId { get; set; }

        [Required]
        [MaxLength(50)]
        public string? Name { get; set; }

        // Code is now optional - will be auto-generated if not provided
        [MaxLength(20)]
        public string? Code { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public bool IsCurrent { get; set; } = false;

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    public class UpdateAcademicYearRequest
    {
        [MaxLength(50)]
        public string? Name { get; set; }

        [MaxLength(20)]
        public string? Code { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool? IsCurrent { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    public class AcademicYearDto
    {
        public Guid Id { get; set; }
        public Guid SchoolId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsClosed { get; set; }
        public bool IsActive { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public Guid? UpdatedBy { get; set; }
    }
}