import { Component, Inject, OnInit, OnDestroy, ViewChild, TemplateRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { forkJoin, Observable, Subject, of, BehaviorSubject, combineLatest } from 'rxjs';
import { catchError, tap, takeUntil, map, finalize, shareReplay } from 'rxjs/operators';
import { TeacherDto } from 'app/core/DevKenService/Types/Teacher';
import { EnumItemDto, EnumService } from 'app/core/DevKenService/common/enum.service';
import { API_BASE_URL } from 'app/app.config';
import { 
  FormDialogComponent, 
  DialogHeader, 
  DialogTab, 
  PhotoUploadConfig, 
  DialogFooter,
  DialogSize,
  DialogTheme 
} from 'app/shared/dialogs/form/form-dialog.component';
import { AuthService } from 'app/core/auth/auth.service';
import { HttpClient, HttpHeaders } from '@angular/common/http';

export interface CreateEditTeacherDialogData {
  mode: 'create' | 'edit';
  teacher?: TeacherDto;
}

export interface CreateEditTeacherDialogResult {
  formData: any;
  photoFile?: File | null;
}

// Enum cache service to avoid duplicate requests
class EnumCacheService {
  private static instance: EnumCacheService;
  private cache = new Map<string, BehaviorSubject<EnumItemDto[]>>();
  
  static getInstance(): EnumCacheService {
    if (!this.instance) {
      this.instance = new EnumCacheService();
    }
    return this.instance;
  }
  
  getEnum(key: string, loader: () => Observable<EnumItemDto[]>): Observable<EnumItemDto[]> {
    if (!this.cache.has(key)) {
      const subject = new BehaviorSubject<EnumItemDto[]>([]);
      this.cache.set(key, subject);
      
      loader().pipe(
        catchError(() => of(this.getDefaultEnum(key))),
        tap(data => subject.next(data))
      ).subscribe();
    }
    
    return this.cache.get(key)!.asObservable();
  }
  
  private getDefaultEnum(key: string): EnumItemDto[] {
    switch(key) {
      case 'genders':
        return [
          { value: 0, name: 'Male', id: '' },
          { value: 1, name: 'Female', id: '' },
          { value: 2, name: 'Other', id: '' }
        ];
      case 'employmentTypes':
        return [
          { value: 0, name: 'Permanent', id: '' },
          { value: 1, name: 'Contract', id: '' },
          { value: 2, name: 'PartTime', id: '' },
          { value: 3, name: 'Intern', id: '' },
          { value: 4, name: 'Temporary', id: '' }
        ];
      case 'designations':
        return [
          { value: 0, name: 'Teacher', id: '' },
          { value: 1, name: 'Senior Teacher', id: '' },
          { value: 2, name: 'Head Teacher', id: '' },
          { value: 3, name: 'Deputy Head Teacher', id: '' },
          { value: 4, name: 'Subject Head', id: '' },
          { value: 5, name: 'Department Head', id: '' }
        ];
      default:
        return [];
    }
  }
}

@Component({
  selector: 'app-create-edit-teacher-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatDatepickerModule,
    MatSlideToggleModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    FormDialogComponent,
  ],
  templateUrl: './create-edit-teacher-dialog.component.html',
  styleUrls: ['./create-edit-teacher-dialog.component.scss']
})
export class CreateEditTeacherDialogComponent implements OnInit, OnDestroy {
  @ViewChild('personalTab') personalTabTemplate!: TemplateRef<any>;
  @ViewChild('contactTab') contactTabTemplate!: TemplateRef<any>;
  @ViewChild('professionalTab') professionalTabTemplate!: TemplateRef<any>;
  
  // Custom templates for advanced customization
  @ViewChild('customHeaderIcon') customHeaderIcon?: TemplateRef<any>;
  @ViewChild('customPhotoPreview') customPhotoPreview?: TemplateRef<any>;
  @ViewChild('customUploadButton') customUploadButton?: TemplateRef<any>;
  @ViewChild('loadingState') loadingStateTemplate?: TemplateRef<any>;
  @ViewChild('errorState') errorStateTemplate?: TemplateRef<any>;

  private _unsubscribe = new Subject<void>();
  private _apiBaseUrl = inject(API_BASE_URL);
  private _sanitizer = inject(DomSanitizer);
  private _authService = inject(AuthService);
  private _http = inject(HttpClient);
  private _enumCache = EnumCacheService.getInstance();

  form!: FormGroup;
  formSubmitted = false;
  activeTab = 0;
  
  photoPreview: SafeUrl | string | null = null;
  selectedPhotoFile: File | null = null;
  isLoadingPhoto = false;
  photoError = false;

  // ── Dialog Configuration ─────────────────────────────────────────────────────
  dialogHeader!: DialogHeader;
  
  dialogTabs: DialogTab[] = [
    { 
      id: 'personal', 
      label: 'Personal Information', 
      icon: 'person',
      fields: ['firstName', 'lastName', 'teacherNumber', 'gender'],
      badge: 'Required',
      badgeColor: 'bg-primary-500 text-white'
    },
    { 
      id: 'contact', 
      label: 'Contact Details', 
      icon: 'contacts',
      fields: ['email', 'phoneNumber']
    },
    { 
      id: 'professional', 
      label: 'Professional Info', 
      icon: 'work',
      fields: ['employmentType', 'designation']
    },
  ];

  // Responsive dialog size configuration
  dialogSize: DialogSize = {
    width: 'w-full',
    maxWidth: 'max-w-5xl',
    height: 'h-auto',
    maxHeight: 'max-h-[90vh]',
    minWidth: 'min-w-[320px]',
  };

  // Modern theme configuration with CSS variables for easy customization
  dialogTheme: DialogTheme = {
    header: {
      background: 'bg-gradient-to-r from-primary-600 via-primary-700 to-primary-800',
      textColor: 'text-white',
      iconBackground: 'bg-white/20 backdrop-blur-sm',
      iconColor: 'text-white',
      closeButtonColor: 'text-white/80',
      closeButtonHoverColor: 'hover:text-white hover:bg-white/10'
    },
    tabs: {
      containerBackground: 'bg-white dark:bg-gray-800',
      borderColor: 'border-gray-200 dark:border-gray-700',
      activeTab: {
        borderColor: 'border-primary-600',
        textColor: 'text-primary-600',
        backgroundColor: 'bg-primary-50 dark:bg-primary-900/20'
      },
      inactiveTab: {
        borderColor: 'border-transparent',
        textColor: 'text-gray-500 dark:text-gray-400',
        backgroundColor: 'bg-transparent',
        hoverTextColor: 'hover:text-gray-700 dark:hover:text-gray-300'
      }
    },
    content: {
      background: 'bg-gray-50/50 dark:bg-gray-900/50'
    },
    footer: {
      background: 'bg-white dark:bg-gray-800',
      borderColor: 'border-gray-200 dark:border-gray-700',
      textColor: 'text-gray-900 dark:text-gray-100'
    },
    photo: {
      containerBackground: 'bg-white dark:bg-gray-800',
      borderColor: 'border-gray-200 dark:border-gray-700',
      previewBorderColor: 'border-primary-200 dark:border-primary-800',
      removeButtonBackground: 'bg-red-500',
      removeButtonHoverBackground: 'hover:bg-red-600',
      removeButtonIconColor: 'text-white'
    }
  };

  photoConfig!: PhotoUploadConfig;
  footerConfig!: DialogFooter;
  tabTemplates: { [key: string]: TemplateRef<any> } = {};

  // ── Enum Observables with Caching ────────────────────────────────────────────
  genders$!: Observable<EnumItemDto[]>;
  employmentTypes$!: Observable<EnumItemDto[]>;
  designations$!: Observable<EnumItemDto[]>;

  // ── Loading States ───────────────────────────────────────────────────────────
  isLoading = false;
  isEnumsLoading = true;

  // ── Enum Name to Value Maps for Reverse Lookup ───────────────────────────────
  private enumValueToNameMap = new Map<string, Map<number, string>>();

  get isEditMode(): boolean {
    return this.data.mode === 'edit';
  }

  get dialogTitle(): string {
    return this.isEditMode ? 'Edit Teacher' : 'Add New Teacher';
  }

  get dialogSubtitle(): string {
    return this.isEditMode 
      ? `Updating ${this.data.teacher?.fullName || 'teacher'}'s information` 
      : 'Fill in the teacher details below';
  }

  constructor(
    private _fb: FormBuilder,
    private _dialogRef: MatDialogRef<CreateEditTeacherDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: CreateEditTeacherDialogData,
    private _enumService: EnumService,
  ) {
    // Make dialog responsive on mobile
    _dialogRef.addPanelClass('responsive-dialog');
  }

  ngOnInit(): void {
    this._buildForm();
    this._loadEnums();
    this._configureDialog();

    if (this.isEditMode && this.data.teacher) {
      this._patchFormWithTeacher(this.data.teacher);
    }
  }

  ngAfterViewInit(): void {
    // Set up tab templates after view initialization
    this.tabTemplates = {
      'personal': this.personalTabTemplate,
      'contact': this.contactTabTemplate,
      'professional': this.professionalTabTemplate,
    };
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
    
    // Revoke object URLs to prevent memory leaks
    if (this.photoPreview && typeof this.photoPreview === 'string' && 
        (this.photoPreview.startsWith('blob:') || this.photoPreview.startsWith('data:'))) {
      URL.revokeObjectURL(this.photoPreview);
    }
  }

  // ── Dialog Configuration ─────────────────────────────────────────────────────
  private _configureDialog(): void {
    // Configure Header
    this.dialogHeader = {
      title: this.dialogTitle,
      subtitle: this.dialogSubtitle,
      icon: this.isEditMode ? 'edit' : 'person_add',
      iconTemplate: this.customHeaderIcon,
      showCloseButton: true,
      class: 'border-b border-white/10',
    };

    // Configure Photo Upload
    this.photoConfig = {
      enabled: true,
      photoUrl: this.form.get('photoUrl')?.value,
      preview: null,
      label: 'Profile Photo',
      description: 'JPEG, PNG or WebP · Max 5 MB',
      buttonText: this.isEditMode ? 'Change Photo' : 'Upload Photo',
      onChange: (file) => this.onPhotoSelected(file),
      onRemove: () => this.removePhoto(),
      accept: 'image/jpeg,image/png,image/webp,image/jpg',
      maxSize: 5,
      shape: 'circle',
      previewSize: 100,
      showRemoveButton: true,
      buttonTemplate: this.customUploadButton,
      previewTemplate: this.customPhotoPreview,
      class: 'photo-upload-section'
    };

    // Configure Footer
    this.footerConfig = {
      cancelText: 'Cancel',
      submitText: this.isEditMode ? 'Save Changes' : 'Create Teacher',
      submitIcon: this.isEditMode ? 'save' : 'check_circle',
      loading: false,
      loadingText: this.isEditMode ? 'Saving...' : 'Creating...',
      showError: true,
      errorMessage: 'Please fix all errors before proceeding.',
      errorIcon: 'warning',
      errorPosition: 'left',
      showCancel: true,
      showSubmit: true,
      disableCancelOnLoading: true,
      class: 'dialog-footer'
    };
  }

  // ── Form Setup ───────────────────────────────────────────────────────────────
  private _buildForm(): void {
    this.form = this._fb.group({
      // Personal
      firstName:        ['', [Validators.required, Validators.maxLength(100)]],
      middleName:       ['', [Validators.maxLength(100)]],
      lastName:         ['', [Validators.required, Validators.maxLength(100)]],
      teacherNumber:    ['', [Validators.required, Validators.maxLength(50)]],
      gender:           [null, [Validators.required]],
      dateOfBirth:      [null],
      idNumber:         ['', [Validators.maxLength(100)]],
      nationality:      ['Kenyan', [Validators.maxLength(100)]],
      photoUrl:         [''],
      
      // Contact
      phoneNumber:      ['', [Validators.pattern('^[+0-9\\s-]{10,}$'), Validators.maxLength(100)]],
      email:            ['', [Validators.email, Validators.maxLength(100)]],
      address:          ['', [Validators.maxLength(500)]],
      
      // Professional
      tscNumber:        ['', [Validators.maxLength(50)]],
      employmentType:   [null],
      designation:      [null],
      qualification:    ['', [Validators.maxLength(100)]],
      specialization:   ['', [Validators.maxLength(100)]],
      dateOfEmployment: [null],
      isClassTeacher:   [false],
      isActive:         [true],
      notes:            ['', [Validators.maxLength(2000)]],
    });
  }

  // ── Enum Loading with Caching ────────────────────────────────────────────────
  private _loadEnums(): void {
    this.isEnumsLoading = true;

    // Load enums with caching
    this.genders$ = this._enumCache.getEnum(
      'genders', 
      () => this._enumService.getGenders()
    ).pipe(
      tap(data => this._buildEnumMap('gender', data)),
      shareReplay(1)
    );

    this.employmentTypes$ = this._enumCache.getEnum(
      'employmentTypes',
      () => this._enumService.getTeacherEmploymentTypes()
    ).pipe(
      tap(data => this._buildEnumMap('employmentType', data)),
      shareReplay(1)
    );

    this.designations$ = this._enumCache.getEnum(
      'designations',
      () => this._enumService.getTeacherDesignations()
    ).pipe(
      tap(data => this._buildEnumMap('designation', data)),
      shareReplay(1)
    );

    // Wait for all enums to load
    combineLatest([
      this.genders$,
      this.employmentTypes$,
      this.designations$
    ]).pipe(
      takeUntil(this._unsubscribe),
      finalize(() => {
        this.isEnumsLoading = false;
      })
    ).subscribe();
  }

  private _buildEnumMap(type: string, items: EnumItemDto[]): void {
    const map = new Map<number, string>();
    items.forEach(item => {
      if (item.value !== undefined && item.name) {
        map.set(item.value, item.name);
      }
    });
    this.enumValueToNameMap.set(type, map);
  }

  private _getEnumNameFromValue(type: string, value: number | null): string | null {
    if (value === null || value === undefined) return null;
    const map = this.enumValueToNameMap.get(type);
    return map?.get(value) || null;
  }

  private _getEnumValueFromName(type: string, name: string | undefined): number | null {
    if (!name) return null;
    
    const map = this.enumValueToNameMap.get(type);
    if (!map) return null;
    
    // Find value by name (case-insensitive)
    for (const [value, enumName] of map.entries()) {
      if (enumName.toLowerCase() === name.toLowerCase()) {
        return value;
      }
    }
    return null;
  }

  // ── Teacher Data Population ─────────────────────────────────────────────────
  private async _patchFormWithTeacher(teacher: TeacherDto): Promise<void> {
    // Wait for enums to be loaded
    await this._waitForEnums();

    const genderValue = this._getEnumValueFromName('gender', teacher.gender);
    const employmentValue = this._getEnumValueFromName('employmentType', teacher.employmentType);
    const designationValue = this._getEnumValueFromName('designation', teacher.designation);

    // Build photo URL
    let photoUrl = '';
    if (teacher.photoUrl) {
      photoUrl = teacher.photoUrl.startsWith('http')
        ? teacher.photoUrl
        : `${this._apiBaseUrl}${teacher.photoUrl}`;
    }

    // Patch form values
    this.form.patchValue({
      firstName: teacher.firstName || '',
      middleName: teacher.middleName || '',
      lastName: teacher.lastName || '',
      teacherNumber: teacher.teacherNumber || '',
      gender: genderValue,
      dateOfBirth: teacher.dateOfBirth ? new Date(teacher.dateOfBirth) : null,
      idNumber: teacher.idNumber || '',
      nationality: teacher.nationality || 'Kenyan',
      photoUrl: photoUrl,
      phoneNumber: teacher.phoneNumber || '',
      email: teacher.email || '',
      address: teacher.address || '',
      tscNumber: teacher.tscNumber || '',
      employmentType: employmentValue,
      designation: designationValue,
      qualification: teacher.qualification || '',
      specialization: teacher.specialization || '',
      dateOfEmployment: teacher.dateOfEmployment ? new Date(teacher.dateOfEmployment) : null,
      isClassTeacher: teacher.isClassTeacher || false,
      isActive: teacher.isActive !== undefined ? teacher.isActive : true,
      notes: teacher.notes || '',
    });

    // Load teacher photo if exists
    if (photoUrl) {
      await this._loadTeacherPhoto(photoUrl);
    }
  }

  private _waitForEnums(): Promise<void> {
    return new Promise((resolve) => {
      if (!this.isEnumsLoading) {
        resolve();
        return;
      }

      const subscription = combineLatest([
        this.genders$,
        this.employmentTypes$,
        this.designations$
      ]).pipe(
        takeUntil(this._unsubscribe)
      ).subscribe(() => {
        resolve();
        subscription.unsubscribe();
      });
    });
  }

  private async _loadTeacherPhoto(photoUrl: string): Promise<void> {
    this.isLoadingPhoto = true;
    this.photoError = false;
    
    try {
      const token = this._authService.accessToken;
      const headers = token ? new HttpHeaders().set('Authorization', `Bearer ${token}`) : undefined;
      const authPhotoUrl = token ? `${photoUrl}?token=${token}` : photoUrl;

      const blob = await this._http.get(authPhotoUrl, { 
        responseType: 'blob',
        headers 
      }).pipe(
        takeUntil(this._unsubscribe),
        catchError((error) => {
          console.error('Failed to load photo:', error);
          this.photoError = true;
          return of(null);
        })
      ).toPromise();

      if (blob) {
        // Revoke previous object URL if exists
        if (this.photoPreview && typeof this.photoPreview === 'string' && 
            this.photoPreview.startsWith('blob:')) {
          URL.revokeObjectURL(this.photoPreview);
        }

        const objectUrl = URL.createObjectURL(blob);
        this.photoPreview = this._sanitizer.bypassSecurityTrustUrl(objectUrl);
        
        this.photoConfig = {
          ...this.photoConfig,
          photoUrl: authPhotoUrl,
          preview: objectUrl,
        };
        
        this.form.patchValue({ photoUrl: objectUrl });
      }
    } catch (error) {
      console.error('Error loading photo:', error);
      this.photoError = true;
    } finally {
      this.isLoadingPhoto = false;
    }
  }

  // ── Photo Handling ───────────────────────────────────────────────────────────
  onPhotoSelected(file: File): void {
    this.selectedPhotoFile = file;
    this.photoError = false;

    // Validate file type
    if (!file.type.startsWith('image/')) {
      this.photoError = true;
      return;
    }

    // Validate file size
    if (file.size > (this.photoConfig.maxSize || 5) * 1024 * 1024) {
      this.photoError = true;
      return;
    }

    // Revoke previous object URL if exists
    if (this.photoPreview && typeof this.photoPreview === 'string' && 
        this.photoPreview.startsWith('blob:')) {
      URL.revokeObjectURL(this.photoPreview);
    }

    const reader = new FileReader();
    reader.onload = () => { 
      const result = reader.result as string;
      this.photoPreview = this._sanitizer.bypassSecurityTrustUrl(result);
      
      this.photoConfig = {
        ...this.photoConfig,
        preview: result,
      };
      
      // Update form control with data URL
      this.form.patchValue({ photoUrl: result });
    };
    
    reader.onerror = () => {
      this.photoError = true;
    };
    
    reader.readAsDataURL(file);
  }

  removePhoto(): void {
    // Revoke object URL if exists
    if (this.photoPreview && typeof this.photoPreview === 'string' && 
        this.photoPreview.startsWith('blob:')) {
      URL.revokeObjectURL(this.photoPreview);
    }
    
    this.photoPreview = null;
    this.selectedPhotoFile = null;
    this.photoError = false;
    this.form.patchValue({ photoUrl: '' });
    
    this.photoConfig = {
      ...this.photoConfig,
      photoUrl: '',
      preview: null,
    };
  }

  // ── Event Handlers ───────────────────────────────────────────────────────────
  onTabChange(tabIndex: number): void {
    this.activeTab = tabIndex;
  }

  onSubmit(): void {
    this.formSubmitted = true;
    
    if (this.form.invalid) {
      // Jump to first tab with errors
      for (let i = 0; i < this.dialogTabs.length; i++) {
        const tab = this.dialogTabs[i];
        if (tab.fields && tab.fields.some(field => this.form.get(field)?.invalid)) {
          this.activeTab = i;
          break;
        }
      }
      return;
    }

    const result: CreateEditTeacherDialogResult = {
      formData: this._buildPayload(),
      photoFile: this.selectedPhotoFile,
    };

    this._dialogRef.close(result);
  }

  onCancel(): void {
    this._dialogRef.close(null);
  }

  // ── Payload Builder ─────────────────────────────────────────────────────────
  private _buildPayload(): any {
    const v = this.form.value;

    const toIsoString = (val: any): string | null => {
      if (!val) return null;
      const date = val instanceof Date ? val : new Date(val);
      return isNaN(date.getTime()) ? null : date.toISOString();
    };

    // Get enum names from cached maps
    const genderName = this._getEnumNameFromValue('gender', v.gender);
    const employmentTypeName = this._getEnumNameFromValue('employmentType', v.employmentType);
    const designationName = this._getEnumNameFromValue('designation', v.designation);

    // Remove photoUrl from payload if it's a data URL or blob URL (new upload)
    let photoUrl = v.photoUrl;
    if (photoUrl && (photoUrl.startsWith('data:') || photoUrl.startsWith('blob:'))) {
      photoUrl = ''; // Will be uploaded as file separately
    }

    return {
      firstName: v.firstName?.trim(),
      middleName: v.middleName?.trim() || null,
      lastName: v.lastName?.trim(),
      teacherNumber: v.teacherNumber?.trim(),
      gender: genderName,
      dateOfBirth: toIsoString(v.dateOfBirth),
      idNumber: v.idNumber?.trim() || null,
      nationality: v.nationality?.trim() || 'Kenyan',
      photoUrl: photoUrl || null,
      phoneNumber: v.phoneNumber?.trim() || null,
      email: v.email?.trim() || null,
      address: v.address?.trim() || null,
      tscNumber: v.tscNumber?.trim() || null,
      employmentType: employmentTypeName,
      designation: designationName,
      qualification: v.qualification?.trim() || null,
      specialization: v.specialization?.trim() || null,
      dateOfEmployment: toIsoString(v.dateOfEmployment),
      isClassTeacher: v.isClassTeacher || false,
      isActive: v.isActive !== undefined ? v.isActive : true,
      notes: v.notes?.trim() || null,
    };
  }
}