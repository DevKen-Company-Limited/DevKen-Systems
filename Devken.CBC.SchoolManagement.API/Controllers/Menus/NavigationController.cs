using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Menus
{
    [ApiController]
    [Route("api/navigation")]
    [Authorize]
    public class NavigationController : BaseApiController
    {
        private readonly INavigationService _navigationService;

        public NavigationController(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetNavigation()
        {
            var permissions = CurrentUserPermissions.ToList();
            var navigation = await _navigationService.GenerateNavigationAsync(permissions);

            return Ok(navigation);
        }

        [HttpGet("permissions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetUserPermissions()
        {
            var permissions = CurrentUserPermissions.ToList();
            return Ok(new { Permissions = permissions });
        }

        [HttpGet("check-permission/{permissionKey}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult CheckPermission(string permissionKey)
        {
            var hasPermission = HasPermission(permissionKey);
            return Ok(new { PermissionKey = permissionKey, HasPermission = hasPermission });
        }
    }
}
