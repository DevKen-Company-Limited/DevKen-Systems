// Infrastructure/Seed/SubscriptionPlanSeeder.cs
using Devken.CBC.SchoolManagement.Application.Service.ISubscription;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using static Devken.CBC.SchoolManagement.Application.Service.ISubscription.ISubscriptionPlanService;

namespace Devken.CBC.SchoolManagement.Infrastructure.Seed
{
    public static class SubscriptionPlanSeeder
    {
        public static async Task SeedSubscriptionPlansAsync(ISubscriptionPlanService planService, ILogger logger)
        {
            logger.LogInformation("Starting subscription plan seeding...");

            var plans = new[]
            {
                new CreatePlanRequest(
                    PlanType: SubscriptionPlan.Trial,
                    Name: "Trial",
                    Description: "Try our platform for free",
                    MonthlyPrice: 0m,
                    Currency: "USD",
                    MaxStudents: 50,
                    MaxTeachers: 5,
                    MaxStorageGB: 1,
                    EnabledFeatures: new[] { "basic_features" },
                    FeatureDescriptions: new[]
                    {
                        "Up to 50 students",
                        "Up to 5 teachers",
                        "1GB storage",
                        "14-day trial period",
                        "Basic reporting"
                    },
                    DisplayOrder: 0,
                    IsMostPopular: false,
                    QuarterlyDiscountPercent: 0,
                    YearlyDiscountPercent: 0
                ),
                new CreatePlanRequest(
                    PlanType: SubscriptionPlan.Basic,
                    Name: "Basic",
                    Description: "Perfect for small schools getting started",
                    MonthlyPrice: 49m,
                    Currency: "USD",
                    MaxStudents: 100,
                    MaxTeachers: 10,
                    MaxStorageGB: 5,
                    EnabledFeatures: new[] { "basic_features", "email_support" },
                    FeatureDescriptions: new[]
                    {
                        "Up to 100 students",
                        "Up to 10 teachers",
                        "5GB storage",
                        "Email support",
                        "Basic reporting",
                        "Student management"
                    },
                    DisplayOrder: 1,
                    IsMostPopular: false,
                    QuarterlyDiscountPercent: 10,
                    YearlyDiscountPercent: 20
                ),
                new CreatePlanRequest(
                    PlanType: SubscriptionPlan.Standard,
                    Name: "Standard",
                    Description: "Most popular for growing schools",
                    MonthlyPrice: 99m,
                    Currency: "USD",
                    MaxStudents: 500,
                    MaxTeachers: 50,
                    MaxStorageGB: 20,
                    EnabledFeatures: new[]
                    {
                        "basic_features",
                        "advanced_analytics",
                        "priority_support",
                        "api_access"
                    },
                    FeatureDescriptions: new[]
                    {
                        "Up to 500 students",
                        "Up to 50 teachers",
                        "20GB storage",
                        "Advanced analytics",
                        "Priority support",
                        "API access",
                        "Custom reports",
                        "Bulk operations"
                    },
                    DisplayOrder: 2,
                    IsMostPopular: true,
                    QuarterlyDiscountPercent: 10,
                    YearlyDiscountPercent: 20
                ),
                new CreatePlanRequest(
                    PlanType: SubscriptionPlan.Premium,
                    Name: "Premium",
                    Description: "Complete solution for large institutions",
                    MonthlyPrice: 199m,
                    Currency: "USD",
                    MaxStudents: 9999,
                    MaxTeachers: 999,
                    MaxStorageGB: 100,
                    EnabledFeatures: new[]
                    {
                        "all_features",
                        "custom_integrations",
                        "24x7_support",
                        "dedicated_account_manager"
                    },
                    FeatureDescriptions: new[]
                    {
                        "Unlimited students",
                        "Unlimited teachers",
                        "100GB storage",
                        "All features",
                        "24/7 support",
                        "Custom integrations",
                        "White-label option",
                        "Dedicated account manager",
                        "Advanced security"
                    },
                    DisplayOrder: 3,
                    IsMostPopular: false,
                    QuarterlyDiscountPercent: 10,
                    YearlyDiscountPercent: 20
                ),
                new CreatePlanRequest(
                    PlanType: SubscriptionPlan.Enterprise,
                    Name: "Enterprise",
                    Description: "Custom solution for large organizations",
                    MonthlyPrice: 499m,
                    Currency: "USD",
                    MaxStudents: 99999,
                    MaxTeachers: 9999,
                    MaxStorageGB: 500,
                    EnabledFeatures: new[]
                    {
                        "all_features",
                        "custom_integrations",
                        "24x7_support",
                        "dedicated_account_manager",
                        "white_label",
                        "sla",
                        "on_premise_option"
                    },
                    FeatureDescriptions: new[]
                    {
                        "Unlimited students",
                        "Unlimited teachers",
                        "500GB+ storage",
                        "All features",
                        "24/7 priority support",
                        "Custom integrations",
                        "White-label solution",
                        "Dedicated account manager",
                        "SLA guarantee",
                        "On-premise deployment option",
                        "Custom training"
                    },
                    DisplayOrder: 4,
                    IsMostPopular: false,
                    QuarterlyDiscountPercent: 15,
                    YearlyDiscountPercent: 25
                )
            };

            int seededCount = 0;
            int skippedCount = 0;

            foreach (var planRequest in plans)
            {
                try
                {
                    var existing = await planService.GetPlanByTypeAsync(planRequest.PlanType);
                    if (existing == null)
                    {
                        await planService.CreatePlanAsync(planRequest);
                        logger.LogInformation("✓ Seeded plan: {PlanName} ({PlanType})", planRequest.Name, planRequest.PlanType);
                        seededCount++;
                    }
                    else
                    {
                        logger.LogInformation("○ Plan already exists: {PlanName} ({PlanType})", planRequest.Name, planRequest.PlanType);
                        skippedCount++;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "✗ Failed to seed plan: {PlanName} ({PlanType})", planRequest.Name, planRequest.PlanType);
                }
            }

            logger.LogInformation("Subscription plan seeding completed. Seeded: {SeededCount}, Skipped: {SkippedCount}", seededCount, skippedCount);
        }
    }
}