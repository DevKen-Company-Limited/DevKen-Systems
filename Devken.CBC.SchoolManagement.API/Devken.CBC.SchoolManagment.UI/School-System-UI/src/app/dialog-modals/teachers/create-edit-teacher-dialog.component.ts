import {
  Component,
  Inject,
  OnInit,
  OnDestroy,
  inject,
  ChangeDetectorRef,
  AfterViewInit
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import {
  MatDialogRef,
  MAT_DIALOG_DATA,
  MatDialogModule
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatRippleModule } from '@angular/material/core';
import { MatTooltipModule } from '@angular/material/tooltip';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { Subject, forkJoin, of } from 'rxjs';
import {
  catchError,
  takeUntil,
  finalize,
  timeout
} from 'rxjs/operators';
import { TeacherDto } from 'app/core/DevKenService/Types/Teacher';
import { EnumItemDto, EnumService } from 'app/core/DevKenService/common/enum.service';
import { API_BASE_URL } from 'app/app.config';
import { AuthService } from 'app/core/auth/auth.service';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { SchoolDto } from 'app/Tenant/types/school';

export interface CreateEditTeacherDialogData {
  mode: 'create' | 'edit';
  teacher?: TeacherDto;
}

export interface CreateEditTeacherDialogResult {
  formData: any;
  photoFile?: File | null;
}

type TabId = 'personal' | 'contact' | 'professional';

interface TabConfig {
  id: TabId;
  label: string;
  icon: string;
  fields: string[];
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
    MatRippleModule,
    MatTooltipModule,
  ],
  templateUrl: './create-edit-teacher-dialog.component.html',
  styleUrls: ['./create-edit-teacher-dialog.component.scss']
})
export class CreateEditTeacherDialogComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly _unsubscribe = new Subject<void>();
  private readonly _apiBaseUrl = inject(API_BASE_URL);
  private readonly _sanitizer = inject(DomSanitizer);
  private readonly _authService = inject(AuthService);
  private readonly _http = inject(HttpClient);
  private readonly _enumService = inject(EnumService);
  private readonly _schoolService = inject(SchoolService); // Add this

  // ── Form State ──────────────────────────────────────────────────────────────
  form!: FormGroup;
  formSubmitted = false;
  activeTab: TabId = 'personal';

  // ── Photo State ─────────────────────────────────────────────────────────────
  photoPreview: SafeUrl | null = null;
  selectedPhotoFile: File | null = null;
  isLoadingPhoto = false;
  photoError = false;
  photoErrorMessage = '';
  existingPhotoUrl: string | null = null;

  // ── Enum State ──────────────────────────────────────────────────────────────
  genders: EnumItemDto[] = [];
  employmentTypes: EnumItemDto[] = [];
  designations: EnumItemDto[] = [];
  schools: SchoolDto[] = []; // Add this
  isEnumsLoading = true;

  // Enum lookup maps
  private enumMaps = new Map<string, {
    idToValue: Map<string, number>;
    nameToValue: Map<string, number>;
    valueToName: Map<number, string>;
  }>();

  // ── Tab Configuration ────────────────────────────────────────────────────────
  // readonly tabs: TabConfig[] = [
  //   {
  //     id: 'personal',
  //     label: 'Personal',
  //     icon: 'person',
  //     fields: ['firstName', 'lastName', 'teacherNumber', 'gender', 'schoolId'] // Add schoolId to validation
  //   },
  //   {
  //     id: 'contact',
  //     label: 'Contact',
  //     icon: 'contact_phone',
  //     fields: ['email', 'phoneNumber']
  //   },
  //   {
  //     id: 'professional',
  //     label: 'Professional',
  //     icon: 'work',
  //     fields: ['employmentType', 'designation']
  //   }
  // ];

  // ── Getters ──────────────────────────────────────────────────────────────────
  get isEditMode(): boolean { return this.data.mode === 'edit'; }

  get isSuperAdmin(): boolean { 
    return this._authService.authUser?.isSuperAdmin ?? false; 
  }

  get dialogTitle(): string {
    return this.isEditMode ? 'Edit Teacher' : 'Add New Teacher';
  }

  get dialogSubtitle(): string {
    return this.isEditMode
      ? `Updating ${this.data.teacher?.fullName || 'teacher'}'s profile`
      : 'Fill in the details to register a new teacher';
  }

  get currentTabIndex(): number {
    return this.tabs.findIndex(t => t.id === this.activeTab);
  }

  get isFirstTab(): boolean { return this.currentTabIndex === 0; }
  get isLastTab(): boolean  { return this.currentTabIndex === this.tabs.length - 1; }

  get notesLength(): number {
    return this.form.get('notes')?.value?.length || 0;
  }

  // ── Constructor ──────────────────────────────────────────────────────────────
  constructor(
    private readonly _fb: FormBuilder,
    private readonly _dialogRef: MatDialogRef<CreateEditTeacherDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: CreateEditTeacherDialogData,
    private readonly _cdr: ChangeDetectorRef
  ) {
    _dialogRef.addPanelClass(['teacher-dialog', 'responsive-dialog']);
  }

  ngOnInit(): void {
    this._buildForm();
    this._loadEnums();
  }

  ngAfterViewInit(): void {}

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  // ── Form Setup ───────────────────────────────────────────────────────────────
private _buildForm(): void {
  this.form = this._fb.group({
    schoolId:         [null, this.isSuperAdmin ? [Validators.required] : []],
    firstName:        ['', [Validators.required, Validators.maxLength(100)]],
    middleName:       ['', [Validators.maxLength(100)]],
    lastName:         ['', [Validators.required, Validators.maxLength(100)]],
  teacherNumber: [{
    value: '',
    disabled: true
  }],

    gender:           [null, [Validators.required]],
    dateOfBirth:      [null],
    idNumber:         ['', [Validators.maxLength(100)]],
    nationality:      ['Kenyan', [Validators.maxLength(100)]],
    phoneNumber:      ['', [Validators.pattern('^[+0-9\\s\\-]{7,20}$')]],
    email:            ['', [Validators.email, Validators.maxLength(100)]],
    address:          ['', [Validators.maxLength(500)]],
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

// Remove teacherNumber from required fields validation in tabs
readonly tabs: TabConfig[] = [
  {
    id: 'personal',
    label: 'Personal',
    icon: 'person',
    fields: ['firstName', 'lastName', 'gender', 'schoolId'] // Remove teacherNumber from here
  },
  {
    id: 'contact',
    label: 'Contact',
    icon: 'contact_phone',
    fields: ['email', 'phoneNumber']
  },
  {
    id: 'professional',
    label: 'Professional',
    icon: 'work',
    fields: ['employmentType', 'designation']
  }
];

  // ── Enum Loading ─────────────────────────────────────────────────────────────
  private _loadEnums(): void {
    this.isEnumsLoading = true;

    const requests: any = {
      genders:         this._enumService.getGenders(),
      employmentTypes: this._enumService.getTeacherEmploymentTypes(),
      designations:    this._enumService.getTeacherDesignations(),
    };

    // Add schools request only for SuperAdmin
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
        this.isEnumsLoading = false;
        this._cdr.detectChanges();
      })
    ).subscribe({
      next: (results: any) => {
        this.genders = results.genders;
        this.employmentTypes = results.employmentTypes;
        this.designations = results.designations;
        
        if (results.schools) {
          this.schools = results.schools.data || [];
        }

        this._buildEnumMaps('gender', this.genders);
        this._buildEnumMaps('employmentType', this.employmentTypes);
        this._buildEnumMaps('designation', this.designations);

        if (this.isEditMode && this.data.teacher) {
          this._patchFormWithTeacher(this.data.teacher);
        }
      },
      error: (err) => {
        console.error('Failed to load enums:', err);
        if (this.isEditMode && this.data.teacher) {
          this._patchFormWithTeacher(this.data.teacher);
        }
      }
    });

    // Safety timeout
    setTimeout(() => {
      if (this.isEnumsLoading) {
        this.isEnumsLoading = false;
        this._cdr.detectChanges();
      }
    }, 12000);
  }

  private _buildEnumMaps(type: string, items: EnumItemDto[]): void {
    const idToValue   = new Map<string, number>();
    const nameToValue = new Map<string, number>();
    const valueToName = new Map<number, string>();

    items.forEach(item => {
      if (item.value === undefined) return;
      if (item.id) {
        idToValue.set(item.id, item.value);
        idToValue.set(item.id.toLowerCase(), item.value);
      }
      if (item.name) {
        nameToValue.set(item.name, item.value);
        nameToValue.set(item.name.toLowerCase(), item.value);
        valueToName.set(item.value, item.name);
      }
    });

    this.enumMaps.set(type, { idToValue, nameToValue, valueToName });
  }

  private _getEnumValue(type: string, raw: any): number | null {
    if (raw === null || raw === undefined) return null;
    if (typeof raw === 'number') return raw;

    const maps = this.enumMaps.get(type);
    if (!maps) return null;

    const str = String(raw);
    const num = Number(str);
    if (!isNaN(num) && maps.valueToName.has(num)) return num;

    if (maps.idToValue.has(str)) return maps.idToValue.get(str)!;
    if (maps.idToValue.has(str.toLowerCase())) return maps.idToValue.get(str.toLowerCase())!;
    if (maps.nameToValue.has(str)) return maps.nameToValue.get(str)!;
    if (maps.nameToValue.has(str.toLowerCase())) return maps.nameToValue.get(str.toLowerCase())!;

    return null;
  }

  // ── Form Population ──────────────────────────────────────────────────────────
private _patchFormWithTeacher(teacher: TeacherDto): void {
  const genderVal     = this._getEnumValue('gender', teacher.gender);
  const empVal        = this._getEnumValue('employmentType', teacher.employmentType);
  const designVal     = this._getEnumValue('designation', teacher.designation);

  // Store existing photo URL separately (don't put in form)
  if (teacher.photoUrl) {
    this.existingPhotoUrl = teacher.photoUrl.startsWith('http')
      ? teacher.photoUrl
      : `${this._apiBaseUrl}${teacher.photoUrl}`;
  }

  this.form.patchValue({
    schoolId:         teacher.schoolId         || null,
    firstName:        teacher.firstName        || '',
    middleName:       teacher.middleName       || '',
    lastName:         teacher.lastName         || '',
    teacherNumber:    teacher.teacherNumber    || '', // Will be included since form is enabled in edit mode
    gender:           genderVal,
    dateOfBirth:      teacher.dateOfBirth      ? new Date(teacher.dateOfBirth) : null,
    idNumber:         teacher.idNumber         || '',
    nationality:      teacher.nationality      || 'Kenyan',
    phoneNumber:      teacher.phoneNumber      || '',
    email:            teacher.email            || '',
    address:          teacher.address          || '',
    tscNumber:        teacher.tscNumber        || '',
    employmentType:   empVal,
    designation:      designVal,
    qualification:    teacher.qualification    || '',
    specialization:   teacher.specialization   || '',
    dateOfEmployment: teacher.dateOfEmployment ? new Date(teacher.dateOfEmployment) : null,
    isClassTeacher:   teacher.isClassTeacher   || false,
    isActive:         teacher.isActive !== undefined ? teacher.isActive : true,
    notes:            teacher.notes            || '',
  });

  this._cdr.detectChanges();

  if (this.existingPhotoUrl) {
    this._loadTeacherPhoto(this.existingPhotoUrl);
  }
}

  private _loadTeacherPhoto(photoUrl: string): void {
    this.isLoadingPhoto = true;
    this.photoError = false;

    const token = this._authService.accessToken;
    const headers = token
      ? new HttpHeaders().set('Authorization', `Bearer ${token}`)
      : undefined;
    const authUrl = token ? `${photoUrl}?token=${encodeURIComponent(token)}` : photoUrl;

    this._http.get(authUrl, { responseType: 'blob', headers }).pipe(
      timeout(10000),
      catchError(err => {
        console.error('Photo load failed:', err);
        this.photoError = true;
        this.photoErrorMessage = 'Failed to load photo';
        return of(null);
      }),
      finalize(() => {
        this.isLoadingPhoto = false;
        this._cdr.detectChanges();
      }),
      takeUntil(this._unsubscribe)
    ).subscribe(blob => {
      if (blob) {
        const objectUrl = URL.createObjectURL(blob);
        this.photoPreview = this._sanitizer.bypassSecurityTrustUrl(objectUrl);
        this._cdr.detectChanges();
      }
    });
  }

  // ── Photo Handling ───────────────────────────────────────────────────────────
  onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.photoError = false;
    this.photoErrorMessage = '';

    if (!file.type.match(/^image\/(jpeg|png|webp|jpg)$/)) {
      this.photoError = true;
      this.photoErrorMessage = 'Only JPEG, PNG or WebP allowed';
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      this.photoError = true;
      this.photoErrorMessage = 'File must be under 5 MB';
      return;
    }

    this.selectedPhotoFile = file;

    const reader = new FileReader();
    reader.onload = () => {
      const result = reader.result as string;
      this.photoPreview = this._sanitizer.bypassSecurityTrustUrl(result);
      this._cdr.detectChanges();
    };
    reader.onerror = () => {
      this.photoError = true;
      this.photoErrorMessage = 'Could not read the file';
      this._cdr.detectChanges();
    };
    reader.readAsDataURL(file);
    input.value = '';
  }

  removePhoto(): void {
    this.photoPreview = null;
    this.selectedPhotoFile = null;
    this.existingPhotoUrl = null;
    this.photoError = false;
    this.photoErrorMessage = '';
    this._cdr.detectChanges();
  }

  triggerPhotoUpload(): void {
    document.getElementById('teacher-photo-input')?.click();
  }

  // ── Navigation ───────────────────────────────────────────────────────────────
  setTab(tabId: TabId): void {
    this.activeTab = tabId;
  }

  nextTab(): void {
    const idx = this.currentTabIndex;
    if (idx < this.tabs.length - 1) {
      this.activeTab = this.tabs[idx + 1].id;
    }
  }

  prevTab(): void {
    const idx = this.currentTabIndex;
    if (idx > 0) this.activeTab = this.tabs[idx - 1].id;
  }

  // ── Submit & Cancel ──────────────────────────────────────────────────────────
  onSubmit(): void {
    this.formSubmitted = true;

    if (this.form.invalid) {
      // Jump to first tab with errors
      for (const tab of this.tabs) {
        if (tab.fields.some(f => this.form.get(f)?.invalid)) {
          this.activeTab = tab.id;
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

  // ── Payload Builder ──────────────────────────────────────────────────────────
private _buildPayload(): any {
  const v = this.form.getRawValue(); // Use getRawValue() to include disabled fields

  const toIso = (val: any): string | null => {
    if (!val) return null;
    const d = val instanceof Date ? val : new Date(val);
    return isNaN(d.getTime()) ? null : d.toISOString();
  };

  // Only include existing photoUrl if we're editing and haven't selected a new photo
  let photoUrl = null;
  if (this.isEditMode && !this.selectedPhotoFile && this.existingPhotoUrl) {
    const url = this.existingPhotoUrl;
    if (url.includes('/uploads/')) {
      photoUrl = url.substring(url.indexOf('/uploads/'));
    } else if (url.startsWith('/')) {
      photoUrl = url;
    }
  }

  const payload: any = {
    firstName:        v.firstName?.trim()     || null,
    middleName:       v.middleName?.trim()    || null,
    lastName:         v.lastName?.trim()      || null,
    gender:           v.gender,
    dateOfBirth:      toIso(v.dateOfBirth),
    idNumber:         v.idNumber?.trim()      || null,
    nationality:      v.nationality?.trim()   || 'Kenyan',
    photoUrl:         photoUrl,
    phoneNumber:      v.phoneNumber?.trim()   || null,
    email:            v.email?.trim()         || null,
    address:          v.address?.trim()       || null,
    tscNumber:        v.tscNumber?.trim()     || null,
    employmentType:   v.employmentType,
    designation:      v.designation,
    qualification:    v.qualification?.trim() || null,
    specialization:   v.specialization?.trim()|| null,
    dateOfEmployment: toIso(v.dateOfEmployment),
    isClassTeacher:   v.isClassTeacher        ?? false,
    isActive:         v.isActive              ?? true,
    notes:            v.notes?.trim()         || null,
  };

  // Only include teacherNumber in edit mode
  if (this.isEditMode && v.teacherNumber) {
    payload.teacherNumber = v.teacherNumber?.trim() || null;
  }

  // Add schoolId for SuperAdmins
  if (this.isSuperAdmin && v.schoolId) {
    payload.schoolId = v.schoolId;
  }

  return payload;
}

  // ── Helpers ──────────────────────────────────────────────────────────────────
  hasError(field: string, error: string): boolean {
    const c = this.form.get(field);
    return !!(c && (this.formSubmitted || c.touched) && c.hasError(error));
  }

  tabHasErrors(tab: TabConfig): boolean {
    return this.formSubmitted && tab.fields.some(f => this.form.get(f)?.invalid);
  }

  getFieldError(field: string): string {
    const c = this.form.get(field);
    if (!c || !(this.formSubmitted || c.touched)) return '';
    if (c.hasError('required'))   return 'This field is required';
    if (c.hasError('email'))      return 'Enter a valid email address';
    if (c.hasError('pattern'))    return 'Invalid format';
    if (c.hasError('maxlength')) {
      const max = c.getError('maxlength').requiredLength;
      return `Maximum ${max} characters allowed`;
    }
    return 'Invalid value';
  }
}