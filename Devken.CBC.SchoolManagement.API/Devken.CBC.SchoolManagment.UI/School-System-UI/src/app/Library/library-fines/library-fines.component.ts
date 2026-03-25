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
import { Subject, forkJoin, of } from 'rxjs';
import { catchError, takeUntil, finalize } from 'rxjs/operators';

import { AuthService } from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { SchoolDto } from 'app/Tenant/types/school';

import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';
import {
  DataTableComponent, TableColumn, TableAction, TableHeader, TableEmptyState,
} from 'app/shared/data-table/data-table.component';
import { BaseListComponent } from 'app/shared/Lists/BaseListComponent';
import { LibraryFineDto } from './Types/library-fine.types';
import { LibraryFineService } from 'app/core/DevKenService/Library/library-fine.service';
import { CreateLibraryFineDialogComponent } from 'app/dialog-modals/Library/library-fine-dialog/create-library-fine-dialog.component';
import { ActivatedRoute } from '@angular/router';


@Component({
  selector: 'app-library-fines',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatButtonModule, MatDialogModule, MatProgressSpinnerModule,
    PageHeaderComponent, FilterPanelComponent, PaginationComponent,
    StatsCardsComponent, DataTableComponent,
  ],
  templateUrl: './library-fines.component.html',
})
export class LibraryFinesComponent
  extends BaseListComponent<LibraryFineDto>
  implements OnInit, OnDestroy, AfterViewInit {

  private _destroy$    = new Subject<void>();
  private _alertService!: AlertService;
  private _fineService!:  LibraryFineService;
  private _schoolService!: SchoolService;

  @ViewChild('amountCell', { static: true }) amountCell!: TemplateRef<any>;
  @ViewChild('memberCell', { static: true }) memberCell!: TemplateRef<any>;
  @ViewChild('bookCell',   { static: true }) bookCell!:   TemplateRef<any>;
  @ViewChild('issuedCell', { static: true }) issuedCell!: TemplateRef<any>;
  @ViewChild('statusCell', { static: true }) statusCell!: TemplateRef<any>;
  @ViewChild('schoolCell', { static: true }) schoolCell!: TemplateRef<any>;

  cellTemplates!: Record<string, TemplateRef<any>>;

  ngAfterViewInit(): void {
    this.cellTemplates = {
      amount: this.amountCell,
      member: this.memberCell,
      book:   this.bookCell,
      issued: this.issuedCell,
      status: this.statusCell,
      school: this.schoolCell,
    };
  }

  // ── State ─────────────────────────────────────────────────────────────────
  schools:        SchoolDto[] = [];
  isDataLoading   = true;
  showFilterPanel = false;
  currentPage     = 1;
  itemsPerPage    = 10;

  filterValues = {
    search: '', status: 'all', schoolId: 'all',
  };

  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Library',   url: '/library'   },
    { label: 'Fines' },
  ];

  get isSuperAdmin(): boolean { return this._injectedAuth.authUser?.isSuperAdmin ?? false; }

  // ── Table config ──────────────────────────────────────────────────────────

  get tableColumns(): TableColumn<LibraryFineDto>[] {
    const cols: TableColumn<LibraryFineDto>[] = [
      { id: 'amount', label: 'Amount',  align: 'left', sortable: true },
      { id: 'member', label: 'Member',  align: 'left', hideOnMobile: true },
      { id: 'book',   label: 'Book',    align: 'left', hideOnMobile: true },
      { id: 'issued', label: 'Issued',  align: 'left', hideOnMobile: true },
    ];
    if (this.isSuperAdmin) {
      cols.push({ id: 'school', label: 'School', align: 'left', hideOnMobile: true });
    }
    cols.push({ id: 'status', label: 'Status', align: 'center' });
    return cols;
  }

  tableActions: TableAction<LibraryFineDto>[] = [
    {
      id: 'pay', label: 'Mark as Paid', icon: 'payment', color: 'green',
      handler: r => this._payFine(r),
      visible: r => !r.isPaid && !r.isWaived,
      divider: true,
    },
    {
      id: 'waive', label: 'Waive Fine', icon: 'do_not_disturb_on', color: 'amber',
      handler: r => this._waiveFine(r),
      visible: r => !r.isPaid && !r.isWaived,
      divider: true,
    },
    {
      id: 'delete', label: 'Delete', icon: 'delete', color: 'red',
      handler: r => this._confirmDelete(r),
    },
  ];

  tableHeader: TableHeader = {
    title:        'Library Fines',
    subtitle:     '',
    icon:         'receipt_long',
    iconGradient: 'bg-gradient-to-br from-rose-500 via-red-600 to-orange-600',
  };

  tableEmptyState: TableEmptyState = {
    icon:        'receipt_long',
    message:     'No fines found',
    description: 'No fines have been issued yet',
    action:      { label: 'Issue Fine', icon: 'add', handler: () => this._openCreate() },
  };

  filterFields: FilterField[] = [];

  // ── Stats ─────────────────────────────────────────────────────────────────

  get statsCards(): StatCard[] {
    const data = this.dataSource.data;
    const unpaid  = data.filter(f => !f.isPaid && !f.isWaived);
    const paid    = data.filter(f => f.isPaid);
    const waived  = data.filter(f => f.isWaived);
    const totalUnpaid = unpaid.reduce((s, f) => s + f.amount, 0);
    const base: StatCard[] = [
      { label: 'Total Fines',   value: data.length,                          icon: 'receipt_long', iconColor: 'indigo' },
      { label: 'Unpaid',        value: `${unpaid.length} (${this.formatCurrency(totalUnpaid)})`, icon: 'pending',     iconColor: 'red'    },
      { label: 'Paid',          value: paid.length,                           icon: 'check_circle', iconColor: 'green'  },
      { label: 'Waived',        value: waived.length,                         icon: 'do_not_disturb_on', iconColor: 'amber' },
    ];
    if (this.isSuperAdmin) {
      base.push({ label: 'Schools', value: new Set(data.map(f => f.schoolId)).size, icon: 'school', iconColor: 'violet' });
    }
    return base;
  }

  // ── Filtered & Paginated ──────────────────────────────────────────────────

  get filteredData(): LibraryFineDto[] {
    const q = this.filterValues.search.toLowerCase();
    return this.dataSource.data.filter(f =>
      (!q || f.memberName?.toLowerCase().includes(q)
          || f.bookTitle?.toLowerCase().includes(q)
          || f.reason?.toLowerCase().includes(q)) &&
      (this.filterValues.status   === 'all'
        || (this.filterValues.status === 'unpaid'  && !f.isPaid && !f.isWaived)
        || (this.filterValues.status === 'paid'    && f.isPaid)
        || (this.filterValues.status === 'waived'  && f.isWaived)) &&
      (this.filterValues.schoolId === 'all' || f.schoolId === this.filterValues.schoolId)
    );
  }

  get paginatedData(): LibraryFineDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  // ── Constructor ───────────────────────────────────────────────────────────

  constructor(
    dialog:        MatDialog,
    snackBar:      MatSnackBar,
    alertService:  AlertService,
    fineService:   LibraryFineService,
    private readonly _injectedAuth:  AuthService,
    schoolService: SchoolService,
    private _route: ActivatedRoute,
  ) {
    super(fineService, dialog, snackBar);
    this._alertService  = alertService;
    this._fineService   = fineService;
    this._schoolService = schoolService;
  }

  ngOnInit(): void {
    // Listen for incoming search parameters from the Borrow page
  this._route.queryParams.pipe(takeUntil(this._destroy$)).subscribe(params => {
    if (params['search']) {
      this.filterValues.search = params['search'];
    }
    if (params['schoolId']) {
      this.filterValues.schoolId = params['schoolId'];
    }
    
    // Now that filterValues is updated, load the UI and Data
        this._loadMeta();
    });
  }
  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  // ── Data ──────────────────────────────────────────────────────────────────

  protected override loadAll(): void {
    this.isLoading = true;
    const schoolId = this.filterValues.schoolId !== 'all' ? this.filterValues.schoolId : undefined;
    this._fineService.getAll(schoolId).pipe(takeUntil(this._destroy$)).subscribe({
      next: res => {
        if (res.success) {
          this.dataSource.data = res.data;
          this.tableHeader.subtitle = `${this.filteredData.length} fines found`;
        } else {
          this._alertService.error(res.message || 'Failed to load fines');
        }
        this.isLoading = false;
      },
      error: err => {
        this._alertService.error(err.error?.message || 'Failed to load fines');
        this.isLoading = false;
      }
    });
  }

  private _loadMeta(): void {
    this.isDataLoading = true;
    const requests: any = {};

    if (this.isSuperAdmin) {
      requests.schools = this._schoolService.getAll().pipe(catchError(() => of({ success: false, data: [] })));
    }

    if (Object.keys(requests).length === 0) {
      this.isDataLoading = false;
      this._initFilters();
      this.loadAll();
      return;
    }

    forkJoin(requests).pipe(
      takeUntil(this._destroy$),
      finalize(() => { this.isDataLoading = false; })
    ).subscribe({
      next: (res: any) => {
        if (res.schools) this.schools = res.schools?.data || [];
        this._initFilters();
        this.loadAll();
      },
      error: () => { this._initFilters(); this.loadAll(); }
    });
  }

  // ── Filters ───────────────────────────────────────────────────────────────

  private _initFilters(): void {
    this.filterFields = [
      { id: 'search', label: 'Search', type: 'text', placeholder: 'Member, book or reason...', 
        // Use the value already in state (potentially from the URL)
         value: this.filterValues.search
      },
    ];

    if (this.isSuperAdmin) {
      this.filterFields.push({
        id: 'schoolId', label: 'School', type: 'select',
        value: this.filterValues.schoolId, // Use the state value 
        // value: 'all',
        options: [
          { label: 'All Schools', value: 'all' },
          ...this.schools.map(s => ({ label: s.name, value: s.id })),
        ],
      });
    }

    this.filterFields.push({
      id: 'status', label: 'Status', type: 'select', value: 'all',
      options: [
        { label: 'All Fines', value: 'all'    },
        { label: 'Unpaid',    value: 'unpaid' },
        { label: 'Paid',      value: 'paid'   },
        { label: 'Waived',    value: 'waived' },
      ],
    });
  }

  toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

  onFilterChange(event: FilterChangeEvent): void {
    (this.filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} fines found`;
    if (event.filterId === 'schoolId' && this.isSuperAdmin) this.loadAll();
  }

  onClearFilters(): void {
    this.filterValues = { search: '', status: 'all', schoolId: 'all' };
    this.filterFields.forEach(f => { f.value = (this.filterValues as any)[f.id]; });
    this.currentPage = 1;
    this.loadAll();
  }

  onPageChange(page: number):      void { this.currentPage = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

  // ── CRUD & Actions ────────────────────────────────────────────────────────

  openCreate(): void { this._openCreate(); }

  private _openCreate(): void {
    this.openDialog(CreateLibraryFineDialogComponent, {
      panelClass: ['library-fine-dialog', 'no-padding-dialog'],
      width: '600px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: true, autoFocus: 'input',
      data: { mode: 'create' },
    });
  }

  private _payFine(fine: LibraryFineDto): void {
    this._alertService.confirm({
      title:       'Mark Fine as Paid',
      message:     `Mark fine of ${this.formatCurrency(fine.amount)} as paid?`,
      confirmText: 'Mark Paid',
      onConfirm: () => {
        this._fineService.payFine({ fineId: fine.id }).pipe(takeUntil(this._destroy$)).subscribe({
          next: res => {
            if (res.success) {
              this._alertService.success('Fine marked as paid');
              this.loadAll();
            } else {
              this._alertService.error(res.message || 'Failed');
            }
          },
          error: err => this._alertService.error(err.error?.message || 'Failed'),
        });
      },
    });
  }

  private _waiveFine(fine: LibraryFineDto): void {
    this.openDialog(CreateLibraryFineDialogComponent, {
      panelClass: ['library-fine-dialog', 'no-padding-dialog'],
      width: '500px', maxWidth: '95vw', maxHeight: '80vh',
      disableClose: true,
      data: { mode: 'waive', fine },
    });
  }

  private _confirmDelete(fine: LibraryFineDto): void {
    this._alertService.confirm({
      title:       'Delete Fine',
      message:     `Delete fine of ${this.formatCurrency(fine.amount)}? This cannot be undone.`,
      confirmText: 'Delete',
      onConfirm: () => {
        this.isLoading = true;
        this._fineService.delete(fine.id).pipe(takeUntil(this._destroy$)).subscribe({
          next: res => {
            if (res.success) {
              this._alertService.success('Fine deleted');
              if (this.paginatedData.length === 1 && this.currentPage > 1) this.currentPage--;
              this.loadAll();
            } else {
              this._alertService.error(res.message || 'Failed to delete');
            }
            this.isLoading = false;
          },
          error: err => {
            this._alertService.error(err.error?.message || 'Failed to delete');
            this.isLoading = false;
          }
        });
      },
    });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  getFineStatusColor(fine: LibraryFineDto): string {
    if (fine.isWaived) return 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400';
    if (fine.isPaid)   return 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400';
    return 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400';
  }

  getFineStatusIcon(fine: LibraryFineDto): string {
    if (fine.isWaived) return 'do_not_disturb_on';
    if (fine.isPaid)   return 'check_circle';
    return 'pending';
  }

  getFineStatusLabel(fine: LibraryFineDto): string {
    if (fine.isWaived) return 'Waived';
    if (fine.isPaid)   return 'Paid';
    return 'Unpaid';
  }

  formatDate(dateStr?: string): string {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'KES', minimumFractionDigits: 2 }).format(amount || 0);
  }
}