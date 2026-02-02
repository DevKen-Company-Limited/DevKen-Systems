using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Security
{
    /// <summary>
    /// Custom claim types used throughout the application.
    /// </summary>
    public static class CustomClaimTypes
    {
        public const string TenantId = "tenant_id";
        public const string UserId = "user_id";
        public const string UserEmail = "user_email";
        public const string Permissions = "permissions";  // repeated claim
    }
}
