using Devken.CBC.SchoolManagement.Domain.Entities.Subscription;
using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Isubscription
{
    /// <summary>
    /// Service for seeding initial subscription data
    /// </summary>
    public interface ISubscriptionSeedService
    {
        /// <summary>
        /// Seeds a default trial subscription for a newly registered school
        /// </summary>
        /// <param name="schoolId">The school ID to create subscription for</param>
        /// <returns>The created subscription</returns>
        Task<Subscription> SeedTrialSubscriptionAsync(Guid schoolId);

        /// <summary>
        /// Seeds predefined subscription plans for testing/demo purposes
        /// </summary>
        /// <param name="schoolId">The school ID to create subscriptions for</param>
        /// <returns>True if seeding was successful</returns>
        Task<bool> SeedDemoSubscriptionsAsync(Guid schoolId);

        /// <summary>
        /// Checks if a school already has a subscription
        /// </summary>
        /// <param name="schoolId">The school ID to check</param>
        /// <returns>True if subscription exists</returns>
        Task<bool> HasSubscriptionAsync(Guid schoolId);
    }
}