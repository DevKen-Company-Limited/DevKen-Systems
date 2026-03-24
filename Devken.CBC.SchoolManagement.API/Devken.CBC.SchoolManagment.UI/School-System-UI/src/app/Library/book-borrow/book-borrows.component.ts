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
import { BookBorrowDto } from './Types/book-borrow.types';
import { BookBorrowService } from 'app/core/DevKenService/Library/book-borrow.service';
import { CreateBookBorrowDialogComponent } from 'app/dialog-modals/Library/book-borrow-dialog/create-book-borrow-dialog.component';

@Component({
  selector: 'app-book-borrows',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatButtonModule, MatDialogModule, MatProgressSpinnerModule,
    PageHeaderComponent, FilterPanelComponent, PaginationComponent,
    StatsCardsComponent, DataTableComponent,
  ],
  templateUrl: './book-borrows.component.html',
})
export class BookBorrowsComponent
  extends BaseListComponent<BookBorrowDto>
  implements OnInit, OnDestroy, AfterViewInit {

  private _destroy$ = new Subject<void>();
  private _alertService!: AlertService;
  private _borrowService!: BookBorrowService;
  private _schoolService!: SchoolService;

  @ViewChild('memberCell',  { static: true }) memberCell!:  TemplateRef<any>;
  @ViewChild('datesCell',   { static: true }) datesCell!:   TemplateRef<any>;
  @ViewChild('itemsCell',   { static: true }) itemsCell!:   TemplateRef<any>;
  @ViewChild('statusCell',  { static: true }) statusCell!:  TemplateRef<any>;
  @ViewChild('finesCell',   { static: true }) finesCell!:   TemplateRef<any>;
  @ViewChild('schoolCell',  { static: true }) schoolCell!:  TemplateRef<any>;

  cellTemplates!: Record<string, TemplateRef<any>>;

  ngAfterViewInit(): void {
    this.cellTemplates = {
      member:  this.memberCell,
      dates:   this.datesCell,
      items:   this.itemsCell,
      status:  this.statusCell,
      fines:   this.finesCell,
      school:  this.schoolCell,
    };
  }

  // ── State ─────────────────────────────────────────────────────────────────
  schools:        SchoolDto[] = [];
  isDataLoading   = true;
  showFilterPanel = false;
  currentPage     = 1;
  itemsPerPage    = 10;

  filterValues = {
    search: '', status: 'all', view: 'all', schoolId: 'all',
  };

  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Library',   url: '/library'   },
    { label: 'Borrows' },
  ];

  get isSuperAdmin(): boolean { return this._injectedAuth.authUser?.isSuperAdmin ?? false; }

  // ── Table config ──────────────────────────────────────────────────────────

  get tableColumns(): TableColumn<BookBorrowDto>[] {
    const cols: TableColumn<BookBorrowDto>[] = [
      { id: 'member', label: 'Member',  align: 'left', sortable: true },
      { id: 'dates',  label: 'Dates',   align: 'left', hideOnMobile: true },
      { id: 'items',  label: 'Items',   align: 'center', hideOnMobile: true },
      { id: 'fines',  label: 'Fines',   align: 'right', hideOnMobile: true },
    ];
    if (this.isSuperAdmin) {
      cols.push({ id: 'school', label: 'School', align: 'left', hideOnMobile: true });
    }
    cols.push({ id: 'status', label: 'Status', align: 'center' });
    return cols;
  }

  tableActions: TableAction<BookBorrowDto>[] = [
    {
      id: 'view', label: 'View Details', icon: 'visibility', color: 'indigo',
      handler: r => this._openView(r),
      divider: true,
    },
    {
      id: 'edit', label: 'Edit Due Date', icon: 'edit', color: 'blue',
      handler: r => this._openEdit(r),
      visible: r => r.borrowStatus === 'Borrowed' || r.borrowStatus === 'Overdue',
    },
    {
      id: 'returnAll', label: 'Return All', icon: 'assignment_return', color: 'green',
      handler: r => this._returnAll(r),
      visible: r => r.unreturnedItems > 0,
      divider: true,
    },
    {
      id: 'processOverdue', label: 'Mark Overdue', icon: 'schedule', color: 'amber',
      handler: r => this._markOverdue(r),
      visible: r => r.borrowStatus === 'Borrowed' && r.isOverdue,
    },
    {
      id: 'delete', label: 'Delete', icon: 'delete', color: 'red',
      handler: r => this._confirmDelete(r),
    },
  ];

  tableHeader: TableHeader = {
    title:        'Book Borrows',
    subtitle:     '',
    icon:         'assignment_return',
    iconGradient: 'bg-gradient-to-br from-indigo-500 via-violet-600 to-purple-700',
  };

  tableEmptyState: TableEmptyState = {
    icon:        'assignment_return',
    message:     'No borrow transactions found',
    description: 'Try adjusting your filters or create a new borrow',
    action:      { label: 'New Borrow', icon: 'add', handler: () => this._openCreate() },
  };

  filterFields: FilterField[] = [];

  // ── Stats ─────────────────────────────────────────────────────────────────

  get statsCards(): StatCard[] {
    const data = this.dataSource.data;
    const base: StatCard[] = [
      { label: 'Total Borrows',   value: data.length,                                           icon: 'library_books',     iconColor: 'indigo' },
      { label: 'Active',          value: data.filter(b => b.borrowStatus === 'Borrowed').length, icon: 'assignment_return', iconColor: 'blue'   },
      { label: 'Overdue',         value: data.filter(b => b.isOverdue).length,                   icon: 'schedule',          iconColor: 'red'    },
      { label: 'Returned',        value: data.filter(b => b.borrowStatus === 'Returned').length,  icon: 'check_circle',      iconColor: 'green'  },
    ];
    if (this.isSuperAdmin) {
      base.push({ label: 'Schools', value: new Set(data.map(b => b.schoolId)).size, icon: 'school', iconColor: 'violet' });
    }
    return base;
  }

  // ── Filtered & Paginated ──────────────────────────────────────────────────

  get filteredData(): BookBorrowDto[] {
    const q = this.filterValues.search.toLowerCase();
    return this.dataSource.data.filter(b =>
      (!q || b.memberName?.toLowerCase().includes(q)
          || b.memberNumber?.toLowerCase().includes(q)) &&
      (this.filterValues.status   === 'all' || b.borrowStatus?.toLowerCase() === this.filterValues.status.toLowerCase()) &&
      (this.filterValues.view     === 'all'
        || (this.filterValues.view === 'overdue'  && b.isOverdue)
        || (this.filterValues.view === 'active'   && b.borrowStatus === 'Borrowed' && !b.isOverdue)
        || (this.filterValues.view === 'returned' && b.borrowStatus === 'Returned')) &&
      (this.filterValues.schoolId === 'all' || b.schoolId === this.filterValues.schoolId)
    );
  }

  get paginatedData(): BookBorrowDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  // ── Constructor ───────────────────────────────────────────────────────────

  constructor(
    dialog:        MatDialog,
    snackBar:      MatSnackBar,
    alertService:  AlertService,
    borrowService: BookBorrowService,
    private readonly _injectedAuth:  AuthService,
    schoolService: SchoolService,
  ) {
    super(borrowService, dialog, snackBar);
    this._alertService   = alertService;
    this._borrowService  = borrowService;
    this._schoolService  = schoolService;
  }

  ngOnInit():    void { this._loadMeta(); }
  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  // ── Data ──────────────────────────────────────────────────────────────────

  protected override loadAll(): void {
    this.isLoading = true;
    const schoolId = this.filterValues.schoolId !== 'all' ? this.filterValues.schoolId : undefined;
    this._borrowService.getAll(schoolId).pipe(takeUntil(this._destroy$)).subscribe({
      next: res => {
        if (res.success) {
          this.dataSource.data = res.data;
          this.tableHeader.subtitle = `${this.filteredData.length} transactions found`;
        } else {
          this._alertService.error(res.message || 'Failed to load borrows');
        }
        this.isLoading = false;
      },
      error: err => {
        this._alertService.error(err.error?.message || 'Failed to load borrows');
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
      { id: 'search', label: 'Search', type: 'text', placeholder: 'Member name or number...', value: '' },
    ];

    if (this.isSuperAdmin) {
      this.filterFields.push({
        id: 'schoolId', label: 'School', type: 'select', value: 'all',
        options: [
          { label: 'All Schools', value: 'all' },
          ...this.schools.map(s => ({ label: s.name, value: s.id })),
        ],
      });
    }

    this.filterFields.push(
      {
        id: 'status', label: 'Status', type: 'select', value: 'all',
        options: [
          { label: 'All Statuses', value: 'all'      },
          { label: 'Borrowed',     value: 'Borrowed'  },
          { label: 'Returned',     value: 'Returned'  },
          { label: 'Overdue',      value: 'Overdue'   },
        ],
      },
      {
        id: 'view', label: 'Quick View', type: 'select', value: 'all',
        options: [
          { label: 'All',          value: 'all'      },
          { label: 'Active',       value: 'active'   },
          { label: 'Overdue Only', value: 'overdue'  },
          { label: 'Returned',     value: 'returned' },
        ],
      },
    );
  }

  toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

  onFilterChange(event: FilterChangeEvent): void {
    (this.filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} transactions found`;
    if (event.filterId === 'schoolId' && this.isSuperAdmin) this.loadAll();
  }

  onClearFilters(): void {
    this.filterValues = { search: '', status: 'all', view: 'all', schoolId: 'all' };
    this.filterFields.forEach(f => { f.value = (this.filterValues as any)[f.id]; });
    this.currentPage = 1;
    this.loadAll();
  }

  onPageChange(page: number):      void { this.currentPage = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

  // ── CRUD & Actions ────────────────────────────────────────────────────────

  openCreate(): void { this._openCreate(); }

  private _openCreate(): void {
    this.openDialog(CreateBookBorrowDialogComponent, {
      panelClass: ['book-borrow-dialog', 'no-padding-dialog'],
      width: '750px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: true, autoFocus: 'input',
      data: { mode: 'create' },
    });
  }

  private _openEdit(borrow: BookBorrowDto): void {
    this.openDialog(CreateBookBorrowDialogComponent, {
      panelClass: ['book-borrow-dialog', 'no-padding-dialog'],
      width: '750px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: true, autoFocus: 'input',
      data: { mode: 'edit', borrow },
    });
  }

  private _openView(borrow: BookBorrowDto): void {
    this.openDialog(CreateBookBorrowDialogComponent, {
      panelClass: ['book-borrow-dialog', 'no-padding-dialog'],
      width: '900px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: false,
      data: { mode: 'view', borrow },
    });
  }

  private _returnAll(borrow: BookBorrowDto): void {
    const unreturned = borrow.items.filter(i => !i.isReturned).map(i => i.id);
    if (!unreturned.length) return;

    this._alertService.confirm({
      title:       'Return All Books',
      message:     `Return all ${unreturned.length} unreturned books from this borrow?`,
      confirmText: 'Return All',
      onConfirm: () => {
        this._borrowService.returnMultipleBooks({ borrowItemIds: unreturned }).pipe(takeUntil(this._destroy$)).subscribe({
          next: res => {
            if (res.success) {
              this._alertService.success('All books returned successfully');
              this.loadAll();
            } else {
              this._alertService.error(res.message || 'Failed to return books');
            }
          },
          error: err => this._alertService.error(err.error?.message || 'Failed'),
        });
      },
    });
  }

  private _markOverdue(borrow: BookBorrowDto): void {
    this._alertService.confirm({
      title:       'Process Overdue',
      message:     `Process overdue items for this transaction?`,
      confirmText: 'Process',
      onConfirm: () => {
        this._borrowService.processOverdue().pipe(takeUntil(this._destroy$)).subscribe({
          next: res => {
            if (res.success) {
              this._alertService.success('Overdue items processed');
              this.loadAll();
            } else {
              this._alertService.error(res.message || 'Failed to process');
            }
          },
          error: err => this._alertService.error(err.error?.message || 'Failed'),
        });
      },
    });
  }

  private _confirmDelete(borrow: BookBorrowDto): void {
    this._alertService.confirm({
      title:       'Delete Borrow',
      message:     `Delete borrow transaction for "${borrow.memberName}"? This cannot be undone.`,
      confirmText: 'Delete',
      onConfirm: () => {
        this.isLoading = true;
        this._borrowService.delete(borrow.id).pipe(takeUntil(this._destroy$)).subscribe({
          next: res => {
            if (res.success) {
              this._alertService.success('Borrow deleted successfully');
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

  getStatusColor(status: string, isOverdue: boolean): string {
    if (isOverdue) return 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400';
    const map: Record<string, string> = {
      'Borrowed': 'bg-blue-100  dark:bg-blue-900/30  text-blue-700  dark:text-blue-400',
      'Returned': 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400',
      'Overdue':  'bg-red-100   dark:bg-red-900/30   text-red-700   dark:text-red-400',
    };
    return map[status] || 'bg-gray-100 text-gray-600';
  }

  getStatusIcon(status: string, isOverdue: boolean): string {
    if (isOverdue) return 'schedule';
    const map: Record<string, string> = {
      'Borrowed': 'assignment_return',
      'Returned': 'check_circle',
      'Overdue':  'schedule',
    };
    return map[status] || 'info';
  }

  formatDate(dateStr?: string): string {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  formatCurrency(amount: number): string {
    if (!amount) return '—';
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'KES', minimumFractionDigits: 2 }).format(amount);
  }
}