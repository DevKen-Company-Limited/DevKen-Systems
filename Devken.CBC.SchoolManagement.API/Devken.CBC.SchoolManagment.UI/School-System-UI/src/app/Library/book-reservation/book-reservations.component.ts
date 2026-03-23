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
import { DataTableComponent, TableColumn, TableAction, TableHeader, TableEmptyState } from 'app/shared/data-table/data-table.component';
import { BaseListComponent } from 'app/shared/Lists/BaseListComponent';

import { BookReservationService } from 'app/core/DevKenService/Library/book-reservation.service';
import { BookService } from 'app/core/DevKenService/Library/book.service';
import { BookDto } from 'app/Library/book/Types/book.types';
import { BookReservationDto } from './Types/book-reservation.types';
import { CreateEditReservationDialogComponent } from 'app/dialog-modals/Library/book-reservation-dialog/create-edit-reservation-dialog.component';


@Component({
  selector: 'app-book-reservations',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatButtonModule, MatDialogModule, MatProgressSpinnerModule,
    PageHeaderComponent, FilterPanelComponent, PaginationComponent,
    StatsCardsComponent, DataTableComponent,
  ],
  templateUrl: './book-reservations.component.html',
})
export class BookReservationsComponent
  extends BaseListComponent<BookReservationDto>
  implements OnInit, OnDestroy, AfterViewInit {

  private _destroy$ = new Subject<void>();

  private _alertService!: AlertService;
  private _reservationService!: BookReservationService;
  private _bookService!: BookService;
  private _schoolService!: SchoolService;

  @ViewChild('bookCell',     { static: true }) bookCell!:     TemplateRef<any>;
  @ViewChild('memberCell',   { static: true }) memberCell!:   TemplateRef<any>;
  @ViewChild('dateCell',     { static: true }) dateCell!:     TemplateRef<any>;
  @ViewChild('statusCell',   { static: true }) statusCell!:   TemplateRef<any>;
  @ViewChild('schoolCell',   { static: true }) schoolCell!:   TemplateRef<any>;

  cellTemplates!: Record<string, TemplateRef<any>>;

  ngAfterViewInit(): void {
    this.cellTemplates = {
      book:    this.bookCell,
      member:  this.memberCell,
      date:    this.dateCell,
      status:  this.statusCell,
      school:  this.schoolCell,
    };
  }

  // ── State ──────────────────────────────────────────────────────────────────
  schools: SchoolDto[] = [];
  books:   BookDto[]   = [];
  isDataLoading   = true;
  showFilterPanel = false;
  currentPage     = 1;
  itemsPerPage    = 10;

  filterValues = {
    search: '', statusFilter: 'all', schoolId: 'all',
  };

  // ── Breadcrumbs ────────────────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Library',   url: '/library'   },
    { label: 'Reservations' },
  ];

  // ── Table config ───────────────────────────────────────────────────────────
  get isSuperAdmin(): boolean { return this._injectedAuth.authUser?.isSuperAdmin ?? false; }

  get tableColumns(): TableColumn<BookReservationDto>[] {
    const cols: TableColumn<BookReservationDto>[] = [
      { id: 'book',   label: 'Book',       align: 'left', sortable: true },
      { id: 'member', label: 'Member',     align: 'left', hideOnMobile: true },
      { id: 'date',   label: 'Reserved On',align: 'left', hideOnTablet: true },
      { id: 'status', label: 'Status',     align: 'center' },
    ];
    if (this.isSuperAdmin) {
      cols.push({ id: 'school', label: 'School', align: 'left', hideOnMobile: true });
    }
    return cols;
  }

  tableActions: TableAction<BookReservationDto>[] = [
    {
      id: 'fulfill', label: 'Mark Fulfilled', icon: 'check_circle', color: 'green',
      handler: r => this._confirmFulfill(r),
      visible: r => !r.isFulfilled,
    },
    {
      id: 'edit', label: 'Edit', icon: 'edit', color: 'blue',
      handler: r => this._openEdit(r),
    },
    {
      id: 'delete', label: 'Delete', icon: 'delete', color: 'red',
      handler: r => this._confirmDelete(r),
      divider: true,
      visible: r => !r.isFulfilled,
    },
  ];

  tableHeader: TableHeader = {
    title:        'Book Reservations',
    subtitle:     '',
    icon:         'table_chart',
    iconGradient: 'bg-gradient-to-br from-teal-500 via-cyan-600 to-blue-700',
  };

  tableEmptyState: TableEmptyState = {
    icon:        'bookmark',
    message:     'No reservations found',
    description: 'Try adjusting your filters or create a new reservation',
    action: { label: 'New Reservation', icon: 'add', handler: () => this._openCreate() },
  };

  filterFields: FilterField[] = [];

  // ── Stats ──────────────────────────────────────────────────────────────────
  get statsCards(): StatCard[] {
    const data = this.dataSource.data;
    const pending   = data.filter(r => !r.isFulfilled).length;
    const fulfilled = data.filter(r =>  r.isFulfilled).length;

    const base: StatCard[] = [
      { label: 'Total',     value: data.length, icon: 'bookmark',      iconColor: 'indigo' },
      { label: 'Pending',   value: pending,      icon: 'pending',       iconColor: 'amber'  },
      { label: 'Fulfilled', value: fulfilled,    icon: 'check_circle',  iconColor: 'green'  },
    ];
    if (this.isSuperAdmin) {
      base.push({ label: 'Schools', value: new Set(data.map(r => r.schoolId)).size, icon: 'school', iconColor: 'violet' });
    }
    return base;
  }

  // ── Filtered & paginated ───────────────────────────────────────────────────
  get filteredData(): BookReservationDto[] {
    const q = this.filterValues.search.toLowerCase();
    return this.dataSource.data.filter(r =>
      (!q || r.bookTitle?.toLowerCase().includes(q) || r.memberName?.toLowerCase().includes(q)) &&
      (this.filterValues.statusFilter === 'all'       ||
       (this.filterValues.statusFilter === 'pending'   && !r.isFulfilled) ||
       (this.filterValues.statusFilter === 'fulfilled' &&  r.isFulfilled)) &&
      (this.filterValues.schoolId === 'all' || r.schoolId === this.filterValues.schoolId)
    );
  }

  get paginatedData(): BookReservationDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  constructor(
    dialog: MatDialog,
    snackBar: MatSnackBar,
    reservationService: BookReservationService,
    private readonly _injectedAuth: AuthService,
    alertService: AlertService,
    schoolService: SchoolService,
    bookService: BookService,
  ) {
    super(reservationService, dialog, snackBar);
    this._alertService        = alertService;
    this._reservationService  = reservationService;
    this._schoolService       = schoolService;
    this._bookService         = bookService;
  }

  // ── Lifecycle ──────────────────────────────────────────────────────────────
  ngOnInit(): void    { this._loadMeta(); }
  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  // ── Data loading ───────────────────────────────────────────────────────────
  protected override loadAll(): void {
    this.isLoading = true;
    const schoolId = this.filterValues.schoolId !== 'all' ? this.filterValues.schoolId : undefined;
    this._reservationService.getAll(schoolId)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: res => {
          if (res.success) {
            this.dataSource.data = res.data;
            this.tableHeader.subtitle = `${this.filteredData.length} reservations found`;
          } else {
            this._alertService.error(res.message || 'Failed to load reservations');
          }
          this.isLoading = false;
        },
        error: err => {
          this._alertService.error(err.error?.message || 'Failed to load reservations');
          this.isLoading = false;
        },
      });
  }

  private _loadMeta(): void {
    this.isDataLoading = true;

    const requests: any = {
      books: this._bookService.getAll().pipe(catchError(() => of({ success: false, data: [] }))),
    };
    if (this.isSuperAdmin) {
      requests.schools = this._schoolService.getAll().pipe(
        catchError(() => of({ success: false, data: [] }))
      );
    }

    forkJoin(requests).pipe(
      takeUntil(this._destroy$),
      finalize(() => { this.isDataLoading = false; })
    ).subscribe({
      next: (res: any) => {
        this.books = res.books?.data || [];
        if (res.schools) this.schools = res.schools?.data || [];
        this._initFilters();
        this.loadAll();
      },
      error: () => { this._initFilters(); this.loadAll(); },
    });
  }

  // ── Filters ────────────────────────────────────────────────────────────────
  private _initFilters(): void {
    this.filterFields = [
      { id: 'search', label: 'Search', type: 'text', placeholder: 'Book title or member...', value: '' },
      {
        id: 'statusFilter', label: 'Status', type: 'select', value: 'all',
        options: [
          { label: 'All Statuses', value: 'all' },
          { label: 'Pending',      value: 'pending' },
          { label: 'Fulfilled',    value: 'fulfilled' },
        ],
      },
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
  }

  toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

  onFilterChange(event: FilterChangeEvent): void {
    (this.filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} reservations found`;
    if (event.filterId === 'schoolId' && this.isSuperAdmin) {
      this.loadAll();
    }
  }

  onClearFilters(): void {
    this.filterValues = { search: '', statusFilter: 'all', schoolId: 'all' };
    this.filterFields.forEach(f => { f.value = (this.filterValues as any)[f.id]; });
    this.currentPage = 1;
    this.loadAll();
  }

  onPageChange(page: number): void      { this.currentPage = page; }
  onItemsPerPageChange(n: number): void  { this.itemsPerPage = n; this.currentPage = 1; }

  // ── CRUD ──────────────────────────────────────────────────────────────────
  private _openCreate(): void {
    this.openDialog(CreateEditReservationDialogComponent, {
      panelClass: ['reservation-dialog', 'no-padding-dialog'],
      width: '620px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: true, autoFocus: 'mat-select',
      data: { mode: 'create' },
    });
  }

  private _openEdit(reservation: BookReservationDto): void {
    this.openDialog(CreateEditReservationDialogComponent, {
      panelClass: ['reservation-dialog', 'no-padding-dialog'],
      width: '620px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: true, autoFocus: 'mat-select',
      data: { mode: 'edit', reservation },
    });
  }

  openCreate(): void { this._openCreate(); }

  private _confirmFulfill(reservation: BookReservationDto): void {
    this._alertService.confirm({
      title:       'Fulfill Reservation',
      message:     `Mark reservation for "${reservation.bookTitle}" as fulfilled? This means the book has been handed to ${reservation.memberName || 'the member'}.`,
      confirmText: 'Mark Fulfilled',
      onConfirm:   () => this._doFulfill(reservation),
    });
  }

  private _doFulfill(reservation: BookReservationDto): void {
    this.isLoading = true;
    this._reservationService.fulfill(reservation.id)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: res => {
          if (res.success) {
            this._alertService.success('Reservation marked as fulfilled');
            this.loadAll();
          } else {
            this._alertService.error(res.message || 'Failed to fulfill reservation');
          }
          this.isLoading = false;
        },
        error: err => {
          this._alertService.error(err.error?.message || 'Failed to fulfill reservation');
          this.isLoading = false;
        },
      });
  }

  private _confirmDelete(reservation: BookReservationDto): void {
    this._alertService.confirm({
      title:       'Delete Reservation',
      message:     `Delete reservation for "${reservation.bookTitle}"? This action cannot be undone.`,
      confirmText: 'Delete',
      onConfirm:   () => this._doDelete(reservation),
    });
  }

  private _doDelete(reservation: BookReservationDto): void {
    this.isLoading = true;
    this._reservationService.delete(reservation.id)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: res => {
          if (res.success) {
            this._alertService.success('Reservation deleted successfully');
            if (this.paginatedData.length === 1 && this.currentPage > 1) this.currentPage--;
            this.loadAll();
          } else {
            this._alertService.error(res.message || 'Failed to delete');
          }
          this.isLoading = false;
        },
        error: err => {
          this._alertService.error(err.error?.message || 'Failed to delete reservation');
          this.isLoading = false;
        },
      });
  }
}