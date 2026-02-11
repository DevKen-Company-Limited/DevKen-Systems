import { Component, Inject, OnInit, OnDestroy, inject } from '@angular/core';
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
import { Observable, Subject } from 'rxjs';
import { TeacherDto } from 'app/core/DevKenService/Types/Teacher';
import { EnumItemDto, EnumService } from 'app/core/DevKenService/common/enum.service';
import { API_BASE_URL } from 'app/app.config';


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
  ],
  templateUrl: './create-edit-teacher-dialog.component.html',
})
export class CreateEditTeacherDialogComponent implements OnInit, OnDestroy {
  private _unsubscribe = new Subject<void>();
  private _apiBaseUrl = inject(API_BASE_URL);

  form!: FormGroup;
  isSaving = false;
  formSubmitted = false;
  photoPreview: string | null = null;
  selectedPhotoFile: File | null = null;

  activeTab = 0;
  tabs = [
    { label: 'Personal',     icon: 'person'    },
    { label: 'Contact',      icon: 'contacts'  },
    { label: 'Professional', icon: 'work'      },
  ];

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

    if (this.isEditMode && this.data.teacher) {
      this._patchForm(this.data.teacher);
    }
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
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

  private _patchForm(teacher: TeacherDto): void {
    // Construct full photo URL if photoUrl exists and is not already a full URL
    let fullPhotoUrl = '';
    if (teacher.photoUrl) {
      fullPhotoUrl = teacher.photoUrl.startsWith('http') 
        ? teacher.photoUrl 
        : `${this._apiBaseUrl}${teacher.photoUrl}`;
    }

    this.form.patchValue({
      firstName:        teacher.firstName,
      middleName:       teacher.middleName,
      lastName:         teacher.lastName,
      teacherNumber:    teacher.teacherNumber,
      gender:           this._enumNameToValue(teacher.gender),
      dateOfBirth:      teacher.dateOfBirth ? new Date(teacher.dateOfBirth) : null,
      idNumber:         teacher.idNumber,
      nationality:      teacher.nationality,
      photoUrl:         fullPhotoUrl,
      phoneNumber:      teacher.phoneNumber,
      email:            teacher.email,
      address:          teacher.address,
      tscNumber:        teacher.tscNumber,
      employmentType:   this._enumNameToValue(teacher.employmentType),
      designation:      this._enumNameToValue(teacher.designation),
      qualification:    teacher.qualification,
      specialization:   teacher.specialization,
      dateOfEmployment: teacher.dateOfEmployment ? new Date(teacher.dateOfEmployment) : null,
      isClassTeacher:   teacher.isClassTeacher,
      isActive:         teacher.isActive,
      notes:            teacher.notes,
    });
  }

  private _loadEnums(): void {
    this.genders$         = this._enumService.getGenders();
    this.employmentTypes$ = this._enumService.getTeacherEmploymentTypes();
    this.designations$    = this._enumService.getTeacherDesignations();
  }

  // ── Photo Handling ───────────────────────────────────────────────────────────
  onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;

    const file = input.files[0];
    this.selectedPhotoFile = file;

    const reader = new FileReader();
    reader.onload = () => { this.photoPreview = reader.result as string; };
    reader.readAsDataURL(file);
  }

  removePhoto(): void {
    this.photoPreview = null;
    this.selectedPhotoFile = null;
    this.form.patchValue({ photoUrl: '' });
  }

  // ── Tab Validation Hints ─────────────────────────────────────────────────────
  tabHasErrors(tabIndex: number): boolean {
    if (!this.formSubmitted) return false;

    const tabFields: Record<number, string[]> = {
      0: ['firstName', 'lastName', 'teacherNumber', 'gender'],
      1: ['email'],
      2: [],
    };

    return (tabFields[tabIndex] || []).some(field => this.form.get(field)?.invalid);
  }

  // ── Submit ───────────────────────────────────────────────────────────────────
  onSubmit(): void {
    this.formSubmitted = true;
    if (this.form.invalid) {
      // Jump to first tab with errors
      for (let i = 0; i < this.tabs.length; i++) {
        if (this.tabHasErrors(i)) { this.activeTab = i; break; }
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