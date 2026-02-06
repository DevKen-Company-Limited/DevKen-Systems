using Devken.CBC.SchoolManagement.Application.Dtos;

namespace Devken.CBC.SchoolManagement.Application.Service.Navigation
{
    public interface INavigationService
    {
        Task<NavigationResponse> GenerateNavigationAsync(
            List<string> userPermissions,
            List<string> userRoles,
            bool useCache = true);
    }
}
