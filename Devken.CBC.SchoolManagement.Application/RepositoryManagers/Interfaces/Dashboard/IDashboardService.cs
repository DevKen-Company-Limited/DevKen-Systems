using Devken.CBC.SchoolManagement.Application.DTOs.Dashboard;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Dashboard
{
    public interface IDashboardService
    {
        Task<DashboardResponse> GetDashboardAsync(
            DashboardQueryParams query,
            DashboardPermissions permissions,
            Guid userId,
            Guid? userSchoolId,
            bool isSuperAdmin);

        Task<StatsSection> GetStatsAsync(
            DashboardQueryParams query,
            DashboardPermissions permissions,
            Guid? userSchoolId,
            bool isSuperAdmin);

        Task<ClassPerformanceSection> GetClassPerformanceAsync(
            DashboardQueryParams query,
            Guid? userSchoolId,
            bool isSuperAdmin,
            bool isClassTeacher,
            Guid userId);

        Task<CompetencySection> GetCompetencyAsync(
            DashboardQueryParams query,
            Guid? userSchoolId,
            bool isSuperAdmin);

        Task<RecentAssessmentsSection> GetRecentAssessmentsAsync(
            DashboardQueryParams query,
            Guid? userSchoolId,
            bool isSuperAdmin,
            bool isClassTeacher,
            Guid userId);

        Task<EventsSection> GetEventsAsync(
            DashboardQueryParams query,
            Guid? userSchoolId,
            bool isSuperAdmin);

        Task<FeeCollectionSection> GetFeeCollectionAsync(
            DashboardQueryParams query,
            Guid? userSchoolId,
            bool isSuperAdmin);

        Task<QuickActionsSection> GetQuickActionsAsync(
            Guid? userSchoolId,
            bool isSuperAdmin,
            ClaimsPrincipal caller);
    }
}