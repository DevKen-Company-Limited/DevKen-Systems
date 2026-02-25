using Devken.CBC.SchoolManagement.Application.DTOs.Finance;
using Devken.CBC.SchoolManagement.Domain.Enums;

namespace Devken.CBC.SchoolManagement.Application.Service.Finance
{
    public interface IFeeItemService
    {
        Task<IEnumerable<FeeItemResponseDto>> GetAllFeeItemsAsync(
            Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin,
            FeeType? feeType = null, CBCLevel? applicableLevel = null, bool? isActive = null);

        Task<FeeItemResponseDto> GetFeeItemByIdAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);

        Task<FeeItemResponseDto> GetFeeItemByCodeAsync(string code, Guid? userSchoolId, bool isSuperAdmin);

        Task<FeeItemResponseDto> CreateFeeItemAsync(CreateFeeItemDto dto, Guid? userSchoolId, bool isSuperAdmin);

        Task<FeeItemResponseDto> UpdateFeeItemAsync(Guid id, UpdateFeeItemDto dto, Guid? userSchoolId, bool isSuperAdmin);

        Task DeleteFeeItemAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);

        Task<FeeItemResponseDto> ToggleFeeItemActiveAsync(Guid id, bool isActive, Guid? userSchoolId, bool isSuperAdmin);
    }
}