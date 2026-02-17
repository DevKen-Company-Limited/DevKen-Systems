import {
  Component,
  OnInit,
  OnDestroy,
  AfterViewInit,
  ViewChild,
  TemplateRef,
  inject,
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Subject, of } from 'rxjs';
import { takeUntil, take, catchError } from 'rxjs/operators';

import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { SchoolDto, SchoolType, SchoolCategory } from './types/school';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { API_BASE_URL } from 'app/app.config';
import { AuthService } from 'app/core/auth/auth.service';
import { CreateEditSchoolDialogComponent } from 'app/dialog-modals/Tenant/create-edit-school-dialog.component';
import { SchoolViewDialogComponent } from 'app/dialog-modals/Student/school-view-dialog.component';

// ── Reusable shared components ──────────────────────────────────────────────
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';
import {
  DataTableComponent,
  TableColumn,
  TableAction,
  TableHeader,
  TableEmptyState,
} from 'app/shared/data-table/data-table.component';

// ── Logo cache entry — mirrors PhotoCacheEntry from teachers ───────────────
export interface LogoCacheEntry {
  url:       SafeUrl;
  blobUrl:   string;
  isLoading: boolean;
  error:     boolean;
}

@Component({
  selector: 'app-schools-management',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatButtonModule,
    MatDialogModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    PageHeaderComponent,
    FilterPanelComponent,
    PaginationComponent,
    StatsCardsComponent,
    DataTableComponent,
  ],
  templateUrl: './schools-management.component.html',
  providers: [DatePipe],
})
export class SchoolsManagementComponent implements OnInit, AfterViewInit, OnDestroy {

  // ── Cell template refs ────────────────────────────────────────────────────
  @ViewChild('schoolCell')  schoolCellTemplate!:  TemplateRef<any>;
  @ViewChild('typeCell')    typeCellTemplate!:    TemplateRef<any>;
  @ViewChild('statusCell')  statusCellTemplate!:  TemplateRef<any>;
  @ViewChild('createdCell') createdCellTemplate!: TemplateRef<any>;

  private _baseUrl     = inject(API_BASE_URL);
  private _sanitizer   = inject(DomSanitizer);
  private _http        = inject(HttpClient);
  private _authService = inject(AuthService);
  private _unsubscribe = new Subject<void>();

  // ── Breadcrumbs ───────────────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Administration',    url: '/admin' },
    { label: 'Tenant Management', url: '/admin/tenants' },
    { label: 'Schools' },
  ];

  // ── Label maps ────────────────────────────────────────────────────────────
  readonly schoolTypeLabels: Record<number, string> = {
    [SchoolType.Public]:        'Public School',
    [SchoolType.Private]:       'Private School',
    [SchoolType.International]: 'International School',
    [SchoolType.NGO]:           'NGO / Mission School',
  };

  readonly categoryLabels: Record<number, string> = {
    [SchoolCategory.Day]:      'Day School',
    [SchoolCategory.Boarding]: 'Boarding School',
    [SchoolCategory.Mixed]:    'Mixed (Day & Boarding)',
  };

  // ── Stats Cards ───────────────────────────────────────────────────────────
  get statsCards(): StatCard[] {
    return [
      {
        label:     'Total Schools',
        value:     this.allData.length,
        icon:      'school',
        iconColor: 'indigo',
      },
      {
        label:     'Active',
        value:     this.allData.filter(s => s.isActive).length,
        icon:      'check_circle',
        iconColor: 'green',
      },
      {
        label:     'Inactive',
        value:     this.allData.filter(s => !s.isActive).length,
        icon:      'block',
        iconColor: 'red',
      },
      {
        label:     'Public Schools',
        value:     this.allData.filter(s => s.schoolType === SchoolType.Public).length,
        icon:      'account_balance',
        iconColor: 'blue',
      },
    ];
  }

  // ── Table columns ─────────────────────────────────────────────────────────
  readonly tableColumns: TableColumn<SchoolDto>[] = [
    { id: 'school',  label: 'School',  align: 'left',   sortable: true },
    { id: 'type',    label: 'Type',    align: 'left',   hideOnMobile: true },
    { id: 'status',  label: 'Status',  align: 'center' },
    { id: 'created', label: 'Created', align: 'left',   hideOnTablet: true },
  ];

  tableActions: TableAction<SchoolDto>[] = [
    {
      id:      'view',
      label:   'View Details',
      icon:    'visibility',
      color:   'indigo',
      handler: (school) => this.openView(school),
    },
    {
      id:      'edit',
      label:   'Edit School',
      icon:    'edit',
      color:   'blue',
      handler: (school) => this.openEdit(school),
    },
    {
      id:      'deactivate',
      label:   'Deactivate',
      icon:    'block',
      color:   'amber',
      handler: (school) => this.toggleSchoolStatus(school),
      visible: (school) => school.isActive,
    },
    {
      id:      'activate',
      label:   'Activate',
      icon:    'check_circle',
      color:   'green',
      handler: (school) => this.toggleSchoolStatus(school),
      visible: (school) => !school.isActive,
      divider: true,
    },
    {
      id:      'delete',
      label:   'Delete School',
      icon:    'delete',
      color:   'red',
      handler: (school) => this.removeSchool(school),
    },
  ];

  tableHeader: TableHeader = {
    title:        'All Schools',
    subtitle:     '',
    icon:         'table_chart',
    iconGradient: 'bg-gradient-to-br from-indigo-500 via-violet-600 to-purple-700',
  };

  tableEmptyState: TableEmptyState = {
    icon:        'school',
    message:     'No Schools Found',
    description: 'Try adjusting your filters or create your first school.',
    action: {
      label:   'Create First School',
      icon:    'add',
      handler: () => this.openCreate(),
    },
  };

  cellTemplates: { [key: string]: TemplateRef<any> } = {};

  // ── Filter ────────────────────────────────────────────────────────────────
  filterFields:   FilterField[] = [];
  showFilterPanel = false;

  private _filterValues = { search: '', status: 'all', type: 'all' };

  // ── State ─────────────────────────────────────────────────────────────────
  allData:  SchoolDto[] = [];
  isLoading = false;

  // ── Logo cache — keyed by school id, same shape as teachers' photoCache ───
  logoCache: { [id: string]: LogoCacheEntry } = {};

  // ── Pagination ────────────────────────────────────────────────────────────
  currentPage  = 1;
  itemsPerPage = 10;

  // ── Derived data ──────────────────────────────────────────────────────────
  get filteredData(): SchoolDto[] {
    return this.allData.filter(s => {
      const q = this._filterValues.search.toLowerCase();
      const searchMatch =
        !q ||
        s.name.toLowerCase().includes(q) ||
        (s.email       ?? '').toLowerCase().includes(q) ||
        (s.phoneNumber ?? '').toLowerCase().includes(q) ||
        (s.slugName    ?? '').toLowerCase().includes(q);

      const statusMatch =
        this._filterValues.status === 'all' ||
        (this._filterValues.status === 'active'   &&  s.isActive) ||
        (this._filterValues.status === 'inactive' && !s.isActive);

      const typeMatch =
        this._filterValues.type === 'all' ||
        String(s.schoolType) === this._filterValues.type;

      return searchMatch && statusMatch && typeMatch;
    });
  }

  get paginatedData(): SchoolDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  constructor(
    private _service:      SchoolService,
    private _dialog:       MatDialog,
    private _alertService: AlertService,
    private _datePipe:     DatePipe,
  ) {}

  ngOnInit(): void {
    this._initFilterFields();
    this.loadData();
  }

  ngAfterViewInit(): void {
    this.cellTemplates = {
      school:  this.schoolCellTemplate,
      type:    this.typeCellTemplate,
      status:  this.statusCellTemplate,
      created: this.createdCellTemplate,
    };
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
    // Revoke all blob URLs to prevent memory leaks
    Object.values(this.logoCache).forEach(e => { if (e?.blobUrl) URL.revokeObjectURL(e.blobUrl); });
  }

  // ── Filter setup ──────────────────────────────────────────────────────────
  private _initFilterFields(): void {
    this.filterFields = [
      {
        id: 'search', label: 'Search', type: 'text',
        placeholder: 'Name, email, phone or slug…', value: '',
      },
      {
        id: 'status', label: 'Status', type: 'select', value: 'all',
        options: [
          { label: 'All Statuses', value: 'all' },
          { label: 'Active',       value: 'active' },
          { label: 'Inactive',     value: 'inactive' },
        ],
      },
      {
        id: 'type', label: 'School Type', type: 'select', value: 'all',
        options: [
          { label: 'All Types',            value: 'all' },
          { label: 'Public School',        value: String(SchoolType.Public) },
          { label: 'Private School',       value: String(SchoolType.Private) },
          { label: 'International School', value: String(SchoolType.International) },
          { label: 'NGO / Mission School', value: String(SchoolType.NGO) },
        ],
      },
    ];
  }

  // ── Data loading ──────────────────────────────────────────────────────────
  loadData(): void {
    this.isLoading = true;
    this._service.getAll().pipe(takeUntil(this._unsubscribe)).subscribe({
      next: (response) => {
        if (response?.success && response.data) {
          this.allData = response.data;
          this.tableHeader.subtitle = `${this.filteredData.length} schools found`;
          setTimeout(() => this._preloadVisibleLogos(), 0);
        } else {
          this._alertService.error(response?.message || 'Failed to load schools', 'Load Error');
        }
        this.isLoading = false;
      },
      error: () => {
        this._alertService.error('An unexpected error occurred while loading schools.', 'Load Error');
        this.isLoading = false;
      },
    });
  }

  // ── Logo loading (identical pattern to teachers' _loadTeacherPhoto) ───────

  /**
   * Resolves a relative logo path to an absolute URL.
   */
  private _resolveLogoUrl(logoUrl: string): string {
    if (logoUrl.startsWith('http://') || logoUrl.startsWith('https://')) return logoUrl;
    const base = this._baseUrl.replace(/\/$/, '');
    const path = logoUrl.startsWith('/') ? logoUrl : `/${logoUrl}`;
    return `${base}${path}`;
  }

  /**
   * Fetches a school logo as an authenticated blob and stores
   * a SafeUrl in logoCache — mirrors loadTeacherPhoto() exactly.
   */
  private _loadLogo(schoolId: string, logoUrl: string): void {
    // Initialise or reset the cache entry to loading state
    if (!this.logoCache[schoolId]) {
      this.logoCache[schoolId] = { url: null!, blobUrl: '', isLoading: true, error: false };
    } else {
      this.logoCache[schoolId].isLoading = true;
      this.logoCache[schoolId].error     = false;
    }

    const resolved = this._resolveLogoUrl(logoUrl);
    const token    = this._authService.accessToken;
    const headers  = token ? new HttpHeaders().set('Authorization', `Bearer ${token}`) : undefined;
    const fetchUrl = token ? `${resolved}?token=${token}` : resolved;

    this._http.get(fetchUrl, { responseType: 'blob', headers })
      .pipe(
        takeUntil(this._unsubscribe),
        catchError(err => {
          console.error(`Failed to load logo for school ${schoolId}:`, err);
          this.logoCache[schoolId] = { ...this.logoCache[schoolId], isLoading: false, error: true };
          return of(null);
        }),
      )
      .subscribe(blob => {
        if (!blob) return;

        // Revoke stale blob URL before replacing
        if (this.logoCache[schoolId]?.blobUrl) {
          URL.revokeObjectURL(this.logoCache[schoolId].blobUrl);
        }

        const blobUrl = URL.createObjectURL(blob);
        const safeUrl = this._sanitizer.bypassSecurityTrustUrl(blobUrl);

        this.logoCache[schoolId] = { url: safeUrl, blobUrl, isLoading: false, error: false };
      });
  }

  /** Queues logo loads for every school visible on the current page. */
  private _preloadVisibleLogos(): void {
    this.paginatedData.forEach(school => {
      if (school.logoUrl && !this.logoCache[school.id]?.url && !this.logoCache[school.id]?.isLoading) {
        this._loadLogo(school.id, school.logoUrl);
      }
    });
  }

  /** Clears cached entry and re-fetches (used after a logo update). */
  refreshLogo(schoolId: string, logoUrl: string | null): void {
    if (this.logoCache[schoolId]?.blobUrl) URL.revokeObjectURL(this.logoCache[schoolId].blobUrl);
    delete this.logoCache[schoolId];
    if (logoUrl) this._loadLogo(schoolId, logoUrl);
  }

  // ── Display helpers ───────────────────────────────────────────────────────
  getSchoolTypeLabel(schoolType: number | string | null | undefined): string {
    if (schoolType == null) return '—';
    const key = typeof schoolType === 'string' ? parseInt(schoolType, 10) : schoolType;
    return this.schoolTypeLabels[key] ?? `Type ${schoolType}`;
  }

  getCategoryLabel(category: number | string | null | undefined): string {
    if (category == null) return '—';
    const key = typeof category === 'string' ? parseInt(category, 10) : category;
    return this.categoryLabels[key] ?? `Category ${category}`;
  }

  formatDate(dateString: string): string {
    if (!dateString) return '—';
    return this._datePipe.transform(dateString, 'MMM d, y') || '—';
  }

  getInitials(name: string): string {
    return name.split(' ').slice(0, 2).map(w => w[0]).join('').toUpperCase();
  }

  // ── Filter handlers ───────────────────────────────────────────────────────
  toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

  onFilterChange(event: FilterChangeEvent): void {
    (this._filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} schools found`;
    setTimeout(() => this._preloadVisibleLogos(), 0);
  }

  onClearFilters(): void {
    this._filterValues = { search: '', status: 'all', type: 'all' };
    this.filterFields.forEach(f => (f.value = (this._filterValues as any)[f.id]));
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} schools found`;
    setTimeout(() => this._preloadVisibleLogos(), 0);
  }

  // ── Pagination handlers ───────────────────────────────────────────────────
  onPageChange(page: number): void {
    this.currentPage = page;
    setTimeout(() => this._preloadVisibleLogos(), 0);
  }

  onItemsPerPageChange(n: number): void {
    this.itemsPerPage = n;
    this.currentPage  = 1;
    setTimeout(() => this._preloadVisibleLogos(), 0);
  }

  // ── CRUD ──────────────────────────────────────────────────────────────────
  openView(school: SchoolDto): void {
    const ref = this._dialog.open(SchoolViewDialogComponent, { data: { school } });
    ref.afterClosed().pipe(take(1)).subscribe(result => {
      if (result?.action === 'edit') this.openEdit(result.school);
    });
  }

  openCreate(): void {
    const ref = this._dialog.open(CreateEditSchoolDialogComponent, { data: { mode: 'create' } });
    ref.afterClosed().pipe(take(1)).subscribe(result => {
      if (result?.success) {
        this.loadData();
        this.loadData();
      }
    });
  }

  openEdit(school: SchoolDto): void {
    const ref = this._dialog.open(CreateEditSchoolDialogComponent, {
      data: { mode: 'edit', school },
    });
    ref.afterClosed().pipe(take(1)).subscribe(result => {
      if (result?.success) {
        // Refresh cached logo if it may have changed
        this.refreshLogo(school.id, result.school?.logoUrl ?? school.logoUrl ?? null);
        this.loadData();
           this.loadData();
      }
    });
  }

  removeSchool(school: SchoolDto): void {
    this._alertService.confirm({
      title: 'Delete School',
      message: `Are you sure you want to delete "${school.name}"? This action cannot be undone.`,
      confirmText: 'Delete', cancelText: 'Cancel',
      onConfirm: () => {
        this.isLoading = true;
        this._service.delete(school.id).pipe(takeUntil(this._unsubscribe)).subscribe({
          next: (response) => {
            if (response?.success) {
              // Clean up blob URL before removing from cache
              if (this.logoCache[school.id]?.blobUrl) URL.revokeObjectURL(this.logoCache[school.id].blobUrl);
              delete this.logoCache[school.id];

              this.loadData();
              this._alertService.success(`"${school.name}" was deleted successfully.`, 'Deleted');
            } else {
              this._alertService.error(response?.message || 'Failed to delete school.', 'Error');
            }
            this.isLoading = false;
          },
          error: () => { this._alertService.error('An unexpected error occurred.', 'Error'); this.isLoading = false; },
        });
      },
    });
  }

  toggleSchoolStatus(school: SchoolDto): void {
    const newStatus = !school.isActive;
    const action    = newStatus ? 'activate' : 'deactivate';
    this._alertService.confirm({
      title:       newStatus ? 'Activate School' : 'Deactivate School',
      message:     `Are you sure you want to ${action} "${school.name}"?`,
      confirmText: newStatus ? 'Activate' : 'Deactivate',
      cancelText:  'Cancel',
      onConfirm:   () => {
        this.isLoading = true;
        this._service.updateStatus(school.id, newStatus).pipe(takeUntil(this._unsubscribe)).subscribe({
          next: (response) => {
            if (response?.success) {
              this.loadData();
              this._alertService.success(`School ${action}d successfully.`, 'Status Updated');
            } else {
              this._alertService.error(response?.message || `Failed to ${action} school.`, 'Error');
            }
            this.isLoading = false;
          },
          error: () => {
            this._alertService.error(`An error occurred while trying to ${action} the school.`, 'Error');
            this.isLoading = false;
          },
        });
      },
    });
  }
}