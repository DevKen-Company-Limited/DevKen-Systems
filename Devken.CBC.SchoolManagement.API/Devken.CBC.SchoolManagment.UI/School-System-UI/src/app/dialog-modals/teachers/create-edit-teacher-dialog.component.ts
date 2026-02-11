import { 
  Component, 
  Inject, 
  OnInit, 
  OnDestroy, 
  ViewChild, 
  TemplateRef, 
  inject,
  ChangeDetectorRef 
} from '@angular/core';
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
import { Observable, Subject, of, combineLatest } from 'rxjs';
import { catchError, tap, takeUntil, shareReplay, first, timeout } from 'rxjs/operators';
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
import { firstValueFrom } from 'rxjs';

export interface CreateEditTeacherDialogData {
  mode: 'create' | 'edit';
  teacher?: TeacherDto;
}

export interface CreateEditTeacherDialogResult {
  formData: any;
  photoFile?: File | null;
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
  private _enumService = inject(EnumService);

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
  private enumLoadAttempted = false;
  private enumLoadStartTime = 0;

  // ── Enum Maps for Reverse Lookup ────────────────────────────────────────────
  // Map by ID (string) to value (number) - for teacher data that comes with ID
  private enumIdToValueMap = new Map<string, Map<string, number>>();
  // Map by name (string) to value (number) - for display and case-insensitive lookup
  private enumNameToValueMap = new Map<string, Map<string, number>>();
  // Map by value (number) to name (string) - for payload building
  private enumValueToNameMap = new Map<string, Map<number, string>>();
  // Map by value (number) to id (string) - if needed
  private enumValueToIdMap = new Map<string, Map<number, string>>();

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
    private _cdr: ChangeDetectorRef
  ) {
    // Make dialog responsive on mobile
    _dialogRef.addPanelClass('responsive-dialog');
  }

  ngOnInit(): void {
    this.enumLoadStartTime = Date.now();
    this._buildForm();
    this._loadEnums();
    this._configureDialog();

    // Initialize templates with setTimeout to avoid ExpressionChanged error
    setTimeout(() => {
      if (this.personalTabTemplate && this.contactTabTemplate && this.professionalTabTemplate) {
        this.tabTemplates = {
          'personal': this.personalTabTemplate,
          'contact': this.contactTabTemplate,
          'professional': this.professionalTabTemplate,
        };
        this._cdr.detectChanges();
      }
    });

    if (this.isEditMode && this.data.teacher) {
      // Add a small delay to ensure enums are loading
      setTimeout(() => {
        this._patchFormWithTeacher(this.data.teacher!);
      }, 100);
    }

    // Safety timeout to hide loading overlay if it gets stuck
    setTimeout(() => {
      if (this.isEnumsLoading) {
        console.warn('Enum loading timeout - forcing hide');
        this.isEnumsLoading = false;
        this._cdr.detectChanges();
      }
    }, 15000);
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

  // ── Enum Loading with Caching (using EnumService) ────────────────────────────
  private _loadEnums(): void {
    if (this.enumLoadAttempted) {
      return;
    }
    
    this.enumLoadAttempted = true;
    this.isEnumsLoading = true;

    // Load enums directly from EnumService
    this.genders$ = this._enumService.getGenders().pipe(
      tap(data => {
        this._buildEnumMaps('gender', data);
        this._checkAllEnumsLoaded();
      }),
      shareReplay(1),
      takeUntil(this._unsubscribe)
    );

    this.employmentTypes$ = this._enumService.getTeacherEmploymentTypes().pipe(
      tap(data => {
        this._buildEnumMaps('employmentType', data);
        this._checkAllEnumsLoaded();
      }),
      shareReplay(1),
      takeUntil(this._unsubscribe)
    );

    this.designations$ = this._enumService.getTeacherDesignations().pipe(
      tap(data => {
        this._buildEnumMaps('designation', data);
        this._checkAllEnumsLoaded();
      }),
      shareReplay(1),
      takeUntil(this._unsubscribe)
    );

    // Subscribe to trigger loading
    this.genders$.subscribe();
    this.employmentTypes$.subscribe();
    this.designations$.subscribe();

    // Force loading to complete after a timeout
    setTimeout(() => {
      this._checkAllEnumsLoaded(true);
    }, 5000);
  }

  private _checkAllEnumsLoaded(force: boolean = false): void {
    // Check if we have all enum maps built
    const hasGenders = this.enumValueToNameMap.has('gender');
    const hasEmploymentTypes = this.enumValueToNameMap.has('employmentType');
    const hasDesignations = this.enumValueToNameMap.has('designation');
    
    if ((hasGenders && hasEmploymentTypes && hasDesignations) || force) {
      if (this.isEnumsLoading) {
        this.isEnumsLoading = false;
        this._cdr.detectChanges();
      }
    }
  }

  /**
   * Build comprehensive enum maps for bidirectional lookup
   * Supports lookup by:
   * - id (string) -> value (number)
   * - name (string) -> value (number)  
   * - value (number) -> name (string)
   * - value (number) -> id (string)
   */
  private _buildEnumMaps(type: string, items: EnumItemDto[]): void {
    const idToValueMap = new Map<string, number>();
    const nameToValueMap = new Map<string, number>();
    const valueToNameMap = new Map<number, string>();
    const valueToIdMap = new Map<number, string>();
    
    items.forEach(item => {
      if (item.value !== undefined) {
        // Store by ID (string) - this is what comes from the backend in teacher object
        if (item.id) {
          idToValueMap.set(item.id, item.value);
          idToValueMap.set(item.id.toLowerCase(), item.value); // Case-insensitive version
        }
        
        // Store by name (string) - for display and fallback
        if (item.name) {
          nameToValueMap.set(item.name, item.value);
          nameToValueMap.set(item.name.toLowerCase(), item.value); // Case-insensitive
          valueToNameMap.set(item.value, item.name);
        }
        
        // Store by value -> id
        if (item.id) {
          valueToIdMap.set(item.value, item.id);
        }
      }
    });
    
    this.enumIdToValueMap.set(type, idToValueMap);
    this.enumNameToValueMap.set(type, nameToValueMap);
    this.enumValueToNameMap.set(type, valueToNameMap);
    this.enumValueToIdMap.set(type, valueToIdMap);
  }

  /**
   * Get enum value (number) from either ID (string) or Name (string)
   * This is used when patching the form with teacher data
   */
  private _getEnumValue(type: string, idOrName: string | undefined): number | null {
    if (!idOrName) return null;
    
    // Try lookup by ID first (most reliable for your backend)
    const idMap = this.enumIdToValueMap.get(type);
    if (idMap) {
      // Try exact match
      if (idMap.has(idOrName)) {
        return idMap.get(idOrName)!;
      }
      // Try lowercase match
      if (idMap.has(idOrName.toLowerCase())) {
        return idMap.get(idOrName.toLowerCase())!;
      }
    }
    
    // Fallback to lookup by name
    const nameMap = this.enumNameToValueMap.get(type);
    if (nameMap) {
      // Try exact match
      if (nameMap.has(idOrName)) {
        return nameMap.get(idOrName)!;
      }
      // Try lowercase match
      if (nameMap.has(idOrName.toLowerCase())) {
        return nameMap.get(idOrName.toLowerCase())!;
      }
    }
    
    return null;
  }

  /**
   * Get enum name from value (for sending to backend)
   * Your backend expects the name string, not the ID
   */
  private _getEnumNameFromValue(type: string, value: number | null): string | null {
    if (value === null || value === undefined) return null;
    const map = this.enumValueToNameMap.get(type);
    return map?.get(value) || null;
  }

  /**
   * Get enum ID from value (if needed)
   */
  private _getEnumIdFromValue(type: string, value: number | null): string | null {
    if (value === null || value === undefined) return null;
    const map = this.enumValueToIdMap.get(type);
    return map?.get(value) || null;
  }

  // ── Teacher Data Population ─────────────────────────────────────────────────
  private async _patchFormWithTeacher(teacher: TeacherDto): Promise<void> {
    try {
      // Wait for enums to be loaded with timeout
      await this._waitForEnums();

      // Get enum values using the ID that comes from the backend
      // Your teacher object has fields like: "gender": "headteacher" (ID string)
      const genderValue = this._getEnumValue('gender', teacher.gender);
      const employmentValue = this._getEnumValue('employmentType', teacher.employmentType);
      const designationValue = this._getEnumValue('designation', teacher.designation);

      // Build photo URL
      let photoUrl = '';
      if (teacher.photoUrl) {
        photoUrl = teacher.photoUrl.startsWith('http')
          ? teacher.photoUrl
          : `${this._apiBaseUrl}${teacher.photoUrl}`;
      }

      // Patch form values - store as numbers (values)
      this.form.patchValue({
        firstName: teacher.firstName || '',
        middleName: teacher.middleName || '',
        lastName: teacher.lastName || '',
        teacherNumber: teacher.teacherNumber || '',
        gender: genderValue,        // Store as number value
        dateOfBirth: teacher.dateOfBirth ? new Date(teacher.dateOfBirth) : null,
        idNumber: teacher.idNumber || '',
        nationality: teacher.nationality || 'Kenyan',
        photoUrl: photoUrl,
        phoneNumber: teacher.phoneNumber || '',
        email: teacher.email || '',
        address: teacher.address || '',
        tscNumber: teacher.tscNumber || '',
        employmentType: employmentValue,  // Store as number value
        designation: designationValue,    // Store as number value
        qualification: teacher.qualification || '',
        specialization: teacher.specialization || '',
        dateOfEmployment: teacher.dateOfEmployment ? new Date(teacher.dateOfEmployment) : null,
        isClassTeacher: teacher.isClassTeacher || false,
        isActive: teacher.isActive !== undefined ? teacher.isActive : true,
        notes: teacher.notes || '',
      });

      this._cdr.detectChanges();

      // Load teacher photo if exists
      if (photoUrl) {
        setTimeout(() => {
          this._loadTeacherPhoto(photoUrl);
        }, 100);
      }
    } catch (error) {
      console.error('Error patching form with teacher data:', error);
      
      // Even on error, try to patch with basic values
      this.form.patchValue({
        firstName: teacher.firstName || '',
        lastName: teacher.lastName || '',
        teacherNumber: teacher.teacherNumber || '',
      });
      
      this._cdr.detectChanges();
    }
  }

  private async _waitForEnums(): Promise<void> {
    // If already loaded, return immediately
    if (!this.isEnumsLoading) {
      return;
    }

    try {
      // Wait for first emission from each enum with timeout
      await Promise.race([
        Promise.all([
          firstValueFrom(this.genders$.pipe(first())),
          firstValueFrom(this.employmentTypes$.pipe(first())),
          firstValueFrom(this.designations$.pipe(first()))
        ]),
        new Promise((_, reject) => 
          setTimeout(() => reject(new Error('Enum loading timeout')), 8000)
        )
      ]);
    } catch (error) {
      console.warn('Enum loading timeout or error:', error);
      // Force loading to complete
      this.isEnumsLoading = false;
    } finally {
      this.isEnumsLoading = false;
      this._cdr.detectChanges();
    }
  }

  private async _loadTeacherPhoto(photoUrl: string): Promise<void> {
    this.isLoadingPhoto = true;
    this.photoError = false;
    
    try {
      const token = this._authService.accessToken;
      const headers = token ? new HttpHeaders().set('Authorization', `Bearer ${token}`) : undefined;
      const authPhotoUrl = token ? `${photoUrl}?token=${token}` : photoUrl;

      const blob = await firstValueFrom(
        this._http.get(authPhotoUrl, { 
          responseType: 'blob',
          headers 
        }).pipe(
          timeout(10000),
          catchError((error) => {
            console.error('Failed to load photo:', error);
            this.photoError = true;
            return of(null);
          })
        )
      );

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
        this._cdr.detectChanges();
      }
    } catch (error) {
      console.error('Error loading photo:', error);
      this.photoError = true;
    } finally {
      this.isLoadingPhoto = false;
      this._cdr.detectChanges();
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
      this._cdr.detectChanges();
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
    
    this._cdr.detectChanges();
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

    // Get enum names from cached maps (BACKEND EXPECTS NAME STRING, NOT ID)
    // Your backend wants "HeadTeacher" not "headteacher" or 3
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
      gender: genderName,           // Send as name string (e.g., "HeadTeacher")
      dateOfBirth: toIsoString(v.dateOfBirth),
      idNumber: v.idNumber?.trim() || null,
      nationality: v.nationality?.trim() || 'Kenyan',
      photoUrl: photoUrl || null,
      phoneNumber: v.phoneNumber?.trim() || null,
      email: v.email?.trim() || null,
      address: v.address?.trim() || null,
      tscNumber: v.tscNumber?.trim() || null,
      employmentType: employmentTypeName,  // Send as name string
      designation: designationName,        // Send as name string
      qualification: v.qualification?.trim() || null,
      specialization: v.specialization?.trim() || null,
      dateOfEmployment: toIsoString(v.dateOfEmployment),
      isClassTeacher: v.isClassTeacher || false,
      isActive: v.isActive !== undefined ? v.isActive : true,
      notes: v.notes?.trim() || null,
    };
  }
}