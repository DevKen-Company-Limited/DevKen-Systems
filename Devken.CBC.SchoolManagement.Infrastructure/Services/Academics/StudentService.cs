using Devken.CBC.SchoolManagement.Application.DTOs.Academics;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Domain.Enums.Students;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Academic
{
    public class StudentService : IStudentService
    {
        private readonly IRepositoryManager _repository;
        private readonly ILogger<StudentService> _logger;

        public StudentService(IRepositoryManager repository, ILogger<StudentService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<(bool Success, string Message, StudentResponse? Student)> CreateStudentAsync(
            CreateStudentRequest request, Guid tenantId)
        {
            try
            {
                // Validate admission number uniqueness
                if (await _repository.Student.AdmissionNumberExistsAsync(request.AdmissionNumber, tenantId))
                {
                    return (false, "Admission number already exists", null);
                }

                // Validate NEMIS number if provided
                if (!string.IsNullOrWhiteSpace(request.NemisNumber))
                {
                    if (await _repository.Student.NemisNumberExistsAsync(request.NemisNumber, tenantId))
                    {
                        return (false, "NEMIS number already exists", null);
                    }
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
                    PlaceOfBirth = request.PlaceOfBirth?.Trim(),
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
                    PrimaryGuardianOccupation = request.PrimaryGuardianOccupation?.Trim(),
                    PrimaryGuardianAddress = request.PrimaryGuardianAddress?.Trim(),
                    SecondaryGuardianName = request.SecondaryGuardianName?.Trim(),
                    SecondaryGuardianRelationship = request.SecondaryGuardianRelationship?.Trim(),
                    SecondaryGuardianPhone = request.SecondaryGuardianPhone?.Trim(),
                    SecondaryGuardianEmail = request.SecondaryGuardianEmail?.Trim(),
                    SecondaryGuardianOccupation = request.SecondaryGuardianOccupation?.Trim(),
                    EmergencyContactName = request.EmergencyContactName?.Trim(),
                    EmergencyContactPhone = request.EmergencyContactPhone?.Trim(),
                    EmergencyContactRelationship = request.EmergencyContactRelationship?.Trim(),
                    PhotoUrl = request.PhotoUrl?.Trim(),
                    Notes = request.Notes?.Trim(),
                    IsActive = true
                };

                _repository.Student.Create(student);
                await _repository.SaveAsync();

                _logger.LogInformation("Student {AdmissionNumber} created successfully in tenant {TenantId}",
                    student.AdmissionNumber, tenantId);

                var response = await GetStudentByIdAsync(student.Id, tenantId);
                return (true, "Student created successfully", response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating student in tenant {TenantId}", tenantId);
                return (false, "An error occurred while creating the student", null);
            }
        }

        public async Task<(bool Success, string Message, StudentResponse? Student)> UpdateStudentAsync(
            UpdateStudentRequest request, Guid tenantId)
        {
            try
            {
                var student = await _repository.Student.GetByIdAsync(request.Id);

                if (student == null || student.TenantId != tenantId)
                {
                    return (false, "Student not found", null);
                }

                // Validate NEMIS number if changed
                if (!string.IsNullOrWhiteSpace(request.NemisNumber) && request.NemisNumber != student.NemisNumber)
                {
                    if (await _repository.Student.NemisNumberExistsAsync(request.NemisNumber, tenantId, student.Id))
                    {
                        return (false, "NEMIS number already exists", null);
                    }
                }

                // Update only provided fields
                if (request.FirstName != null) student.FirstName = request.FirstName.Trim();
                if (request.MiddleName != null) student.MiddleName = request.MiddleName.Trim();
                if (request.LastName != null) student.LastName = request.LastName.Trim();
                if (request.NemisNumber != null) student.NemisNumber = request.NemisNumber.Trim();
                if (request.BirthCertificateNumber != null) student.BirthCertificateNumber = request.BirthCertificateNumber.Trim();
                if (request.DateOfBirth.HasValue) student.DateOfBirth = request.DateOfBirth.Value;
                if (request.Gender.HasValue) student.Gender = request.Gender.Value;
                if (request.PlaceOfBirth != null) student.PlaceOfBirth = request.PlaceOfBirth.Trim();
                if (request.Nationality != null) student.Nationality = request.Nationality.Trim();
                if (request.County != null) student.County = request.County.Trim();
                if (request.SubCounty != null) student.SubCounty = request.SubCounty.Trim();
                if (request.HomeAddress != null) student.HomeAddress = request.HomeAddress.Trim();
                if (request.Religion != null) student.Religion = request.Religion.Trim();
                if (request.CurrentLevel.HasValue) student.CurrentLevel = request.CurrentLevel.Value;
                if (request.CurrentClassId.HasValue) student.CurrentClassId = request.CurrentClassId.Value;
                if (request.CurrentAcademicYearId.HasValue) student.CurrentAcademicYearId = request.CurrentAcademicYearId;
                if (request.StudentStatus.HasValue) student.StudentStatus = request.StudentStatus.Value;
                if (request.PreviousSchool != null) student.PreviousSchool = request.PreviousSchool.Trim();
                if (request.BloodGroup != null) student.BloodGroup = request.BloodGroup.Trim();
                if (request.MedicalConditions != null) student.MedicalConditions = request.MedicalConditions.Trim();
                if (request.Allergies != null) student.Allergies = request.Allergies.Trim();
                if (request.SpecialNeeds != null) student.SpecialNeeds = request.SpecialNeeds.Trim();
                if (request.RequiresSpecialSupport.HasValue) student.RequiresSpecialSupport = request.RequiresSpecialSupport.Value;
                if (request.PrimaryGuardianName != null) student.PrimaryGuardianName = request.PrimaryGuardianName.Trim();
                if (request.PrimaryGuardianRelationship != null) student.PrimaryGuardianRelationship = request.PrimaryGuardianRelationship.Trim();
                if (request.PrimaryGuardianPhone != null) student.PrimaryGuardianPhone = request.PrimaryGuardianPhone.Trim();
                if (request.PrimaryGuardianEmail != null) student.PrimaryGuardianEmail = request.PrimaryGuardianEmail.Trim();
                if (request.PrimaryGuardianOccupation != null) student.PrimaryGuardianOccupation = request.PrimaryGuardianOccupation.Trim();
                if (request.PrimaryGuardianAddress != null) student.PrimaryGuardianAddress = request.PrimaryGuardianAddress.Trim();
                if (request.SecondaryGuardianName != null) student.SecondaryGuardianName = request.SecondaryGuardianName.Trim();
                if (request.SecondaryGuardianRelationship != null) student.SecondaryGuardianRelationship = request.SecondaryGuardianRelationship.Trim();
                if (request.SecondaryGuardianPhone != null) student.SecondaryGuardianPhone = request.SecondaryGuardianPhone.Trim();
                if (request.SecondaryGuardianEmail != null) student.SecondaryGuardianEmail = request.SecondaryGuardianEmail.Trim();
                if (request.SecondaryGuardianOccupation != null) student.SecondaryGuardianOccupation = request.SecondaryGuardianOccupation.Trim();
                if (request.EmergencyContactName != null) student.EmergencyContactName = request.EmergencyContactName.Trim();
                if (request.EmergencyContactPhone != null) student.EmergencyContactPhone = request.EmergencyContactPhone.Trim();
                if (request.EmergencyContactRelationship != null) student.EmergencyContactRelationship = request.EmergencyContactRelationship.Trim();
                if (request.PhotoUrl != null) student.PhotoUrl = request.PhotoUrl.Trim();
                if (request.Notes != null) student.Notes = request.Notes.Trim();
                if (request.IsActive.HasValue) student.IsActive = request.IsActive.Value;

                _repository.Student.Update(student);
                await _repository.SaveAsync();

                _logger.LogInformation("Student {StudentId} updated successfully", request.Id);

                var response = await GetStudentByIdAsync(student.Id, tenantId);
                return (true, "Student updated successfully", response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student {StudentId}", request.Id);
                return (false, "An error occurred while updating the student", null);
            }
        }

        public async Task<StudentResponse?> GetStudentByIdAsync(Guid studentId, Guid tenantId)
        {
            var student = await _repository.Student.GetStudentWithDetailsAsync(studentId, tenantId);
            return student != null ? MapToStudentResponse(student) : null;
        }

        public async Task<StudentResponse?> GetStudentByAdmissionNumberAsync(string admissionNumber, Guid tenantId)
        {
            var student = await _repository.Student.GetByAdmissionNumberAsync(admissionNumber, tenantId);
            return student != null ? MapToStudentResponse(student) : null;
        }

        public async Task<StudentPagedResponse> GetStudentsPagedAsync(StudentSearchRequest request, Guid tenantId)
        {
            var (students, totalCount) = await _repository.Student.GetStudentsPagedAsync(
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
                Students = students.Select(MapToStudentListItem).ToList(),
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        public async Task<List<StudentListItemResponse>> GetStudentsByClassAsync(Guid classId, Guid tenantId)
        {
            var students = await _repository.Student.GetStudentsByClassAsync(classId, tenantId);
            return students.Select(MapToStudentListItem).ToList();
        }

        public async Task<List<StudentListItemResponse>> GetStudentsByLevelAsync(CBCLevel level, Guid tenantId)
        {
            var students = await _repository.Student.GetStudentsByLevelAsync(level, tenantId);
            return students.Select(MapToStudentListItem).ToList();
        }

        public async Task<List<StudentListItemResponse>> SearchStudentsAsync(string searchTerm, Guid tenantId)
        {
            var students = await _repository.Student.SearchStudentsAsync(searchTerm, tenantId);
            return students.Select(MapToStudentListItem).ToList();
        }

        public async Task<(bool Success, string Message)> TransferStudentAsync(TransferStudentRequest request, Guid tenantId)
        {
            try
            {
                var student = await _repository.Student.GetByIdAsync(request.StudentId);

                if (student == null || student.TenantId != tenantId)
                {
                    return (false, "Student not found");
                }

                student.CurrentClassId = request.NewClassId;
                if (request.NewLevel.HasValue)
                {
                    student.CurrentLevel = request.NewLevel.Value;
                }

                _repository.Student.Update(student);
                await _repository.SaveAsync();

                _logger.LogInformation("Student {StudentId} transferred to class {ClassId}",
                    request.StudentId, request.NewClassId);

                return (true, "Student transferred successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transferring student {StudentId}", request.StudentId);
                return (false, "An error occurred while transferring the student");
            }
        }

        public async Task<(bool Success, string Message)> WithdrawStudentAsync(WithdrawStudentRequest request, Guid tenantId)
        {
            try
            {
                var student = await _repository.Student.GetByIdAsync(request.StudentId);

                if (student == null || student.TenantId != tenantId)
                {
                    return (false, "Student not found");
                }

                student.DateOfLeaving = request.DateOfLeaving;
                student.LeavingReason = request.Reason;
                student.StudentStatus = request.NewStatus;
                student.IsActive = false;

                _repository.Student.Update(student);
                await _repository.SaveAsync();

                _logger.LogInformation("Student {StudentId} withdrawn from school", request.StudentId);

                return (true, "Student withdrawn successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error withdrawing student {StudentId}", request.StudentId);
                return (false, "An error occurred while withdrawing the student");
            }
        }

        public async Task<(bool Success, string Message)> DeleteStudentAsync(Guid studentId, Guid tenantId)
        {
            try
            {
                var success = await _repository.Student.SoftDeleteStudentAsync(studentId, tenantId);

                if (!success)
                {
                    return (false, "Student not found");
                }

                _logger.LogInformation("Student {StudentId} deleted (soft delete)", studentId);
                return (true, "Student deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student {StudentId}", studentId);
                return (false, "An error occurred while deleting the student");
            }
        }

        public async Task<(bool Success, string Message)> RestoreStudentAsync(Guid studentId, Guid tenantId)
        {
            try
            {
                var success = await _repository.Student.RestoreStudentAsync(studentId, tenantId);

                if (!success)
                {
                    return (false, "Student not found");
                }

                _logger.LogInformation("Student {StudentId} restored", studentId);
                return (true, "Student restored successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring student {StudentId}", studentId);
                return (false, "An error occurred while restoring the student");
            }
        }

        public async Task<StudentStatisticsResponse> GetStudentStatisticsAsync(Guid tenantId)
        {
            var allStudents = await _repository.Student.GetStudentsBySchoolAsync(tenantId, includeInactive: true);
            var activeStudents = allStudents.Where(s => s.IsActive).ToList();

            var levelCounts = await _repository.Student.GetStudentCountByLevelAsync(tenantId);

            return new StudentStatisticsResponse
            {
                TotalStudents = allStudents.Count,
                ActiveStudents = activeStudents.Count,
                InactiveStudents = allStudents.Count - activeStudents.Count,
                MaleStudents = activeStudents.Count(s => s.Gender == Gender.Male),
                FemaleStudents = activeStudents.Count(s => s.Gender == Gender.Female),
                StudentsWithSpecialNeeds = activeStudents.Count(s => s.RequiresSpecialSupport),
                StudentsByLevel = levelCounts.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
                StudentsByStatus = allStudents.GroupBy(s => s.StudentStatus)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count())
            };
        }

        public async Task<List<StudentListItemResponse>> GetStudentsWithSpecialNeedsAsync(Guid tenantId)
        {
            var students = await _repository.Student.GetStudentsWithSpecialNeedsAsync(tenantId);
            return students.Select(MapToStudentListItem).ToList();
        }

        public async Task<List<StudentListItemResponse>> GetStudentsWithPendingFeesAsync(Guid tenantId)
        {
            var students = await _repository.Student.GetStudentsWithPendingFeesAsync(tenantId);
            return students.Select(MapToStudentListItem).ToList();
        }

        public async Task<bool> ValidateAdmissionNumberAsync(string admissionNumber, Guid tenantId, Guid? excludeStudentId = null)
        {
            return !await _repository.Student.AdmissionNumberExistsAsync(admissionNumber, tenantId, excludeStudentId);
        }

        public async Task<bool> ValidateNemisNumberAsync(string nemisNumber, Guid tenantId, Guid? excludeStudentId = null)
        {
            return !await _repository.Student.NemisNumberExistsAsync(nemisNumber, tenantId, excludeStudentId);
        }

        #region Helper Methods

        private StudentResponse MapToStudentResponse(Student student)
        {
            return new StudentResponse
            {
                Id = student.Id,
                FirstName = student.FirstName,
                MiddleName = student.MiddleName,
                LastName = student.LastName,
                FullName = student.FullName,
                AdmissionNumber = student.AdmissionNumber,
                NemisNumber = student.NemisNumber,
                BirthCertificateNumber = student.BirthCertificateNumber,
                DateOfBirth = student.DateOfBirth,
                Age = student.Age,
                Gender = student.Gender,
                PlaceOfBirth = student.PlaceOfBirth,
                Nationality = student.Nationality,
                County = student.County,
                SubCounty = student.SubCounty,
                HomeAddress = student.HomeAddress,
                Religion = student.Religion,
                DateOfAdmission = student.DateOfAdmission,
                CurrentLevel = student.CurrentLevel,
                CurrentLevelName = student.CurrentLevel.ToString(),
                CurrentClassId = student.CurrentClassId,
                CurrentClassName = student.CurrentClass?.Name,
                CurrentAcademicYearId = student.CurrentAcademicYearId,
                StudentStatus = student.StudentStatus,
                PreviousSchool = student.PreviousSchool,
                DateOfLeaving = student.DateOfLeaving,
                LeavingReason = student.LeavingReason,
                BloodGroup = student.BloodGroup,
                MedicalConditions = student.MedicalConditions,
                Allergies = student.Allergies,
                SpecialNeeds = student.SpecialNeeds,
                RequiresSpecialSupport = student.RequiresSpecialSupport,
                PrimaryGuardianName = student.PrimaryGuardianName,
                PrimaryGuardianRelationship = student.PrimaryGuardianRelationship,
                PrimaryGuardianPhone = student.PrimaryGuardianPhone,
                PrimaryGuardianEmail = student.PrimaryGuardianEmail,
                PrimaryGuardianOccupation = student.PrimaryGuardianOccupation,
                SecondaryGuardianName = student.SecondaryGuardianName,
                SecondaryGuardianPhone = student.SecondaryGuardianPhone,
                EmergencyContactName = student.EmergencyContactName,
                EmergencyContactPhone = student.EmergencyContactPhone,
                PhotoUrl = student.PhotoUrl,
                Notes = student.Notes,
                IsActive = student.IsActive,
                CreatedOn = student.CreatedOn,
                UpdatedOn = student.UpdatedOn
            };
        }

        private StudentListItemResponse MapToStudentListItem(Student student)
        {
            return new StudentListItemResponse
            {
                Id = student.Id,
                FullName = student.FullName,
                AdmissionNumber = student.AdmissionNumber,
                Gender = student.Gender,
                Age = student.Age,
                CurrentLevel = student.CurrentLevel,
                CurrentLevelName = student.CurrentLevel.ToString(),
                CurrentClassName = student.CurrentClass?.Name,
                StudentStatus = student.StudentStatus,
                IsActive = student.IsActive,
                PhotoUrl = student.PhotoUrl
            };
        }

        #endregion
    }
}