using System;
using System.Collections.Generic;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Identity
{
    public static class PermissionCatalogue
    {
        public static readonly (string Key, string Display, string Group, string Desc)[] All =
        {
            (PermissionKeys.DashboardView,                    "View Dashboard",                  "Dashboard", "Access the school overview dashboard"),
            (PermissionKeys.DashboardStatsEnrollment,         "View Enrollment Stats",           "Dashboard", "See enrolled student count and trend"),
            (PermissionKeys.DashboardStatsStaff,              "View Staff Stats",                "Dashboard", "See teaching staff count"),
            (PermissionKeys.DashboardStatsAssessmentsPending, "View Pending Assessments Stats",  "Dashboard", "See pending and overdue assessment counts"),
            (PermissionKeys.DashboardStatsFeeRate,            "View Fee Rate Stat",              "Dashboard", "See fee collection rate card"),
            (PermissionKeys.DashboardClassPerformance,        "View Class Performance",          "Dashboard", "See class-by-class performance bar chart"),
            (PermissionKeys.DashboardCompetency,              "View Competency Distribution",    "Dashboard", "See EE / ME / AE / BE competency breakdown"),
            (PermissionKeys.DashboardRecentAssessments,       "View Recent Assessments",         "Dashboard", "See the recent assessments table"),
            (PermissionKeys.DashboardEvents,                  "View Upcoming Events",            "Dashboard", "See upcoming school events list"),
            (PermissionKeys.DashboardFeeCollection,           "View Fee Collection Panel",       "Dashboard", "See detailed fee collection amounts and defaulters"),
            (PermissionKeys.DashboardQuickActions,            "Use Dashboard Quick Actions",     "Dashboard", "Access quick-action buttons on the dashboard"),

            (PermissionKeys.SchoolRead,   "View School Settings",   "Administration", "Read school profile and configuration"),
            (PermissionKeys.SchoolWrite,  "Edit School Settings",   "Administration", "Update school name, logo, contact info"),
            (PermissionKeys.SchoolDelete, "Delete Schools",         "Administration", "Permanently remove a school"),
            (PermissionKeys.UserRead,     "View Users",             "Administration", "List and view user accounts"),
            (PermissionKeys.UserWrite,    "Create / Edit Users",    "Administration", "Create new users or update existing ones"),
            (PermissionKeys.UserDelete,   "Delete Users",           "Administration", "Permanently remove a user account"),
            (PermissionKeys.RoleRead,     "View Roles",             "Administration", "List roles and their permissions"),
            (PermissionKeys.RoleWrite,    "Create / Edit Roles",    "Administration", "Add or modify roles"),
            (PermissionKeys.RoleDelete,   "Delete Roles",           "Administration", "Remove a role from the system"),

            (PermissionKeys.DocumentNumberSeriesRead,   "View Number Series",   "Settings", "View document numbering configuration"),
            (PermissionKeys.DocumentNumberSeriesWrite,  "Manage Number Series", "Settings", "Create and update document numbering rules"),
            (PermissionKeys.DocumentNumberSeriesDelete, "Delete Number Series", "Settings", "Remove document numbering configurations"),

            (PermissionKeys.AcademicYearRead,   "View Academic Years",   "Academic Year", "Access academic year records and schedules"),
            (PermissionKeys.AcademicYearWrite,  "Manage Academic Years", "Academic Year", "Create or update academic year periods"),
            (PermissionKeys.AcademicYearDelete, "Delete Academic Years", "Academic Year", "Remove academic year records"),
            (PermissionKeys.AcademicYearClose,  "Close Academic Years",  "Academic Year", "Finalize and close academic year for archival"),

            (PermissionKeys.TermRead,   "View Terms",   "Academic - Terms", "Access term records and schedules"),
            (PermissionKeys.TermWrite,  "Manage Terms", "Academic - Terms", "Create, update, close, or reopen terms"),
            (PermissionKeys.TermDelete, "Delete Terms", "Academic - Terms", "Remove term records permanently"),

            (PermissionKeys.StudentRead,   "View Students",   "Academic", "Access student records"),
            (PermissionKeys.StudentWrite,  "Manage Students", "Academic", "Add or update student data"),
            (PermissionKeys.StudentDelete, "Delete Students", "Academic", "Remove student records"),
            (PermissionKeys.TeacherRead,   "View Teachers",   "Academic", "Access teacher records"),
            (PermissionKeys.TeacherWrite,  "Manage Teachers", "Academic", "Add or update teacher data"),
            (PermissionKeys.TeacherDelete, "Delete Teachers", "Academic", "Remove teacher records"),
            (PermissionKeys.SubjectRead,   "View Subjects",   "Academic", "Access subject catalogue"),
            (PermissionKeys.SubjectWrite,  "Manage Subjects", "Academic", "Add or update subjects"),
            (PermissionKeys.ClassRead,     "View Classes",    "Academic", "Access class records"),
            (PermissionKeys.ClassWrite,    "Manage Classes",  "Academic", "Create or update classes"),
            (PermissionKeys.GradeRead,     "View Grades",     "Academic", "Access grading records"),
            (PermissionKeys.GradeWrite,    "Manage Grades",   "Academic", "Enter or update grades"),
            (PermissionKeys.ParentRead,    "View Parents",    "Academic", "Access parent/guardian records"),
            (PermissionKeys.ParentWrite,   "Manage Parents",  "Academic", "Add or update parent/guardian data"),
            (PermissionKeys.ParentDelete,  "Delete Parents",  "Academic", "Remove parent/guardian records"),

            (PermissionKeys.AssessmentRead,   "View Assessments",   "Assessment", "Access assessment records"),
            (PermissionKeys.AssessmentWrite,  "Manage Assessments", "Assessment", "Create or update assessments"),
            (PermissionKeys.AssessmentDelete, "Delete Assessments", "Assessment", "Remove assessment records"),
            (PermissionKeys.ReportRead,       "View Reports",       "Assessment", "Access progress and summary reports"),
            (PermissionKeys.ReportWrite,      "Generate Reports",   "Assessment", "Create new reports"),

            (PermissionKeys.FinanceRead,      "View Finance",         "Finance", "Broad read access to all finance resources"),
            (PermissionKeys.FinanceWrite,     "Manage Finance",       "Finance", "Broad write access to all finance resources"),
            (PermissionKeys.FeeRead,          "View Fees",            "Finance", "Access fee structures"),
            (PermissionKeys.FeeWrite,         "Manage Fees",          "Finance", "Create or update fee structures"),
            (PermissionKeys.PaymentRead,      "View Payments",        "Finance", "Access payment records"),
            (PermissionKeys.PaymentWrite,     "Record Payments",      "Finance", "Log new payments"),
            (PermissionKeys.InvoiceRead,      "View Invoices",        "Finance", "Access invoice records"),
            (PermissionKeys.InvoiceWrite,     "Generate Invoices",    "Finance", "Create new invoices"),
            (PermissionKeys.InvoiceItemRead,  "View Invoice Items",   "Finance", "Access individual line items on invoices"),
            (PermissionKeys.InvoiceItemWrite, "Manage Invoice Items", "Finance", "Add or update line items on invoices"),

            (PermissionKeys.FeeStructureRead,   "View Fee Structures",   "Finance - Fee Structure", "View fee amounts per academic year and term"),
            (PermissionKeys.FeeStructureWrite,  "Manage Fee Structures", "Finance - Fee Structure", "Create, update, or activate fee structures"),
            (PermissionKeys.FeeStructureDelete, "Delete Fee Structures", "Finance - Fee Structure", "Permanently remove a fee structure record"),

            (PermissionKeys.AccountingRead,      "View Accounting",      "Accounting", "Broad read access to all accounting records"),
            (PermissionKeys.AccountingWrite,     "Manage Accounting",    "Accounting", "Broad write access to all accounting records"),
            (PermissionKeys.AccountingDelete,    "Delete Accounting",    "Accounting", "Delete accounting records"),
            (PermissionKeys.JournalEntryRead,    "View Journal Entries",   "Accounting", "Access journal entry records"),
            (PermissionKeys.JournalEntryWrite,   "Manage Journal Entries", "Accounting", "Create or update journal entries"),
            (PermissionKeys.JournalEntryDelete,  "Delete Journal Entries", "Accounting", "Remove journal entry records"),
            (PermissionKeys.BudgetRead,          "View Budgets",   "Accounting", "Access budget records"),
            (PermissionKeys.BudgetWrite,         "Manage Budgets", "Accounting", "Create or update budgets"),
            (PermissionKeys.BudgetDelete,        "Delete Budgets", "Accounting", "Remove budget records"),
            (PermissionKeys.ExpenseRead,         "View Expenses",   "Accounting", "Access expense records"),
            (PermissionKeys.ExpenseWrite,        "Manage Expenses", "Accounting", "Create or update expense records"),
            (PermissionKeys.ExpenseDelete,       "Delete Expenses", "Accounting", "Remove expense records"),

            (PermissionKeys.CurriculumRead,  "View Curriculum",     "Curriculum", "Access curriculum structure"),
            (PermissionKeys.CurriculumWrite, "Manage Curriculum",   "Curriculum", "Update curriculum structure"),
            (PermissionKeys.LessonPlanRead,  "View Lesson Plans",   "Curriculum", "Access lesson plans"),
            (PermissionKeys.LessonPlanWrite, "Create Lesson Plans", "Curriculum", "Create or update lesson plans"),

            (PermissionKeys.LibraryRead,       "View Library",          "Library", "Broad read access to all library resources"),
            (PermissionKeys.LibraryWrite,      "Manage Library",        "Library", "Broad write access to all library resources"),
            (PermissionKeys.LibraryDelete,     "Delete Library",        "Library", "Delete library records"),
            (PermissionKeys.BookRead,          "View Books",            "Library", "Access book catalogue"),
            (PermissionKeys.BookWrite,         "Manage Books",          "Library", "Add or update books in the catalogue"),
            (PermissionKeys.BookDelete,        "Delete Books",          "Library", "Remove books from the catalogue"),
            (PermissionKeys.BookIssueRead,     "View Book Issues",      "Library", "Access book issue records"),
            (PermissionKeys.BookIssueWrite,    "Manage Book Issues",    "Library", "Issue books to students or staff"),
            (PermissionKeys.BookReturnRead,    "View Book Returns",     "Library", "Access book return records"),
            (PermissionKeys.BookReturnWrite,   "Process Book Returns",  "Library", "Record book returns"),

            (PermissionKeys.MpesaInitiate,         "Initiate M-Pesa Payments",  "Finance - M-Pesa", "Start M-Pesa STK push transactions"),
            (PermissionKeys.MpesaViewTransactions, "View M-Pesa Transactions",  "Finance - M-Pesa", "View M-Pesa payment history and status"),
            (PermissionKeys.MpesaRefund,           "Process M-Pesa Refunds",    "Finance - M-Pesa", "Initiate refunds for M-Pesa transactions"),
            (PermissionKeys.MpesaReconcile,        "Reconcile M-Pesa Payments", "Finance - M-Pesa", "Match M-Pesa transactions with invoices"),
        };
    }
}