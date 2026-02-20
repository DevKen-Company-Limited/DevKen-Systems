using System;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Curriculum
{
    // ── Response ─────────────────────────────────────────────────────────────
    public class StrandResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid LearningAreaId { get; set; }
        public string? LearningAreaName { get; set; }
        public Guid TenantId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
    }

    // ── Create ────────────────────────────────────────────────────────────────
    public class CreateStrandDto
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = null!;

        [Required]
        public Guid LearningAreaId { get; set; }

        /// <summary>Required when caller is SuperAdmin; ignored for school users.</summary>
        public Guid? TenantId { get; set; }
    }

    // ── Update ────────────────────────────────────────────────────────────────
    public class UpdateStrandDto
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = null!;

        [Required]
        public Guid LearningAreaId { get; set; }
    }
}