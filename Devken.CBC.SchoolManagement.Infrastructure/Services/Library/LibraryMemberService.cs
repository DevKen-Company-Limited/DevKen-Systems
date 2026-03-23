using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.DTOs.Library;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Library;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Library
{
    public class LibraryMemberService : ILibraryMemberService
    {
        private readonly IRepositoryManager _repositories;

        public LibraryMemberService(IRepositoryManager repositories)
        {
            _repositories = repositories
                ?? throw new ArgumentNullException(nameof(repositories));
        }

        // ── GET ALL ───────────────────────────────────────────────────────────
        // User data is batch-loaded via GetByIdsAsync — this method does NOT apply
        // the tenant query filter (same pattern as UserActivityService), which is
        // critical for SuperAdmin who may see members across multiple schools.

        public async Task<IEnumerable<LibraryMemberDto>> GetAllMembersAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var members = (await FetchByAccessLevel(schoolId, userSchoolId, isSuperAdmin)).ToList();

            var userIds = members.Select(m => m.UserId).Distinct().ToList();
            var users = (await _repositories.User.GetByIdsAsync(userIds))
                .ToDictionary(u => u.Id);

            return members.Select(m => MapToDto(m, users.GetValueOrDefault(m.UserId)));
        }

        // ── GET BY ID ─────────────────────────────────────────────────────────

        public async Task<LibraryMemberDto> GetMemberByIdAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var member = await _repositories.LibraryMember.GetByIdWithDetailsAsync(id, false)
                ?? throw new NotFoundException($"Library member with ID '{id}' not found.");

            ValidateSchoolAccess(member.TenantId, userSchoolId, isSuperAdmin);

            var users = (await _repositories.User.GetByIdsAsync(new[] { member.UserId }))
                .ToDictionary(u => u.Id);

            return MapToDto(member, users.GetValueOrDefault(member.UserId));
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        public async Task<LibraryMemberDto> CreateMemberAsync(
            CreateLibraryMemberRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var targetSchoolId = ResolveSchoolId(
                request.SchoolId, userSchoolId, isSuperAdmin, isRequired: true);

            await ValidateSchoolExistsAsync(targetSchoolId!.Value);

            // Validate linked user exists
            var user = await _repositories.User.GetByIdAsync(request.UserId, false)
                ?? throw new NotFoundException(
                    $"User with ID '{request.UserId}' not found.");

            // One membership per user per school
            var existingForUser = await _repositories.LibraryMember
                .GetByUserIdAsync(request.UserId, targetSchoolId.Value);
            if (existingForUser != null)
                throw new ConflictException(
                    "This user is already a library member at this school.");

            // ── Auto-generate member number ────────────────────────────────────────
            string memberNumber;

            if (!string.IsNullOrWhiteSpace(request.MemberNumber))
            {
                // Caller supplied one (e.g. data import) — validate uniqueness manually
                memberNumber = request.MemberNumber.Trim();
                await ValidateMemberNumberUniqueAsync(memberNumber, targetSchoolId.Value);
            }
            else
            {
                // Normal path: use the number series
                try
                {
                    memberNumber = await _repositories.DocumentNumberSeries
                        .GenerateAsync("LibraryMember", targetSchoolId.Value);
                }
                catch (InvalidOperationException ex)
                {
                    throw new ValidationException(
                        "No number series is configured for 'LibraryMember' at this school. " +
                        "Ask an administrator to create one before adding members. " +
                        $"Detail: {ex.Message}");
                }
            }
            // ──────────────────────────────────────────────────────────────────────


            var member = new LibraryMember
            {
                Id = Guid.NewGuid(),
                TenantId = targetSchoolId.Value,
                UserId = request.UserId,

                MemberNumber = memberNumber,          // ← was request.MemberNumber.Trim()
                MemberType = request.MemberType,
                JoinedOn = request.JoinedOn?.ToUniversalTime() ?? DateTime.UtcNow,
                IsActive = true
            };

            _repositories.LibraryMember.Create(member);
            await _repositories.SaveAsync();

            var created = await _repositories.LibraryMember
                .GetByIdWithDetailsAsync(member.Id, false);

            // user was already validated and fetched above — reuse it directly
            return MapToDto(created ?? member, user);
        }

        // ── UPDATE ────────────────────────────────────────────────────────────

        public async Task<LibraryMemberDto> UpdateMemberAsync(
            Guid id,
            UpdateLibraryMemberRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var member = await _repositories.LibraryMember.GetByIdAsync(id, true)
                ?? throw new NotFoundException($"Library member with ID '{id}' not found.");

            ValidateSchoolAccess(member.TenantId, userSchoolId, isSuperAdmin);

            // MemberNumber uniqueness (excluding self)
            var existing = await _repositories.LibraryMember
                .GetByMemberNumberAsync(request.MemberNumber.Trim(), member.TenantId);

            if (existing != null && existing.Id != id)
                throw new ConflictException(
                    $"Member number '{request.MemberNumber}' is already in use at this school.");

            member.MemberNumber = request.MemberNumber.Trim();
            member.MemberType = request.MemberType;
            member.IsActive = request.IsActive;

            _repositories.LibraryMember.Update(member);
            await _repositories.SaveAsync();

            var updated = await _repositories.LibraryMember.GetByIdWithDetailsAsync(id, false);

            var users = (await _repositories.User.GetByIdsAsync(new[] { member.UserId }))
                .ToDictionary(u => u.Id);

            return MapToDto(updated ?? member, users.GetValueOrDefault(member.UserId));
        }

        // ── DELETE ────────────────────────────────────────────────────────────

        public async Task DeleteMemberAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var member = await _repositories.LibraryMember.GetByIdWithDetailsAsync(id, true)
                ?? throw new NotFoundException($"Library member with ID '{id}' not found.");

            ValidateSchoolAccess(member.TenantId, userSchoolId, isSuperAdmin);

            if (member.BorrowTransactions.Any())
                throw new ValidationException(
                    "Cannot delete a member who has borrow transactions. " +
                    "Deactivate the member instead.");

            _repositories.LibraryMember.Delete(member);
            await _repositories.SaveAsync();
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private async Task<IEnumerable<LibraryMember>> FetchByAccessLevel(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            if (isSuperAdmin)
            {
                return schoolId.HasValue
                    ? await _repositories.LibraryMember.GetBySchoolIdAsync(schoolId.Value, false)
                    : await _repositories.LibraryMember.GetAllAsync(false);
            }

            if (!userSchoolId.HasValue)
                throw new UnauthorizedException(
                    "You must be assigned to a school to view library members.");

            return await _repositories.LibraryMember
                .GetBySchoolIdAsync(userSchoolId.Value, false);
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
                    "You must be assigned to a school to manage library members.");

            return userSchoolId;
        }

        private void ValidateSchoolAccess(Guid entitySchoolId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin) return;

            if (!userSchoolId.HasValue || entitySchoolId != userSchoolId.Value)
                throw new UnauthorizedException(
                    "You do not have access to this library member.");
        }

        private async Task ValidateSchoolExistsAsync(Guid schoolId)
        {
            var school = await _repositories.School.GetByIdAsync(schoolId, false);
            if (school == null)
                throw new NotFoundException($"School with ID '{schoolId}' not found.");
        }

        private async Task ValidateMemberNumberUniqueAsync(string memberNumber, Guid schoolId)
        {
            var existing = await _repositories.LibraryMember
                .GetByMemberNumberAsync(memberNumber, schoolId);
            if (existing != null)
                throw new ConflictException(
                    $"Member number '{memberNumber}' is already in use at this school.");
        }

        // ── MapToDto ──────────────────────────────────────────────────────────
        // User is passed separately:
        //   • Single-record path  → already loaded via Include in GetByIdWithDetailsAsync
        //   • List path           → batch-loaded from IUserRepository to avoid EF 10622
        private static LibraryMemberDto MapToDto(LibraryMember m, User? user) => new()
        {
            Id = m.Id,
            SchoolId = m.TenantId,
            UserId = m.UserId,
            UserFullName = user?.FullName ?? string.Empty,
            UserEmail = user?.Email ?? string.Empty,
            MemberNumber = m.MemberNumber,
            MemberType = m.MemberType,
            JoinedOn = m.JoinedOn,
            IsActive = m.IsActive,
            TotalBorrows = m.BorrowTransactions?.Count ?? 0
        };
    }
}