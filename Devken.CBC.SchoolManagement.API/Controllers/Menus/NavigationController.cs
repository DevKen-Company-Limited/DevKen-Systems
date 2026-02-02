using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.Service;
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
        private readonly ILogger<NavigationController> _logger;

        public NavigationController(
            INavigationService navigationService,
            ILogger<NavigationController> logger)
        {
            _navigationService = navigationService;
            _logger = logger;
        }

        /// <summary>
        /// Get navigation menu based on user permissions
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetNavigation()
        {
            try
            {
                _logger.LogInformation("Generating navigation for user {UserId}", CurrentUserId);

                var permissions = CurrentUserPermissions.ToList();
                var navigation = await _navigationService.GenerateNavigationAsync(permissions);

                _logger.LogInformation(
                    "Navigation generated successfully for user {UserId} with {PermissionCount} permissions",
                    CurrentUserId,
                    permissions.Count);

                return SuccessResponse(navigation, "Navigation generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating navigation for user {UserId}", CurrentUserId);
                return ErrorResponse("Failed to generate navigation", 500);
            }
        }

        /// <summary>
        /// Get all current user permissions
        /// </summary>
        [HttpGet("permissions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetUserPermissions()
        {
            try
            {
                var permissions = CurrentUserPermissions.ToList();

                return SuccessResponse(new
                {
                    Permissions = permissions,
                    PermissionCount = permissions.Count,
                    UserId = CurrentUserId,
                    UserEmail = CurrentUserEmail,
                    UserName = CurrentUserName,
                    TenantId = CurrentTenantId,
                    IsSuperAdmin = IsSuperAdmin
                }, "User permissions retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving permissions");
                return ErrorResponse("Failed to retrieve permissions", 500);
            }
        }

        /// <summary>
        /// Check if user has a specific permission
        /// </summary>
        [HttpGet("check-permission/{permissionKey}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult CheckPermission(string permissionKey)
        {
            try
            {
                var hasPermission = HasPermission(permissionKey);

                return SuccessResponse(new
                {
                    PermissionKey = permissionKey,
                    HasPermission = hasPermission,
                    UserId = CurrentUserId,
                    IsSuperAdmin = IsSuperAdmin
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission}", permissionKey);
                return ErrorResponse("Failed to check permission", 500);
            }
        }

        /// <summary>
        /// Check multiple permissions at once
        /// </summary>
        [HttpPost("check-permissions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult CheckPermissions([FromBody] List<string> permissionKeys)
        {
            try
            {
                if (permissionKeys == null || !permissionKeys.Any())
                {
                    return ValidationErrorResponse(new Dictionary<string, string[]>
                    {
                        { "permissionKeys", new[] { "At least one permission key is required" } }
                    });
                }

                var results = permissionKeys.ToDictionary(
                    key => key,
                    key => HasPermission(key)
                );

                return SuccessResponse(new
                {
                    PermissionChecks = results,
                    UserId = CurrentUserId,
                    IsSuperAdmin = IsSuperAdmin
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking multiple permissions");
                return ErrorResponse("Failed to check permissions", 500);
            }
        }

        /// <summary>
        /// Get all claims from the current user's JWT token (for debugging)
        /// </summary>
        [HttpGet("debug/claims")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetClaims()
        {
            try
            {
                return SuccessResponse(new
                {
                    UserId = CurrentUserId,
                    TenantId = CurrentTenantId,
                    Email = CurrentUserEmail,
                    Name = CurrentUserName,
                    Permissions = CurrentUserPermissions.ToList(),
                    PermissionCount = CurrentUserPermissions.Count(),
                    Roles = CurrentUserRoles.ToList(),
                    RoleCount = CurrentUserRoles.Count(),
                    IsSuperAdmin = IsSuperAdmin,
                    AllClaims = GetAllClaims(),
                    ClaimCount = GetAllClaims().Count
                }, "Claims retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving claims");
                return ErrorResponse("Failed to retrieve claims", 500);
            }
        }

        /// <summary>
        /// Get user profile with permissions (for initial app load)
        /// </summary>
        [HttpGet("profile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetUserProfile()
        {
            try
            {
                _logger.LogInformation("Loading profile for user {UserId}", CurrentUserId);

                var permissions = CurrentUserPermissions.ToList();
                var navigation = await _navigationService.GenerateNavigationAsync(permissions);

                return SuccessResponse(new
                {
                    User = new
                    {
                        Id = CurrentUserId,
                        Email = CurrentUserEmail,
                        Name = CurrentUserName,
                        TenantId = CurrentTenantId,
                        IsSuperAdmin = IsSuperAdmin
                    },
                    Permissions = permissions,
                    PermissionCount = permissions.Count,
                    Roles = CurrentUserRoles.ToList(),
                    Navigation = navigation
                }, "User profile loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile for user {UserId}", CurrentUserId);
                return ErrorResponse("Failed to load user profile", 500);
            }
        }

        /// <summary>
        /// Health check endpoint - verifies authentication is working
        /// </summary>
        [HttpGet("health")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult HealthCheck()
        {
            try
            {
                return SuccessResponse(new
                {
                    Status = "Healthy",
                    IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                    AuthenticationType = User.Identity?.AuthenticationType,
                    UserId = CurrentUserId,
                    Timestamp = DateTime.UtcNow
                }, "Authentication is working correctly");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return ErrorResponse($"Health check failed: {ex.Message}", 500);
            }
        }
    }
}