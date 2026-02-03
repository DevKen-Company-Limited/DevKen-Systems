using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Assessment
{
    public class SummativeAssessment : Assessment
    {
        [MaxLength(50)]
        public string? ExamType { get; set; } // EndTerm, MidTerm, Final

        public TimeSpan? Duration { get; set; }

        public int NumberOfQuestions { get; set; }

        public decimal PassMark { get; set; } = 50.0m;

        public bool HasPracticalComponent { get; set; } = false;

        public decimal PracticalWeight { get; set; } = 0.0m;

        public decimal TheoryWeight { get; set; } = 100.0m;

        [MaxLength(1000)]
        public string? Instructions { get; set; }

        // Navigation Properties
        public ICollection<SummativeAssessmentScore> Scores { get; set; } = new List<SummativeAssessmentScore>();
    }
}