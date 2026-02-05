using Devken.CBC.SchoolManagement.Application.Dtos;

namespace Devken.CBC.SchoolManagement.Application.Service.Navigation
{
    /// <summary>
    /// Service for generating permission-based navigation menus
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// Generates navigation menu items based on user permissions
        /// </summary>
        /// <param name="userPermissions">List of permission keys the user has</param>
        /// <param name="useCache">Whether to use cached navigation if available</param>
        /// <returns>Navigation structure for all layout types</returns>
        Task<NavigationResponse> GenerateNavigationAsync(List<string> userPermissions, bool useCache = true);

        /// <summary>
        /// Clears the navigation cache for specific permissions
        /// </summary>
        /// <param name="permissionKey">Optional specific permission key to clear</param>
        Task ClearCacheAsync(string? permissionKey = null);
    }
}