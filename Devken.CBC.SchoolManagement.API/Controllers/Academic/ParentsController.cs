// Api/Controllers/Academic/ParentsController.cs

using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Parents;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Academic
{
    [Route("api/academic/[controller]")]
    [ApiController]
    [Authorize]
    public class ParentsController : BaseApiController
    {
        private readonly IParentService _parentService;

        public ParentsController(
            IParentService parentService,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _parentService = parentService ?? throw new ArgumentNullException(nameof(parentService));
        }

        private string GetFullExceptionMessage(Exception ex)
        {
            var message = ex.Message;
            var inner = ex.InnerException;
            while (inner != null) { message += $" | Inner: {inner.Message}"; inner = inner.InnerException; }
            return message;
        }

        // GET /api/academic/parents
        [HttpGet]
        [Authorize(Policy = PermissionKeys.ParentRead)]
        public async Task<IActionResult> GetAll(
            [FromQuery] ParentQueryDto query,
            [FromQuery] Guid? schoolId = null)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var result = await _parentService.GetAllAsync(
                    schoolId: schoolId,
                    userSchoolId: userSchoolId,
                    isSuperAdmin: IsSuperAdmin,
                    query: query);

                return SuccessResponse(result);
            }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // GET /api/academic/parents/{id}
        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionKeys.ParentRead)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var result = await _parentService.GetByIdAsync(
                    id: id,
                    userSchoolId: GetUserSchoolIdOrNullWithValidation(),
                    isSuperAdmin: IsSuperAdmin);

                return SuccessResponse(result);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // GET /api/academic/parents/by-student/{studentId}
        [HttpGet("by-student/{studentId:guid}")]
        [Authorize(Policy = PermissionKeys.ParentRead)]
        public async Task<IActionResult> GetByStudent(Guid studentId)
        {
            try
            {
                var result = await _parentService.GetByStudentIdAsync(
                    studentId: studentId,
                    userSchoolId: GetUserSchoolIdOrNullWithValidation(),
                    isSuperAdmin: IsSuperAdmin);

                return SuccessResponse(result);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // POST /api/academic/parents
        [HttpPost]
        [Authorize(Policy = PermissionKeys.ParentWrite)]
        public async Task<IActionResult> Create([FromBody] CreateParentDto dto)
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

                var result = await _parentService.CreateAsync(
                    dto: dto,
                    userSchoolId: userSchoolId,
                    isSuperAdmin: IsSuperAdmin);

                await LogUserActivityAsync("parent.create",
                    $"Created parent '{result.FullName}' (ID: {result.Id}) in school {result.TenantId}");

                return CreatedResponse(
                    $"api/academic/parents/{result.Id}",
                    result,
                    "Parent created successfully.");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // PUT /api/academic/parents/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionKeys.ParentWrite)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateParentDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var result = await _parentService.UpdateAsync(
                    id: id,
                    dto: dto,
                    userSchoolId: GetUserSchoolIdOrNullWithValidation(),
                    isSuperAdmin: IsSuperAdmin);

                await LogUserActivityAsync("parent.update",
                    $"Updated parent '{result.FullName}' (ID: {result.Id})");

                return SuccessResponse(result, "Parent updated successfully.");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // DELETE /api/academic/parents/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionKeys.ParentDelete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _parentService.DeleteAsync(
                    id: id,
                    userSchoolId: GetUserSchoolIdOrNullWithValidation(),
                    isSuperAdmin: IsSuperAdmin);

                await LogUserActivityAsync("parent.delete", $"Deleted parent ID: {id}");

                return SuccessResponse("Parent deleted successfully.");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // PATCH /api/academic/parents/{id}/activate
        [HttpPatch("{id:guid}/activate")]
        [Authorize(Policy = PermissionKeys.ParentWrite)]
        public async Task<IActionResult> Activate(Guid id)
        {
            try
            {
                var result = await _parentService.ActivateAsync(
                    id: id,
                    userSchoolId: GetUserSchoolIdOrNullWithValidation(),
                    isSuperAdmin: IsSuperAdmin);

                await LogUserActivityAsync("parent.activate", $"Activated parent '{result.FullName}' (ID: {id})");

                return SuccessResponse(result, "Parent activated successfully.");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // PATCH /api/academic/parents/{id}/deactivate
        [HttpPatch("{id:guid}/deactivate")]
        [Authorize(Policy = PermissionKeys.ParentWrite)]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            try
            {
                var result = await _parentService.DeactivateAsync(
                    id: id,
                    userSchoolId: GetUserSchoolIdOrNullWithValidation(),
                    isSuperAdmin: IsSuperAdmin);

                await LogUserActivityAsync("parent.deactivate", $"Deactivated parent '{result.FullName}' (ID: {id})");

                return SuccessResponse(result, "Parent deactivated successfully.");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }
    }
}