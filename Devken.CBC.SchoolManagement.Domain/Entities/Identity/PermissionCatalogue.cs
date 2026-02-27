using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Identity
{
    public static class PermissionCatalogue
    {
        public static readonly (string Key, string Display, string Group, string Desc)[] All =
        {
            // Administration
            (PermissionKeys.SchoolRead,   "View School Settings",   "Administration", "Read school profile and configuration"),
            (PermissionKeys.SchoolWrite,  "Edit School Settings",   "Administration", "Update school name, logo, contact info"),
               (PermissionKeys.SchoolDelete, "Delete Schools",         "Administration", "Permanently remove a school"),
            (PermissionKeys.UserRead,     "View Users",             "Administration", "List and view user accounts"),
            (PermissionKeys.UserWrite,    "Create / Edit Users",    "Administration", "Create new users or update existing ones"),
            (PermissionKeys.UserDelete,   "Delete Users",           "Administration", "Permanently remove a user account"),
            (PermissionKeys.RoleRead,     "View Roles",             "Administration", "List roles and their permissions"),
            (PermissionKeys.RoleWrite,    "Create / Edit Roles",    "Administration", "Add or modify roles"),
            (PermissionKeys.RoleDelete,   "Delete Roles",           "Administration", "Remove a role from the system"),

                // Academic Year
            (PermissionKeys.AcademicYearRead, "View Academic Years", "Academic Year", "Access academic year records and schedules"),
            (PermissionKeys.AcademicYearWrite, "Manage Academic Years", "Academic Year", "Create or update academic year periods"),
            (PermissionKeys.AcademicYearDelete, "Delete Academic Years", "Academic Year", "Remove academic year records"),
            (PermissionKeys.AcademicYearClose, "Close Academic Years", "Academic Year", "Finalize and close academic year for archival"),
            //  TERMS 
            (PermissionKeys.TermRead, "View Terms", "Academic - Terms", "Access term records and schedules"),
            (PermissionKeys.TermWrite, "Manage Terms", "Academic - Terms", "Create, update, close, or reopen terms"),
            (PermissionKeys.TermDelete, "Delete Terms", "Academic - Terms", "Remove term records permanently"),

            // Academic
            (PermissionKeys.StudentRead,  "View Students",          "Academic", "Access student records"),
            (PermissionKeys.StudentWrite, "Manage Students",        "Academic", "Add or update student data"),
            (PermissionKeys.StudentDelete,"Delete Students",        "Academic", "Remove student records"),
            (PermissionKeys.TeacherRead,  "View Teachers",          "Academic", "Access teacher records"),
            (PermissionKeys.TeacherWrite, "Manage Teachers",        "Academic", "Add or update teacher data"),
            (PermissionKeys.TeacherDelete,"Delete Teachers",        "Academic", "Remove teacher records"),
            (PermissionKeys.SubjectRead,  "View Subjects",          "Academic", "Access subject catalogue"),
            (PermissionKeys.SubjectWrite, "Manage Subjects",        "Academic", "Add or update subjects"),
            (PermissionKeys.ClassRead,    "View Classes",           "Academic", "Access class records"),
            (PermissionKeys.ClassWrite,   "Manage Classes",         "Academic", "Create or update classes"),
            (PermissionKeys.GradeRead,    "View Grades",            "Academic", "Access grading records"),
            (PermissionKeys.GradeWrite,   "Manage Grades",          "Academic", "Enter or update grades"),
            (PermissionKeys.ParentRead,   "View Parents",   "Academic", "Access parent/guardian records"),
            (PermissionKeys.ParentWrite,  "Manage Parents", "Academic", "Add or update parent/guardian data"),
            (PermissionKeys.ParentDelete, "Delete Parents", "Academic", "Remove parent/guardian records"),


            // Assessment
            (PermissionKeys.AssessmentRead,  "View Assessments",    "Assessment", "Access assessment records"),
            (PermissionKeys.AssessmentWrite, "Manage Assessments",  "Assessment", "Create or update assessments"),
            (PermissionKeys.AssessmentDelete,"Delete Assessments",  "Assessment", "Remove assessment records"),
            (PermissionKeys.ReportRead,      "View Reports",        "Assessment", "Access progress and summary reports"),
            (PermissionKeys.ReportWrite,     "Generate Reports",    "Assessment", "Create new reports"),

            // Finance
            (PermissionKeys.FeeRead,      "View Fees",              "Finance", "Access fee structures"),
            (PermissionKeys.FeeWrite,     "Manage Fees",            "Finance", "Create or update fee structures"),
            (PermissionKeys.PaymentRead,  "View Payments",          "Finance", "Access payment records"),
            (PermissionKeys.PaymentWrite, "Record Payments",        "Finance", "Log new payments"),
            (PermissionKeys.InvoiceRead,  "View Invoices",          "Finance", "Access invoice records"),
            (PermissionKeys.InvoiceWrite, "Generate Invoices",      "Finance", "Create new invoices"),

            // Curriculum
            (PermissionKeys.CurriculumRead,  "View Curriculum",     "Curriculum", "Access curriculum structure"),
            (PermissionKeys.CurriculumWrite, "Manage Curriculum",   "Curriculum", "Update curriculum structure"),
            (PermissionKeys.LessonPlanRead,  "View Lesson Plans",   "Curriculum", "Access lesson plans"),
            (PermissionKeys.LessonPlanWrite, "Create Lesson Plans", "Curriculum", "Create or update lesson plans"),
                        // ✅ NEW: M-Pesa Specific Permissions
            (PermissionKeys.MpesaInitiate, "Initiate M-Pesa Payments", "Finance - M-Pesa", "Start M-Pesa STK push transactions"),
            (PermissionKeys.MpesaViewTransactions, "View M-Pesa Transactions", "Finance - M-Pesa", "View M-Pesa payment history and status"),
            (PermissionKeys.MpesaRefund, "Process M-Pesa Refunds", "Finance - M-Pesa", "Initiate refunds for M-Pesa transactions"),
            (PermissionKeys.MpesaReconcile, "Reconcile M-Pesa Payments", "Finance - M-Pesa", "Match M-Pesa transactions with invoices"),

            // Add this section after the Curriculum permissions in the All array

            // Settings / Configuration
            (PermissionKeys.DocumentNumberSeriesRead,   "View Number Series",    "Settings", "View document numbering configuration"),
            (PermissionKeys.DocumentNumberSeriesWrite,  "Manage Number Series",  "Settings", "Create and update document numbering rules"),
            (PermissionKeys.DocumentNumberSeriesDelete, "Delete Number Series",  "Settings", "Remove document numbering configurations"),
        };
    }
}
