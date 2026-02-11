// Domain/Entities/Subscription/Subscription.cs
using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Subscription
{
    public class Subscription : TenantBaseEntity<Guid>
    {
        public Guid SchoolId { get; set; }
        public School? School { get; set; }

        // Link to plan template
        public Guid? PlanId { get; set; }
        public SubscriptionPlanEntity? PlanTemplate { get; set; }

        public SubscriptionPlan Plan { get; set; }
        public BillingCycle BillingCycle { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime ExpiryDate { get; set; }

        public bool AutoRenew { get; set; }

        public int MaxStudents { get; set; }
        public int MaxTeachers { get; set; }
        public decimal MaxStorageGB { get; set; }

        public string? EnabledFeatures { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "KES";

        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

        public int GracePeriodDays { get; set; } = 7;

        public string? AdminNotes { get; set; }

        /* ===================== Computed Properties ===================== */

        public bool IsExpired => DateTime.UtcNow > ExpiryDate;

        public bool IsInGracePeriod =>
            IsExpired &&
            DateTime.UtcNow <= ExpiryDate.AddDays(GracePeriodDays);

        public bool CanAccess =>
            Status == SubscriptionStatus.Active ||
            Status == SubscriptionStatus.GracePeriod;

        public bool IsActive
        {
            get => Status == SubscriptionStatus.Active;
            set => Status = value
                ? SubscriptionStatus.Active
                : SubscriptionStatus.Cancelled;
        }

        public int DaysRemaining
        {
            get
            {
                var remaining = (ExpiryDate - DateTime.UtcNow).Days;
                return remaining > 0 ? remaining : 0;
            }
        }

        public void RefreshStatus()
        {
            if (Status == SubscriptionStatus.Cancelled ||
                Status == SubscriptionStatus.Suspended)
                return;

            if (!IsExpired)
            {
                Status = SubscriptionStatus.Active;
                return;
            }

            Status = IsInGracePeriod
                ? SubscriptionStatus.GracePeriod
                : SubscriptionStatus.Expired;
        }
    }
}