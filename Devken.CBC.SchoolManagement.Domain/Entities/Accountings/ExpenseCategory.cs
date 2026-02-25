using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Accounting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Accountings
{
    /// <summary>
    /// Categorises expenses for reporting (links to GL accounts).
    /// </summary>
    public class ExpenseCategory : TenantBaseEntity<Guid>
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(20)]
        public string Code { get; set; } = null!;

        [MaxLength(300)]
        public string? Description { get; set; }

        public Guid? GlAccountId { get; set; } // FK → ChartOfAccount.Id

        public Guid? ParentCategoryId { get; set; }

        public bool IsActive { get; set; } = true;

        // ─── Navigation ──────────────────────────────────────────────────────────────

        public ChartOfAccount? GlAccount { get; set; }
        public ExpenseCategory? ParentCategory { get; set; }
        public ICollection<ExpenseCategory> SubCategories { get; set; } = new List<ExpenseCategory>();
        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    }
}
