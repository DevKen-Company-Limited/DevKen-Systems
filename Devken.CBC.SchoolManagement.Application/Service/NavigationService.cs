using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Devken.CBC.SchoolManagement.Application.Service
{
    public class NavigationService : INavigationService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<NavigationService> _logger;

        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

        public NavigationService(
            IMemoryCache cache,
            ILogger<NavigationService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public Task<NavigationResponse> GenerateNavigationAsync(
            List<string>? userPermissions,
            bool useCache = true)
        {
            // 🔒 NOT LOGGED IN / NO PERMISSIONS
            if (userPermissions == null || !userPermissions.Any())
            {
                _logger.LogInformation(
                    "Navigation requested without permissions. Returning empty navigation.");

                return Task.FromResult(EmptyNavigation());
            }

            var permissionSet = userPermissions.ToHashSet();
            var cacheKey = GenerateCacheKey(permissionSet);

            if (useCache &&
                _cache.TryGetValue(cacheKey, out NavigationResponse cachedNavigation))
            {
                _logger.LogDebug(
                    "Navigation loaded from cache. Key: {CacheKey}", cacheKey);

                return Task.FromResult(cachedNavigation);
            }

            var navigation = BuildNavigation(permissionSet);

            if (useCache)
            {
                _cache.Set(
                    cacheKey,
                    navigation,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = CacheDuration,
                        SlidingExpiration = TimeSpan.FromMinutes(20)
                    });

                _logger.LogDebug(
                    "Navigation cached successfully. Key: {CacheKey}", cacheKey);
            }

            return Task.FromResult(navigation);
        }

        public Task ClearCacheAsync(string? permissionKey = null)
        {
            _logger.LogInformation(
                "Navigation cache clear requested. PermissionKey: {PermissionKey}",
                permissionKey ?? "ALL");

            // Placeholder – real implementation would track keys
            return Task.CompletedTask;
        }

        #region Helpers

        private static NavigationResponse EmptyNavigation()
        {
            return new NavigationResponse
            {
                Default = new List<NavigationItem>(),
                Compact = new List<NavigationItem>(),
                Futuristic = new List<NavigationItem>(),
                Horizontal = new List<NavigationItem>()
            };
        }

        private static string GenerateCacheKey(HashSet<string> permissions)
        {
            return $"nav:{string.Join(",", permissions.OrderBy(p => p))}";
        }

        #endregion

        #region Navigation Builders

        private NavigationResponse BuildNavigation(HashSet<string> permissions)
        {
            var defaultNav = BuildDefaultNavigation(permissions);

            return new NavigationResponse
            {
                Default = defaultNav,
                Compact = BuildCompactNavigation(defaultNav),
                Futuristic = BuildFuturisticNavigation(defaultNav),
                Horizontal = BuildHorizontalNavigation(defaultNav)
            };
        }

        private List<NavigationItem> BuildDefaultNavigation(HashSet<string> permissions)
        {
            // 🛑 Safety net
            if (permissions.Count == 0)
            {
                return new List<NavigationItem>();
            }

            var navigation = new List<NavigationItem>
            {
                CreateNavigationItem(
                    "dashboard",
                    "Dashboard",
                    "basic",
                    "heroicons_outline:home",
                    "/dashboard"
                )
            };

            AddSectionIfNotEmpty(navigation, BuildAdministrationSection(permissions));
            AddSectionIfNotEmpty(navigation, BuildAcademicSection(permissions));
            AddSectionIfNotEmpty(navigation, BuildAssessmentSection(permissions));
            AddSectionIfNotEmpty(navigation, BuildFinanceSection(permissions));
            AddSectionIfNotEmpty(navigation, BuildCurriculumSection(permissions));

            return navigation;
        }

        private static void AddSectionIfNotEmpty(
            List<NavigationItem> navigation,
            NavigationItem section)
        {
            if (section.Children?.Any() == true)
            {
                navigation.Add(section);
            }
        }

        #endregion

        #region Sections

        private NavigationItem BuildAdministrationSection(HashSet<string> permissions)
        {
            var children = new List<NavigationItem>();

            if (permissions.Contains(PermissionKeys.SchoolRead))
            {
                children.Add(CreateNavigationItem(
                    "administration.school",
                    "School Settings",
                    "basic",
                    "heroicons_outline:building-office",
                    "/administration/school",
                    PermissionKeys.SchoolRead
                ));
            }

            if (permissions.Contains(PermissionKeys.UserRead))
            {
                children.Add(CreateNavigationItem(
                    "administration.users",
                    "Users",
                    "basic",
                    "heroicons_outline:users",
                    "/administration/users",
                    PermissionKeys.UserRead
                ));
            }

            if (permissions.Contains(PermissionKeys.RoleRead))
            {
                children.Add(CreateNavigationItem(
                    "administration.roles",
                    "Roles & Permissions",
                    "basic",
                    "heroicons_outline:shield-check",
                    "/administration/roles",
                    PermissionKeys.RoleRead
                ));
            }

            return CreateNavigationItem(
                "administration",
                "Administration",
                "collapsable",
                "heroicons_outline:cog-6-tooth",
                children: children
            );
        }

        private NavigationItem BuildAcademicSection(HashSet<string> permissions)
        {
            var children = new List<NavigationItem>();

            var items = new[]
            {
                (PermissionKeys.StudentRead, "students", "Students", "heroicons_outline:academic-cap", "/academic/students"),
                (PermissionKeys.TeacherRead, "teachers", "Teachers", "heroicons_outline:user-group", "/academic/teachers"),
                (PermissionKeys.ClassRead, "classes", "Classes", "heroicons_outline:rectangle-group", "/academic/classes"),
                (PermissionKeys.SubjectRead, "subjects", "Subjects", "heroicons_outline:book-open", "/academic/subjects"),
                (PermissionKeys.GradeRead, "grades", "Grades", "heroicons_outline:chart-bar", "/academic/grades")
            };

            foreach (var (permission, id, title, icon, link) in items)
            {
                if (permissions.Contains(permission))
                {
                    children.Add(CreateNavigationItem(
                        $"academic.{id}",
                        title,
                        "basic",
                        icon,
                        link,
                        permission
                    ));
                }
            }

            return CreateNavigationItem(
                "academic",
                "Academic",
                "collapsable",
                "heroicons_outline:academic-cap",
                children: children
            );
        }

        private NavigationItem BuildAssessmentSection(HashSet<string> permissions)
        {
            var children = new List<NavigationItem>();

            if (permissions.Contains(PermissionKeys.AssessmentRead))
            {
                children.Add(CreateNavigationItem(
                    "assessment.assessments",
                    "Assessments",
                    "basic",
                    "heroicons_outline:clipboard-document-list",
                    "/assessment/assessments",
                    PermissionKeys.AssessmentRead
                ));
            }

            if (permissions.Contains(PermissionKeys.ReportRead))
            {
                children.Add(CreateNavigationItem(
                    "assessment.reports",
                    "Reports",
                    "basic",
                    "heroicons_outline:document-chart-bar",
                    "/assessment/reports",
                    PermissionKeys.ReportRead
                ));
            }

            return CreateNavigationItem(
                "assessment",
                "Assessment",
                "collapsable",
                "heroicons_outline:clipboard-document-check",
                children: children
            );
        }

        private NavigationItem BuildFinanceSection(HashSet<string> permissions)
        {
            var children = new List<NavigationItem>();

            var items = new[]
            {
                (PermissionKeys.FeeRead, "fees", "Fee Structure", "heroicons_outline:currency-dollar", "/finance/fees"),
                (PermissionKeys.PaymentRead, "payments", "Payments", "heroicons_outline:credit-card", "/finance/payments"),
                (PermissionKeys.InvoiceRead, "invoices", "Invoices", "heroicons_outline:document-text", "/finance/invoices")
            };

            foreach (var (permission, id, title, icon, link) in items)
            {
                if (permissions.Contains(permission))
                {
                    children.Add(CreateNavigationItem(
                        $"finance.{id}",
                        title,
                        "basic",
                        icon,
                        link,
                        permission
                    ));
                }
            }

            return CreateNavigationItem(
                "finance",
                "Finance",
                "collapsable",
                "heroicons_outline:banknotes",
                children: children
            );
        }

        private NavigationItem BuildCurriculumSection(HashSet<string> permissions)
        {
            var children = new List<NavigationItem>();

            if (permissions.Contains(PermissionKeys.CurriculumRead))
            {
                children.Add(CreateNavigationItem(
                    "curriculum.structure",
                    "Curriculum Structure",
                    "basic",
                    "heroicons_outline:squares-2x2",
                    "/curriculum/structure",
                    PermissionKeys.CurriculumRead
                ));
            }

            if (permissions.Contains(PermissionKeys.LessonPlanRead))
            {
                children.Add(CreateNavigationItem(
                    "curriculum.lesson-plans",
                    "Lesson Plans",
                    "basic",
                    "heroicons_outline:document-duplicate",
                    "/curriculum/lesson-plans",
                    PermissionKeys.LessonPlanRead
                ));
            }

            return CreateNavigationItem(
                "curriculum",
                "Curriculum",
                "collapsable",
                "heroicons_outline:book-open",
                children: children
            );
        }

        #endregion

        #region Layout Variations

        private static List<NavigationItem> BuildCompactNavigation(
            List<NavigationItem> defaultNav)
        {
            return defaultNav
         .Select(item => CloneNavigationItem(item))
         .ToList();

        }

        private static List<NavigationItem> BuildFuturisticNavigation(
            List<NavigationItem> defaultNav)
        {
            return BuildCompactNavigation(defaultNav);
        }

        private static List<NavigationItem> BuildHorizontalNavigation(
            List<NavigationItem> defaultNav)
        {
            return defaultNav.Select(item =>
                CloneNavigationItem(
                    item,
                    item.Type == "collapsable" ? "group" : item.Type
                )
            ).ToList();
        }

        #endregion

        #region Factory

        private static NavigationItem CreateNavigationItem(
            string id,
            string title,
            string type,
            string icon,
            string? link = null,
            string? permission = null,
            List<NavigationItem>? children = null)
        {
            var item = new NavigationItem
            {
                Id = id,
                Title = title,
                Type = type,
                Icon = icon,
                Link = link,
                Children = children
            };

            if (!string.IsNullOrWhiteSpace(permission))
            {
                item.RequiredPermissions = new List<string> { permission };
            }

            return item;
        }

        private static NavigationItem CloneNavigationItem(
            NavigationItem source,
            string? type = null)
        {
            return new NavigationItem
            {
                Id = source.Id,
                Title = source.Title,
                Type = type ?? source.Type,
                Icon = source.Icon,
                Link = source.Link,
                RequiredPermissions = source.RequiredPermissions,
                Children = source.Children?
                    .Select(child => CloneNavigationItem(child))
                    .ToList()
            };
        }


        #endregion
    }
}
