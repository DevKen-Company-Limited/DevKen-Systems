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
import { MatTooltipModule } from '@angular/material/tooltip';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { FuseConfirmationService } from '@fuse/services/confirmation';
import { Observable, of, Subject, forkJoin } from 'rxjs';
import { catchError, takeUntil, map, finalize } from 'rxjs/operators';
import { EnumItemDto, EnumService } from 'app/core/DevKenService/common/enum.service';
import { StudentService } from 'app/core/DevKenService/administration/students/StudentService';

import { API_BASE_URL } from 'app/app.config';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { SchoolDto } from 'app/Tenant/types/school';

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
import { StudentDto } from './types/studentdto';
import { BulkPhotoUploadDialogComponent } from 'app/dialog-modals/Student/bulk-photo-upload-dialog';
import { PhotoViewerDialogComponent } from 'app/dialog-modals/Student/photo-viewer-dialog';


interface PhotoCacheEntry {
  url: SafeUrl;
  blobUrl: string;
  isLoading: boolean;
  error: boolean;
}

interface EnumMaps {
  genderValueToName: Map<number, string>;
  genderNameToValue: Map<string, number>;
  studentStatusValueToName: Map<number, string>;
  studentStatusNameToValue: Map<string, number>;
  cbcLevelValueToName: Map<number, string>;
  cbcLevelNameToValue: Map<string, number>;
}

@Component({
  selector: 'app-students',
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
    MatTooltipModule,
    // Reusable components
    PageHeaderComponent,
    FilterPanelComponent,
    PaginationComponent,
    StatsCardsComponent,
    DataTableComponent,
  ],
  templateUrl: './students.component.html',
})
export class StudentsComponent implements OnInit, OnDestroy {
  @ViewChild('photoInput') photoInputRef!: ElementRef<HTMLInputElement>;
  @ViewChild('studentCell') studentCellTemplate!: TemplateRef<any>;
  @ViewChild('numberCell') numberCellTemplate!: TemplateRef<any>;
  @ViewChild('contactCell') contactCellTemplate!: TemplateRef<any>;
  @ViewChild('academicCell') academicCellTemplate!: TemplateRef<any>;
  @ViewChild('guardianCell') guardianCellTemplate!: TemplateRef<any>;
  @ViewChild('statusCell') statusCellTemplate!: TemplateRef<any>;
  @ViewChild('schoolCell') schoolCellTemplate!: TemplateRef<any>;

  private _unsubscribe = new Subject<void>();
  private _apiBaseUrl = inject(API_BASE_URL);
  private _http = inject(HttpClient);
  private _sanitizer = inject(DomSanitizer);
  private _authService = inject(AuthService);
  private _schoolService = inject(SchoolService);
  private _router = inject(Router);

  // ── Breadcrumbs ──────────────────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Academic', url: '/academic' },
    { label: 'Students' }
  ];

  // ── SuperAdmin State ──────────────────────────────────────────────────────────
  get isSuperAdmin(): boolean {
    return this._authService.authUser?.isSuperAdmin ?? false;
  }

  schools: SchoolDto[] = [];
  
  get schoolsCount(): number {
    const uniqueSchools = new Set(this.allData.map(s => s.schoolId));
    return uniqueSchools.size;
  }

  // ── Stats Cards Configuration ────────────────────────────────────────────────
  get statsCards(): StatCard[] {
    const baseCards: StatCard[] = [
      {
        label: 'Total Students',
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
        label: 'Male',
        value: this.maleCount,
        icon: 'male',
        iconColor: 'blue',
      },
      {
        label: 'Female',
        value: this.femaleCount,
        icon: 'female',
        iconColor: 'pink',
      },
    ];

    if (this.isSuperAdmin) {
      baseCards.push({
        label: 'Schools',
        value: this.schoolsCount,
        icon: 'school',
        iconColor: 'green',
      });
    }

    return baseCards;
  }

  // ── Table Configuration ──────────────────────────────────────────────────────
  get tableColumns(): TableColumn<StudentDto>[] {
    const baseColumns: TableColumn<StudentDto>[] = [
      {
        id: 'student',
        label: 'Student',
        align: 'left',
        sortable: true,
      },
      {
        id: 'number',
        label: 'Admission No.',
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
        id: 'contact',
        label: 'Guardian Contact',
        align: 'left',
        hideOnMobile: true,
      },
      {
        id: 'academic',
        label: 'Academic Details',
        align: 'left',
        hideOnTablet: true,
      },
      {
        id: 'guardian',
        label: 'Guardian',
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

  tableActions: TableAction<StudentDto>[] = [
    {
      id: 'view',
      label: 'View Details',
      icon: 'visibility',
      color: 'blue',
      handler: (student) => this.viewStudent(student),
    },
    {
      id: 'edit',
      label: 'Edit',
      icon: 'edit',
      color: 'indigo',
      handler: (student) => this.editStudent(student),
    },
    {
      id: 'uploadPhoto',
      label: 'Upload Photo',
      icon: 'photo_camera',
      color: 'violet',
      handler: (student) => this.uploadPhoto(student),
    },
    {
      id: 'toggleActive',
      label: 'Deactivate',
      icon: 'block',
      color: 'amber',
      handler: (student) => this.toggleActive(student),
      visible: (student) => student.isActive,
    },
    {
      id: 'activate',
      label: 'Activate',
      icon: 'check_circle',
      color: 'green',
      handler: (student) => this.toggleActive(student),
      visible: (student) => !student.isActive,
      divider: true,
    },
    {
      id: 'delete',
      label: 'Delete',
      icon: 'delete',
      color: 'red',
      handler: (student) => this.removeStudent(student),
    },
  ];

  tableHeader: TableHeader = {
    title: 'Students List',
    subtitle: '',
    icon: 'table_chart',
    iconGradient: 'bg-gradient-to-br from-indigo-500 via-violet-600 to-purple-700',
  };

  tableEmptyState: TableEmptyState = {
    icon: 'person_search',
    message: 'No students found',
    description: 'Try adjusting your filters or enroll a new student',
    action: {
      label: 'Enroll First Student',
      icon: 'person_add',
      handler: () => this.enrollStudent(),
    },
  };

  cellTemplates: { [key: string]: TemplateRef<any> } = {};

  // ── Filter Fields Configuration ──────────────────────────────────────────────
  filterFields: FilterField[] = [];
  showFilterPanel = false;

  // ── State ────────────────────────────────────────────────────────────────────
  allData: StudentDto[] = [];
  isLoading = false;
  isEnumLoading = true;
  photoCache: { [key: string]: PhotoCacheEntry } = {};
  private _photoTargetStudent: StudentDto | null = null;

  // ── Filter Values ────────────────────────────────────────────────────────────
  private _filterValues = {
    search: '',
    status: 'all',
    gender: 'all',
    cbcLevel: 'all',
    studentStatus: 'all',
    schoolId: 'all',
  };

  // ── Pagination ───────────────────────────────────────────────────────────────
  currentPage = 1;
  itemsPerPage = 10;

  // ── Enum Observables and Maps ────────────────────────────────────────────────
  genders$!: Observable<EnumItemDto[]>;
  studentStatuses$!: Observable<EnumItemDto[]>;
  cbcLevels$!: Observable<EnumItemDto[]>;
  
  private enumMaps: EnumMaps = {
    genderValueToName: new Map<number, string>(),
    genderNameToValue: new Map<string, number>(),
    studentStatusValueToName: new Map<number, string>(),
    studentStatusNameToValue: new Map<string, number>(),
    cbcLevelValueToName: new Map<number, string>(),
    cbcLevelNameToValue: new Map<string, number>()
  };

  // ── Computed Stats ───────────────────────────────────────────────────────────
  get total(): number { 
    return this.allData.length; 
  }
  
  get activeCount(): number { 
    return this.allData.filter(s => s.isActive).length; 
  }
  
  get maleCount(): number { 
    const maleValue = this.enumMaps.genderNameToValue.get('Male');
    return this.allData.filter(s => {
      if (maleValue !== undefined) {
        return Number(s.gender) === maleValue;
      }
      return s.gender === 'Male';
    }).length; 
  }
  
  get femaleCount(): number { 
    const femaleValue = this.enumMaps.genderNameToValue.get('Female');
    return this.allData.filter(s => {
      if (femaleValue !== undefined) {
        return Number(s.gender) === femaleValue;
      }
      return s.gender === 'Female';
    }).length; 
  }

  // ── Filtered Data ─────────────────────────────────────────────────────────────
  get filteredData(): StudentDto[] {
    return this.allData.filter(s => {
      const q = this._filterValues.search.toLowerCase();
      
      let genderName = s.gender;
      if (s.gender && !isNaN(Number(s.gender))) {
        const value = Number(s.gender);
        genderName = this.enumMaps.genderValueToName.get(value) || s.gender;
      }

      let cbcLevelName = s.cbcLevel;
      if (s.cbcLevel && !isNaN(Number(s.cbcLevel))) {
        const value = Number(s.cbcLevel);
        cbcLevelName = this.enumMaps.cbcLevelValueToName.get(value) || s.cbcLevel;
      }

      let studentStatusName = s.studentStatus;
      if (s.studentStatus && !isNaN(Number(s.studentStatus))) {
        const value = Number(s.studentStatus);
        studentStatusName = this.enumMaps.studentStatusValueToName.get(value) || s.studentStatus;
      }
      
      return (
        (!q || 
          s.fullName?.toLowerCase().includes(q) || 
          s.admissionNumber?.toLowerCase().includes(q) ||
          s.nemisNumber?.toString().includes(q)) &&
        (this._filterValues.status === 'all' ||
          (this._filterValues.status === 'active' && s.isActive) ||
          (this._filterValues.status === 'inactive' && !s.isActive)) &&
        (this._filterValues.gender === 'all' || genderName === this._filterValues.gender) &&
        (this._filterValues.cbcLevel === 'all' || cbcLevelName === this._filterValues.cbcLevel) &&
        (this._filterValues.studentStatus === 'all' || studentStatusName === this._filterValues.studentStatus) &&
        (this._filterValues.schoolId === 'all' || s.schoolId === this._filterValues.schoolId)
      );
    });
  }

  // ── Pagination Helpers ────────────────────────────────────────────────────────
  get paginatedData(): StudentDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  constructor(
    private _service: StudentService,
    private _enumService: EnumService,
    private _dialog: MatDialog,
    private _snackBar: MatSnackBar,
    private _confirmation: FuseConfirmationService,
  ) {}

  ngOnInit(): void {
    this.loadEnumsAndInit();
  }

  ngAfterViewInit(): void {
    this.cellTemplates = {
      student: this.studentCellTemplate,
      number: this.numberCellTemplate,
      school: this.schoolCellTemplate,
      contact: this.contactCellTemplate,
      academic: this.academicCellTemplate,
      guardian: this.guardianCellTemplate,
      status: this.statusCellTemplate,
    };
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
    
    Object.values(this.photoCache).forEach(entry => {
      if (entry.blobUrl) {
        URL.revokeObjectURL(entry.blobUrl);
      }
    });
  }

  // ── Enum Loading and Mapping ─────────────────────────────────────────────────
  private loadEnumsAndInit(): void {
    this.isEnumLoading = true;
    
    this.genders$ = this._enumService.getGenders().pipe(
      map(items => {
        items.forEach(item => {
          if (item.value !== undefined && item.name) {
            this.enumMaps.genderValueToName.set(item.value, item.name);
            this.enumMaps.genderNameToValue.set(item.name, item.value);
            this.enumMaps.genderNameToValue.set(item.name.toLowerCase(), item.value);
          }
        });
        return items;
      }),
      takeUntil(this._unsubscribe)
    );

    this.studentStatuses$ = this._enumService.getStudentStatuses().pipe(
      map(items => {
        items.forEach(item => {
          if (item.value !== undefined && item.name) {
            this.enumMaps.studentStatusValueToName.set(item.value, item.name);
            this.enumMaps.studentStatusNameToValue.set(item.name, item.value);
            this.enumMaps.studentStatusNameToValue.set(item.name.toLowerCase(), item.value);
          }
        });
        return items;
      }),
      takeUntil(this._unsubscribe)
    );

    this.cbcLevels$ = this._enumService.getCBCLevels().pipe(
      map(items => {
        items.forEach(item => {
          if (item.value !== undefined && item.name) {
            this.enumMaps.cbcLevelValueToName.set(item.value, item.name);
            this.enumMaps.cbcLevelNameToValue.set(item.name, item.value);
            this.enumMaps.cbcLevelNameToValue.set(item.name.toLowerCase(), item.value);
          }
        });
        return items;
      }),
      takeUntil(this._unsubscribe)
    );

    const requests: any = {
      genders: this.genders$,
      studentStatuses: this.studentStatuses$,
      cbcLevels: this.cbcLevels$
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
        this.isEnumLoading = false;
      })
    ).subscribe({
      next: (results: any) => {
        if (results.schools) {
          this.schools = results.schools.data || [];
        }
        
        this.initializeFilterFields(results.genders, results.studentStatuses, results.cbcLevels);
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
  private initializeFilterFields(
    genders: EnumItemDto[], 
    statuses: EnumItemDto[], 
    levels: EnumItemDto[]
  ): void {
    this.filterFields = [
      {
        id: 'search',
        label: 'Search',
        type: 'text',
        placeholder: 'Name, admission number, NEMIS...',
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
          { label: 'Active', value: 'active' },
          { label: 'Inactive', value: 'inactive' },
        ],
      },
      {
        id: 'gender',
        label: 'Gender',
        type: 'select',
        value: this._filterValues.gender,
        options: [
          { label: 'All Genders', value: 'all' },
          ...genders.map(g => ({ label: g.name, value: g.name })),
        ],
      },
      {
        id: 'cbcLevel',
        label: 'CBC Level',
        type: 'select',
        value: this._filterValues.cbcLevel,
        options: [
          { label: 'All Levels', value: 'all' },
          ...levels.map(l => ({ label: l.name, value: l.name })),
        ],
      },
      {
        id: 'studentStatus',
        label: 'Student Status',
        type: 'select',
        value: this._filterValues.studentStatus,
        options: [
          { label: 'All Types', value: 'all' },
          ...statuses.map(s => ({ label: s.name, value: s.name })),
        ],
      }
    );
  }

  // ── Helper Methods for Display ───────────────────────────────────────────────
  getGenderName(value: string | number | undefined): string {
    if (value === undefined || value === null) return '—';
    
    if (typeof value === 'string' && isNaN(Number(value))) {
      return value;
    }
    
    const numValue = typeof value === 'string' ? parseInt(value, 10) : value;
    return this.enumMaps.genderValueToName.get(numValue) || value.toString();
  }

  getCBCLevelName(value: string | number | undefined): string {
    if (value === undefined || value === null) return '—';
    
    if (typeof value === 'string' && isNaN(Number(value))) {
      return value;
    }
    
    const numValue = typeof value === 'string' ? parseInt(value, 10) : value;
    return this.enumMaps.cbcLevelValueToName.get(numValue) || value.toString();
  }

  getStudentStatusName(value: string | number | undefined): string {
    if (value === undefined || value === null) return '—';
    
    if (typeof value === 'string' && isNaN(Number(value))) {
      return value;
    }
    
    const numValue = typeof value === 'string' ? parseInt(value, 10) : value;
    return this.enumMaps.studentStatusValueToName.get(numValue) || value.toString();
  }

  // ── Filter Handlers ──────────────────────────────────────────────────────────
  toggleFilterPanel(): void {
    this.showFilterPanel = !this.showFilterPanel;
  }

  onFilterChange(event: FilterChangeEvent): void {
    (this._filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} students found`;
    
    if (event.filterId === 'schoolId' && this.isSuperAdmin) {
      const schoolId = event.value === 'all' ? null : event.value;
      this.loadAll(schoolId);
    }
  }

  onClearFilters(): void {
    this._filterValues = {
      search: '',
      status: 'all',
      gender: 'all',
      cbcLevel: 'all',
      studentStatus: 'all',
      schoolId: 'all',
    };
    
    this.filterFields.forEach(field => {
      field.value = (this._filterValues as any)[field.id];
    });
    
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} students found`;
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
  private loadStudentPhoto(studentId: string, photoUrl: string): void {
    console.log(`Loading photo for student ${studentId}:`, photoUrl);
    
    if (!this.photoCache[studentId]) {
      this.photoCache[studentId] = {
        url: null!,
        blobUrl: '',
        isLoading: true,
        error: false
      };
    } else {
      this.photoCache[studentId].isLoading = true;
      this.photoCache[studentId].error = false;
    }

    const url = photoUrl.startsWith('http')
      ? photoUrl
      : `${this._apiBaseUrl}${photoUrl}`;

    console.log(`Fetching from: ${url}`);

    const token = this._authService.accessToken;
    const headers = token ? new HttpHeaders().set('Authorization', `Bearer ${token}`) : undefined;
    const authUrl = token ? `${url}?token=${token}` : url;

    this._http.get(authUrl, { 
      responseType: 'blob',
      headers 
    }).pipe(
      takeUntil(this._unsubscribe),
      catchError((error) => {
        console.error(`❌ Failed to load photo for student ${studentId}:`, error);
        this.photoCache[studentId] = {
          ...this.photoCache[studentId],
          isLoading: false,
          error: true
        };
        return of(null);
      })
    ).subscribe(blob => {
      if (blob) {
        console.log(`✅ Photo blob received for ${studentId}, size: ${blob.size} bytes, type: ${blob.type}`);
        
        if (this.photoCache[studentId]?.blobUrl) {
          URL.revokeObjectURL(this.photoCache[studentId].blobUrl);
        }

        const blobUrl = URL.createObjectURL(blob);
        const safeUrl = this._sanitizer.bypassSecurityTrustUrl(blobUrl);
        
        console.log(`✅ Created blob URL for ${studentId}:`, blobUrl);
        
        this.photoCache[studentId] = {
          url: safeUrl,
          blobUrl: blobUrl,
          isLoading: false,
          error: false
        };
      } else {
        console.error(`❌ No blob received for student ${studentId}`);
      }
    });
  }

  private preloadVisibleImages(): void {
    this.paginatedData.forEach(student => {
      if (student.photoUrl && !this.photoCache[student.id]?.url) {
        this.loadStudentPhoto(student.id, student.photoUrl);
      }
    });
  }

  private clearAndReloadImage(studentId: string, photoUrl: string | null): void {
    if (this.photoCache[studentId]?.blobUrl) {
      URL.revokeObjectURL(this.photoCache[studentId].blobUrl);
    }
    
    delete this.photoCache[studentId];

    if (photoUrl) {
      this.loadStudentPhoto(studentId, photoUrl);
    }
  }

  // ── Data Loading ──────────────────────────────────────────────────────────────
  loadAll(schoolId?: string | null): void {
    this.isLoading = true;
    this._service.getAll(schoolId || undefined)
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next: res => {
          this.allData = Array.isArray(res) ? res : [];
          this.tableHeader.subtitle = `${this.filteredData.length} students found`;
          setTimeout(() => this.preloadVisibleImages(), 0);
          this.isLoading = false;
        },
        error: (err) => {
          console.error('Failed to load students:', err);
          this.isLoading = false;
          this._showError(err.error?.message || 'Failed to load students');
        }
      });
  }

  // ── Actions ──────────────────────────────────────────────────────────────────
  enrollStudent(): void {
    this._router.navigate(['/academic/students/enroll']);
  }

  viewStudent(student: StudentDto): void {
    this._router.navigate(['/academic/students', student.id]);
  }

  editStudent(student: StudentDto): void {
    this._router.navigate(['/academic/students/edit', student.id]);
  }

  /**
   * Open bulk photo upload dialog
   */
  bulkUploadPhotos(): void {
    const dialogRef = this._dialog.open(BulkPhotoUploadDialogComponent, {
      width: '900px',
      maxWidth: '95vw',
      maxHeight: '95vh',
      disableClose: false,
      panelClass: 'bulk-photo-upload-dialog',
    });

    dialogRef.afterClosed()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe((uploaded: boolean) => {
        if (uploaded) {
          this._showSuccess('Photos uploaded successfully! Refreshing list...');
          // Clear photo cache to reload updated photos
          Object.keys(this.photoCache).forEach(key => {
            if (this.photoCache[key].blobUrl) {
              URL.revokeObjectURL(this.photoCache[key].blobUrl);
            }
          });
          this.photoCache = {};
          // Reload data
          this.loadAll();
        }
      });
  }

  /**
   * Open photo viewer to enlarge student photo
   */
  viewStudentPhoto(student: StudentDto): void {
    console.log('=== Opening Photo Viewer ===');
    console.log('Student:', student.fullName, '(', student.admissionNumber, ')');
    console.log('Student photoUrl property:', student.photoUrl);
    console.log('Cache entry exists:', !!this.photoCache[student.id]);
    
    if (this.photoCache[student.id]) {
      console.log('Cache entry:', {
        hasUrl: !!this.photoCache[student.id].url,
        hasBlobUrl: !!this.photoCache[student.id].blobUrl,
        blobUrl: this.photoCache[student.id].blobUrl,
        isLoading: this.photoCache[student.id].isLoading,
        error: this.photoCache[student.id].error
      });
    }
    
    // Check if photo is still loading
    if (this.photoCache[student.id]?.isLoading) {
      this._showError('Photo is still loading. Please wait a moment.');
      return;
    }
    
    // Check if photo had an error
    if (this.photoCache[student.id]?.error) {
      this._showError('Photo failed to load. Please try refreshing the page.');
      return;
    }
    
    // Check if we have any photo URL at all
    if (!student.photoUrl && !this.photoCache[student.id]?.blobUrl) {
      console.error('❌ No photo available');
      this._showError('No photo available for this student');
      return;
    }

    // Use the blob URL string instead of SafeUrl object
    let photoUrl: string;
    
    if (this.photoCache[student.id]?.blobUrl) {
      // Use the blob URL string from cache (preferred)
      photoUrl = this.photoCache[student.id].blobUrl;
      console.log('✅ Using cached blob URL:', photoUrl);
      
      // Verify blob URL is still valid
      if (!photoUrl.startsWith('blob:')) {
        console.error('❌ Invalid blob URL format');
        this._showError('Photo URL is invalid. Please refresh the page.');
        return;
      }
    } else if (student.photoUrl) {
      // Use the server URL as fallback
      photoUrl = student.photoUrl.startsWith('http')
        ? student.photoUrl
        : `${this._apiBaseUrl}${student.photoUrl}`;
      console.log('✅ Using server URL:', photoUrl);
      console.warn('⚠️ Using direct URL instead of cached blob - may have CORS issues');
    } else {
      console.error('❌ Could not determine photo URL');
      this._showError('No photo available for this student');
      return;
    }
    
    console.log('Final photo URL:', photoUrl);
    console.log('URL type:', typeof photoUrl);
    console.log('URL length:', photoUrl.length);
    console.log('===========================');
    
    const dialogRef = this._dialog.open(PhotoViewerDialogComponent, {
      width: '90vw',
      maxWidth: '1400px',
      height: '90vh',
      maxHeight: '900px',
      panelClass: 'photo-viewer-dialog',
      disableClose: false,
      data: {
        photoUrl: photoUrl,
        studentName: student.fullName,
        admissionNumber: student.admissionNumber,
        additionalInfo: `${this.getGenderName(student.gender)} • ${this.getCBCLevelName(student.cbcLevel)} • ${student.currentLevel || 'N/A'}`
      }
    });
    
    console.log('Dialog opened');
  }

  toggleActive(student: StudentDto): void {
    const newStatus = !student.isActive;
    const action = newStatus ? 'activate' : 'deactivate';
    
    const confirmation = this._confirmation.open({
      title: `${newStatus ? 'Activate' : 'Deactivate'} Student`,
      message: `Are you sure you want to ${action} ${student.fullName}?`,
      icon: {
        name: newStatus ? 'check_circle' : 'block',
        color: newStatus ? 'success' : 'warn',
      },
      actions: {
        confirm: {
          label: newStatus ? 'Activate' : 'Deactivate',
          color: newStatus ? 'primary' : 'warn',
        },
        cancel: {
          label: 'Cancel',
        },
      },
    });

    confirmation.afterClosed().pipe(takeUntil(this._unsubscribe)).subscribe(result => {
      if (result === 'confirmed') {
        const payload: Partial<StudentDto> = { isActive: newStatus };
        
        this._service.updatePartial(student.id, payload)
          .pipe(takeUntil(this._unsubscribe))
          .subscribe({
            next: () => {
              this._showSuccess(`Student ${action}d successfully`);
              this.loadAll();
            },
            error: (err) => {
              console.error('Failed to update student status:', err);
              this._showError(err.error?.message || `Failed to ${action} student`);
            }
          });
      }
    });
  }

  removeStudent(student: StudentDto): void {
    const confirmation = this._confirmation.open({
      title: 'Delete Student',
      message: `Are you sure you want to delete ${student.fullName}? This action cannot be undone.`,
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
        this._service.delete(student.id)
          .pipe(takeUntil(this._unsubscribe))
          .subscribe({
            next: () => {
              this._showSuccess('Student deleted successfully');
              
              if (this.photoCache[student.id]?.blobUrl) {
                URL.revokeObjectURL(this.photoCache[student.id].blobUrl);
              }
              delete this.photoCache[student.id];
              
              if (this.paginatedData.length === 0 && this.currentPage > 1) {
                this.currentPage--;
              }
              
              this.loadAll();
            },
            error: (err) => {
              console.error('Failed to delete student:', err);
              this._showError(err.error?.message || 'Failed to delete student');
            }
          });
      }
    });
  }

  uploadPhoto(student: StudentDto): void {
    this._photoTargetStudent = student;
    this.photoInputRef.nativeElement.value = '';
    this.photoInputRef.nativeElement.click();
  }

  onPhotoFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length || !this._photoTargetStudent) return;
    
    const file = input.files[0];
    
    if (file.size > 5 * 1024 * 1024) {
      this._showError('File size must be less than 5MB');
      return;
    }

    if (!file.type.startsWith('image/')) {
      this._showError('File must be an image');
      return;
    }

    this._service.uploadPhoto(this._photoTargetStudent.id, file)
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next: () => {
          this._showSuccess('Photo uploaded successfully');
          this.clearAndReloadImage(this._photoTargetStudent!.id, this._photoTargetStudent!.photoUrl || '');
          this.loadAll();
        },
        error: (err) => {
          console.error('Failed to upload photo:', err);
          this._showError(err.error?.message || 'Failed to upload photo');
        }
      });
  }

  private _showSuccess(message: string): void {
    this._snackBar.open(message, 'Close', { 
      duration: 3000, 
      panelClass: ['bg-green-600', 'text-white'] 
    });
  }

  private _showError(message: string): void {
    this._snackBar.open(message, 'Close', { 
      duration: 5000, 
      panelClass: ['bg-red-600', 'text-white'] 
    });
  }
}