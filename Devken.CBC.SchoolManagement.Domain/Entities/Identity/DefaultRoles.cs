using System;
using System.Collections.Generic;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Identity
{
    public static class DefaultRoles
    {
        public static readonly (string RoleName, string Description, bool IsSystem, string[] Permissions)[] All =
        {
            (
                "SchoolAdmin",
                "Full administrative access within this school",
                true,
                new[]
                {
                    PermissionKeys.SchoolRead, PermissionKeys.SchoolWrite, PermissionKeys.SchoolDelete,
                    PermissionKeys.UserRead, PermissionKeys.UserWrite, PermissionKeys.UserDelete,
                    PermissionKeys.RoleRead, PermissionKeys.RoleWrite, PermissionKeys.RoleDelete,
                    PermissionKeys.DocumentNumberSeriesRead, PermissionKeys.DocumentNumberSeriesWrite, PermissionKeys.DocumentNumberSeriesDelete,
                    PermissionKeys.AcademicYearRead, PermissionKeys.AcademicYearWrite, PermissionKeys.AcademicYearDelete, PermissionKeys.AcademicYearClose,
                    PermissionKeys.StudentRead, PermissionKeys.StudentWrite, PermissionKeys.StudentDelete,
                    PermissionKeys.TeacherRead, PermissionKeys.TeacherWrite, PermissionKeys.TeacherDelete,
                    PermissionKeys.TermRead, PermissionKeys.TermWrite, PermissionKeys.TermDelete,
                    PermissionKeys.SubjectRead, PermissionKeys.SubjectWrite,
                    PermissionKeys.ClassRead, PermissionKeys.ClassWrite,
                    PermissionKeys.GradeRead, PermissionKeys.GradeWrite,
                    PermissionKeys.ParentRead, PermissionKeys.ParentWrite, PermissionKeys.ParentDelete,
                    PermissionKeys.AssessmentRead, PermissionKeys.AssessmentWrite, PermissionKeys.AssessmentDelete,
                    PermissionKeys.ReportRead, PermissionKeys.ReportWrite,
                    PermissionKeys.FinanceRead, PermissionKeys.FinanceWrite,
                    PermissionKeys.FeeRead, PermissionKeys.FeeWrite,
                    PermissionKeys.PaymentRead, PermissionKeys.PaymentWrite,
                    PermissionKeys.InvoiceRead, PermissionKeys.InvoiceWrite,
                    PermissionKeys.InvoiceItemRead, PermissionKeys.InvoiceItemWrite,
                    PermissionKeys.FeeStructureRead, PermissionKeys.FeeStructureWrite, PermissionKeys.FeeStructureDelete,
                    PermissionKeys.AccountingRead, PermissionKeys.AccountingWrite, PermissionKeys.AccountingDelete,
                    PermissionKeys.JournalEntryRead, PermissionKeys.JournalEntryWrite, PermissionKeys.JournalEntryDelete,
                    PermissionKeys.BudgetRead, PermissionKeys.BudgetWrite, PermissionKeys.BudgetDelete,
                    PermissionKeys.ExpenseRead, PermissionKeys.ExpenseWrite, PermissionKeys.ExpenseDelete,
                    PermissionKeys.CurriculumRead, PermissionKeys.CurriculumWrite,
                    PermissionKeys.LessonPlanRead, PermissionKeys.LessonPlanWrite,
                    PermissionKeys.LibraryRead, PermissionKeys.LibraryWrite, PermissionKeys.LibraryDelete,
                    PermissionKeys.BookRead, PermissionKeys.BookWrite, PermissionKeys.BookDelete,
                    PermissionKeys.BookIssueRead, PermissionKeys.BookIssueWrite,
                    PermissionKeys.BookReturnRead, PermissionKeys.BookReturnWrite,
                    PermissionKeys.MpesaInitiate, PermissionKeys.MpesaViewTransactions, PermissionKeys.MpesaRefund, PermissionKeys.MpesaReconcile,
                }
            ),

            (
                "Teacher",
                "Can view and manage academic content for assigned classes",
                true,
                new[]
                {
                    PermissionKeys.StudentRead, PermissionKeys.SubjectRead,
                    PermissionKeys.ClassRead, PermissionKeys.AcademicYearRead,
                    PermissionKeys.TermRead, PermissionKeys.ParentRead,
                    PermissionKeys.GradeRead, PermissionKeys.GradeWrite,
                    PermissionKeys.AssessmentRead, PermissionKeys.AssessmentWrite,
                    PermissionKeys.ReportRead, PermissionKeys.ReportWrite,
                    PermissionKeys.LessonPlanRead, PermissionKeys.LessonPlanWrite,
                    PermissionKeys.CurriculumRead,
                    PermissionKeys.LibraryRead, PermissionKeys.BookRead,
                    PermissionKeys.BookIssueRead, PermissionKeys.BookReturnRead,
                }
            ),

            (
                "Parent",
                "Read-only access to their children's academic and financial data",
                true,
                new[]
                {
                    PermissionKeys.StudentRead,
                    PermissionKeys.GradeRead,
                    PermissionKeys.AssessmentRead, PermissionKeys.ReportRead,
                    PermissionKeys.FinanceRead, PermissionKeys.PaymentRead,
                    PermissionKeys.InvoiceRead, PermissionKeys.FeeStructureRead,
                    PermissionKeys.MpesaViewTransactions,
                    PermissionKeys.LibraryRead, PermissionKeys.BookRead,
                    PermissionKeys.BookIssueRead, PermissionKeys.BookReturnRead,
                }
            ),

            (
                "FinanceOfficer",
                "Manages fees, fee structures, payments, invoices, and M-Pesa transactions",
                true,
                new[]
                {
                    PermissionKeys.FinanceRead, PermissionKeys.FinanceWrite,
                    PermissionKeys.FeeRead, PermissionKeys.FeeWrite,
                    PermissionKeys.PaymentRead, PermissionKeys.PaymentWrite,
                    PermissionKeys.InvoiceRead, PermissionKeys.InvoiceWrite,
                    PermissionKeys.InvoiceItemRead, PermissionKeys.InvoiceItemWrite,
                    PermissionKeys.FeeStructureRead, PermissionKeys.FeeStructureWrite,
                    PermissionKeys.MpesaInitiate, PermissionKeys.MpesaViewTransactions,
                    PermissionKeys.MpesaRefund, PermissionKeys.MpesaReconcile,
                    PermissionKeys.StudentRead,
                    PermissionKeys.DocumentNumberSeriesRead,
                }
            ),

            (
                "Registrar",
                "Manages student and teacher enrollment and records",
                true,
                new[]
                {
                    PermissionKeys.StudentRead, PermissionKeys.StudentWrite, PermissionKeys.StudentDelete,
                    PermissionKeys.TeacherRead, PermissionKeys.TeacherWrite,
                    PermissionKeys.ClassRead, PermissionKeys.ClassWrite,
                    PermissionKeys.SubjectRead,
                    PermissionKeys.AcademicYearRead, PermissionKeys.TermRead,
                    PermissionKeys.ParentRead, PermissionKeys.ParentWrite,
                    PermissionKeys.ReportRead,
                }
            ),

            (
                "HeadTeacher",
                "Senior academic staff with broader curriculum and assessment oversight",
                true,
                new[]
                {
                    PermissionKeys.StudentRead,
                    PermissionKeys.TeacherRead,
                    PermissionKeys.SubjectRead, PermissionKeys.SubjectWrite,
                    PermissionKeys.ClassRead, PermissionKeys.ClassWrite,
                    PermissionKeys.GradeRead, PermissionKeys.GradeWrite,
                    PermissionKeys.AcademicYearRead, PermissionKeys.AcademicYearWrite,
                    PermissionKeys.TermRead, PermissionKeys.TermWrite,
                    PermissionKeys.ParentRead,
                    PermissionKeys.AssessmentRead, PermissionKeys.AssessmentWrite, PermissionKeys.AssessmentDelete,
                    PermissionKeys.ReportRead, PermissionKeys.ReportWrite,
                    PermissionKeys.CurriculumRead, PermissionKeys.CurriculumWrite,
                    PermissionKeys.LessonPlanRead, PermissionKeys.LessonPlanWrite,
                    PermissionKeys.FinanceRead, PermissionKeys.FeeStructureRead,
                    PermissionKeys.LibraryRead, PermissionKeys.BookRead,
                    PermissionKeys.BookIssueRead, PermissionKeys.BookReturnRead,
                }
            ),

            (
                "Accountant",
                "Full access to accounting, journal entries, budgets and expenses. View-only for finance and M-Pesa",
                true,
                new[]
                {
                    PermissionKeys.FinanceRead,
                    PermissionKeys.FeeRead, PermissionKeys.PaymentRead,
                    PermissionKeys.InvoiceRead, PermissionKeys.FeeStructureRead,
                    PermissionKeys.AccountingRead, PermissionKeys.AccountingWrite, PermissionKeys.AccountingDelete,
                    PermissionKeys.JournalEntryRead, PermissionKeys.JournalEntryWrite, PermissionKeys.JournalEntryDelete,
                    PermissionKeys.BudgetRead, PermissionKeys.BudgetWrite, PermissionKeys.BudgetDelete,
                    PermissionKeys.ExpenseRead, PermissionKeys.ExpenseWrite, PermissionKeys.ExpenseDelete,
                    PermissionKeys.MpesaViewTransactions,
                    PermissionKeys.StudentRead,
                }
            ),

            (
                "Cashier",
                "Process payments and view basic financial records",
                true,
                new[]
                {
                    PermissionKeys.FinanceRead,
                    PermissionKeys.PaymentRead, PermissionKeys.PaymentWrite,
                    PermissionKeys.InvoiceRead,
                    PermissionKeys.FeeStructureRead,
                    PermissionKeys.MpesaInitiate, PermissionKeys.MpesaViewTransactions,
                    PermissionKeys.StudentRead,
                }
            ),

            (
                "Librarian",
                "Manages all library resources including books, issues and returns",
                true,
                new[]
                {
                    PermissionKeys.LibraryRead, PermissionKeys.LibraryWrite, PermissionKeys.LibraryDelete,
                    PermissionKeys.BookRead, PermissionKeys.BookWrite, PermissionKeys.BookDelete,
                    PermissionKeys.BookIssueRead, PermissionKeys.BookIssueWrite,
                    PermissionKeys.BookReturnRead, PermissionKeys.BookReturnWrite,
                    PermissionKeys.StudentRead,
                    PermissionKeys.TeacherRead,
                }
            ),
        };
    }
}