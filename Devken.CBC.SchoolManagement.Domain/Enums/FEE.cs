using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Enums
{
    // ─────────────────────────────────────────────
    // FEE ENUMS
    // ─────────────────────────────────────────────

    public enum FeeType
    {
        Tuition = 0,
        Activity = 1,
        Examination = 2,
        Uniform = 3,
        Transport = 4,
        Boarding = 5,
        Library = 6,
        Technology = 7,
        Development = 8,
        Registration = 9,
        Caution = 10,  // Refundable deposit
        Other = 99
    }

    public enum RecurrenceType
    {
        None = 0,
        Monthly = 1,
        Termly = 2,
        Annually = 3
    }

    public enum ApplicableTo
    {
        All = 0,
        Day = 1,
        Boarding = 2,
        Special = 3
    }

    // ─────────────────────────────────────────────
    // INVOICE ENUMS
    // ─────────────────────────────────────────────

    public enum InvoiceStatus
    {
        Draft = 0,
        Pending = 1,
        PartiallyPaid = 2,
        Paid = 3,
        Overdue = 4,
        Cancelled = 5,
        Refunded = 6
    }

    // ─────────────────────────────────────────────
    // PAYMENT ENUMS
    // ─────────────────────────────────────────────

    public enum PaymentStatus
    {
        Pending = 0,   // ✅ Consistent start at 0
        Completed = 1,
        Failed = 2,
        Refunded = 3,
        Cancelled = 4,
        Reversed = 5
    }

    public enum PaymentMethod
    {
        Cash = 0,
        Mpesa = 1,
        BankTransfer = 2,
        Cheque = 3,
        CreditCard = 4,
        OnlinePortal = 5,
        DirectDebit = 6
    }

    // ─────────────────────────────────────────────
    // DISCOUNT ENUMS
    // ─────────────────────────────────────────────

    public enum DiscountType
    {
        Percentage = 0,
        FixedAmount = 1
    }

    public enum DiscountReason
    {
        Scholarship = 0,
        Bursary = 1,
        SiblingRate = 2,
        StaffChild = 3,
        EarlyBird = 4,
        Government = 5,
        Sponsored = 6,
        Other = 99
    }

    // ─────────────────────────────────────────────
    // PAYMENT PLAN ENUMS
    // ─────────────────────────────────────────────

    public enum InstallmentStatus
    {
        Pending = 0,
        PartiallyPaid = 1,
        Paid = 2,
        Overdue = 3,
        Waived = 4
    }

    // ─────────────────────────────────────────────
    // CREDIT NOTE ENUMS
    // ─────────────────────────────────────────────

    public enum CreditNoteStatus
    {
        Issued = 0,
        Applied = 1,
        Voided = 2
    }

    // ─────────────────────────────────────────────
    // ACCOUNTING ENUMS
    // ─────────────────────────────────────────────

    public enum AccountType
    {
        Asset = 0,
        Liability = 1,
        Equity = 2,
        Revenue = 3,
        Expense = 4
    }

    public enum AccountSubType
    {
        // Assets
        CurrentAsset = 0,
        FixedAsset = 1,
        BankAccount = 2,
        CashAccount = 3,
        AccountReceivable = 4,
        // Liabilities
        CurrentLiability = 10,
        LongTermLiability = 11,
        AccountPayable = 12,
        // Equity
        RetainedEarnings = 20,
        Capital = 21,
        // Revenue
        FeeIncome = 30,
        OtherIncome = 31,
        // Expense
        StaffExpense = 40,
        OperatingExpense = 41,
        CapitalExpense = 42
    }

    public enum JournalEntryType
    {
        Manual = 0,
        Payment = 1,
        Invoice = 2,
        CreditNote = 3,
        Adjustment = 4,
        Opening = 5,
        Closing = 6
    }

    public enum JournalEntryStatus
    {
        Draft = 0,
        Posted = 1,
        Reversed = 2,
        Voided = 3
    }

    public enum DebitCredit
    {
        Debit = 0,
        Credit = 1
    }

    public enum AccountingPeriodStatus
    {
        Open = 0,
        Closed = 1,
        Locked = 2
    }

    public enum BudgetStatus
    {
        Draft = 0,
        Active = 1,
        Closed = 2,
        Revised = 3
    }

    public enum ExpenseStatus
    {
        Draft = 0,
        Submitted = 1,
        Approved = 2,
        Rejected = 3,
        Paid = 4
    }

    public enum FundTransferStatus
    {
        Pending = 0,
        Completed = 1,
        Reversed = 2
    }
}
