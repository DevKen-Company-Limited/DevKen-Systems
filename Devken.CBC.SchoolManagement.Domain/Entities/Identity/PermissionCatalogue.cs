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
            (PermissionKeys.UserRead,     "View Users",             "Administration", "List and view user accounts"),
            (PermissionKeys.UserWrite,    "Create / Edit Users",    "Administration", "Create new users or update existing ones"),
            (PermissionKeys.UserDelete,   "Delete Users",           "Administration", "Permanently remove a user account"),
            (PermissionKeys.RoleRead,     "View Roles",             "Administration", "List roles and their permissions"),
            (PermissionKeys.RoleWrite,    "Create / Edit Roles",    "Administration", "Add or modify roles"),
            (PermissionKeys.RoleDelete,   "Delete Roles",           "Administration", "Remove a role from the system"),

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
        };
    }
}
