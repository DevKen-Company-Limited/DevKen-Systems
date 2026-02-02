namespace Devken.CBC.SchoolManagement.Application.Dtos
{
    /// <summary>
    /// Navigation response containing all layout variations
    /// </summary>
    public class NavigationResponse
    {
        public List<NavigationItem> Default { get; set; } = new();
        public List<NavigationItem> Compact { get; set; } = new();
        public List<NavigationItem> Futuristic { get; set; } = new();
        public List<NavigationItem> Horizontal { get; set; } = new();
    }

    /// <summary>
    /// Individual navigation menu item
    /// </summary>
    public class NavigationItem
    {
        public string Id { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Type { get; set; } = "basic"; // basic, collapsable, group, divider
        public string? Icon { get; set; }
        public string? Link { get; set; }
        public bool? Hidden { get; set; }
        public bool? Disabled { get; set; }
        public string? Tooltip { get; set; }
        public string? Badge { get; set; }
        public List<NavigationItem>? Children { get; set; }
        public List<string>? RequiredPermissions { get; set; }
        public string? PermissionOperator { get; set; } = "AND"; // AND or OR
    }

    /// <summary>
    /// Navigation group for organizing menu items
    /// </summary>
    public class NavigationGroup
    {
        public string Id { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? Icon { get; set; }
        public int Order { get; set; }
        public List<NavigationItem> Items { get; set; } = new();
    }
}