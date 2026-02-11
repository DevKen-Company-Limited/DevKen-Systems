import { Component, ElementRef, OnInit, OnDestroy, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { FuseConfirmationService } from '@fuse/services/confirmation';
import { Observable, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { EnumItemDto, EnumService } from 'app/core/DevKenService/common/enum.service';
import { TeacherService } from 'app/core/DevKenService/Teacher/TeacherService';
import { TeacherDto } from 'app/core/DevKenService/Types/Teacher';
import { CreateEditTeacherDialogComponent, CreateEditTeacherDialogResult } from 'app/dialog-modals/teachers/create-edit-teacher-dialog.component';
import { API_BASE_URL } from 'app/app.config';
import { HttpClient } from '@angular/common/http';

// Import reusable components
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';


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
  ],
  templateUrl: './teachers.component.html',
})
export class TeachersComponent implements OnInit, OnDestroy {
  @ViewChild('photoInput') photoInputRef!: ElementRef<HTMLInputElement>;

  private _unsubscribe = new Subject<void>();
  private _apiBaseUrl = inject(API_BASE_URL);
  private _http = inject(HttpClient);

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

  // ── Filter Fields Configuration ──────────────────────────────────────────────
  filterFields: FilterField[] = [];
  showFilterPanel = false;

  // ── State ────────────────────────────────────────────────────────────────────
  allData: TeacherDto[] = [];
  isLoading = false;
  photoCache: { [key: string]: string } = {};
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

      const matchesSearch = !q ||
        t.fullName.toLowerCase().includes(q) ||
        t.teacherNumber.toLowerCase().includes(q) ||
        t.email.toLowerCase().includes(q) ||
        t.phoneNumber.toLowerCase().includes(q);

      const matchesStatus =
        this._filterValues.status === 'all' ||
        (this._filterValues.status === 'active' && t.isActive) ||
        (this._filterValues.status === 'inactive' && !t.isActive);

      const matchesEmployment =
        this._filterValues.employmentType === 'all' ||
        t.employmentType === this._filterValues.employmentType;

      const matchesRole =
        this._filterValues.role === 'all' ||
        (this._filterValues.role === 'classTeacher' && t.isClassTeacher) ||
        (this._filterValues.role === 'subject' && !t.isClassTeacher);

      return matchesSearch && matchesStatus && matchesEmployment && matchesRole;
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

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
    // Clean up all cached photo URLs
    Object.values(this.photoCache).forEach(url => URL.revokeObjectURL(url));
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
    this.currentPage = 1; // Reset to first page when filters change
  }

  onClearFilters(): void {
    this._filterValues = {
      search: '',
      status: 'all',
      employmentType: 'all',
      role: 'all',
    };
    
    // Update filter field values
    this.filterFields.forEach(field => {
      field.value = (this._filterValues as any)[field.id];
    });
    
    this.currentPage = 1;
  }

  // ── Pagination Handlers ──────────────────────────────────────────────────────
  onPageChange(page: number): void {
    this.currentPage = page;
  }

  onItemsPerPageChange(itemsPerPage: number): void {
    this.itemsPerPage = itemsPerPage;
    this.currentPage = 1; // Reset to first page
  }

  // ── Image Preloading ──────────────────────────────────────────────────────────
  private preloadImages(): void {
    this.allData.forEach(teacher => {
      if (!teacher.photoUrl) return;
      if (this.photoCache[teacher.id]) return;

      const url = teacher.photoUrl.startsWith('http')
        ? teacher.photoUrl
        : `${this._apiBaseUrl}${teacher.photoUrl}`;

      this._http.get(url, { responseType: 'blob' })
        .pipe(takeUntil(this._unsubscribe))
        .subscribe({
          next: blob => {
            const objectUrl = URL.createObjectURL(blob);
            this.photoCache[teacher.id] = objectUrl;
          },
          error: err => {
            console.error('Image load failed for teacher:', teacher.id, err);
          }
        });
    });
  }

  // ── Clear and Reload Single Image ────────────────────────────────────────────
  private clearAndReloadImage(teacherId: string, photoUrl: string | null): void {
    if (this.photoCache[teacherId]) {
      URL.revokeObjectURL(this.photoCache[teacherId]);
      delete this.photoCache[teacherId];
    }

    if (!photoUrl) return;

    const cacheBustedUrl = `${photoUrl}?t=${Date.now()}`;

    this._http.get(cacheBustedUrl, { responseType: 'blob' })
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next: blob => {
          const objectUrl = URL.createObjectURL(blob);
          this.photoCache[teacherId] = objectUrl;
        },
        error: err => {
          console.error('Image reload failed for teacher:', teacherId, err);
        }
      });
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
            this.preloadImages();
          }
          this.isLoading = false;
        },
        error: err => {
          console.error('Failed to load teachers:', err);
          this.isLoading = false;
        }
      });
  }

  // ── CRUD ──────────────────────────────────────────────────────────────────────
  openCreate(): void {
    const dialogRef = this._dialog.open(CreateEditTeacherDialogComponent, {
      width: '720px',
      maxHeight: '90vh',
      data: { mode: 'create' },
    });

    dialogRef.afterClosed()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe((result: CreateEditTeacherDialogResult | null) => {
        if (!result) return;

        this._service.create(result.formData)
          .pipe(takeUntil(this._unsubscribe))
          .subscribe({
            next: res => {
              if (res.success) {
                if (result.photoFile) {
                  this._uploadPhoto(res.data.id, result.photoFile, () => this.loadAll());
                } else {
                  this._showSuccess('Teacher created successfully');
                  this.loadAll();
                }
              }
            },
            error: err => {
              console.error('Create teacher failed:', err);
              this._showError(err.error?.message || 'Failed to create teacher');
            },
          });
      });
  }

  openEdit(teacher: TeacherDto): void {
    const dialogRef = this._dialog.open(CreateEditTeacherDialogComponent, {
      width: '720px',
      maxHeight: '90vh',
      data: { mode: 'edit', teacher },
    });

    dialogRef.afterClosed()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe((result: CreateEditTeacherDialogResult | null) => {
        if (!result) return;

        this._service.update(teacher.id, result.formData)
          .pipe(takeUntil(this._unsubscribe))
          .subscribe({
            next: res => {
              if (res.success) {
                if (result.photoFile) {
                  this._uploadPhoto(teacher.id, result.photoFile, () => this.loadAll());
                } else {
                  this._showSuccess('Teacher updated successfully');
                  
                  let photoUrl = res.data.photoUrl;
                  if (photoUrl && !photoUrl.startsWith('http')) {
                    photoUrl = `${this._apiBaseUrl}${photoUrl}`;
                  }
                  
                  this.clearAndReloadImage(teacher.id, photoUrl);
                  this.loadAll();
                }
              }
            },
            error: err => {
              console.error('Update teacher failed:', err);
              this._showError(err.error?.message || 'Failed to update teacher');
            },
          });
      });
  }

  toggleActive(teacher: TeacherDto): void {
    const action = teacher.isActive ? 'deactivate' : 'activate';
    const label = teacher.isActive ? 'Deactivate' : 'Activate';
    const color = teacher.isActive ? 'warn' : 'primary';

    this._confirmation.open({
      title: `${label} Teacher`,
      message: `Are you sure you want to ${action} "${teacher.fullName}"?`,
      actions: { confirm: { label, color } },
    }).afterClosed()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe(result => {
        if (result !== 'confirmed') return;

        const payload = { ...teacher, isActive: !teacher.isActive };
        this._service.update(teacher.id, payload as any)
          .pipe(takeUntil(this._unsubscribe))
          .subscribe({
            next: res => {
              if (res.success) {
                this._showSuccess(`Teacher ${action}d successfully`);
                this.loadAll();
              }
            },
            error: err => {
              console.error(`Failed to ${action} teacher:`, err);
              this._showError(err.error?.message || `Failed to ${action} teacher`);
            },
          });
      });
  }

  removeTeacher(teacher: TeacherDto): void {
    this._confirmation.open({
      title: 'Delete Teacher',
      message: `Are you sure you want to permanently delete "${teacher.fullName}" (${teacher.teacherNumber})? This cannot be undone.`,
      actions: { confirm: { label: 'Delete', color: 'warn' } },
    }).afterClosed()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe(result => {
        if (result !== 'confirmed') return;

        this._service.delete(teacher.id)
          .pipe(takeUntil(this._unsubscribe))
          .subscribe({
            next: res => {
              if (res.success) {
                if (this.photoCache[teacher.id]) {
                  URL.revokeObjectURL(this.photoCache[teacher.id]);
                  delete this.photoCache[teacher.id];
                }
                
                this._showSuccess('Teacher deleted successfully');
                this.loadAll();
              }
            },
            error: err => {
              console.error('Delete teacher failed:', err);
              this._showError(err.error?.message || 'Failed to delete teacher');
            },
          });
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
    const id = this._photoTargetTeacher.id;
    this._uploadPhoto(id, file, () => this.loadAll());
    this._photoTargetTeacher = null;
  }

  private _uploadPhoto(teacherId: string, file: File, onSuccess: () => void): void {
    this._service.uploadPhoto(teacherId, file)
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next: res => {
          if (res.success) {
            this._showSuccess('Photo uploaded successfully');
            
            if (this.photoCache[teacherId]) {
              URL.revokeObjectURL(this.photoCache[teacherId]);
              delete this.photoCache[teacherId];
            }
            
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
      panelClass: ['snack-success'] 
    });
  }

  private _showError(message: string): void {
    this._snackBar.open(message, 'Close', { 
      duration: 5000, 
      panelClass: ['snack-error'] 
    });
  }
}