import {
  Component, OnInit, OnDestroy, AfterViewInit, ViewChild, TemplateRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
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
import { BookCopyService } from 'app/core/DevKenService/Library/book-copy.service';
import { BookService } from 'app/core/DevKenService/Library/book.service';
import { LibraryBranchService } from 'app/core/DevKenService/Library/library-branch.service';
import { CreateEditBookCopyDialogComponent } from 'app/dialog-modals/Library/book-copy-dialog/create-edit-book-copy-dialog.component';
import { BaseListComponent } from 'app/shared/Lists/BaseListComponent';
import { BookDto } from '../book/Types/book.types';
import { LibraryBranchDto } from '../library-branch/Types/library-branch.types';
import { BookCopyDto } from './Types/book-copy.types';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-book-copies',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatButtonModule, MatDialogModule, MatProgressSpinnerModule,
    PageHeaderComponent, FilterPanelComponent, PaginationComponent,
    StatsCardsComponent, DataTableComponent,
  ],
  templateUrl: './book-copies.component.html',
})
export class BookCopiesComponent
  extends BaseListComponent<BookCopyDto>
  implements OnInit, OnDestroy, AfterViewInit {

  private _destroy$       = new Subject<void>();
  private alertService!: AlertService;
  private _copyService!:  BookCopyService;
  private _bookService!:  BookService;
  private _branchService!: LibraryBranchService;
  private _schoolService!: SchoolService;

  @ViewChild('copyCell',   { static: true }) copyCell!:   TemplateRef<any>;
  @ViewChild('bookCell',   { static: true }) bookCell!:   TemplateRef<any>;
  @ViewChild('branchCell', { static: true }) branchCell!: TemplateRef<any>;
  @ViewChild('statusCell', { static: true }) statusCell!: TemplateRef<any>;
  @ViewChild('schoolCell', { static: true }) schoolCell!: TemplateRef<any>;

  cellTemplates!: Record<string, TemplateRef<any>>;

  ngAfterViewInit(): void {
    this.cellTemplates = {
      copy:   this.copyCell,
      book:   this.bookCell,
      branch: this.branchCell,
      status: this.statusCell,
      school: this.schoolCell,
    };
  }

  // ── State ──────────────────────────────────────────────────────────────────
  schools:  SchoolDto[]        = [];
  books:    BookDto[]          = [];
  branches: LibraryBranchDto[] = [];
  isDataLoading   = true;
  showFilterPanel = false;
  currentPage     = 1;
  itemsPerPage    = 10;

  filterValues = {
    search: '', status: 'all', bookId: 'all',
    branchId: 'all', condition: 'all', schoolId: 'all',
  };

  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Library',   url: '/library'   },
    { label: 'Book Copies' },
  ];

  get isSuperAdmin(): boolean { return this._injectedAuth.authUser?.isSuperAdmin ?? false; }

  // ── Table config ───────────────────────────────────────────────────────────

  get tableColumns(): TableColumn<BookCopyDto>[] {
    const cols: TableColumn<BookCopyDto>[] = [
      { id: 'copy',   label: 'Copy',   align: 'left', sortable: true },
      { id: 'book',   label: 'Book',   align: 'left', hideOnMobile: true },
      { id: 'branch', label: 'Branch', align: 'left', hideOnMobile: true },
    ];
    if (this.isSuperAdmin) {
      cols.push({ id: 'school', label: 'School', align: 'left', hideOnMobile: true });
    }
    cols.push({ id: 'status', label: 'Status', align: 'center' });
    return cols;
  }

  tableActions: TableAction<BookCopyDto>[] = [
    {
      id: 'edit', label: 'Edit', icon: 'edit', color: 'blue',
      handler: r => this._openEdit(r),
    },
    {
      id: 'markAvailable', label: 'Mark Available', icon: 'check_circle', color: 'green',
      handler: r => this._markAvailable(r),
      visible: r => r.isLost || r.isDamaged || !r.isAvailable,
      divider: true,
    },
    {
      id: 'markLost', label: 'Mark as Lost', icon: 'help_outline', color: 'red',
      handler: r => this._markLost(r),
      visible: r => !r.isLost,
    },
    {
      id: 'markDamaged', label: 'Mark as Damaged', icon: 'warning', color: 'amber',
      handler: r => this._markDamaged(r),
      visible: r => !r.isDamaged,
      divider: true,
    },
    {
      id: 'delete', label: 'Delete', icon: 'delete', color: 'red',
      handler: r => this._confirmDelete(r),
    },
  ];

  tableHeader: TableHeader = {
    title:        'Book Copies',
    subtitle:     '',
    icon:         'table_chart',
    iconGradient: 'bg-gradient-to-br from-cyan-500 via-teal-600 to-emerald-700',
  };

  tableEmptyState: TableEmptyState = {
    icon:        'library_add',
    message:     'No book copies found',
    description: 'Try adjusting your filters or add a new copy',
    action:      { label: 'Add First Copy', icon: 'add', handler: () => this._openCreate() },
  };

  filterFields: FilterField[] = [];

  // ── Stats ──────────────────────────────────────────────────────────────────

  get statsCards(): StatCard[] {
    const data = this.dataSource.data;
    const base: StatCard[] = [
      { label: 'Total Copies',   value: data.length,                                                                         icon: 'library_books',     iconColor: 'indigo' },
      { label: 'Available',      value: data.filter(c => c.isAvailable).length,                                              icon: 'check_circle',      iconColor: 'green'  },
      { label: 'Borrowed',       value: data.filter(c => !c.isAvailable && !c.isLost && !c.isDamaged).length,               icon: 'assignment_return', iconColor: 'blue'   },
      { label: 'Lost / Damaged', value: data.filter(c => c.isLost || c.isDamaged).length,                                   icon: 'warning',           iconColor: 'red'    },
    ];
    if (this.isSuperAdmin) {
      base.push({ label: 'Schools', value: new Set(data.map(c => c.schoolId)).size, icon: 'school', iconColor: 'violet' });
    }
    return base;
  }

  // ── Filtered & Paginated ───────────────────────────────────────────────────

  get filteredData(): BookCopyDto[] {
    const q = this.filterValues.search.toLowerCase();
    return this.dataSource.data.filter(c =>
      (!q || c.accessionNumber?.toLowerCase().includes(q)
          || c.barcode?.toLowerCase().includes(q)
          || c.bookTitle?.toLowerCase().includes(q)) &&
      (this.filterValues.status    === 'all' || c.status?.toLowerCase()    === this.filterValues.status) &&
      (this.filterValues.bookId    === 'all' || c.bookId                   === this.filterValues.bookId) &&
      (this.filterValues.branchId  === 'all' || c.libraryBranchId          === this.filterValues.branchId) &&
      (this.filterValues.condition === 'all' || c.condition?.toLowerCase() === this.filterValues.condition.toLowerCase()) &&
      (this.filterValues.schoolId  === 'all' || c.schoolId                 === this.filterValues.schoolId)
    );
  }

  get paginatedData(): BookCopyDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  // ── Constructor ────────────────────────────────────────────────────────────

  constructor(
    dialog:       MatDialog,
    snackBar: MatSnackBar,
    alertService: AlertService,           // ← replaces MatSnackBar
    copyService:  BookCopyService,
    private readonly _injectedAuth: AuthService,
    schoolService:  SchoolService,
    bookService:    BookService,
    branchService:  LibraryBranchService,
  ) {
    super(copyService, dialog, snackBar);    
    this.alertService   = alertService;
    this._copyService   = copyService;
    this._schoolService = schoolService;
    this._bookService   = bookService;
    this._branchService = branchService;
  }

  ngOnInit():    void { this._loadMeta(); }
  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  // ── Data ───────────────────────────────────────────────────────────────────

  protected override loadAll(): void {
    this.isLoading = true;
    const schoolId = this.filterValues.schoolId !== 'all' ? this.filterValues.schoolId : undefined;
    this._copyService.getAll(schoolId).pipe(takeUntil(this._destroy$)).subscribe({
      next: res => {
        if (res.success) {
          this.dataSource.data = res.data;
          this.tableHeader.subtitle = `${this.filteredData.length} copies found`;
        } else {
          this.alertService.error(res.message || 'Failed to load copies');
        }
        this.isLoading = false;
      },
      error: err => {
        this.alertService.error(err.error?.message || 'Failed to load copies');
        this.isLoading = false;
      }
    });
  }

  private _loadMeta(): void {
    this.isDataLoading = true;

    const requests: any = {
      books:    this._bookService.getAll().pipe(catchError(() => of({ success: false, data: [] }))),
      branches: this._branchService.getAll().pipe(catchError(() => of({ success: false, data: [] }))),
    };
    if (this.isSuperAdmin) {
      requests.schools = this._schoolService.getAll().pipe(catchError(() => of({ success: false, data: [] })));
    }

    forkJoin(requests).pipe(
      takeUntil(this._destroy$),
      finalize(() => { this.isDataLoading = false; })
    ).subscribe({
      next: (res: any) => {
        this.books    = res.books?.data    || [];
        this.branches = res.branches?.data || [];
        if (res.schools) this.schools = res.schools?.data || [];
        this._initFilters();
        this.loadAll();
      },
      error: () => { this._initFilters(); this.loadAll(); }
    });
  }

  // ── Filters ────────────────────────────────────────────────────────────────

  private _initFilters(): void {
    this.filterFields = [
      { id: 'search', label: 'Search', type: 'text', placeholder: 'Accession no., barcode or title...', value: '' },
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
          { label: 'Available',    value: 'available' },
          { label: 'Borrowed',     value: 'borrowed'  },
          { label: 'Lost',         value: 'lost'      },
          { label: 'Damaged',      value: 'damaged'   },
        ],
      },
      {
        id: 'bookId', label: 'Book', type: 'select', value: 'all',
        options: [
          { label: 'All Books', value: 'all' },
          ...this.books.map(b => ({ label: b.title, value: b.id })),
        ],
      },
      {
        id: 'branchId', label: 'Branch', type: 'select', value: 'all',
        options: [
          { label: 'All Branches', value: 'all' },
          ...this.branches.map(b => ({ label: b.name, value: b.id })),
        ],
      },
      {
        id: 'condition', label: 'Condition', type: 'select', value: 'all',
        options: [
          { label: 'All Conditions', value: 'all'     },
          { label: 'Good',           value: 'Good'    },
          { label: 'Fair',           value: 'Fair'    },
          { label: 'Poor',           value: 'Poor'    },
          { label: 'Damaged',        value: 'Damaged' },
        ],
      },
    );
  }

  toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

  onFilterChange(event: FilterChangeEvent): void {
    (this.filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} copies found`;
    if (event.filterId === 'schoolId' && this.isSuperAdmin) this.loadAll();
  }

  onClearFilters(): void {
    this.filterValues = { search: '', status: 'all', bookId: 'all', branchId: 'all', condition: 'all', schoolId: 'all' };
    this.filterFields.forEach(f => { f.value = (this.filterValues as any)[f.id]; });
    this.currentPage = 1;
    this.loadAll();
  }

  onPageChange(page: number):      void { this.currentPage = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

  // ── CRUD & Actions ─────────────────────────────────────────────────────────

  openCreate(): void { this._openCreate(); }

  private _openCreate(): void {
    this.openDialog(CreateEditBookCopyDialogComponent, {
      panelClass: ['book-copy-dialog', 'no-padding-dialog'],
      width: '750px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: true, autoFocus: 'input',
      data: { mode: 'create' },
    });
  }

  private _openEdit(copy: BookCopyDto): void {
    this.openDialog(CreateEditBookCopyDialogComponent, {
      panelClass: ['book-copy-dialog', 'no-padding-dialog'],
      width: '750px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: true, autoFocus: 'input',
      data: { mode: 'edit', copy },
    });
  }

  private _markAvailable(copy: BookCopyDto): void {
    this.alertService.confirm({
      title:       'Mark as Available',
      message:     `Mark copy "${copy.accessionNumber}" as available?`,
      confirmText: 'Mark Available',
      onConfirm: () => {
        this._copyService.markAvailable(copy.id).pipe(takeUntil(this._destroy$)).subscribe({
          next: res => {
            if (res.success) {
              this.alertService.success('Copy marked as available');
              this.loadAll();
            } else {
              this.alertService.error(res.message || 'Failed to mark as available');
            }
          },
          error: err => this.alertService.error(err.error?.message || 'Failed'),
        });
      },
    });
  }

  private _markLost(copy: BookCopyDto): void {
    this.alertService.confirm({
      title:       'Mark as Lost',
      message:     `Mark copy "${copy.accessionNumber}" as lost?`,
      confirmText: 'Mark Lost',
      onConfirm: () => {
        this._copyService.markLost(copy.id, {}).pipe(takeUntil(this._destroy$)).subscribe({
          next: res => {
            if (res.success) {
              this.alertService.success('Copy marked as lost');
              this.loadAll();
            } else {
              this.alertService.error(res.message || 'Failed to mark as lost');
            }
          },
          error: err => this.alertService.error(err.error?.message || 'Failed'),
        });
      },
    });
  }

  private _markDamaged(copy: BookCopyDto): void {
    this.alertService.confirm({
      title:       'Mark as Damaged',
      message:     `Mark copy "${copy.accessionNumber}" as damaged?`,
      confirmText: 'Mark Damaged',
      onConfirm: () => {
        this._copyService.markDamaged(copy.id, {}).pipe(takeUntil(this._destroy$)).subscribe({
          next: res => {
            if (res.success) {
              this.alertService.success('Copy marked as damaged');
              this.loadAll();
            } else {
              this.alertService.error(res.message || 'Failed to mark as damaged');
            }
          },
          error: err => this.alertService.error(err.error?.message || 'Failed'),
        });
      },
    });
  }

  private _confirmDelete(copy: BookCopyDto): void {
    this.alertService.confirm({
      title:       'Delete Copy',
      message:     `Delete copy "${copy.accessionNumber}"? This cannot be undone.`,
      confirmText: 'Delete',
      onConfirm: () => {
        this.isLoading = true;
        this._copyService.delete(copy.id).pipe(takeUntil(this._destroy$)).subscribe({
          next: res => {
            if (res.success) {
              this.alertService.success('Copy deleted successfully');
              if (this.paginatedData.length === 1 && this.currentPage > 1) this.currentPage--;
              this.loadAll();
            } else {
              this.alertService.error(res.message || 'Failed to delete');
            }
            this.isLoading = false;
          },
          error: err => {
            this.alertService.error(err.error?.message || 'Failed to delete');
            this.isLoading = false;
          }
        });
      },
    });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────

  getConditionColor(condition: string): string {
    const map: Record<string, string> = {
      'Good':    'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400',
      'Fair':    'bg-blue-100  dark:bg-blue-900/30  text-blue-700  dark:text-blue-400',
      'Poor':    'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400',
      'Damaged': 'bg-red-100   dark:bg-red-900/30   text-red-700   dark:text-red-400',
    };
    return map[condition] || 'bg-gray-100 text-gray-600';
  }

  getStatusColor(status: string): string {
    const map: Record<string, string> = {
      'Available': 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400',
      'Borrowed':  'bg-blue-100  dark:bg-blue-900/30  text-blue-700  dark:text-blue-400',
      'Lost':      'bg-red-100   dark:bg-red-900/30   text-red-700   dark:text-red-400',
      'Damaged':   'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400',
    };
    return map[status] || 'bg-gray-100 text-gray-600';
  }

  getStatusIcon(status: string): string {
    const map: Record<string, string> = {
      'Available': 'check_circle',
      'Borrowed':  'assignment_return',
      'Lost':      'help_outline',
      'Damaged':   'warning',
    };
    return map[status] || 'info';
  }

  formatDate(dateStr?: string): string {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }
}