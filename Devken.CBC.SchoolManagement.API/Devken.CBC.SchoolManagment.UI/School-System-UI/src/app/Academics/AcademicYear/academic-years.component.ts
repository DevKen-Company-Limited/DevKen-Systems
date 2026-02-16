import { Component, OnInit, OnDestroy, ViewChild, TemplateRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';

import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AcademicYearService } from 'app/core/DevKenService/AcademicYearService/AcademicYearService';
import { CreateEditAcademicYearDialogComponent } from 'app/dialog-modals/Academic Year/create-edit-academic-year-dialog.component';
import { AcademicYearDto } from './Types/AcademicYear';

// Import reusable components
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';
import { 
  DataTableComponent, 
  TableColumn, 
  TableAction, 
  TableHeader, 
  TableEmptyState 
} from 'app/shared/data-table/data-table.component';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';

@Component({
  selector: 'app-academic-years',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatButtonModule,
    MatDialogModule,
    MatMenuModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    // Reusable components
    PageHeaderComponent,
    FilterPanelComponent,
    PaginationComponent,
    StatsCardsComponent,
    DataTableComponent,
  ],
  templateUrl: './academic-years.component.html',
})
export class AcademicYearsComponent implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild('codeCell') codeCellTemplate!: TemplateRef<any>;
  @ViewChild('nameCell') nameCellTemplate!: TemplateRef<any>;
  @ViewChild('startDateCell') startDateCellTemplate!: TemplateRef<any>;
  @ViewChild('endDateCell') endDateCellTemplate!: TemplateRef<any>;
  @ViewChild('statusCell') statusCellTemplate!: TemplateRef<any>;

  private _unsubscribe = new Subject<void>();

  // ── Breadcrumbs ──────────────────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Settings', url: '/settings' },
    { label: 'Academic Years' }
  ];

  // ── Stats Cards Configuration ────────────────────────────────────────────────
  get statsCards(): StatCard[] {
    return [
      {
        label: 'Total Years',
        value: this.total,
        icon: 'calendar_today',
        iconColor: 'pink',
      },
      {
        label: 'Current Year',
        value: this.currentYearName,
        icon: 'today',
        iconColor: 'green',
        //isString: true,
      },
      {
        label: 'Open Years',
        value: this.openYearsCount,
        icon: 'lock_open',
        iconColor: 'blue',
      },
    ];
  }

  // ── Table Configuration ──────────────────────────────────────────────────────
  get tableColumns(): TableColumn<AcademicYearDto>[] {
    return [
      {
        id: 'code',
        label: 'Code',
        align: 'left',
        sortable: true,
      },
      {
        id: 'name',
        label: 'Name',
        align: 'left',
        sortable: true,
      },
      {
        id: 'startDate',
        label: 'Start Date',
        align: 'left',
        hideOnMobile: true,
      },
      {
        id: 'endDate',
        label: 'End Date',
        align: 'left',
        hideOnMobile: true,
      },
      {
        id: 'status',
        label: 'Status',
        align: 'center',
      },
    ];
  }

  tableActions: TableAction<AcademicYearDto>[] = [
    {
      id: 'edit',
      label: 'Edit',
      icon: 'edit',
      color: 'blue',
      handler: (year) => this.openEdit(year),
      disabled: (year) => year.isClosed,
    },
    {
      id: 'setCurrent',
      label: 'Set as Current',
      icon: 'check_circle',
      color: 'green',
      handler: (year) => this.setAsCurrent(year),
      visible: (year) => !year.isCurrent && !year.isClosed,
    },
    {
      id: 'closeYear',
      label: 'Close Year',
      icon: 'lock',
      color: 'amber',
      handler: (year) => this.closeYear(year),
      visible: (year) => !year.isClosed,
      divider: true,
    },
    {
      id: 'delete',
      label: 'Delete',
      icon: 'delete',
      color: 'red',
      handler: (year) => this.removeAcademicYear(year),
    },
  ];

  tableHeader: TableHeader = {
    title: 'Academic Years List',
    subtitle: '',
    icon: 'table_chart',
    iconGradient: 'bg-gradient-to-br from-blue-500 via-cyan-600 to-teal-700',
  };

  tableEmptyState: TableEmptyState = {
    icon: 'search_off',
    message: 'No academic years found',
    description: 'Try adjusting your filters or create a new academic year',
    action: {
      label: 'Create Academic Year',
      icon: 'add',
      handler: () => this.openCreate(),
    },
  };

  cellTemplates: { [key: string]: TemplateRef<any> } = {};

  // ── Filter Fields Configuration ──────────────────────────────────────────────
  filterFields: FilterField[] = [];
  showFilterPanel = false;

  // ── State ────────────────────────────────────────────────────────────────────
  allData: AcademicYearDto[] = [];
  isLoading = false;
  availableYears: string[] = [];

  // ── Filter Values ────────────────────────────────────────────────────────────
  private _filterValues = {
    search: '',
    status: 'all',
    year: 'all',
  };

  // ── Pagination ───────────────────────────────────────────────────────────────
  currentPage = 1;
  itemsPerPage = 10;

  // ── Computed Stats ───────────────────────────────────────────────────────────
  get total(): number {
    return this.allData.length;
  }

  get currentYearName(): string {
    const current = this.allData.find(ay => ay.isCurrent);
    return current ? current.name : 'None';
  }

  get openYearsCount(): number {
    return this.allData.filter(ay => !ay.isClosed).length;
  }

  // ── Filtered Data ─────────────────────────────────────────────────────────────
  get filteredData(): AcademicYearDto[] {
    return this.allData.filter(ay => {
      const q = this._filterValues.search.toLowerCase();

      const matchesSearch = !q ||
        ay.name.toLowerCase().includes(q) ||
        ay.code.toLowerCase().includes(q);

      const matchesStatus =
        this._filterValues.status === 'all' ||
        (this._filterValues.status === 'current' && ay.isCurrent) ||
        (this._filterValues.status === 'closed' && ay.isClosed) ||
        (this._filterValues.status === 'open' && !ay.isClosed && !ay.isCurrent);

      let matchesYear = true;
      if (this._filterValues.year !== 'all') {
        const startYear = new Date(ay.startDate).getFullYear().toString();
        matchesYear = startYear === this._filterValues.year;
      }

      return matchesSearch && matchesStatus && matchesYear;
    });
  }

  // ── Pagination Helpers ────────────────────────────────────────────────────────
  get paginatedData(): AcademicYearDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  constructor(
    private _service: AcademicYearService,
    private _dialog: MatDialog,
    private _alertService: AlertService,
  ) {}

  ngOnInit(): void {
    this.initializeFilterFields();
    this.loadAll();
  }

  ngAfterViewInit(): void {
    this.cellTemplates = {
      code: this.codeCellTemplate,
      name: this.nameCellTemplate,
      startDate: this.startDateCellTemplate,
      endDate: this.endDateCellTemplate,
      status: this.statusCellTemplate,
    };
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  // ── Initialize Filter Fields ─────────────────────────────────────────────────
  private initializeFilterFields(): void {
    this.filterFields = [
      {
        id: 'search',
        label: 'Search',
        type: 'text',
        placeholder: 'Code or name...',
        value: this._filterValues.search,
      },
      {
        id: 'status',
        label: 'Status',
        type: 'select',
        value: this._filterValues.status,
        options: [
          { label: 'All Statuses', value: 'all' },
          { label: 'Current', value: 'current' },
          { label: 'Open', value: 'open' },
          { label: 'Closed', value: 'closed' },
        ],
      },
      {
        id: 'year',
        label: 'Year',
        type: 'select',
        value: this._filterValues.year,
        options: [
          { label: 'All Years', value: 'all' },
          ...this.availableYears.map(y => ({ label: y, value: y })),
        ],
      },
    ];
  }

  // ── Filter Handlers ──────────────────────────────────────────────────────────
  toggleFilterPanel(): void {
    this.showFilterPanel = !this.showFilterPanel;
  }

  onFilterChange(event: FilterChangeEvent): void {
    (this._filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} years found`;
  }

  onClearFilters(): void {
    this._filterValues = {
      search: '',
      status: 'all',
      year: 'all',
    };

    this.filterFields.forEach(field => {
      field.value = (this._filterValues as any)[field.id];
    });

    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} years found`;
  }

  // ── Pagination Handlers ──────────────────────────────────────────────────────
  onPageChange(page: number): void {
    this.currentPage = page;
  }

  onItemsPerPageChange(itemsPerPage: number): void {
    this.itemsPerPage = itemsPerPage;
    this.currentPage = 1;
  }

  // ── Data Loading ──────────────────────────────────────────────────────────────
  loadAll(): void {
    this.isLoading = true;
    this._service.getAll()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.allData = response.data;
            this.extractAvailableYears();
            this.updateYearFilterOptions();
            this.tableHeader.subtitle = `${this.filteredData.length} years found`;
          }
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Failed to load academic years', error);
          this._alertService.error('Failed to load academic years');
          this.isLoading = false;
        }
      });
  }

  private extractAvailableYears(): void {
    const years = new Set<string>();
    this.allData.forEach(ay => {
      const year = new Date(ay.startDate).getFullYear().toString();
      years.add(year);
    });
    this.availableYears = Array.from(years).sort((a, b) => b.localeCompare(a));
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

  // ── CRUD Operations ──────────────────────────────────────────────────────────
  openCreate(): void {
    const dialogRef = this._dialog.open(CreateEditAcademicYearDialogComponent, {
      width: '600px',
      data: { mode: 'create' }
    });

    dialogRef.afterClosed()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe((result) => {
        if (result) {
          this._service.create(result)
            .pipe(takeUntil(this._unsubscribe))
            .subscribe({
              next: (response) => {
                if (response.success) {
                  this._alertService.success('Academic year created successfully');
                  this.loadAll();
                }
              },
              error: (error) => {
                console.error('Failed to create academic year', error);
                this._alertService.error(error.error?.message || 'Failed to create academic year');
              }
            });
        }
      });
  }

  openEdit(academicYear: AcademicYearDto): void {
    if (academicYear.isClosed) {
      this._alertService.error('Cannot edit a closed academic year');
      return;
    }

    const dialogRef = this._dialog.open(CreateEditAcademicYearDialogComponent, {
      width: '600px',
      data: { mode: 'edit', academicYear }
    });

    dialogRef.afterClosed()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe((result) => {
        if (result) {
          this._service.update(academicYear.id, result)
            .pipe(takeUntil(this._unsubscribe))
            .subscribe({
              next: (response) => {
                if (response.success) {
                  this._alertService.success('Academic year updated successfully');
                  this.loadAll();
                }
              },
              error: (error) => {
                console.error('Failed to update academic year', error);
                this._alertService.error(error.error?.message || 'Failed to update academic year');
              }
            });
        }
      });
  }

  setAsCurrent(academicYear: AcademicYearDto): void {
    this._alertService.confirm({
      title: 'Set as Current',
      message: `Are you sure you want to set "${academicYear.name}" as the current academic year? This will unset any other current academic year.`,
      confirmText: 'Set as Current',
      cancelText: 'Cancel',
      onConfirm: () => {
        this._service.setAsCurrent(academicYear.id)
          .pipe(takeUntil(this._unsubscribe))
          .subscribe({
            next: (response) => {
              if (response.success) {
                this._alertService.success('Academic year set as current');
                this.loadAll();
              }
            },
            error: (error) => {
              console.error('Failed to set academic year as current', error);
              this._alertService.error(error.error?.message || 'Failed to set as current');
            }
          });
      }
    });
  }

  closeYear(academicYear: AcademicYearDto): void {
    this._alertService.confirm({
      title: 'Close Academic Year',
      message: `Are you sure you want to close "${academicYear.name}"? This action cannot be undone and the year will no longer be editable.`,
      confirmText: 'Close Year',
      cancelText: 'Cancel',
      onConfirm: () => {
        this._service.close(academicYear.id)
          .pipe(takeUntil(this._unsubscribe))
          .subscribe({
            next: (response) => {
              if (response.success) {
                this._alertService.success('Academic year closed successfully');
                this.loadAll();
              }
            },
            error: (error) => {
              console.error('Failed to close academic year', error);
              this._alertService.error(error.error?.message || 'Failed to close academic year');
            }
          });
      }
    });
  }

  removeAcademicYear(academicYear: AcademicYearDto): void {
    this._alertService.confirm({
      title: 'Delete Academic Year',
      message: `Are you sure you want to delete "${academicYear.name}"? This action cannot be undone.`,
      confirmText: 'Delete',
      cancelText: 'Cancel',
      onConfirm: () => {
        this._service.delete(academicYear.id)
          .pipe(takeUntil(this._unsubscribe))
          .subscribe({
            next: (response) => {
              if (response.success) {
                this._alertService.success('Academic year deleted successfully');
                
                if (this.paginatedData.length === 0 && this.currentPage > 1) {
                  this.currentPage--;
                }
                
                this.loadAll();
              }
            },
            error: (error) => {
              console.error('Failed to delete academic year', error);
              this._alertService.error(error.error?.message || 'Failed to delete academic year');
            }
          });
      }
    });
  }
}