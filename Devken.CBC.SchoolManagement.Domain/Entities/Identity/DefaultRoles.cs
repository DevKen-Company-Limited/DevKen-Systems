using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Identity
{
    /// <summary>
    /// Default roles seeded for each new school.
    /// Each entry is (RoleName, Description, IsSystem, Permissions[]).
    /// </summary>
    public static class DefaultRoles
    {
        public static readonly (string RoleName, string Description, bool IsSystem, string[] Permissions)[] All =
        {
            // ══════════════════════════════════════════════════════════
            // SCHOOL ADMIN — Full administrative access within the school
            // ══════════════════════════════════════════════════════════
            (
                "SchoolAdmin",
                "Full administrative access within this school",
                true,
                new[]
                {
                    // Administration
                    PermissionKeys.SchoolRead,
                    PermissionKeys.SchoolWrite,
                    PermissionKeys.SchoolDelete,
                    PermissionKeys.UserRead,
                    PermissionKeys.UserWrite,
                    PermissionKeys.UserDelete,
                    PermissionKeys.RoleRead,
                    PermissionKeys.RoleWrite,
                    PermissionKeys.RoleDelete,

                    // Academic Year
                    PermissionKeys.AcademicYearRead,
                    PermissionKeys.AcademicYearWrite,
                    PermissionKeys.AcademicYearDelete,
                    PermissionKeys.AcademicYearClose,

                    // Academic
                    PermissionKeys.StudentRead,
                    PermissionKeys.StudentWrite,
                    PermissionKeys.StudentDelete,
                    PermissionKeys.TeacherRead,
                    PermissionKeys.TeacherWrite,
                    PermissionKeys.TeacherDelete,
                    PermissionKeys.SubjectRead,
                    PermissionKeys.SubjectWrite,
                    PermissionKeys.ClassRead,
                    PermissionKeys.ClassWrite,
                    PermissionKeys.GradeRead,
                    PermissionKeys.GradeWrite,

                    // Term
                    PermissionKeys.TermRead,
                    PermissionKeys.TermWrite,
                    PermissionKeys.TermDelete,

                    // Assessment
                    PermissionKeys.AssessmentRead,
                    PermissionKeys.AssessmentWrite,
                    PermissionKeys.AssessmentDelete,
                    PermissionKeys.ReportRead,
                    PermissionKeys.ReportWrite,

                    // Finance — Broad
                    PermissionKeys.FinanceRead,
                    PermissionKeys.FinanceWrite,

                    // Finance — Granular
                    PermissionKeys.FeeRead,
                    PermissionKeys.FeeWrite,
                    PermissionKeys.PaymentRead,
                    PermissionKeys.PaymentWrite,
                    PermissionKeys.InvoiceRead,
                    PermissionKeys.InvoiceWrite,

                    // Finance — Fee Structure (full access)
                    PermissionKeys.FeeStructureRead,
                    PermissionKeys.FeeStructureWrite,
                    PermissionKeys.FeeStructureDelete,

                    // M-Pesa — Full Access
                    PermissionKeys.MpesaInitiate,
                    PermissionKeys.MpesaViewTransactions,
                    PermissionKeys.MpesaRefund,
                    PermissionKeys.MpesaReconcile,

                    // Curriculum
                    PermissionKeys.CurriculumRead,
                    PermissionKeys.CurriculumWrite,
                    PermissionKeys.LessonPlanRead,
                    PermissionKeys.LessonPlanWrite,

                    // Settings
                    PermissionKeys.DocumentNumberSeriesRead,
                    PermissionKeys.DocumentNumberSeriesWrite,
                    PermissionKeys.DocumentNumberSeriesDelete,
                }
            ),

            // ══════════════════════════════════════════════════════════
            // TEACHER — Academic content for assigned classes
            // ══════════════════════════════════════════════════════════
            (
                "Teacher",
                "Can view and manage academic content for assigned classes",
                true,
                new[]
                {
                    // Academic — Read Only
                    PermissionKeys.StudentRead,
                    PermissionKeys.SubjectRead,
                    PermissionKeys.ClassRead,
                    PermissionKeys.AcademicYearRead,
                    PermissionKeys.TermRead,

                    // Grades — Read/Write
                    PermissionKeys.GradeRead,
                    PermissionKeys.GradeWrite,

                    // Assessment — Read/Write
                    PermissionKeys.AssessmentRead,
                    PermissionKeys.AssessmentWrite,

                    // Reports — Read/Write
                    PermissionKeys.ReportRead,
                    PermissionKeys.ReportWrite,

                    // Curriculum — Read/Write
                    PermissionKeys.LessonPlanRead,
                    PermissionKeys.LessonPlanWrite,
                    PermissionKeys.CurriculumRead,
                }
            ),

            // ══════════════════════════════════════════════════════════
            // PARENT — Read-only access to their children's data
            // ══════════════════════════════════════════════════════════
            (
                "Parent",
                "Read-only access to their children's academic and financial data",
                true,
                new[]
                {
                    // Academic — Read Only
                    PermissionKeys.StudentRead,
                    PermissionKeys.GradeRead,

                    // Assessment — Read Only
                    PermissionKeys.AssessmentRead,
                    PermissionKeys.ReportRead,

                    // Finance — Read Only
                    PermissionKeys.FinanceRead,
                    PermissionKeys.PaymentRead,
                    PermissionKeys.InvoiceRead,

                    // Fee Structure — Read Only (parent can view school fee breakdown)
                    PermissionKeys.FeeStructureRead,

                    // M-Pesa — View Only
                    PermissionKeys.MpesaViewTransactions,
                }
            ),

            // ══════════════════════════════════════════════════════════
            // FINANCE OFFICER — Manages fees, payments, and invoices
            // ══════════════════════════════════════════════════════════
            (
                "FinanceOfficer",
                "Manages fees, fee structures, payments, invoices, and M-Pesa transactions",
                true,
                new[]
                {
                    // Finance — Broad
                    PermissionKeys.FinanceRead,
                    PermissionKeys.FinanceWrite,

                    // Finance — Granular
                    PermissionKeys.FeeRead,
                    PermissionKeys.FeeWrite,
                    PermissionKeys.PaymentRead,
                    PermissionKeys.PaymentWrite,
                    PermissionKeys.InvoiceRead,
                    PermissionKeys.InvoiceWrite,

                    // Finance — Fee Structure (full operational access; no delete)
                    PermissionKeys.FeeStructureRead,
                    PermissionKeys.FeeStructureWrite,

                    // M-Pesa — Full Operational Access
                    PermissionKeys.MpesaInitiate,
                    PermissionKeys.MpesaViewTransactions,
                    PermissionKeys.MpesaRefund,
                    PermissionKeys.MpesaReconcile,

                    // Student — Read Only (for billing)
                    PermissionKeys.StudentRead,

                    // Settings — Read Only
                    PermissionKeys.DocumentNumberSeriesRead,
                }
            ),

            // ══════════════════════════════════════════════════════════
            // REGISTRAR — Student and teacher enrollment
            // ══════════════════════════════════════════════════════════
            (
                "Registrar",
                "Manages student and teacher enrollment and records",
                true,
                new[]
                {
                    // Academic — Full Student/Teacher Management
                    PermissionKeys.StudentRead,
                    PermissionKeys.StudentWrite,
                    PermissionKeys.StudentDelete,
                    PermissionKeys.TeacherRead,
                    PermissionKeys.TeacherWrite,
                    PermissionKeys.ClassRead,
                    PermissionKeys.ClassWrite,
                    PermissionKeys.SubjectRead,
                    PermissionKeys.AcademicYearRead,
                    PermissionKeys.TermRead,

                    // Reports — Read Only
                    PermissionKeys.ReportRead,
                }
            ),

            // ══════════════════════════════════════════════════════════
            // HEAD TEACHER — Senior academic oversight
            // ══════════════════════════════════════════════════════════
            (
                "HeadTeacher",
                "Senior academic staff with broader curriculum and assessment oversight",
                true,
                new[]
                {
                    // Academic — Read All, Write Limited
                    PermissionKeys.StudentRead,
                    PermissionKeys.TeacherRead,
                    PermissionKeys.SubjectRead,
                    PermissionKeys.SubjectWrite,
                    PermissionKeys.ClassRead,
                    PermissionKeys.ClassWrite,
                    PermissionKeys.GradeRead,
                    PermissionKeys.GradeWrite,
                    PermissionKeys.AcademicYearRead,
                    PermissionKeys.AcademicYearWrite,
                    PermissionKeys.TermRead,
                    PermissionKeys.TermWrite,

                    // Assessment — Full Access
                    PermissionKeys.AssessmentRead,
                    PermissionKeys.AssessmentWrite,
                    PermissionKeys.AssessmentDelete,
                    PermissionKeys.ReportRead,
                    PermissionKeys.ReportWrite,

                    // Curriculum — Full Access
                    PermissionKeys.CurriculumRead,
                    PermissionKeys.CurriculumWrite,
                    PermissionKeys.LessonPlanRead,
                    PermissionKeys.LessonPlanWrite,

                    // Finance — Read Only (to see student fee status)
                    PermissionKeys.FinanceRead,
                    PermissionKeys.FeeStructureRead,
                }
            ),

            // ══════════════════════════════════════════════════════════
            // ACCOUNTANT — View-only financial records
            // ══════════════════════════════════════════════════════════
            (
                "Accountant",
                "View-only access to financial records and M-Pesa transactions",
                true,
                new[]
                {
                    // Finance — Read Only (broad)
                    PermissionKeys.FinanceRead,

                    // Finance — Granular Read
                    PermissionKeys.FeeRead,
                    PermissionKeys.PaymentRead,
                    PermissionKeys.InvoiceRead,

                    // Fee Structure — Read Only
                    PermissionKeys.FeeStructureRead,

                    // M-Pesa — Read Only
                    PermissionKeys.MpesaViewTransactions,

                    // Student — Read Only (for financial reporting)
                    PermissionKeys.StudentRead,
                }
            ),

            // ══════════════════════════════════════════════════════════
            // CASHIER — Limited payment processing
            // ══════════════════════════════════════════════════════════
            (
                "Cashier",
                "Process payments and view basic financial records",
                true,
                new[]
                {
                    // Finance — Limited
                    PermissionKeys.FinanceRead,
                    PermissionKeys.PaymentRead,
                    PermissionKeys.PaymentWrite,
                    PermissionKeys.InvoiceRead,

                    // Fee Structure — Read Only (to look up amounts when collecting payment)
                    PermissionKeys.FeeStructureRead,

                    // M-Pesa — Operational Access (no refunds)
                    PermissionKeys.MpesaInitiate,
                    PermissionKeys.MpesaViewTransactions,

                    // Student — Read Only
                    PermissionKeys.StudentRead,
                }
            ),
        };
    }
}