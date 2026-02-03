using Devken.CBC.SchoolManagement.Application.DTOs.Academics;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Academic
{
    public sealed class StudentService : IStudentService
    {
        private readonly IRepositoryManager _repository;
        private readonly ILogger<StudentService> _logger;

        public StudentService(
            IRepositoryManager repository,
            ILogger<StudentService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        #region Create / Update

        public async Task<(bool Success, string Message, StudentResponse? Student)>
            CreateStudentAsync(CreateStudentRequest request, Guid tenantId)
        {
            if (await _repository.Student.AdmissionNumberExistsAsync(
                    request.AdmissionNumber, tenantId))
            {
                return (false, "Admission number already exists", null);
            }

            if (!string.IsNullOrWhiteSpace(request.NemisNumber) &&
                await _repository.Student.NemisNumberExistsAsync(
                    request.NemisNumber, tenantId))
            {
                return (false, "NEMIS number already exists", null);
            }

            var student = new Student
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FirstName = request.FirstName.Trim(),
                MiddleName = request.MiddleName?.Trim(),
                LastName = request.LastName.Trim(),
                AdmissionNumber = request.AdmissionNumber.Trim(),
                NemisNumber = request.NemisNumber?.Trim(),
                BirthCertificateNumber = request.BirthCertificateNumber?.Trim(),
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                Nationality = request.Nationality?.Trim() ?? "Kenyan",
                County = request.County?.Trim(),
                SubCounty = request.SubCounty?.Trim(),
                HomeAddress = request.HomeAddress?.Trim(),
                Religion = request.Religion?.Trim(),
                DateOfAdmission = request.DateOfAdmission,
                CurrentLevel = request.CurrentLevel,
                CurrentClassId = request.CurrentClassId,
                CurrentAcademicYearId = request.CurrentAcademicYearId,
                StudentStatus = StudentStatus.Active,
                PreviousSchool = request.PreviousSchool?.Trim(),
                BloodGroup = request.BloodGroup?.Trim(),
                MedicalConditions = request.MedicalConditions?.Trim(),
                Allergies = request.Allergies?.Trim(),
                SpecialNeeds = request.SpecialNeeds?.Trim(),
                RequiresSpecialSupport = request.RequiresSpecialSupport,
                PrimaryGuardianName = request.PrimaryGuardianName?.Trim(),
                PrimaryGuardianRelationship = request.PrimaryGuardianRelationship?.Trim(),
                PrimaryGuardianPhone = request.PrimaryGuardianPhone?.Trim(),
                PrimaryGuardianEmail = request.PrimaryGuardianEmail?.Trim(),
                SecondaryGuardianName = request.SecondaryGuardianName?.Trim(),
                SecondaryGuardianPhone = request.SecondaryGuardianPhone?.Trim(),
                EmergencyContactName = request.EmergencyContactName?.Trim(),
                EmergencyContactPhone = request.EmergencyContactPhone?.Trim(),
                PhotoUrl = request.PhotoUrl?.Trim(),
                Notes = request.Notes?.Trim(),
                IsActive = true
            };

            _repository.Student.Create(student);
            await _repository.SaveAsync();

            return (true, "Student created successfully",
                await GetStudentByIdAsync(student.Id, tenantId));
        }

        public async Task<(bool Success, string Message, StudentResponse? Student)>
            UpdateStudentAsync(UpdateStudentRequest request, Guid tenantId)
        {
            var student = await _repository.Student.GetByIdAsync(request.Id);
            if (student == null || student.TenantId != tenantId)
                return (false, "Student not found", null);

            if (request.StudentStatus.HasValue)
                student.StudentStatus = request.StudentStatus.Value;

            if (request.IsActive.HasValue)
                student.IsActive = request.IsActive.Value;

            _repository.Student.Update(student);
            await _repository.SaveAsync();

            return (true, "Student updated successfully",
                await GetStudentByIdAsync(student.Id, tenantId));
        }

        #endregion

        #region Queries

        public async Task<StudentResponse?> GetStudentByIdAsync(Guid studentId, Guid tenantId)
        {
            var student = await _repository.Student
                .GetStudentWithDetailsAsync(studentId, tenantId);

            return student == null ? null : MapToStudentResponse(student);
        }

        public async Task<StudentResponse?> GetStudentByAdmissionNumberAsync(
            string admissionNumber, Guid tenantId)
        {
            var student =
                await _repository.Student.GetByAdmissionNumberAsync(admissionNumber, tenantId);

            return student == null ? null : MapToStudentResponse(student);
        }

        public async Task<StudentPagedResponse> GetStudentsPagedAsync(
            StudentSearchRequest request, Guid tenantId)
        {
            var result =
                await _repository.Student.GetStudentsPagedAsync(
                    tenantId,
                    request.PageNumber,
                    request.PageSize,
                    request.SearchTerm,
                    request.Level,
                    request.ClassId,
                    request.Status,
                    request.IncludeInactive);

            return new StudentPagedResponse
            {
                Students = result.Students.Select(MapToStudentListItem).ToList(),
                TotalCount = result.TotalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        public async Task<List<StudentListItemResponse>>
            GetStudentsByClassAsync(Guid classId, Guid tenantId)
        {
            return (await _repository.Student
                    .GetStudentsByClassAsync(classId, tenantId))
                .Select(MapToStudentListItem)
                .ToList();
        }

        public async Task<List<StudentListItemResponse>>
            GetStudentsByLevelAsync(CBCLevel level, Guid tenantId)
        {
            return (await _repository.Student
                    .GetStudentsByLevelAsync(level, tenantId))
                .Select(MapToStudentListItem)
                .ToList();
        }

        public async Task<List<StudentListItemResponse>>
            SearchStudentsAsync(string searchTerm, Guid tenantId)
        {
            return (await _repository.Student
                    .SearchStudentsAsync(searchTerm, tenantId))
                .Select(MapToStudentListItem)
                .ToList();
        }

        #endregion

        #region Special Lists

        public async Task<List<StudentListItemResponse>>
            GetStudentsWithSpecialNeedsAsync(Guid tenantId)
        {
            return (await _repository.Student
                    .GetStudentsBySchoolAsync(tenantId))
                .Where(s => s.RequiresSpecialSupport)
                .Select(MapToStudentListItem)
                .ToList();
        }

        public async Task<List<StudentListItemResponse>>
            GetStudentsWithPendingFeesAsync(Guid tenantId)
        {
            return (await _repository.Student
                    .GetStudentsWithPendingFeesAsync(tenantId))
                .Select(MapToStudentListItem)
                .ToList();
        }

        #endregion

        #region Statistics & Validation

        public async Task<StudentStatisticsResponse>
            GetStudentStatisticsAsync(Guid tenantId)
        {
            var students =
                await _repository.Student.GetStudentsBySchoolAsync(
                    tenantId, includeInactive: true);

            return new StudentStatisticsResponse
            {
                TotalStudents = students.Count,
                ActiveStudents = students.Count(s => s.IsActive),
                MaleStudents = students.Count(s => s.Gender == Gender.Male),
                FemaleStudents = students.Count(s => s.Gender == Gender.Female)
            };
        }

        public Task<bool>
            ValidateAdmissionNumberAsync(string admissionNumber,
                Guid tenantId, Guid? excludeStudentId = null)
            => _repository.Student.AdmissionNumberExistsAsync(
                    admissionNumber, tenantId, excludeStudentId)
                .ContinueWith(t => !t.Result);

        public Task<bool>
            ValidateNemisNumberAsync(string nemisNumber,
                Guid tenantId, Guid? excludeStudentId = null)
            => _repository.Student.NemisNumberExistsAsync(
                    nemisNumber, tenantId, excludeStudentId)
                .ContinueWith(t => !t.Result);

        #endregion
        #region Actions (Transfer, Withdraw, Restore, Delete)

        public async Task<(bool Success, string Message)> TransferStudentAsync(TransferStudentRequest request, Guid tenantId)
        {
            var student = await _repository.Student.GetByIdAsync(request.StudentId);
            if (student == null || student.TenantId != tenantId)
                return (false, "Student not found");

            student.CurrentClassId = request.NewClassId;
            student.CurrentLevel = (CBCLevel)request.NewLevel;
            _repository.Student.Update(student);
            await _repository.SaveAsync();

            return (true, "Student transferred successfully");
        }

        public async Task<(bool Success, string Message)> WithdrawStudentAsync(WithdrawStudentRequest request, Guid tenantId)
        {
            var student = await _repository.Student.GetByIdAsync(request.StudentId);
            if (student == null || student.TenantId != tenantId)
                return (false, "Student not found");

            student.StudentStatus = StudentStatus.Withdrawn;
            student.IsActive = false;
            student.Notes = string.IsNullOrWhiteSpace(student.Notes)
                ? request.Reason
                : $"{student.Notes}\nWithdrawal Reason: {request.Reason}";

            _repository.Student.Update(student);
            await _repository.SaveAsync();

            return (true, "Student withdrawn successfully");
        }

        public async Task<(bool Success, string Message)> RestoreStudentAsync(Guid studentId, Guid tenantId)
        {
            var student = await _repository.Student.GetByIdAsync(studentId);
            if (student == null || student.TenantId != tenantId)
                return (false, "Student not found");

            student.StudentStatus = StudentStatus.Active;
            student.IsActive = true;

            _repository.Student.Update(student);
            await _repository.SaveAsync();

            return (true, "Student restored successfully");
        }

        public async Task<(bool Success, string Message)> DeleteStudentAsync(Guid studentId, Guid tenantId)
        {
            var student = await _repository.Student.GetByIdAsync(studentId);
            if (student == null || student.TenantId != tenantId)
                return (false, "Student not found");

            _repository.Student.Delete(student);
            await _repository.SaveAsync();

            return (true, "Student deleted successfully");
        }

        #endregion


        #region Mapping

        private static StudentResponse MapToStudentResponse(Student s) => new()
        {
            Id = s.Id,
            FullName = s.FullName,
            AdmissionNumber = s.AdmissionNumber,
            Gender = s.Gender,
            CurrentLevel = s.CurrentLevel,
            StudentStatus = s.StudentStatus,
            IsActive = s.IsActive
        };

        private static StudentListItemResponse MapToStudentListItem(Student s) => new()
        {
            Id = s.Id,
            FullName = s.FullName,
            AdmissionNumber = s.AdmissionNumber,
            Gender = s.Gender,
            CurrentLevel = s.CurrentLevel,
            StudentStatus = s.StudentStatus,
            IsActive = s.IsActive
        };

        #endregion
    }
}
