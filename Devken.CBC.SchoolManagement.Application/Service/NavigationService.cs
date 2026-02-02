using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;

namespace Devken.CBC.SchoolManagement.Application.Service
{
    public class NavigationService : INavigationService
    {
        public Task<NavigationResponse> GenerateNavigationAsync(List<string> userPermissions)
        {
            var permissionSet = userPermissions.ToHashSet();

            // Build the complete navigation structure
            var defaultNav = BuildDefaultNavigation(permissionSet);

            // Create response with all layout variations
            var response = new NavigationResponse
            {
                Default = defaultNav,
                Compact = BuildCompactNavigation(defaultNav),
                Futuristic = BuildFuturisticNavigation(defaultNav),
                Horizontal = BuildHorizontalNavigation(defaultNav)
            };

            return Task.FromResult(response);
        }

        private List<NavigationItem> BuildDefaultNavigation(HashSet<string> permissions)
        {
            var navigation = new List<NavigationItem>();

            // Dashboard - Always visible
            navigation.Add(new NavigationItem
            {
                Id = "dashboard",
                Title = "Dashboard",
                Type = "basic",
                Icon = "heroicons_outline:home",
                Link = "/dashboard"
            });

            // Administration Section
            var administrationItems = BuildAdministrationSection(permissions);
            if (administrationItems.Children?.Any() == true)
            {
                navigation.Add(administrationItems);
            }

            // Academic Section
            var academicItems = BuildAcademicSection(permissions);
            if (academicItems.Children?.Any() == true)
            {
                navigation.Add(academicItems);
            }

            // Assessment Section
            var assessmentItems = BuildAssessmentSection(permissions);
            if (assessmentItems.Children?.Any() == true)
            {
                navigation.Add(assessmentItems);
            }

            // Finance Section
            var financeItems = BuildFinanceSection(permissions);
            if (financeItems.Children?.Any() == true)
            {
                navigation.Add(financeItems);
            }

            // Curriculum Section
            var curriculumItems = BuildCurriculumSection(permissions);
            if (curriculumItems.Children?.Any() == true)
            {
                navigation.Add(curriculumItems);
            }

            return navigation;
        }

        private NavigationItem BuildAdministrationSection(HashSet<string> permissions)
        {
            var children = new List<NavigationItem>();

            // School Settings
            if (permissions.Contains(PermissionKeys.SchoolRead))
            {
                children.Add(new NavigationItem
                {
                    Id = "administration.school",
                    Title = "School Settings",
                    Type = "basic",
                    Icon = "heroicons_outline:building-office",
                    Link = "/administration/school",
                    RequiredPermissions = new List<string> { PermissionKeys.SchoolRead }
                });
            }

            // User Management
            if (permissions.Contains(PermissionKeys.UserRead))
            {
                children.Add(new NavigationItem
                {
                    Id = "administration.users",
                    Title = "Users",
                    Type = "basic",
                    Icon = "heroicons_outline:users",
                    Link = "/administration/users",
                    RequiredPermissions = new List<string> { PermissionKeys.UserRead }
                });
            }

            // Role Management
            if (permissions.Contains(PermissionKeys.RoleRead))
            {
                children.Add(new NavigationItem
                {
                    Id = "administration.roles",
                    Title = "Roles & Permissions",
                    Type = "basic",
                    Icon = "heroicons_outline:shield-check",
                    Link = "/administration/roles",
                    RequiredPermissions = new List<string> { PermissionKeys.RoleRead }
                });
            }

            return new NavigationItem
            {
                Id = "administration",
                Title = "Administration",
                Type = "collapsable",
                Icon = "heroicons_outline:cog-6-tooth",
                Children = children
            };
        }

        private NavigationItem BuildAcademicSection(HashSet<string> permissions)
        {
            var children = new List<NavigationItem>();

            // Students
            if (permissions.Contains(PermissionKeys.StudentRead))
            {
                children.Add(new NavigationItem
                {
                    Id = "academic.students",
                    Title = "Students",
                    Type = "basic",
                    Icon = "heroicons_outline:academic-cap",
                    Link = "/academic/students",
                    RequiredPermissions = new List<string> { PermissionKeys.StudentRead }
                });
            }

            // Teachers
            if (permissions.Contains(PermissionKeys.TeacherRead))
            {
                children.Add(new NavigationItem
                {
                    Id = "academic.teachers",
                    Title = "Teachers",
                    Type = "basic",
                    Icon = "heroicons_outline:user-group",
                    Link = "/academic/teachers",
                    RequiredPermissions = new List<string> { PermissionKeys.TeacherRead }
                });
            }

            // Classes
            if (permissions.Contains(PermissionKeys.ClassRead))
            {
                children.Add(new NavigationItem
                {
                    Id = "academic.classes",
                    Title = "Classes",
                    Type = "basic",
                    Icon = "heroicons_outline:rectangle-group",
                    Link = "/academic/classes",
                    RequiredPermissions = new List<string> { PermissionKeys.ClassRead }
                });
            }

            // Subjects
            if (permissions.Contains(PermissionKeys.SubjectRead))
            {
                children.Add(new NavigationItem
                {
                    Id = "academic.subjects",
                    Title = "Subjects",
                    Type = "basic",
                    Icon = "heroicons_outline:book-open",
                    Link = "/academic/subjects",
                    RequiredPermissions = new List<string> { PermissionKeys.SubjectRead }
                });
            }

            // Grades
            if (permissions.Contains(PermissionKeys.GradeRead))
            {
                children.Add(new NavigationItem
                {
                    Id = "academic.grades",
                    Title = "Grades",
                    Type = "basic",
                    Icon = "heroicons_outline:chart-bar",
                    Link = "/academic/grades",
                    RequiredPermissions = new List<string> { PermissionKeys.GradeRead }
                });
            }

            return new NavigationItem
            {
                Id = "academic",
                Title = "Academic",
                Type = "collapsable",
                Icon = "heroicons_outline:academic-cap",
                Children = children
            };
        }

        private NavigationItem BuildAssessmentSection(HashSet<string> permissions)
        {
            var children = new List<NavigationItem>();

            // Assessments
            if (permissions.Contains(PermissionKeys.AssessmentRead))
            {
                children.Add(new NavigationItem
                {
                    Id = "assessment.assessments",
                    Title = "Assessments",
                    Type = "basic",
                    Icon = "heroicons_outline:clipboard-document-list",
                    Link = "/assessment/assessments",
                    RequiredPermissions = new List<string> { PermissionKeys.AssessmentRead }
                });
            }

            // Reports
            if (permissions.Contains(PermissionKeys.ReportRead))
            {
                children.Add(new NavigationItem
                {
                    Id = "assessment.reports",
                    Title = "Reports",
                    Type = "basic",
                    Icon = "heroicons_outline:document-chart-bar",
                    Link = "/assessment/reports",
                    RequiredPermissions = new List<string> { PermissionKeys.ReportRead }
                });
            }

            return new NavigationItem
            {
                Id = "assessment",
                Title = "Assessment",
                Type = "collapsable",
                Icon = "heroicons_outline:clipboard-document-check",
                Children = children
            };
        }

        private NavigationItem BuildFinanceSection(HashSet<string> permissions)
        {
            var children = new List<NavigationItem>();

            // Fee Structure
            if (permissions.Contains(PermissionKeys.FeeRead))
            {
                children.Add(new NavigationItem
                {
                    Id = "finance.fees",
                    Title = "Fee Structure",
                    Type = "basic",
                    Icon = "heroicons_outline:currency-dollar",
                    Link = "/finance/fees",
                    RequiredPermissions = new List<string> { PermissionKeys.FeeRead }
                });
            }

            // Payments
            if (permissions.Contains(PermissionKeys.PaymentRead))
            {
                children.Add(new NavigationItem
                {
                    Id = "finance.payments",
                    Title = "Payments",
                    Type = "basic",
                    Icon = "heroicons_outline:credit-card",
                    Link = "/finance/payments",
                    RequiredPermissions = new List<string> { PermissionKeys.PaymentRead }
                });
            }

            // Invoices
            if (permissions.Contains(PermissionKeys.InvoiceRead))
            {
                children.Add(new NavigationItem
                {
                    Id = "finance.invoices",
                    Title = "Invoices",
                    Type = "basic",
                    Icon = "heroicons_outline:document-text",
                    Link = "/finance/invoices",
                    RequiredPermissions = new List<string> { PermissionKeys.InvoiceRead }
                });
            }

            return new NavigationItem
            {
                Id = "finance",
                Title = "Finance",
                Type = "collapsable",
                Icon = "heroicons_outline:banknotes",
                Children = children
            };
        }

        private NavigationItem BuildCurriculumSection(HashSet<string> permissions)
        {
            var children = new List<NavigationItem>();

            // Curriculum
            if (permissions.Contains(PermissionKeys.CurriculumRead))
            {
                children.Add(new NavigationItem
                {
                    Id = "curriculum.structure",
                    Title = "Curriculum Structure",
                    Type = "basic",
                    Icon = "heroicons_outline:squares-2x2",
                    Link = "/curriculum/structure",
                    RequiredPermissions = new List<string> { PermissionKeys.CurriculumRead }
                });
            }

            // Lesson Plans
            if (permissions.Contains(PermissionKeys.LessonPlanRead))
            {
                children.Add(new NavigationItem
                {
                    Id = "curriculum.lesson-plans",
                    Title = "Lesson Plans",
                    Type = "basic",
                    Icon = "heroicons_outline:document-duplicate",
                    Link = "/curriculum/lesson-plans",
                    RequiredPermissions = new List<string> { PermissionKeys.LessonPlanRead }
                });
            }

            return new NavigationItem
            {
                Id = "curriculum",
                Title = "Curriculum",
                Type = "collapsable",
                Icon = "heroicons_outline:book-open",
                Children = children
            };
        }

        private List<NavigationItem> BuildCompactNavigation(List<NavigationItem> defaultNav)
        {
            // For compact view, keep only top-level items with children populated from default
            return defaultNav.Select(item => new NavigationItem
            {
                Id = item.Id,
                Title = item.Title,
                Type = item.Type,
                Icon = item.Icon,
                Link = item.Link,
                Children = item.Children
            }).ToList();
        }

        private List<NavigationItem> BuildFuturisticNavigation(List<NavigationItem> defaultNav)
        {
            // For futuristic view, same as compact but could have different styling hints
            return BuildCompactNavigation(defaultNav);
        }

        private List<NavigationItem> BuildHorizontalNavigation(List<NavigationItem> defaultNav)
        {
            // For horizontal view, flatten the structure slightly
            return defaultNav.Select(item => new NavigationItem
            {
                Id = item.Id,
                Title = item.Title,
                Type = item.Type == "collapsable" ? "group" : item.Type,
                Icon = item.Icon,
                Link = item.Link,
                Children = item.Children
            }).ToList();
        }
    }
}