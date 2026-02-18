// Api/Controllers/Academic/SubjectsController.cs
using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Academic;
using Devken.CBC.SchoolManagement.Application.DTOs.Academics;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Academics;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Academic
{
        public class SubjectsController : BaseApiController
        {
            private readonly ISubjectService _subjectService;

            public SubjectsController(
                ISubjectService subjectService,
                IUserActivityService? activityService = null)
                : base(activityService)
            {
                _subjectService = subjectService ?? throw new ArgumentNullException(nameof(subjectService));
            }

            private string GetFullExceptionMessage(Exception ex)
            {
                var message = ex.Message;
                var inner = ex.InnerException;
                while (inner != null) { message += $" | Inner: {inner.Message}"; inner = inner.InnerException; }
                return message;
            }

            // GET /api/academic/subjects
            [HttpGet]
            [Authorize(Policy = PermissionKeys.SubjectRead)]
            public async Task<IActionResult> GetAll(
                [FromQuery] Guid? schoolId = null,
                [FromQuery] CBCLevel? level = null,
                [FromQuery] SubjectType? subjectType = null,
                [FromQuery] bool? isActive = null)
            {
                try
                {
                    var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                    var targetSchoolId = IsSuperAdmin ? schoolId : userSchoolId;

                    var subjects = await _subjectService.GetAllSubjectsAsync(
                        targetSchoolId, userSchoolId, IsSuperAdmin, level, subjectType, isActive);

                    return SuccessResponse(subjects);
                }
                catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
                catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
                catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
            }

            // GET /api/academic/subjects/{id}
            [HttpGet("{id:guid}")]
            [Authorize(Policy = PermissionKeys.SubjectRead)]
            public async Task<IActionResult> GetById(Guid id)
            {
                try
                {
                    var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                    var subject = await _subjectService.GetSubjectByIdAsync(id, userSchoolId, IsSuperAdmin);
                    return SuccessResponse(subject);
                }
                catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
                catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
                catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
                catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
            }

            // GET /api/academic/subjects/by-code/{code}
            [HttpGet("by-code/{code}")]
            [Authorize(Policy = PermissionKeys.SubjectRead)]
            public async Task<IActionResult> GetByCode(string code)
            {
                try
                {
                    var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                    var subject = await _subjectService.GetSubjectByCodeAsync(code, userSchoolId, IsSuperAdmin);
                    return SuccessResponse(subject);
                }
                catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
                catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
                catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
                catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
            }

            // POST /api/academic/subjects
            [HttpPost]
            [Authorize(Policy = PermissionKeys.SubjectWrite)]
            public async Task<IActionResult> Create([FromBody] CreateSubjectDto dto)
            {
                if (!ModelState.IsValid)
                    return ValidationErrorResponse(ModelState);

                try
                {
                    var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                    if (!IsSuperAdmin)
                        dto.TenantId = userSchoolId;
                    else if (dto.TenantId == null || dto.TenantId == Guid.Empty)
                        return ValidationErrorResponse("TenantId is required for SuperAdmin.");

                    var result = await _subjectService.CreateSubjectAsync(dto, userSchoolId, IsSuperAdmin);

                    await LogUserActivityAsync(
                        "subject.create",
                        $"Created subject '{result.Name}' [{result.Code}] in school {result.TenantId}");

                    return CreatedResponse($"api/academic/subjects/{result.Id}", result, "Subject created successfully.");
                }
                catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
                catch (ConflictException ex) { return ConflictResponse(ex.Message); }
                catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
                catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
                catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
                catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
            }

            // PUT /api/academic/subjects/{id}
            [HttpPut("{id:guid}")]
            [Authorize(Policy = PermissionKeys.SubjectWrite)]
            public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSubjectDto dto)
            {
                if (!ModelState.IsValid)
                    return ValidationErrorResponse(ModelState);

                try
                {
                    var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                    var result = await _subjectService.UpdateSubjectAsync(id, dto, userSchoolId, IsSuperAdmin);

                    await LogUserActivityAsync("subject.update", $"Updated subject '{result.Name}' [{result.Code}]");

                    return SuccessResponse(result, "Subject updated successfully.");
                }
                catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
                catch (ConflictException ex) { return ConflictResponse(ex.Message); }
                catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
                catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
                catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
                catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
            }

            // DELETE /api/academic/subjects/{id}
            [HttpDelete("{id:guid}")]
            [Authorize(Policy = PermissionKeys.SubjectDelete)]
            public async Task<IActionResult> Delete(Guid id)
            {
                try
                {
                    var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                    await _subjectService.DeleteSubjectAsync(id, userSchoolId, IsSuperAdmin);

                    await LogUserActivityAsync("subject.delete", $"Deleted subject ID: {id}");

                    return SuccessResponse("Subject deleted successfully.");
                }
                catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
                catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
                catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
                catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
            }

            // PATCH /api/academic/subjects/{id}/toggle-active
            [HttpPatch("{id:guid}/toggle-active")]
            [Authorize(Policy = PermissionKeys.SubjectWrite)]
            public async Task<IActionResult> ToggleActive(Guid id, [FromBody] bool isActive)
            {
                try
                {
                    var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                    var result = await _subjectService.ToggleSubjectActiveAsync(id, isActive, userSchoolId, IsSuperAdmin);

                    var action = isActive ? "activated" : "deactivated";
                    await LogUserActivityAsync("subject.toggle-active", $"{action} subject '{result.Name}' [{result.Code}]");

                    return SuccessResponse(result, $"Subject {action} successfully.");
                }
                catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
                catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
                catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
                catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
            }
        }
 }
