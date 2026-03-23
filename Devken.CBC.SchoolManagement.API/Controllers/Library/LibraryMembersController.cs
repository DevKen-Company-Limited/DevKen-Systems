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
    public class LibraryMembersController : BaseApiController
    {
        private readonly ILibraryMemberService _memberService;

        public LibraryMembersController(
            ILibraryMemberService memberService,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _memberService = memberService
                ?? throw new ArgumentNullException(nameof(memberService));
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

                var members = await _memberService.GetAllMembersAsync(
                    targetSchoolId, userSchoolId, IsSuperAdmin);

                Response.Headers.Append("X-Access-Level",
                    IsSuperAdmin ? "SuperAdmin" : "SchoolAdmin");
                Response.Headers.Append("X-School-Filter",
                    targetSchoolId?.ToString() ?? "All Schools");

                return SuccessResponse(members);
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

                var member = await _memberService.GetMemberByIdAsync(
                    id, userSchoolId, IsSuperAdmin);

                return SuccessResponse(member);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        [HttpPost]
        [Authorize(Policy = PermissionKeys.LibraryWrite)]
        public async Task<IActionResult> Create([FromBody] CreateLibraryMemberRequest request)
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

                var result = await _memberService.CreateMemberAsync(
                    request, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "library-member.create",
                    $"Created library member '{result.MemberNumber}' for user '{result.UserFullName}'");

                return CreatedResponse(result, "Library member created successfully");
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
        public async Task<IActionResult> Update(
            Guid id, [FromBody] UpdateLibraryMemberRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var result = await _memberService.UpdateMemberAsync(
                    id, request, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "library-member.update",
                    $"Updated library member '{result.MemberNumber}'");

                return SuccessResponse(result, "Library member updated successfully");
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

                await _memberService.DeleteMemberAsync(id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "library-member.delete",
                    $"Deleted library member with ID: {id}");

                return SuccessResponse("Library member deleted successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }
    }
}