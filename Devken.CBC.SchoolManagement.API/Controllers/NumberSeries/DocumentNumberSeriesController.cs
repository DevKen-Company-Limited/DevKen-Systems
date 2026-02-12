using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.NumberSeries;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Domain.Entities.NumberSeries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Api.Controllers.NumberSeries
{
    [Route("api/document-number-series")]
    [ApiController]
    [Authorize]
    public class DocumentNumberSeriesController : BaseApiController
    {
        private readonly IRepositoryManager _repositories;

        public DocumentNumberSeriesController(
            IRepositoryManager repositories,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
        }

        // ───────────────────────────────────────────────
        // GET ALL
        // ───────────────────────────────────────────────
        [HttpGet]
        public IActionResult GetAll([FromQuery] Guid? tenantId = null)
        {
            if (!HasPermission("DocumentNumberSeries.Read"))
                return ForbiddenResponse("You do not have permission to view document number series.");

            IQueryable<DocumentNumberSeries> query;

            if (IsSuperAdmin)
            {
                // SuperAdmin: get all if no tenantId specified
                query = tenantId.HasValue && tenantId != Guid.Empty
                    ? _repositories.DocumentNumberSeries.FindByCondition(x => x.TenantId == tenantId, trackChanges: false)
                    : _repositories.DocumentNumberSeries.FindAll(trackChanges: false); // ALL schools
            }
            else
            {
                // Normal user: only own tenant
                var currentTenantId = GetCurrentUserTenantId();
                query = _repositories.DocumentNumberSeries.FindByCondition(x => x.TenantId == currentTenantId, trackChanges: false);
            }

            var list = query.OrderBy(x => x.EntityName).ToList();

            return SuccessResponse(list);
        }


        // ───────────────────────────────────────────────
        // CREATE
        // ───────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDocumentNumberSeriesDto dto)
        {
            if (!HasPermission("DocumentNumberSeries.Write"))
                return ForbiddenResponse("You do not have permission to create document number series.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState.ToDictionary(
                    k => k.Key,
                    v => v.Value.Errors.Select(e => e.ErrorMessage).ToArray()));

            // Determine tenant
            Guid tenantId;
            if (IsSuperAdmin)
            {
                if ( dto.TenantId == Guid.Empty)
                    return ValidationErrorResponse("SuperAdmin must specify a school/tenant ID.");
                tenantId = dto.TenantId;
            }
            else
            {
                tenantId = GetCurrentUserTenantId();
            }

            // Check if number series already exists
            var exists = _repositories.DocumentNumberSeries
                .FindByCondition(
                    x => x.EntityName == dto.EntityName && x.TenantId == tenantId,
                    trackChanges: false)
                .Any();

            if (exists)
                return ConflictResponse($"Number series for {dto.EntityName} already exists.");

            var entity = new DocumentNumberSeries
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                EntityName = dto.EntityName,
                Prefix = dto.Prefix,
                Padding = dto.Padding,
                ResetEveryYear = dto.ResetEveryYear,
                LastNumber = 0,
                LastGeneratedYear = DateTime.UtcNow.Year
            };

            _repositories.DocumentNumberSeries.Create(entity);
            await _repositories.SaveAsync();

            await LogUserActivityAsync(
                "documentnumberseries.create",
                $"Created number series for entity {dto.EntityName}");

            return CreatedResponse($"api/document-number-series/{entity.Id}", entity, "Document number series created successfully");
        }

        // ───────────────────────────────────────────────
        // UPDATE
        // ───────────────────────────────────────────────
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDocumentNumberSeriesDto dto)
        {
            if (!HasPermission("DocumentNumberSeries.Write"))
                return ForbiddenResponse("You do not have permission to update document number series.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState.ToDictionary(
                    k => k.Key,
                    v => v.Value.Errors.Select(e => e.ErrorMessage).ToArray()));

            var entity = await _repositories.DocumentNumberSeries.GetByIdAsync(id, trackChanges: true);

            if (entity == null)
                return NotFoundResponse();

            var accessError = ValidateTenantAccess(entity.TenantId);
            if (accessError != null)
                return accessError;

            entity.Prefix = dto.Prefix;
            entity.Padding = dto.Padding;
            entity.ResetEveryYear = dto.ResetEveryYear;

            _repositories.DocumentNumberSeries.Update(entity);
            await _repositories.SaveAsync();

            await LogUserActivityAsync(
                "documentnumberseries.update",
                $"Updated number series {entity.EntityName}");

            return SuccessResponse(entity, "Document number series updated successfully");
        }

        // ───────────────────────────────────────────────
        // DELETE
        // ───────────────────────────────────────────────
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!HasPermission("DocumentNumberSeries.Delete"))
                return ForbiddenResponse("You do not have permission to delete document number series.");

            var entity = await _repositories.DocumentNumberSeries.GetByIdAsync(id, trackChanges: true);
            if (entity == null)
                return NotFoundResponse();

            var accessError = ValidateTenantAccess(entity.TenantId);
            if (accessError != null)
                return accessError;

            _repositories.DocumentNumberSeries.Delete(entity);
            await _repositories.SaveAsync();

            await LogUserActivityAsync(
                "documentnumberseries.delete",
                $"Deleted number series {entity.EntityName}");

            return SuccessResponse<object?>(null, "Document number series deleted successfully");
        }
    }
}
