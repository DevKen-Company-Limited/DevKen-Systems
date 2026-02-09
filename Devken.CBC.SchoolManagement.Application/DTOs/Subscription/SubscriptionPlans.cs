using Devken.CBC.SchoolManagement.Domain.Enums;
using System;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Subscription
{
    public class SubscriptionPlans
    {
        /// <summary>
        /// Request payload for creating a new subscription
        /// </summary>
        public record CreateSubscriptionRequest(
            Guid SchoolId,
            SubscriptionPlan Plan,
            BillingCycle BillingCycle,
            DateTime StartDate,
            int MaxStudents,
            int MaxTeachers,
            decimal MaxStorageGB,
            string[]? EnabledFeatures,
            decimal Amount,
            string Currency,
            bool AutoRenew,
            int GracePeriodDays,
            string? AdminNotes
        );

        /// <summary>
        /// Request payload for updating an existing subscription
        /// All fields are optional for partial updates
        /// </summary>
        public record UpdateSubscriptionRequest(
            SubscriptionPlan? Plan,
            int? MaxStudents,
            int? MaxTeachers,
            decimal? MaxStorageGB,
            string[]? EnabledFeatures,
            bool? AutoRenew,
            int? GracePeriodDays,
            DateTime? ExpiryDate,
            string? AdminNotes
        );

        /// <summary>
        /// Filtering options for querying subscriptions
        /// </summary>
        public record SubscriptionFilter(
            SubscriptionStatus? Status,
            SubscriptionPlan? Plan,
            bool? IsExpiringSoon,   // Within grace/expiry threshold (e.g. 7 days)
            bool? IsExpired,        // Fully expired (outside grace period)
            DateTime? ExpiringBefore
        );
    }
}
