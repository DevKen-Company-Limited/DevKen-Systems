using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    [HttpGet]
    public async Task<IActionResult> GetNavigation([FromQuery] bool useCache = true)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            // Return empty navigation
            return SuccessResponse(NavigationResponse.Empty(), "User not authenticated");
        }

        var permissions = CurrentUserPermissions.ToList();
        var navigation = await _navigationService.GenerateNavigationAsync(permissions, useCache);

        return SuccessResponse(navigation, "Navigation retrieved successfully");
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshNavigation()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return SuccessResponse(NavigationResponse.Empty(), "User not authenticated");
        }

        var permissions = CurrentUserPermissions.ToList();
        var navigation = await _navigationService.GenerateNavigationAsync(permissions, useCache: false);

        await LogUserActivityAsync("NavigationRefresh");

        return SuccessResponse(navigation, "Navigation refreshed successfully");
    }

    [HttpGet("{layout}")]
    public async Task<IActionResult> GetNavigationByLayout(
        [FromRoute] string layout,
        [FromQuery] bool useCache = true)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return SuccessResponse(NavigationResponse.Empty(), "User not authenticated");
        }

        var permissions = CurrentUserPermissions.ToList();
        var navigation = await _navigationService.GenerateNavigationAsync(permissions, useCache);

        var layoutNavigation = layout.ToLowerInvariant() switch
        {
            "compact" => navigation.Compact,
            "futuristic" => navigation.Futuristic,
            "horizontal" => navigation.Horizontal,
            "default" => navigation.Default,
            _ => navigation.Default
        };

        return SuccessResponse(new { Layout = layout, Items = layoutNavigation },
            $"Navigation retrieved for {layout} layout");
    }
}
