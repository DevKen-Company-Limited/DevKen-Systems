using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Academics
{
    public class CreateGradeDto
    {
        [Required(ErrorMessage = "Student ID is required.")]
        public Guid StudentId { get; set; }

        [Required(ErrorMessage = "Subject ID is required.")]
        public Guid SubjectId { get; set; }

        public Guid? TermId { get; set; }

        public Guid? AssessmentId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Score must be non-negative.")]
        public decimal? Score { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Maximum score must be non-negative.")]
        public decimal? MaximumScore { get; set; }

        public GradeLetter? GradeLetter { get; set; }

        public AssessmentType? GradeType { get; set; }

        [Required(ErrorMessage = "Assessment date is required.")]
        public DateTime AssessmentDate { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public bool IsFinalized { get; set; } = false;

        /// <summary>Required only when called by SuperAdmin.</summary>
        public Guid? TenantId { get; set; }
    }

    public class UpdateGradeDto
    {
        [Range(0, double.MaxValue, ErrorMessage = "Score must be non-negative.")]
        public decimal? Score { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Maximum score must be non-negative.")]
        public decimal? MaximumScore { get; set; }

        public GradeLetter? GradeLetter { get; set; }

        public AssessmentType? GradeType { get; set; }

        [Required(ErrorMessage = "Assessment date is required.")]
        public DateTime AssessmentDate { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public bool IsFinalized { get; set; } = false;
    }

    public class GradeResponseDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string? StudentName { get; set; }
        public Guid SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public Guid? TermId { get; set; }
        public string? TermName { get; set; }
        public Guid? AssessmentId { get; set; }
        public decimal? Score { get; set; }
        public decimal? MaximumScore { get; set; }
        public decimal? Percentage { get; set; }
        public string? GradeLetter { get; set; }
        public string? GradeType { get; set; }
        public DateTime AssessmentDate { get; set; }
        public string? Remarks { get; set; }
        public bool IsFinalized { get; set; }
        public Guid TenantId { get; set; }
        public Guid SchoolId => TenantId;
        public string? SchoolName { get; set; }
        public string Status { get; set; } = default!;
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
    }
}