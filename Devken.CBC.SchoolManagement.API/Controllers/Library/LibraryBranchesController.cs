using System;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Library;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.Library;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Library
{
    [Route("api/library/[controller]")]
    [ApiController]
    [Authorize]
    public class LibraryBranchesController : BaseApiController
    {
        private readonly ILibraryBranchService _branchService;

        public LibraryBranchesController(
            ILibraryBranchService branchService,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _branchService = branchService ?? throw new ArgumentNullException(nameof(branchService));
        }

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

        // ── GET ALL ───────────────────────────────────────────────────────────

        [HttpGet]
        [Authorize(Policy = PermissionKeys.LibraryRead)]
        public async Task<IActionResult> GetAll([FromQuery] Guid? schoolId = null)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var targetSchoolId = IsSuperAdmin ? schoolId : userSchoolId;

                var branches = await _branchService.GetAllBranchesAsync(
                    targetSchoolId, userSchoolId, IsSuperAdmin);

                //Response.Headers.Add("X-Access-Level", IsSuperAdmin ? "SuperAdmin" : "SchoolAdmin");
                //Response.Headers.Add("X-School-Filter", targetSchoolId?.ToString() ?? "All Schools");

                return SuccessResponse(branches);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── GET BY ID ─────────────────────────────────────────────────────────

        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionKeys.LibraryRead)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var branch = await _branchService.GetBranchByIdAsync(id, userSchoolId, IsSuperAdmin);

                return SuccessResponse(branch);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        [HttpPost]
        [Authorize(Policy = PermissionKeys.LibraryWrite)]
        public async Task<IActionResult> Create([FromBody] CreateLibraryBranchRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                if (!IsSuperAdmin)
                    request.SchoolId = userSchoolId!.Value;
                else if (request.SchoolId == null || request.SchoolId == Guid.Empty)
                    return ValidationErrorResponse("SchoolId is required for SuperAdmin.");

                var result = await _branchService.CreateBranchAsync(request, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "library-branch.create",
                    $"Created library branch '{result.Name}'");

                return CreatedResponse(result, "Library branch created successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── UPDATE ────────────────────────────────────────────────────────────

        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionKeys.LibraryWrite)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLibraryBranchRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var result = await _branchService.UpdateBranchAsync(id, request, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "library-branch.update",
                    $"Updated library branch '{result.Name}'");

                return SuccessResponse(result, "Library branch updated successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── DELETE ────────────────────────────────────────────────────────────

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionKeys.LibraryDelete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                await _branchService.DeleteBranchAsync(id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync("library-branch.delete", $"Deleted library branch with ID: {id}");

                return SuccessResponse("Library branch deleted successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }
    }
}