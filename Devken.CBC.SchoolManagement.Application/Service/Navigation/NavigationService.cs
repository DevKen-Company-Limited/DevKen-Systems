using Devken.CBC.SchoolManagement.Application.Dtos;
using Microsoft.Extensions.Logging;

namespace Devken.CBC.SchoolManagement.Application.Service.Navigation
{
    public class NavigationService : INavigationService
    {
        private readonly ILogger<NavigationService> _logger;

        public NavigationService(ILogger<NavigationService> logger)
        {
            _logger = logger;
        }

        public Task<NavigationResponse> GenerateNavigationAsync(
            List<string> userPermissions,
            List<string> userRoles,
            bool useCache = true)
        {
            if (!userPermissions.Any() && !userRoles.Any())
            {
                return Task.FromResult(NavigationResponse.Empty());
            }

            var permissions = userPermissions.ToHashSet();
            var roles = userRoles.ToHashSet();

            var defaultNav = BuildDefault(permissions, roles);

            return Task.FromResult(new NavigationResponse
            {
                Default = defaultNav,
                Compact = Clone(defaultNav),
                Futuristic = Clone(defaultNav),
                Horizontal = Clone(defaultNav, true)
            });
        }

        private List<NavigationItem> BuildDefault(
            HashSet<string> permissions,
            HashSet<string> roles)
        {
            var nav = new List<NavigationItem>
            {
                CreateItem("dashboard", "Dashboard", "heroicons_outline:home", "/dashboard")
            };

            var isSuperAdmin = roles.Contains("SuperAdmin");

            foreach (var section in NavigationConfiguration.GetAll())
            {
                if (!string.IsNullOrEmpty(section.RequiredRole) &&
                    !roles.Contains(section.RequiredRole))
                    continue;

                var children = new List<NavigationItem>();

                foreach (var item in section.Items)
                {
                    var roleOk = string.IsNullOrEmpty(item.RequiredRole) ||
                                 roles.Contains(item.RequiredRole);

                    var permOk = isSuperAdmin ||
                                 string.IsNullOrEmpty(item.RequiredPermission) ||
                                 permissions.Contains(item.RequiredPermission);

                    if (roleOk && permOk)
                    {
                        children.Add(CreateItem(
                            item.Id,
                            item.Title,
                            item.Icon,
                            item.Link,
                            item.RequiredPermission));
                    }
                }

                if (children.Any())
                {
                    nav.Add(new NavigationItem
                    {
                        Id = section.Id,
                        Title = section.Title,
                        Icon = section.Icon,
                        Type = "collapsable",
                        Children = children
                    });
                }
            }

            return nav;
        }

        private static NavigationItem CreateItem(
            string id,
            string title,
            string icon,
            string link,
            string? permission = null)
        {
            return new NavigationItem
            {
                Id = id,
                Title = title,
                Icon = icon,
                Link = link,
                RequiredPermissions = permission == null ? null : new List<string> { permission }
            };
        }

        private static List<NavigationItem> Clone(
            List<NavigationItem> source,
            bool horizontal = false)
        {
            return source.Select(i => new NavigationItem
            {
                Id = i.Id,
                Title = i.Title,
                Icon = i.Icon,
                Link = i.Link,
                Type = horizontal && i.Type == "collapsable" ? "group" : i.Type,
                Children = i.Children?.Select(c => new NavigationItem
                {
                    Id = c.Id,
                    Title = c.Title,
                    Icon = c.Icon,
                    Link = c.Link,
                    Type = c.Type
                }).ToList()
            }).ToList();
        }
    }
}
