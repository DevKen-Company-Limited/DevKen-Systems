using Devken.CBC.SchoolManagement.Application.DTOs.Finance;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Finance;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.NumberSeries;
using Devken.CBC.SchoolManagement.Application.Service.Finance;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Finance
{
    public class FeeItemService : IFeeItemService
    {
        private readonly IRepositoryManager _repositories;
        private readonly IFeeItemRepository _feeItemRepository;
        private readonly IDocumentNumberSeriesRepository _documentNumberService;

        private const string FEE_ITEM_NUMBER_SERIES = "FeeItem";
        private const string FEE_ITEM_PREFIX = "FEE";

        public FeeItemService(
            IRepositoryManager repositories,
            IFeeItemRepository feeItemRepository,
            IDocumentNumberSeriesRepository documentNumberService)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
            _feeItemRepository = feeItemRepository ?? throw new ArgumentNullException(nameof(feeItemRepository));
            _documentNumberService = documentNumberService ?? throw new ArgumentNullException(nameof(documentNumberService));
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET ALL
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<FeeItemResponseDto>> GetAllFeeItemsAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin,
            FeeType? feeType = null,
            CBCLevel? applicableLevel = null,
            bool? isActive = null)
        {
            IEnumerable<FeeItem> feeItems;

            if (isSuperAdmin)
            {
                feeItems = schoolId.HasValue
                    ? await _feeItemRepository.GetByTenantIdAsync(schoolId.Value, trackChanges: false)
                    : await _feeItemRepository.GetAllAsync(trackChanges: false);
            }
            else
            {
                if (!userSchoolId.HasValue)
                    throw new UnauthorizedException("You must be assigned to a school to view fee items.");

                feeItems = await _feeItemRepository.GetByTenantIdAsync(userSchoolId.Value, trackChanges: false);
            }

            if (feeType.HasValue)
                feeItems = feeItems.Where(f => f.FeeType == feeType.Value);

            if (applicableLevel.HasValue)
                feeItems = feeItems.Where(f => f.ApplicableLevel == applicableLevel.Value || f.ApplicableLevel == null);

            if (isActive.HasValue)
                feeItems = feeItems.Where(f => f.IsActive == isActive.Value);

            var itemList = feeItems.OrderBy(f => f.Name).ToList();

            var schoolNameMap = await BuildSchoolNameMapAsync(
                itemList.Select(f => f.TenantId).Distinct());

            return itemList.Select(f => MapToDto(f, schoolNameMap.GetValueOrDefault(f.TenantId)));
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET BY ID
        // ─────────────────────────────────────────────────────────────────────
        public async Task<FeeItemResponseDto> GetFeeItemByIdAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var feeItem = await _feeItemRepository.GetByIdWithDetailsAsync(id, trackChanges: false)
                ?? throw new NotFoundException($"Fee item with ID '{id}' not found.");

            ValidateAccess(feeItem.TenantId, userSchoolId, isSuperAdmin);

            var schoolName = await ResolveSchoolNameAsync(feeItem.TenantId);
            return MapToDto(feeItem, schoolName);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET BY CODE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<FeeItemResponseDto> GetFeeItemByCodeAsync(
            string code, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (!userSchoolId.HasValue && !isSuperAdmin)
                throw new UnauthorizedException("School context is required.");

            var tenantId = userSchoolId
                ?? throw new System.ComponentModel.DataAnnotations.ValidationException(
                    "SuperAdmin must use GetAll with schoolId filter for code lookup.");

            var feeItem = await _feeItemRepository.GetByCodeAsync(code, tenantId)
                ?? throw new NotFoundException($"Fee item with code '{code}' not found.");

            var schoolName = await ResolveSchoolNameAsync(feeItem.TenantId);
            return MapToDto(feeItem, schoolName);
        }

        // ─────────────────────────────────────────────────────────────────────
        // CREATE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<FeeItemResponseDto> CreateFeeItemAsync(
            CreateFeeItemDto dto,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var strategy = _repositories.Context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                var tenantId = ResolveTenantId(dto.TenantId, userSchoolId, isSuperAdmin);

                var school = await _repositories.School.GetByIdAsync(tenantId, trackChanges: false)
                    ?? throw new NotFoundException($"School with ID '{tenantId}' not found.");

                if (await _feeItemRepository.ExistsByNameAsync(dto.Name, tenantId))
                    throw new ConflictException(
                        $"A fee item named '{dto.Name}' already exists for this school.");

                var feeCode = await ResolveFeeItemCodeAsync(tenantId);

                var feeItem = new FeeItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Name = dto.Name,
                    Code = feeCode,
                    Description = dto.Description,
                    DefaultAmount = dto.DefaultAmount,
                    FeeType = dto.FeeType,
                    IsMandatory = dto.IsMandatory,
                    IsRecurring = dto.IsRecurring,
                    Recurrence = dto.Recurrence ?? RecurrenceType.None,
                    IsTaxable = dto.IsTaxable,
                    TaxRate = dto.TaxRate,
                    GlCode = dto.GlCode,
                    IsActive = dto.IsActive,
                    ApplicableLevel = dto.ApplicableLevel,
                    ApplicableTo = dto.ApplicableTo ?? ApplicableTo.All,
                };

                _feeItemRepository.Create(feeItem);
                await _repositories.SaveAsync();

                return MapToDto(feeItem, school.Name);
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<FeeItemResponseDto> UpdateFeeItemAsync(
            Guid id,
            UpdateFeeItemDto dto,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var existing = await _feeItemRepository.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Fee item with ID '{id}' not found.");

            ValidateAccess(existing.TenantId, userSchoolId, isSuperAdmin);

            if (await _feeItemRepository.ExistsByNameAsync(
                    dto.Name, existing.TenantId, excludeId: id))
                throw new ConflictException(
                    $"A fee item named '{dto.Name}' already exists for this school.");

            existing.Name = dto.Name;
            existing.Description = dto.Description;
            existing.DefaultAmount = dto.DefaultAmount;
            existing.FeeType = dto.FeeType;
            existing.IsMandatory = dto.IsMandatory;
            existing.IsRecurring = dto.IsRecurring;
            existing.Recurrence = dto.Recurrence ?? RecurrenceType.None;
            existing.IsTaxable = dto.IsTaxable;
            existing.TaxRate = dto.TaxRate;
            existing.GlCode = dto.GlCode;
            existing.IsActive = dto.IsActive;
            existing.ApplicableLevel = dto.ApplicableLevel;
            existing.ApplicableTo = dto.ApplicableTo ?? ApplicableTo.All;

            _feeItemRepository.Update(existing);
            await _repositories.SaveAsync();

            var schoolName = await ResolveSchoolNameAsync(existing.TenantId);
            return MapToDto(existing, schoolName);
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE
        // ─────────────────────────────────────────────────────────────────────
        public async Task DeleteFeeItemAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var feeItem = await _feeItemRepository.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Fee item with ID '{id}' not found.");

            ValidateAccess(feeItem.TenantId, userSchoolId, isSuperAdmin);

            _feeItemRepository.Delete(feeItem);
            await _repositories.SaveAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // TOGGLE ACTIVE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<FeeItemResponseDto> ToggleFeeItemActiveAsync(
            Guid id,
            bool isActive,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var feeItem = await _feeItemRepository.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Fee item with ID '{id}' not found.");

            ValidateAccess(feeItem.TenantId, userSchoolId, isSuperAdmin);

            feeItem.IsActive = isActive;
            _feeItemRepository.Update(feeItem);
            await _repositories.SaveAsync();

            var schoolName = await ResolveSchoolNameAsync(feeItem.TenantId);
            return MapToDto(feeItem, schoolName);
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private async Task<string?> ResolveSchoolNameAsync(Guid tenantId)
        {
            var school = await _repositories.School.GetByIdAsync(tenantId, trackChanges: false);
            return school?.Name;
        }

        private async Task<Dictionary<Guid, string>> BuildSchoolNameMapAsync(
            IEnumerable<Guid> tenantIds)
        {
            var map = new Dictionary<Guid, string>();
            foreach (var tid in tenantIds)
            {
                var school = await _repositories.School.GetByIdAsync(tid, trackChanges: false);
                if (school != null)
                    map[tid] = school.Name;
            }
            return map;
        }

        private static Guid ResolveTenantId(Guid? requestTenantId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin)
            {
                if (!requestTenantId.HasValue || requestTenantId == Guid.Empty)
                    throw new System.ComponentModel.DataAnnotations.ValidationException(
                        "TenantId is required for SuperAdmin when creating a fee item.");
                return requestTenantId.Value;
            }

            if (!userSchoolId.HasValue || userSchoolId == Guid.Empty)
                throw new UnauthorizedException(
                    "You must be assigned to a school to create fee items.");

            return userSchoolId.Value;
        }

        private static void ValidateAccess(Guid feeItemTenantId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin) return;
            if (!userSchoolId.HasValue || feeItemTenantId != userSchoolId.Value)
                throw new UnauthorizedException("You do not have access to this fee item.");
        }

        private async Task<string> ResolveFeeItemCodeAsync(Guid tenantId)
        {
            var seriesExists = await _documentNumberService
                .SeriesExistsAsync(FEE_ITEM_NUMBER_SERIES, tenantId);

            if (!seriesExists)
            {
                await _documentNumberService.CreateSeriesAsync(
                    entityName: FEE_ITEM_NUMBER_SERIES,
                    tenantId: tenantId,
                    prefix: FEE_ITEM_PREFIX,
                    padding: 5,
                    resetEveryYear: false,
                    description: "Fee item codes");
            }

            return await _documentNumberService.GenerateAsync(FEE_ITEM_NUMBER_SERIES, tenantId);
        }

        /// <summary>
        /// Maps FeeItem entity → FeeItemResponseDto.
        /// FeeType, Recurrence, ApplicableTo, and ApplicableLevel are enums on the
        /// entity — serialised as numeric strings so frontend resolvers work unchanged.
        /// </summary>
        private static FeeItemResponseDto MapToDto(FeeItem f, string? schoolName = null) => new()
        {
            Id = (Guid)f.Id!,
            Code = f.Code,
            Name = f.Name,
            Description = f.Description,
            DefaultAmount = f.DefaultAmount,
            FeeType = ((int)f.FeeType).ToString(),
            IsMandatory = f.IsMandatory,
            IsRecurring = f.IsRecurring,
            Recurrence = ((int)f.Recurrence).ToString(),
            IsTaxable = f.IsTaxable,
            TaxRate = f.TaxRate,
            GlCode = f.GlCode,
            IsActive = f.IsActive,
            ApplicableLevel = f.ApplicableLevel.HasValue
                ? ((int)f.ApplicableLevel.Value).ToString()
                : null,
            ApplicableTo = ((int)f.ApplicableTo).ToString(),
            DisplayName = f.DisplayName,
            TenantId = f.TenantId,
            SchoolName = schoolName,
            Status = f.Status.ToString(),
            CreatedOn = f.CreatedOn,
            UpdatedOn = f.UpdatedOn,
        };
    }
}