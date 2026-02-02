using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;

using Microsoft.Extensions.Logging;
using System;

namespace Devken.CBC.SchoolManagement.Application.Service
{
    /// <summary>
    /// Service for seeding permissions and roles during school registration
    /// </summary>
    public interface IPermissionSeedService
    {
        /// <summary>
        /// Seeds all permissions and default roles for a school
        /// </summary>
        /// <param name="tenantId">School/Tenant ID</param>
        /// <returns>The SchoolAdmin role ID for assigning to first user</returns>
        Task<Guid> SeedPermissionsAndRolesAsync(Guid tenantId);
    }

 
}