using Devken.CBC.SchoolManagement.Application.DTOs.Academics;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Academic
{
    public interface IStudentService
    {
        Task<(bool Success, string Message, StudentResponse? Student)>
            CreateStudentAsync(CreateStudentRequest request, Guid tenantId);

        Task<(bool Success, string Message, StudentResponse? Student)>
            UpdateStudentAsync(UpdateStudentRequest request, Guid tenantId);

        Task<StudentResponse?> GetStudentByIdAsync(Guid studentId, Guid tenantId);

        Task<StudentResponse?> GetStudentByAdmissionNumberAsync(
            string admissionNumber, Guid tenantId);

        Task<StudentPagedResponse> GetStudentsPagedAsync(
            StudentSearchRequest request, Guid tenantId);

        Task<List<StudentListItemResponse>>
            GetStudentsByClassAsync(Guid classId, Guid tenantId);

        Task<List<StudentListItemResponse>>
            GetStudentsByLevelAsync(CBCLevel level, Guid tenantId);

        Task<List<StudentListItemResponse>>
            SearchStudentsAsync(string searchTerm, Guid tenantId);

        Task<List<StudentListItemResponse>>
            GetStudentsWithSpecialNeedsAsync(Guid tenantId);

        Task<List<StudentListItemResponse>>
            GetStudentsWithPendingFeesAsync(Guid tenantId);

        Task<StudentStatisticsResponse>
            GetStudentStatisticsAsync(Guid tenantId);

        Task<bool>
            ValidateAdmissionNumberAsync(
                string admissionNumber, Guid tenantId, Guid? excludeStudentId = null);

        Task<bool>
            ValidateNemisNumberAsync(
                string nemisNumber, Guid tenantId, Guid? excludeStudentId = null);

        Task<(bool Success, string Message)> TransferStudentAsync(TransferStudentRequest request, Guid tenantId);
        Task<(bool Success, string Message)> WithdrawStudentAsync(WithdrawStudentRequest request, Guid tenantId);
        Task<(bool Success, string Message)> RestoreStudentAsync(Guid studentId, Guid tenantId);
        Task<(bool Success, string Message)> DeleteStudentAsync(Guid studentId, Guid tenantId);

    }
}
