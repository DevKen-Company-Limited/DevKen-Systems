import { Component, OnInit, OnDestroy, Inject, Optional, Injector } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, FormGroup } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Subject, takeUntil, debounceTime, forkJoin, finalize, of } from 'rxjs';

import { ClassService } from 'app/core/DevKenService/ClassService';
import { AcademicYearService } from 'app/core/DevKenService/AcademicYearService/AcademicYearService';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';

import {
  CreateClassRequest,
  UpdateClassRequest,
  ClassDto,
  AcademicYearOption,
  TeacherOption
} from 'app/Classes/Types/Class';

interface SchoolOption {
  id: string;
  name: string;
  code: string;
}

@Component({
  selector: 'app-create-edit-class-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCheckboxModule,
    MatDialogModule,
    MatSelectModule,
    MatIconModule,
    MatSlideToggleModule,
    MatTooltipModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './create-edit-class-dialog.component.html',
  styles: [`
    :host ::ng-deep .mat-mdc-dialog-container {
      --mdc-dialog-container-shape: 12px;
    }
  `]
})
export class CreateEditClassDialogComponent implements OnInit, OnDestroy {
  private _unsubscribe = new Subject<void>();
  private _codeGenerated = false;

  // Form
  form!: FormGroup;
  formSubmitted = false;
  isSaving = false;

  // Dropdown options
  schools: SchoolOption[] = [];
  cbcLevels: { value: number; label: string }[] = [];
  academicYears: AcademicYearOption[] = [];
  teachers: TeacherOption[] = [];

  // Loading states
  isLoadingSchools = false;
  isLoadingAcademicYears = false;
  isLoadingTeachers = false;

  // Code preview
  codePreview: string | null = null;
  isLoadingPreview = false;

  // Teacher service availability flag
  teacherServiceAvailable = false;
  private teacherService: any = null;

  get isEditMode(): boolean {
    return this.data.mode === 'edit';
  }

  get dialogTitle(): string {
    return this.isEditMode ? 'Edit Class' : 'Create New Class';
  }

  get dialogSubtitle(): string {
    return this.isEditMode 
      ? `Update class information and assignments`
      : 'Add a new class to your school system';
  }

  get showCodePreview(): boolean {
    return !this.isEditMode && !this.form.get('code')?.value && this.codePreview !== null;
  }

  constructor(
    private fb: FormBuilder,
    private service: ClassService,
   private _schoolService: SchoolService,
    private alertService: AlertService,
    private dialogRef: MatDialogRef<CreateEditClassDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { mode: 'create' | 'edit'; class?: ClassDto; schoolId?: string },
    private academicYearService: AcademicYearService,
    private injector: Injector
  ) {
    // Try to get TeacherService if available (to avoid circular dependency)
    try {
      this.teacherService = this.injector.get('TeacherService' as any, null);
      this.teacherServiceAvailable = !!this.teacherService;
    } catch (e) {
      console.info('TeacherService not available - teacher assignment will be optional');
      this.teacherServiceAvailable = false;
    }
  }

  ngOnInit(): void {
    this.buildForm();
    this.cbcLevels = this.service.getAllCBCLevels();
    
    // Load schools first
    this.loadSchools();
    this.setupCodeGeneration();

    if (this.isEditMode && this.data.class) {
      this.patchForm(this.data.class);
    } else {
      // In create mode, if schoolId is pre-selected, load dependent data
      const preSelectedSchoolId = this.form.get('schoolId')?.value;
      if (preSelectedSchoolId) {
        this.onSchoolChange(preSelectedSchoolId);
      }
    }

    // Watch for school changes to reload academic years and teachers
    this.form.get('schoolId')?.valueChanges
      .pipe(takeUntil(this._unsubscribe))
      .subscribe((schoolId) => {
        if (schoolId) {
          this.onSchoolChange(schoolId);
        }
      });
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  private buildForm(): void {
    // Get schoolId with proper fallback logic
    const initialSchoolId = this.isEditMode && this.data.class?.schoolId 
      ? this.data.class.schoolId 
      : (this.data.schoolId || '');
    
    console.log('[Class Dialog] buildForm - schoolId resolution:', {
      mode: this.data.mode,
      dataSchoolId: this.data.schoolId,
      classSchoolId: this.data.class?.schoolId,
      finalSchoolId: initialSchoolId
    });
    
    this.form = this.fb.group({
      schoolId: [initialSchoolId, Validators.required],
      name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      code: ['', [Validators.maxLength(20)]],
      level: ['', Validators.required],
      description: ['', Validators.maxLength(500)],
      capacity: [40, [Validators.required, Validators.min(1), Validators.max(100)]],
      academicYearId: ['', Validators.required],
      teacherId: [''],
      isActive: [true]
    });
  }

  private patchForm(classData: ClassDto): void {
    this.form.patchValue({
      schoolId: classData.schoolId,
      name: classData.name,
      code: classData.code,
      level: classData.level,
      description: classData.description || '',
      capacity: classData.capacity,
      academicYearId: classData.academicYearId,
      teacherId: classData.teacherId || '',
      isActive: classData.isActive
    });

    // In edit mode, disable schoolId as it shouldn't be changed
    this.form.get('schoolId')?.disable();
  }

  /** Load all schools for the dropdown */
  private loadSchools(): void {
    this.isLoadingSchools = true;
    this._schoolService.getAll()
      .pipe(
        takeUntil(this._unsubscribe),
        finalize(() => {
          this.isLoadingSchools = false;
          console.log('[Class Dialog] Schools loaded:', this.schools.length);
        })
      )
      .subscribe({
        next: (response) => {
          console.log('[Class Dialog] Schools API response:', response);
          
          if (response.success && response.data) {
            if (Array.isArray(response.data) && response.data.length > 0) {
              this.schools = response.data.map((school: any) => ({
                id: school.id,
                name: school.name,
                code: school.code
              }));
              console.log('[Class Dialog] ✓ Schools loaded successfully:', this.schools.length);
            } else if (Array.isArray(response.data) && response.data.length === 0) {
              console.warn('[Class Dialog] Schools API returned empty array');
              this.schools = [];
              this.alertService.warning(
                'No schools found. Please create a school first.',
                'No Schools'
              );
            }
          } else {
            console.warn('[Class Dialog] ✗ Schools failed to load');
            this.schools = [];
          }
        },
        error: (error) => {
          console.error('[Class Dialog] ✗ Error loading schools:', error);
          const errorMessage = error?.error?.message || error?.message || 'Unknown error occurred';
          
          this.alertService.error(
            `Failed to load schools: ${errorMessage}`,
            'Loading Error'
          );
        }
      });
  }

  /** Called when school selection changes */
  private onSchoolChange(schoolId: string): void {
    console.log('[Class Dialog] School changed to:', schoolId);
    
    // Reset dependent fields
    this.form.patchValue({
      academicYearId: '',
      teacherId: ''
    });
    
    // Clear existing data
    this.academicYears = [];
    this.teachers = [];
    this.codePreview = null;
    
    // Load new data for selected school
    this.loadDropdownData(schoolId);
    
    // Load code preview in create mode
    if (!this.isEditMode) {
      this.loadCodePreview(schoolId);
    }
  }

  /** Load academic years and teachers for selected school */
  private loadDropdownData(schoolId: string): void {
    if (!schoolId) {
      console.warn('[Class Dialog] No schoolId provided to loadDropdownData');
      return;
    }

    console.log('[Class Dialog] Loading dropdown data for schoolId:', schoolId);

    this.isLoadingAcademicYears = true;
    this.isLoadingTeachers = true;

    const requests: any = {
      academicYears: this.academicYearService.getAll(schoolId)
    };

    if (this.teacherServiceAvailable && this.teacherService?.getAll) {
      requests.teachers = this.teacherService.getAll(schoolId);
    } else {
      requests.teachers = of({ success: true, data: [], message: 'TeacherService not available' });
    }

    forkJoin(requests)
      .pipe(
        takeUntil(this._unsubscribe),
        finalize(() => {
          this.isLoadingAcademicYears = false;
          this.isLoadingTeachers = false;
        })
      )
      .subscribe({
        next: ({ academicYears, teachers }) => {
          // Academic Years
          if (academicYears?.success && academicYears.data && Array.isArray(academicYears.data)) {
            if (academicYears.data.length > 0) {
              this.academicYears = academicYears.data.map((ay: any) => ({
                id: ay.id,
                name: ay.name,
                code: ay.code
              }));
              console.log('[Class Dialog] ✓ Academic years loaded:', this.academicYears.length);
            } else {
              this.academicYears = [];
              this.alertService.warning(
                'No academic years found for this school.',
                'No Academic Years'
              );
            }
          } else {
            this.academicYears = [];
          }

          // Teachers
          if (teachers?.success && teachers.data && Array.isArray(teachers.data) && teachers.data.length > 0) {
            this.teachers = teachers.data.map((t: any) => ({
              id: t.id,
              name: `${t.firstName} ${t.lastName}`,
              teacherNumber: t.teacherNumber
            }));
            console.log('[Class Dialog] ✓ Teachers loaded:', this.teachers.length);
          } else {
            this.teachers = [];
          }
        },
        error: (error) => {
          console.error('[Class Dialog] ✗ Error loading dropdown data:', error);
          this.alertService.error('Failed to load dropdown data', 'Loading Error');
        }
      });
  }

  /** Load code preview from number series */
  private loadCodePreview(schoolId?: string): void {
    const sid = schoolId || this.form.get('schoolId')?.value;
    if (!sid) {
      this.codePreview = null;
      return;
    }

    this.isLoadingPreview = true;
    this.service.previewNextCode(sid)
      .pipe(
        takeUntil(this._unsubscribe),
        finalize(() => this.isLoadingPreview = false)
      )
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.codePreview = res.data.nextCode;
          }
        },
        error: () => {
          this.codePreview = null;
        }
      });
  }

  private setupCodeGeneration(): void {
    if (this.data.mode === 'create') {
      this.form.get('level')?.valueChanges
        .pipe(debounceTime(300), takeUntil(this._unsubscribe))
        .subscribe(() => this.generateCodeIfNeeded());

      this.form.get('name')?.valueChanges
        .pipe(debounceTime(300), takeUntil(this._unsubscribe))
        .subscribe(() => this.generateCodeIfNeeded());

      this.form.get('code')?.valueChanges
        .pipe(takeUntil(this._unsubscribe))
        .subscribe(() => {
          this._codeGenerated = true;
        });
    }
  }

  private generateCodeIfNeeded(): void {
    if (this._codeGenerated) return;

    const level = this.form.get('level')?.value;
    const name = this.form.get('name')?.value;

    if (level !== '' && name) {
      const code = this.service.generateClassCode(level, name);
      this.form.patchValue({ code }, { emitEvent: false });
    }
  }

  generateCode(): void {
    const level = this.form.get('level')?.value;
    const name = this.form.get('name')?.value;

    if (level !== '' && name) {
      const code = this.service.generateClassCode(level, name);
      this.form.patchValue({ code });
      this._codeGenerated = true;
    } else {
      this.alertService.info('Please select a level and enter a name first', 'Missing Information');
    }
  }

  useGeneratedCode(): void {
    if (this.codePreview) {
      this.form.patchValue({ code: this.codePreview });
      this._codeGenerated = true;
    }
  }

  submit(): void {
    this.formSubmitted = true;
    this.form.markAllAsTouched();

    if (this.form.invalid) {
      const firstError = Object.keys(this.form.controls)
        .find(key => this.form.get(key)?.invalid);
      if (firstError) {
        const element = document.querySelector(`[formControlName="${firstError}"]`);
        element?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }

      this.alertService.error('Please fix the errors in the form', 'Validation Error');
      return;
    }

    this.isSaving = true;
    const raw = this.form.getRawValue();

    if (this.isEditMode) {
      const updateRequest: UpdateClassRequest = {
        name: raw.name?.trim(),
        code: raw.code?.trim() || undefined,
        level: Number(raw.level),
        description: raw.description?.trim() || undefined,
        capacity: Number(raw.capacity),
        academicYearId: raw.academicYearId,
        teacherId: raw.teacherId || undefined,
        isActive: raw.isActive
      };

      this.service.update(this.data.class!.id, updateRequest)
        .pipe(
          takeUntil(this._unsubscribe),
          finalize(() => this.isSaving = false)
        )
        .subscribe({
          next: (response) => {
            if (response.success) {
              this.alertService.success(response.message || 'Class updated successfully', 'Success');
              this.dialogRef.close({ success: true, data: response.data });
            } else {
              this.alertService.error(response.message || 'Failed to update class', 'Update Failed');
            }
          },
          error: (err) => {
            console.error('Update error:', err);
            this.alertService.error(
              err?.error?.message || 'An error occurred while updating the class',
              'Error'
            );
          }
        });
    } else {
      const createRequest: CreateClassRequest = {
        schoolId: raw.schoolId,
        name: raw.name?.trim(),
        code: raw.code?.trim() || '',
        level: Number(raw.level),
        description: raw.description?.trim() || undefined,
        capacity: Number(raw.capacity),
        academicYearId: raw.academicYearId,
        teacherId: raw.teacherId || undefined,
        isActive: raw.isActive ?? true
      };

      this.service.create(createRequest)
        .pipe(
          takeUntil(this._unsubscribe),
          finalize(() => this.isSaving = false)
        )
        .subscribe({
          next: (response) => {
            if (response.success) {
              this.alertService.success(response.message || 'Class created successfully', 'Success');
              this.dialogRef.close({ success: true, data: response.data });
            } else {
              this.alertService.error(response.message || 'Failed to create class', 'Creation Failed');
            }
          },
          error: (err) => {
            console.error('Create error:', err);
            this.alertService.error(
              err?.error?.message || 'An error occurred while creating the class',
              'Error'
            );
          }
        });
    }
  }

  cancel(): void {
    if (this.form.dirty && !confirm('You have unsaved changes. Are you sure you want to cancel?')) {
      return;
    }
    this.dialogRef.close({ success: false });
  }

  getLevelDisplay(level: number): string {
    return this.service.getCBCLevelDisplay(level);
  }
}