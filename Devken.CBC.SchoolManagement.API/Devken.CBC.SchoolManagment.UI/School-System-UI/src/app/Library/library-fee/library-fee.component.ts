// app/Library/library-fee/library-fee.component.ts
import {
  Component, OnInit, OnDestroy, AfterViewInit, ViewChild, TemplateRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subject, takeUntil } from 'rxjs';

import { AuthService } from 'app/core/auth/auth.service';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { LibraryFeeService } from 'app/core/DevKenService/Library/library-fee.service';
import { SchoolDto } from 'app/Tenant/types/school';

import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';
import { DataTableComponent, TableColumn, TableAction, TableHeader, TableEmptyState } from 'app/shared/data-table/data-table.component';
import { BaseListComponent } from 'app/shared/Lists/BaseListComponent';

import {
  LibraryFeeDto, LibraryFeeStatus, LibraryFeeType,
} from './Types/library-fee.types';
import {
  CreateEditLibraryFeeDialogComponent,
} from 'app/dialog-modals/Library/library-fee/create-edit-library-fee-dialog.component';
import {
  RecordPaymentDialogComponent,
} from 'app/dialog-modals/Library/library-fee/record-payment-dialog.component';
import {
  WaiveFeeDialogComponent,
} from 'app/dialog-modals/Library/library-fee/waive-fee-dialog.component';

@Component({
  selector: 'app-library-fees',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatButtonModule, MatDialogModule, MatProgressSpinnerModule,
    PageHeaderComponent, FilterPanelComponent, PaginationComponent,
    StatsCardsComponent, DataTableComponent,
  ],
  templateUrl: './library-fee.component.html',
})
export class LibraryFeeComponent
  extends BaseListComponent<LibraryFeeDto>
  implements OnInit, OnDestroy, AfterViewInit {

  private readonly _destroy$ = new Subject<void>();
  private _alertService!:  AlertService;
  private _feeService!:    LibraryFeeService;
  private _schoolService!: SchoolService;
  

  @ViewChild('memberCell',      { static: true }) memberCell!:      TemplateRef<any>;
  @ViewChild('feeTypeCell',     { static: true }) feeTypeCell!:     TemplateRef<any>;
  @ViewChild('amountCell',      { static: true }) amountCell!:      TemplateRef<any>;
  @ViewChild('statusCell',      { static: true }) statusCell!:      TemplateRef<any>;
  @ViewChild('feeDateCell',     { static: true }) feeDateCell!:     TemplateRef<any>;
  @ViewChild('schoolCell',      { static: true }) schoolCell!:      TemplateRef<any>;

  cellTemplates!: Record<string, TemplateRef<any>>;

  ngAfterViewInit(): void {
    this.cellTemplates = {
      member:    this.memberCell,
      feeType:   this.feeTypeCell,
      amount:    this.amountCell,
      feeStatus: this.statusCell,
      feeDate:   this.feeDateCell,
      school:    this.schoolCell,
    };
  }

  // ── State ──────────────────────────────────────────────────────────────────
  schools:         SchoolDto[] = [];
  isDataLoading    = false;
  showFilterPanel  = false;
  currentPage      = 1;
  itemsPerPage     = 10;

  filterValues = {
    search:    '',
    feeStatus: 'all',
    feeType:   'all',
    schoolId:  'all',
  };

  readonly feeStatusOptions = [
    { label: 'All Statuses',    value: 'all'                        },
    { label: 'Unpaid',          value: LibraryFeeStatus.Unpaid      },
    { label: 'Partially Paid',  value: LibraryFeeStatus.PartiallyPaid },
    { label: 'Paid',            value: LibraryFeeStatus.Paid        },
    { label: 'Waived',          value: LibraryFeeStatus.Waived      },
  ];

  readonly feeTypeOptions = [
    { label: 'All Types',       value: 'all'                          },
    { label: 'Late Fine',       value: LibraryFeeType.LateFine        },
    { label: 'Damage Fee',      value: LibraryFeeType.DamageFee       },
    { label: 'Lost Book Fee',   value: LibraryFeeType.LostBookFee     },
    { label: 'Membership Fee',  value: LibraryFeeType.MembershipFee   },
    { label: 'Processing Fee',  value: LibraryFeeType.ProcessingFee   },
    { label: 'Other',           value: LibraryFeeType.Other           },
  ];

  // ── Breadcrumbs ────────────────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Library',   url: '/library'   },
    { label: 'Fees' },
  ];

  // ── Table config ───────────────────────────────────────────────────────────
  get isSuperAdmin(): boolean { return this._injectedAuth.authUser?.isSuperAdmin ?? false; }

  get tableColumns(): TableColumn<LibraryFeeDto>[] {
    const cols: TableColumn<LibraryFeeDto>[] = [
      { id: 'member',    label: 'Member',    align: 'left'                          },
      { id: 'feeType',   label: 'Fee Type',  align: 'left',  hideOnMobile: true     },
      { id: 'amount',    label: 'Amount',    align: 'right'                         },
      { id: 'feeStatus', label: 'Status',    align: 'center'                        },
      { id: 'feeDate',   label: 'Date',      align: 'left',  hideOnTablet: true     },
    ];
    if (this.isSuperAdmin) {
      cols.splice(1, 0, { id: 'school', label: 'School', align: 'left', hideOnMobile: true });
    }
    return cols;
  }

  tableActions: TableAction<LibraryFeeDto>[] = [
    {
      id: 'pay', label: 'Record Payment', icon: 'payments', color: 'green',
      visible: row => row.feeStatus !== LibraryFeeStatus.Paid &&
                      row.feeStatus !== LibraryFeeStatus.Waived,
      handler: row => this._openPayDialog(row),
    },
    {
      id: 'waive', label: 'Waive Fee', icon: 'do_not_disturb_on', color: 'violet',
      visible: row => row.feeStatus !== LibraryFeeStatus.Paid &&
                      row.feeStatus !== LibraryFeeStatus.Waived,
      handler: row => this._openWaiveDialog(row),
      divider: true,
    },
    {
      id: 'edit', label: 'Edit', icon: 'edit', color: 'blue',
      visible: row => row.feeStatus === LibraryFeeStatus.Unpaid,
      handler: row => this._openEdit(row),
    },
    {
      id: 'delete', label: 'Delete', icon: 'delete', color: 'red',
      visible: row => row.feeStatus !== LibraryFeeStatus.Paid,
      handler: row => this._confirmDelete(row),
    },
  ];

  tableHeader: TableHeader = {
    title: 'Library Fees',
    subtitle: '',
    icon: 'receipt_long',
    iconGradient: 'bg-gradient-to-br from-amber-500 via-orange-500 to-red-500',
  };

  tableEmptyState: TableEmptyState = {
    icon: 'receipt_long',
    message: 'No fees found',
    description: 'Try adjusting your filters or add a new fee',
    action: { label: 'Add First Fee', icon: 'add', handler: () => this._openCreate() },
  };

  filterFields: FilterField[] = [];

  // ── Stats ──────────────────────────────────────────────────────────────────
  get statsCards(): StatCard[] {
    const data = this.dataSource.data;
    const unpaid = data.filter(f =>
      f.feeStatus === LibraryFeeStatus.Unpaid ||
      f.feeStatus === LibraryFeeStatus.PartiallyPaid
    );
    return [
      {
        label: 'Total Fees',
        value: data.length,
        icon: 'receipt_long',
        iconColor: 'amber',
      },
      {
        label: 'Outstanding',
        value: `KES ${unpaid.reduce((s, f) => s + f.balance, 0).toLocaleString('en-KE', { minimumFractionDigits: 2 })}`,
        icon: 'money_off',
        iconColor: 'red',
      },
      {
        label: 'Collected',
        value: `KES ${data.reduce((s, f) => s + f.amountPaid, 0).toLocaleString('en-KE', { minimumFractionDigits: 2 })}`,
        icon: 'paid',
        iconColor: 'green',
      },
      {
        label: 'Unpaid / Partial',
        value: unpaid.length,
        icon: 'pending',
        iconColor: 'orange',
      },
    ];
  }

  // ── Filtered & paginated ───────────────────────────────────────────────────
  get filteredData(): LibraryFeeDto[] {
    const q = this.filterValues.search.toLowerCase();
    return this.dataSource.data.filter(f =>
      (!q ||
        f.memberNumber?.toLowerCase().includes(q) ||
        f.userFullName?.toLowerCase().includes(q) ||
        f.description?.toLowerCase().includes(q)
      ) &&
      (this.filterValues.feeStatus === 'all' || f.feeStatus === this.filterValues.feeStatus) &&
      (this.filterValues.feeType   === 'all' || f.feeType   === this.filterValues.feeType  ) &&
      (this.filterValues.schoolId  === 'all' || f.schoolId  === this.filterValues.schoolId )
    );
  }

  get paginatedData(): LibraryFeeDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  constructor(
    dialog:      MatDialog,
    snackBar:    MatSnackBar,
    feeService:  LibraryFeeService,
    private readonly _injectedAuth:  AuthService,
    alertService:  AlertService,
    schoolService: SchoolService,
  ) {
    super(feeService as any, dialog, snackBar);
    this._alertService  = alertService;
    this._feeService    = feeService;
    this._schoolService = schoolService;
  }

  // ── Lifecycle ──────────────────────────────────────────────────────────────
  ngOnInit(): void { this._initFilters(); this.loadAll(); }
  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  // ── Data loading ───────────────────────────────────────────────────────────
  protected override loadAll(): void {
    this.isLoading = true;
    const schoolId = this.isSuperAdmin && this.filterValues.schoolId !== 'all'
      ? this.filterValues.schoolId
      : undefined;

    this._feeService.getAll(schoolId)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: res => {
          if (res.success) {
            this.dataSource.data = res.data;
            this.tableHeader.subtitle = `${this.filteredData.length} fees found`;
          } else {
            this._alertService.error(res.message || 'Failed to load fees');
          }
          this.isLoading = false;
        },
        error: err => {
          this._alertService.error(err?.error?.message || 'Failed to load fees');
          this.isLoading = false;
        },
      });

    if (this.isSuperAdmin && this.schools.length === 0) {
      this._schoolService.getAll()
        .pipe(takeUntil(this._destroy$))
        .subscribe(res => {
          if (res.success) {
            this.schools = res.data;
            this._initFilters();
          }
        });
    }
  }

  // ── Filters ────────────────────────────────────────────────────────────────
  private _initFilters(): void {
    this.filterFields = [
      {
        id: 'search', label: 'Search', type: 'text',
        placeholder: 'Member number or name...', value: '',
      },
      {
        id: 'feeStatus', label: 'Status', type: 'select', value: 'all',
        options: this.feeStatusOptions,
      },
      {
        id: 'feeType', label: 'Fee Type', type: 'select', value: 'all',
        options: this.feeTypeOptions,
      },
    ];

    if (this.isSuperAdmin) {
      this.filterFields.splice(1, 0, {
        id: 'schoolId', label: 'School', type: 'select', value: 'all',
        options: [
          { label: 'All Schools', value: 'all' },
          ...this.schools.map(s => ({ label: s.name, value: s.id })),
        ],
      });
    }
  }

  toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

  onFilterChange(event: FilterChangeEvent): void {
    (this.filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} fees found`;
    if (event.filterId === 'schoolId') this.loadAll();
  }

  onClearFilters(): void {
    this.filterValues = { search: '', feeStatus: 'all', feeType: 'all', schoolId: 'all' };
    this.filterFields.forEach(f => { f.value = (this.filterValues as any)[f.id] ?? 'all'; });
    this.currentPage = 1;
    this.loadAll();
  }

  onPageChange(page: number): void      { this.currentPage  = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

  // ── CRUD ───────────────────────────────────────────────────────────────────
  private _openCreate(): void {
    this.openDialog(CreateEditLibraryFeeDialogComponent, {
      width: '700px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: true, autoFocus: 'input',
      data: { mode: 'create' },
    });
  }

  private _openEdit(fee: LibraryFeeDto): void {
    this.openDialog(CreateEditLibraryFeeDialogComponent, {
      width: '700px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: true, autoFocus: 'input',
      data: { mode: 'edit', fee },
    });
  }

  private _openPayDialog(fee: LibraryFeeDto): void {
    this.openDialog(RecordPaymentDialogComponent, {
      width: '480px', maxWidth: '95vw',
      disableClose: true,
      data: { fee },
    });
  }

  private _openWaiveDialog(fee: LibraryFeeDto): void {
    this.openDialog(WaiveFeeDialogComponent, {
      width: '480px', maxWidth: '95vw',
      disableClose: true,
      data: { fee },
    });
  }

  openCreate(): void { this._openCreate(); }

  private _confirmDelete(fee: LibraryFeeDto): void {
    this._alertService.confirm({
      title:       'Delete Fee',
      message:     `Delete this ${fee.feeTypeDisplay} of KES ${fee.amount} for member "${fee.memberNumber}"? This cannot be undone.`,
      confirmText: 'Delete',
      onConfirm:   () => this._doDelete(fee),
    });
  }

  private _doDelete(fee: LibraryFeeDto): void {
    this.isLoading = true;
    this._feeService.delete(fee.id)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: res => {
          if (res.success) {
            this._alertService.success('Fee deleted successfully');
            if (this.paginatedData.length === 1 && this.currentPage > 1) this.currentPage--;
            this.loadAll();
          } else {
            this._alertService.error(res.message || 'Failed to delete fee');
          }
          this.isLoading = false;
        },
        error: err => {
          this._alertService.error(err?.error?.message || 'Failed to delete fee');
          this.isLoading = false;
        },
      });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  getStatusClasses(status: LibraryFeeStatus): string {
    const map: Record<LibraryFeeStatus, string> = {
      [LibraryFeeStatus.Unpaid]:        'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400',
      [LibraryFeeStatus.PartiallyPaid]: 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400',
      [LibraryFeeStatus.Paid]:          'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400',
      [LibraryFeeStatus.Waived]:        'bg-violet-100 dark:bg-violet-900/30 text-violet-700 dark:text-violet-400',
    };
    return map[status] ?? 'bg-gray-100 text-gray-600';
  }

  getStatusIcon(status: LibraryFeeStatus): string {
    const map: Record<LibraryFeeStatus, string> = {
      [LibraryFeeStatus.Unpaid]:        'cancel',
      [LibraryFeeStatus.PartiallyPaid]: 'hourglass_bottom',
      [LibraryFeeStatus.Paid]:          'check_circle',
      [LibraryFeeStatus.Waived]:        'do_not_disturb_on',
    };
    return map[status] ?? 'help';
  }

  getFeeTypeIcon(type: LibraryFeeType): string {
    const map: Record<LibraryFeeType, string> = {
      [LibraryFeeType.LateFine]:      'schedule',
      [LibraryFeeType.DamageFee]:     'broken_image',
      [LibraryFeeType.LostBookFee]:   'search_off',
      [LibraryFeeType.MembershipFee]: 'card_membership',
      [LibraryFeeType.ProcessingFee]: 'receipt_long',
      [LibraryFeeType.Other]:         'more_horiz',
    };
    return map[type] ?? 'payments';
  }
}