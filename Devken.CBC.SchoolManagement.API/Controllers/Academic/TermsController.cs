using Azure;
using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Academics;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.Service.Academics;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Devken.CBC.SchoolManagement.API.Controllers.Academic
{
    [Route("api/academic/[controller]")]
    [ApiController]
    [Authorize]
    public class TermsController : BaseApiController
    {
        private readonly ITermService _termService;

        public TermsController(
            ITermService termService,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _termService = termService ?? throw new ArgumentNullException(nameof(termService));
        }

        #region Helpers

        private string GetFullExceptionMessage(Exception ex)
        {
            var message = ex.Message;
            var inner = ex.InnerException;
            while (inner != null)
            {
                message += $" | Inner: {inner.Message}";
                inner = inner.InnerException;
            }
            return message;
        }

        #endregion

        #region GET

        [HttpGet]
        [Authorize(Policy = PermissionKeys.TermRead)]
        public async Task<IActionResult> GetAll([FromQuery] Guid? schoolId = null)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var targetSchoolId = IsSuperAdmin ? schoolId : userSchoolId;

                var terms = await _termService.GetAllTermsAsync(
                    targetSchoolId,
                    userSchoolId,
                    IsSuperAdmin);

                Response.Headers.Add("X-Access-Level", IsSuperAdmin ? "SuperAdmin" : "SchoolAdmin");
                Response.Headers.Add("X-School-Filter", targetSchoolId?.ToString() ?? "All Schools");

                return SuccessResponse(terms);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        [HttpGet("academic-year/{academicYearId:guid}")]
        [Authorize(Policy = PermissionKeys.TermRead)]
        public async Task<IActionResult> GetByAcademicYear(Guid academicYearId)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var terms = await _termService.GetTermsByAcademicYearAsync(
                    academicYearId,
                    userSchoolId,
                    IsSuperAdmin);

                return SuccessResponse(terms);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionKeys.TermRead)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var term = await _termService.GetTermByIdAsync(
                    id,
                    userSchoolId,
                    IsSuperAdmin);

                return SuccessResponse(term);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        [HttpGet("current")]
        [Authorize(Policy = PermissionKeys.TermRead)]
        public async Task<IActionResult> GetCurrent([FromQuery] Guid? schoolId = null)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var targetSchoolId = IsSuperAdmin ? schoolId : userSchoolId;

                var term = await _termService.GetCurrentTermAsync(
                    targetSchoolId,
                    userSchoolId,
                    IsSuperAdmin);

                if (term == null)
                    return NotFoundResponse("No current term found for the specified school.");

                return SuccessResponse(term);
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        [HttpGet("active")]
        [Authorize(Policy = PermissionKeys.TermRead)]
        public async Task<IActionResult> GetActive([FromQuery] Guid? schoolId = null)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var targetSchoolId = IsSuperAdmin ? schoolId : userSchoolId;

                var terms = await _termService.GetActiveTermsAsync(
                    targetSchoolId,
                    userSchoolId,
                    IsSuperAdmin);

                return SuccessResponse(terms);
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        #endregion

        #region CREATE

        [HttpPost]
        [Authorize(Policy = PermissionKeys.TermWrite)]
        public async Task<IActionResult> Create([FromBody] CreateTermRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                // Enforce schoolId handling based on user role
                if (!IsSuperAdmin)
                {
                    // Non-SuperAdmin: Force their own school
                    request.SchoolId = userSchoolId!.Value;
                }
                else if (request.SchoolId == null || request.SchoolId == Guid.Empty)
                {
                    // SuperAdmin: Require schoolId in request
                    return ValidationErrorResponse("SchoolId is required for SuperAdmin.");
                }

                var result = await _termService.CreateTermAsync(request, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "term.create",
                    $"Created term {result.Name} (Term {result.TermNumber}) for academic year {result.AcademicYearName}");

                return CreatedResponse(result, "Term created successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        #endregion

        #region UPDATE

        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionKeys.TermWrite)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTermRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var result = await _termService.UpdateTermAsync(id, request, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "term.update",
                    $"Updated term {result.Name} (Term {result.TermNumber})");

                return SuccessResponse(result, "Term updated successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        #endregion

        #region TERM STATUS MANAGEMENT

        [HttpPatch("{id:guid}/set-current")]
        [Authorize(Policy = PermissionKeys.TermWrite)]
        public async Task<IActionResult> SetCurrent(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var result = await _termService.SetCurrentTermAsync(id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "term.set-current",
                    $"Set term {result.Name} (Term {result.TermNumber}) as current");

                return SuccessResponse(result, "Term set as current successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        [HttpPatch("{id:guid}/close")]
        [Authorize(Policy = PermissionKeys.TermWrite)]
        public async Task<IActionResult> Close(Guid id, [FromBody] CloseTermRequest request)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var result = await _termService.CloseTermAsync(
                    id,
                    request.Remarks,
                    userSchoolId,
                    IsSuperAdmin);

                await LogUserActivityAsync(
                    "term.close",
                    $"Closed term {result.Name} (Term {result.TermNumber})");

                return SuccessResponse(result, "Term closed successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        [HttpPatch("{id:guid}/reopen")]
        [Authorize(Policy = PermissionKeys.TermWrite)]
        public async Task<IActionResult> Reopen(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var result = await _termService.ReopenTermAsync(id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "term.reopen",
                    $"Reopened term {result.Name} (Term {result.TermNumber})");

                return SuccessResponse(result, "Term reopened successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        #endregion

        #region DELETE

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionKeys.TermDelete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                await _termService.DeleteTermAsync(id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync("term.delete", $"Deleted term with ID: {id}");

                return SuccessResponse("Term deleted successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        #endregion
    }
}
