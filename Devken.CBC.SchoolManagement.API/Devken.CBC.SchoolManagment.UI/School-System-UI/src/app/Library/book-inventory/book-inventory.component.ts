import {
  Component, OnInit, OnDestroy, AfterViewInit, ViewChild, TemplateRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Subject, of } from 'rxjs';
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
import { BookInventoryService } from 'app/core/DevKenService/Library/book-inventory.service';
import { CreateEditBookInventoryDialogComponent } from 'app/dialog-modals/Library/book-inventory-dialog/create-edit-book-inventory-dialog.component';
import { BaseListComponent } from 'app/shared/Lists/BaseListComponent';
import { BookInventoryDto } from './Types/book-inventory.types';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-book-inventory',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatButtonModule, MatDialogModule, MatProgressSpinnerModule,
    PageHeaderComponent, FilterPanelComponent, PaginationComponent,
    StatsCardsComponent, DataTableComponent,
  ],
  templateUrl: './book-inventory.component.html',
})
export class BookInventoryComponent
  extends BaseListComponent<BookInventoryDto>
  implements OnInit, OnDestroy, AfterViewInit {

  private _destroy$          = new Subject<void>();
  private alertService!: AlertService;
  private _inventoryService!: BookInventoryService;
  private _schoolService!:   SchoolService;

  @ViewChild('bookCell',         { static: true }) bookCell!:         TemplateRef<any>;
  @ViewChild('countsCell',       { static: true }) countsCell!:       TemplateRef<any>;
  @ViewChild('availabilityCell', { static: true }) availabilityCell!: TemplateRef<any>;
  @ViewChild('schoolCell',       { static: true }) schoolCell!:       TemplateRef<any>;

  cellTemplates!: Record<string, TemplateRef<any>>;

  ngAfterViewInit(): void {
    this.cellTemplates = {
      book:         this.bookCell,
      counts:       this.countsCell,
      availability: this.availabilityCell,
      school:       this.schoolCell,
    };
  }

  // ── State ──────────────────────────────────────────────────────────────────
  schools:        SchoolDto[] = [];
  isDataLoading   = true;
  showFilterPanel = false;
  currentPage     = 1;
  itemsPerPage    = 10;

  filterValues = { search: '', schoolId: 'all' };

  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Library',   url: '/library'   },
    { label: 'Inventory' },
  ];

  get isSuperAdmin(): boolean { return this._injectedAuth.authUser?.isSuperAdmin ?? false; }

  // ── Table config ───────────────────────────────────────────────────────────

  get tableColumns(): TableColumn<BookInventoryDto>[] {
    const cols: TableColumn<BookInventoryDto>[] = [
      { id: 'book',         label: 'Book',         align: 'left', sortable: true },
      { id: 'counts',       label: 'Copies',       align: 'center' },
      { id: 'availability', label: 'Availability', align: 'center', hideOnMobile: true },
    ];
    if (this.isSuperAdmin) {
      cols.push({ id: 'school', label: 'School', align: 'left', hideOnMobile: true });
    }
    return cols;
  }

  tableActions: TableAction<BookInventoryDto>[] = [
    {
      id: 'edit', label: 'Edit', icon: 'edit', color: 'blue',
      handler: r => this._openEdit(r),
    },
    {
      id: 'recalculate', label: 'Recalculate', icon: 'refresh', color: 'indigo',
      handler: r => this._recalculate(r),
      divider: true,
    },
    {
      id: 'delete', label: 'Delete', icon: 'delete', color: 'red',
      handler: r => this._confirmDelete(r),
    },
  ];

  tableHeader: TableHeader = {
    title:        'Book Inventory',
    subtitle:     '',
    icon:         'table_chart',
    iconGradient: 'bg-gradient-to-br from-violet-500 via-purple-600 to-indigo-700',
  };

  tableEmptyState: TableEmptyState = {
    icon:        'inventory',
    message:     'No inventory records found',
    description: 'Create inventory records or add book copies to auto-generate them',
    action:      { label: 'Create Record', icon: 'add', handler: () => this._openCreate() },
  };

  filterFields: FilterField[] = [];

  // ── Stats ──────────────────────────────────────────────────────────────────

  get statsCards(): StatCard[] {
    const data = this.dataSource.data;
    return [
      { label: 'Total Books',    value: data.length,                                                            icon: 'inventory',         iconColor: 'violet' },
      { label: 'Total Copies',   value: data.reduce((s, i) => s + i.totalCopies, 0),                          icon: 'library_books',     iconColor: 'indigo' },
      { label: 'Available',      value: data.reduce((s, i) => s + i.availableCopies, 0),                      icon: 'check_circle',      iconColor: 'green'  },
      { label: 'Borrowed',       value: data.reduce((s, i) => s + i.borrowedCopies, 0),                       icon: 'assignment_return', iconColor: 'blue'   },
      { label: 'Lost / Damaged', value: data.reduce((s, i) => s + i.lostCopies + i.damagedCopies, 0),        icon: 'warning',           iconColor: 'red'    },
    ];
  }

  // ── Filtered & Paginated ───────────────────────────────────────────────────

  get filteredData(): BookInventoryDto[] {
    const q = this.filterValues.search.toLowerCase();
    return this.dataSource.data.filter(i =>
      (!q || i.bookTitle?.toLowerCase().includes(q)
          || i.bookISBN?.toLowerCase().includes(q)
          || i.authorName?.toLowerCase().includes(q)) &&
      (this.filterValues.schoolId === 'all' || i.schoolId === this.filterValues.schoolId)
    );
  }

  get paginatedData(): BookInventoryDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  // ── Constructor ────────────────────────────────────────────────────────────

  constructor(
    dialog:           MatDialog,
    snackBar: MatSnackBar,
    alertService:     AlertService,       // ← replaces MatSnackBar
    inventoryService: BookInventoryService,
    private readonly _injectedAuth: AuthService,
    schoolService:    SchoolService,
  ) {
    super(inventoryService, dialog, snackBar);
    this.alertService    = alertService;
    this._inventoryService = inventoryService;
    this._schoolService    = schoolService;
  }

  ngOnInit():    void { this._loadMeta(); }
  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  // ── Data ───────────────────────────────────────────────────────────────────

  protected override loadAll(): void {
    this.isLoading = true;
    const schoolId = this.filterValues.schoolId !== 'all' ? this.filterValues.schoolId : undefined;
    this._inventoryService.getAll(schoolId).pipe(takeUntil(this._destroy$)).subscribe({
      next: res => {
        if (res.success) {
          this.dataSource.data = res.data;
          this.tableHeader.subtitle = `${this.filteredData.length} records found`;
        } else {
          this.alertService.error(res.message || 'Failed to load inventory');
        }
        this.isLoading = false;
      },
      error: err => {
        this.alertService.error(err.error?.message || 'Failed to load inventory');
        this.isLoading = false;
      }
    });
  }

  private _loadMeta(): void {
    this.isDataLoading = true;
    if (!this.isSuperAdmin) {
      this.isDataLoading = false;
      this._initFilters();
      this.loadAll();
      return;
    }

    this._schoolService.getAll()
      .pipe(
        catchError(() => of({ success: false, data: [] })),
        takeUntil(this._destroy$),
        finalize(() => { this.isDataLoading = false; })
      )
      .subscribe((res: any) => {
        this.schools = res.data || [];
        this._initFilters();
        this.loadAll();
      });
  }

  private _initFilters(): void {
    this.filterFields = [
      { id: 'search', label: 'Search', type: 'text', placeholder: 'Title, ISBN or author...', value: '' },
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
    this.tableHeader.subtitle = `${this.filteredData.length} records found`;
    if (event.filterId === 'schoolId' && this.isSuperAdmin) this.loadAll();
  }

  onClearFilters(): void {
    this.filterValues = { search: '', schoolId: 'all' };
    this.filterFields.forEach(f => { f.value = (this.filterValues as any)[f.id]; });
    this.currentPage = 1;
    this.loadAll();
  }

  onPageChange(page: number):      void { this.currentPage = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

  // ── CRUD & Actions ─────────────────────────────────────────────────────────

  openCreate(): void { this._openCreate(); }

  private _openCreate(): void {
    this.openDialog(CreateEditBookInventoryDialogComponent, {
      panelClass: ['book-inventory-dialog', 'no-padding-dialog'],
      width: '600px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: true, autoFocus: 'input',
      data: { mode: 'create' },
    });
  }

  private _openEdit(inventory: BookInventoryDto): void {
    this.openDialog(CreateEditBookInventoryDialogComponent, {
      panelClass: ['book-inventory-dialog', 'no-padding-dialog'],
      width: '600px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: true, autoFocus: 'input',
      data: { mode: 'edit', inventory },
    });
  }

  private _recalculate(inventory: BookInventoryDto): void {
    this.alertService.confirm({
      title:       'Recalculate Inventory',
      message:     `Recalculate inventory counts for "${inventory.bookTitle}" from actual copy records?`,
      confirmText: 'Recalculate',
      onConfirm: () => {
        this.isLoading = true;
        this._inventoryService.recalculate(inventory.bookId).pipe(takeUntil(this._destroy$)).subscribe({
          next: res => {
            if (res.success) {
              this.alertService.success('Inventory recalculated successfully');
              this.loadAll();
            } else {
              this.alertService.error(res.message || 'Recalculate failed');
            }
            this.isLoading = false;
          },
          error: err => {
            this.alertService.error(err.error?.message || 'Recalculate failed');
            this.isLoading = false;
          }
        });
      },
    });
  }

  private _confirmDelete(inventory: BookInventoryDto): void {
    this.alertService.confirm({
      title:       'Delete Inventory Record',
      message:     `Delete inventory record for "${inventory.bookTitle}"?`,
      confirmText: 'Delete',
      onConfirm: () => {
        this.isLoading = true;
        this._inventoryService.delete(inventory.id).pipe(takeUntil(this._destroy$)).subscribe({
          next: res => {
            if (res.success) {
              this.alertService.success('Record deleted successfully');
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

  getAvailabilityColor(pct: number): string {
    if (pct >= 70) return 'bg-green-500';
    if (pct >= 40) return 'bg-amber-500';
    return 'bg-red-500';
  }

  getAvailabilityTextColor(pct: number): string {
    if (pct >= 70) return 'text-green-700 dark:text-green-400';
    if (pct >= 40) return 'text-amber-700 dark:text-amber-400';
    return 'text-red-700 dark:text-red-400';
  }
}