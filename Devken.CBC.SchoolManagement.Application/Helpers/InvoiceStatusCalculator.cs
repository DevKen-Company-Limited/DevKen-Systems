// Application/Helpers/InvoiceStatusCalculator.cs
// ─────────────────────────────────────────────────────────────────────────────
// A pure static helper that computes new invoice figures from a list of
// completed payments.  Has no dependencies — unit-testable in isolation.
//
// Both InvoiceService AND PaymentService can call this without circular
// injection, which makes it cleaner than Option B (injecting IInvoiceService
// into PaymentService).
// ─────────────────────────────────────────────────────────────────────────────

using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Devken.CBC.SchoolManagement.Application.Helpers
{
    public static class InvoiceStatusCalculator
    {
        public record Result(
            decimal AmountPaid,
            decimal Balance,
            bool IsOverdue,
            InvoiceStatus Status);

        /// <summary>
        /// Given the invoice's TotalAmount, DueDate and the sum of all
        /// Completed non-reversal payments, returns the new derived fields.
        ///
        /// Status priority:
        ///   Balance == 0   → Paid
        ///   AmountPaid > 0 → PartiallyPaid
        ///   IsOverdue      → Overdue
        ///   otherwise      → Pending
        ///
        /// Cancelled and Refunded are terminal — pass currentStatus and they
        /// are returned unchanged.
        /// </summary>
        public static Result Compute(
            decimal totalAmount,
            DateTime dueDate,
            decimal completedPaymentsSum,
            InvoiceStatus currentStatus)
        {
            // Guard terminal states
            if (currentStatus is InvoiceStatus.Cancelled or InvoiceStatus.Refunded)
                return new Result(0, 0, false, currentStatus);

            decimal amountPaid = completedPaymentsSum;
            decimal balance = totalAmount - amountPaid;
            bool overdue = balance > 0 && dueDate.Date < DateTime.UtcNow.Date;

            InvoiceStatus newStatus =
                balance <= 0 ? InvoiceStatus.Paid :
                amountPaid > 0 ? InvoiceStatus.PartiallyPaid :
                overdue ? InvoiceStatus.Overdue :
                                  InvoiceStatus.Pending;

            return new Result(amountPaid, balance, overdue, newStatus);
        }
    }
}