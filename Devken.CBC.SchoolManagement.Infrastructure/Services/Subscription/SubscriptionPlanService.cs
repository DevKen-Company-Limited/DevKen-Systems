// Application/Service/Subscription/SubscriptionPlanService.cs
using Devken.CBC.SchoolManagement.Application.Service.ISubscription;
using Devken.CBC.SchoolManagement.Domain.Entities.Subscription;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Subscription
{
    public class SubscriptionPlanService : ISubscriptionPlanService
    {
        private readonly AppDbContext _context;

        public SubscriptionPlanService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SubscriptionPlanEntity>> GetAllPlansAsync(bool activeOnly = true)
        {
            var query = _context.SubscriptionPlans.AsQueryable();

            if (activeOnly)
            {
                query = query.Where(p => p.IsActive && p.IsVisible);
            }

            return await query
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.MonthlyPrice)
                .ToListAsync();
        }

        public async Task<SubscriptionPlanEntity?> GetPlanByIdAsync(Guid planId)
        {
            return await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Id == planId);
        }

        public async Task<SubscriptionPlanEntity?> GetPlanByTypeAsync(SubscriptionPlan planType)
        {
            return await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.PlanType == planType && p.IsActive);
        }

        public async Task<SubscriptionPlanEntity> CreatePlanAsync(CreatePlanRequest request)
        {
            // Calculate discounted prices
            var quarterlyPrice = request.MonthlyPrice * 3 * (1 - request.QuarterlyDiscountPercent / 100);
            var yearlyPrice = request.MonthlyPrice * 12 * (1 - request.YearlyDiscountPercent / 100);

            var plan = new SubscriptionPlanEntity
            {
                Id = Guid.NewGuid(),
                PlanType = request.PlanType,
                Name = request.Name,
                Description = request.Description,
                MonthlyPrice = request.MonthlyPrice,
                QuarterlyPrice = quarterlyPrice,
                YearlyPrice = yearlyPrice,
                Currency = request.Currency,
                MaxStudents = request.MaxStudents,
                MaxTeachers = request.MaxTeachers,
                MaxStorageGB = request.MaxStorageGB,
                EnabledFeatures = string.Join(",", request.EnabledFeatures),
                FeatureList = JsonSerializer.Serialize(request.FeatureDescriptions),
                DisplayOrder = request.DisplayOrder,
                IsMostPopular = request.IsMostPopular,
                QuarterlyDiscountPercent = request.QuarterlyDiscountPercent,
                YearlyDiscountPercent = request.YearlyDiscountPercent,
                IsActive = true,
                IsVisible = true,
                CreatedOn = DateTime.UtcNow
            };

            _context.SubscriptionPlans.Add(plan);
            await _context.SaveChangesAsync();

            return plan;
        }

        public async Task<SubscriptionPlanEntity?> UpdatePlanAsync(Guid planId, UpdatePlanRequest request)
        {
            var plan = await GetPlanByIdAsync(planId);
            if (plan == null) return null;

            if (!string.IsNullOrEmpty(request.Name))
                plan.Name = request.Name;

            if (!string.IsNullOrEmpty(request.Description))
                plan.Description = request.Description;

            if (request.MonthlyPrice.HasValue)
            {
                plan.MonthlyPrice = request.MonthlyPrice.Value;

                // Recalculate quarterly and yearly prices
                var quarterlyDiscount = request.QuarterlyDiscountPercent ?? plan.QuarterlyDiscountPercent;
                var yearlyDiscount = request.YearlyDiscountPercent ?? plan.YearlyDiscountPercent;

                plan.QuarterlyPrice = plan.MonthlyPrice * 3 * (1 - quarterlyDiscount / 100);
                plan.YearlyPrice = plan.MonthlyPrice * 12 * (1 - yearlyDiscount / 100);
            }

            if (!string.IsNullOrEmpty(request.Currency))
                plan.Currency = request.Currency;

            if (request.MaxStudents.HasValue)
                plan.MaxStudents = request.MaxStudents.Value;

            if (request.MaxTeachers.HasValue)
                plan.MaxTeachers = request.MaxTeachers.Value;

            if (request.MaxStorageGB.HasValue)
                plan.MaxStorageGB = request.MaxStorageGB.Value;

            if (request.EnabledFeatures != null)
                plan.EnabledFeatures = string.Join(",", request.EnabledFeatures);

            if (request.FeatureDescriptions != null)
                plan.FeatureList = JsonSerializer.Serialize(request.FeatureDescriptions);

            if (request.DisplayOrder.HasValue)
                plan.DisplayOrder = request.DisplayOrder.Value;

            if (request.IsMostPopular.HasValue)
                plan.IsMostPopular = request.IsMostPopular.Value;

            if (request.IsActive.HasValue)
                plan.IsActive = request.IsActive.Value;

            if (request.IsVisible.HasValue)
                plan.IsVisible = request.IsVisible.Value;

            if (request.QuarterlyDiscountPercent.HasValue)
                plan.QuarterlyDiscountPercent = request.QuarterlyDiscountPercent.Value;

            if (request.YearlyDiscountPercent.HasValue)
                plan.YearlyDiscountPercent = request.YearlyDiscountPercent.Value;

            plan.UpdatedOn = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return plan;
        }

        public async Task<bool> DeletePlanAsync(Guid planId)
        {
            var plan = await GetPlanByIdAsync(planId);
            if (plan == null) return false;

            // Check if any subscriptions are using this plan
            var hasSubscriptions = await _context.Subscriptions
                .AnyAsync(s => s.PlanId == planId);

            if (hasSubscriptions)
            {
                // Soft delete - just mark as inactive
                plan.IsActive = false;
                plan.IsVisible = false;
            }
            else
            {
                // Hard delete if no subscriptions
                _context.SubscriptionPlans.Remove(plan);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TogglePlanVisibilityAsync(Guid planId, bool isVisible)
        {
            var plan = await GetPlanByIdAsync(planId);
            if (plan == null) return false;

            plan.IsVisible = isVisible;
            plan.UpdatedOn= DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<decimal> GetPriceForCycleAsync(Guid planId, BillingCycle cycle)
        {
            var plan = await GetPlanByIdAsync(planId);
            if (plan == null) return 0;

            return cycle switch
            {
                BillingCycle.Monthly => plan.MonthlyPrice,
                BillingCycle.Quarterly => plan.QuarterlyPrice,
                BillingCycle.Yearly => plan.YearlyPrice,
                _ => plan.MonthlyPrice
            };
        }
    }
}