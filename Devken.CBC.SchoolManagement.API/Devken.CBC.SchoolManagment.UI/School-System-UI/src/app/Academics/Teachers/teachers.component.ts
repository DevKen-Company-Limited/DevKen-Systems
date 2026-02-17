import { Component, ElementRef, OnInit, OnDestroy, ViewChild, TemplateRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { FuseConfirmationService } from '@fuse/services/confirmation';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService'; 
import { Observable, of, Subject, forkJoin } from 'rxjs';
import { catchError, takeUntil, map, finalize } from 'rxjs/operators';
import { EnumItemDto, EnumService } from 'app/core/DevKenService/common/enum.service';
import { TeacherService } from 'app/core/DevKenService/Teacher/TeacherService';
import { TeacherDto, CreateTeacherRequest, UpdateTeacherRequest } from 'app/core/DevKenService/Types/Teacher';
import { CreateEditTeacherDialogComponent, CreateEditTeacherDialogResult } from 'app/dialog-modals/teachers/create-edit-teacher-dialog.component';
import { API_BASE_URL } from 'app/app.config';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService'; // ✅ Add this
import { SchoolDto } from 'app/Tenant/types/school'; // ✅ Add this

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

interface PhotoCacheEntry {
  url: SafeUrl;
  blobUrl: string;
  isLoading: boolean;
  error: boolean;
}

interface EnumMaps {
  employmentTypeValueToName: Map<number, string>;
  employmentTypeNameToValue: Map<string, number>;
  designationValueToName: Map<number, string>;
  designationNameToValue: Map<string, number>;
}

@Component({
  selector: 'app-teachers',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatButtonModule,
    MatDialogModule,
    MatSnackBarModule,
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
  templateUrl: './teachers.component.html',
})
export class TeachersComponent implements OnInit, OnDestroy {
  @ViewChild('photoInput') photoInputRef!: ElementRef<HTMLInputElement>;
  @ViewChild('teacherCell') teacherCellTemplate!: TemplateRef<any>;
  @ViewChild('numberCell') numberCellTemplate!: TemplateRef<any>;
  @ViewChild('contactCell') contactCellTemplate!: TemplateRef<any>;
  @ViewChild('designationCell') designationCellTemplate!: TemplateRef<any>;
  @ViewChild('employmentCell') employmentCellTemplate!: TemplateRef<any>;
  @ViewChild('statusCell') statusCellTemplate!: TemplateRef<any>;
  @ViewChild('schoolCell') schoolCellTemplate!: TemplateRef<any>; // ✅ Add this for school column
  

  private _unsubscribe = new Subject<void>();
  private _apiBaseUrl = inject(API_BASE_URL);
  private _http = inject(HttpClient);
  private _sanitizer = inject(DomSanitizer);
  private _authService = inject(AuthService);
  private _schoolService = inject(SchoolService); // ✅ Add this
  private _alert= inject(AlertService);

  // ── Breadcrumbs ──────────────────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Academic', url: '/academic' },
    { label: 'Teachers' }
  ];

  // ── SuperAdmin State ──────────────────────────────────────────────────────────
  get isSuperAdmin(): boolean {
    return this._authService.authUser?.isSuperAdmin ?? false;
  }

  schools: SchoolDto[] = []; // ✅ Add this
  
  // ✅ Add schools count
  get schoolsCount(): number {
    // Get unique school IDs from teachers
    const uniqueSchools = new Set(this.allData.map(t => t.schoolId));
    return uniqueSchools.size;
  }

  // ── Stats Cards Configuration ────────────────────────────────────────────────
  get statsCards(): StatCard[] {
    const baseCards: StatCard[] = [
      {
        label: 'Total Teachers',
        value: this.total,
        icon: 'groups',
        iconColor: 'indigo',
      },
      {
        label: 'Active',
        value: this.activeCount,
        icon: 'check_circle',
        iconColor: 'green',
      },
      {
        label: 'Class Teachers',
        value: this.classTeachersCount,
        icon: 'class',
        iconColor: 'violet',
      },
      {
        label: 'Permanent',
        value: this.permanentCount,
        icon: 'work',
        iconColor: 'amber',
      },
    ];

    // ✅ Add Schools card for SuperAdmin
    if (this.isSuperAdmin) {
      baseCards.push({
        label: 'Schools',
        value: this.schoolsCount,
        icon: 'school',
        iconColor: 'blue',
      });
    }

    return baseCards;
  }

  // ── Table Configuration ──────────────────────────────────────────────────────
  get tableColumns(): TableColumn<TeacherDto>[] {
    const baseColumns: TableColumn<TeacherDto>[] = [
      {
        id: 'teacher',
        label: 'Teacher',
        align: 'left',
        sortable: true,
      },
      {
        id: 'number',
        label: 'Number',
        align: 'left',
        hideOnMobile: true,
      },
    ];

    // ✅ Add School column for SuperAdmin
    if (this.isSuperAdmin) {
      baseColumns.push({
        id: 'school',
        label: 'School',
        align: 'left',
        hideOnMobile: true,
      });
    }

    // Add remaining columns
    baseColumns.push(
      {
        id: 'contact',
        label: 'Contact',
        align: 'left',
        hideOnMobile: true,
      },
      {
        id: 'designation',
        label: 'Designation',
        align: 'left',
        hideOnTablet: true,
      },
      {
        id: 'employment',
        label: 'Employment',
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

  tableActions: TableAction<TeacherDto>[] = [
    {
      id: 'edit',
      label: 'Edit',
      icon: 'edit',
      color: 'blue',
      handler: (teacher) => this.openEdit(teacher),
    },
    {
      id: 'uploadPhoto',
      label: 'Upload Photo',
      icon: 'photo_camera',
      color: 'violet',
      handler: (teacher) => this.uploadPhoto(teacher),
    },
    {
      id: 'toggleActive',
      label: 'Deactivate',
      icon: 'block',
      color: 'amber',
      handler: (teacher) => this.toggleActive(teacher),
      visible: (teacher) => teacher.isActive,
    },
    {
      id: 'activate',
      label: 'Activate',
      icon: 'check_circle',
      color: 'green',
      handler: (teacher) => this.toggleActive(teacher),
      visible: (teacher) => !teacher.isActive,
      divider: true,
    },
    {
      id: 'delete',
      label: 'Delete',
      icon: 'delete',
      color: 'red',
      handler: (teacher) => this.removeTeacher(teacher),
    },
  ];

  tableHeader: TableHeader = {
    title: 'Teachers List',
    subtitle: '',
    icon: 'table_chart',
    iconGradient: 'bg-gradient-to-br from-indigo-500 via-violet-600 to-purple-700',
  };

  tableEmptyState: TableEmptyState = {
    icon: 'person_search',
    message: 'No teachers found',
    description: 'Try adjusting your filters or add a new teacher',
    action: {
      label: 'Add First Teacher',
      icon: 'person_add',
      handler: () => this.openCreate(),
    },
  };

  // Cell templates will be set in ngAfterViewInit
  cellTemplates: { [key: string]: TemplateRef<any> } = {};

  // ── Filter Fields Configuration ──────────────────────────────────────────────
  filterFields: FilterField[] = [];
  showFilterPanel = false;

  // ── State ────────────────────────────────────────────────────────────────────
  allData: TeacherDto[] = [];
  isLoading = false;
  isEnumLoading = true;
  photoCache: { [key: string]: PhotoCacheEntry } = {};
  private _photoTargetTeacher: TeacherDto | null = null;

  // ── Filter Values ────────────────────────────────────────────────────────────
  private _filterValues = {
    search: '',
    status: 'all',
    employmentType: 'all',
    role: 'all',
    schoolId: 'all', // ✅ Add this
  };

  // ── Pagination ───────────────────────────────────────────────────────────────
  currentPage = 1;
  itemsPerPage = 10;

  // ── Enum Observables and Maps ────────────────────────────────────────────────
  employmentTypes$!: Observable<EnumItemDto[]>;
  designations$!: Observable<EnumItemDto[]>;
  
  private enumMaps: EnumMaps = {
    employmentTypeValueToName: new Map<number, string>(),
    employmentTypeNameToValue: new Map<string, number>(),
    designationValueToName: new Map<number, string>(),
    designationNameToValue: new Map<string, number>()
  };

  // ── Computed Stats ───────────────────────────────────────────────────────────
  get total(): number { 
    return this.allData.length; 
  }
  
  get activeCount(): number { 
    return this.allData.filter(t => t.isActive).length; 
  }
  
  get classTeachersCount(): number { 
    return this.allData.filter(t => t.isClassTeacher).length; 
  }
  
  get permanentCount(): number { 
    const permanentValue = this.enumMaps.employmentTypeNameToValue.get('Permanent');
    return this.allData.filter(t => {
      if (permanentValue !== undefined) {
        return Number(t.employmentType) === permanentValue;
      }
      return t.employmentType === 'Permanent';
    }).length; 
  }

  // ── Filtered Data ─────────────────────────────────────────────────────────────
  get filteredData(): TeacherDto[] {
    return this.allData.filter(t => {
      const q = this._filterValues.search.toLowerCase();
      
      // Get employment type name for filtering
      let employmentTypeName = t.employmentType;
      if (t.employmentType && !isNaN(Number(t.employmentType))) {
        const value = Number(t.employmentType);
        employmentTypeName = this.enumMaps.employmentTypeValueToName.get(value) || t.employmentType;
      }
      
      return (
        (!q || t.fullName?.toLowerCase().includes(q) || t.teacherNumber?.toLowerCase().includes(q)) &&
        (this._filterValues.status === 'all' ||
          (this._filterValues.status === 'active' && t.isActive) ||
          (this._filterValues.status === 'inactive' && !t.isActive)) &&
        (this._filterValues.employmentType === 'all' ||
          employmentTypeName === this._filterValues.employmentType) &&
        (this._filterValues.role === 'all' ||
          (this._filterValues.role === 'classTeacher' && t.isClassTeacher)) &&
        // ✅ Add school filter
        (this._filterValues.schoolId === 'all' || t.schoolId === this._filterValues.schoolId)
      );
    });
  }

  // ── Pagination Helpers ────────────────────────────────────────────────────────
  get paginatedData(): TeacherDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  constructor(
    private _service: TeacherService,
    private _enumService: EnumService,
    private _dialog: MatDialog,
    private _confirmation: FuseConfirmationService,
  ) {}

  ngOnInit(): void {
    this.loadEnumsAndInit();
  }

  ngAfterViewInit(): void {
    this.cellTemplates = {
      teacher: this.teacherCellTemplate,
      number: this.numberCellTemplate,
      school: this.schoolCellTemplate, // ✅ Add this
      contact: this.contactCellTemplate,
      designation: this.designationCellTemplate,
      employment: this.employmentCellTemplate,
      status: this.statusCellTemplate,
    };
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
    
    // Revoke all blob URLs to prevent memory leaks
    Object.values(this.photoCache).forEach(entry => {
      if (entry.blobUrl) {
        URL.revokeObjectURL(entry.blobUrl);
      }
    });
  }

  // ── Enum Loading and Mapping ─────────────────────────────────────────────────
  private loadEnumsAndInit(): void {
    this.isEnumLoading = true;
    
    // Load employment types
    this.employmentTypes$ = this._enumService.getTeacherEmploymentTypes().pipe(
      map(types => {
        types.forEach(item => {
          if (item.value !== undefined && item.name) {
            this.enumMaps.employmentTypeValueToName.set(item.value, item.name);
            this.enumMaps.employmentTypeNameToValue.set(item.name, item.value);
            this.enumMaps.employmentTypeNameToValue.set(item.name.toLowerCase(), item.value);
          }
        });
        return types;
      }),
      takeUntil(this._unsubscribe)
    );

    // Load designations
    this.designations$ = this._enumService.getTeacherDesignations().pipe(
      map(designations => {
        designations.forEach(item => {
          if (item.value !== undefined && item.name) {
            this.enumMaps.designationValueToName.set(item.value, item.name);
            this.enumMaps.designationNameToValue.set(item.name, item.value);
            this.enumMaps.designationNameToValue.set(item.name.toLowerCase(), item.value);
          }
        });
        return designations;
      }),
      takeUntil(this._unsubscribe)
    );

    // ✅ Build requests object
    const requests: any = {
      employmentTypes: this.employmentTypes$,
      designations: this.designations$
    };

    // ✅ Add schools request only for SuperAdmin
    if (this.isSuperAdmin) {
      requests.schools = this._schoolService.getAll().pipe(
        catchError(err => {
          console.error('Failed to load schools:', err);
          return of({ success: false, message: '', data: [] });
        })
      );
    }

    // Wait for all requests to complete
    forkJoin(requests).pipe(
      takeUntil(this._unsubscribe),
      finalize(() => {
        this.isEnumLoading = false;
      })
    ).subscribe({
      next: (results: any) => {
        // ✅ Store schools for SuperAdmin
        if (results.schools) {
          this.schools = results.schools.data || [];
        }
        
        this.initializeFilterFields(results.employmentTypes);
        this.loadAll();
      },
      error: (error) => {
        console.error('Failed to load enums:', error);
        this._showError('Failed to load configuration data');
        this.isEnumLoading = false;
        this.loadAll();
      }
    });
  }

  // ── Initialize Filter Fields ─────────────────────────────────────────────────
  private initializeFilterFields(types: EnumItemDto[]): void {
    this.filterFields = [
      {
        id: 'search',
        label: 'Search',
        type: 'text',
        placeholder: 'Name or teacher number...',
        value: this._filterValues.search,
      },
    ];

    // ✅ Add school filter for SuperAdmin
    if (this.isSuperAdmin) {
      this.filterFields.push({
        id: 'schoolId',
        label: 'School',
        type: 'select',
        value: this._filterValues.schoolId,
        options: [
          { label: 'All Schools', value: 'all' },
          ...this.schools.map(s => ({ 
            label: `${s.name}${s.slugName ? ' (' + s.phoneNumber + ')' : ''}`, 
            value: s.id 
          })),
        ],
      });
    }

    // Add remaining filters
    this.filterFields.push(
      {
        id: 'status',
        label: 'Status',
        type: 'select',
        value: this._filterValues.status,
        options: [
          { label: 'All Statuses', value: 'all' },
          { label: 'Active', value: 'active' },
          { label: 'Inactive', value: 'inactive' },
        ],
      },
      {
        id: 'employmentType',
        label: 'Employment Type',
        type: 'select',
        value: this._filterValues.employmentType,
        options: [
          { label: 'All Types', value: 'all' },
          ...types.map(t => ({ label: t.name, value: t.name })),
        ],
      },
      {
        id: 'role',
        label: 'Role',
        type: 'select',
        value: this._filterValues.role,
        options: [
          { label: 'All Roles', value: 'all' },
          { label: 'Class Teachers', value: 'classTeacher' },
          { label: 'Subject Teachers', value: 'subject' },
        ],
      }
    );
  }
  private _showSuccess(message: string): void { this._alert.success(message); }
  private _showError(message: string): void { this._alert.error(message); }

  // In teachers.component.ts
toggleActive(teacher: TeacherDto): void {
  const newStatus = !teacher.isActive;
  const action = newStatus ? 'activate' : 'deactivate';

  // Use AlertService.confirm instead of FuseConfirmationService
  this._alert.confirm({
    title: `${newStatus ? 'Activate' : 'Deactivate'} Teacher`,
    message: `Are you sure you want to ${action} ${teacher.fullName}?`,
    confirmText: newStatus ? 'Activate' : 'Deactivate',
    cancelText: 'Cancel',
    onConfirm: () => {
      this._service.toggleStatus(teacher.id, newStatus)
        .pipe(takeUntil(this._unsubscribe))
        .subscribe({
          next: (res) => {
            if (res.success) {
              this._alert.success(`Teacher ${action}d successfully`);
              this.loadAll();
            }
          },
          error: (err) => {
            console.error('Failed to update teacher status:', err);
            this._alert.error(err.error?.message || `Failed to ${action} teacher`);
          }
        });
    },
    onCancel: () => {
      // optional: you can show an info alert or just do nothing
      this._alert.info('Action cancelled');
    }
  });
}


  // ── Helper Methods for Display ───────────────────────────────────────────────
  
  getEmploymentTypeName(value: string | number | undefined): string {
    if (value === undefined || value === null) return '—';
    
    if (typeof value === 'string' && isNaN(Number(value))) {
      return value;
    }
    
    const numValue = typeof value === 'string' ? parseInt(value, 10) : value;
    return this.enumMaps.employmentTypeValueToName.get(numValue) || value.toString();
  }

  getDesignationName(value: string | number | undefined): string {
    if (value === undefined || value === null) return '—';
    
    if (typeof value === 'string' && isNaN(Number(value))) {
      return value;
    }
    
    const numValue = typeof value === 'string' ? parseInt(value, 10) : value;
    return this.enumMaps.designationValueToName.get(numValue) || value.toString();
  }

  getEmploymentTypeValue(name: string | undefined): number | null {
    if (!name) return null;
    return this.enumMaps.employmentTypeNameToValue.get(name) || 
           this.enumMaps.employmentTypeNameToValue.get(name.toLowerCase()) || null;
  }

  // ── Filter Handlers ──────────────────────────────────────────────────────────
  toggleFilterPanel(): void {
    this.showFilterPanel = !this.showFilterPanel;
  }

  onFilterChange(event: FilterChangeEvent): void {
    (this._filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} teachers found`;
    
    // ✅ If school filter changed and SuperAdmin, reload from API
    if (event.filterId === 'schoolId' && this.isSuperAdmin) {
      const schoolId = event.value === 'all' ? null : event.value;
      this.loadAll(schoolId);
    }
  }

  onClearFilters(): void {
    this._filterValues = {
      search: '',
      status: 'all',
      employmentType: 'all',
      role: 'all',
      schoolId: 'all', // ✅ Add this
    };
    
    this.filterFields.forEach(field => {
      field.value = (this._filterValues as any)[field.id];
    });
    
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} teachers found`;
    
    // ✅ Reload all teachers
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

  // ── Image Loading ────────────────────────────────────────────────────────────
  private loadTeacherPhoto(teacherId: string, photoUrl: string): void {
    if (!this.photoCache[teacherId]) {
      this.photoCache[teacherId] = {
        url: null!,
        blobUrl: '',
        isLoading: true,
        error: false
      };
    } else {
      this.photoCache[teacherId].isLoading = true;
      this.photoCache[teacherId].error = false;
    }

    const url = photoUrl.startsWith('http')
      ? photoUrl
      : `${this._apiBaseUrl}${photoUrl}`;

    const token = this._authService.accessToken;
    const headers = token ? new HttpHeaders().set('Authorization', `Bearer ${token}`) : undefined;
    const authUrl = token ? `${url}?token=${token}` : url;

    this._http.get(authUrl, { 
      responseType: 'blob',
      headers 
    }).pipe(
      takeUntil(this._unsubscribe),
      catchError((error) => {
        console.error(`Failed to load photo for teacher ${teacherId}:`, error);
        this.photoCache[teacherId] = {
          ...this.photoCache[teacherId],
          isLoading: false,
          error: true
        };
        return of(null);
      })
    ).subscribe(blob => {
      if (blob) {
        if (this.photoCache[teacherId]?.blobUrl) {
          URL.revokeObjectURL(this.photoCache[teacherId].blobUrl);
        }

        const blobUrl = URL.createObjectURL(blob);
        const safeUrl = this._sanitizer.bypassSecurityTrustUrl(blobUrl);
        
        this.photoCache[teacherId] = {
          url: safeUrl,
          blobUrl: blobUrl,
          isLoading: false,
          error: false
        };
      }
    });
  }

  private preloadVisibleImages(): void {
    this.paginatedData.forEach(teacher => {
      if (teacher.photoUrl && !this.photoCache[teacher.id]?.url) {
        this.loadTeacherPhoto(teacher.id, teacher.photoUrl);
      }
    });
  }

  private clearAndReloadImage(teacherId: string, photoUrl: string | null): void {
    if (this.photoCache[teacherId]?.blobUrl) {
      URL.revokeObjectURL(this.photoCache[teacherId].blobUrl);
    }
    
    delete this.photoCache[teacherId];

    if (photoUrl) {
      this.loadTeacherPhoto(teacherId, photoUrl);
    }
  }

  // ── Data Loading ──────────────────────────────────────────────────────────────
  // ✅ Updated to accept optional schoolId parameter
  loadAll(schoolId?: string | null): void {
    this.isLoading = true;
    this._service.getAll(schoolId || undefined)
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next: res => {
          if (res.success) {
            this.allData = res.data;
            this.tableHeader.subtitle = `${this.filteredData.length} teachers found`;
            setTimeout(() => this.preloadVisibleImages(), 0);
          }
          this.isLoading = false;
        },
        error: (err) => {
          console.error('Failed to load teachers:', err);
          this.isLoading = false;
          this._showError(err.error?.message || 'Failed to load teachers');
        }
      });
  }

  // ... (rest of the methods remain the same: openCreate, openEdit, toggleActive, etc.)
  
  openCreate(): void {
    const dialogRef = this._dialog.open(CreateEditTeacherDialogComponent, {
      panelClass: ['teacher-dialog', 'no-padding-dialog'],
      width: '900px',
      maxWidth: '95vw',
      maxHeight: '95vh',
      disableClose: true,
      autoFocus: 'input',
      data: { mode: 'create' },
    });

    dialogRef.afterClosed()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe((result: CreateEditTeacherDialogResult | null) => {
        if (!result) return;

        const request: CreateTeacherRequest = result.formData;
        const photoFile = result.photoFile ?? null;

        this._service.create(request)
          .pipe(takeUntil(this._unsubscribe))
          .subscribe({
            next: (res) => {
              if (res.success) {
                const teacherId = res.data.id;

                if (photoFile && teacherId) {
                  this._service.uploadPhoto(teacherId, photoFile)
                    .pipe(takeUntil(this._unsubscribe))
                    .subscribe({
                      next: () => {
                        this._showSuccess('Teacher created with photo successfully');
                        this.loadAll();
                      },
                      error: (photoErr) => {
                        console.error('Photo upload failed:', photoErr);
                        this._showSuccess('Teacher created, but photo upload failed. You can upload it later.');
                        this.loadAll();
                      }
                    });
                } else {
                  this._showSuccess('Teacher created successfully');
                  this.loadAll();
                }
              }
            },
            error: (err) => {
              console.error('Failed to create teacher:', err);
              this._showError(err.error?.message || 'Failed to create teacher');
            }
          });
      });
  }

  openEdit(teacher: TeacherDto): void {
    const dialogRef = this._dialog.open(CreateEditTeacherDialogComponent, {
      panelClass: ['teacher-dialog', 'no-padding-dialog'],
      width: '900px',
      maxWidth: '95vw',
      maxHeight: '95vh',
      disableClose: true,
      autoFocus: 'input',
      data: { mode: 'edit', teacher },
    });

    dialogRef.afterClosed()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe((result: CreateEditTeacherDialogResult | null) => {
        if (!result) return;

        const request: UpdateTeacherRequest = result.formData;
        const photoFile = result.photoFile ?? null;

        this._service.update(teacher.id, request)
          .pipe(takeUntil(this._unsubscribe))
          .subscribe({
            next: (res) => {
              if (res.success) {
                if (photoFile) {
                  this._service.uploadPhoto(teacher.id, photoFile)
                    .pipe(takeUntil(this._unsubscribe))
                    .subscribe({
                      next: () => {
                        this._showSuccess('Teacher updated with photo successfully');
                        this.clearAndReloadImage(teacher.id, null);
                        this.loadAll();
                      },
                      error: (photoErr) => {
                        console.error('Photo upload failed:', photoErr);
                        this._showSuccess('Teacher updated, but photo upload failed. You can upload it later.');
                        this.loadAll();
                      }
                    });
                } else {
                  this._showSuccess('Teacher updated successfully');
                  this.loadAll();
                }
              }
            },
            error: (err) => {
              console.error('Failed to update teacher:', err);
              this._showError(err.error?.message || 'Failed to update teacher');
            }
          });
      });
  }


  removeTeacher(teacher: TeacherDto): void {
    const confirmation = this._confirmation.open({
      title: 'Delete Teacher',
      message: `Are you sure you want to delete ${teacher.fullName}? This action cannot be undone.`,
      icon: {
        name: 'delete',
        color: 'warn',
      },
      actions: {
        confirm: {
          label: 'Delete',
          color: 'warn',
        },
        cancel: {
          label: 'Cancel',
        },
      },
    });

    confirmation.afterClosed().pipe(takeUntil(this._unsubscribe)).subscribe(result => {
      if (result === 'confirmed') {
        this._service.delete(teacher.id)
          .pipe(takeUntil(this._unsubscribe))
          .subscribe({
            next: (res) => {
              if (res.success) {
                this._showSuccess('Teacher deleted successfully');
                
                if (this.photoCache[teacher.id]?.blobUrl) {
                  URL.revokeObjectURL(this.photoCache[teacher.id].blobUrl);
                }
                delete this.photoCache[teacher.id];
                
                if (this.paginatedData.length === 0 && this.currentPage > 1) {
                  this.currentPage--;
                }
                
                this.loadAll();
              }
            },
            error: (err) => {
              console.error('Failed to delete teacher:', err);
              this._showError(err.error?.message || 'Failed to delete teacher');
            }
          });
      }
    });
  }

  uploadPhoto(teacher: TeacherDto): void {
    this._photoTargetTeacher = teacher;
    this.photoInputRef.nativeElement.value = '';
    this.photoInputRef.nativeElement.click();
  }

  onPhotoFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length || !this._photoTargetTeacher) return;
    
    const file = input.files[0];
    
    if (file.size > 5 * 1024 * 1024) {
      this._showError('File size must be less than 5MB');
      return;
    }

    if (!file.type.startsWith('image/')) {
      this._showError('File must be an image');
      return;
    }

    this._uploadPhoto(this._photoTargetTeacher.id, file, () => {
      this._showSuccess('Photo uploaded successfully');
      this.clearAndReloadImage(this._photoTargetTeacher!.id, this._photoTargetTeacher!.photoUrl || '');
      this.loadAll();
    });
  }

  private _uploadPhoto(teacherId: string, file: File, onSuccess: () => void): void {
    this._service.uploadPhoto(teacherId, file)
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next: res => {
          if (res.success) {
            if (this.photoCache[teacherId]?.blobUrl) {
              URL.revokeObjectURL(this.photoCache[teacherId].blobUrl);
            }
            delete this.photoCache[teacherId];
            
            onSuccess();
          }
        },
        error: err => {
          console.error('Photo upload failed:', err);
          this._showError(err.error?.message || 'Failed to upload photo');
        },
      });
  }


}