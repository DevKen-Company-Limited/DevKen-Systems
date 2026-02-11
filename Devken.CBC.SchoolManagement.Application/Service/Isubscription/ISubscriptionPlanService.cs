// Application/Service/ISubscription/ISubscriptionPlanService.cs
using Devken.CBC.SchoolManagement.Domain.Entities.Subscription;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.ISubscription
{
    public interface ISubscriptionPlanService
    {
        Task<List<SubscriptionPlanEntity>> GetAllPlansAsync(bool activeOnly = true);
        Task<SubscriptionPlanEntity?> GetPlanByIdAsync(Guid planId);
        Task<SubscriptionPlanEntity?> GetPlanByTypeAsync(SubscriptionPlan planType);
        Task<SubscriptionPlanEntity> CreatePlanAsync(CreatePlanRequest request);
        Task<SubscriptionPlanEntity?> UpdatePlanAsync(Guid planId, UpdatePlanRequest request);
        Task<bool> DeletePlanAsync(Guid planId);
        Task<bool> TogglePlanVisibilityAsync(Guid planId, bool isVisible);
        Task<decimal> GetPriceForCycleAsync(Guid planId, BillingCycle cycle);
    }

    public record CreatePlanRequest(
        SubscriptionPlan PlanType,
        string Name,
        string Description,
        decimal MonthlyPrice,
        string Currency,
        int MaxStudents,
        int MaxTeachers,
        decimal MaxStorageGB,
        string[] EnabledFeatures,
        string[] FeatureDescriptions,
        int DisplayOrder = 0,
        bool IsMostPopular = false,
        decimal QuarterlyDiscountPercent = 10,
        decimal YearlyDiscountPercent = 20
    );

    public record UpdatePlanRequest(
        string? Name,
        string? Description,
        decimal? MonthlyPrice,
        string? Currency,
        int? MaxStudents,
        int? MaxTeachers,
        decimal? MaxStorageGB,
        string[]? EnabledFeatures,
        string[]? FeatureDescriptions,
        int? DisplayOrder,
        bool? IsMostPopular,
        bool? IsActive,
        bool? IsVisible,
        decimal? QuarterlyDiscountPercent,
        decimal? YearlyDiscountPercent
    );
}