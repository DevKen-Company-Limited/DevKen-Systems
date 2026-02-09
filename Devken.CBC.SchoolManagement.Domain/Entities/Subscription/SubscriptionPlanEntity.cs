// Domain/Entities/Subscription/SubscriptionPlanEntity.cs
using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Subscription
{
    /// <summary>
    /// Represents a subscription plan template with pricing and features
    /// </summary>
    public class SubscriptionPlanEntity : BaseEntity<Guid>
    {
        public SubscriptionPlan PlanType { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal MonthlyPrice { get; set; }

        public decimal QuarterlyPrice { get; set; }

        public decimal YearlyPrice { get; set; }

        public string Currency { get; set; } = "USD";

        public int MaxStudents { get; set; }

        public int MaxTeachers { get; set; }

        public decimal MaxStorageGB { get; set; }

        /// <summary>
        /// Comma-separated list of enabled features
        /// </summary>
        public string EnabledFeatures { get; set; } = string.Empty;

        /// <summary>
        /// JSON array of feature descriptions for display
        /// </summary>
        public string FeatureList { get; set; } = "[]";

        public bool IsActive { get; set; } = true;

        public bool IsVisible { get; set; } = true;

        public int DisplayOrder { get; set; }

        public bool IsMostPopular { get; set; }

        /// <summary>
        /// Discount percentage for quarterly billing
        /// </summary>
        public decimal QuarterlyDiscountPercent { get; set; } = 10;

        /// <summary>
        /// Discount percentage for yearly billing
        /// </summary>
        public decimal YearlyDiscountPercent { get; set; } = 20;

        // Navigation property
        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    }
}