// Infrastructure/Services/Finance/InvoiceService.cs

using Devken.CBC.SchoolManagement.Application.DTOs.Invoices;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.NumberSeries;
using Devken.CBC.SchoolManagement.Application.Service.Finance;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Finance
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IRepositoryManager _repositories;
        private readonly IDocumentNumberSeriesRepository _documentNumberService;

        private const string INVOICE_NUMBER_SERIES = "Invoice";
        private const string INVOICE_PREFIX = "INV";

        public InvoiceService(
            IRepositoryManager repositories,
            IDocumentNumberSeriesRepository documentNumberService)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
            _documentNumberService = documentNumberService ?? throw new ArgumentNullException(nameof(documentNumberService));
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET ALL
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<InvoiceSummaryResponseDto>> GetAllInvoicesAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin,
            InvoiceQueryDto query)
        {
            IEnumerable<Invoice> invoices;

            if (isSuperAdmin)
            {
                if (!schoolId.HasValue)
                    throw new System.ComponentModel.DataAnnotations.ValidationException(
                        "schoolId is required for SuperAdmin when listing invoices.");

                invoices = await _repositories.Invoice.GetByTenantIdAsync(
                    schoolId.Value, trackChanges: false);
            }
            else
            {
                if (!userSchoolId.HasValue)
                    throw new UnauthorizedException("You must be assigned to a school to view invoices.");

                invoices = await _repositories.Invoice.GetByTenantIdAsync(
                    userSchoolId.Value, trackChanges: false);
            }

            if (query.StudentId.HasValue)
                invoices = invoices.Where(i => i.StudentId == query.StudentId.Value);

            if (query.ParentId.HasValue)
                invoices = invoices.Where(i => i.ParentId == query.ParentId.Value);

            if (query.AcademicYearId.HasValue)
                invoices = invoices.Where(i => i.AcademicYearId == query.AcademicYearId.Value);

            if (query.TermId.HasValue)
                invoices = invoices.Where(i => i.TermId == query.TermId.Value);

            if (query.InvoiceStatus.HasValue)
                invoices = invoices.Where(i => i.StatusInvoice == query.InvoiceStatus.Value);

            if (query.IsOverdue.HasValue && query.IsOverdue.Value)
                invoices = invoices.Where(i => i.IsOverdue);

            if (query.DateFrom.HasValue)
                invoices = invoices.Where(i => i.InvoiceDate >= query.DateFrom.Value);

            if (query.DateTo.HasValue)
                invoices = invoices.Where(i => i.InvoiceDate <= query.DateTo.Value);

            if (query.IsActive.HasValue)
                invoices = query.IsActive.Value
                    ? invoices.Where(i => i.Status == EntityStatus.Active)
                    : invoices.Where(i => i.Status != EntityStatus.Active);

            return invoices.Select(MapToSummaryDto).ToList();
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET BY ID
        // ─────────────────────────────────────────────────────────────────────
        public async Task<InvoiceResponseDto> GetInvoiceByIdAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var invoice = await _repositories.Invoice.GetByIdAsync(id, trackChanges: false)
                ?? throw new NotFoundException($"Invoice with ID '{id}' not found.");

            ValidateAccess(invoice.TenantId, userSchoolId, isSuperAdmin);

            var detailed = await _repositories.Invoice.GetWithDetailsAsync(
                id, invoice.TenantId, trackChanges: false)
                ?? throw new NotFoundException($"Invoice with ID '{id}' not found.");

            return MapToDto(detailed);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET BY STUDENT
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<InvoiceSummaryResponseDto>> GetInvoicesByStudentAsync(
            Guid studentId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (!isSuperAdmin && !userSchoolId.HasValue)
                throw new UnauthorizedException("You must be assigned to a school to view invoices.");

            var student = await _repositories.Student.GetByIdAsync(studentId, trackChanges: false)
                ?? throw new NotFoundException($"Student with ID '{studentId}' not found.");

            ValidateAccess(student.TenantId, userSchoolId, isSuperAdmin);

            var invoices = await _repositories.Invoice.GetByStudentIdAsync(
                studentId, student.TenantId, trackChanges: false);

            return invoices.Select(MapToSummaryDto).ToList();
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET BY PARENT
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<InvoiceSummaryResponseDto>> GetInvoicesByParentAsync(
            Guid parentId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (!isSuperAdmin && !userSchoolId.HasValue)
                throw new UnauthorizedException("You must be assigned to a school to view invoices.");

            var parent = await _repositories.Parent.GetByIdAsync(parentId, trackChanges: false)
                ?? throw new NotFoundException($"Parent with ID '{parentId}' not found.");

            ValidateAccess(parent.TenantId, userSchoolId, isSuperAdmin);

            var invoices = await _repositories.Invoice.GetByParentIdAsync(
                parentId, parent.TenantId, trackChanges: false);

            return invoices.Select(MapToSummaryDto).ToList();
        }

        // ─────────────────────────────────────────────────────────────────────
        // CREATE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<InvoiceResponseDto> CreateInvoiceAsync(
            CreateInvoiceDto dto, Guid? userSchoolId, bool isSuperAdmin)
        {
            var strategy = _repositories.Context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                var tenantId = ResolveTenantId(dto.TenantId, userSchoolId, isSuperAdmin);

                if (!dto.Items.Any())
                    throw new System.ComponentModel.DataAnnotations.ValidationException(
                        "Invoice must contain at least one item.");

                if (dto.DueDate.Date < dto.InvoiceDate.Date)
                    throw new System.ComponentModel.DataAnnotations.ValidationException(
                        "Due date cannot be before the invoice date.");

                var student = await _repositories.Student.GetByIdAsync(dto.StudentId, trackChanges: false)
                    ?? throw new NotFoundException($"Student with ID '{dto.StudentId}' not found.");

                if (student.TenantId != tenantId)
                    throw new System.ComponentModel.DataAnnotations.ValidationException(
                        "Student does not belong to this school.");

                var invoiceNumber = await ResolveInvoiceNumberAsync(tenantId);

                // CreateNew uses EF PropertyEntry internally — no private-setter conflict
                var invoice = _repositories.Invoice.CreateNew(
                    id: Guid.NewGuid(),
                    tenantId: tenantId,
                    invoiceNumber: invoiceNumber,
                    studentId: dto.StudentId,
                    academicYearId: dto.AcademicYearId,
                    termId: dto.TermId,
                    parentId: dto.ParentId,
                    invoiceDate: dto.InvoiceDate.Date,
                    dueDate: dto.DueDate.Date,
                    description: dto.Description?.Trim(),
                    notes: dto.Notes?.Trim());

                // Build and compute line items
                foreach (var itemDto in dto.Items)
                {
                    var item = new InvoiceItem
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        FeeItemId = itemDto.FeeItemId,
                        TermId = itemDto.TermId,
                        Description = itemDto.Description.Trim(),
                        ItemType = itemDto.ItemType?.Trim(),
                        Quantity = itemDto.Quantity,
                        UnitPrice = itemDto.UnitPrice,
                        Discount = itemDto.Discount,
                        IsTaxable = itemDto.IsTaxable,
                        TaxRate = itemDto.TaxRate,
                        GlCode = itemDto.GlCode?.Trim(),
                        Notes = itemDto.Notes?.Trim()
                    };
                    item.Compute();
                    invoice.Items.Add(item);
                }

                // Domain method RecalculateTotals already exists and is public —
                // it only reads Items and writes TotalAmount/StatusInvoice via private set,
                // but since we're calling it on the tracked instance EF already
                // knows about, EF will detect the change automatically via
                // change tracking on the scalar properties.
                invoice.RecalculateTotals();

                await _repositories.SaveAsync();

                var created = await _repositories.Invoice.GetWithDetailsAsync(
                    (Guid)invoice.Id!, tenantId, trackChanges: false)
                    ?? throw new NotFoundException("Failed to reload created invoice.");

                return MapToDto(created);
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE (header fields only)
        // ─────────────────────────────────────────────────────────────────────
        public async Task<InvoiceResponseDto> UpdateInvoiceAsync(
            Guid id, UpdateInvoiceDto dto, Guid? userSchoolId, bool isSuperAdmin)
        {
            var invoice = await _repositories.Invoice.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Invoice with ID '{id}' not found.");

            ValidateAccess(invoice.TenantId, userSchoolId, isSuperAdmin);

            if (invoice.StatusInvoice == InvoiceStatus.Paid)
                throw new System.ComponentModel.DataAnnotations.ValidationException(
                    "Cannot update a paid invoice.");

            if (invoice.StatusInvoice == InvoiceStatus.Cancelled)
                throw new System.ComponentModel.DataAnnotations.ValidationException(
                    "Cannot update a cancelled invoice.");

            if (dto.DueDate.Date < invoice.InvoiceDate.Date)
                throw new System.ComponentModel.DataAnnotations.ValidationException(
                    "Due date cannot be before the invoice date.");

            // UpdateHeader writes via EF PropertyEntry — no private-setter conflict
            _repositories.Invoice.UpdateHeader(
                invoice: invoice,
                dueDate: dto.DueDate.Date,
                parentId: dto.ParentId,
                description: dto.Description?.Trim(),
                notes: dto.Notes?.Trim());

            await _repositories.SaveAsync();

            var updated = await _repositories.Invoice.GetWithDetailsAsync(
                id, invoice.TenantId, trackChanges: false)
                ?? throw new NotFoundException($"Invoice with ID '{id}' not found.");

            return MapToDto(updated);
        }

        // ─────────────────────────────────────────────────────────────────────
        // APPLY DISCOUNT
        // ─────────────────────────────────────────────────────────────────────
        public async Task<InvoiceResponseDto> ApplyDiscountAsync(
            Guid id, ApplyDiscountDto dto, Guid? userSchoolId, bool isSuperAdmin)
        {
            var invoice = await _repositories.Invoice.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Invoice with ID '{id}' not found.");

            ValidateAccess(invoice.TenantId, userSchoolId, isSuperAdmin);

            // ApplyCredit is an existing public domain method — no change needed
            invoice.ApplyCredit(dto.DiscountAmount);

            await _repositories.SaveAsync();

            var updated = await _repositories.Invoice.GetWithDetailsAsync(
                id, invoice.TenantId, trackChanges: false)
                ?? throw new NotFoundException($"Invoice with ID '{id}' not found.");

            return MapToDto(updated);
        }

        // ─────────────────────────────────────────────────────────────────────
        // CANCEL
        // ─────────────────────────────────────────────────────────────────────
        public async Task<InvoiceResponseDto> CancelInvoiceAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var invoice = await _repositories.Invoice.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Invoice with ID '{id}' not found.");

            ValidateAccess(invoice.TenantId, userSchoolId, isSuperAdmin);

            // Cancel() is an existing public domain method — no change needed
            invoice.Cancel();

            await _repositories.SaveAsync();

            var updated = await _repositories.Invoice.GetWithDetailsAsync(
                id, invoice.TenantId, trackChanges: false)
                ?? throw new NotFoundException($"Invoice with ID '{id}' not found.");

            return MapToDto(updated);
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE (soft)
        // ─────────────────────────────────────────────────────────────────────
        public async Task DeleteInvoiceAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var invoice = await _repositories.Invoice.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Invoice with ID '{id}' not found.");

            ValidateAccess(invoice.TenantId, userSchoolId, isSuperAdmin);

            if (invoice.StatusInvoice == InvoiceStatus.Paid)
                throw new System.ComponentModel.DataAnnotations.ValidationException(
                    "Cannot delete a paid invoice. Cancel it instead.");

            // SoftDelete writes Status via EF PropertyEntry — no private-setter conflict
            _repositories.Invoice.SoftDelete(invoice);

            await _repositories.SaveAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private Guid ResolveTenantId(Guid? requestTenantId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin)
            {
                if (!requestTenantId.HasValue || requestTenantId == Guid.Empty)
                    throw new System.ComponentModel.DataAnnotations.ValidationException(
                        "TenantId is required for SuperAdmin when creating an invoice.");
                return requestTenantId.Value;
            }

            if (!userSchoolId.HasValue || userSchoolId == Guid.Empty)
                throw new UnauthorizedException("You must be assigned to a school to create invoices.");

            return userSchoolId.Value;
        }

        private void ValidateAccess(Guid invoiceTenantId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin) return;
            if (!userSchoolId.HasValue || invoiceTenantId != userSchoolId.Value)
                throw new UnauthorizedException("You do not have access to this invoice.");
        }

        private async Task<string> ResolveInvoiceNumberAsync(Guid tenantId)
        {
            var seriesExists = await _documentNumberService
                .SeriesExistsAsync(INVOICE_NUMBER_SERIES, tenantId);

            if (!seriesExists)
            {
                await _documentNumberService.CreateSeriesAsync(
                    entityName: INVOICE_NUMBER_SERIES,
                    tenantId: tenantId,
                    prefix: INVOICE_PREFIX,
                    padding: 6,
                    resetEveryYear: true,
                    description: "Invoice numbers");
            }

            return await _documentNumberService.GenerateAsync(INVOICE_NUMBER_SERIES, tenantId);
        }

        // ─────────────────────────────────────────────────────────────────────
        // MAPPERS
        // ─────────────────────────────────────────────────────────────────────

        private static InvoiceResponseDto MapToDto(Invoice i) => new()
        {
            Id = (Guid)i.Id!,
            TenantId = i.TenantId,
            InvoiceNumber = i.InvoiceNumber,
            StudentId = i.StudentId,
            StudentName = i.Student != null
                                 ? $"{i.Student.FirstName} {i.Student.LastName}".Trim()
                                 : string.Empty,
            AcademicYearId = i.AcademicYearId,
            AcademicYearName = i.AcademicYear?.Name ?? string.Empty,
            TermId = i.TermId,
            TermName = i.Term?.Name,
            ParentId = i.ParentId,
            ParentName = i.Parent != null
                                 ? $"{i.Parent.FirstName} {i.Parent.LastName}".Trim()
                                 : null,
            InvoiceDate = i.InvoiceDate,
            DueDate = i.DueDate,
            Description = i.Description,
            TotalAmount = i.TotalAmount,
            DiscountAmount = i.DiscountAmount,
            AmountPaid = i.AmountPaid,
            Balance = i.Balance,
            StatusInvoice = i.StatusInvoice,
            IsOverdue = i.IsOverdue,
            Notes = i.Notes,
            Status = i.Status.ToString(),
            CreatedOn = i.CreatedOn,
            UpdatedOn = i.UpdatedOn,
            Items = i.Items.Select(item => new InvoiceItemResponseDto
            {
                Id = (Guid)item.Id!,
                FeeItemId = item.FeeItemId,
                Description = item.Description,
                ItemType = item.ItemType,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Discount = item.Discount,
                IsTaxable = item.IsTaxable,
                TaxRate = item.TaxRate,
                Total = item.Total,
                TaxAmount = item.TaxAmount,
                NetAmount = item.NetAmount,
                GlCode = item.GlCode,
                Notes = item.Notes
            }).ToList()
        };

        private static InvoiceSummaryResponseDto MapToSummaryDto(Invoice i) => new()
        {
            Id = (Guid)i.Id!,
            InvoiceNumber = i.InvoiceNumber,
            StudentId = i.StudentId,
            StudentName = i.Student != null
                                ? $"{i.Student.FirstName} {i.Student.LastName}".Trim()
                                : string.Empty,
            InvoiceDate = i.InvoiceDate,
            DueDate = i.DueDate,
            TotalAmount = i.TotalAmount,
            AmountPaid = i.AmountPaid,
            Balance = i.Balance,
            StatusInvoice = i.StatusInvoice,
            IsOverdue = i.IsOverdue,
            TermName = i.Term?.Name,
            Status = i.Status.ToString()
        };
    }
}