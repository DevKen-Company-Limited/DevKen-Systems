using System;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Library
{
    public class CreateBookRecommendationRequest
    {
        public Guid SchoolId { get; set; }

        [Required]
        public Guid BookId { get; set; }

        [Required]
        public Guid StudentId { get; set; }

        [Required]
        [Range(0, 100)]
        public int Score { get; set; }

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    public class UpdateBookRecommendationRequest
    {
        [Range(0, 100)]
        public int? Score { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }
    }

    public class BookRecommendationDto
    {
        public Guid Id { get; set; }
        public Guid SchoolId { get; set; }
        public string SchoolName { get; set; } = string.Empty;
        public Guid BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int Score { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public Guid? UpdatedBy { get; set; }
    }

    public class GenerateRecommendationsRequest
    {
        public Guid SchoolId { get; set; }

        [Required]
        public Guid StudentId { get; set; }

        [Range(1, 50)]
        public int MaxRecommendations { get; set; } = 10;
    }
}