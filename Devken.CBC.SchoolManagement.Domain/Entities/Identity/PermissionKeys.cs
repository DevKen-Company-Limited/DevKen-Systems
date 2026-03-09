using System;
using System.Collections.Generic;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Identity
{
    public static class PermissionKeys
    {
        public const string SuperAdmin = "SuperAdmin";

        public const string SchoolRead = "School.Read";
        public const string SchoolWrite = "School.Write";
        public const string SchoolDelete = "School.Delete";
        public const string UserRead = "User.Read";
        public const string UserWrite = "User.Write";
        public const string UserDelete = "User.Delete";
        public const string RoleRead = "Role.Read";
        public const string RoleWrite = "Role.Write";
        public const string RoleDelete = "Role.Delete";

        public const string DocumentNumberSeriesRead = "DocumentNumberSeries.Read";
        public const string DocumentNumberSeriesWrite = "DocumentNumberSeries.Write";
        public const string DocumentNumberSeriesDelete = "DocumentNumberSeries.Delete";

        public const string AcademicYearRead = "AcademicYear.Read";
        public const string AcademicYearWrite = "AcademicYear.Write";
        public const string AcademicYearDelete = "AcademicYear.Delete";
        public const string AcademicYearClose = "AcademicYear.Close";

        public const string StudentRead = "Student.Read";
        public const string StudentWrite = "Student.Write";
        public const string StudentDelete = "Student.Delete";
        public const string TeacherRead = "Teacher.Read";
        public const string TeacherWrite = "Teacher.Write";
        public const string TeacherDelete = "Teacher.Delete";
        public const string TermRead = "Term.Read";
        public const string TermWrite = "Term.Write";
        public const string TermDelete = "Term.Delete";
        public const string SubjectRead = "Subject.Read";
        public const string SubjectWrite = "Subject.Write";
        public const string SubjectDelete = "Subject.Delete";
        public const string ClassRead = "Class.Read";
        public const string ClassWrite = "Class.Write";
        public const string GradeRead = "Grade.Read";
        public const string GradeWrite = "Grade.Write";
        public const string ParentRead = "Parent.Read";
        public const string ParentWrite = "Parent.Write";
        public const string ParentDelete = "Parent.Delete";

        public const string AssessmentRead = "Assessment.Read";
        public const string AssessmentWrite = "Assessment.Write";
        public const string AssessmentDelete = "Assessment.Delete";
        public const string ReportRead = "Report.Read";
        public const string ReportWrite = "Report.Write";

        public const string FinanceRead = "Finance.Read";
        public const string FinanceWrite = "Finance.Write";
        public const string FeeRead = "Fee.Read";
        public const string FeeWrite = "Fee.Write";
        public const string PaymentRead = "Payment.Read";
        public const string PaymentWrite = "Payment.Write";
        public const string InvoiceRead = "Invoice.Read";
        public const string InvoiceWrite = "Invoice.Write";
        public const string InvoiceItemRead = "InvoiceItem.Read";
        public const string InvoiceItemWrite = "InvoiceItem.Write";

        // ── Finance — Fee Structure ──────────────────────
        /// <summary>View fee structures for a school.</summary>
        public const string FeeStructureRead = "FeeStructure.Read";
        public const string FeeStructureWrite = "FeeStructure.Write";
        public const string FeeStructureDelete = "FeeStructure.Delete";

        public const string AccountingRead = "Accounting.Read";
        public const string AccountingWrite = "Accounting.Write";
        public const string AccountingDelete = "Accounting.Delete";
        public const string JournalEntryRead = "JournalEntry.Read";
        public const string JournalEntryWrite = "JournalEntry.Write";
        public const string JournalEntryDelete = "JournalEntry.Delete";
        public const string BudgetRead = "Budget.Read";
        public const string BudgetWrite = "Budget.Write";
        public const string BudgetDelete = "Budget.Delete";
        public const string ExpenseRead = "Expense.Read";
        public const string ExpenseWrite = "Expense.Write";
        public const string ExpenseDelete = "Expense.Delete";

        public const string CurriculumRead = "Curriculum.Read";
        public const string CurriculumWrite = "Curriculum.Write";
        public const string LessonPlanRead = "LessonPlan.Read";
        public const string LessonPlanWrite = "LessonPlan.Write";

        public const string LibraryRead = "Library.Read";
        public const string LibraryWrite = "Library.Write";
        public const string LibraryDelete = "Library.Delete";
        public const string BookRead = "Book.Read";
        public const string BookWrite = "Book.Write";
        public const string BookDelete = "Book.Delete";
        public const string BookIssueRead = "BookIssue.Read";
        public const string BookIssueWrite = "BookIssue.Write";
        public const string BookReturnRead = "BookReturn.Read";
        public const string BookReturnWrite = "BookReturn.Write";

        public const string MpesaInitiate = "Mpesa.Initiate";
        public const string MpesaViewTransactions = "Mpesa.ViewTransactions";
        public const string MpesaRefund = "Mpesa.Refund";
        public const string MpesaReconcile = "Mpesa.Reconcile";

        public static IEnumerable<string> AllPermissions => new[]
        {
            SchoolRead, SchoolWrite, SchoolDelete,
            UserRead, UserWrite, UserDelete,
            RoleRead, RoleWrite, RoleDelete,

            DocumentNumberSeriesRead, DocumentNumberSeriesWrite, DocumentNumberSeriesDelete,

            AcademicYearRead, AcademicYearWrite, AcademicYearDelete, AcademicYearClose,

            StudentRead, StudentWrite, StudentDelete,
            TeacherRead, TeacherWrite, TeacherDelete,
            TermRead, TermWrite, TermDelete,
            SubjectRead, SubjectWrite, SubjectDelete,
            ClassRead, ClassWrite,
            GradeRead, GradeWrite,
            ParentRead, ParentWrite, ParentDelete,

            AssessmentRead, AssessmentWrite, AssessmentDelete,
            ReportRead, ReportWrite,

            FinanceRead, FinanceWrite,
            FeeRead, FeeWrite,
            PaymentRead, PaymentWrite,
            InvoiceRead, InvoiceWrite,
            InvoiceItemRead, InvoiceItemWrite,
            FeeStructureRead, FeeStructureWrite, FeeStructureDelete,

            AccountingRead, AccountingWrite, AccountingDelete,
            JournalEntryRead, JournalEntryWrite, JournalEntryDelete,
            BudgetRead, BudgetWrite, BudgetDelete,
            ExpenseRead, ExpenseWrite, ExpenseDelete,

            CurriculumRead, CurriculumWrite,
            LessonPlanRead, LessonPlanWrite,

            LibraryRead, LibraryWrite, LibraryDelete,
            BookRead, BookWrite, BookDelete,
            BookIssueRead, BookIssueWrite,
            BookReturnRead, BookReturnWrite,

            MpesaInitiate, MpesaViewTransactions, MpesaRefund, MpesaReconcile,
        };
    }
}