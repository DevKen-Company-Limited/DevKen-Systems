using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Subscription
{
    public class Subscription : TenantBaseEntity<Guid>
    {
        public Guid SchoolId { get; set; }
        public School? School { get; set; }

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

        public new SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

        public int GracePeriodDays { get; set; } = 7;
        public string? AdminNotes { get; set; }

        // Helper properties
        public bool IsExpired => DateTime.UtcNow > ExpiryDate;

        public bool IsInGracePeriod => IsExpired && DateTime.UtcNow <= ExpiryDate.AddDays(GracePeriodDays);

        public bool CanAccess => Status == SubscriptionStatus.Active || IsInGracePeriod;

        public bool IsActive
        {
            get => Status == SubscriptionStatus.Active;
            set => Status = value ? SubscriptionStatus.Active : SubscriptionStatus.Cancelled;
        }

        public int DaysRemaining
        {
            get
            {
                var diff = ExpiryDate - DateTime.UtcNow;
                return diff.Days > 0 ? diff.Days : 0;
            }
        }
    }

}
