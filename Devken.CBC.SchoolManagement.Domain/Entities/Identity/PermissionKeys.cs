using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Identity
{
    public static class PermissionKeys
    {
        // ── Administration ─────────────────────────────────
        public const string SchoolRead = "School.Read";
        public const string SchoolWrite = "School.Write";
        public const string UserRead = "User.Read";
        public const string UserWrite = "User.Write";
        public const string UserDelete = "User.Delete";
        public const string RoleRead = "Role.Read";
        public const string RoleWrite = "Role.Write";
        public const string RoleDelete = "Role.Delete";

        // ── Academic ───────────────────────────────────────
        public const string StudentRead = "Student.Read";
        public const string StudentWrite = "Student.Write";
        public const string StudentDelete = "Student.Delete";
        public const string TeacherRead = "Teacher.Read";
        public const string TeacherWrite = "Teacher.Write";
        public const string TeacherDelete = "Teacher.Delete";
        public const string SubjectRead = "Subject.Read";
        public const string SubjectWrite = "Subject.Write";
        public const string ClassRead = "Class.Read";
        public const string ClassWrite = "Class.Write";
        public const string GradeRead = "Grade.Read";
        public const string GradeWrite = "Grade.Write";

        // ── Assessment ─────────────────────────────────────
        public const string AssessmentRead = "Assessment.Read";
        public const string AssessmentWrite = "Assessment.Write";
        public const string AssessmentDelete = "Assessment.Delete";
        public const string ReportRead = "Report.Read";
        public const string ReportWrite = "Report.Write";

        // ── Finance ────────────────────────────────────────
        public const string FeeRead = "Fee.Read";
        public const string FeeWrite = "Fee.Write";
        public const string PaymentRead = "Payment.Read";
        public const string PaymentWrite = "Payment.Write";
        public const string InvoiceRead = "Invoice.Read";
        public const string InvoiceWrite = "Invoice.Write";

        // ── Curriculum ─────────────────────────────────────
        public const string CurriculumRead = "Curriculum.Read";
        public const string CurriculumWrite = "Curriculum.Write";
        public const string LessonPlanRead = "LessonPlan.Read";
        public const string LessonPlanWrite = "LessonPlan.Write";
    }

}
