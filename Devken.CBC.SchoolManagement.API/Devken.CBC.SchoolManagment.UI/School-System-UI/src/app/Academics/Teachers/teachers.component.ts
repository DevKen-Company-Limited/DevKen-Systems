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
import { Observable, of, Subject } from 'rxjs';
import { catchError, takeUntil } from 'rxjs/operators';
import { EnumItemDto, EnumService } from 'app/core/DevKenService/common/enum.service';
import { TeacherService } from 'app/core/DevKenService/Teacher/TeacherService';
import { TeacherDto } from 'app/core/DevKenService/Types/Teacher';
import { CreateEditTeacherDialogComponent, CreateEditTeacherDialogResult } from 'app/dialog-modals/teachers/create-edit-teacher-dialog.component';
import { API_BASE_URL } from 'app/app.config';
import { HttpClient, HttpHeaders } from '@angular/common/http';
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

interface PhotoCacheEntry {
  url: SafeUrl;
  blobUrl: string;
  isLoading: boolean;
  error: boolean;
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

  private _unsubscribe = new Subject<void>();
  private _apiBaseUrl = inject(API_BASE_URL);
  private _http = inject(HttpClient);
  private _sanitizer = inject(DomSanitizer);
  private _authService = inject(AuthService);

  // ── Breadcrumbs ──────────────────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Academic', url: '/academic' },
    { label: 'Teachers' }
  ];

  // ── Stats Cards Configuration ────────────────────────────────────────────────
  get statsCards(): StatCard[] {
    return [
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
  }

  // ── Table Configuration ──────────────────────────────────────────────────────
  tableColumns: TableColumn<TeacherDto>[] = [
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
    },
  ];

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
      label: 'Toggle Active',
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
  photoCache: { [key: string]: PhotoCacheEntry } = {};
  private _photoTargetTeacher: TeacherDto | null = null;

  // ── Filter Values ────────────────────────────────────────────────────────────
  private _filterValues = {
    search: '',
    status: 'all',
    employmentType: 'all',
    role: 'all',
  };

  // ── Pagination ───────────────────────────────────────────────────────────────
  currentPage = 1;
  itemsPerPage = 10;

  // ── Enum Observables ─────────────────────────────────────────────────────────
  employmentTypes$!: Observable<EnumItemDto[]>;

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
    return this.allData.filter(t => t.employmentType === 'Permanent').length; 
  }

  // ── Filtered Data ─────────────────────────────────────────────────────────────
  get filteredData(): TeacherDto[] {
    return this.allData.filter(t => {
      const q = this._filterValues.search.toLowerCase();
      return (
        (!q || t.fullName.toLowerCase().includes(q) || t.teacherNumber.toLowerCase().includes(q)) &&
        (this._filterValues.status === 'all' ||
          (this._filterValues.status === 'active' && t.isActive) ||
          (this._filterValues.status === 'inactive' && !t.isActive)) &&
        (this._filterValues.employmentType === 'all' ||
          t.employmentType === this._filterValues.employmentType) &&
        (this._filterValues.role === 'all' ||
          (this._filterValues.role === 'classTeacher' && t.isClassTeacher))
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
    private _snackBar: MatSnackBar,
    private _confirmation: FuseConfirmationService,
  ) {}

  ngOnInit(): void {
    this.employmentTypes$ = this._enumService.getTeacherEmploymentTypes();
    this.initializeFilterFields();
    this.loadAll();
  }

  ngAfterViewInit(): void {
    this.cellTemplates = {
      teacher: this.teacherCellTemplate,
      number: this.numberCellTemplate,
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

  // ── Initialize Filter Fields ─────────────────────────────────────────────────
  private initializeFilterFields(): void {
    this.employmentTypes$.pipe(takeUntil(this._unsubscribe)).subscribe(types => {
      this.filterFields = [
        {
          id: 'search',
          label: 'Search',
          type: 'text',
          placeholder: 'Name or teacher number...',
          value: this._filterValues.search,
        },
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
        },
      ];
    });
  }

  // ── Filter Handlers ──────────────────────────────────────────────────────────
  toggleFilterPanel(): void {
    this.showFilterPanel = !this.showFilterPanel;
  }

  onFilterChange(event: FilterChangeEvent): void {
    (this._filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} teachers found`;
  }

  onClearFilters(): void {
    this._filterValues = {
      search: '',
      status: 'all',
      employmentType: 'all',
      role: 'all',
    };
    
    this.filterFields.forEach(field => {
      field.value = (this._filterValues as any)[field.id];
    });
    
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} teachers found`;
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
    // Initialize cache entry
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
        // Revoke previous blob URL if exists
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

  // ── Preload Images for Visible Data ─────────────────────────────────────────
  private preloadVisibleImages(): void {
    this.paginatedData.forEach(teacher => {
      if (teacher.photoUrl && !this.photoCache[teacher.id]?.url) {
        this.loadTeacherPhoto(teacher.id, teacher.photoUrl);
      }
    });
  }

  // ── Clear and Reload Single Image ────────────────────────────────────────────
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
  loadAll(): void {
    this.isLoading = true;
    this._service.getAll()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next: res => {
          if (res.success) {
            this.allData = res.data;
            setTimeout(() => this.preloadVisibleImages(), 0);
          }
          this.isLoading = false;
        },
        error: () => {
          this.isLoading = false;
          this._showError('Failed to load teachers');
        }
      });
  }

  // ── CRUD ──────────────────────────────────────────────────────────────────────
  openCreate(): void {
    const dialogRef = this._dialog.open(CreateEditTeacherDialogComponent, {
      width: '720px',
      maxHeight: '90vh',
      disableClose: true,
      data: { mode: 'create' },
    });

    dialogRef.afterClosed().pipe(takeUntil(this._unsubscribe)).subscribe(result => {
      if (result) {
        this.loadAll();
      }
    });
  }

  openEdit(teacher: TeacherDto): void {
    const dialogRef = this._dialog.open(CreateEditTeacherDialogComponent, {
      width: '720px',
      maxHeight: '90vh',
      disableClose: true,
      data: { mode: 'edit', teacher },
    });

    dialogRef.afterClosed().pipe(takeUntil(this._unsubscribe)).subscribe(result => {
      if (result) {
        this.loadAll();
      }
    });
  }

  toggleActive(teacher: TeacherDto): void {
    const payload = { ...teacher, isActive: !teacher.isActive };
    this._service.update(teacher.id, payload as any)
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this._showSuccess(`Teacher ${teacher.isActive ? 'deactivated' : 'activated'} successfully`);
            this.loadAll();
          }
        },
        error: () => this._showError('Failed to update teacher status')
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
                
                // Clean up cache
                if (this.photoCache[teacher.id]?.blobUrl) {
                  URL.revokeObjectURL(this.photoCache[teacher.id].blobUrl);
                }
                delete this.photoCache[teacher.id];
                
                this.loadAll();
              }
            },
            error: () => this._showError('Failed to delete teacher')
          });
      }
    });
  }

  // ── Photo Upload ──────────────────────────────────────────────────────────────
  uploadPhoto(teacher: TeacherDto): void {
    this._photoTargetTeacher = teacher;
    this.photoInputRef.nativeElement.value = '';
    this.photoInputRef.nativeElement.click();
  }

  onPhotoFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length || !this._photoTargetTeacher) return;
    
    const file = input.files[0];
    
    // Validate file size (5MB max)
    if (file.size > 5 * 1024 * 1024) {
      this._showError('File size must be less than 5MB');
      return;
    }

    // Validate file type
    if (!file.type.startsWith('image/')) {
      this._showError('File must be an image');
      return;
    }

    this._uploadPhoto(this._photoTargetTeacher.id, file, () => {
      this._showSuccess('Photo uploaded successfully');
      this.loadAll();
    });
  }

  private _uploadPhoto(teacherId: string, file: File, onSuccess: () => void): void {
    this._service.uploadPhoto(teacherId, file)
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next: res => {
          if (res.success) {
            // Clear cache for this teacher
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

  // ── Notifications ─────────────────────────────────────────────────────────────
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