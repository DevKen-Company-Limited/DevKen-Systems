using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Navigation
{
    /// <summary>
    /// Central configuration for all navigation menu items.
    /// Add new navigation paths here without modifying the NavigationService.
    /// </summary>
    public static class NavigationConfiguration
    {
        /// <summary>
        /// Define all navigation sections and their menu items here.
        /// Each section is built based on user permissions.
        /// </summary>
        public static class Sections
        {
            public static NavigationSection SuperAdmin => new()
            {
                Id = "superadmin.panel",
                Title = "Super Admin Panel",
                Icon = "heroicons_outline:shield-check",
                RequiredPermission = "SuperAdmin",
                Items = new[]
                {
                    new NavItem("superadmin.settings", "Settings", "heroicons_outline:cog-6-tooth", "/superadmin/settings"),
                    new NavItem("superadmin.logs", "Activity Logs", "heroicons_outline:document-text", "/superadmin/logs")
                }
            };

            public static NavigationSection Administration => new()
            {
                Id = "administration",
                Title = "Administration",
                Icon = "heroicons_outline:cog-6-tooth",
                Items = new[]
                {
                    new NavItem("administration.school", "School Settings", "heroicons_outline:building-office", "/administration/school", PermissionKeys.SchoolRead),
                    new NavItem("administration.users", "Users", "heroicons_outline:users", "/administration/users", PermissionKeys.UserRead),
                    new NavItem("administration.roles", "Roles & Permissions", "heroicons_outline:shield-check", "/administration/roles", PermissionKeys.RoleRead)
                }
            };

            public static NavigationSection Academic => new()
            {
                Id = "academic",
                Title = "Academic",
                Icon = "heroicons_outline:academic-cap",
                Items = new[]
                {
                    new NavItem("academic.students", "Students", "heroicons_outline:academic-cap", "/academic/students", PermissionKeys.StudentRead),
                    new NavItem("academic.teachers", "Teachers", "heroicons_outline:user-group", "/academic/teachers", PermissionKeys.TeacherRead),
                    new NavItem("academic.classes", "Classes", "heroicons_outline:rectangle-group", "/academic/classes", PermissionKeys.ClassRead),
                    new NavItem("academic.subjects", "Subjects", "heroicons_outline:book-open", "/academic/subjects", PermissionKeys.SubjectRead),
                    new NavItem("academic.grades", "Grades", "heroicons_outline:chart-bar", "/academic/grades", PermissionKeys.GradeRead)
                }
            };

            public static NavigationSection Assessment => new()
            {
                Id = "assessment",
                Title = "Assessment",
                Icon = "heroicons_outline:clipboard-document-check",
                Items = new[]
                {
                    new NavItem("assessment.assessments", "Assessments", "heroicons_outline:clipboard-document-list", "/assessment/assessments", PermissionKeys.AssessmentRead),
                    new NavItem("assessment.reports", "Reports", "heroicons_outline:document-chart-bar", "/assessment/reports", PermissionKeys.ReportRead)
                }
            };

            public static NavigationSection Finance => new()
            {
                Id = "finance",
                Title = "Finance",
                Icon = "heroicons_outline:banknotes",
                Items = new[]
                {
                    new NavItem("finance.fees", "Fee Structure", "heroicons_outline:currency-dollar", "/finance/fees", PermissionKeys.FeeRead),
                    new NavItem("finance.payments", "Payments", "heroicons_outline:credit-card", "/finance/payments", PermissionKeys.PaymentRead),
                    new NavItem("finance.invoices", "Invoices", "heroicons_outline:document-text", "/finance/invoices", PermissionKeys.InvoiceRead)
                }
            };

            public static NavigationSection Curriculum => new()
            {
                Id = "curriculum",
                Title = "Curriculum",
                Icon = "heroicons_outline:book-open",
                Items = new[]
                {
                    new NavItem("curriculum.structure", "Curriculum Structure", "heroicons_outline:squares-2x2", "/curriculum/structure", PermissionKeys.CurriculumRead),
                    new NavItem("curriculum.lesson-plans", "Lesson Plans", "heroicons_outline:document-duplicate", "/curriculum/lesson-plans", PermissionKeys.LessonPlanRead)
                }
            };

            /// <summary>
            /// Returns all sections in the order they should appear in the navigation.
            /// Add new sections to this list to include them in the navigation.
            /// </summary>
            public static IEnumerable<NavigationSection> GetAll()
            {
                yield return Administration;
                yield return Academic;
                yield return SuperAdmin;
                yield return Assessment;
                yield return Finance;
                yield return Curriculum;

                // ✅ ADD NEW SECTIONS HERE:
                // yield return YourNewSection;
            }
        }

        /// <summary>
        /// Represents a navigation section (collapsible group).
        /// </summary>
        public class NavigationSection
        {
            public string Id { get; init; } = string.Empty;
            public string Title { get; init; } = string.Empty;
            public string Icon { get; init; } = string.Empty;
            public string? RequiredPermission { get; init; }
            public IEnumerable<NavItem> Items { get; init; } = Array.Empty<NavItem>();
        }

        /// <summary>
        /// Represents a single navigation menu item.
        /// </summary>
        public class NavItem
        {
            public string Id { get; }
            public string Title { get; }
            public string Icon { get; }
            public string Link { get; }
            public string? RequiredPermission { get; }

            public NavItem(string id, string title, string icon, string link, string? requiredPermission = null)
            {
                Id = id;
                Title = title;
                Icon = icon;
                Link = link;
                RequiredPermission = requiredPermission;
            }
        }
    }
}
