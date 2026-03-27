// Domain/Enums/LibraryFeeType.cs
namespace Devken.CBC.SchoolManagement.Domain.Enums
{
    public enum LibraryFeeType
    {
        MembershipFee,
        LateFine,
        DamageFee,
        LostBookFee,
        ProcessingFee,
        Other
    }

    public enum LibraryFeeStatus
    {
        Unpaid,
        Paid,
        Waived,
        PartiallyPaid
    }
}