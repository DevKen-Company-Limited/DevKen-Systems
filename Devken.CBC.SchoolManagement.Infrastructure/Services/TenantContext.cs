using System;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services
{
    /// <summary>
    /// Scoped service that holds the current tenant and user context
    /// UPDATED: Added IsSuperAdmin flag to handle SuperAdmin vs User table separation
    /// </summary>
    public class TenantContext
    {
        /// <summary>
        /// The current tenant (school) ID
        /// Null for SuperAdmin users who don't belong to a specific school
        /// </summary>
        public Guid? TenantId { get; set; }

        /// <summary>
        /// The ID of the currently acting user
        /// For SuperAdmins, this ID exists in SuperAdmins table
        /// For regular users, this ID exists in Users table
        /// </summary>
        public Guid? ActingUserId { get; set; }

        /// <summary>
        /// ADDED: Flag to indicate if the current user is a SuperAdmin
        /// Critical for handling audit fields correctly since SuperAdmins
        /// exist in a separate table from Users
        /// </summary>
        public bool IsSuperAdmin { get; set; }

        /// <summary>
        /// Email of the current user (for logging/audit purposes)
        /// </summary>
        public string? UserEmail { get; set; }

        /// <summary>
        /// Display name for the current user (optional)
        /// </summary>
        public string? UserDisplayName { get; set; }
    }
}