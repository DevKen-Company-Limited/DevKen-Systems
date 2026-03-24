using Devken.CBC.SchoolManagement.Application.DTOs.Library;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Library;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Library
{
    public class LibraryFineService : ILibraryFineService
    {
        private readonly IRepositoryManager _repository;

        public LibraryFineService(IRepositoryManager repository)
        {
            _repository = repository;
        }

        public async Task<LibraryFineDto> CreateFineAsync(CreateLibraryFineDto dto, Guid? userSchoolId)
        {
            // Validate borrow item exists
            var borrowItem = await _repository.BookBorrowItem.GetByIdWithDetailsAsync(dto.BorrowItemId);
            if (borrowItem == null)
                throw new KeyNotFoundException("Borrow item not found");

            if (userSchoolId.HasValue && borrowItem.TenantId != userSchoolId.Value)
                throw new UnauthorizedAccessException("Access denied to this borrow item");

            // Create fine
            var fine = new LibraryFine
            {
                Id = Guid.NewGuid(),
                BorrowItemId = dto.BorrowItemId,
                Amount = dto.Amount,
                Reason = dto.Reason,
                IssuedOn = dto.IssuedOn ?? DateTime.UtcNow,
                IsPaid = false,
                TenantId = borrowItem.TenantId
            };

            _repository.LibraryFine.Create(fine);
            await _repository.SaveAsync();

            return await GetFineByIdAsync(fine.Id, userSchoolId);
        }

        public async Task<LibraryFineDto> GetFineByIdAsync(Guid id, Guid? userSchoolId)
        {
            var fine = await _repository.LibraryFine.GetByIdWithDetailsAsync(id);
            if (fine == null)
                throw new KeyNotFoundException("Fine not found");

            if (userSchoolId.HasValue && fine.TenantId != userSchoolId.Value)
                throw new UnauthorizedAccessException("Access denied to this fine");

            return MapToDto(fine);
        }

        public async Task<IEnumerable<LibraryFineDto>> GetAllFinesAsync(Guid? userSchoolId)
        {
            var query = _repository.LibraryFine.FindAll(trackChanges: false);

            if (userSchoolId.HasValue)
                query = query.Where(f => f.TenantId == userSchoolId.Value);

            var fines = await query
                .Include(f => f.BorrowItem)
                    .ThenInclude(bi => bi.Borrow)
                        .ThenInclude(b => b.Member)
                .Include(f => f.BorrowItem)
                    .ThenInclude(bi => bi.BookCopy)
                        .ThenInclude(bc => bc.Book)
                .OrderByDescending(f => f.IssuedOn)
                .ToListAsync();

            return fines.Select(MapToDto);
        }

        public async Task<IEnumerable<LibraryFineDto>> GetUnpaidFinesAsync(Guid? userSchoolId)
        {
            var fines = await _repository.LibraryFine.GetUnpaidFinesAsync();

            if (userSchoolId.HasValue)
                fines = fines.Where(f => f.TenantId == userSchoolId.Value);

            return fines.Select(MapToDto);
        }

        public async Task<IEnumerable<LibraryFineDto>> GetFinesByMemberIdAsync(Guid memberId, Guid? userSchoolId)
        {
            var fines = await _repository.LibraryFine.GetFinesByMemberIdAsync(memberId);

            if (userSchoolId.HasValue)
                fines = fines.Where(f => f.TenantId == userSchoolId.Value);

            return fines.Select(MapToDto);
        }

        public async Task<LibraryFineDto> PayFineAsync(PayFineDto dto, Guid? userSchoolId)
        {
            var fine = await _repository.LibraryFine.GetByIdAsync(dto.FineId);
            if (fine == null)
                throw new KeyNotFoundException("Fine not found");

            if (userSchoolId.HasValue && fine.TenantId != userSchoolId.Value)
                throw new UnauthorizedAccessException("Access denied to this fine");

            if (fine.IsPaid)
                throw new InvalidOperationException("Fine has already been paid");

            fine.IsPaid = true;
            fine.PaidOn = dto.PaymentDate ?? DateTime.UtcNow;

            _repository.LibraryFine.Update(fine);
            await _repository.SaveAsync();

            return await GetFineByIdAsync(dto.FineId, userSchoolId);
        }

        public async Task<IEnumerable<LibraryFineDto>> PayMultipleFinesAsync(PayMultipleFinesDto dto, Guid? userSchoolId)
        {
            var paidFines = new List<LibraryFineDto>();

            foreach (var fineId in dto.FineIds)
            {
                var payDto = new PayFineDto
                {
                    FineId = fineId,
                    PaymentDate = dto.PaymentDate
                };

                var paidFine = await PayFineAsync(payDto, userSchoolId);
                paidFines.Add(paidFine);
            }

            return paidFines;
        }

        public async Task WaiveFineAsync(WaiveFineDto dto, Guid? userSchoolId)
        {
            var fine = await _repository.LibraryFine.GetByIdAsync(dto.FineId);
            if (fine == null)
                throw new KeyNotFoundException("Fine not found");

            if (userSchoolId.HasValue && fine.TenantId != userSchoolId.Value)
                throw new UnauthorizedAccessException("Access denied to this fine");

            if (fine.IsPaid)
                throw new InvalidOperationException("Cannot waive a fine that has been paid");

            // Mark as paid with a note that it was waived
            fine.IsPaid = true;
            fine.PaidOn = DateTime.UtcNow;
            fine.Reason = $"{fine.Reason} [WAIVED: {dto.Reason}]";

            _repository.LibraryFine.Update(fine);
            await _repository.SaveAsync();
        }

        public async Task DeleteFineAsync(Guid id, Guid? userSchoolId)
        {
            var fine = await _repository.LibraryFine.GetByIdAsync(id);
            if (fine == null)
                throw new KeyNotFoundException("Fine not found");

            if (userSchoolId.HasValue && fine.TenantId != userSchoolId.Value)
                throw new UnauthorizedAccessException("Access denied to this fine");

            if (fine.IsPaid)
                throw new InvalidOperationException("Cannot delete a paid fine");

            _repository.LibraryFine.Delete(fine);
            await _repository.SaveAsync();
        }

        public async Task<decimal> GetTotalUnpaidFinesForMemberAsync(Guid memberId, Guid? userSchoolId)
        {
            var member = await _repository.LibraryMember.GetByIdAsync(memberId);
            if (member == null)
                throw new KeyNotFoundException("Library member not found");

            if (userSchoolId.HasValue && member.TenantId != userSchoolId.Value)
                throw new UnauthorizedAccessException("Access denied to this library member");

            return await _repository.LibraryFine.GetTotalUnpaidFinesForMemberAsync(memberId);
        }

        public async Task<decimal> GetTotalPaidFinesForMemberAsync(Guid memberId, Guid? userSchoolId)
        {
            var member = await _repository.LibraryMember.GetByIdAsync(memberId);
            if (member == null)
                throw new KeyNotFoundException("Library member not found");

            if (userSchoolId.HasValue && member.TenantId != userSchoolId.Value)
                throw new UnauthorizedAccessException("Access denied to this library member");

            return await _repository.LibraryFine.GetTotalPaidFinesForMemberAsync(memberId);
        }

        private LibraryFineDto MapToDto(LibraryFine fine)
        {
            return new LibraryFineDto
            {
                Id = fine.Id,
                BorrowItemId = fine.BorrowItemId,
                Amount = fine.Amount,
                IsPaid = fine.IsPaid,
                IssuedOn = fine.IssuedOn,
                PaidOn = fine.PaidOn,
                Reason = fine.Reason
            };
        }
    }
}