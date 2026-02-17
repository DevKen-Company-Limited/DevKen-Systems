using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Reports;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
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

        // Pass activityService and logger to base controller if needed
        public StudentsReportsController(
            IReportService reportService,
            IUserActivityService? activityService = null,
            ILogger<StudentsReportsController>? logger = null)
            : base(activityService, logger)
        {
            _reportService = reportService;
        }

        [HttpGet("students-list")]
        public async Task<IActionResult> DownloadStudentsList([FromQuery] Guid? schoolId)
        {
            try
            {
                bool isSuperAdmin = IsSuperAdmin;
                var userSchoolId = GetUserSchoolIdOrNull();

                // Validate access for non-SuperAdmin users
                if (!isSuperAdmin && schoolId.HasValue)
                {
                    var forbiddenResult = ValidateSchoolAccess(schoolId.Value);
                    if (forbiddenResult != null)
                        return forbiddenResult;
                }

                // Use provided schoolId or current user's school
                var finalSchoolId = schoolId ?? userSchoolId;

                if (!finalSchoolId.HasValue)
                    throw new UnauthorizedAccessException("School context is required.");

                // Generate the PDF
                var pdfBytes = await _reportService.GenerateStudentsListReportAsync(
                    finalSchoolId.Value, userSchoolId, isSuperAdmin);

                // Log the download activity
                await LogUserActivityAsync(
                    activityType: "report.download.students_list",
                    details: $"Downloaded students list report for schoolId={finalSchoolId.Value}");

                // Return PDF file
                return File(
                    pdfBytes,
                    "application/pdf",
                    $"Students_List_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
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

    }
}
