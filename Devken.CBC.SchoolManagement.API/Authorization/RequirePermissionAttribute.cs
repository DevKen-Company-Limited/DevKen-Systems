using Microsoft.AspNetCore.Authorization;

namespace Devken.CBC.SchoolManagement.API.Authorization
{
    /// <summary>
    /// Usage on a controller or action:
    ///     [RequirePermission("Students.Read")]
    ///     [RequirePermission("Students.Write", "Students.Delete")]  // ANY of these
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class RequirePermissionAttribute : AuthorizeAttribute
    {
        public RequirePermissionAttribute(params string[] permissions)
        {
            // Map to a policy name: "Permission:Students.Read|Students.Write"
            Policy = "Permission:" + string.Join("|", permissions);
        }
    }
}
