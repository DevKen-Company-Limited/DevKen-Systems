//using System;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Devken.CBC.SchoolManagement.Application.DTOs.Academics;
//using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
//using Devken.CBC.SchoolManagement.Application.Service.Academics;
//using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
//using Microsoft.Extensions.Logging;

//namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Academics
//{
//    public class AcademicYearService : IAcademicYearService
//    {
//        private readonly IRepositoryManager _repository;
//        private readonly ILogger<AcademicYearService> _logger;

//        public AcademicYearService(
//            IRepositoryManager repository,
//            ILogger<AcademicYearService> logger)
//        {
//            _repository = repository;
//            _logger = logger;
//        }

//        public async Task<AcademicYearResponseDto> CreateAsync(Guid tenantId, CreateAcademicYearDto dto)
//        {
//            try
//            {
//                _logger.LogInformation("Creating academic year: {Name} for tenant: {TenantId}", dto.Name, tenantId);

//                // Validate required fields
//                if (string.IsNullOrWhiteSpace(dto.Name))
//                    throw new ValidationException("Academic year name is required");

//                if (string.IsNullOrWhiteSpace(dto.Code))
//                    throw new ValidationException("Academic year code is required");

//                if (dto.StartDate >= dto.EndDate)
//                    throw new ValidationException("End date must be after start date");

//                // Check if code already exists
//                if (await _repository.AcademicYear.CodeExistsAsync(tenantId, dto.Code))
//                    throw new ValidationException($"Academic year with code '{dto.Code}' already exists");

//                // Check for overlapping years if validation is enabled
//                if (await _repository.AcademicYear.HasOverlappingYearsAsync(tenantId, dto.StartDate, dto.EndDate))
//                {
//                    _logger.LogWarning("Overlapping academic year detected for tenant: {TenantId}", tenantId);
//                    // You can choose to throw an exception or just log a warning
//                    // throw new ValidationException("This academic year overlaps with an existing year");
//                }

//                var academicYear = new AcademicYear
//                {
//                    TenantId = tenantId,
//                    Name = dto.Name,
//                    Code = dto.Code,
//                    StartDate = dto.StartDate,
//                    EndDate = dto.EndDate,
//                    IsCurrent = dto.IsCurrent,
//                    Notes = dto.Notes
//                };

//                _repository.AcademicYear.Create(academicYear);

//                // If this is set as current, unset all others
//                if (dto.IsCurrent)
//                {
//                    await _repository.AcademicYear.SetAsCurrentAsync(tenantId, academicYear.Id);
//                }

//                await _repository.SaveAsync();

//                _logger.LogInformation("Successfully created academic year with ID: {Id}", academicYear.Id);

//                return MapToDto(academicYear);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error creating academic year: {Name}", dto?.Name);
//                throw;
//            }
//        }

//        public async Task<AcademicYearResponseDto> UpdateAsync(Guid tenantId, Guid id, UpdateAcademicYearDto dto)
//        {
//            try
//            {
//                _logger.LogInformation("Updating academic year: {Id} for tenant: {TenantId}", id, tenantId);

//                var academicYear = await _repository.AcademicYear.GetByIdAsync(id, trackChanges: true);

//                if (academicYear == null || academicYear.TenantId != tenantId)
//                    throw new KeyNotFoundException($"Academic year with ID {id} not found");

//                if (academicYear.IsClosed)
//                    throw new InvalidOperationException("Cannot update a closed academic year");

//                // Validate if updating dates
//                if (dto.StartDate.HasValue && dto.EndDate.HasValue)
//                {
//                    if (dto.StartDate.Value >= dto.EndDate.Value)
//                        throw new ValidationException("End date must be after start date");
//                }

//                // Update fields
//                if (!string.IsNullOrWhiteSpace(dto.Name))
//                    academicYear.Name = dto.Name;

//                if (!string.IsNullOrWhiteSpace(dto.Code))
//                {
//                    if (await _repository.AcademicYear.CodeExistsAsync(tenantId, dto.Code, id))
//                        throw new ValidationException($"Academic year with code '{dto.Code}' already exists");

//                    academicYear.Code = dto.Code;
//                }

//                if (dto.StartDate.HasValue)
//                    academicYear.StartDate = dto.StartDate.Value;

//                if (dto.EndDate.HasValue)
//                    academicYear.EndDate = dto.EndDate.Value;

//                if (dto.Notes != null)
//                    academicYear.Notes = dto.Notes;

//                _repository.AcademicYear.Update(academicYear);

//                // Handle IsCurrent flag
//                if (dto.IsCurrent.HasValue && dto.IsCurrent.Value && !academicYear.IsCurrent)
//                {
//                    await _repository.AcademicYear.SetAsCurrentAsync(tenantId, id);
//                }

//                await _repository.SaveAsync();

//                _logger.LogInformation("Successfully updated academic year: {Id}", id);

//                return MapToDto(academicYear);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error updating academic year: {Id}", id);
//                throw;
//            }
//        }

//        public async Task<AcademicYearResponseDto?> GetByIdAsync(Guid tenantId, Guid id)
//        {
//            var academicYear = await _repository.AcademicYear.GetByIdAsync(id, trackChanges: false);

//            if (academicYear == null || academicYear.TenantId != tenantId)
//                return null;

//            return MapToDto(academicYear);
//        }

//        public async Task<IEnumerable<AcademicYearResponseDto>> GetAllAsync(Guid tenantId)
//        {
//            var academicYears = await _repository.AcademicYear.GetAllByTenantAsync(tenantId);
//            return academicYears.Select(MapToDto);
//        }

//        public async Task<AcademicYearResponseDto?> GetCurrentAsync(Guid tenantId)
//        {
//            var academicYear = await _repository.AcademicYear.GetCurrentAcademicYearAsync(tenantId);
//            return academicYear == null ? null : MapToDto(academicYear);
//        }

//        public async Task<bool> SetAsCurrentAsync(Guid tenantId, Guid id)
//        {
//            try
//            {
//                var academicYear = await _repository.AcademicYear.GetByIdAsync(id, trackChanges: false);

//                if (academicYear == null || academicYear.TenantId != tenantId)
//                    return false;

//                if (academicYear.IsClosed)
//                    throw new InvalidOperationException("Cannot set a closed academic year as current");

//                await _repository.AcademicYear.SetAsCurrentAsync(tenantId, id);
//                await _repository.SaveAsync();

//                _logger.LogInformation("Set academic year {Id} as current for tenant {TenantId}", id, tenantId);

//                return true;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error setting academic year {Id} as current", id);
//                throw;
//            }
//        }

//        public async Task<bool> CloseAcademicYearAsync(Guid tenantId, Guid id)
//        {
//            try
//            {
//                var academicYear = await _repository.AcademicYear.GetByIdAsync(id, trackChanges: true);

//                if (academicYear == null || academicYear.TenantId != tenantId)
//                    return false;

//                academicYear.IsClosed = true;
//                academicYear.IsCurrent = false; // A closed year cannot be current

//                _repository.AcademicYear.Update(academicYear);
//                await _repository.SaveAsync();

//                _logger.LogInformation("Closed academic year: {Id}", id);

//                return true;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error closing academic year: {Id}", id);
//                throw;
//            }
//        }

//        public async Task<bool> DeleteAsync(Guid tenantId, Guid id)
//        {
//            try
//            {
//                var academicYear = await _repository.AcademicYear.GetByIdAsync(id, trackChanges: true);

//                if (academicYear == null || academicYear.TenantId != tenantId)
//                    return false;

//                // Check if there are related entities (you may want to add these checks)
//                // if (academicYear.Classes.Any())
//                //     throw new InvalidOperationException("Cannot delete academic year with associated classes");

//                _repository.AcademicYear.Delete(academicYear);
//                await _repository.SaveAsync();

//                _logger.LogInformation("Deleted academic year: {Id}", id);

//                return true;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error deleting academic year: {Id}", id);
//                throw;
//            }
//        }

//        public async Task<IEnumerable<AcademicYearResponseDto>> GetOpenAcademicYearsAsync(Guid tenantId)
//        {
//            var academicYears = await _repository.AcademicYear.GetOpenAcademicYearsAsync(tenantId);
//            return academicYears.Select(MapToDto);
//        }

//        // Helper method to map entity to DTO
//        private AcademicYearResponseDto MapToDto(AcademicYear academicYear)
//        {
//            return new AcademicYearResponseDto
//            {
//                Id = academicYear.Id,
//                Name = academicYear.Name,
//                Code = academicYear.Code,
//                StartDate = academicYear.StartDate,
//                EndDate = academicYear.EndDate,
//                IsCurrent = academicYear.IsCurrent,
//                IsClosed = academicYear.IsClosed,
//                IsActive = academicYear.IsActive,
//                Notes = academicYear.Notes,
//                CreatedOn = academicYear.CreatedOn,
//                CreatedBy = academicYear.CreatedBy,
//                UpdatedOn = academicYear.UpdatedOn,
//                UpdatedBy = academicYear.UpdatedBy
//            };
//        }
//    }
//}
