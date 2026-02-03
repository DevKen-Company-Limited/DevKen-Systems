using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Enums
{
    public enum EntityStatus
    {
        Active = 1,
        Inactive = 2,
        Deleted = 3
    }

    public enum SubscriptionStatus
    {
        Active = 0,
        Expired = 1,
        Suspended = 2,
        Cancelled = 3,
        PendingPayment = 4
    }

    public enum SubscriptionPlan
    {
        Trial = 0,
        Basic = 1,
        Standard = 2,
        Premium = 3,
        Enterprise = 4
    }

    public enum BillingCycle
    {
        Daily = 0,
        Weekly = 1,
        Monthly = 2,
        Quarterly = 3,
        Yearly = 4,
        Custom = 5
    }
}

