using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Domain.Enums;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Academics
{
    public class CreateSubjectDto
    {
        [Required(ErrorMessage = "Subject name is required.")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = default!;

        /// <summary>
        /// Leave empty — auto-generated via DocumentNumberSeries.
        /// </summary>
        public string? Code { get; set; }

        [Required(ErrorMessage = "CBC Level is required.")]
        public CBCLevel Level { get; set; }

        [Required(ErrorMessage = "Subject type is required.")]
        public SubjectType SubjectType { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>Required only when called by SuperAdmin.</summary>
        public Guid? TenantId { get; set; }
    }

    public class UpdateSubjectDto
    {
        [Required(ErrorMessage = "Subject name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = default!;

        [Required]
        public CBCLevel Level { get; set; }

        [Required]
        public SubjectType SubjectType { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class SubjectResponseDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public string Level { get; set; } = default!;
        public string SubjectType { get; set; } = default!;
        public bool IsActive { get; set; }
        public Guid TenantId { get; set; }
        public string Status { get; set; } = default!;
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
    }
    public class SubjectReportDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string SubjectType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string SchoolName { get; set; } = string.Empty;
        public Guid SchoolId { get; set; }
    }
}
