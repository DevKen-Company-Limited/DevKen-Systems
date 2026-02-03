using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Reports
{
    public class ProgressReportComment : TenantBaseEntity<Guid>
    {
        public Guid ProgressReportId { get; set; }

        public Guid CommentedById { get; set; }

        [Required]
        [MaxLength(50)]
        public string CommentType { get; set; } = null!; // ClassTeacher, HeadTeacher, Parent, Student

        [Required]
        [MaxLength(2000)]
        public string Comment { get; set; } = null!;

        public DateTime CommentDate { get; set; }

        public bool IsInternal { get; set; } = false; // Not shown to parents

        [MaxLength(500)]
        public string? ActionRequired { get; set; }

        public DateTime? ActionDueDate { get; set; }

        public bool ActionCompleted { get; set; } = false;

        public DateTime? ActionCompletionDate { get; set; }

        // For parent/student acknowledgement
        public bool Acknowledged { get; set; } = false;

        public DateTime? AcknowledgedDate { get; set; }

        [MaxLength(500)]
        public string? AcknowledgedBy { get; set; }

        // Navigation Properties
        public ProgressReport ProgressReport { get; set; } = null!;
        public Teacher? CommentedByTeacher { get; set; }
        public Parent? CommentedByParent { get; set; }

        // Computed Properties
        public bool IsOverdue => ActionDueDate.HasValue &&
                                 DateTime.Today > ActionDueDate.Value &&
                                 !ActionCompleted;
    }
}