using Devken.CBC.SchoolManagement.Application.DTOs.Library;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Library;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Library
{
    public class LibraryFeeService : ILibraryFeeService
    {
        private readonly IRepositoryManager _repositories;

        public LibraryFeeService(IRepositoryManager repositories)
        {
            _repositories = repositories
                ?? throw new ArgumentNullException(nameof(repositories));
        }

        // ── GET ALL ───────────────────────────────────────────────────────────

        public async Task<IEnumerable<LibraryFeeDto>> GetAllFeesAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var fees = (await FetchByAccessLevel(schoolId, userSchoolId, isSuperAdmin)).ToList();
            return fees.Select(MapToDto);
        }

        // ── GET FILTERED ──────────────────────────────────────────────────────

        public async Task<IEnumerable<LibraryFeeDto>> GetFilteredFeesAsync(
            LibraryFeeFilterRequest filter,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            // Non-SuperAdmin can only query their own school
            var resolvedSchoolId = isSuperAdmin ? filter.SchoolId : userSchoolId;

            var fees = await _repositories.LibraryFee.GetFilteredAsync(
                resolvedSchoolId,
                filter.MemberId,
                filter.FeeStatus,
                filter.FeeType,
                filter.FromDate,
                filter.ToDate,
                trackChanges: false);

            return fees.Select(MapToDto);
        }

        // ── GET BY MEMBER ─────────────────────────────────────────────────────

        public async Task<IEnumerable<LibraryFeeDto>> GetFeesByMemberAsync(
            Guid memberId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var member = await _repositories.LibraryMember.GetByIdAsync(memberId, false)
                ?? throw new NotFoundException($"Library member with ID '{memberId}' not found.");

            ValidateSchoolAccess(member.TenantId, userSchoolId, isSuperAdmin);

            var fees = await _repositories.LibraryFee
                .GetByMemberIdAsync(memberId, member.TenantId, trackChanges: false);

            return fees.Select(MapToDto);
        }

        // ── GET BY ID ─────────────────────────────────────────────────────────

        public async Task<LibraryFeeDto> GetFeeByIdAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var fee = await _repositories.LibraryFee.GetByIdWithDetailsAsync(id, false)
                ?? throw new NotFoundException($"Library fee with ID '{id}' not found.");

            ValidateSchoolAccess(fee.TenantId, userSchoolId, isSuperAdmin);

            return MapToDto(fee);
        }

        // ── OUTSTANDING BALANCE ───────────────────────────────────────────────

        public async Task<decimal> GetOutstandingBalanceAsync(
            Guid memberId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var member = await _repositories.LibraryMember.GetByIdAsync(memberId, false)
                ?? throw new NotFoundException($"Library member with ID '{memberId}' not found.");

            ValidateSchoolAccess(member.TenantId, userSchoolId, isSuperAdmin);

            return await _repositories.LibraryFee
                .GetOutstandingBalanceAsync(memberId, member.TenantId);
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        public async Task<LibraryFeeDto> CreateFeeAsync(
            CreateLibraryFeeRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var targetSchoolId = ResolveSchoolId(
                request.SchoolId, userSchoolId, isSuperAdmin, isRequired: true);

            await ValidateSchoolExistsAsync(targetSchoolId!.Value);

            // Validate member exists and belongs to this school
            var member = await _repositories.LibraryMember
                             .GetByIdAsync(request.MemberId, false)
                         ?? throw new NotFoundException(
                             $"Library member with ID '{request.MemberId}' not found.");

            if (member.TenantId != targetSchoolId.Value)
                throw new ValidationException(
                    "The specified member does not belong to this school.");

            // Validate borrow reference if supplied
            if (request.BookBorrowId.HasValue)
            {
                var borrow = await _repositories.BookBorrow
                                 .GetByIdAsync(request.BookBorrowId.Value, false)
                             ?? throw new NotFoundException(
                                 $"Book borrow record with ID '{request.BookBorrowId}' not found.");

                if (borrow.MemberId != request.MemberId)
                    throw new ValidationException(
                        "The specified borrow transaction does not belong to this member.");
            }

            var fee = new LibraryFee
            {
                Id = Guid.NewGuid(),
                TenantId = targetSchoolId.Value,
                MemberId = request.MemberId,
                BookBorrowId = request.BookBorrowId,
                FeeType = request.FeeType,
                Amount = request.Amount,
                AmountPaid = 0,
                FeeStatus = LibraryFeeStatus.Unpaid,
                Description = request.Description,
                FeeDate = (request.FeeDate ?? DateTime.UtcNow).ToUniversalTime()
            };

            _repositories.LibraryFee.Create(fee);
            await _repositories.SaveAsync();

            var created = await _repositories.LibraryFee.GetByIdWithDetailsAsync(fee.Id, false);
            return MapToDto(created ?? fee);
        }

        // ── UPDATE ────────────────────────────────────────────────────────────

        public async Task<LibraryFeeDto> UpdateFeeAsync(
            Guid id,
            UpdateLibraryFeeRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var fee = await _repositories.LibraryFee.GetByIdWithDetailsAsync(id, true)
                      ?? throw new NotFoundException($"Library fee with ID '{id}' not found.");

            ValidateSchoolAccess(fee.TenantId, userSchoolId, isSuperAdmin);

            if (fee.FeeStatus == LibraryFeeStatus.Paid)
                throw new ValidationException(
                    "Cannot update a fee that has already been fully paid.");

            if (fee.FeeStatus == LibraryFeeStatus.Waived)
                throw new ValidationException(
                    "Cannot update a fee that has been waived.");

            // Ensure the new amount is not less than what has already been paid
            if (request.Amount < fee.AmountPaid)
                throw new ValidationException(
                    $"New amount ({request.Amount}) cannot be less than amount already paid ({fee.AmountPaid}).");

            fee.FeeType = request.FeeType;
            fee.Amount = request.Amount;
            fee.Description = request.Description;
            fee.FeeDate = (request.FeeDate ?? fee.FeeDate).ToUniversalTime();

            // Recompute status after amount change
            fee.FeeStatus = ComputeStatus(fee.Amount, fee.AmountPaid);

            _repositories.LibraryFee.Update(fee);
            await _repositories.SaveAsync();

            var updated = await _repositories.LibraryFee.GetByIdWithDetailsAsync(id, false);
            return MapToDto(updated ?? fee);
        }

        // ── RECORD PAYMENT ────────────────────────────────────────────────────

        public async Task<LibraryFeeDto> RecordPaymentAsync(
            Guid id,
            RecordLibraryFeePaymentRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var fee = await _repositories.LibraryFee.GetByIdWithDetailsAsync(id, true)
                      ?? throw new NotFoundException($"Library fee with ID '{id}' not found.");

            ValidateSchoolAccess(fee.TenantId, userSchoolId, isSuperAdmin);

            if (fee.FeeStatus == LibraryFeeStatus.Paid)
                throw new ValidationException("This fee has already been fully paid.");

            if (fee.FeeStatus == LibraryFeeStatus.Waived)
                throw new ValidationException("Cannot record payment for a waived fee.");

            var remaining = fee.Amount - fee.AmountPaid;

            if (request.AmountPaid > remaining)
                throw new ValidationException(
                    $"Payment amount ({request.AmountPaid}) exceeds the outstanding balance ({remaining}).");

            fee.AmountPaid += request.AmountPaid;
            fee.FeeStatus = ComputeStatus(fee.Amount, fee.AmountPaid);

            if (fee.FeeStatus == LibraryFeeStatus.Paid)
                fee.PaidOn = (request.PaidOn ?? DateTime.UtcNow).ToUniversalTime();

            _repositories.LibraryFee.Update(fee);
            await _repositories.SaveAsync();

            var updated = await _repositories.LibraryFee.GetByIdWithDetailsAsync(id, false);
            return MapToDto(updated ?? fee);
        }

        // ── WAIVE ─────────────────────────────────────────────────────────────

        public async Task<LibraryFeeDto> WaiveFeeAsync(
            Guid id,
            WaiveLibraryFeeRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var fee = await _repositories.LibraryFee.GetByIdWithDetailsAsync(id, true)
                      ?? throw new NotFoundException($"Library fee with ID '{id}' not found.");

            ValidateSchoolAccess(fee.TenantId, userSchoolId, isSuperAdmin);

            if (fee.FeeStatus == LibraryFeeStatus.Paid)
                throw new ValidationException("Cannot waive a fee that has already been fully paid.");

            if (fee.FeeStatus == LibraryFeeStatus.Waived)
                throw new ValidationException("This fee has already been waived.");

            fee.FeeStatus = LibraryFeeStatus.Waived;
            fee.WaivedReason = request.Reason;

            _repositories.LibraryFee.Update(fee);
            await _repositories.SaveAsync();

            var updated = await _repositories.LibraryFee.GetByIdWithDetailsAsync(id, false);
            return MapToDto(updated ?? fee);
        }

        // ── DELETE ────────────────────────────────────────────────────────────

        public async Task DeleteFeeAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var fee = await _repositories.LibraryFee.GetByIdWithDetailsAsync(id, true)
                      ?? throw new NotFoundException($"Library fee with ID '{id}' not found.");

            ValidateSchoolAccess(fee.TenantId, userSchoolId, isSuperAdmin);

            if (fee.FeeStatus == LibraryFeeStatus.Paid)
                throw new ValidationException(
                    "Cannot delete a fee that has been paid. Waive it instead.");

            _repositories.LibraryFee.Delete(fee);
            await _repositories.SaveAsync();
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private async Task<IEnumerable<LibraryFee>> FetchByAccessLevel(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            if (isSuperAdmin)
            {
                return schoolId.HasValue
                    ? await _repositories.LibraryFee.GetBySchoolIdAsync(schoolId.Value, false)
                    : await _repositories.LibraryFee.GetAllAsync(false);
            }

            if (!userSchoolId.HasValue)
                throw new UnauthorizedException(
                    "You must be assigned to a school to view library fees.");

            return await _repositories.LibraryFee.GetBySchoolIdAsync(userSchoolId.Value, false);
        }

        private Guid? ResolveSchoolId(
            Guid? requestSchoolId,
            Guid? userSchoolId,
            bool isSuperAdmin,
            bool isRequired)
        {
            if (isSuperAdmin)
            {
                if (isRequired && (!requestSchoolId.HasValue || requestSchoolId.Value == Guid.Empty))
                    throw new ValidationException("SchoolId is required for SuperAdmin.");
                return requestSchoolId;
            }

            if (!userSchoolId.HasValue || userSchoolId.Value == Guid.Empty)
                throw new UnauthorizedException(
                    "You must be assigned to a school to manage library fees.");

            return userSchoolId;
        }

        private void ValidateSchoolAccess(Guid entitySchoolId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin) return;

            if (!userSchoolId.HasValue || entitySchoolId != userSchoolId.Value)
                throw new UnauthorizedException(
                    "You do not have access to this library fee.");
        }

        private async Task ValidateSchoolExistsAsync(Guid schoolId)
        {
            var school = await _repositories.School.GetByIdAsync(schoolId, false);
            if (school is null)
                throw new NotFoundException($"School with ID '{schoolId}' not found.");
        }

        private static LibraryFeeStatus ComputeStatus(decimal amount, decimal amountPaid)
        {
            if (amountPaid <= 0) return LibraryFeeStatus.Unpaid;
            if (amountPaid >= amount) return LibraryFeeStatus.Paid;
            return LibraryFeeStatus.PartiallyPaid;
        }

        // ── MAP ───────────────────────────────────────────────────────────────

        private static LibraryFeeDto MapToDto(LibraryFee f) => new()
        {
            Id = f.Id,
            SchoolId = f.TenantId,
            SchoolName = f.School?.Name ?? "N/A",
            MemberId = f.MemberId,
            MemberNumber = f.Member?.MemberNumber ?? string.Empty,
            UserFullName = f.Member?.User != null
                ? $"{f.Member.User.FirstName} {f.Member.User.LastName}".Trim()
                : "Name Not Found",
            BookBorrowId = f.BookBorrowId,
            FeeType = f.FeeType,
            FeeTypeDisplay = f.FeeType.ToString(),
            Amount = f.Amount,
            AmountPaid = f.AmountPaid,
            Balance = f.Amount - f.AmountPaid,
            FeeStatus = f.FeeStatus,
            FeeStatusDisplay = f.FeeStatus.ToString(),
            Description = f.Description,
            FeeDate = f.FeeDate,
            PaidOn = f.PaidOn,
            WaivedReason = f.WaivedReason
        };
    }
}