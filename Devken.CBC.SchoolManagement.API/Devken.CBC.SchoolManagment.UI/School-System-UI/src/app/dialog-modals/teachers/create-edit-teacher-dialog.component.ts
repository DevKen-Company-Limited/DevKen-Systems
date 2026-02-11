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
import { forkJoin, Observable, Subject, takeUntil } from 'rxjs';
import { TeacherDto } from 'app/core/DevKenService/Types/Teacher';
import { EnumItemDto, EnumService } from 'app/core/DevKenService/common/enum.service';
import { API_BASE_URL } from 'app/app.config';
import { FormDialogComponent, DialogHeader, DialogTab, PhotoUploadConfig, DialogFooter } from 'app/shared/dialogs/form/form-dialog.component';


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
    // Reusable form dialog
    FormDialogComponent,
  ],
  templateUrl: './create-edit-teacher-dialog.component.html',
})
export class CreateEditTeacherDialogComponent implements OnInit, OnDestroy {
  @ViewChild('personalTab') personalTabTemplate!: TemplateRef<any>;
  @ViewChild('contactTab') contactTabTemplate!: TemplateRef<any>;
  @ViewChild('professionalTab') professionalTabTemplate!: TemplateRef<any>;

  private _unsubscribe = new Subject<void>();
  private _apiBaseUrl = inject(API_BASE_URL);

  form!: FormGroup;
  formSubmitted = false;
  activeTab = 0;
  
  photoPreview: string | null = null;
  selectedPhotoFile: File | null = null;

  // ── Dialog Configuration ─────────────────────────────────────────────────────
  dialogHeader!: DialogHeader;
  dialogTabs: DialogTab[] = [
    { 
      id: 'personal', 
      label: 'Personal', 
      icon: 'person',
      fields: ['firstName', 'lastName', 'teacherNumber', 'gender']
    },
    { 
      id: 'contact', 
      label: 'Contact', 
      icon: 'contacts',
      fields: ['email']
    },
    { 
      id: 'professional', 
      label: 'Professional', 
      icon: 'work',
      fields: []
    },
  ];

  photoConfig!: PhotoUploadConfig;
  footerConfig!: DialogFooter;
  tabTemplates: { [key: string]: TemplateRef<any> } = {};

  // ── Enum Observables ─────────────────────────────────────────────────────────
  genders$!: Observable<EnumItemDto[]>;
  employmentTypes$!: Observable<EnumItemDto[]>;
  designations$!: Observable<EnumItemDto[]>;

  get isEditMode(): boolean {
    return this.data.mode === 'edit';
  }

  constructor(
    private _fb: FormBuilder,
    private _dialogRef: MatDialogRef<CreateEditTeacherDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: CreateEditTeacherDialogData,
    private _enumService: EnumService,
  ) {}


    ngOnInit(): void {
    this._buildForm();
    this._loadEnums();
    this._configureDialog();

    if (this.isEditMode && this.data.teacher) {
        this._patchFormAfterEnums(this.data.teacher);
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
  }

  // ── Dialog Configuration ─────────────────────────────────────────────────────
  private _configureDialog(): void {
    this.dialogHeader = {
      title: this.isEditMode ? 'Edit Teacher' : 'Add New Teacher',
      subtitle: this.isEditMode ? 'Update teacher information' : 'Fill in teacher details below',
      icon: this.isEditMode ? 'edit' : 'person_add',
    };

    this.photoConfig = {
      enabled: true,
      photoUrl: this.form.get('photoUrl')?.value,
      preview: this.photoPreview,
      label: 'Profile Photo',
      description: 'JPEG, PNG or WebP · Max 5 MB',
      buttonText: this.isEditMode ? 'Change Photo' : 'Upload Photo',
      onChange: (file) => this.onPhotoSelected(file),
      onRemove: () => this.removePhoto(),
    };

    this.footerConfig = {
      cancelText: 'Cancel',
      submitText: this.isEditMode ? 'Save Changes' : 'Create Teacher',
      submitIcon: this.isEditMode ? 'save' : 'add',
      loading: false,
      loadingText: 'Saving...',
      showError: true,
      errorMessage: 'Please fix all errors before saving.',
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
      phoneNumber:      ['', [Validators.maxLength(100)]],
      email:            ['', [Validators.email, Validators.maxLength(100)]],
      address:          ['', [Validators.maxLength(500)]],
      // Professional
      tscNumber:        ['', [Validators.maxLength(50)]],
      employmentType:   [0],
      designation:      [0],
      qualification:    ['', [Validators.maxLength(100)]],
      specialization:   ['', [Validators.maxLength(100)]],
      dateOfEmployment: [null],
      isClassTeacher:   [false],
      isActive:         [true],
      notes:            ['', [Validators.maxLength(2000)]],
    });
  }

    private _patchFormAfterEnums(teacher: TeacherDto): void {

    forkJoin({
        genders: this.genders$,
        employmentTypes: this.employmentTypes$,
        designations: this.designations$
    })
    .pipe(takeUntil(this._unsubscribe))
    .subscribe(({ genders, employmentTypes, designations }) => {

        const genderValue =
        genders.find(g => g.name === teacher.gender)?.value ?? null;

        const employmentValue =
        employmentTypes.find(e => e.name === teacher.employmentType)?.value ?? null;

        const designationValue =
        designations.find(d => d.name === teacher.designation)?.value ?? null;

        let fullPhotoUrl = '';
        if (teacher.photoUrl) {
        fullPhotoUrl = teacher.photoUrl.startsWith('http')
            ? teacher.photoUrl
            : `${this._apiBaseUrl}${teacher.photoUrl}`;
        }

        this.form.patchValue({
        firstName: teacher.firstName,
        middleName: teacher.middleName,
        lastName: teacher.lastName,
        teacherNumber: teacher.teacherNumber,
        gender: genderValue,
        dateOfBirth: teacher.dateOfBirth ? new Date(teacher.dateOfBirth) : null,
        idNumber: teacher.idNumber,
        nationality: teacher.nationality,
        photoUrl: fullPhotoUrl,
        phoneNumber: teacher.phoneNumber,
        email: teacher.email,
        address: teacher.address,
        tscNumber: teacher.tscNumber,
        employmentType: employmentValue,
        designation: designationValue,
        qualification: teacher.qualification,
        specialization: teacher.specialization,
        dateOfEmployment: teacher.dateOfEmployment ? new Date(teacher.dateOfEmployment) : null,
        isClassTeacher: teacher.isClassTeacher,
        isActive: teacher.isActive,
        notes: teacher.notes,
        });

        this.photoPreview = fullPhotoUrl;

        this.photoConfig = {
        ...this.photoConfig,
        photoUrl: fullPhotoUrl,
        preview: fullPhotoUrl,
        };
    });
    }


  private _loadEnums(): void {
    this.genders$         = this._enumService.getGenders();
    this.employmentTypes$ = this._enumService.getTeacherEmploymentTypes();
    this.designations$    = this._enumService.getTeacherDesignations();
  }

  // ── Photo Handling ───────────────────────────────────────────────────────────
  onPhotoSelected(file: File): void {
    this.selectedPhotoFile = file;

    const reader = new FileReader();
    reader.onload = () => { 
      this.photoPreview = reader.result as string;
      this.photoConfig = {
        ...this.photoConfig,
        preview: this.photoPreview,
      };
    };
    reader.readAsDataURL(file);
  }

  removePhoto(): void {
    this.photoPreview = null;
    this.selectedPhotoFile = null;
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
      formData:  this._buildPayload(),
      photoFile: this.selectedPhotoFile,
    };

    this._dialogRef.close(result);
  }

  onCancel(): void {
    this._dialogRef.close(null);
  }

  // ── Helpers ───────────────────────────────────────────────────────────────────
  private _buildPayload(): any {
    const v = this.form.value;

    const toIsoString = (val: any) => {
      if (!val) return null;
      const date = val instanceof Date ? val : new Date(val);
      return isNaN(date.getTime()) ? null : date.toISOString();
    };

    return {
      ...v,
      dateOfBirth:      toIsoString(v.dateOfBirth),
      dateOfEmployment: toIsoString(v.dateOfEmployment),
    };
  }

  /** Backend returns enum names like "Permanent", so map back to numeric value for dropdowns */
  private _enumNameToValue(name: string): number {
    const map: Record<string, number> = {
      // Gender
      Male: 0, Female: 1, Other: 2,
      // EmploymentType
      Permanent: 0, Contract: 1, PartTime: 2,
      // Designation
      Teacher: 0, SeniorTeacher: 1, HeadTeacher: 2, DeputyHeadTeacher: 3,
    };
    return map[name] ?? 0;
  }
}