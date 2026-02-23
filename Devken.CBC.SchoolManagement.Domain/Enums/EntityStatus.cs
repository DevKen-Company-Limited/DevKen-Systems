using System;
using System.ComponentModel;

namespace Devken.CBC.SchoolManagement.Domain.Enums
{
    /// <summary>
    /// Gender options
    /// </summary>
    /// 


    public enum SchoolType
    {
        [Description("Public School (Government Funded)")]
        Public = 1,

        [Description("Private School")]
        Private = 2,

        [Description("International Curriculum School")]
        International = 3,

        [Description("NGO / Mission Sponsored School")]
        NGO = 4
    }

    public enum SchoolCategory
    {
        [Description("Day School")]
        Day = 1,

        [Description("Boarding School")]
        Boarding = 2,

        [Description("Mixed (Day & Boarding)")]
        Mixed = 3
    }

    public enum SubjectType
    {
        [Description("Core Subject (Compulsory for all learners)")]
        Core = 1,

        [Description("Optional Subject (School-based choice)")]
        Optional = 2,

        [Description("Elective Subject (Learner selected subject)")]
        Elective = 3,

        [Description("Co-Curricular Activity (Non-examinable activity)")]
        CoCurricular = 4
    }



    public enum Gender
    {
        [Description("Male")]
        Male = 1,

        [Description("Female")]
        Female = 2,

        [Description("Other / Prefer not to say")]
        Other = 3
    }

    /// <summary>
    /// Student status in the school
    /// </summary>
    public enum StudentStatus
    {
        [Description("Currently active in school")]
        Active = 1,

        [Description("Inactive but not removed")]
        Inactive = 2,

        [Description("Transferred to another school")]
        Transferred = 3,

        [Description("Completed final grade and graduated")]
        Graduated = 4,

        [Description("Temporarily suspended")]
        Suspended = 5,

        [Description("Permanently expelled")]
        Expelled = 6,

        [Description("Voluntarily withdrawn")]
        Withdrawn = 7,

        [Description("Marked as deceased")]
        Deceased = 8,

        [Description("Soft deleted record")]
        Deleted = 9
    }

    /// <summary>
    /// CBC Levels as per Kenyan Curriculum
    /// </summary>
    public enum CBCLevel
    {
        [Description("Pre-Primary 1")]
        PP1 = 1,

        [Description("Pre-Primary 2")]
        PP2 = 2,

        [Description("Grade 1 - Lower Primary")]
        Grade1 = 3,

        [Description("Grade 2 - Lower Primary")]
        Grade2 = 4,

        [Description("Grade 3 - Lower Primary")]
        Grade3 = 5,

        [Description("Grade 4 - Upper Primary")]
        Grade4 = 6,

        [Description("Grade 5 - Upper Primary")]
        Grade5 = 7,

        [Description("Grade 6 - Upper Primary")]
        Grade6 = 8,

        [Description("Grade 7 - Junior Secondary")]
        Grade7 = 9,

        [Description("Grade 8 - Junior Secondary")]
        Grade8 = 10,

        [Description("Grade 9 - Junior Secondary")]
        Grade9 = 11,

        [Description("Grade 10 - Senior Secondary")]
        Grade10 = 12,

        [Description("Grade 11 - Senior Secondary")]
        Grade11 = 13,

        [Description("Grade 12 - Senior Secondary")]
        Grade12 = 14
    }

    /// <summary>
    /// Academic term
    /// </summary>
    public enum TermType
    {
        [Description("Term 1 (January - April)")]
        Term1 = 1,

        [Description("Term 2 (May - August)")]
        Term2 = 2,

        [Description("Term 3 (September - December)")]
        Term3 = 3
    }

    /// <summary>
    /// Assessment types in CBC
    /// </summary>
    public enum AssessmentType
    {
        [Description("Continuous classroom assessment")]
        Formative = 1,

        [Description("End of term examination")]
        Summative = 2,

        [Description("Competency-based evaluation")]
        Competency = 3,

        [Description("Project-based assessment")]
        Project = 4,

        [Description("Portfolio evaluation")]
        Portfolio = 5
    }

    /// <summary>
    /// Competency levels in CBC
    /// </summary>
    public enum CompetencyLevel
    {
        [Description("Exceeding expectations")]
        Exceeding = 1,

        [Description("Meeting expectations")]
        Meeting = 2,

        [Description("Approaching expectations")]
        Approaching = 3,

        [Description("Below expectations - Needs support")]
        Below = 4
    }

    /// <summary>
    /// Entity status for soft delete
    /// </summary>
    public enum EntityStatus
    {
        [Description("Active record")]
        Active = 1,

        [Description("Inactive record")]
        Inactive = 2,

        [Description("Soft deleted record")]
        Deleted = 3,

        [Description("Archived record")]
        Archived = 4,

        [Description("Temporarily suspended")]
        Suspended = 5
    }
    #region Supporting Enums

    public enum EmploymentType
    {
        Permanent,
        Contract,
        Probation
    }

    public enum Designation
    {
        Teacher,
        SeniorTeacher,
        DeputyHeadTeacher,
        HeadTeacher,
        Coordinator
    }

    #endregion

    /// <summary>
    /// General Payment status
    /// </summary>
    public enum PaymentStatus
    {
        [Description("Awaiting payment")]
        Pending = 1,

        [Description("Partially paid")]
        Partial = 2,

        [Description("Fully paid")]
        Paid = 3,

        [Description("Payment overdue")]
        Overdue = 4,

        [Description("Payment cancelled")]
        Cancelled = 5,

        [Description("Payment refunded")]
        Refunded = 6
    }

    /// <summary>
    /// Subscription status for school accounts
    /// </summary>
    public enum SubscriptionStatus
    {
        [Description("Subscription is active and fully accessible")]
        Active = 0,

        [Description("Subscription has expired and access is blocked")]
        Expired = 1,

        [Description("Subscription is temporarily suspended by administrator")]
        Suspended = 2,

        [Description("Subscription cancelled and will not renew")]
        Cancelled = 3,

        [Description("Subscription awaiting payment confirmation")]
        PendingPayment = 4,

        [Description("Expired but within grace period")]
        GracePeriod = 5
    }

    /// <summary>
    /// Subscription plans for schools
    /// </summary>
    public enum SubscriptionPlan
    {
        [Description("Free trial plan")]
        Trial = 0,

        [Description("Basic plan")]
        Basic = 1,

        [Description("Standard plan")]
        Standard = 2,

        [Description("Premium plan")]
        Premium = 3,

        [Description("Enterprise plan")]
        Enterprise = 4
    }

    /// <summary>
    /// Billing cycles
    /// </summary>
    public enum BillingCycle
    {
        [Description("Daily billing")]
        Daily = 0,

        [Description("Weekly billing")]
        Weekly = 1,

        [Description("Monthly billing")]
        Monthly = 2,

        [Description("Quarterly billing")]
        Quarterly = 3,

        [Description("Yearly billing")]
        Yearly = 4,

        [Description("Custom billing period")]
        Custom = 5
    }

    /// <summary>
    /// Religious affiliations for students
    /// </summary>
    public enum Religion
    {
        [Description("Christianity")]
        Christianity = 1,

        [Description("Islam")]
        Islam = 2,

        [Description("Hinduism")]
        Hinduism = 3,

        [Description("Buddhism")]
        Buddhism = 4,

        [Description("Sikhism")]
        Sikhism = 5,

        [Description("Judaism")]
        Judaism = 6,

        [Description("Traditional African Religion")]
        TraditionalAfrican = 7,

        [Description("Other")]
        Other = 8,

        [Description("Prefer not to say")]
        PreferNotToSay = 9
    }

    /// <summary>
    /// Nationalities for students and staff
    /// Primary focus on East African Community countries
    /// </summary>
    public enum Nationality
    {
        [Description("Kenyan")]
        Kenyan = 1,

        [Description("Ugandan")]
        Ugandan = 2,

        [Description("Tanzanian")]
        Tanzanian = 3,

        [Description("Rwandan")]
        Rwandan = 4,

        [Description("Burundian")]
        Burundian = 5,

        [Description("South Sudanese")]
        SouthSudanese = 6,

        [Description("Somali")]
        Somali = 7,

        [Description("Ethiopian")]
        Ethiopian = 8,

        [Description("Congolese (DRC)")]
        Congolese = 9,

        [Description("Nigerian")]
        Nigerian = 10,

        [Description("Ghanaian")]
        Ghanaian = 11,

        [Description("South African")]
        SouthAfrican = 12,

        [Description("Eritrean")]
        Eritrean = 13,

        [Description("Sudanese")]
        Sudanese = 14,

        [Description("Other African")]
        OtherAfrican = 15,

        [Description("British")]
        British = 16,

        [Description("American")]
        American = 17,

        [Description("Canadian")]
        Canadian = 18,

        [Description("Indian")]
        Indian = 19,

        [Description("Pakistani")]
        Pakistani = 20,

        [Description("Chinese")]
        Chinese = 21,

        [Description("Other")]
        Other = 22
    }
    /// <summary>
    /// Academic grading letters
    /// </summary>
    public enum GradeLetter
    {
        [Description("Excellent")]
        A = 1,

        [Description("Very Good")]
        B = 2,

        [Description("Good")]
        C = 3,

        [Description("Satisfactory")]
        D = 4,

        [Description("Needs Improvement")]
        E = 5,

        [Description("Fail")]
        F = 6
    }

    /// <summary>
    /// Mpesa Result Codes
    /// </summary>
    public enum MpesaResultCode
    {
        [Description("General failure")]
        Failed = -1,

        [Description("Transaction successful")]
        Success = 0,

        [Description("Insufficient funds")]
        InsufficientFunds = 1,

        [Description("Amount below minimum allowed")]
        LessThanMinimum = 2,

        [Description("Amount exceeds maximum allowed")]
        MoreThanMaximum = 3,

        [Description("Exceeds balance limit")]
        ExceedsBalanceLimit = 4,

        [Description("Transaction timed out")]
        Timeout = 1037,

        [Description("Cancelled by user")]
        CancelledByUser = 1032
    }
}
