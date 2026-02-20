using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Assessments;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Reports;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.API.Controllers.Reports.Administration.Assessments
{
    /// <summary>
    /// Downloads Assessments List PDF reports.
    ///
    /// Routing behaviour (mirrors StudentsReportsController):
    ///   SuperAdmin, no schoolId        → all-schools report (extra School column)
    ///   SuperAdmin, with schoolId      → scoped to that specific school
    ///   Regular user                   → always scoped to their own school
    ///                                    (403 if they try a different schoolId)
    ///
    /// Optional filter:
    ///   &amp;type=Formative | Summative | Competency   (omit for all types)
    /// </summary>
    [Route("api/reports/[controller]")]
    [ApiController]
    [Authorize]
    public class AssessmentsReportsController : BaseApiController
    {
        private readonly IReportService _reportService;

        public AssessmentsReportsController(
            IReportService reportService,
            IUserActivityService? activityService = null,
            ILogger<AssessmentsReportsController>? logger = null)
            : base(activityService, logger)
        {
            _reportService = reportService
                ?? throw new ArgumentNullException(nameof(reportService));
        }

        // ─────────────────────────────────────────────────────────────────
        // GET /api/reports/assessmentsreports/assessments-list
        //     ?schoolId=<guid>          (optional — SuperAdmin only)
        //     &type=Formative            (optional filter)
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Downloads the Assessments List PDF.
        ///
        ///  • SuperAdmin, no schoolId  → all-schools report (extra School column)
        ///  • SuperAdmin, with schoolId → scoped to that specific school
        ///  • Regular user             → always scoped to their own school (403 if they try another)
        /// </summary>
        [HttpGet("assessments-list")]
        public async Task<IActionResult> DownloadAssessmentsList(
            [FromQuery] Guid? schoolId,
            [FromQuery] AssessmentTypeDto? type = null)
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
                        var pdf = await _reportService.GenerateAllSchoolsAssessmentsListReportAsync(type);

                        await LogUserActivityAsync(
                            "report.download.assessments_list.all_schools",
                            $"[SuperAdmin] Downloaded assessments list for ALL schools" +
                            (type.HasValue ? $" (type={type})" : string.Empty));

                        return PdfFile(pdf, "Assessments_List_AllSchools");
                    }
                    else
                    {
                        // Single school chosen by SuperAdmin
                        var pdf = await _reportService.GenerateAssessmentsListReportAsync(
                            schoolId: schoolId,
                            userSchoolId: null,
                            isSuperAdmin: true,
                            type: type);

                        await LogUserActivityAsync(
                            "report.download.assessments_list",
                            $"[SuperAdmin] Downloaded assessments list for schoolId={schoolId.Value}" +
                            (type.HasValue ? $" (type={type})" : string.Empty));

                        return PdfFile(pdf, "Assessments_List");
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

                    var pdf = await _reportService.GenerateAssessmentsListReportAsync(
                        schoolId: finalSchoolId,
                        userSchoolId: userSchoolId,
                        isSuperAdmin: false,
                        type: type);

                    await LogUserActivityAsync(
                        "report.download.assessments_list",
                        $"Downloaded assessments list for schoolId={finalSchoolId.Value}" +
                        (type.HasValue ? $" (type={type})" : string.Empty));

                    return PdfFile(pdf, "Assessments_List");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return UnauthorizedResponse(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFoundResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return InternalServerErrorResponse(ex.Message);
            }
        }

        // ── Private helper ─────────────────────────────────────────────────
        private FileContentResult PdfFile(byte[] bytes, string baseName) =>
            File(bytes, "application/pdf",
                $"{baseName}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
    }
}