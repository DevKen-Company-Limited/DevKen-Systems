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

        // ─────────────────────────────────────────────────────────────────────
        // GetDashboardAsync
        // ─────────────────────────────────────────────────────────────────────
        public async Task<DashboardResponse> GetDashboardAsync(
            DashboardQueryParams query,
            DashboardPermissions permissions,
            Guid userId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
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

        // ─────────────────────────────────────────────────────────────────────
        // GetStatsAsync
        // ─────────────────────────────────────────────────────────────────────
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
                // All TenantBaseEntity subclasses carry TenantId which equals School.Id.
                // School itself extends BaseEntity (not TenantBaseEntity) so it has no TenantId —
                // it IS the tenant root; its own Id is used directly in GetSchoolNameAsync.

                var studentCount = await _repo.Student
                    .FindByCondition(
                        s => (!schoolId.HasValue || s.TenantId == schoolId.Value) && s.IsActive,
                        trackChanges: false)
                    .CountAsync();

                section.EnrolledStudents = new StatCard
                {
                    Icon = "👥",
                    Value = studentCount.ToString("N0"),
                    Label = "Enrolled Students",
                    Trend = "Current enrolment",
                };

                var staffCount = await _repo.Teacher
                    .FindByCondition(
                        t => (!schoolId.HasValue || t.TenantId == schoolId.Value) && t.IsActive,
                        trackChanges: false)
                    .CountAsync();

                section.TeachingStaff = new StatCard
                {
                    Icon = "🎓",
                    Value = staffCount.ToString(),
                    Label = "Teaching Staff",
                    Trend = "Active teachers",
                };

                var pendingCount = await _repo.FormativeAssessment
                    .FindByCondition(
                        a => (!schoolId.HasValue || a.TenantId == schoolId.Value) && !a.IsPublished,
                        trackChanges: false)
                    .CountAsync();

                section.AssessmentsPending = new StatCard
                {
                    Icon = "📋",
                    Value = pendingCount.ToString(),
                    Label = "Assessments Pending",
                    Trend = pendingCount > 0 ? $"{pendingCount} awaiting publish" : "All up to date",
                };

                // Net expected = TotalAmount - DiscountAmount, matching Invoice.Balance definition.
                // AmountPaid and Balance are [NotMapped], so we must expand them here.
                var totalInvoiced = await _repo.Invoice
                    .FindByCondition(
                        i => !schoolId.HasValue || i.TenantId == schoolId.Value,
                        trackChanges: false)
                    .SumAsync(i => (decimal?)(i.TotalAmount - i.DiscountAmount)) ?? 0;

                var totalPaid = await _repo.Payment
                    .FindByCondition(
                        p => !schoolId.HasValue || p.TenantId == schoolId.Value,
                        trackChanges: false)
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

        // ─────────────────────────────────────────────────────────────────────
        // GetClassPerformanceAsync
        // ─────────────────────────────────────────────────────────────────────
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
                    c => (!schoolId.HasValue || c.TenantId == schoolId.Value) && c.IsActive,
                    trackChanges: false);

            // Class teachers are scoped to only the class(es) they own.
            // Class.TeacherId is the FK backing the ClassTeacher navigation property.
            if (isClassTeacher)
                classesQuery = classesQuery.Where(c => c.TeacherId == userId);

            var classes = await classesQuery
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            var items = new List<ClassPerformanceItem>();

            foreach (var cls in classes)
            {
                // FormativeAssessmentScore has no direct ClassId column.
                // Navigate through the parent FormativeAssessment to reach ClassId.
                var totalScores = await _repo.FormativeAssessmentScore
                    .FindByCondition(
                        s => s.FormativeAssessment.ClassId == cls.Id,
                        trackChanges: false)
                    .Select(s => (double?)s.Score)
                    .ToListAsync();

                var pct = totalScores.Count > 0 && totalScores.Any(s => s.HasValue)
                    ? (int)Math.Round(totalScores.Where(s => s.HasValue).Average(s => s!.Value))
                    : 0;

                items.Add(new ClassPerformanceItem
                {
                    ClassId = cls.Id,
                    ClassName = cls.Name,
                    Pct = pct,
                });
            }

            return new ClassPerformanceSection
            {
                TermLabel = await GetCurrentTermLabelAsync(schoolId, query.TermId),
                Classes = items.OrderByDescending(c => c.Pct).ToList(),
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // GetCompetencyAsync
        // ─────────────────────────────────────────────────────────────────────
        public async Task<CompetencySection> GetCompetencyAsync(
            DashboardQueryParams query,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var schoolId = ResolveSchoolId(query.SchoolId, userSchoolId, isSuperAdmin);

            // Project the persisted Rating column ("Exceeds" | "Meets" | "Approaching" | "Below").
            // CompetencyLevel is a computed property decorated with Ignore() in Fluent API
            // and therefore cannot be translated to SQL.
            var ratings = await _repo.CompetencyAssessmentScore
                .FindByCondition(
                    s => !schoolId.HasValue || s.TenantId == schoolId.Value,
                    trackChanges: false)
                .Select(s => s.Rating)
                .ToListAsync();

            var total = ratings.Count;

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

            // Match against the actual Rating strings stored in CompetencyAssessmentScore.
            int Pct(string ratingValue) =>
                (int)Math.Round(ratings.Count(r => r == ratingValue) / (double)total * 100);

            return new CompetencySection
            {
                Items = new List<CompetencyItem>
                {
                    new() { Label = "Exceeding Expectations",   Code = "EE", Pct = Pct("Exceeds"),    Color = "#2563EB" },
                    new() { Label = "Meeting Expectations",     Code = "ME", Pct = Pct("Meets"),       Color = "#16a34a" },
                    new() { Label = "Approaching Expectations", Code = "AE", Pct = Pct("Approaching"), Color = "#d97706" },
                    new() { Label = "Below Expectations",       Code = "BE", Pct = Pct("Below"),       Color = "#dc2626" },
                }
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // GetRecentAssessmentsAsync
        // ─────────────────────────────────────────────────────────────────────
        public async Task<RecentAssessmentsSection> GetRecentAssessmentsAsync(
            DashboardQueryParams query,
            Guid? userSchoolId,
            bool isSuperAdmin,
            bool isClassTeacher,
            Guid userId)
        {
            var schoolId = ResolveSchoolId(query.SchoolId, userSchoolId, isSuperAdmin);

            var formativeQuery = _repo.FormativeAssessment
                .FindByCondition(
                    a => !schoolId.HasValue || a.TenantId == schoolId.Value,
                    trackChanges: false);

            if (isClassTeacher)
                formativeQuery = formativeQuery.Where(a => a.TeacherId == userId);

            var formative = await formativeQuery
                .OrderByDescending(a => a.AssessmentDate)
                .Take(10)
                .Select(a => new AssessmentRow
                {
                    AssessmentId = a.Id,
                    ClassName = a.Class != null ? a.Class.Name : string.Empty,
                    LearningArea = a.Subject != null ? a.Subject.Name : string.Empty,
                    AssessmentType = "Formative",
                    AssessmentDate = a.AssessmentDate,
                    TeacherName = a.Teacher != null ? a.Teacher.FullName : string.Empty,
                })
                .ToListAsync();

            var summativeQuery = _repo.SummativeAssessment
                .FindByCondition(
                    a => !schoolId.HasValue || a.TenantId == schoolId.Value,
                    trackChanges: false);

            if (isClassTeacher)
                summativeQuery = summativeQuery.Where(a => a.TeacherId == userId);

            var summative = await summativeQuery
                .OrderByDescending(a => a.AssessmentDate)
                .Take(10)
                .Select(a => new AssessmentRow
                {
                    AssessmentId = a.Id,
                    ClassName = a.Class != null ? a.Class.Name : string.Empty,
                    LearningArea = a.Subject != null ? a.Subject.Name : string.Empty,
                    AssessmentType = "Summative",
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

        // ─────────────────────────────────────────────────────────────────────
        // GetEventsAsync  (scaffold — events repo not yet wired)
        // ─────────────────────────────────────────────────────────────────────
        public Task<EventsSection> GetEventsAsync(
            DashboardQueryParams query,
            Guid? userSchoolId,
            bool isSuperAdmin)
            => Task.FromResult(new EventsSection());

        // ─────────────────────────────────────────────────────────────────────
        // GetFeeCollectionAsync
        // ─────────────────────────────────────────────────────────────────────
        public async Task<FeeCollectionSection> GetFeeCollectionAsync(
            DashboardQueryParams query,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var schoolId = ResolveSchoolId(query.SchoolId, userSchoolId, isSuperAdmin);
            var termId = query.TermId;

            var invoicesQuery = _repo.Invoice
                .FindByCondition(
                    i => !schoolId.HasValue || i.TenantId == schoolId.Value,
                    trackChanges: false);

            if (termId.HasValue)
                invoicesQuery = invoicesQuery.Where(i => i.TermId == termId.Value);

            // Net expected = TotalAmount - DiscountAmount, matching Invoice.Balance definition.
            var totalExpected = await invoicesQuery
                .SumAsync(i => (decimal?)(i.TotalAmount - i.DiscountAmount)) ?? 0;

            var paymentsQuery = _repo.Payment
                .FindByCondition(
                    p => !schoolId.HasValue || p.TenantId == schoolId.Value,
                    trackChanges: false);

            var totalCollected = await paymentsQuery
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var outstanding = totalExpected - totalCollected;
            var collectedPct = totalExpected > 0
                ? (int)Math.Round(totalCollected / totalExpected * 100)
                : 0;

            // Invoice.Balance and Invoice.AmountPaid are both [NotMapped] so EF Core
            // cannot reference them in a query.  Expand Balance to its definition:
            //   Balance = TotalAmount - DiscountAmount - Payments.Sum(p => p.Amount)
            // EF Core translates the navigation .Sum() as a correlated subquery,
            // so this remains fully server-side with no client evaluation needed.
            var defaulterCount = await invoicesQuery
                .Where(i => i.TotalAmount - i.DiscountAmount - i.Payments.Sum(p => p.Amount) > 0)
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

        // ─────────────────────────────────────────────────────────────────────
        // GetQuickActionsAsync
        // ─────────────────────────────────────────────────────────────────────
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

        // ─────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns null  → SuperAdmin with no schoolId supplied; all queries skip the school filter.
        /// Returns Guid  → queries are scoped to that school (matched against TenantId).
        /// </summary>
        private static Guid? ResolveSchoolId(Guid? querySchoolId, Guid? userSchoolId, bool isSuperAdmin)
            => isSuperAdmin
                ? (querySchoolId == Guid.Empty ? null : querySchoolId)
                : userSchoolId;

        /// <summary>
        /// Heuristic: a class teacher can see class performance and recent assessments
        /// but not fee collection or competency — both of which require broader privileges.
        /// </summary>
        private static bool IsClassTeacherFromPermissions(DashboardPermissions permissions)
            => permissions.CanViewClassPerformance
            && !permissions.CanViewFeeCollection
            && !permissions.CanViewCompetency;

        private async Task<string> GetSchoolNameAsync(Guid schoolId)
        {
            // School extends BaseEntity (not TenantBaseEntity) — it IS the tenant root.
            // Filter by its own primary key, not by TenantId.
            var name = await _repo.School
                .FindByCondition(s => s.Id == schoolId, trackChanges: false)
                .Select(s => s.Name)
                .FirstOrDefaultAsync();
            return name ?? string.Empty;
        }

        private async Task<string> GetCurrentAcademicYearLabelAsync(Guid schoolId)
        {
            // AcademicYear.IsActive is a computed property (not persisted) so EF Core
            // cannot translate it to SQL.  Expand it to its three backing columns:
            //   IsActive => !IsClosed && today >= StartDate && today <= EndDate
            var today = DateTime.Today;
            var name = await _repo.AcademicYear
                .FindByCondition(
                    y => y.TenantId == schoolId
                         && !y.IsClosed
                         && today >= y.StartDate
                         && today <= y.EndDate,
                    trackChanges: false)
                .Select(y => y.Name)
                .FirstOrDefaultAsync();
            return name ?? string.Empty;
        }

        /// <summary>
        /// Term extends TenantBaseEntity so TenantId is available directly —
        /// no join through AcademicYear is needed.
        /// When schoolId is null (system-wide SA view) the school filter is skipped entirely.
        /// </summary>
        private async Task<string> GetCurrentTermLabelAsync(Guid? schoolId, Guid? termId)
        {
            var query = _repo.Term
                .FindByCondition(
                    t => !schoolId.HasValue || t.TenantId == schoolId.Value,
                    trackChanges: false);

            query = termId.HasValue
                ? query.Where(t => t.Id == termId.Value)
                : query.Where(t => t.IsCurrent);

            var name = await query.Select(t => t.Name).FirstOrDefaultAsync();
            return name ?? string.Empty;
        }

        private static string FormatKsh(decimal amount)
        {
            if (amount >= 1_000_000) return $"KSh {amount / 1_000_000:0.##}M";
            if (amount >= 1_000) return $"KSh {amount / 1_000:0.##}K";
            return $"KSh {amount:N0}";
        }
    }
}