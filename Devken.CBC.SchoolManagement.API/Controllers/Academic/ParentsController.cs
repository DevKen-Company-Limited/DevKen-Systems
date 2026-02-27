using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Parents;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<ParentsController> _logger;

        public ParentsController(
            IParentService parentService,
            ILogger<ParentsController> logger,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _parentService = parentService ?? throw new ArgumentNullException(nameof(parentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

                Response.Headers.Add("X-Access-Level", IsSuperAdmin ? "SuperAdmin" : "SchoolAdmin");
                Response.Headers.Add("X-School-Filter", schoolId?.ToString() ?? "All Schools");

                return SuccessResponse(result);
            }
            catch (UnauthorizedException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to parents list");
                return ForbiddenResponse(ex.Message);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error in parents list");
                return ValidationErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetAll");
                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
            }
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
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Parent not found: {Id}", id);
                return NotFoundResponse(ex.Message);
            }
            catch (UnauthorizedException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to parent {Id}", id);
                return ForbiddenResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetById for {Id}", id);
                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
            }
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
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Student not found: {StudentId}", studentId);
                return NotFoundResponse(ex.Message);
            }
            catch (UnauthorizedException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to parents for student {StudentId}", studentId);
                return ForbiddenResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetByStudent for {StudentId}", studentId);
                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
            }
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
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Referenced entity not found during parent creation");
                return NotFoundResponse(ex.Message);
            }
            catch (UnauthorizedException ex)
            {
                _logger.LogWarning(ex, "Unauthorized parent creation attempt");
                return ForbiddenResponse(ex.Message);
            }
            catch (ConflictException ex)
            {
                _logger.LogWarning(ex, "Conflict during parent creation");
                return ConflictResponse(ex.Message);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error during parent creation");
                return ValidationErrorResponse(ex.Message);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during parent creation");
                return InternalServerErrorResponse("A database error occurred. Please check the data and try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Create");
                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
            }
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
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Parent not found for update: {Id}", id);
                return NotFoundResponse(ex.Message);
            }
            catch (UnauthorizedException ex)
            {
                _logger.LogWarning(ex, "Unauthorized parent update attempt for {Id}", id);
                return ForbiddenResponse(ex.Message);
            }
            catch (ConflictException ex)
            {
                _logger.LogWarning(ex, "Conflict during parent update for {Id}", id);
                return ConflictResponse(ex.Message);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error during parent update for {Id}", id);
                return ValidationErrorResponse(ex.Message);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during parent update for {Id}", id);
                return InternalServerErrorResponse("A database error occurred. Please check the data and try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Update for {Id}", id);
                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
            }
        }

        // DELETE /api/academic/parents/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionKeys.ParentDelete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                _logger.LogInformation("Attempting to delete parent with ID: {Id}", id);

                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                await _parentService.DeleteAsync(
                    id: id,
                    userSchoolId: userSchoolId,
                    isSuperAdmin: IsSuperAdmin);

                await LogUserActivityAsync("parent.delete", $"Deleted parent ID: {id}");
                return SuccessResponse("Parent deleted successfully.");
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Parent not found for deletion: {Id}", id);
                return NotFoundResponse(ex.Message);
            }
            catch (UnauthorizedException ex)
            {
                _logger.LogWarning(ex, "Unauthorized parent deletion attempt for {Id}", id);
                return ForbiddenResponse(ex.Message);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during parent deletion for {Id}", id);
                if (ex.InnerException?.Message.Contains("FK_") == true ||
                    ex.InnerException?.Message.Contains("foreign key") == true)
                {
                    return ConflictResponse("Cannot delete this parent because they are linked to one or more students. Remove the associations first.");
                }
                return InternalServerErrorResponse("A database error occurred while deleting the parent.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Delete for {Id}", id);
                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
            }
        }
        //public async Task<IActionResult> Delete(Guid id)
        //{
        //    try
        //    {
        //        _logger.LogInformation("Attempting to delete parent with ID: {Id}", id);

        //        // First, check if the parent exists and if the user has permission (service will throw if not)
        //        // Optionally, you could fetch the parent before deletion to log details
        //        await _parentService.DeleteAsync(
        //            id: id,
        //            userSchoolId: GetUserSchoolIdOrNullWithValidation(),
        //            isSuperAdmin: IsSuperAdmin);

        //        // Verification: try to fetch the parent again – if it's still there, deletion failed silently
        //        try
        //        {
        //            var check = await _parentService.GetByIdAsync(
        //                id: id,
        //                userSchoolId: GetUserSchoolIdOrNullWithValidation(),
        //                isSuperAdmin: IsSuperAdmin);

        //            // If we reach here, the parent still exists
        //            _logger.LogError("DeleteAsync reported success but parent {Id} still exists", id);
        //            return InternalServerErrorResponse("Parent could not be deleted. It may be referenced by other records.");
        //        }
        //        catch (NotFoundException)
        //        {
        //            // Expected – parent is gone, good
        //            _logger.LogInformation("Parent {Id} successfully deleted", id);
        //        }

        //        await LogUserActivityAsync("parent.delete", $"Deleted parent ID: {id}");

        //        return SuccessResponse("Parent deleted successfully.");
        //    }
        //    catch (NotFoundException ex)
        //    {
        //        _logger.LogWarning(ex, "Parent not found for deletion: {Id}", id);
        //        return NotFoundResponse(ex.Message);
        //    }
        //    catch (UnauthorizedException ex)
        //    {
        //        _logger.LogWarning(ex, "Unauthorized parent deletion attempt for {Id}", id);
        //        return ForbiddenResponse(ex.Message);
        //    }
        //    catch (DbUpdateException ex)
        //    {
        //        _logger.LogError(ex, "Database error during parent deletion for {Id}", id);
        //        // Check if it's a foreign key constraint violation
        //        if (ex.InnerException?.Message.Contains("FK_") == true ||
        //            ex.InnerException?.Message.Contains("foreign key") == true)
        //        {
        //            return ConflictResponse("Cannot delete this parent because they are linked to one or more students. Remove the associations first.");
        //        }
        //        return InternalServerErrorResponse("A database error occurred while deleting the parent.");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Unexpected error in Delete for {Id}", id);
        //        return InternalServerErrorResponse(GetFullExceptionMessage(ex));
        //    }
        //}

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
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Parent not found for activation: {Id}", id);
                return NotFoundResponse(ex.Message);
            }
            catch (UnauthorizedException ex)
            {
                _logger.LogWarning(ex, "Unauthorized parent activation attempt for {Id}", id);
                return ForbiddenResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Activate for {Id}", id);
                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
            }
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
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Parent not found for deactivation: {Id}", id);
                return NotFoundResponse(ex.Message);
            }
            catch (UnauthorizedException ex)
            {
                _logger.LogWarning(ex, "Unauthorized parent deactivation attempt for {Id}", id);
                return ForbiddenResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Deactivate for {Id}", id);
                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
            }
        }
    }
}