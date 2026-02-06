using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.Navigation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Api.Controllers
{
    [ApiController]
    [Route("api/navigation")]
    [Authorize]
    public class NavigationController : BaseApiController
    {
        private readonly INavigationService _navigationService;

        public NavigationController(
            INavigationService navigationService,
            IUserActivityService activityService)
            : base(activityService)
        {
            _navigationService = navigationService;
        }

        /// <summary>
        /// Gets the full navigation menu for the authenticated user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNavigation()
        {
            var permissions = CurrentUserPermissions.ToList();
            var roles = CurrentUserRoles.ToList();

            var navigation = await _navigationService.GenerateNavigationAsync(
                permissions,
                roles);

            return SuccessResponse(navigation, "Navigation retrieved successfully");
        }

        /// <summary>
        /// Gets navigation for a specific layout type
        /// </summary>
        /// <param name="layout">default | compact | futuristic | horizontal</param>
        [HttpGet("{layout}")]
        public async Task<IActionResult> GetNavigationByLayout([FromRoute] string layout)
        {
            var permissions = CurrentUserPermissions.ToList();
            var roles = CurrentUserRoles.ToList();

            var navigation = await _navigationService.GenerateNavigationAsync(
                permissions,
                roles);

            var items = layout.ToLowerInvariant() switch
            {
                "compact" => navigation.Compact,
                "futuristic" => navigation.Futuristic,
                "horizontal" => navigation.Horizontal,
                _ => navigation.Default
            };

            return SuccessResponse(
                new
                {
                    Layout = layout,
                    Items = items
                },
                $"Navigation retrieved for {layout} layout");
        }

        /// <summary>
        /// Refresh navigation (re-generated on demand)
        /// </summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshNavigation()
        {
            var permissions = CurrentUserPermissions.ToList();
            var roles = CurrentUserRoles.ToList();

            var navigation = await _navigationService.GenerateNavigationAsync(
                permissions,
                roles);

            await LogUserActivityAsync(
                "navigation.refresh",
                "User refreshed navigation");

            return SuccessResponse(navigation, "Navigation refreshed successfully");
        }

        /// <summary>
        /// Gets current user context (debug / verification)
        /// </summary>
        [HttpGet("context")]
        public IActionResult GetUserContext()
        {
            var context = new
            {
                UserId = CurrentUserId,
                Email = CurrentUserEmail,
                Name = CurrentUserName,
                IsSuperAdmin = IsSuperAdmin,
                SchoolId = CurrentSchoolId,
                Roles = CurrentUserRoles,
                Permissions = CurrentUserPermissions
            };

            return SuccessResponse(context, "User context retrieved successfully");
        }
    }
}
