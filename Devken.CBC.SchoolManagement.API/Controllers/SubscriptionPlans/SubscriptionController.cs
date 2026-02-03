using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Application.Service.Isubscription;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Entities.Subscription;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Devken.CBC.SchoolManagement.Application.DTOs.Subscription.SubscriptionPlans;

namespace Devken.CBC.SchoolManagement.API.Controllers.SubscriptionPlans
{
    [Route("api/[controller]")]
    public class SubscriptionController : BaseApiController
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
        {
            if (!IsSuperAdmin)
                return ForbiddenResponse();

            try
            {
                var subscription = await _subscriptionService.CreateSubscriptionAsync(request);

                return SuccessResponse(new
                {
                    subscription.Id,
                    subscription.SchoolId,
                    subscription.Plan,
                    subscription.BillingCycle,
                    subscription.StartDate,
                    subscription.ExpiryDate,
                    subscription.Status,
                    subscription.Amount,
                    subscription.Currency,
                    subscription.MaxStudents,
                    subscription.MaxTeachers,
                    subscription.MaxStorageGB,
                    subscription.EnabledFeatures
                });
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResponse(ex.Message);
            }
        }

        [HttpPut("{subscriptionId}")]
        [Authorize]
        public async Task<IActionResult> UpdateSubscription(Guid subscriptionId, [FromBody] UpdateSubscriptionRequest request)
        {
            if (!IsSuperAdmin)
                return ForbiddenResponse();

            var subscription = await _subscriptionService.UpdateSubscriptionAsync(subscriptionId, request);
            if (subscription == null)
                return NotFoundResponse("Subscription not found");

            return SuccessResponse(subscription);
        }

        [HttpPost("{subscriptionId}/suspend")]
        [Authorize]
        public async Task<IActionResult> SuspendSubscription(Guid subscriptionId, [FromBody] SuspendRequest request)
        {
            if (!IsSuperAdmin)
                return ForbiddenResponse();

            var success = await _subscriptionService.SuspendSubscriptionAsync(subscriptionId, request.Reason ?? "No reason provided");
            if (!success)
                return NotFoundResponse("Subscription not found");

            return SuccessResponse(true, "Subscription suspended");
        }

        [HttpPost("{subscriptionId}/activate")]
        [Authorize]
        public async Task<IActionResult> ActivateSubscription(Guid subscriptionId)
        {
            if (!IsSuperAdmin)
                return ForbiddenResponse();

            var success = await _subscriptionService.ActivateSubscriptionAsync(subscriptionId);
            if (!success)
                return NotFoundResponse("Subscription not found");

            return SuccessResponse(true, "Subscription activated");
        }

        [HttpPost("{subscriptionId}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelSubscription(Guid subscriptionId)
        {
            if (!IsSuperAdmin)
                return ForbiddenResponse();

            var success = await _subscriptionService.CancelSubscriptionAsync(subscriptionId);
            if (!success)
                return NotFoundResponse("Subscription not found");

            return SuccessResponse(true, "Subscription cancelled");
        }

        [HttpPost("{subscriptionId}/renew")]
        [Authorize]
        public async Task<IActionResult> RenewSubscription(Guid subscriptionId, [FromBody] RenewRequest request)
        {
            if (!IsSuperAdmin)
                return ForbiddenResponse();

            try
            {
                var subscription = await _subscriptionService.RenewSubscriptionAsync(subscriptionId, request.BillingCycle);
                return SuccessResponse(subscription);
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResponse(ex.Message);
            }
        }

        [HttpGet("all")]
        [Authorize]
        public async Task<IActionResult> GetAllSubscriptions([FromQuery] SubscriptionStatus? status = null,
                                                             [FromQuery] SubscriptionPlan? plan = null,
                                                             [FromQuery] bool? isExpiringSoon = null,
                                                             [FromQuery] bool? isExpired = null)
        {
            if (!IsSuperAdmin)
                return ForbiddenResponse();

            var filter = new SubscriptionFilter(status, plan, isExpiringSoon, isExpired, null);
            var subscriptions = await _subscriptionService.GetAllSubscriptionsAsync(filter);

            return SuccessResponse(new
            {
                Count = subscriptions.Count,
                Subscriptions = subscriptions.Select(s => new
                {
                    s.Id,
                    s.SchoolId,
                    SchoolName = s.School?.Name,
                    s.Plan,
                    s.BillingCycle,
                    s.StartDate,
                    s.ExpiryDate,
                    s.Status,
                    s.CanAccess,
                    s.DaysRemaining,
                    s.Amount,
                    s.Currency,
                    s.MaxStudents,
                    s.MaxTeachers,
                    s.MaxStorageGB,
                    s.EnabledFeatures
                })
            });
        }

        [HttpGet("school/{schoolId}")]
        [Authorize]
        public async Task<IActionResult> GetSchoolSubscription(Guid schoolId)
        {
            if (!IsSuperAdmin)
                return ForbiddenResponse();

            var subscription = await _subscriptionService.GetSchoolSubscriptionAsync(schoolId);
            if (subscription == null)
                return NotFoundResponse("No subscription found for this school");

            return SuccessResponse(subscription);
        }

        [HttpGet("status")]
        [Authorize]
        public async Task<IActionResult> GetMySubscriptionStatus()
        {
            if (CurrentTenantId == null)
                return ErrorResponse("Invalid tenant");

            var subscription = await _subscriptionService.GetMySubscriptionAsync(CurrentTenantId.Value);
            if (subscription == null)
                return NotFoundResponse("No active subscription found");

            return SuccessResponse(new
            {
                subscription.Plan,
                subscription.BillingCycle,
                subscription.ExpiryDate,
                subscription.DaysRemaining,
                subscription.Status,
                subscription.CanAccess,
                subscription.IsInGracePeriod,
                subscription.MaxStudents,
                subscription.MaxTeachers,
                subscription.MaxStorageGB,
                EnabledFeatures = subscription.EnabledFeatures?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            });
        }

        public record SuspendRequest(string? Reason);
        public record RenewRequest(BillingCycle BillingCycle);
    }
}
