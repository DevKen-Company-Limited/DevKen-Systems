// terms.component.ts
import { Component, OnInit, OnDestroy, ViewChild, TemplateRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { Observable, of, Subject, forkJoin } from 'rxjs';
import { catchError, takeUntil, finalize } from 'rxjs/operators';
import { AuthService } from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { SchoolDto } from 'app/Tenant/types/school';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';

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
import { AcademicYearService } from 'app/core/DevKenService/AcademicYearService/AcademicYearService';
import { TermService } from 'app/core/DevKenService/TermService/term.service';
import { CreateEditTermDialogComponent, CreateEditTermDialogResult } from 'app/dialog-modals/Terms/create-edit-term-dialog.component';
import { AcademicYearDto } from '../AcademicYear/Types/AcademicYear';
import { TermDto, CreateTermRequest, UpdateTermRequest, CloseTermRequest } from './Types/types';

@Component({
  selector: 'app-terms',
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
  templateUrl: './terms.component.html',
})
export class TermsComponent implements OnInit, OnDestroy {
  @ViewChild('termCell') termCellTemplate!: TemplateRef<any>;
  @ViewChild('datesCell') datesCellTemplate!: TemplateRef<any>;
  @ViewChild('academicYearCell') academicYearCellTemplate!: TemplateRef<any>;
  @ViewChild('statusCell') statusCellTemplate!: TemplateRef<any>;
  @ViewChild('schoolCell') schoolCellTemplate!: TemplateRef<any>;

  private _unsubscribe = new Subject<void>();
  private _authService = inject(AuthService);
  private _schoolService = inject(SchoolService);
  private _academicYearService = inject(AcademicYearService);
  private _alert = inject(AlertService);

  // ── Breadcrumbs ──────────────────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Academic', url: '/academic' },
    { label: 'Terms' }
  ];

  // ── SuperAdmin State ──────────────────────────────────────────────────────────
  get isSuperAdmin(): boolean {
    return this._authService.authUser?.isSuperAdmin ?? false;
  }

  schools: SchoolDto[] = [];
  academicYears: AcademicYearDto[] = [];
  
  get schoolsCount(): number {
    const uniqueSchools = new Set(this.allData.map(t => t.schoolId));
    return uniqueSchools.size;
  }

  // ── Stats Cards Configuration ────────────────────────────────────────────────
  get statsCards(): StatCard[] {
    const baseCards: StatCard[] = [
      {
        label: 'Total Terms',
        value: this.total,
        icon: 'event_note',
        iconColor: 'indigo',
      },
      {
        label: 'Current',
        value: this.currentCount,
        icon: 'radio_button_checked',
        iconColor: 'blue',
      },
      {
        label: 'Active',
        value: this.activeCount,
        icon: 'check_circle',
        iconColor: 'green',
      },
      {
        label: 'Closed',
        value: this.closedCount,
        icon: 'lock',
        iconColor: 'red',
      },
    ];

    if (this.isSuperAdmin) {
      baseCards.push({
        label: 'Schools',
        value: this.schoolsCount,
        icon: 'school',
        iconColor: 'violet',
      });
    }

    return baseCards;
  }

  // ── Table Configuration ──────────────────────────────────────────────────────
  get tableColumns(): TableColumn<TermDto>[] {
    const baseColumns: TableColumn<TermDto>[] = [
      {
        id: 'term',
        label: 'Term',
        align: 'left',
        sortable: true,
      },
      {
        id: 'academicYear',
        label: 'Academic Year',
        align: 'left',
        hideOnMobile: true,
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
        id: 'dates',
        label: 'Dates',
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

  tableActions: TableAction<TermDto>[] = [
    {
      id: 'edit',
      label: 'Edit',
      icon: 'edit',
      color: 'blue',
      handler: (term) => this.openEdit(term),
    },
    {
      id: 'setCurrent',
      label: 'Set as Current',
      icon: 'radio_button_checked',
      color: 'indigo',
      handler: (term) => this.setAsCurrent(term),
      visible: (term) => !term.isCurrent && !term.isClosed,
    },
    {
      id: 'close',
      label: 'Close Term',
      icon: 'lock',
      color: 'amber',
      handler: (term) => this.closeTerm(term),
      visible: (term) => !term.isClosed,
      divider: true,
    },
    {
      id: 'reopen',
      label: 'Reopen Term',
      icon: 'lock_open',
      color: 'green',
      handler: (term) => this.reopenTerm(term),
      visible: (term) => term.isClosed,
      divider: true,
    },
    {
      id: 'delete',
      label: 'Delete',
      icon: 'delete',
      color: 'red',
      handler: (term) => this.removeTerm(term),
    },
  ];

  tableHeader: TableHeader = {
    title: 'Terms List',
    subtitle: '',
    icon: 'table_chart',
    iconGradient: 'bg-gradient-to-br from-blue-500 via-indigo-600 to-violet-700',
  };

  tableEmptyState: TableEmptyState = {
    icon: 'event_note',
    message: 'No terms found',
    description: 'Try adjusting your filters or add a new term',
    action: {
      label: 'Add First Term',
      icon: 'add',
      handler: () => this.openCreate(),
    },
  };

  cellTemplates: { [key: string]: TemplateRef<any> } = {};

  // ── Filter Fields Configuration ──────────────────────────────────────────────
  filterFields: FilterField[] = [];
  showFilterPanel = false;

  // ── State ────────────────────────────────────────────────────────────────────
  allData: TermDto[] = [];
  isLoading = false;
  isDataLoading = true;

  // ── Filter Values ────────────────────────────────────────────────────────────
  private _filterValues = {
    search: '',
    status: 'all',
    academicYearId: 'all',
    schoolId: 'all',
    termNumber: 'all',
  };

  // ── Pagination ───────────────────────────────────────────────────────────────
  currentPage = 1;
  itemsPerPage = 10;

  // ── Computed Stats ───────────────────────────────────────────────────────────
  get total(): number { 
    return this.allData.length; 
  }
  
  get currentCount(): number { 
    return this.allData.filter(t => t.isCurrent).length; 
  }
  
  get activeCount(): number { 
    return this.allData.filter(t => t.isActive).length; 
  }
  
  get closedCount(): number { 
    return this.allData.filter(t => t.isClosed).length; 
  }

  // ── Filtered Data ─────────────────────────────────────────────────────────────
  get filteredData(): TermDto[] {
    return this.allData.filter(t => {
      const q = this._filterValues.search.toLowerCase();
      
      return (
        (!q || t.name?.toLowerCase().includes(q) || 
               t.academicYearName?.toLowerCase().includes(q)) &&
        (this._filterValues.status === 'all' ||
          (this._filterValues.status === 'current' && t.isCurrent) ||
          (this._filterValues.status === 'active' && t.isActive && !t.isClosed) ||
          (this._filterValues.status === 'closed' && t.isClosed)) &&
        (this._filterValues.academicYearId === 'all' || 
          t.academicYearId === this._filterValues.academicYearId) &&
        (this._filterValues.schoolId === 'all' || 
          t.schoolId === this._filterValues.schoolId) &&
        (this._filterValues.termNumber === 'all' || 
          t.termNumber === Number(this._filterValues.termNumber))
      );
    });
  }

  // ── Pagination Helpers ────────────────────────────────────────────────────────
  get paginatedData(): TermDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  constructor(
    private _service: TermService,
    private _dialog: MatDialog,
  ) {}

  ngOnInit(): void {
    this.loadDataAndInit();
  }

  ngAfterViewInit(): void {
    this.cellTemplates = {
      term: this.termCellTemplate,
      academicYear: this.academicYearCellTemplate,
      school: this.schoolCellTemplate,
      dates: this.datesCellTemplate,
      status: this.statusCellTemplate,
    };
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  // ── Data Loading and Initialization ──────────────────────────────────────────
  private loadDataAndInit(): void {
    this.isDataLoading = true;
    
    const requests: any = {
      academicYears: this._academicYearService.getAll().pipe(
        catchError(err => {
          console.error('Failed to load academic years:', err);
          return of({ success: false, message: '', data: [] });
        })
      ),
    };

    if (this.isSuperAdmin) {
      requests.schools = this._schoolService.getAll().pipe(
        catchError(err => {
          console.error('Failed to load schools:', err);
          return of({ success: false, message: '', data: [] });
        })
      );
    }

    forkJoin(requests).pipe(
      takeUntil(this._unsubscribe),
      finalize(() => {
        this.isDataLoading = false;
      })
    ).subscribe({
      next: (results: any) => {
        this.academicYears = results.academicYears.data || [];
        
        if (results.schools) {
          this.schools = results.schools.data || [];
        }

        this.initializeFilterFields();
        this.loadAll();
      },
      error: (error) => {
        console.error('Failed to load data:', error);
        this._alert.error('Failed to load configuration data');
        this.isDataLoading = false;
        this.loadAll();
      }
    });
  }

  // ── Initialize Filter Fields ─────────────────────────────────────────────────
  private initializeFilterFields(): void {
    this.filterFields = [
      {
        id: 'search',
        label: 'Search',
        type: 'text',
        placeholder: 'Term name or academic year...',
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
            label: `${s.name}${s.phone ? ' (' + s.phone + ')' : ''}`, 
            value: s.id 
          })),
        ],
      });
    }

    this.filterFields.push(
      {
        id: 'status',
        label: 'Status',
        type: 'select',
        value: this._filterValues.status,
        options: [
          { label: 'All Statuses', value: 'all' },
          { label: 'Current', value: 'current' },
          { label: 'Active', value: 'active' },
          { label: 'Closed', value: 'closed' },
        ],
      },
      {
        id: 'academicYearId',
        label: 'Academic Year',
        type: 'select',
        value: this._filterValues.academicYearId,
        options: [
          { label: 'All Years', value: 'all' },
          ...this.academicYears.map(ay => ({ 
            label: `${ay.name} (${ay.code})`, 
            value: ay.id 
          })),
        ],
      },
      {
        id: 'termNumber',
        label: 'Term Number',
        type: 'select',
        value: this._filterValues.termNumber,
        options: [
          { label: 'All Terms', value: 'all' },
          { label: 'Term 1', value: '1' },
          { label: 'Term 2', value: '2' },
          { label: 'Term 3', value: '3' },
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
    this.tableHeader.subtitle = `${this.filteredData.length} terms found`;
    
    if (event.filterId === 'schoolId' && this.isSuperAdmin) {
      const schoolId = event.value === 'all' ? null : event.value;
      this.loadAll(schoolId);
    }
  }

  onClearFilters(): void {
    this._filterValues = {
      search: '',
      status: 'all',
      academicYearId: 'all',
      schoolId: 'all',
      termNumber: 'all',
    };
    
    this.filterFields.forEach(field => {
      field.value = (this._filterValues as any)[field.id];
    });
    
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} terms found`;
    this.loadAll();
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
  loadAll(schoolId?: string | null): void {
    this.isLoading = true;
    this._service.getAll(schoolId || undefined)
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next: res => {
          if (res.success) {
            this.allData = res.data;
            this.tableHeader.subtitle = `${this.filteredData.length} terms found`;
          }
          this.isLoading = false;
        },
        error: (err) => {
          console.error('Failed to load terms:', err);
          this.isLoading = false;
          this._alert.error(err.error?.message || 'Failed to load terms');
        }
      });
  }

  // ── CRUD Operations ──────────────────────────────────────────────────────────
  openCreate(): void {
    const dialogRef = this._dialog.open(CreateEditTermDialogComponent, {
      panelClass: ['term-dialog', 'no-padding-dialog'],
      width: '700px',
      maxWidth: '95vw',
      maxHeight: '95vh',
      disableClose: true,
      autoFocus: 'input',
      data: { mode: 'create' },
    });

    dialogRef.afterClosed()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe((result: CreateEditTermDialogResult | null) => {
        if (!result) return;

        const request: CreateTermRequest = result.formData;

        this._service.create(request)
          .pipe(takeUntil(this._unsubscribe))
          .subscribe({
            next: (res) => {
              if (res.success) {
                this._alert.success('Term created successfully');
                this.loadAll();
              }
            },
            error: (err) => {
              console.error('Failed to create term:', err);
              this._alert.error(err.error?.message || 'Failed to create term');
            }
          });
      });
  }

  openEdit(term: TermDto): void {
    const dialogRef = this._dialog.open(CreateEditTermDialogComponent, {
      panelClass: ['term-dialog', 'no-padding-dialog'],
      width: '700px',
      maxWidth: '95vw',
      maxHeight: '95vh',
      disableClose: true,
      autoFocus: 'input',
      data: { mode: 'edit', term },
    });

    dialogRef.afterClosed()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe((result: CreateEditTermDialogResult | null) => {
        if (!result) return;

        const request: UpdateTermRequest = result.formData;

        this._service.update(term.id, request)
          .pipe(takeUntil(this._unsubscribe))
          .subscribe({
            next: (res) => {
              if (res.success) {
                this._alert.success('Term updated successfully');
                this.loadAll();
              }
            },
            error: (err) => {
              console.error('Failed to update term:', err);
              this._alert.error(err.error?.message || 'Failed to update term');
            }
          });
      });
  }

  setAsCurrent(term: TermDto): void {
    this._alert.confirm({
      title: 'Set as Current Term',
      message: `Are you sure you want to set "${term.name}" as the current term? This will unset any other current terms.`,
      confirmText: 'Set as Current',
      cancelText: 'Cancel',
      onConfirm: () => {
        this._service.setCurrent(term.id)
          .pipe(takeUntil(this._unsubscribe))
          .subscribe({
            next: (res) => {
              if (res.success) {
                this._alert.success('Term set as current successfully');
                this.loadAll();
              }
            },
            error: (err) => {
              console.error('Failed to set term as current:', err);
              this._alert.error(err.error?.message || 'Failed to set term as current');
            }
          });
      },
    });
  }

  closeTerm(term: TermDto): void {
    this._alert.confirm({
      title: 'Close Term',
      message: `Are you sure you want to close "${term.name}"? This action will prevent further modifications.`,
      confirmText: 'Close Term',
      cancelText: 'Cancel',
      onConfirm: () => {
        const request: CloseTermRequest = {
          termId: term.id,
          remarks: 'Term closed by user',
        };

        this._service.close(term.id, request)
          .pipe(takeUntil(this._unsubscribe))
          .subscribe({
            next: (res) => {
              if (res.success) {
                this._alert.success('Term closed successfully');
                this.loadAll();
              }
            },
            error: (err) => {
              console.error('Failed to close term:', err);
              this._alert.error(err.error?.message || 'Failed to close term');
            }
          });
      },
    });
  }

  reopenTerm(term: TermDto): void {
    this._alert.confirm({
      title: 'Reopen Term',
      message: `Are you sure you want to reopen "${term.name}"? This will allow modifications again.`,
      confirmText: 'Reopen Term',
      cancelText: 'Cancel',
      onConfirm: () => {
        this._service.reopen(term.id)
          .pipe(takeUntil(this._unsubscribe))
          .subscribe({
            next: (res) => {
              if (res.success) {
                this._alert.success('Term reopened successfully');
                this.loadAll();
              }
            },
            error: (err) => {
              console.error('Failed to reopen term:', err);
              this._alert.error(err.error?.message || 'Failed to reopen term');
            }
          });
      },
    });
  }

  removeTerm(term: TermDto): void {
    this._alert.confirm({
      title: 'Delete Term',
      message: `Are you sure you want to delete "${term.name}"? This action cannot be undone.`,
      confirmText: 'Delete',
      cancelText: 'Cancel',
      onConfirm: () => {
        this._service.delete(term.id)
          .pipe(takeUntil(this._unsubscribe))
          .subscribe({
            next: (res) => {
              if (res.success) {
                this._alert.success('Term deleted successfully');
                
                if (this.paginatedData.length === 0 && this.currentPage > 1) {
                  this.currentPage--;
                }
                
                this.loadAll();
              }
            },
            error: (err) => {
              console.error('Failed to delete term:', err);
              this._alert.error(err.error?.message || 'Failed to delete term');
            }
          });
      },
    });
  }

  // ── Helper Methods ───────────────────────────────────────────────────────────
  getStatusColor(status: string): string {
    switch (status.toLowerCase()) {
      case 'current': return 'blue';
      case 'active': return 'green';
      case 'closed': return 'red';
      case 'upcoming': return 'violet';
      case 'past': return 'gray';
      default: return 'gray';
    }
  }

  getStatusIcon(status: string): string {
    switch (status.toLowerCase()) {
      case 'current': return 'radio_button_checked';
      case 'active': return 'check_circle';
      case 'closed': return 'lock';
      case 'upcoming': return 'schedule';
      case 'past': return 'history';
      default: return 'info';
    }
  }

  formatDate(dateString: string): string {
    if (!dateString) return '—';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { 
      year: 'numeric', 
      month: 'short', 
      day: 'numeric' 
    });
  }
}