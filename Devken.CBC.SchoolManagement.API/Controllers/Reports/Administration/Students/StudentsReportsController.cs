using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Reports;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.API.Controllers.Reports.Administration.Students
{
    [Route("api/reports/[controller]")]
    [ApiController]
    [Authorize]
    public class StudentsReportsController : BaseApiController
    {
        private readonly IReportService _reportService;

        public StudentsReportsController(
            IReportService reportService,
            IUserActivityService? activityService = null,
            ILogger<StudentsReportsController>? logger = null)
            : base(activityService, logger)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// Downloads the Students List PDF.
        ///
        ///  • SuperAdmin, no schoolId  → all-schools report (extra School column)
        ///  • SuperAdmin, with schoolId → scoped to that specific school
        ///  • Regular user             → always scoped to their own school (403 if they try another)
        /// </summary>
        [HttpGet("students-list")]
        public async Task<IActionResult> DownloadStudentsList([FromQuery] Guid? schoolId)
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
                        var pdf = await _reportService.GenerateAllSchoolsStudentsListReportAsync();

                        await LogUserActivityAsync(
                            "report.download.students_list.all_schools",
                            "[SuperAdmin] Downloaded students list for ALL schools");

                        return PdfFile(pdf, "Students_List_AllSchools");
                    }
                    else
                    {
                        // Single school chosen by SuperAdmin
                        var pdf = await _reportService.GenerateStudentsListReportAsync(
                            schoolId: schoolId,
                            userSchoolId: null,
                            isSuperAdmin: true);

                        await LogUserActivityAsync(
                            "report.download.students_list",
                            $"[SuperAdmin] Downloaded students list for schoolId={schoolId.Value}");

                        return PdfFile(pdf, "Students_List");
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

                    var pdf = await _reportService.GenerateStudentsListReportAsync(
                        schoolId: finalSchoolId,
                        userSchoolId: userSchoolId,
                        isSuperAdmin: false);

                    await LogUserActivityAsync(
                        "report.download.students_list",
                        $"Downloaded students list for schoolId={finalSchoolId.Value}");

                    return PdfFile(pdf, "Students_List");
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