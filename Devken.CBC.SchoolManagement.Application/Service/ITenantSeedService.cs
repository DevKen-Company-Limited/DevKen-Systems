using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service
{
    public interface ITenantSeedService
    {
        /// <summary>
        /// Seeds a newly created tenant (school) with default roles, permissions, and first admin.
        /// </summary>
        /// <param name="tenantId">ID of the tenant (school).</param>
        /// <param name="adminEmail">Email of the first admin.</param>
        /// <param name="adminPasswordHash">Hashed password of the first admin.</param>
        /// <param name="adminFirstName">Optional first name.</param>
        /// <param name="adminLastName">Optional last name.</param>
        /// <param name="actingUserId">Optional SuperAdmin ID or null for initial bootstrap.</param>
        Task SeedNewTenantAsync(
            Guid tenantId,
            string adminEmail,
            string adminPasswordHash,
            string? adminFirstName = null,
            string? adminLastName = null,
            Guid? actingUserId = null);
    }
}
