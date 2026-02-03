using Devken.CBC.SchoolManagement.Domain.Entities.Subscription;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Devken.CBC.SchoolManagement.Application.DTOs.Subscription.SubscriptionPlans;

namespace Devken.CBC.SchoolManagement.Application.Service.Isubscription
{
    public interface ISubscriptionService
    {
        // SuperAdmin only - Create subscription
        Task<Subscription> CreateSubscriptionAsync(CreateSubscriptionRequest request);

        // SuperAdmin only - Update subscription
        Task<Subscription?> UpdateSubscriptionAsync(Guid subscriptionId, UpdateSubscriptionRequest request);

        // SuperAdmin only - Suspend/Activate subscription
        Task<bool> SuspendSubscriptionAsync(Guid subscriptionId, string reason);
        Task<bool> ActivateSubscriptionAsync(Guid subscriptionId);

        // SuperAdmin only - Cancel subscription
        Task<bool> CancelSubscriptionAsync(Guid subscriptionId);

        // SuperAdmin only - Get all subscriptions
        Task<List<Subscription>> GetAllSubscriptionsAsync(SubscriptionFilter? filter = null);

        // SuperAdmin only - Get subscription by school
        Task<Subscription?> GetSchoolSubscriptionAsync(Guid schoolId);

        // School can view their own subscription
        Task<Subscription?> GetMySubscriptionAsync(Guid schoolId);

        // SuperAdmin only - Renew subscription
        Task<Subscription> RenewSubscriptionAsync(Guid subscriptionId, BillingCycle newCycle);

        // System - Check if school has access
        Task<bool> HasActiveSubscriptionAsync(Guid schoolId);

        // System - Check if feature is enabled
        Task<bool> HasFeatureAccessAsync(Guid schoolId, string featureName);
    }
}
