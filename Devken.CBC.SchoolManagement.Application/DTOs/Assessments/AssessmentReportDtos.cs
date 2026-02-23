using System;


namespace Devken.CBC.SchoolManagement.Application.DTOs.Assessments
{
    /// <summary>
    /// Lightweight projection used exclusively by the Assessments List PDF report.
    /// </summary>
    public class AssessmentReportDto
    {
        // ── Shared columns ──────────────────────────────────────────────────
        public string Title { get; set; } = null!;
        public AssessmentTypeDto AssessmentType { get; set; }
        public string AssessmentTypeLabel => AssessmentType.ToString();

        public string TeacherName { get; set; } = null!;
        public string SubjectName { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public string TermName { get; set; } = null!;
        public DateTime AssessmentDate { get; set; }
        public decimal MaximumScore { get; set; }
        public bool IsPublished { get; set; }
        public int ScoreCount { get; set; }

        // ── SuperAdmin cross-school column ──────────────────────────────────
        public Guid SchoolId { get; set; }
        public string? SchoolName { get; set; }
    }
}