using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Security
{
    public static class CustomClaimTypes
    {
        // ───────────────
        // COMMON
        // ───────────────
        public const string Permissions = "permissions";   // repeated claim
        public const string Roles = "roles";               // repeated claim
        public const string IsSuperAdmin = "is_super_admin";

        // ───────────────
        // TENANT USER
        // ───────────────
        public const string TenantId = "tenant_id";
        public const string UserId = "user_id";
        public const string UserEmail = "user_email";

        // ───────────────
        // SUPER ADMIN
        // ───────────────
        public const string SuperAdminId = "super_admin_id";
        public const string SuperAdminEmail = "super_admin_email";
    }
}
