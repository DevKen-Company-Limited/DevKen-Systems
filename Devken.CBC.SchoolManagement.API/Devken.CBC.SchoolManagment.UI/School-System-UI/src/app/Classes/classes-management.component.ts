import { Component, OnInit, OnDestroy, ViewChild, TemplateRef, inject, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, forkJoin, of } from 'rxjs';
import { catchError, takeUntil, finalize } from 'rxjs/operators';
import { ClassService } from 'app/core/DevKenService/ClassService';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { CreateEditClassDialogComponent } from 'app/dialog-modals/Classes/create-edit-class-dialog.component';
import { ClassDto } from './Types/Class';
import { SchoolDto } from 'app/Tenant/types/school';
import { AuthService } from 'app/core/auth/auth.service';

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

@Component({
  selector: 'app-classes-management',
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
    MatTooltipModule,
    // Reusable components
    PageHeaderComponent,
    FilterPanelComponent,
    PaginationComponent,
    StatsCardsComponent,
    DataTableComponent,
  ],
  templateUrl: './classes-management.component.html',
})
export class ClassesManagementComponent implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild('classCell') classCellTemplate!: TemplateRef<any>;
  @ViewChild('schoolCell') schoolCellTemplate!: TemplateRef<any>;
  @ViewChild('levelCell') levelCellTemplate!: TemplateRef<any>;
  @ViewChild('capacityCell') capacityCellTemplate!: TemplateRef<any>;
  @ViewChild('teacherCell') teacherCellTemplate!: TemplateRef<any>;
  @ViewChild('academicYearCell') academicYearCellTemplate!: TemplateRef<any>;
  @ViewChild('statusCell') statusCellTemplate!: TemplateRef<any>;

  private _unsubscribe = new Subject<void>();
  private _authService = inject(AuthService);
  private _schoolService = inject(SchoolService);
  private _alert = inject(AlertService);

  // ── Breadcrumbs ──────────────────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Settings', url: '/settings' },
    { label: 'Classes' }
  ];

  // ── SuperAdmin State ──────────────────────────────────────────────────────────
  get isSuperAdmin(): boolean {
    return this._authService.authUser?.isSuperAdmin ?? false;
  }

  schools: SchoolDto[] = [];

  get schoolsCount(): number {
    const uniqueSchools = new Set(this.allData.map(c => c.schoolId));
    return uniqueSchools.size;
  }

  // ── Stats Cards Configuration ────────────────────────────────────────────────
  get statsCards(): StatCard[] {
    const baseCards: StatCard[] = [
      {
        label: 'Total Classes',
        value: this.total,
        icon: 'school',
        iconColor: 'indigo',
      },
      {
        label: 'Active',
        value: this.activeCount,
        icon: 'check_circle',
        iconColor: 'green',
      },
      {
        label: 'Total Capacity',
        value: this.totalCapacity,
        icon: 'groups',
        iconColor: 'pink',
      },
      {
        label: 'Enrollment',
        value: this.totalEnrollment,
        icon: 'person',
        iconColor: 'violet',
      },
    ];

    if (this.isSuperAdmin) {
      baseCards.push({
        label: 'Schools',
        value: this.schoolsCount,
        icon: 'business',
        iconColor: 'blue',
      });
    }

    return baseCards;
  }

  // ── Table Configuration ──────────────────────────────────────────────────────
  get tableColumns(): TableColumn<ClassDto>[] {
    const baseColumns: TableColumn<ClassDto>[] = [
      {
        id: 'class',
        label: 'Class',
        align: 'left',
        sortable: true,
      },
    ];

    if (this.isSuperAdmin) {
      baseColumns.push({
        id: 'school',
        label: 'School',
        align: 'left',
        hideOnMobile: true,
      });
    }

    baseColumns.push(
      {
        id: 'level',
        label: 'Level',
        align: 'left',
        hideOnMobile: true,
      },
      {
        id: 'capacity',
        label: 'Capacity',
        align: 'left',
        hideOnTablet: true,
      },
      {
        id: 'teacher',
        label: 'Teacher',
        align: 'left',
        hideOnTablet: true,
      },
      {
        id: 'academicYear',
        label: 'Academic Year',
        align: 'left',
        hideOnTablet: true,
      },
      {
        id: 'status',
        label: 'Status',
        align: 'center',
      }
    );

    return baseColumns;
  }

  tableActions: TableAction<ClassDto>[] = [
    {
      id: 'edit',
      label: 'Edit',
      icon: 'edit',
      color: 'blue',
      handler: (classItem) => this.openEdit(classItem),
    },
    {
      id: 'viewDetails',
      label: 'View Details',
      icon: 'info',
      color: 'purple',
      handler: (classItem) => this.viewClassDetails(classItem),
      divider: true,
    },
    {
      id: 'delete',
      label: 'Delete',
      icon: 'delete',
      color: 'red',
      handler: (classItem) => this.removeClass(classItem),
      disabled: (classItem) => classItem.currentEnrollment > 0,
    },
  ];

  tableHeader: TableHeader = {
    title: 'Classes List',
    subtitle: '',
    icon: 'table_chart',
    iconGradient: 'bg-gradient-to-br from-blue-500 via-cyan-600 to-teal-700',
  };

  tableEmptyState: TableEmptyState = {
    icon: 'search_off',
    message: 'No classes found',
    description: 'Try adjusting your filters or create a new class',
    action: {
      label: 'Create Class',
      icon: 'add',
      handler: () => this.openCreate(),
    },
  };

  cellTemplates: { [key: string]: TemplateRef<any> } = {};

  // ── Filter Fields ─────────────────────────────────────────────────────────────
  filterFields: FilterField[] = [];
  showFilterPanel = false;

  // ── CBC Levels ───────────────────────────────────────────────────────────────
  cbcLevels: { value: number; label: string }[] = [];

  // ── State ────────────────────────────────────────────────────────────────────
  allData: ClassDto[] = [];
  isLoading = false;

  // ── Filter Values ────────────────────────────────────────────────────────────
  private _filterValues = {
    search: '',
    schoolId: 'all',
    level: 'all',
    status: 'all',
  };

  // ── Pagination ───────────────────────────────────────────────────────────────
  currentPage = 1;
  itemsPerPage = 20;

  // ── Computed Stats ───────────────────────────────────────────────────────────
  get total(): number { return this.allData.length; }
  get activeCount(): number { return this.allData.filter(c => c.isActive).length; }
  get totalCapacity(): number { return this.allData.reduce((sum, c) => sum + c.capacity, 0); }
  get totalEnrollment(): number { return this.allData.reduce((sum, c) => sum + c.currentEnrollment, 0); }

  // ── Filtered Data ─────────────────────────────────────────────────────────────
  get filteredData(): ClassDto[] {
    return this.allData.filter(c => {
      const q = this._filterValues.search.toLowerCase();
      return (
        (!q ||
          c.name?.toLowerCase().includes(q) ||
          c.code?.toLowerCase().includes(q) ||
          c.levelName?.toLowerCase().includes(q) ||
          c.teacherName?.toLowerCase().includes(q)) &&
        (this._filterValues.schoolId === 'all' || c.schoolId === this._filterValues.schoolId) &&
        (this._filterValues.level === 'all' || c.level.toString() === this._filterValues.level) &&
        (this._filterValues.status === 'all' ||
          (this._filterValues.status === 'active' && c.isActive) ||
          (this._filterValues.status === 'inactive' && !c.isActive) ||
          (this._filterValues.status === 'full' && c.isFull))
      );
    });
  }

  // ── Pagination Helpers ────────────────────────────────────────────────────────
  get paginatedData(): ClassDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  constructor(
    private _service: ClassService,
    private _dialog: MatDialog,
  ) {}

  ngOnInit(): void {
    this.cbcLevels = this._service.getAllCBCLevels();
    this.loadSchoolsAndInit();
  }

  ngAfterViewInit(): void {
    this.cellTemplates = {
      class: this.classCellTemplate,
      school: this.schoolCellTemplate,
      level: this.levelCellTemplate,
      capacity: this.capacityCellTemplate,
      teacher: this.teacherCellTemplate,
      academicYear: this.academicYearCellTemplate,
      status: this.statusCellTemplate,
    };
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  // ── Load Schools and Initialize ──────────────────────────────────────────────
  private loadSchoolsAndInit(): void {
    const requests: any = {};

    if (this.isSuperAdmin) {
      requests.schools = this._schoolService.getAll().pipe(
        catchError(err => {
          console.error('Failed to load schools:', err);
          return of({ success: false, message: '', data: [] });
        })
      );
    }

    if (Object.keys(requests).length > 0) {
      forkJoin(requests).pipe(
        takeUntil(this._unsubscribe),
        finalize(() => {
          this.initializeFilterFields();
          this.loadAll();
        })
      ).subscribe({
        next: (results: any) => {
          if (results.schools) {
            this.schools = results.schools.data || [];
          }
        },
        error: (error) => {
          console.error('Failed to load configuration:', error);
          this._alert.error('Failed to load configuration data');
        }
      });
    } else {
      this.initializeFilterFields();
      this.loadAll();
    }
  }

  // ── Initialize Filter Fields ─────────────────────────────────────────────────
  private initializeFilterFields(): void {
    this.filterFields = [
      {
        id: 'search',
        label: 'Search',
        type: 'text',
        placeholder: 'Name, code, or teacher...',
        value: this._filterValues.search,
      },
    ];

    if (this.isSuperAdmin) {
      this.filterFields.push({
        id: 'schoolId',
        label: 'School',
        type: 'select',
        value: this._filterValues.schoolId,
        options: [
          { label: 'All Schools', value: 'all' },
          ...this.schools.map(s => ({
            label: `${s.name}${s.slugName ? ' (' + s.slugName + ')' : ''}`,
            value: s.id
          })),
        ],
      });
    }

    this.filterFields.push(
      {
        id: 'level',
        label: 'Level',
        type: 'select',
        value: this._filterValues.level,
        options: [
          { label: 'All Levels', value: 'all' },
          ...this.cbcLevels.map(l => ({ label: l.label, value: l.value.toString() })),
        ],
      },
      {
        id: 'status',
        label: 'Status',
        type: 'select',
        value: this._filterValues.status,
        options: [
          { label: 'All Classes', value: 'all' },
          { label: 'Active Only', value: 'active' },
          { label: 'Inactive Only', value: 'inactive' },
          { label: 'Full Classes', value: 'full' },
        ],
      }
    );
  }

  // ── Filter Handlers ──────────────────────────────────────────────────────────
  toggleFilterPanel(): void {
    this.showFilterPanel = !this.showFilterPanel;
  }

  onFilterChange(event: FilterChangeEvent): void {
    (this._filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} classes found`;

    if (event.filterId === 'schoolId' && this.isSuperAdmin) {
      const schoolId = event.value === 'all' ? undefined : event.value;
      this.loadAll(schoolId);
    }
  }

  onClearFilters(): void {
    this._filterValues = {
      search: '',
      schoolId: 'all',
      level: 'all',
      status: 'all',
    };

    this.filterFields.forEach(field => {
      field.value = (this._filterValues as any)[field.id];
    });

    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} classes found`;
    this.loadAll();
  }

  // ── Pagination Handlers ──────────────────────────────────────────────────────
  onPageChange(page: number): void { this.currentPage = page; }
  onItemsPerPageChange(itemsPerPage: number): void {
    this.itemsPerPage = itemsPerPage;
    this.currentPage = 1;
  }

  // ── Data Loading ──────────────────────────────────────────────────────────────
  loadAll(schoolId?: string): void {
    this.isLoading = true;
    this._service.getAll(schoolId)
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next: res => {
          if (res?.success) {
            this.allData = res.data;
            this.tableHeader.subtitle = `${this.filteredData.length} classes found`;
          }
          this.isLoading = false;
        },
        error: (err) => {
          console.error('Failed to load classes:', err);
          this.isLoading = false;
          this._alert.error(err.error?.message || 'Failed to load classes');
        }
      });
  }

  // ── CRUD Operations ──────────────────────────────────────────────────────────
  openCreate(): void {
    const schoolId = this._filterValues.schoolId !== 'all' ? this._filterValues.schoolId : undefined;

    const dialogRef = this._dialog.open(CreateEditClassDialogComponent, {
      width: '800px',
      data: { mode: 'create', schoolId }
    });

    dialogRef.afterClosed()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe((result) => {
        if (result?.success) {
          this._alert.success(result.message || 'Class created successfully');
          this.loadAll();
        }
      });
  }

  openEdit(classItem: ClassDto): void {
    const dialogRef = this._dialog.open(CreateEditClassDialogComponent, {
      width: '800px',
      data: { mode: 'edit', class: classItem }
    });

    dialogRef.afterClosed()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe((result) => {
        if (result?.success) {
          this._alert.success(result.message || 'Class updated successfully');
          this.loadAll();
        }
      });
  }

  removeClass(classItem: ClassDto): void {
    if (classItem.currentEnrollment > 0) {
      this._alert.error(
        `Cannot delete "${classItem.name}" — it has ${classItem.currentEnrollment} enrolled students. Please reassign students first.`
      );
      return;
    }

    this._alert.confirm({
      title: 'Delete Class',
      message: `Are you sure you want to delete "${classItem.name}"? This action cannot be undone.`,
      confirmText: 'Delete',
      cancelText: 'Cancel',
      onConfirm: () => {
        this._service.delete(classItem.id)
          .pipe(takeUntil(this._unsubscribe))
          .subscribe({
            next: (res) => {
              if (res?.success) {
                this._alert.success('Class deleted successfully');
                if (this.paginatedData.length === 0 && this.currentPage > 1) {
                  this.currentPage--;
                }
                this.loadAll();
              }
            },
            error: (err) => {
              console.error('Failed to delete class:', err);
              this._alert.error(err.error?.message || 'Failed to delete class');
            }
          });
      },
    });
  }

  viewClassDetails(classItem: ClassDto): void {
    this._service.getById(classItem.id, true)
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next: (response) => {
          if (response?.success && response.data) {
            this._alert.info(`Viewing details for ${classItem.name}`);
          }
        },
        error: (error) => {
          console.error('Error loading class details:', error);
          this._alert.error('Failed to load class details');
        }
      });
  }

  // ── Helper Methods ───────────────────────────────────────────────────────────
  getSchoolName(schoolId: string): string {
    const school = this.schools.find(s => s.id === schoolId);
    return school ? school.name : 'Unknown School';
  }

  getSchoolSlug(schoolId: string): string {
    const school = this.schools.find(s => s.id === schoolId);
    return school ? school.slugName : '';
  }

  getCapacityUtilization(classItem: ClassDto): number {
    return this._service.getCapacityUtilization(classItem.currentEnrollment, classItem.capacity);
  }
}