using Devken.CBC.SchoolManagement.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Devken.CBC.SchoolManagement.API.Authorization
{
    /// <summary>
    /// Requirement: the user must hold at least one of the listed permissions.
    /// </summary>
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public IReadOnlyList<string> Permissions { get; }

        public PermissionRequirement(IEnumerable<string> permissions)
        {
            Permissions = permissions.ToList();
        }
    }

    /// <summary>
    /// Handler: inspects the "permissions" claims on the JWT and
    /// succeeds if ANY of the required permissions are present.
    /// SuperAdmin (is_super_admin == "true") bypasses all checks.
    /// </summary>
    public class PermissionAuthorizationHandler
        : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            // SuperAdmin bypass
            if (context.User.HasClaim("is_super_admin", "true"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Collect all permission claims
            var userPermissions = context.User
                .FindAll(CustomClaimTypes.Permissions)
                .Select(c => c.Value)
                .ToHashSet();

            // Succeed if the user holds ANY of the required permissions
            if (requirement.Permissions.Any(p => userPermissions.Contains(p)))
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Policy provider that dynamically creates a PermissionRequirement
    /// from the policy name pattern "Permission:Perm1|Perm2|..."
    /// </summary>
    public class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallback;

        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallback = new DefaultAuthorizationPolicyProvider(options);
        }

        /// <summary>
        /// Correct method to implement in ASP.NET Core 6+
        /// </summary>
        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith("Permission:"))
            {
                var permissions = policyName
                    .Substring("Permission:".Length)
                    .Split('|');

                var builder = new AuthorizationPolicyBuilder()
                    .AddRequirements(new PermissionRequirement(permissions));

                return Task.FromResult<AuthorizationPolicy?>(builder.Build());
            }

            return _fallback.GetPolicyAsync(policyName);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
            _fallback.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
            _fallback.GetFallbackPolicyAsync();
    }
}
