// API/Controllers/SubscriptionPlansController.cs
using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.Service.ISubscription;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using static Devken.CBC.SchoolManagement.Application.Service.ISubscription.ISubscriptionPlanService;

namespace Devken.CBC.SchoolManagement.API.Controllers.SubscriptionPlans
{
    [Route("api/subscription-plans")]
    public class SubscriptionPlansController : BaseApiController
    {
        private readonly ISubscriptionPlanService _planService;

        public SubscriptionPlansController(ISubscriptionPlanService planService)
        {
            _planService = planService;
        }

        /// <summary>
        /// Get all available subscription plans
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllPlans([FromQuery] bool includeInactive = false)
        {
            try
            {
                var plans = await _planService.GetAllPlansAsync(!includeInactive);

                var planData = plans.Select(p => new
                {
                    p.Id,
                    PlanType = p.PlanType.ToString().ToLower(),
                    p.Name,
                    p.Description,
                    p.MonthlyPrice,
                    p.QuarterlyPrice,
                    p.YearlyPrice,
                    p.Currency,
                    p.MaxStudents,
                    p.MaxTeachers,
                    p.MaxStorageGB,
                    Features = JsonSerializer.Deserialize<string[]>(p.FeatureList),
                    EnabledFeatures = p.EnabledFeatures.Split(',', StringSplitOptions.RemoveEmptyEntries),
                    p.DisplayOrder,
                    p.IsMostPopular,
                    p.IsActive,
                    p.QuarterlyDiscountPercent,
                    p.YearlyDiscountPercent,
                    QuarterlyDiscountText = $"{p.QuarterlyDiscountPercent}% off",
                    YearlyDiscountText = $"{p.YearlyDiscountPercent}% off"
                }).ToList();

                return SuccessResponse(planData);
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to retrieve plans: {ex.Message}");
            }
        }

        /// <summary>
        /// Get a specific plan by ID
        /// </summary>
        [HttpGet("{planId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPlan(Guid planId)
        {
            try
            {
                var plan = await _planService.GetPlanByIdAsync(planId);
                if (plan == null)
                    return NotFoundResponse("Plan not found");

                var planData = new
                {
                    plan.Id,
                    PlanType = plan.PlanType.ToString().ToLower(),
                    plan.Name,
                    plan.Description,
                    plan.MonthlyPrice,
                    plan.QuarterlyPrice,
                    plan.YearlyPrice,
                    plan.Currency,
                    plan.MaxStudents,
                    plan.MaxTeachers,
                    plan.MaxStorageGB,
                    Features = JsonSerializer.Deserialize<string[]>(plan.FeatureList),
                    EnabledFeatures = plan.EnabledFeatures.Split(',', StringSplitOptions.RemoveEmptyEntries),
                    plan.DisplayOrder,
                    plan.IsMostPopular,
                    plan.IsActive,
                    plan.QuarterlyDiscountPercent,
                    plan.YearlyDiscountPercent
                };

                return SuccessResponse(planData);
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to retrieve plan: {ex.Message}");
            }
        }

        /// <summary>
        /// Get plan by type
        /// </summary>
        [HttpGet("type/{planType}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPlanByType(string planType)
        {
            try
            {
                if (!Enum.TryParse<SubscriptionPlan>(planType, true, out var type))
                    return ErrorResponse("Invalid plan type");

                var plan = await _planService.GetPlanByTypeAsync(type);
                if (plan == null)
                    return NotFoundResponse("Plan not found");

                var planData = new
                {
                    plan.Id,
                    PlanType = plan.PlanType.ToString().ToLower(),
                    plan.Name,
                    plan.Description,
                    plan.MonthlyPrice,
                    plan.QuarterlyPrice,
                    plan.YearlyPrice,
                    plan.Currency,
                    plan.MaxStudents,
                    plan.MaxTeachers,
                    plan.MaxStorageGB,
                    Features = JsonSerializer.Deserialize<string[]>(plan.FeatureList),
                    EnabledFeatures = plan.EnabledFeatures.Split(',', StringSplitOptions.RemoveEmptyEntries),
                    plan.DisplayOrder,
                    plan.IsMostPopular
                };

                return SuccessResponse(planData);
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to retrieve plan: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a new subscription plan (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePlan([FromBody] CreatePlanRequest request)
        {
            if (!IsSuperAdmin)
                return ForbiddenResponse();

            try
            {
                var plan = await _planService.CreatePlanAsync(request);
                return SuccessResponse(plan, "Plan created successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to create plan: {ex.Message}");
            }
        }

        /// <summary>
        /// Update an existing plan (Admin only)
        /// </summary>
        [HttpPut("{planId}")]
        [Authorize]
        public async Task<IActionResult> UpdatePlan(Guid planId, [FromBody] UpdatePlanRequest request)
        {
            if (!IsSuperAdmin)
                return ForbiddenResponse();

            try
            {
                var plan = await _planService.UpdatePlanAsync(planId, request);
                if (plan == null)
                    return NotFoundResponse("Plan not found");

                return SuccessResponse(plan, "Plan updated successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to update plan: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete a plan (Admin only)
        /// </summary>
        [HttpDelete("{planId}")]
        [Authorize]
        public async Task<IActionResult> DeletePlan(Guid planId)
        {
            if (!IsSuperAdmin)
                return ForbiddenResponse();

            try
            {
                var success = await _planService.DeletePlanAsync(planId);
                if (!success)
                    return NotFoundResponse("Plan not found");

                return SuccessResponse(true, "Plan deleted successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to delete plan: {ex.Message}");
            }
        }

        /// <summary>
        /// Toggle plan visibility (Admin only)
        /// </summary>
        [HttpPatch("{planId}/visibility")]
        [Authorize]
        public async Task<IActionResult> ToggleVisibility(Guid planId, [FromBody] ToggleVisibilityRequest request)
        {
            if (!IsSuperAdmin)
                return ForbiddenResponse();

            try
            {
                var success = await _planService.TogglePlanVisibilityAsync(planId, request.IsVisible);
                if (!success)
                    return NotFoundResponse("Plan not found");

                return SuccessResponse(true, "Plan visibility updated successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to update visibility: {ex.Message}");
            }
        }

        public record ToggleVisibilityRequest(bool IsVisible);
    }
}