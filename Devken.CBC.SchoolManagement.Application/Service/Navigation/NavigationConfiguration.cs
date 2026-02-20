using Devken.CBC.SchoolManagement.Domain.Entities.Identity;

namespace Devken.CBC.SchoolManagement.Application.Service.Navigation
{
    public static class NavigationConfiguration
    {
        public static IEnumerable<NavigationSection> GetAll()
        {
            yield return Design;
            yield return SuperAdmin;
            yield return Administration;
            yield return Academic;
            yield return Assessment;
            yield return Finance;
            yield return Curriculum;
            yield return Settings;
        }

        public static NavigationSection Design => new()
        {
            Id = "design",
            Title = "Design",
            Icon = "heroicons_outline:swatch",
            RequiredRole = "SuperAdmin",
            Items = new[]
    {
        new NavItem(
            "page-design-v1",
            "Page Design V1",
            "heroicons_outline:template",
            "/page-design-v1"
        )
    }
        };

        public static NavigationSection SuperAdmin => new()
        {
            Id = "superadmin",
            Title = "Super Admin Panel",
            Icon = "heroicons_outline:shield-check",
            RequiredRole = "SuperAdmin",
            Items = new[]
            {
                new NavItem("schools", "Schools", "heroicons_outline:building-office-2", "/administration/schools", PermissionKeys.SchoolRead),
                new NavItem("logs", "Activity Logs", "heroicons_outline:document-text", "/administration/logs")
            }
        };

        public static NavigationSection Administration => new()
        {
            Id = "administration",
            Title = "Administration",
            Icon = "heroicons_outline:cog-6-tooth",
            Items = new[]
            {
                new NavItem("users", "Users", "heroicons_outline:users", "/administration/users", PermissionKeys.UserRead),
                new NavItem("roles", "Roles", "heroicons_outline:shield-check", "/administration/roles", PermissionKeys.RoleRead),
                new NavItem("permissions", "permissions", "heroicons_outline:shield-check", "/administration/permissions", PermissionKeys.RoleRead)
            }
        };

        public static NavigationSection Academic => new()
        {
            Id = "academic",
            Title = "Academic",
            Icon = "heroicons_outline:academic-cap",
            Items = new[]
    {
        new NavItem("academic-years", "Academic Years", "heroicons_outline:calendar", "/academic/academic-years", PermissionKeys.AcademicYearRead),
        new NavItem("terms", "Terms", "heroicons_outline:calendar-days", "/academic/terms", PermissionKeys.TermRead),
        new NavItem("students", "Students", "heroicons_outline:user-group", "/academic/students", PermissionKeys.StudentRead),
        new NavItem("subjects", "Subjects", "heroicons_outline:book-open", "/academic/subjects", PermissionKeys.SubjectRead),  // ← ADD THIS
        new NavItem("teachers", "Teachers", "heroicons_outline:user-group", "/academic/teachers", PermissionKeys.TeacherRead),
        new NavItem("classes", "Classes", "heroicons_outline:rectangle-group", "/academic/classes", PermissionKeys.ClassRead)
    }
        };

        public static NavigationSection Assessment => new()
        {
            Id = "assessment",
            Title = "Assessment",
            Icon = "heroicons_outline:clipboard-document-check",
            Items = new[]
            {
            new NavItem("formative-assessments", "Formative",           "heroicons_outline:pencil-square",      "/assessment/formative",    PermissionKeys.AssessmentRead),
            new NavItem("summative-assessments", "Summative",           "heroicons_outline:document-text",      "/assessment/summative",    PermissionKeys.AssessmentRead),
            new NavItem("competency-tracking",   "Competency Tracking", "heroicons_outline:chart-bar",          "/assessment/competencies", PermissionKeys.AssessmentRead),
            new NavItem("assessment-reports",    "Reports",             "heroicons_outline:document-chart-bar", "/assessment/reports",      PermissionKeys.ReportRead)
            }
        };




        public static NavigationSection Finance => new()
        {
            Id = "finance",
            Title = "Finance",
            Icon = "heroicons_outline:banknotes",
            Items = new[]
            {
                new NavItem("fees", "Fees", "heroicons_outline:currency-dollar", "/finance/fees", PermissionKeys.FeeRead),
                new NavItem("payments", "Payments", "heroicons_outline:credit-card", "/finance/payments", PermissionKeys.PaymentRead),
                new NavItem("invoices", "Invoices", "heroicons_outline:document-text", "/finance/invoices", PermissionKeys.InvoiceRead)
            }
        };

        public static NavigationSection Curriculum => new()
        {
            Id = "curriculum",
            Title = "Curriculum",
            Icon = "heroicons_outline:book-open",
            Items = new[]
            {
                new NavItem("structure", "Structure", "heroicons_outline:squares-2x2", "/curriculum/structure", PermissionKeys.CurriculumRead),
                new NavItem("lessonplans", "Lesson Plans", "heroicons_outline:document-duplicate", "/curriculum/lesson-plans", PermissionKeys.LessonPlanRead)
            }
        };

        public static NavigationSection Settings => new()
            {
                Id = "settings",
                Title = "Settings",
                Icon = "heroicons_outline:cog-8-tooth",
                Items = new[]
            {
                new NavItem(
                    "document-number-series",
                    "Number Series",
                    "heroicons_outline:hashtag",
                    "/settings/document-number-series",
                    PermissionKeys.DocumentNumberSeriesRead
                ),
                // You can add more settings items here in the future
                // new NavItem("general-settings", "General Settings", "heroicons_outline:adjustments-horizontal", "/settings/general", PermissionKeys.SchoolWrite),
                // new NavItem("email-templates", "Email Templates", "heroicons_outline:envelope", "/settings/email-templates", PermissionKeys.SchoolWrite),
            }
        };

        public class NavigationSection
        {
            public string Id { get; init; } = string.Empty;
            public string Title { get; init; } = string.Empty;
            public string Icon { get; init; } = string.Empty;
            public string? RequiredPermission { get; init; }
            public string? RequiredRole { get; init; }
            public IEnumerable<NavItem> Items { get; init; } = Array.Empty<NavItem>();
        }

        public class NavItem
        {
            public string Id { get; }
            public string Title { get; }
            public string Icon { get; }
            public string Link { get; }
            public string? RequiredPermission { get; }
            public string? RequiredRole { get; }

            public NavItem(
                string id,
                string title,
                string icon,
                string link,
                string? requiredPermission = null,
                string? requiredRole = null)
            {
                Id = id;
                Title = title;
                Icon = icon;
                Link = link;
                RequiredPermission = requiredPermission;
                RequiredRole = requiredRole;
            }
        }
    }
}
