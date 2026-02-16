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
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';

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
  private _alertService = inject(AlertService);

  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Academic', url: '/academic' },
    { label: 'Students' }
  ];

  get isSuperAdmin(): boolean {
    return this._authService.authUser?.isSuperAdmin ?? false;
  }

  schools: SchoolDto[] = [];
  
  get schoolsCount(): number {
    const uniqueSchools = new Set(this.allData.map(s => s.schoolId));
    return uniqueSchools.size;
  }

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
  filterFields: FilterField[] = [];
  showFilterPanel = false;
  allData: StudentDto[] = [];
  isLoading = false;
  isEnumLoading = true;
  photoCache: { [key: string]: PhotoCacheEntry } = {};
  private _photoTargetStudent: StudentDto | null = null;

  private _filterValues = {
    search: '',
    status: 'all',
    gender: 'all',
    cbcLevel: 'all',
    studentStatus: 'all',
    schoolId: 'all',
  };

  currentPage = 1;
  itemsPerPage = 10;

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
        try {
          URL.revokeObjectURL(entry.blobUrl);
        } catch (error) {
          // Silently handle
        }
      }
    });
  }

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
        catchError(() => of({ success: false, message: '', data: [] }))
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
    error: () => {
      this._alertService.error('Failed to load configuration data');
      this.isEnumLoading = false;
      this.loadAll();
    }
  });
}

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

  onPageChange(page: number): void {
    this.currentPage = page;
  }

  onItemsPerPageChange(itemsPerPage: number): void {
    this.itemsPerPage = itemsPerPage;
    this.currentPage = 1;
  }

  private loadStudentPhoto(studentId: string, photoUrl: string): Promise<void> {
    return new Promise((resolve, reject) => {
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

      const token = this._authService.accessToken;
      const headers = token ? new HttpHeaders().set('Authorization', `Bearer ${token}`) : undefined;

      this._http.get(url, { 
        responseType: 'blob',
        headers 
      }).pipe(
        takeUntil(this._unsubscribe),
        catchError(() => {
          this.photoCache[studentId] = {
            url: null!,
            blobUrl: '',
            isLoading: false,
            error: true
          };
          reject(new Error('Failed to load photo'));
          return of(null);
        })
      ).subscribe(blob => {
        if (blob && blob.size > 0) {
          if (this.photoCache[studentId]?.blobUrl) {
            try {
              URL.revokeObjectURL(this.photoCache[studentId].blobUrl);
            } catch (error) {
              // Silently handle
            }
          }

          const blobUrl = URL.createObjectURL(blob);
          const safeUrl = this._sanitizer.bypassSecurityTrustUrl(blobUrl);
          
          this.photoCache[studentId] = {
            url: safeUrl,
            blobUrl: blobUrl,
            isLoading: false,
            error: false
          };
          
          resolve();
        } else {
          this.photoCache[studentId] = {
            url: null!,
            blobUrl: '',
            isLoading: false,
            error: true
          };
          reject(new Error('Invalid blob received'));
        }
      });
    });
  }

  private preloadVisibleImages(): void {
    this.paginatedData.forEach(student => {
      if (student.photoUrl && !this.photoCache[student.id]?.blobUrl && !this.photoCache[student.id]?.isLoading) {
        this.loadStudentPhoto(student.id, student.photoUrl).catch(() => {
          // Silently handle preload errors
        });
      }
    });
  }

  private clearAndReloadImage(studentId: string, photoUrl: string | null): void {
    if (this.photoCache[studentId]?.blobUrl) {
      try {
        URL.revokeObjectURL(this.photoCache[studentId].blobUrl);
      } catch (error) {
        // Silently handle
      }
    }
    
    delete this.photoCache[studentId];

    if (photoUrl) {
      setTimeout(() => {
        this.loadStudentPhoto(studentId, photoUrl).catch(() => {
          // Silently handle reload errors
        });
      }, 100);
    }
  }

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
          this.isLoading = false;
          this._showError(err.error?.message || 'Failed to load students');
        }
      });
  }

  enrollStudent(): void {
    this._router.navigate(['/academic/students/enroll']);
  }

  viewStudent(student: StudentDto): void {
    this._router.navigate(['/academic/students/details', student.id]);
  }

  editStudent(student: StudentDto): void {
    this._router.navigate(['/academic/students/edit', student.id]);
  }

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
        this._alertService.success('Photos uploaded successfully! Refreshing list...');
        Object.keys(this.photoCache).forEach(key => {
          if (this.photoCache[key].blobUrl) {
            try {
              URL.revokeObjectURL(this.photoCache[key].blobUrl);
            } catch (error) {
              // Silently handle
            }
          }
        });
        this.photoCache = {};
        this.loadAll();
      }
    });
}

viewStudentPhoto(student: StudentDto): void {
  if (!student.photoUrl) {
    this._showError('No photo available for this student');
    return;
  }

  // Build full backend URL
  const backendUrl = student.photoUrl.startsWith('http')
    ? student.photoUrl
    : `${this._apiBaseUrl}${student.photoUrl}`;

  // Get auth token from auth service
  const token = this._authService.accessToken;

  // Open the photo viewer dialog
  this._dialog.open(PhotoViewerDialogComponent, {
    disableClose: false,
    data: {
      photoUrl: backendUrl,              // Backend URL, not a blob
      studentName: student.fullName,
      admissionNumber: student.admissionNumber,
      authToken: token,                  // Optional, for authorization
      additionalInfo: [
        this.getGenderName(student.gender),
        this.getCBCLevelName(student.cbcLevel),
        student.currentLevel || 'N/A'
      ].join(' • ')
    }
  });
}


  getPhotoTooltip(student: StudentDto): string {
    const cache = this.photoCache[student.id];
    
    if (!cache) {
      return student.photoUrl ? 'Loading...' : 'No photo';
    }
    
    if (cache.isLoading) return 'Loading...';
    if (cache.error) return 'Failed to load. Click to retry.';
    if (cache.blobUrl) return 'Click to view';
    
    return 'No photo';
  }

  retryPhotoLoad(student: StudentDto): void {
    if (student.photoUrl) {
      this.clearAndReloadImage(student.id, student.photoUrl);
    }
  }

toggleActive(student: StudentDto): void {
  const newStatus = !student.isActive;
  const action = newStatus ? 'activate' : 'deactivate';

  this._alertService.confirm({
    title: `${newStatus ? 'Activate' : 'Deactivate'} Student`,
    message: `Are you sure you want to ${action} ${student.fullName}?`,
    confirmText: newStatus ? 'Activate' : 'Deactivate',
    onConfirm: () => {

      this._service.toggleStatus(student.id, newStatus)
        .subscribe({
          next: (response) => {
            this._alertService.success(response.message);
            this.loadAll();
          },
          error: (err) => {
            this._alertService.error(
              err.error?.message || `Failed to ${action} student`
            );
          }
        });

    }
  });
}


removeStudent(student: StudentDto): void {
  this._alertService.confirm({
    title: 'Delete Student',
    message: `Are you sure you want to delete ${student.fullName}? This action cannot be undone.`,
    confirmText: 'Delete',
    cancelText: 'Cancel',
    onConfirm: () => {
      this._service.delete(student.id)
        .subscribe({
          next: () => {
            this._alertService.success('Student deleted successfully');

            if (this.photoCache[student.id]?.blobUrl) {
              try {
                URL.revokeObjectURL(this.photoCache[student.id].blobUrl);
              } catch (error) {
                // Silently handle
              }
            }
            delete this.photoCache[student.id];

            if (this.paginatedData.length === 0 && this.currentPage > 1) {
              this.currentPage--;
            }

            this.loadAll();
          },
          error: (err) => {
            this._alertService.error(err.error?.message || 'Failed to delete student');
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
    this._alertService.error('File size must be less than 5MB');
    return;
  }

  if (!file.type.startsWith('image/')) {
    this._alertService.error('File must be an image');
    return;
  }

  this._service.uploadPhoto(this._photoTargetStudent.id, file)
    .pipe(takeUntil(this._unsubscribe))
    .subscribe({
      next: () => {
        this._alertService.success('Photo uploaded successfully');
        this.clearAndReloadImage(this._photoTargetStudent!.id, this._photoTargetStudent!.photoUrl || '');
        this.loadAll();
      },
      error: (err) => {
        this._alertService.error(err.error?.message || 'Failed to upload photo');
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