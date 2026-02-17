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
import { ExportConfig, ExportStudentsDialogComponent } from 'app/dialog-modals/Student/export-students-dialog.component';
import { StudentReportService } from 'app/core/DevKenService/administration/students/StudentExportService ';


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
  @ViewChild('photoInput')   photoInputRef!: ElementRef<HTMLInputElement>;
  @ViewChild('studentCell')  studentCellTemplate!: TemplateRef<any>;
  @ViewChild('numberCell')   numberCellTemplate!: TemplateRef<any>;
  @ViewChild('contactCell')  contactCellTemplate!: TemplateRef<any>;
  @ViewChild('academicCell') academicCellTemplate!: TemplateRef<any>;
  @ViewChild('guardianCell') guardianCellTemplate!: TemplateRef<any>;
  @ViewChild('statusCell')   statusCellTemplate!: TemplateRef<any>;
  @ViewChild('schoolCell')   schoolCellTemplate!: TemplateRef<any>;

    isDownloadingReport = false;

  private _unsubscribe          = new Subject<void>();
  private _apiBaseUrl           = inject(API_BASE_URL);
  private _http                 = inject(HttpClient);
  private _sanitizer            = inject(DomSanitizer);
  private _authService          = inject(AuthService);
  private _schoolService        = inject(SchoolService);
  private _router               = inject(Router);
  private _reportService  = inject(StudentReportService);
  private _alertService         = inject(AlertService);

  // ─── Breadcrumbs ─────────────────────────────────────────────────────────

  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Academic',  url: '/academic'  },
    { label: 'Students' }
  ];

  // ─── Auth ─────────────────────────────────────────────────────────────────

  get isSuperAdmin(): boolean {
    return this._authService.authUser?.isSuperAdmin ?? false;
  }

  // ─── Schools ──────────────────────────────────────────────────────────────

  schools: SchoolDto[] = [];

  get schoolsCount(): number {
    return new Set(this.allData.map(s => s.schoolId)).size;
  }

  // ─── Stats ────────────────────────────────────────────────────────────────

  get statsCards(): StatCard[] {
    const cards: StatCard[] = [
      { label: 'Total Students', value: this.total,       icon: 'groups',       iconColor: 'indigo' },
      { label: 'Active',         value: this.activeCount, icon: 'check_circle', iconColor: 'green'  },
      { label: 'Male',           value: this.maleCount,   icon: 'male',         iconColor: 'blue'   },
      { label: 'Female',         value: this.femaleCount, icon: 'female',       iconColor: 'pink'   },
    ];
    if (this.isSuperAdmin) {
      cards.push({ label: 'Schools', value: this.schoolsCount, icon: 'school', iconColor: 'green' });
    }
    return cards;
  }

  // ─── Table Columns ────────────────────────────────────────────────────────

  get tableColumns(): TableColumn<StudentDto>[] {
    const cols: TableColumn<StudentDto>[] = [
      { id: 'student', label: 'Student',       align: 'left', sortable: true },
      { id: 'number',  label: 'Admission No.', align: 'left', hideOnMobile: true },
    ];
    if (this.isSuperAdmin) {
      cols.push({ id: 'school', label: 'School', align: 'left', hideOnMobile: true });
    }
    cols.push(
      { id: 'contact',  label: 'Guardian Contact', align: 'left',   hideOnMobile: true },
      { id: 'academic', label: 'Academic Details',  align: 'left',   hideOnTablet: true },
      { id: 'guardian', label: 'Guardian',          align: 'left',   hideOnTablet: true },
      { id: 'status',   label: 'Status',            align: 'center' }
    );
    return cols;
  }

  // ─── Table Actions ────────────────────────────────────────────────────────

  tableActions: TableAction<StudentDto>[] = [
    { id: 'view',        label: 'View Details', icon: 'visibility',   color: 'blue',   handler: s => this.viewStudent(s)  },
    { id: 'edit',        label: 'Edit',         icon: 'edit',         color: 'indigo', handler: s => this.editStudent(s)  },
    { id: 'uploadPhoto', label: 'Upload Photo', icon: 'photo_camera', color: 'violet', handler: s => this.uploadPhoto(s)  },
    {
      id: 'toggleActive', label: 'Deactivate', icon: 'block', color: 'amber',
      handler: s => this.toggleActive(s),
      visible: s => s.isActive,
    },
    {
      id: 'activate', label: 'Activate', icon: 'check_circle', color: 'green', divider: true,
      handler: s => this.toggleActive(s),
      visible: s => !s.isActive,
    },
    { id: 'delete', label: 'Delete', icon: 'delete', color: 'red', handler: s => this.removeStudent(s) },
  ];

  tableHeader: TableHeader = {
    title:        'Students List',
    subtitle:     '',
    icon:         'table_chart',
    iconGradient: 'bg-gradient-to-br from-indigo-500 via-violet-600 to-purple-700',
  };

  tableEmptyState: TableEmptyState = {
    icon:        'person_search',
    message:     'No students found',
    description: 'Try adjusting your filters or enroll a new student',
    action: { label: 'Enroll First Student', icon: 'person_add', handler: () => this.enrollStudent() },
  };

  // ─── State ────────────────────────────────────────────────────────────────

  cellTemplates: { [key: string]: TemplateRef<any> } = {};
  filterFields:  FilterField[] = [];
  showFilterPanel = false;
  allData:      StudentDto[] = [];
  isLoading     = false;
  isEnumLoading = true;
  photoCache: { [key: string]: PhotoCacheEntry } = {};

  private _photoTargetStudent: StudentDto | null = null;

  private _filterValues = {
    search: '', status: 'all', gender: 'all',
    cbcLevel: 'all', studentStatus: 'all', schoolId: 'all',
  };

  currentPage  = 1;
  itemsPerPage = 10;

  genders$!:         Observable<EnumItemDto[]>;
  studentStatuses$!: Observable<EnumItemDto[]>;
  cbcLevels$!:       Observable<EnumItemDto[]>;

  private enumMaps: EnumMaps = {
    genderValueToName:        new Map<number, string>(),
    genderNameToValue:        new Map<string, number>(),
    studentStatusValueToName: new Map<number, string>(),
    studentStatusNameToValue: new Map<string, number>(),
    cbcLevelValueToName:      new Map<number, string>(),
    cbcLevelNameToValue:      new Map<string, number>(),
  };

  // ─── Computed ─────────────────────────────────────────────────────────────

  get total():       number { return this.allData.length; }
  get activeCount(): number { return this.allData.filter(s => s.isActive).length; }

  get maleCount(): number {
    const v = this.enumMaps.genderNameToValue.get('Male');
    return this.allData.filter(s => v !== undefined ? Number(s.gender) === v : s.gender === 'Male').length;
  }

  get femaleCount(): number {
    const v = this.enumMaps.genderNameToValue.get('Female');
    return this.allData.filter(s => v !== undefined ? Number(s.gender) === v : s.gender === 'Female').length;
  }

  get filteredData(): StudentDto[] {
    return this.allData.filter(s => {
      const q           = this._filterValues.search.toLowerCase();
      const genderName  = this._resolveEnum(s.gender,        this.enumMaps.genderValueToName);
      const levelName   = this._resolveEnum(s.cbcLevel,      this.enumMaps.cbcLevelValueToName);
      const statusName  = this._resolveEnum(s.studentStatus, this.enumMaps.studentStatusValueToName);

      return (
        (!q || s.fullName?.toLowerCase().includes(q) ||
               s.admissionNumber?.toLowerCase().includes(q) ||
               s.nemisNumber?.toString().includes(q)) &&
        (this._filterValues.status === 'all' ||
          (this._filterValues.status === 'active'   &&  s.isActive) ||
          (this._filterValues.status === 'inactive' && !s.isActive)) &&
        (this._filterValues.gender        === 'all' || genderName === this._filterValues.gender) &&
        (this._filterValues.cbcLevel      === 'all' || levelName  === this._filterValues.cbcLevel) &&
        (this._filterValues.studentStatus === 'all' || statusName === this._filterValues.studentStatus) &&
        (this._filterValues.schoolId      === 'all' || s.schoolId === this._filterValues.schoolId)
      );
    });
  }

  get paginatedData(): StudentDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  constructor(
    private _service:      StudentService,
    private _enumService:  EnumService,
    private _dialog:       MatDialog,
  
    private _confirmation: FuseConfirmationService,
  ) {}

  // ─── Lifecycle ────────────────────────────────────────────────────────────

  ngOnInit(): void { this.loadEnumsAndInit(); }

  ngAfterViewInit(): void {
    this.cellTemplates = {
      student:  this.studentCellTemplate,
      number:   this.numberCellTemplate,
      school:   this.schoolCellTemplate,
      contact:  this.contactCellTemplate,
      academic: this.academicCellTemplate,
      guardian: this.guardianCellTemplate,
      status:   this.statusCellTemplate,
    };
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
    Object.values(this.photoCache).forEach(e => {
      if (e.blobUrl) try { URL.revokeObjectURL(e.blobUrl); } catch { /* silent */ }
    });
  }

  // ─── Init ─────────────────────────────────────────────────────────────────

  private loadEnumsAndInit(): void {
    this.isEnumLoading = true;

    this.genders$ = this._enumService.getGenders().pipe(
      map(items => { this._buildMaps(items, this.enumMaps.genderValueToName, this.enumMaps.genderNameToValue); return items; }),
      takeUntil(this._unsubscribe)
    );
    this.studentStatuses$ = this._enumService.getStudentStatuses().pipe(
      map(items => { this._buildMaps(items, this.enumMaps.studentStatusValueToName, this.enumMaps.studentStatusNameToValue); return items; }),
      takeUntil(this._unsubscribe)
    );
    this.cbcLevels$ = this._enumService.getCBCLevels().pipe(
      map(items => { this._buildMaps(items, this.enumMaps.cbcLevelValueToName, this.enumMaps.cbcLevelNameToValue); return items; }),
      takeUntil(this._unsubscribe)
    );

    const requests: any = {
      genders: this.genders$, studentStatuses: this.studentStatuses$, cbcLevels: this.cbcLevels$,
    };
    if (this.isSuperAdmin) {
      requests.schools = this._schoolService.getAll().pipe(
        catchError(() => of({ success: false, message: '', data: [] }))
      );
    }

    forkJoin(requests)
      .pipe(takeUntil(this._unsubscribe), finalize(() => { this.isEnumLoading = false; }))
      .subscribe({
        next: (results: any) => {
          if (results.schools) this.schools = results.schools.data ?? [];
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

  private _buildMaps(
    items:       EnumItemDto[],
    valueToName: Map<number, string>,
    nameToValue: Map<string, number>
  ): void {
    items.forEach(item => {
      if (item.value !== undefined && item.name) {
        valueToName.set(item.value, item.name);
        nameToValue.set(item.name, item.value);
        nameToValue.set(item.name.toLowerCase(), item.value);
      }
    });
  }

  private _resolveEnum(raw: string | number | undefined, map: Map<number, string>): string {
    if (raw === undefined || raw === null) return '';
    if (typeof raw === 'string' && isNaN(Number(raw))) return raw;
    const n = typeof raw === 'string' ? parseInt(raw, 10) : raw;
    return map.get(n) ?? raw.toString();
  }

  private initializeFilterFields(
    genders:  EnumItemDto[],
    statuses: EnumItemDto[],
    levels:   EnumItemDto[]
  ): void {
    this.filterFields = [{
      id: 'search', label: 'Search', type: 'text',
      placeholder: 'Name, admission number, NEMIS...', value: this._filterValues.search,
    }];

    if (this.isSuperAdmin) {
      this.filterFields.push({
        id: 'schoolId', label: 'School', type: 'select', value: this._filterValues.schoolId,
        options: [
          { label: 'All Schools', value: 'all' },
          // ✅ Fixed: SchoolDto.phoneNumber  (was s.phone – field does not exist)
          ...this.schools.map(s => ({
            label: s.phoneNumber ? `${s.name} (${s.phoneNumber})` : s.name,
            value: s.id,
          })),
        ],
      });
    }

    this.filterFields.push(
      {
        id: 'status', label: 'Status', type: 'select', value: this._filterValues.status,
        options: [
          { label: 'All Statuses', value: 'all' },
          { label: 'Active',       value: 'active'   },
          { label: 'Inactive',     value: 'inactive' },
        ],
      },
      {
        id: 'gender', label: 'Gender', type: 'select', value: this._filterValues.gender,
        options: [{ label: 'All Genders', value: 'all' }, ...genders.map(g => ({ label: g.name, value: g.name }))],
      },
      {
        id: 'cbcLevel', label: 'CBC Level', type: 'select', value: this._filterValues.cbcLevel,
        options: [{ label: 'All Levels', value: 'all' }, ...levels.map(l => ({ label: l.name, value: l.name }))],
      },
      {
        id: 'studentStatus', label: 'Student Status', type: 'select', value: this._filterValues.studentStatus,
        options: [{ label: 'All Types', value: 'all' }, ...statuses.map(s => ({ label: s.name, value: s.name }))],
      }
    );
  }

  // ─── Enum helpers ─────────────────────────────────────────────────────────

  getGenderName(v: string | number | undefined):        string { return this._resolveEnum(v, this.enumMaps.genderValueToName); }
  getCBCLevelName(v: string | number | undefined):      string { return this._resolveEnum(v, this.enumMaps.cbcLevelValueToName); }
  getStudentStatusName(v: string | number | undefined): string { return this._resolveEnum(v, this.enumMaps.studentStatusValueToName); }

  // ─── Filter events ────────────────────────────────────────────────────────

  toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

  onFilterChange(event: FilterChangeEvent): void {
    (this._filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} students found`;
    if (event.filterId === 'schoolId' && this.isSuperAdmin) {
      this.loadAll(event.value === 'all' ? null : event.value);
    }
  }

  onClearFilters(): void {
    this._filterValues = { search: '', status: 'all', gender: 'all', cbcLevel: 'all', studentStatus: 'all', schoolId: 'all' };
    this.filterFields.forEach(f => { f.value = (this._filterValues as any)[f.id]; });
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} students found`;
    this.loadAll();
  }

  onPageChange(page: number):      void { this.currentPage  = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

  // ─── Data ─────────────────────────────────────────────────────────────────

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
        error: err => { this.isLoading = false; this._showError(err.error?.message || 'Failed to load students'); }
      });
  }

  // ─── Photo cache ──────────────────────────────────────────────────────────

  private loadStudentPhoto(studentId: string, photoUrl: string): Promise<void> {
    return new Promise((resolve, reject) => {
      this.photoCache[studentId] = { url: null!, blobUrl: '', isLoading: true, error: false };
      const url     = photoUrl.startsWith('http') ? photoUrl : `${this._apiBaseUrl}${photoUrl}`;
      const token   = this._authService.accessToken;
      const headers = token ? new HttpHeaders().set('Authorization', `Bearer ${token}`) : undefined;

      this._http.get(url, { responseType: 'blob', headers })
        .pipe(
          takeUntil(this._unsubscribe),
          catchError(() => {
            this.photoCache[studentId] = { url: null!, blobUrl: '', isLoading: false, error: true };
            reject(new Error('Failed to load photo'));
            return of(null);
          })
        )
        .subscribe(blob => {
          if (blob && blob.size > 0) {
            try { URL.revokeObjectURL(this.photoCache[studentId]?.blobUrl); } catch { /* silent */ }
            const blobUrl = URL.createObjectURL(blob);
            this.photoCache[studentId] = {
              url: this._sanitizer.bypassSecurityTrustUrl(blobUrl),
              blobUrl, isLoading: false, error: false,
            };
            resolve();
          } else {
            this.photoCache[studentId] = { url: null!, blobUrl: '', isLoading: false, error: true };
            reject(new Error('Invalid blob received'));
          }
        });
    });
  }

  private preloadVisibleImages(): void {
    this.paginatedData.forEach(s => {
      if (s.photoUrl && !this.photoCache[s.id]?.blobUrl && !this.photoCache[s.id]?.isLoading) {
        this.loadStudentPhoto(s.id, s.photoUrl).catch(() => { /* silent */ });
      }
    });
  }

  private clearAndReloadImage(studentId: string, photoUrl: string | null): void {
    try { URL.revokeObjectURL(this.photoCache[studentId]?.blobUrl); } catch { /* silent */ }
    delete this.photoCache[studentId];
    if (photoUrl) setTimeout(() => this.loadStudentPhoto(studentId, photoUrl).catch(() => {}), 100);
  }

  // ─── Actions ─────────────────────────────────────────────────────────────

  enrollStudent():            void { this._router.navigate(['/academic/students/enroll']); }
  viewStudent(s: StudentDto): void { this._router.navigate(['/academic/students/details', s.id]); }
  editStudent(s: StudentDto): void { this._router.navigate(['/academic/students/edit', s.id]); }

  uploadPhoto(student: StudentDto): void {
    this._photoTargetStudent = student;
    this.photoInputRef.nativeElement.value = '';
    this.photoInputRef.nativeElement.click();
  }

  onPhotoFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length || !this._photoTargetStudent) return;
    const file = input.files[0];
    if (file.size > 5 * 1024 * 1024)    { this._alertService.error('File size must be less than 5 MB'); return; }
    if (!file.type.startsWith('image/')) { this._alertService.error('File must be an image'); return; }

    this._service.uploadPhoto(this._photoTargetStudent.id, file)
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next:  () => {
          this._alertService.success('Photo uploaded successfully');
          this.clearAndReloadImage(this._photoTargetStudent!.id, this._photoTargetStudent!.photoUrl || '');
          this.loadAll();
        },
        error: err => this._alertService.error(err.error?.message || 'Failed to upload photo')
      });
  }

  viewStudentPhoto(student: StudentDto): void {
    if (!student.photoUrl) { this._showError('No photo available for this student'); return; }
    const backendUrl = student.photoUrl.startsWith('http')
      ? student.photoUrl
      : `${this._apiBaseUrl}${student.photoUrl}`;

    this._dialog.open(PhotoViewerDialogComponent, {
      disableClose: false,
      data: {
        photoUrl:        backendUrl,
        studentName:     student.fullName,
        admissionNumber: student.admissionNumber,
        authToken:       this._authService.accessToken,
        additionalInfo: [
          this.getGenderName(student.gender),
          this.getCBCLevelName(student.cbcLevel),
          student.currentLevel || 'N/A',
        ].join(' • '),
      },
    });
  }

  getPhotoTooltip(student: StudentDto): string {
    const c = this.photoCache[student.id];
    if (!c)          return student.photoUrl ? 'Loading...' : 'No photo';
    if (c.isLoading) return 'Loading...';
    if (c.error)     return 'Failed to load. Click to retry.';
    if (c.blobUrl)   return 'Click to view';
    return 'No photo';
  }

  retryPhotoLoad(student: StudentDto): void {
    if (student.photoUrl) this.clearAndReloadImage(student.id, student.photoUrl);
  }

  toggleActive(student: StudentDto): void {
    const newStatus = !student.isActive;
    const action    = newStatus ? 'activate' : 'deactivate';

    this._alertService.confirm({
      title: `${newStatus ? 'Activate' : 'Deactivate'} Student`,
      message: `Are you sure you want to ${action} ${student.fullName}?`,
      confirmText: newStatus ? 'Activate' : 'Deactivate',
      onConfirm: () => {
        this._service.toggleStatus(student.id, newStatus).subscribe({
          next:  res => { this._alertService.success(res.message); this.loadAll(); },
          error: err => this._alertService.error(err.error?.message || `Failed to ${action} student`)
        });
      }
    });
  }

  removeStudent(student: StudentDto): void {
    this._alertService.confirm({
      title: 'Delete Student', confirmText: 'Delete', cancelText: 'Cancel',
      message: `Are you sure you want to delete ${student.fullName}? This action cannot be undone.`,
      onConfirm: () => {
        this._service.delete(student.id).subscribe({
          next: () => {
            this._alertService.success('Student deleted successfully');
            try { URL.revokeObjectURL(this.photoCache[student.id]?.blobUrl); } catch { /* silent */ }
            delete this.photoCache[student.id];
            if (this.paginatedData.length === 0 && this.currentPage > 1) this.currentPage--;
            this.loadAll();
          },
          error: err => this._alertService.error(err.error?.message || 'Failed to delete student')
        });
      }
    });
  }

  bulkUploadPhotos(): void {
    const ref = this._dialog.open(BulkPhotoUploadDialogComponent, {
      width: '900px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: false, panelClass: 'bulk-photo-upload-dialog',
    });
    ref.afterClosed().pipe(takeUntil(this._unsubscribe)).subscribe((uploaded: boolean) => {
      if (uploaded) {
        this._alertService.success('Photos uploaded successfully! Refreshing list...');
        Object.values(this.photoCache).forEach(e => {
          try { URL.revokeObjectURL(e.blobUrl); } catch { /* silent */ }
        });
        this.photoCache = {};
        this.loadAll();
      }
    });
  }
 downloadStudentsReport(): void {
    if (this.isDownloadingReport) return;

    this.isDownloadingReport = true;

    const schoolId =
      this.isSuperAdmin && this._filterValues.schoolId !== 'all'
        ? this._filterValues.schoolId
        : null;

    this._alertService.info('Generating PDF report…');

    this._reportService
      .downloadStudentsList(schoolId)
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next: () => {
          this._alertService.success('Students list report downloaded successfully');
          this.isDownloadingReport = false;
        },
        error: (err: Error) => {
          this._alertService.error(err.message || 'Failed to generate report');
          this.isDownloadingReport = false;
        },
      });
  }



  // ─── Snack helpers ────────────────────────────────────────────────────────
private _showSuccess(msg: string): void {
  this._alertService.success(msg);
}

private _showError(msg: string): void {
  this._alertService.error(msg);
}

}