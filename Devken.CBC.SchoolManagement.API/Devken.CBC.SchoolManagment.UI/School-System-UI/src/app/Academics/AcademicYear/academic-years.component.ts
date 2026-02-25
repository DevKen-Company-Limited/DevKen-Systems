import { Component, OnInit, OnDestroy, AfterViewInit, ViewChild, TemplateRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { AcademicYearService } from 'app/core/DevKenService/AcademicYearService/AcademicYearService';
import { CreateEditAcademicYearDialogComponent } from 'app/dialog-modals/Academic Year/create-edit-academic-year-dialog.component';
import { AcademicYearDto } from './Types/AcademicYear';

import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';
import { DataTableComponent, TableColumn, TableAction, TableHeader, TableEmptyState } from 'app/shared/data-table/data-table.component';

@Component({
  selector: 'app-academic-years',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatButtonModule, MatDialogModule, MatProgressSpinnerModule,
    PageHeaderComponent, FilterPanelComponent, PaginationComponent,
    StatsCardsComponent, DataTableComponent,
  ],
  templateUrl: './academic-years.component.html',
})
export class AcademicYearsComponent implements OnInit, OnDestroy, AfterViewInit {
  private _destroy$ = new Subject<void>();
  private _service = inject(AcademicYearService);
  private _alertService = inject(AlertService);
  private _dialog = inject(MatDialog);

  @ViewChild('codeCell', { static: true }) codeCell!: TemplateRef<any>;
  @ViewChild('nameCell', { static: true }) nameCell!: TemplateRef<any>;
  @ViewChild('startDateCell', { static: true }) startDateCell!: TemplateRef<any>;
  @ViewChild('endDateCell', { static: true }) endDateCell!: TemplateRef<any>;
  @ViewChild('statusCell', { static: true }) statusCell!: TemplateRef<any>;

  cellTemplates!: Record<string, TemplateRef<any>>;

  ngAfterViewInit(): void {
    this.cellTemplates = {
      code:      this.codeCell,
      name:      this.nameCell,
      startDate: this.startDateCell,
      endDate:   this.endDateCell,
      status:    this.statusCell,
    };
  }

  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Settings',  url: '/settings' },
    { label: 'Academic Years' },
  ];

  allData: AcademicYearDto[] = [];
  isLoading = false;
  showFilterPanel = false;
  availableYears: string[] = [];

  currentPage = 1;
  itemsPerPage = 10;

  private _filterValues = { search: '', status: 'all', year: 'all' };

  // ── Stats ─────────────────────────────────────────────────────────────────────
  get statsCards(): StatCard[] {
    return [
      { label: 'Total Years',   value: this.allData.length,                                    icon: 'calendar_today', iconColor: 'pink'  },
      { label: 'Current Year',  value: this.currentYearName,                                   icon: 'today',          iconColor: 'green' },
      { label: 'Open Years',    value: this.allData.filter(ay => !ay.isClosed).length,         icon: 'lock_open',      iconColor: 'blue'  },
    ];
  }

  get currentYearName(): string {
    return this.allData.find(ay => ay.isCurrent)?.name ?? 'None';
  }

  // ── Table ─────────────────────────────────────────────────────────────────────
  tableColumns: TableColumn<AcademicYearDto>[] = [
    { id: 'code',      label: 'Code',       align: 'left',   sortable: true  },
    { id: 'name',      label: 'Name',       align: 'left',   sortable: true  },
    { id: 'startDate', label: 'Start Date', align: 'left',   hideOnMobile: true },
    { id: 'endDate',   label: 'End Date',   align: 'left',   hideOnMobile: true },
    { id: 'status',    label: 'Status',     align: 'center'                   },
  ];

  tableActions: TableAction<AcademicYearDto>[] = [
    {
      id: 'edit', label: 'Edit', icon: 'edit', color: 'blue',
      handler:  r => this.openEdit(r),
      disabled: r => r.isClosed,
    },
    {
      id: 'setCurrent', label: 'Set as Current', icon: 'check_circle', color: 'green',
      handler: r => this.setAsCurrent(r),
      visible: r => !r.isCurrent && !r.isClosed,
    },
    {
      id: 'closeYear', label: 'Close Year', icon: 'lock', color: 'amber',
      handler: r => this.closeYear(r),
      visible: r => !r.isClosed,
      divider: true,
    },
    {
      id: 'delete', label: 'Delete', icon: 'delete', color: 'red',
      handler: r => this.removeAcademicYear(r),
    },
  ];

  tableHeader: TableHeader = {
    title: 'Academic Years List', subtitle: '',
    icon: 'table_chart',
    iconGradient: 'bg-gradient-to-br from-blue-500 via-cyan-600 to-teal-700',
  };

  tableEmptyState: TableEmptyState = {
    icon: 'search_off', message: 'No academic years found',
    description: 'Try adjusting your filters or create a new academic year',
    action: { label: 'Create Academic Year', icon: 'add', handler: () => this.openCreate() },
  };

  filterFields: FilterField[] = [];

  // ── Filtering & Pagination ────────────────────────────────────────────────────
  get filteredData(): AcademicYearDto[] {
    const q = this._filterValues.search.toLowerCase();
    return this.allData.filter(ay => {
      const matchesSearch = !q || ay.name.toLowerCase().includes(q) || ay.code.toLowerCase().includes(q);
      const matchesStatus =
        this._filterValues.status === 'all' ||
        (this._filterValues.status === 'current' && ay.isCurrent) ||
        (this._filterValues.status === 'closed'  && ay.isClosed)  ||
        (this._filterValues.status === 'open'    && !ay.isClosed && !ay.isCurrent);
      const matchesYear =
        this._filterValues.year === 'all' ||
        new Date(ay.startDate).getFullYear().toString() === this._filterValues.year;
      return matchesSearch && matchesStatus && matchesYear;
    });
  }

  get paginatedData(): AcademicYearDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  ngOnInit(): void {
    this.initFilterFields();
    this.loadAll();
  }

  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  // ── Filter ────────────────────────────────────────────────────────────────────
  private initFilterFields(): void {
    this.filterFields = [
      { id: 'search', label: 'Search', type: 'text', placeholder: 'Code or name...', value: '' },
      {
        id: 'status', label: 'Status', type: 'select', value: 'all',
        options: [
          { label: 'All Statuses', value: 'all'     },
          { label: 'Current',      value: 'current' },
          { label: 'Open',         value: 'open'    },
          { label: 'Closed',       value: 'closed'  },
        ],
      },
      {
        id: 'year', label: 'Year', type: 'select', value: 'all',
        options: [{ label: 'All Years', value: 'all' }],
      },
    ];
  }

  private updateYearFilterOptions(): void {
    const yearField = this.filterFields.find(f => f.id === 'year');
    if (yearField) {
      yearField.options = [
        { label: 'All Years', value: 'all' },
        ...this.availableYears.map(y => ({ label: y, value: y })),
      ];
    }
  }

  toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

  onFilterChange(event: FilterChangeEvent): void {
    (this._filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} years found`;
  }

  onClearFilters(): void {
    this._filterValues = { search: '', status: 'all', year: 'all' };
    this.filterFields.forEach(f => { f.value = (this._filterValues as any)[f.id]; });
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} years found`;
  }

  onPageChange(page: number): void { this.currentPage = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

  // ── Data ──────────────────────────────────────────────────────────────────────
  loadAll(): void {
    this.isLoading = true;
    this._service.getAll().pipe(takeUntil(this._destroy$)).subscribe({
      next: res => {
        if (res.success) {
          this.allData = res.data;
          this.availableYears = [...new Set(this.allData.map(ay =>
            new Date(ay.startDate).getFullYear().toString()
          ))].sort((a, b) => b.localeCompare(a));
          this.updateYearFilterOptions();
          this.tableHeader.subtitle = `${this.filteredData.length} years found`;
        }
        this.isLoading = false;
      },
      error: err => {
        this._alertService.error(err?.error?.message || 'Failed to load academic years');
        this.isLoading = false;
      },
    });
  }

  // ── CRUD ──────────────────────────────────────────────────────────────────────
  openCreate(): void {
    const ref = this._dialog.open(CreateEditAcademicYearDialogComponent, {
      width: '600px', data: { mode: 'create' },
    });
    ref.afterClosed().pipe(takeUntil(this._destroy$)).subscribe(result => {
      if (!result) return;
      this._service.create(result).pipe(takeUntil(this._destroy$)).subscribe({
        next: res => { if (res.success) { this._alertService.success('Academic year created successfully'); this.loadAll(); } },
        error: err => this._alertService.error(err.error?.message || 'Failed to create academic year'),
      });
    });
  }

  openEdit(year: AcademicYearDto): void {
    if (year.isClosed) { this._alertService.error('Cannot edit a closed academic year'); return; }
    const ref = this._dialog.open(CreateEditAcademicYearDialogComponent, {
      width: '600px', data: { mode: 'edit', academicYear: year },
    });
    ref.afterClosed().pipe(takeUntil(this._destroy$)).subscribe(result => {
      if (!result) return;
      this._service.update(year.id, result).pipe(takeUntil(this._destroy$)).subscribe({
        next: res => { if (res.success) { this._alertService.success('Academic year updated successfully'); this.loadAll(); } },
        error: err => this._alertService.error(err.error?.message || 'Failed to update academic year'),
      });
    });
  }

  setAsCurrent(year: AcademicYearDto): void {
    this._alertService.confirm({
      title: 'Set as Current',
      message: `Set "${year.name}" as the current academic year? This will unset any other current year.`,
      confirmText: 'Set as Current',
      onConfirm: () => {
        this._service.setAsCurrent(year.id).pipe(takeUntil(this._destroy$)).subscribe({
          next: res => { if (res.success) { this._alertService.success('Academic year set as current'); this.loadAll(); } },
          error: err => this._alertService.error(err.error?.message || 'Failed to set as current'),
        });
      },
    });
  }

  closeYear(year: AcademicYearDto): void {
    this._alertService.confirm({
      title: 'Close Academic Year',
      message: `Close "${year.name}"? This cannot be undone and the year will no longer be editable.`,
      confirmText: 'Close Year',
      onConfirm: () => {
        this._service.close(year.id).pipe(takeUntil(this._destroy$)).subscribe({
          next: res => { if (res.success) { this._alertService.success('Academic year closed successfully'); this.loadAll(); } },
          error: err => this._alertService.error(err.error?.message || 'Failed to close academic year'),
        });
      },
    });
  }

  removeAcademicYear(year: AcademicYearDto): void {
    this._alertService.confirm({
      title: 'Delete Academic Year',
      message: `Delete "${year.name}"? This action cannot be undone.`,
      confirmText: 'Delete',
      onConfirm: () => {
        this._service.delete(year.id).pipe(takeUntil(this._destroy$)).subscribe({
          next: res => {
            if (res.success) {
              this._alertService.success('Academic year deleted successfully');
              if (this.paginatedData.length === 1 && this.currentPage > 1) this.currentPage--;
              this.loadAll();
            }
          },
          error: err => this._alertService.error(err.error?.message || 'Failed to delete academic year'),
        });
      },
    });
  }
}