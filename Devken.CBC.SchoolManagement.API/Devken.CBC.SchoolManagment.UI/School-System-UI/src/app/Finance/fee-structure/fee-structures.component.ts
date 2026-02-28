import {
  Component, OnInit, OnDestroy, AfterViewInit,
  TemplateRef, ViewChild, ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Subject } from 'rxjs';
import { take, takeUntil } from 'rxjs/operators';

import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { FeeStructureService } from 'app/core/DevKenService/Finance/FeeStructureService';
import {
  FeeStructureDto,
  CBC_LEVEL_OPTIONS,
  APPLICABLE_TO_OPTIONS,
  resolveLevelLabel,
  resolveApplicableToLabel,
  ApplicableTo,
} from 'app/Finance/fee-structure/types/fee-structure.model';

import {
  DataTableComponent, TableHeader, TableColumn, TableAction,
} from 'app/shared/data-table/data-table.component';
import {
  FilterPanelComponent, FilterField, FilterChangeEvent,
} from 'app/shared/Filter/filter-panel.component';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { PaginationComponent }              from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard }    from 'app/shared/stats-cards/stats-cards.component';

// ── Utility: read the is_super_admin claim from the stored JWT ─────────────────
// Adjust to match however your app exposes the current user's role/claims.
import { AuthService } from 'app/core/auth/auth.service'; // ← update path if needed
import { FeeStructureDialogData, FeeStructureDialogComponent } from 'app/dialog-modals/Finance/fee-structure/fee-structure-dialog.component';

@Component({
  selector: 'app-fee-structures',
  standalone: true,
  imports: [
    CommonModule,
    PageHeaderComponent,
    StatsCardsComponent,
    FilterPanelComponent,
    DataTableComponent,
    PaginationComponent,
    MatButtonModule,
    MatIconModule,
  ],
  templateUrl: './fee-structures.component.html',
})
export class FeeStructuresComponent implements OnInit, AfterViewInit, OnDestroy {
  private _destroy$ = new Subject<void>();

  // ── Templates ──────────────────────────────────────────────────────────────
  @ViewChild('feeItemCell')    feeItemCell!:    TemplateRef<any>;
  @ViewChild('yearTermCell')   yearTermCell!:   TemplateRef<any>;
  @ViewChild('levelCell')      levelCell!:      TemplateRef<any>;
  @ViewChild('applicableCell') applicableCell!: TemplateRef<any>;
  @ViewChild('amountCell')     amountCell!:     TemplateRef<any>;
  @ViewChild('discountCell')   discountCell!:   TemplateRef<any>;
  @ViewChild('statusCell')     statusCell!:     TemplateRef<any>;

  cellTemplates: { [k: string]: TemplateRef<any> } = {};

  // ── State ──────────────────────────────────────────────────────────────────
  allData:      FeeStructureDto[] = [];
  filteredData: FeeStructureDto[] = [];
  isLoading     = false;

  /** Resolved once on init from AuthService; passed into every dialog open. */
  isSuperAdmin = false;

  // ── Pagination ─────────────────────────────────────────────────────────────
  currentPage  = 1;
  itemsPerPage = 10;

  // ── Filters ────────────────────────────────────────────────────────────────
  showFilters = false;
  private activeFilters: Record<string, any> = {};

  // ── Page Header ────────────────────────────────────────────────────────────
  readonly breadcrumbs: Breadcrumb[] = [
    { label: 'Finance' },
    { label: 'Fee Structures' },
  ];

  // ── Stats ──────────────────────────────────────────────────────────────────
  statsCards: StatCard[] = [];

  // ── Table ──────────────────────────────────────────────────────────────────
  readonly tableHeader: TableHeader = {
    title:        'Fee Structures',
    subtitle:     'Defined fee amounts per academic context',
    icon:         'account_balance',
    iconGradient: 'bg-gradient-to-br from-emerald-500 via-teal-600 to-cyan-700',
  };

  tableColumns: TableColumn<FeeStructureDto>[] = [
    { id: 'feeItemName',        label: 'Fee Item',    align: 'left',   sortable: true },
    { id: 'academicYearName',   label: 'Year / Term', align: 'left',   sortable: true, hideOnMobile: true },
    { id: 'level',              label: 'Level',       align: 'center', sortable: true, hideOnMobile: true },
    { id: 'applicableTo',       label: 'Applies To',  align: 'center', hideOnMobile: true },
    { id: 'amount',             label: 'Amount',      align: 'right',  sortable: true },
    { id: 'maxDiscountPercent', label: 'Max Disc.',   align: 'center', hideOnMobile: true },
    { id: 'isActive',           label: 'Status',      align: 'center' },
  ];

  tableActions: TableAction<FeeStructureDto>[] = [
    {
      id:      'edit',
      label:   'Edit',
      icon:    'edit',
      color:   'blue',
      handler: row => this.openEdit(row),
    },
    {
      id:      'toggle',
      label:   'Toggle Active',
      icon:    'toggle_on',
      color:   'amber',
      handler: row => this.toggleActive(row),
    },
    {
      id:      'delete',
      label:   'Delete',
      icon:    'delete',
      color:   'red',
      divider: true,
      handler: row => this.confirmDelete(row),
    },
  ];

  readonly emptyState = {
    icon:        'account_balance',
    message:     'No fee structures found',
    description: 'Create a fee structure to define specific amounts per academic year, term, and CBC level.',
    action: { label: 'Add Fee Structure', icon: 'add', handler: () => this.openCreate() },
  };

  filterFields: FilterField[] = [
    { id: 'search', label: 'Search', type: 'text', placeholder: 'Fee item or year…', value: '' },
    {
      id: 'level', label: 'CBC Level', type: 'select', value: 'all',
      options: [
        { label: 'All Levels', value: 'all' },
        ...CBC_LEVEL_OPTIONS.map(o => ({ label: o.label, value: String(o.value) })),
      ],
    },
    {
      id: 'applicableTo', label: 'Applies To', type: 'select', value: 'all',
      options: [
        { label: 'All', value: 'all' },
        ...APPLICABLE_TO_OPTIONS.map(o => ({ label: o.label, value: String(o.value) })),
      ],
    },
    {
      id: 'status', label: 'Status', type: 'select', value: 'all',
      options: [
        { label: 'All',      value: 'all'   },
        { label: 'Active',   value: 'true'  },
        { label: 'Inactive', value: 'false' },
      ],
    },
  ];

  resolveLevel      = resolveLevelLabel;
  resolveApplicable = resolveApplicableToLabel;

  constructor(
    private service: FeeStructureService,
    private dialog:  MatDialog,
    private alert:   AlertService,
    private cdr:     ChangeDetectorRef,
    private auth:    AuthService,   // ← inject your auth service
  ) {}

  ngOnInit(): void {
    this.isSuperAdmin = this.auth.authUser?.isSuperAdmin ?? false;
    this.loadAll();
  }

  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  ngAfterViewInit(): void {
    this.cellTemplates = {
      feeItemName:        this.feeItemCell,
      academicYearName:   this.yearTermCell,
      level:              this.levelCell,
      applicableTo:       this.applicableCell,
      amount:             this.amountCell,
      maxDiscountPercent: this.discountCell,
      isActive:           this.statusCell,
    };
    this.cdr.detectChanges();
  }

  // ── Data ───────────────────────────────────────────────────────────────────
  loadAll(): void {
    this.isLoading = true;
    this.service.getAll().pipe(take(1)).subscribe({
      next: res => {
        this.isLoading = false;
        if (res.success) {
          this.allData = res.data;
          this.applyFilters();
          this.buildStats();
        } else {
          this.alert.error('Error', res.message);
        }
      },
      error: err => {
        this.isLoading = false;
        this.alert.error('Error', err?.error?.message ?? 'Failed to load fee structures.');
      },
    });
  }

  // ── Computed ───────────────────────────────────────────────────────────────
  get paginatedData(): FeeStructureDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  // ── Filters ────────────────────────────────────────────────────────────────
  onFilterChange(event: FilterChangeEvent): void {
    this.activeFilters[event.filterId] = event.value;
    this.currentPage = 1;
    this.applyFilters();
  }

  onClearFilters(): void {
    this.activeFilters = {};
    this.filterFields.forEach(f => { f.value = f.type === 'select' ? 'all' : ''; });
    this.currentPage = 1;
    this.applyFilters();
  }

  private applyFilters(): void {
    let data = [...this.allData];

    const search     = (this.activeFilters['search']      ?? '').toLowerCase().trim();
    const level      = this.activeFilters['level']        ?? 'all';
    const applicable = this.activeFilters['applicableTo'] ?? 'all';
    const status     = this.activeFilters['status']       ?? 'all';

    if (search) {
      data = data.filter(r =>
        r.feeItemName.toLowerCase().includes(search)      ||
        r.academicYearName.toLowerCase().includes(search) ||
        (r.termName ?? '').toLowerCase().includes(search)
      );
    }
    if (level !== 'all') {
      const lvl = parseInt(level, 10);
      data = data.filter(r => r.level === lvl);
    }
    if (applicable !== 'all') {
      const app = parseInt(applicable, 10) as ApplicableTo;
      data = data.filter(r => r.applicableTo === app);
    }
    if (status !== 'all') {
      const active = status === 'true';
      data = data.filter(r => r.isActive === active);
    }

    this.filteredData = data;
  }

  // ── Pagination ─────────────────────────────────────────────────────────────
  onPageChange(page: number): void      { this.currentPage = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

  // ── Stats ──────────────────────────────────────────────────────────────────
  private buildStats(): void {
    const total      = this.allData.length;
    const active     = this.allData.filter(r => r.isActive).length;
    const discounts  = this.allData.filter(r => r.maxDiscountPercent != null && r.maxDiscountPercent > 0).length;
    const annualFees = this.allData.filter(r => r.termId == null).length;

    this.statsCards = [
      { label: 'Total Structures', value: total,      icon: 'account_balance', iconColor: 'indigo' },
      { label: 'Active',           value: active,     icon: 'check_circle',    iconColor: 'green'  },
      { label: 'With Discounts',   value: discounts,  icon: 'discount',        iconColor: 'amber'  },
      { label: 'Annual Fees',      value: annualFees, icon: 'event_repeat',    iconColor: 'pink'   },
    ];
  }

  // ── Sorting ────────────────────────────────────────────────────────────────
  onColumnHeaderClick(event: { column: TableColumn<FeeStructureDto> }): void {
    const col = event.column.id as keyof FeeStructureDto;
    this.filteredData = [...this.filteredData].sort((a, b) => {
      const va = (a as any)[col] ?? '';
      const vb = (b as any)[col] ?? '';
      return String(va).localeCompare(String(vb), undefined, { numeric: true });
    });
    this.currentPage = 1;
  }

  onRowClick(_row: FeeStructureDto): void { /* navigate to detail if needed */ }

  // ── CRUD ───────────────────────────────────────────────────────────────────

  /** Opens the create dialog. SuperAdmin will be prompted to pick a school. */
openCreate(): void {
  const data: FeeStructureDialogData = {
    mode: 'create',
    isSuperAdmin: this.isSuperAdmin,
  };

  this.dialog
    .open(FeeStructureDialogComponent, {
      data,
      width: '680px',
      panelClass: 'no-container-dialog',
      hasBackdrop: true,
    })
    .afterClosed()
    .pipe(take(1))
    .subscribe(result => {
      if (result?.success) this.loadAll();
    });
}

openEdit(item: FeeStructureDto): void {
  const data: FeeStructureDialogData = {
    mode: 'edit',
    item,
    isSuperAdmin: this.isSuperAdmin,
  };

  this.dialog
    .open(FeeStructureDialogComponent, {
      data,
      width: '680px',
      panelClass: 'no-container-dialog',
      hasBackdrop: true,
    })
    .afterClosed()
    .pipe(take(1))
    .subscribe(result => {
      if (result?.success) this.loadAll();
    });
}

  toggleActive(item: FeeStructureDto): void {
    this.service.toggleActive(item.id).pipe(take(1)).subscribe({
      next: res => {
        if (res.success) {
          this.alert.success('Updated', `Fee structure is now ${res.data.isActive ? 'active' : 'inactive'}.`);
          this.loadAll();
        } else {
          this.alert.error('Error', res.message);
        }
      },
      error: err => this.alert.error('Error', err?.error?.message),
    });
  }

  confirmDelete(item: FeeStructureDto): void {
    this.alert.confirm({
      title:       'Delete Fee Structure',
      message:     `Delete the fee structure for "${item.feeItemName}" (${item.academicYearName})? This cannot be undone.`,
      confirmText: 'Delete',
      onConfirm:   () => this.executeDelete(item),
    });
  }

  private executeDelete(item: FeeStructureDto): void {
    this.service.delete(item.id).pipe(takeUntil(this._destroy$)).subscribe({
      next: res => {
        if (res.success) {
          this.alert.success('Deleted', 'Fee structure has been deleted.');
          if (this.paginatedData.length === 1 && this.currentPage > 1) this.currentPage--;
          this.loadAll();
        } else {
          this.alert.error('Error', res.message);
        }
      },
      error: err => this.alert.error('Error', err?.error?.message ?? 'Failed to delete.'),
    });
  }
}