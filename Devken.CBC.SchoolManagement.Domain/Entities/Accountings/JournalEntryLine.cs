using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Accounting;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Accountings
{
    /// <summary>
    /// A single debit or credit line within a <see cref="JournalEntry"/>.
    /// </summary>
    public class JournalEntryLine : TenantBaseEntity<Guid>
    {
        // ─── Foreign Keys ────────────────────────────────────────────────────────────

        public Guid JournalEntryId { get; set; }
        public Guid AccountId { get; set; } // FK → ChartOfAccount.Id

        // ─── Line Details ────────────────────────────────────────────────────────────

        public DebitCredit Side { get; set; }
        public decimal Amount { get; set; }

        [MaxLength(300)]
        public string? Description { get; set; }

        // ─── Cost Centre / Department Allocation ─────────────────────────────────────

        [MaxLength(100)]
        public string? CostCentre { get; set; } // e.g. "Administration", "Academic", "Boarding"

        // ─── Navigation ──────────────────────────────────────────────────────────────

        public JournalEntry JournalEntry { get; set; } = null!;
        public ChartOfAccount Account { get; set; } = null!;
    }
}
