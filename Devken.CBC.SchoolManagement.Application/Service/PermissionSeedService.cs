using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service
{
    public interface IPermissionSeedService
    {
        /// <summary>
        /// Seeds all permissions and default roles for a single school.
        /// Called during school registration.
        /// Returns the ID of the SchoolAdmin role created for that school.
        /// </summary>
        Task<Guid> SeedPermissionsAndRolesAsync(Guid schoolId);

        /// <summary>
        /// Seeds permissions and default roles for every school in the system
        /// that does not yet have the full set of roles/permissions.
        /// Safe to call multiple times — skips anything already seeded.
        /// </summary>
        Task SeedAllSchoolsAsync();
    }
}