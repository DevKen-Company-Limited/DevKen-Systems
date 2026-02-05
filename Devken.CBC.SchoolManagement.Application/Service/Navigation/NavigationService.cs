using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Application.Service.Navigation;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Devken.CBC.SchoolManagement.Application.Service
{
    /// <summary>
    /// Refactored NavigationService that uses NavigationConfiguration.
    /// This service now focuses on caching and permission filtering logic.
    /// Navigation structure is defined in NavigationConfiguration.cs
    /// </summary>
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
                _cache.TryGetValue(cacheKey, out NavigationResponse? cachedNavigation))
            {
                _logger.LogDebug(
                    "Navigation loaded from cache. Key: {CacheKey}", cacheKey);

                if (cachedNavigation is not null)
                {
                    return Task.FromResult(cachedNavigation);
                }
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

        #region Core Navigation Builder

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
            if (permissions == null || permissions.Count == 0)
                return new List<NavigationItem>();

            // ✅ SuperAdmin gets everything
            if (permissions.Contains("SuperAdmin") || permissions.Contains(PermissionKeys.SuperAdmin))
            {
                permissions = new HashSet<string>(Enum.GetValues(typeof(PermissionKeys))
                    .Cast<string>());
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

            // ✅ Build navigation from configuration
            foreach (var section in NavigationConfiguration.Sections.GetAll())
            {
                var navItem = BuildSectionFromConfig(section, permissions);
                AddSectionIfNotEmpty(navigation, navItem);
            }

            return navigation;
        }

        /// <summary>
        /// Builds a navigation section from configuration, filtering items by permissions.
        /// </summary>
        private NavigationItem BuildSectionFromConfig(
            NavigationConfiguration.NavigationSection section,
            HashSet<string> permissions)
        {
            // Check section-level permission (e.g., SuperAdmin section)
            if (!string.IsNullOrWhiteSpace(section.RequiredPermission) &&
                !permissions.Contains(section.RequiredPermission))
            {
                return CreateNavigationItem("empty", "", "basic", "", null);
            }

            var children = new List<NavigationItem>();

            foreach (var item in section.Items)
            {
                // Filter items by permission
                if (string.IsNullOrWhiteSpace(item.RequiredPermission) ||
                    permissions.Contains(item.RequiredPermission))
                {
                    children.Add(CreateNavigationItem(
                        item.Id,
                        item.Title,
                        "basic",
                        item.Icon,
                        item.Link,
                        item.RequiredPermission
                    ));
                }
            }

            return CreateNavigationItem(
                section.Id,
                section.Title,
                "collapsable",
                section.Icon,
                children: children
            );
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

        #region Factory Methods

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
    }
}