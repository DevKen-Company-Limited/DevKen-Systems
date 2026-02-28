// Application/RepositoryManagers/Interfaces/Finance/IInvoiceRepository.cs

using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Finance
{
    public interface IInvoiceRepository : IRepositoryBase<Invoice, Guid>
    {
        // ── Queries ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns all invoices for a tenant with Student, Term and Payments loaded.
        /// Items are NOT included here for performance.
        /// </summary>
        Task<IEnumerable<Invoice>> GetByTenantIdAsync(Guid tenantId, bool trackChanges);

        /// <summary>
        /// Returns a single invoice with all nav props:
        /// Student, AcademicYear, Term, Parent, Items+FeeItem, Payments.
        /// </summary>
        Task<Invoice?> GetWithDetailsAsync(Guid id, Guid tenantId, bool trackChanges);

        /// <summary>Returns invoices for a specific student.</summary>
        Task<IEnumerable<Invoice>> GetByStudentIdAsync(Guid studentId, Guid tenantId, bool trackChanges);

        /// <summary>Returns invoices linked to a specific parent.</summary>
        Task<IEnumerable<Invoice>> GetByParentIdAsync(Guid parentId, Guid tenantId, bool trackChanges);

        /// <summary>Checks whether an invoice number is already in use within a tenant.</summary>
        Task<bool> InvoiceNumberExistsAsync(string invoiceNumber, Guid tenantId);

        // ── Mutations (bypass private setters via EF PropertyEntry) ──────────

        /// <summary>
        /// Creates a new invoice row with all field values set via EF PropertyEntry,
        /// bypassing the entity's private setters entirely.
        /// Items must be added to the returned tracked invoice before SaveAsync.
        /// </summary>
        Invoice CreateNew(
            Guid id,
            Guid tenantId,
            string invoiceNumber,
            Guid studentId,
            Guid academicYearId,
            Guid? termId,
            Guid? parentId,
            DateTime invoiceDate,
            DateTime dueDate,
            string? description,
            string? notes);

        /// <summary>
        /// Updates only the editable header fields on an already-tracked invoice
        /// via EF PropertyEntry, bypassing private setters.
        /// Call SaveAsync after this.
        /// </summary>
        void UpdateHeader(
            Invoice invoice,
            DateTime dueDate,
            Guid? parentId,
            string? description,
            string? notes);

        /// <summary>
        /// Soft-deletes by setting Status = Deleted directly via EF PropertyEntry.
        /// Call SaveAsync after this.
        /// </summary>
        void SoftDelete(Invoice invoice);
    }
}