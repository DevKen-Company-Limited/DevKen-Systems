using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Enums
{
    /// <summary>
    /// Gender options
    /// </summary>
    public enum Gender
    {
        Male = 1,
        Female = 2,
        Other = 3
    }

    /// <summary>
    /// Student status in the school
    /// </summary>
    public enum StudentStatus
    {
        Active = 1,
        Inactive = 2,
        Transferred = 3,
        Graduated = 4,
        Suspended = 5,
        Expelled = 6,
        Withdrawn = 7,
        Deceased = 8,
        Deleted = 9
    }

    /// <summary>
    /// CBC Levels as per Kenyan Curriculum
    /// </summary>
    public enum CBCLevel
    {
        // Pre-Primary
        PP1 = 1,
        PP2 = 2,

        // Lower Primary
        Grade1 = 3,
        Grade2 = 4,
        Grade3 = 5,

        // Upper Primary
        Grade4 = 6,
        Grade5 = 7,
        Grade6 = 8,

        // Junior Secondary
        Grade7 = 9,
        Grade8 = 10,
        Grade9 = 11,

        // Senior Secondary (for future expansion)
        Grade10 = 12,
        Grade11 = 13,
        Grade12 = 14
    }

    /// <summary>
    /// Academic term
    /// </summary>
    public enum TermType
    {
        Term1 = 1,
        Term2 = 2,
        Term3 = 3
    }

    /// <summary>
    /// Assessment types in CBC
    /// </summary>
    public enum AssessmentType
    {
        Formative = 1,
        Summative = 2,
        Competency = 3,
        Project = 4,
        Portfolio = 5
    }

    /// <summary>
    /// Competency levels in CBC
    /// </summary>
    public enum CompetencyLevel
    {
        Exceeding = 1,      // Above expectations
        Meeting = 2,        // Meeting expectations
        Approaching = 3,    // Developing
        Below = 4           // Needs support
    }

    /// <summary>
    /// Entity status for soft delete (consolidated version)
    /// </summary>
    public enum EntityStatus
    {
        Active = 1,
        Inactive = 2,
        Deleted = 3,
        Archived = 4,
        Suspended = 5
    }

    /// <summary>
    /// Payment status
    /// </summary>
    public enum PaymentStatus
    {
        Pending = 1,
        Partial = 2,
        Paid = 3,
        Overdue = 4,
        Cancelled = 5,
        Refunded = 6
    }

    /// <summary>
    /// Subscription status for school accounts
    /// </summary>
    public enum SubscriptionStatus
    {
        Active = 0,
        Expired = 1,
        Suspended = 2,
        Cancelled = 3,
        PendingPayment = 4
    }

    /// <summary>
    /// Subscription plans for schools
    /// </summary>
    public enum SubscriptionPlan
    {
        Trial = 0,
        Basic = 1,
        Standard = 2,
        Premium = 3,
        Enterprise = 4
    }

    /// <summary>
    /// Billing cycles for subscriptions
    /// </summary>
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