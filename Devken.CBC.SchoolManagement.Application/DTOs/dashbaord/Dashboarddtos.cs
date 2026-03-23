using System;
using System.Collections.Generic;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Dashboard
{
    public sealed class DashboardQueryParams
    {
        public string? Level { get; set; }
        public Guid? TermId { get; set; }
        public Guid? AcademicYearId { get; set; }
        public Guid? SchoolId { get; set; }
    }

    public sealed class DashboardResponse
    {
        public string SchoolName { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public string CurrentTerm { get; set; } = string.Empty;
        public string ActiveLevel { get; set; } = "All Levels";

        public DashboardPermissions Permissions { get; set; } = new();
        public StatsSection? Stats { get; set; }
        public ClassPerformanceSection? ClassPerformance { get; set; }
        public CompetencySection? Competency { get; set; }
        public RecentAssessmentsSection? RecentAssessments { get; set; }
        public EventsSection? Events { get; set; }
        public FeeCollectionSection? FeeCollection { get; set; }
        public QuickActionsSection? QuickActions { get; set; }
    }

    public sealed class DashboardPermissions
    {
        public bool CanViewStats { get; set; }
        public bool CanViewClassPerformance { get; set; }
        public bool CanViewCompetency { get; set; }
        public bool CanViewRecentAssessments { get; set; }
        public bool CanViewEvents { get; set; }
        public bool CanViewFeeCollection { get; set; }
        public bool CanViewQuickActions { get; set; }
    }

    public sealed class StatsSection
    {
        public StatCard? EnrolledStudents { get; set; }
        public StatCard? TeachingStaff { get; set; }
        public StatCard? AssessmentsPending { get; set; }
        public StatCard? FeeCollectionRate { get; set; }
    }

    public sealed class StatCard
    {
        public string Icon { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Trend { get; set; } = string.Empty;
    }

    public sealed class ClassPerformanceSection
    {
        public string TermLabel { get; set; } = string.Empty;
        public List<ClassPerformanceItem> Classes { get; set; } = new();
    }

    public sealed class ClassPerformanceItem
    {
        public Guid ClassId { get; set; }
        public string Badge { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Meta { get; set; } = string.Empty;
        public int Pct { get; set; }
        public string Color { get; set; } = string.Empty;
    }

    public sealed class CompetencySection
    {
        public List<CompetencyItem> Items { get; set; } = new();
    }

    public sealed class CompetencyItem
    {
        public string Label { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int Pct { get; set; }
        public string Color { get; set; } = string.Empty;
    }

    public sealed class RecentAssessmentsSection
    {
        public List<AssessmentRow> Items { get; set; } = new();
    }

    public sealed class AssessmentRow
    {
        public Guid AssessmentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string LearningArea { get; set; } = string.Empty;
        public string AssessmentType { get; set; } = string.Empty;
        public string CompetencyLevel { get; set; } = string.Empty;
        public DateTime AssessmentDate { get; set; }
        public string TeacherName { get; set; } = string.Empty;
    }

    public sealed class EventsSection
    {
        public List<EventItem> Items { get; set; } = new();
    }

    public sealed class EventItem
    {
        public Guid EventId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string SubTitle { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Tag { get; set; } = string.Empty;
    }

    public sealed class FeeCollectionSection
    {
        public int CollectedPct { get; set; }
        public string ExpectedTotal { get; set; } = string.Empty;
        public string CollectedTotal { get; set; } = string.Empty;
        public string OutstandingTotal { get; set; } = string.Empty;
        public int DefaulterCount { get; set; }
    }

    public sealed class QuickActionsSection
    {
        public List<QuickActionItem> Items { get; set; } = new();
    }

    public sealed class QuickActionItem
    {
        public string Icon { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public string Action { get; set; } = string.Empty;
    }
}