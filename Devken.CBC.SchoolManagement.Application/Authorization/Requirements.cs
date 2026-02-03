using Devken.CBC.SchoolManagement.Application.Service;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Authorization
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }

    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly ICurrentUserService _currentUserService;

        public PermissionHandler(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            if (_currentUserService.IsSuperAdmin)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (_currentUserService.HasPermission(requirement.Permission))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }

    public class RoleRequirement : IAuthorizationRequirement
    {
        public string Role { get; }

        public RoleRequirement(string role)
        {
            Role = role;
        }
    }

    public class RoleHandler : AuthorizationHandler<RoleRequirement>
    {
        private readonly ICurrentUserService _currentUserService;

        public RoleHandler(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            RoleRequirement requirement)
        {
            if (_currentUserService.IsSuperAdmin)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (_currentUserService.IsInRole(requirement.Role))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }

    public class TenantAccessRequirement : IAuthorizationRequirement
    {
    }

    public class TenantAccessHandler : AuthorizationHandler<TenantAccessRequirement>
    {
        private readonly ICurrentUserService _currentUserService;

        public TenantAccessHandler(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            TenantAccessRequirement requirement)
        {
            if (_currentUserService.IsSuperAdmin)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (_currentUserService.TenantId.HasValue && _currentUserService.IsAuthenticated)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
