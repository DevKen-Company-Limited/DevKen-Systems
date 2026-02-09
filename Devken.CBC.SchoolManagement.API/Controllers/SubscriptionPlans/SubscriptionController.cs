using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Application.Service.Isubscription;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Entities.Subscription;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Devken.CBC.SchoolManagement.Application.DTOs.Subscription.SubscriptionPlans;

namespace Devken.CBC.SchoolManagement.API.Controllers.SubscriptionPlans
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionController : BaseApiController
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        /// <summary>
        /// Create a new subscription for a school
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
        {
            if (!IsSuperAdmin)
                return ForbiddenResponse();

            try
            {
                if (request.SchoolId == Guid.Empty)
                    return BadRequest("School ID is required");

                var subscription = await _subscriptionService.CreateSubscriptionAsync(request);

                var response = MapSubscriptionResponse(subscription);
                return SuccessResponse(response, "Subscription created successfully");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to create subscription: {ex.Message}");
            }
        }

        /// <summary>
        /// Update an existing subscription
        /// </summary>
        [HttpPut("{subscriptionId}")]
        [Authorize]
        public async Task<IActionResult> UpdateSubscription(Guid subscriptionId, [FromBody] UpdateSubscriptionRequest request)
        {
            if (!IsSuperAdmin)
                return ForbiddenResponse();

            try
            {
                var subscription = await _subscriptionService.UpdateSubscriptionAsync(subscriptionId, request);
                if (subscription == null)
                    return NotFoundResponse("Subscription not found");

                var response = MapSubscriptionResponse(subscription);
                return SuccessResponse(response, "Subscription updated successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to update subscription: {ex.Message}");
            }
        }

        /// <summary>
        /// Suspend a subscription
        /// </summary>
        [HttpPost("{subscriptionId}/suspend")]
        [Authorize]
        public async Task<IActionResult> SuspendSubscription(Guid subscriptionId, [FromBody] SuspendRequest request)
        {
            if (!IsSuperAdmin)
                return ForbiddenResponse();

            try
            {
                var success = await _subscriptionService.SuspendSubscriptionAsync(
                    subscriptionId,
                    request.Reason ?? "No reason provided"
                );

                if (!success)
                    return NotFoundResponse("Subscription not found");

                return SuccessResponse(success, "Subscription suspended successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to suspend subscription: {ex.Message}");
            }
        }

        /// <summary>
        /// Activate a suspended subscription
        /// </summary>
        [HttpPost("{subscriptionId}/activate")]
        [Authorize]
        public async Task<IActionResult> ActivateSubscription(Guid subscriptionId)
        {
            if (!IsSuperAdmin)
                return ForbiddenResponse();

            try
            {
                var success = await _subscriptionService.ActivateSubscriptionAsync(subscriptionId);
                if (!success)
                    return NotFoundResponse("Subscription not found");

                return SuccessResponse(success, "Subscription activated successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to activate subscription: {ex.Message}");
            }
        }

        /// <summary>
        /// Cancel a subscription
        /// </summary>
        [HttpPost("{subscriptionId}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelSubscription(Guid subscriptionId)
        {
            if (!IsSuperAdmin)
                return ForbiddenResponse();

            try
            {
                var success = await _subscriptionService.CancelSubscriptionAsync(subscriptionId);
                if (!success)
                    return NotFoundResponse("Subscription not found");

                return SuccessResponse(success, "Subscription cancelled successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to cancel subscription: {ex.Message}");
            }
        }

        /// <summary>
        /// Renew a subscription with a new billing cycle
        /// </summary>
        [HttpPost("{subscriptionId}/renew")]
        [Authorize]
        public async Task<IActionResult> RenewSubscription(Guid subscriptionId, [FromBody] RenewRequest request)
        {
            if (!IsSuperAdmin)
                return ForbiddenResponse();

            try
            {
                // Convert int to BillingCycle enum
                if (!Enum.IsDefined(typeof(BillingCycle), request.BillingCycle))
                {
                    return BadRequest($"Invalid billing cycle value: {request.BillingCycle}");
                }

                var billingCycle = (BillingCycle)request.BillingCycle;

                var subscription = await _subscriptionService.RenewSubscriptionAsync(
                    subscriptionId,
                    billingCycle
                );

                if (subscription == null)
                    return NotFoundResponse("Subscription not found");

                var response = MapSubscriptionResponse(subscription);
                return SuccessResponse(response, "Subscription renewed successfully");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to renew subscription: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all subscriptions with optional filtering
        /// </summary>
        [HttpGet("all")]
        [Authorize]
        public async Task<IActionResult> GetAllSubscriptions(
            [FromQuery] int? status = null,
            [FromQuery] int? plan = null,
            [FromQuery] bool? isExpiringSoon = null,
            [FromQuery] bool? isExpired = null)
        {
            if (!IsSuperAdmin)
                return ForbiddenResponse();

            try
            {
                var subscriptionStatus = status.HasValue ? (SubscriptionStatus)status.Value : (SubscriptionStatus?)null;
                var subscriptionPlan = plan.HasValue ? (SubscriptionPlan)plan.Value : (SubscriptionPlan?)null;

                var filter = new SubscriptionFilter(subscriptionStatus, subscriptionPlan, isExpiringSoon, isExpired, null);
                var subscriptions = await _subscriptionService.GetAllSubscriptionsAsync(filter);

                var subscriptionData = subscriptions
                    .Select(s => MapSubscriptionResponse(s))
                    .ToList();

                return SuccessResponse(subscriptionData, $"Retrieved {subscriptionData.Count} subscriptions");
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to retrieve subscriptions: {ex.Message}");
            }
        }

        /// <summary>
        /// Get subscription for a specific school
        /// </summary>
        [HttpGet("school/{schoolId}")]
        [Authorize]
        public async Task<IActionResult> GetSchoolSubscription(Guid schoolId)
        {
            if (!IsSuperAdmin)
                return ForbiddenResponse();

            try
            {
                var subscription = await _subscriptionService.GetSchoolSubscriptionAsync(schoolId);
                if (subscription == null)
                    return NotFoundResponse("No subscription found for this school");

                var response = MapSubscriptionResponse(subscription);
                return SuccessResponse(response, "Subscription retrieved successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to retrieve subscription: {ex.Message}");
            }
        }

        /// <summary>
        /// Get current user's subscription status
        /// </summary>
        [HttpGet("status")]
        [Authorize]
        public async Task<IActionResult> GetMySubscriptionStatus()
        {
            if (CurrentTenantId == null)
                return ErrorResponse("Invalid tenant");

            try
            {
                var subscription = await _subscriptionService.GetMySubscriptionAsync(CurrentTenantId.Value);
                if (subscription == null)
                    return NotFoundResponse("No active subscription found");

                var response = new
                {
                    subscription.Id,
                    Plan = subscription.Plan.ToString(),
                    BillingCycle = subscription.BillingCycle.ToString(),
                    subscription.ExpiryDate,
                    subscription.DaysRemaining,
                    Status = subscription.Status.ToString(),
                    subscription.CanAccess,
                    subscription.IsInGracePeriod,
                    subscription.MaxStudents,
                    subscription.MaxTeachers,
                    subscription.MaxStorageGB,
                    EnabledFeatures = subscription.EnabledFeatures?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                };

                return SuccessResponse(response, "Subscription status retrieved successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to retrieve subscription status: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper: Map subscription entity to response DTO
        /// </summary>
        private object MapSubscriptionResponse(Subscription subscription)
        {
            return new
            {
                subscription.Id,
                subscription.SchoolId,
                SchoolName = subscription.School?.Name,
                Plan = (int)subscription.Plan,
                PlanName = subscription.Plan.ToString(),
                BillingCycle = (int)subscription.BillingCycle,
                BillingCycleName = subscription.BillingCycle.ToString(),
                subscription.StartDate,
                subscription.ExpiryDate,
                Status = (int)subscription.Status,
                StatusName = subscription.Status.ToString(),
                subscription.CanAccess,
                subscription.DaysRemaining,
                subscription.Amount,
                subscription.Currency,
                subscription.MaxStudents,
                subscription.MaxTeachers,
                subscription.MaxStorageGB,
                subscription.EnabledFeatures,
                SuspensionReason = subscription.Status == SubscriptionStatus.Suspended ? subscription.AdminNotes : null,
                IsInGracePeriod = subscription.Status == SubscriptionStatus.GracePeriod,
                IsExpired = subscription.Status == SubscriptionStatus.Expired || subscription.DaysRemaining <= 0,
                IsActive = subscription.Status == SubscriptionStatus.Active && subscription.CanAccess
            };
        }

        // ================== Request/Response Models ==================

        /// <summary>
        /// Request model for suspending a subscription
        /// </summary>
        public record SuspendRequest(
            [property: JsonPropertyName("reason")]
            string? Reason
        );

        /// <summary>
        /// Request model for renewing a subscription
        /// The BillingCycle is sent as an integer from the frontend and converted to BillingCycle enum
        /// Valid values: 1 = Monthly, 3 = Quarterly, 4 = Yearly
        /// </summary>
        public record RenewRequest(
            [property: JsonPropertyName("billingCycle")]
            int BillingCycle
        );
    }
}