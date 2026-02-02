using Devken.CBC.SchoolManagement.Application.Dtos;

namespace Devken.CBC.SchoolManagement.Application.Service
{
    public interface INavigationService
    {
        /// <summary>
        /// Generates navigation menu items based on user permissions
        /// </summary>
        /// <param name="userPermissions">List of permission keys the user has</param>
        /// <returns>Navigation structure for all layout types</returns>
        Task<NavigationResponse> GenerateNavigationAsync(List<string> userPermissions);
    }
}