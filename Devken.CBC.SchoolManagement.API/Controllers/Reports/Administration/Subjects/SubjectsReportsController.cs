// Api/Controllers/Reports/Administration/Subjects/SubjectsReportsController.cs
using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Reports;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.API.Controllers.Reports.Administration.Subjects
{
    [Route("api/reports/[controller]")]
    [ApiController]
    [Authorize]
    public class SubjectsReportsController : BaseApiController
    {
        private readonly IReportService _reportService;

        public SubjectsReportsController(
            IReportService reportService,
            IUserActivityService? activityService = null,
            ILogger<SubjectsReportsController>? logger = null)
            : base(activityService, logger)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// Downloads the Subjects List PDF.
        ///
        ///  • SuperAdmin, no schoolId  → all-schools report (extra School column)
        ///  • SuperAdmin, with schoolId → scoped to that specific school
        ///  • Regular user             → always scoped to their own school
        /// </summary>
        [HttpGet("subjects-list")]
        public async Task<IActionResult> DownloadSubjectsList([FromQuery] Guid? schoolId)
        {
            try
            {
                bool isSuperAdmin = IsSuperAdmin;
                var userSchoolId = GetUserSchoolIdOrNull();

                if (isSuperAdmin)
                {
                    if (!schoolId.HasValue)
                    {
                        // All-schools report
                        var pdf = await _reportService.GenerateAllSchoolsSubjectsListReportAsync();

                        await LogUserActivityAsync(
                            "report.download.subjects_list.all_schools",
                            "[SuperAdmin] Downloaded subjects list for ALL schools");

                        return PdfFile(pdf, "Subjects_List_AllSchools");
                    }
                    else
                    {
                        // Single school chosen by SuperAdmin
                        var pdf = await _reportService.GenerateSubjectsListReportAsync(
                            schoolId: schoolId,
                            userSchoolId: null,
                            isSuperAdmin: true);

                        await LogUserActivityAsync(
                            "report.download.subjects_list",
                            $"[SuperAdmin] Downloaded subjects list for schoolId={schoolId.Value}");

                        return PdfFile(pdf, "Subjects_List");
                    }
                }
                else
                {
                    // Regular user — validate they're not requesting another school's data
                    if (schoolId.HasValue)
                    {
                        var forbidden = ValidateSchoolAccess(schoolId.Value);
                        if (forbidden != null)
                            return forbidden;
                    }

                    var finalSchoolId = schoolId ?? userSchoolId;
                    if (!finalSchoolId.HasValue)
                        return UnauthorizedResponse("School context is required.");

                    var pdf = await _reportService.GenerateSubjectsListReportAsync(
                        schoolId: finalSchoolId,
                        userSchoolId: userSchoolId,
                        isSuperAdmin: false);

                    await LogUserActivityAsync(
                        "report.download.subjects_list",
                        $"Downloaded subjects list for schoolId={finalSchoolId.Value}");

                    return PdfFile(pdf, "Subjects_List");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return InternalServerErrorResponse(ex.Message);
            }
        }

        private FileContentResult PdfFile(byte[] bytes, string baseName) =>
            File(bytes, "application/pdf",
                $"{baseName}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
    }
}