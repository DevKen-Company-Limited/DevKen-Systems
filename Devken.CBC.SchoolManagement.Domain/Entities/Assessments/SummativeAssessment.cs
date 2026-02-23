// Devken.CBC.SchoolManagement.Domain/Entities/Assessments/SummativeAssessment.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Assessments
{
    public class SummativeAssessment : Assessment1
    {
        [MaxLength(50)]
        public string? ExamType { get; set; }               // EndTerm | MidTerm | Final

        public TimeSpan? Duration { get; set; }

        public int NumberOfQuestions { get; set; }

        [Range(0, 100)]
        public decimal PassMark { get; set; } = 50.0m;

        public bool HasPracticalComponent { get; set; } = false;

        [Range(0, 100)]
        public decimal PracticalWeight { get; set; } = 0.0m;

        [Range(0, 100)]
        public decimal TheoryWeight { get; set; } = 100.0m;

        [MaxLength(1000)]
        public string? Instructions { get; set; }

        public ICollection<SummativeAssessmentScore> Scores { get; set; }
            = new List<SummativeAssessmentScore>();
    }
}