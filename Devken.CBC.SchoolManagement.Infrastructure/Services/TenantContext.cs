using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services
{
    public class TenantContext
    {
        /// <summary>
        /// Null when the request has not yet been resolved
        /// (e.g. login endpoint before credentials are checked).
        /// </summary>
        public Guid? TenantId { get; set; }

        /// <summary>
        /// The School object if already loaded (cached to avoid re-querying).
        /// </summary>
        public School? CurrentTenant { get; set; }

        /// <summary>
        /// The Id of the user performing the current request.
        /// Set by TenantMiddleware after the JWT is validated.
        /// Null for unauthenticated requests (login, register-school)
        /// and for platform seed operations that run before any user exists.
        /// RepositoryBase reads this to stamp CreatedBy / UpdatedBy.
        /// </summary>
        public Guid? ActingUserId { get; set; }
    }
}
