using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Subscription
{
    public class SubscriptionPlans
    {
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

        public record SubscriptionFilter(
            SubscriptionStatus? Status,
            SubscriptionPlan? Plan,
            bool? IsExpiringSoon, // Within 7 days
            bool? IsExpired,
            DateTime? ExpiringBefore
        );
    }
}
