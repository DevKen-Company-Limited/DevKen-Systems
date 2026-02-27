// Infrastructure/Data/Repositories/Finance/InvoiceRepository.cs

using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Finance;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Finance
{
    public class InvoiceRepository : RepositoryBase<Invoice, Guid>, IInvoiceRepository
    {
        public InvoiceRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext)
        {
        }

        // ── Queries ───────────────────────────────────────────────────────────

        public async Task<IEnumerable<Invoice>> GetByTenantIdAsync(
            Guid tenantId, bool trackChanges)
        {
            return await FindByCondition(i => i.TenantId == tenantId, trackChanges)
                .Include(i => i.Student)
                .Include(i => i.Term)
                .Include(i => i.Payments)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<Invoice?> GetWithDetailsAsync(
            Guid id, Guid tenantId, bool trackChanges)
        {
            return await FindByCondition(
                    i => i.Id == id && i.TenantId == tenantId,
                    trackChanges)
                .Include(i => i.Student)
                .Include(i => i.AcademicYear)
                .Include(i => i.Term)
                .Include(i => i.Parent)
                .Include(i => i.Items)
                    .ThenInclude(item => item.FeeItem)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Invoice>> GetByStudentIdAsync(
            Guid studentId, Guid tenantId, bool trackChanges)
        {
            return await FindByCondition(
                    i => i.TenantId == tenantId && i.StudentId == studentId,
                    trackChanges)
                .Include(i => i.Term)
                .Include(i => i.Payments)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetByParentIdAsync(
            Guid parentId, Guid tenantId, bool trackChanges)
        {
            return await FindByCondition(
                    i => i.TenantId == tenantId && i.ParentId == parentId,
                    trackChanges)
                .Include(i => i.Student)
                .Include(i => i.Term)
                .Include(i => i.Payments)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<bool> InvoiceNumberExistsAsync(
            string invoiceNumber, Guid tenantId)
        {
            return await FindByCondition(
                    i => i.TenantId == tenantId && i.InvoiceNumber == invoiceNumber,
                    trackChanges: false)
                .AnyAsync();
        }

        // ── Mutations via EF PropertyEntry ────────────────────────────────────
        //
        // Invoice uses `private set` on all its domain properties.
        // EF Core's PropertyEntry.CurrentValue setter bypasses C# access modifiers
        // entirely — it writes directly to the backing field through reflection,
        // which is exactly what EF itself does when hydrating entities from the DB.
        // This is the standard, supported pattern for external mutation of
        // read-only domain entities without changing the domain model.

        /// <summary>
        /// Allocates an empty (uninitialized) Invoice, attaches it to the context
        /// in the Added state, then populates every column via PropertyEntry.
        /// EF Core will INSERT all the values on SaveChanges.
        /// Items can be added to invoice.Items after this call — EF tracks them.
        /// </summary>
        public Invoice CreateNew(
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
            string? notes)
        {
            // GetUninitializedObject skips all constructors — no private-setter conflict.
            var invoice = (Invoice)RuntimeHelpers.GetUninitializedObject(typeof(Invoice));

            // Attach to context so EF tracks it
            _context.Set<Invoice>().Add(invoice);

            var entry = _context.Entry(invoice);

            // ── BaseEntity fields (normally set by RepositoryBase.Create) ─────
            var now = DateTime.UtcNow;
            entry.Property(nameof(Invoice.Id)).CurrentValue = id;
            entry.Property(nameof(Invoice.Status)).CurrentValue = EntityStatus.Active;
            entry.Property(nameof(Invoice.CreatedOn)).CurrentValue = now;
            entry.Property(nameof(Invoice.UpdatedOn)).CurrentValue = now;
            entry.Property(nameof(Invoice.CreatedBy)).CurrentValue = _tenantContext.ActingUserId;
            entry.Property(nameof(Invoice.UpdatedBy)).CurrentValue = _tenantContext.ActingUserId;

            // ── TenantBaseEntity ──────────────────────────────────────────────
            entry.Property(nameof(Invoice.TenantId)).CurrentValue = tenantId;

            // ── Invoice-specific ──────────────────────────────────────────────
            entry.Property(nameof(Invoice.InvoiceNumber)).CurrentValue = invoiceNumber;
            entry.Property(nameof(Invoice.StudentId)).CurrentValue = studentId;
            entry.Property(nameof(Invoice.AcademicYearId)).CurrentValue = academicYearId;
            entry.Property(nameof(Invoice.TermId)).CurrentValue = termId;
            entry.Property(nameof(Invoice.ParentId)).CurrentValue = parentId;
            entry.Property(nameof(Invoice.InvoiceDate)).CurrentValue = invoiceDate;
            entry.Property(nameof(Invoice.DueDate)).CurrentValue = dueDate;
            entry.Property(nameof(Invoice.Description)).CurrentValue = description;
            entry.Property(nameof(Invoice.Notes)).CurrentValue = notes;
            entry.Property(nameof(Invoice.TotalAmount)).CurrentValue = 0m;
            entry.Property(nameof(Invoice.DiscountAmount)).CurrentValue = 0m;
            entry.Property(nameof(Invoice.StatusInvoice)).CurrentValue = InvoiceStatus.Pending;

            return invoice;
        }

        /// <summary>
        /// Overwrites only the four editable header columns on a tracked invoice
        /// using EF PropertyEntry — no private-setter violation.
        /// </summary>
        public void UpdateHeader(
            Invoice invoice,
            DateTime dueDate,
            Guid? parentId,
            string? description,
            string? notes)
        {
            var entry = _context.Entry(invoice);

            entry.Property(nameof(Invoice.DueDate)).CurrentValue = dueDate;
            entry.Property(nameof(Invoice.ParentId)).CurrentValue = parentId;
            entry.Property(nameof(Invoice.Description)).CurrentValue = description;
            entry.Property(nameof(Invoice.Notes)).CurrentValue = notes;
            entry.Property(nameof(Invoice.UpdatedOn)).CurrentValue = DateTime.UtcNow;
            entry.Property(nameof(Invoice.UpdatedBy)).CurrentValue = _tenantContext.ActingUserId;

            // Mark only these properties as modified so EF doesn't touch anything else
            entry.Property(nameof(Invoice.DueDate)).IsModified = true;
            entry.Property(nameof(Invoice.ParentId)).IsModified = true;
            entry.Property(nameof(Invoice.Description)).IsModified = true;
            entry.Property(nameof(Invoice.Notes)).IsModified = true;
            entry.Property(nameof(Invoice.UpdatedOn)).IsModified = true;
            entry.Property(nameof(Invoice.UpdatedBy)).IsModified = true;
        }

        /// <summary>
        /// Soft-deletes by setting Status = Deleted via EF PropertyEntry.
        /// </summary>
        public void SoftDelete(Invoice invoice)
        {
            var entry = _context.Entry(invoice);
            entry.Property(nameof(Invoice.Status)).CurrentValue = EntityStatus.Deleted;
            entry.Property(nameof(Invoice.UpdatedOn)).CurrentValue = DateTime.UtcNow;
            entry.Property(nameof(Invoice.UpdatedBy)).CurrentValue = _tenantContext.ActingUserId;
            entry.Property(nameof(Invoice.Status)).IsModified = true;
            entry.Property(nameof(Invoice.UpdatedOn)).IsModified = true;
            entry.Property(nameof(Invoice.UpdatedBy)).IsModified = true;
        }
    }
}