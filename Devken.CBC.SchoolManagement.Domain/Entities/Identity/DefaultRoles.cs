using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Identity
{
    /// <summary>
    /// Default roles seeded for each new school
    /// </summary>
    public static class DefaultRoles
    {
        public static readonly (string RoleName, string Description, bool IsSystem, string[] Permissions)[] All =
        {
            // ══════════════════════════════════════════════════════════
            // SCHOOL ADMIN - Full administrative access
            // ══════════════════════════════════════════════════════════
            (
                "SchoolAdmin",
                "Full administrative access within this school",
                true,
                new[]
                {
                    // Administration - Full Access
                    PermissionKeys.SchoolRead,
                    PermissionKeys.SchoolWrite,
                    PermissionKeys.SchoolDelete,
                    PermissionKeys.UserRead,
                    PermissionKeys.UserWrite,
                    PermissionKeys.UserDelete,
                    PermissionKeys.RoleRead,
                    PermissionKeys.RoleWrite,
                    PermissionKeys.RoleDelete,

                    // Academic - Full Access
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

                    // Assessment - Full Access
                    PermissionKeys.AssessmentRead,
                    PermissionKeys.AssessmentWrite,
                    PermissionKeys.AssessmentDelete,
                    PermissionKeys.ReportRead,
                    PermissionKeys.ReportWrite,

                    // Finance - Full Access
                    PermissionKeys.FeeRead,
                    PermissionKeys.FeeWrite,
                    PermissionKeys.PaymentRead,
                    PermissionKeys.PaymentWrite,
                    PermissionKeys.InvoiceRead,
                    PermissionKeys.InvoiceWrite,

                    // ✅ M-Pesa - Full Access
                    PermissionKeys.MpesaInitiate,
                    PermissionKeys.MpesaViewTransactions,
                    PermissionKeys.MpesaRefund,
                    PermissionKeys.MpesaReconcile,

                    // Curriculum - Full Access
                    PermissionKeys.CurriculumRead,
                    PermissionKeys.CurriculumWrite,
                    PermissionKeys.LessonPlanRead,
                    PermissionKeys.LessonPlanWrite,
                }
            ),

            // ══════════════════════════════════════════════════════════
            // TEACHER - Academic content for assigned classes
            // ══════════════════════════════════════════════════════════
            (
                "Teacher",
                "Can view and manage academic content for assigned classes",
                true,
                new[]
                {
                    // Academic - Read Only
                    PermissionKeys.StudentRead,
                    PermissionKeys.SubjectRead,
                    PermissionKeys.ClassRead,

                    // Grades - Read/Write
                    PermissionKeys.GradeRead,
                    PermissionKeys.GradeWrite,

                    // Assessment - Read/Write
                    PermissionKeys.AssessmentRead,
                    PermissionKeys.AssessmentWrite,

                    // Reports - Read/Write
                    PermissionKeys.ReportRead,
                    PermissionKeys.ReportWrite,

                    // Curriculum - Read/Write
                    PermissionKeys.LessonPlanRead,
                    PermissionKeys.LessonPlanWrite,
                    PermissionKeys.CurriculumRead,
                }
            ),

            // ══════════════════════════════════════════════════════════
            // PARENT - Read-only access to their children's data
            // ══════════════════════════════════════════════════════════
            (
                "Parent",
                "Read-only access to their children's academic and financial data",
                true,
                new[]
                {
                    // Academic - Read Only
                    PermissionKeys.StudentRead,
                    PermissionKeys.GradeRead,

                    // Assessment - Read Only
                    PermissionKeys.AssessmentRead,
                    PermissionKeys.ReportRead,

                    // Finance - Read Only
                    PermissionKeys.PaymentRead,
                    PermissionKeys.InvoiceRead,

                    // ✅ M-Pesa - View Only (to check payment status)
                    PermissionKeys.MpesaViewTransactions,
                }
            ),

            // ══════════════════════════════════════════════════════════
            // FINANCE OFFICER - Manages fees, payments, and invoices
            // ══════════════════════════════════════════════════════════
            (
                "FinanceOfficer",
                "Manages fees, payments, invoices, and M-Pesa transactions",
                true,
                new[]
                {
                    // Finance - Full Access
                    PermissionKeys.FeeRead,
                    PermissionKeys.FeeWrite,
                    PermissionKeys.PaymentRead,
                    PermissionKeys.PaymentWrite,
                    PermissionKeys.InvoiceRead,
                    PermissionKeys.InvoiceWrite,

                    // ✅ M-Pesa - Full Operational Access
                    PermissionKeys.MpesaInitiate,
                    PermissionKeys.MpesaViewTransactions,
                    PermissionKeys.MpesaRefund,
                    PermissionKeys.MpesaReconcile,

                    // Student - Read Only (to view student info for billing)
                    PermissionKeys.StudentRead,
                }
            ),

            // ══════════════════════════════════════════════════════════
            // REGISTRAR - Student and teacher enrollment
            // ══════════════════════════════════════════════════════════
            (
                "Registrar",
                "Manages student and teacher enrollment and records",
                true,
                new[]
                {
                    // Academic - Full Student/Teacher Management
                    PermissionKeys.StudentRead,
                    PermissionKeys.StudentWrite,
                    PermissionKeys.StudentDelete,
                    PermissionKeys.TeacherRead,
                    PermissionKeys.TeacherWrite,
                    PermissionKeys.ClassRead,
                    PermissionKeys.ClassWrite,
                    PermissionKeys.SubjectRead,

                    // Reports - Read Only
                    PermissionKeys.ReportRead,
                }
            ),

            // ══════════════════════════════════════════════════════════
            // HEAD TEACHER - Senior academic oversight
            // ══════════════════════════════════════════════════════════
            (
                "HeadTeacher",
                "Senior academic staff with broader curriculum and assessment oversight",
                true,
                new[]
                {
                    // Academic - Read All, Write Limited
                    PermissionKeys.StudentRead,
                    PermissionKeys.TeacherRead,
                    PermissionKeys.SubjectRead,
                    PermissionKeys.SubjectWrite,
                    PermissionKeys.ClassRead,
                    PermissionKeys.ClassWrite,
                    PermissionKeys.GradeRead,
                    PermissionKeys.GradeWrite,

                    // Assessment - Full Access
                    PermissionKeys.AssessmentRead,
                    PermissionKeys.AssessmentWrite,
                    PermissionKeys.AssessmentDelete,
                    PermissionKeys.ReportRead,
                    PermissionKeys.ReportWrite,

                    // Curriculum - Full Access
                    PermissionKeys.CurriculumRead,
                    PermissionKeys.CurriculumWrite,
                    PermissionKeys.LessonPlanRead,
                    PermissionKeys.LessonPlanWrite,
                }
            ),

            // ══════════════════════════════════════════════════════════
            // ACCOUNTANT - View-only financial records
            // ══════════════════════════════════════════════════════════
            (
                "Accountant",
                "View-only access to financial records and M-Pesa transactions",
                true,
                new[]
                {
                    // Finance - Read Only
                    PermissionKeys.FeeRead,
                    PermissionKeys.PaymentRead,
                    PermissionKeys.InvoiceRead,

                    // ✅ M-Pesa - Read Only
                    PermissionKeys.MpesaViewTransactions,

                    // Student - Read Only (for financial reporting)
                    PermissionKeys.StudentRead,
                }
            ),

            // ══════════════════════════════════════════════════════════
            // ✅ NEW: CASHIER - Limited payment processing
            // ══════════════════════════════════════════════════════════
            (
                "Cashier",
                "Process payments and view basic financial records",
                true,
                new[]
                {
                    // Finance - Limited Access
                    PermissionKeys.PaymentRead,
                    PermissionKeys.PaymentWrite,
                    PermissionKeys.InvoiceRead,

                    // ✅ M-Pesa - Operational Access (no refunds)
                    PermissionKeys.MpesaInitiate,
                    PermissionKeys.MpesaViewTransactions,

                    // Student - Read Only
                    PermissionKeys.StudentRead,
                }
            ),
        };
    }
}