using Devken.CBC.SchoolManagement.Application.DTOs.Dashboard;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Dashboard;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Dashboard
{
    public sealed class DashboardService : IDashboardService
    {
        private readonly IRepositoryManager _repo;

        public DashboardService(IRepositoryManager repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<DashboardResponse> GetDashboardAsync(
            DashboardQueryParams query,
            DashboardPermissions permissions,
            Guid userId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            // null  →  SuperAdmin with no school filter (system-wide aggregate)
            // Guid  →  scoped to that specific school
            var schoolId = ResolveSchoolId(query.SchoolId, userSchoolId, isSuperAdmin);

            var response = new DashboardResponse
            {
                Permissions = permissions,
                ActiveLevel = query.Level ?? "All Levels",
                SchoolName = schoolId.HasValue ? await GetSchoolNameAsync(schoolId.Value) : "All Schools",
                AcademicYear = schoolId.HasValue ? await GetCurrentAcademicYearLabelAsync(schoolId.Value) : string.Empty,
                CurrentTerm = await GetCurrentTermLabelAsync(schoolId, query.TermId),
            };

            if (permissions.CanViewStats)
                response.Stats = await GetStatsAsync(query, permissions, userSchoolId, isSuperAdmin);

            if (permissions.CanViewClassPerformance)
                response.ClassPerformance = await GetClassPerformanceAsync(
                    query, userSchoolId, isSuperAdmin,
                    IsClassTeacherFromPermissions(permissions), userId);

            if (permissions.CanViewCompetency)
                response.Competency = await GetCompetencyAsync(query, userSchoolId, isSuperAdmin);

            if (permissions.CanViewRecentAssessments)
                response.RecentAssessments = await GetRecentAssessmentsAsync(
                    query, userSchoolId, isSuperAdmin,
                    IsClassTeacherFromPermissions(permissions), userId);

            if (permissions.CanViewEvents)
                response.Events = await GetEventsAsync(query, userSchoolId, isSuperAdmin);

            if (permissions.CanViewFeeCollection)
                response.FeeCollection = await GetFeeCollectionAsync(query, userSchoolId, isSuperAdmin);

            if (permissions.CanViewQuickActions)
                response.QuickActions = await GetQuickActionsAsync(userSchoolId, isSuperAdmin, null!);

            return response;
        }

        public async Task<StatsSection> GetStatsAsync(
            DashboardQueryParams query,
            DashboardPermissions permissions,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var schoolId = ResolveSchoolId(query.SchoolId, userSchoolId, isSuperAdmin);
            var section = new StatsSection();

            if (permissions.CanViewStats)
            {
                var studentCount = await _repo.Student
                    .FindByCondition(s => (!schoolId.HasValue || s.Id == schoolId.Value) && s.IsActive, false)
                    .CountAsync();

                section.EnrolledStudents = new StatCard
                {
                    Icon = "👥",
                    Value = studentCount.ToString("N0"),
                    Label = "Enrolled Students",
                    Trend = "Current enrolment",
                };
            }

            if (permissions.CanViewStats)
            {
                var staffCount = await _repo.Teacher
                    .FindByCondition(t => (!schoolId.HasValue || t.Id == schoolId.Value) && t.IsActive, false)
                    .CountAsync();

                section.TeachingStaff = new StatCard
                {
                    Icon = "🎓",
                    Value = staffCount.ToString(),
                    Label = "Teaching Staff",
                    Trend = "Active teachers",
                };
            }

            if (permissions.CanViewStats)
            {
                var pendingCount = await _repo.FormativeAssessment
                    .FindByCondition(a => (!schoolId.HasValue || a.         Id   == schoolId.Value) && !a.IsPublished, false)
                    .CountAsync();

                section.AssessmentsPending = new StatCard
                {
                    Icon = "📋",
                    Value = pendingCount.ToString(),
                    Label = "Assessments Pending",
                    Trend = pendingCount > 0 ? $"{pendingCount} awaiting publish" : "All up to date",
                };
            }

            if (permissions.CanViewStats)
            {
                var totalInvoiced = await _repo.Invoice
                    .FindByCondition(i => !schoolId.HasValue || i.                          Id == schoolId.Value, false)
                    .SumAsync(i => (decimal?)i.TotalAmount) ?? 0;

                var totalPaid = await _repo.Payment
                    .FindByCondition(p => !schoolId.HasValue || p.Id == schoolId.Value, false)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0;

                var rate = totalInvoiced > 0
                    ? (int)Math.Round(totalPaid / totalInvoiced * 100)
                    : 0;

                section.FeeCollectionRate = new StatCard
                {
                    Icon = "💰",
                    Value = $"{rate}%",
                    Label = "Fee Collection Rate",
                    Trend = rate < 100 ? $"{100 - rate}% gap remaining" : "Target met",
                };
            }

            return section;
        }

        public async Task<ClassPerformanceSection> GetClassPerformanceAsync(
            DashboardQueryParams query,
            Guid? userSchoolId,
            bool isSuperAdmin,
            bool isClassTeacher,
            Guid userId)
        {
            var schoolId = ResolveSchoolId(query.SchoolId, userSchoolId, isSuperAdmin);

            var classesQuery = _repo.Class
                .FindByCondition(
                    c => (!schoolId.HasValue || c.Id == schoolId.Value) && c.IsActive, false);

            if (isClassTeacher)
                classesQuery = classesQuery.Where(c => c.TeacherId == userId);

            //if (!string.IsNullOrWhiteSpace(query.Level) && query.Level != "All Levels")
            //    classesQuery = classesQuery.Where(c => c.Level == query.Level);

            var classes = await classesQuery
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    //c.Badge,
                    //TeacherName = c.Teacher != null ? c.Teacher.FullName : string.Empty,
                    //StudentCount = c.Students.Count(s => !s.IsDeleted),
                    //c.Color,
                })
                .ToListAsync();

            var items = new List<ClassPerformanceItem>();

            foreach (var cls in classes)
            {
                var totalScores = await _repo.FormativeAssessmentScore
                    .FindByCondition(s => s.Id == cls.Id, false)
                    .Select(s => (double?)s.Score)
                    .ToListAsync();

                var pct = totalScores.Count > 0 && totalScores.Any(s => s.HasValue)
                    ? (int)Math.Round(totalScores.Where(s => s.HasValue).Average(s => s!.Value))
                    : 0;

                items.Add(new ClassPerformanceItem
                {
                    ClassId = cls.Id,
                    //Badge = cls.Badge ?? cls.Name[..Math.Min(4, cls.Name.Length)],
                    ClassName = cls.Name,
                    //Meta = $"{cls.StudentCount} students · {cls.TeacherName}",
                    Pct = pct,
                    //Color = cls.Color ?? "#2563EB",
                });
            }

            return new ClassPerformanceSection
            {
                TermLabel = await GetCurrentTermLabelAsync(schoolId, query.TermId),
                Classes = items.OrderByDescending(c => c.Pct).ToList(),
            };
        }

        public async Task<CompetencySection> GetCompetencyAsync(
            DashboardQueryParams query,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var schoolId = ResolveSchoolId(query.SchoolId, userSchoolId, isSuperAdmin);

            var scores = await _repo.CompetencyAssessmentScore
                .FindByCondition(s => !schoolId.HasValue || s.Id == schoolId.Value, false)
                .Select(s => s.CompetencyLevel)
                .ToListAsync();

            var total = scores.Count;
            if (total == 0)
            {
                return new CompetencySection
                {
                    Items = new List<CompetencyItem>
                    {
                        new() { Label = "Exceeding Expectations",   Code = "EE", Pct = 0, Color = "#2563EB" },
                        new() { Label = "Meeting Expectations",     Code = "ME", Pct = 0, Color = "#16a34a" },
                        new() { Label = "Approaching Expectations", Code = "AE", Pct = 0, Color = "#d97706" },
                        new() { Label = "Below Expectations",       Code = "BE", Pct = 0, Color = "#dc2626" },
                    }
                };
            }

            int Pct(string level) =>
                (int)Math.Round(scores.Count(s => s == level) / (double)total * 100);

            return new CompetencySection
            {
                Items = new List<CompetencyItem>
                {
                    new() { Label = "Exceeding Expectations",   Code = "EE", Pct = Pct("EE"), Color = "#2563EB" },
                    new() { Label = "Meeting Expectations",     Code = "ME", Pct = Pct("ME"), Color = "#16a34a" },
                    new() { Label = "Approaching Expectations", Code = "AE", Pct = Pct("AE"), Color = "#d97706" },
                    new() { Label = "Below Expectations",       Code = "BE", Pct = Pct("BE"), Color = "#dc2626" },
                }
            };
        }

        public async Task<RecentAssessmentsSection> GetRecentAssessmentsAsync(
            DashboardQueryParams query,
            Guid? userSchoolId,
            bool isSuperAdmin,
            bool isClassTeacher,
            Guid userId)
        {
            var schoolId = ResolveSchoolId(query.SchoolId, userSchoolId, isSuperAdmin);

            var formativeQuery = _repo.FormativeAssessment
                .FindByCondition(a => !schoolId.HasValue || a.Id == schoolId.Value, false);

            if (isClassTeacher)
                formativeQuery = formativeQuery.Where(a => a.TeacherId == userId);

            var formative = await formativeQuery
                .OrderByDescending(a => a.AssessmentDate)
                .Take(10)
                .Select(a => new AssessmentRow
                {
                    AssessmentId = a.Id,
                    //StudentName = a.Student != null ? a.Student.FullName : string.Empty,
                    ClassName = a.Class != null ? a.Class.Name : string.Empty,
                    LearningArea = a.Subject != null ? a.Subject.Name : string.Empty,
                    AssessmentType = "Formative",
                    //CompetencyLevel = a.CompetencyLevel ?? string.Empty,
                    AssessmentDate = a.AssessmentDate,
                    TeacherName = a.Teacher != null ? a.Teacher.FullName : string.Empty,
                })
                .ToListAsync();

            var summativeQuery = _repo.SummativeAssessment
                .FindByCondition(a => !schoolId.HasValue || a.Id == schoolId.Value, false);

            if (isClassTeacher)
                summativeQuery = summativeQuery.Where(a => a.TeacherId == userId);

            var summative = await summativeQuery
                .OrderByDescending(a => a.AssessmentDate)
                .Take(10)
                .Select(a => new AssessmentRow
                {
                    AssessmentId = a.Id,
                    //StudentName = a.Student != null ? a.Student.FullName : string.Empty,
                    ClassName = a.Class != null ? a.Class.Name : string.Empty,
                    LearningArea = a.Subject != null ? a.Subject.Name : string.Empty,
                    AssessmentType = "Summative",
                    //CompetencyLevel = a.CompetencyLevel ?? string.Empty,
                    AssessmentDate = a.AssessmentDate,
                    TeacherName = a.Teacher != null ? a.Teacher.FullName : string.Empty,
                })
                .ToListAsync();

            var combined = formative
                .Concat(summative)
                .OrderByDescending(a => a.AssessmentDate)
                .Take(20)
                .ToList();

            return new RecentAssessmentsSection { Items = combined };
        }

        public async Task<EventsSection> GetEventsAsync(
            DashboardQueryParams query,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var schoolId = ResolveSchoolId(query.SchoolId, userSchoolId, isSuperAdmin);

            //var events = await _repo.SchoolEvent
            //    .FindByCondition(
            //        e => (!schoolId.HasValue || e.Id == schoolId.Value)
            //             && e.Date >= DateTime.UtcNow.Date, false)
            //    .OrderBy(e => e.Date)
            //    .Take(5)
            //    .Select(e => new EventItem
            //    {
            //        EventId = e.Id,
            //        Title = e.Title,
            //        SubTitle = e.SubTitle ?? string.Empty,
            //        Date = e.Date,
            //        Tag = e.Tag ?? string.Empty,
            //    })
            //    .ToListAsync();

            return new EventsSection { };
        }

        public async Task<FeeCollectionSection> GetFeeCollectionAsync(
            DashboardQueryParams query,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var schoolId = ResolveSchoolId(query.SchoolId, userSchoolId, isSuperAdmin);
            var termId = query.TermId;

            var invoicesQuery = _repo.Invoice
                .FindByCondition(i => !schoolId.HasValue || i.Id == schoolId.Value, false);

            if (termId.HasValue)
                invoicesQuery = invoicesQuery.Where(i => i.TermId == termId.Value);

            var totalExpected = await invoicesQuery
                .SumAsync(i => (decimal?)i.TotalAmount) ?? 0;

            var paymentsQuery = _repo.Payment
                .FindByCondition(p => !schoolId.HasValue || p.Id         == schoolId.Value, false);

            //if (termId.HasValue)
            //    paymentsQuery = paymentsQuery.Where(p => p.TermId == termId.Value);

            var totalCollected = await paymentsQuery
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var outstanding = totalExpected - totalCollected;
            var collectedPct = totalExpected > 0
                ? (int)Math.Round(totalCollected / totalExpected * 100)
                : 0;

            var defaulterCount = await invoicesQuery
                .Where(i => i.Balance > 0)
                .Select(i => i.StudentId)
                .Distinct()
                .CountAsync();

            return new FeeCollectionSection
            {
                CollectedPct = collectedPct,
                ExpectedTotal = FormatKsh(totalExpected),
                CollectedTotal = FormatKsh(totalCollected),
                OutstandingTotal = FormatKsh(outstanding),
                DefaulterCount = defaulterCount,
            };
        }

        public Task<QuickActionsSection> GetQuickActionsAsync(
            Guid? userSchoolId,
            bool isSuperAdmin,
            ClaimsPrincipal caller)
        {
            bool Has(string permission) =>
                caller?.HasClaim("permission", permission) ?? isSuperAdmin;

            var items = new List<QuickActionItem>
            {
                new() { Icon = "📝", Label = "Record Assessment", Action = "assessment.record", Enabled = Has(PermissionKeys.AssessmentWrite)  },
                new() { Icon = "📊", Label = "Generate Report",   Action = "report.generate",   Enabled = Has(PermissionKeys.ReportWrite)       },
                new() { Icon = "👤", Label = "Add Student",       Action = "student.add",       Enabled = Has(PermissionKeys.StudentWrite)      },
                new() { Icon = "📚", Label = "Lesson Plan",       Action = "lessonplan.create", Enabled = Has(PermissionKeys.LessonPlanWrite)   },
            };

            return Task.FromResult(new QuickActionsSection { Items = items });
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Returns null  → SuperAdmin with no schoolId supplied; all queries skip the school filter (system-wide).
        /// Returns Guid  → queries are scoped to that school.
        /// </summary>
        private static Guid? ResolveSchoolId(Guid? querySchoolId, Guid? userSchoolId, bool isSuperAdmin)
            => isSuperAdmin
                ? (querySchoolId == Guid.Empty ? null : querySchoolId)
                : userSchoolId;

        private static bool IsClassTeacherFromPermissions(DashboardPermissions permissions)
            => permissions.CanViewClassPerformance
               && !permissions.CanViewFeeCollection
               && !permissions.CanViewCompetency;

        private async Task<string> GetSchoolNameAsync(Guid schoolId)
        {
            var school = await _repo.School
                .FindByCondition(s => s.Id == schoolId, false)
                .Select(s => s.Name)
                .FirstOrDefaultAsync();
            return school ?? string.Empty;
        }

        private async Task<string> GetCurrentAcademicYearLabelAsync(Guid schoolId)
        {
            var year = await _repo.AcademicYear
                .FindByCondition(y => y.Id == schoolId && y.IsActive, false)
                .Select(y => y.Name)
                .FirstOrDefaultAsync();
            return year ?? string.Empty;
        }

        // Accepts Guid? so callers with a null schoolId (system-wide) compile without casting
        private async Task<string> GetCurrentTermLabelAsync(Guid? schoolId, Guid? termId)
        {
            var query = _repo.Term
                .FindByCondition(t => !schoolId.HasValue || t.Id == schoolId.Value, false);

            if (termId.HasValue)
                query = query.Where(t => t.Id == termId.Value);
            else
                query = query.Where(t => t.IsCurrent);

            var term = await query.Select(t => t.Name).FirstOrDefaultAsync();
            return term ?? string.Empty;
        }

        private static string FormatKsh(decimal amount)
        {
            if (amount >= 1_000_000) return $"KSh {amount / 1_000_000:0.##}M";
            if (amount >= 1_000) return $"KSh {amount / 1_000:0.##}K";
            return $"KSh {amount:N0}";
        }
    }
}