import { Component, OnInit, OnDestroy, Inject, inject, ChangeDetectorRef } from '@angular/core';
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
import { Subject, takeUntil, debounceTime, forkJoin, finalize, of, catchError } from 'rxjs';

import { ClassService } from 'app/core/DevKenService/ClassService/ClassService';
import { AcademicYearService } from 'app/core/DevKenService/AcademicYearService/AcademicYearService';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { AuthService } from 'app/core/auth/auth.service';

import {
  CreateClassRequest,
  UpdateClassRequest,
  ClassDto,
} from 'app/Classes/Types/Class';
import { AcademicYearDto } from 'app/Academics/AcademicYear/Types/AcademicYear';
import { SchoolDto } from 'app/Tenant/types/school';
import { TeacherService } from 'app/core/DevKenService/Teacher/TeacherService';
import { TeacherDto } from 'app/core/DevKenService/Types/Teacher';

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
  private readonly _unsubscribe = new Subject<void>();
  private _codeGenerated = false;

  // Guard that suppresses schoolId-change side-effects while we programmatically
  // populate the form in edit mode (prevents academicYearId / teacherId reset).
  private _isPatchingForm = false;

  // ── Services ─────────────────────────────────────────────────────────────
  private readonly _authService = inject(AuthService);
  private readonly _schoolService = inject(SchoolService);
  private readonly _academicYearService = inject(AcademicYearService);
  private readonly _teacherService = inject(TeacherService);
  private readonly _classService = inject(ClassService);
  private readonly _alertService = inject(AlertService);
  private readonly _cdr = inject(ChangeDetectorRef);

  // ── Form State ───────────────────────────────────────────────────────────
  form!: FormGroup;
  formSubmitted = false;
  isSaving = false;
  isDataLoading = true;

  // ── Dropdown Data ────────────────────────────────────────────────────────
  schools: SchoolOption[] = [];
  cbcLevels: { value: number; label: string }[] = [];
  academicYears: AcademicYearDto[] = [];
  teachers: TeacherDto[] = [];

  // ── Loading States ───────────────────────────────────────────────────────
  isLoadingSchools = false;
  isLoadingAcademicYears = false;
  isLoadingTeachers = false;

  // ── Code Preview ─────────────────────────────────────────────────────────
  codePreview: string | null = null;
  isLoadingPreview = false;

  // ── Computed Properties ──────────────────────────────────────────────────
  get isSuperAdmin(): boolean {
    return this._authService.authUser?.isSuperAdmin ?? false;
  }

  get isEditMode(): boolean {
    return this.data.mode === 'edit';
  }

  get dialogTitle(): string {
    return this.isEditMode ? 'Edit Class' : 'Create New Class';
  }

  get dialogSubtitle(): string {
    return this.isEditMode
      ? 'Update class information and assignments'
      : 'Add a new class to your school system';
  }

  get showCodePreview(): boolean {
    return !this.isEditMode && !this.form.get('code')?.value && this.codePreview !== null;
  }

  // ── Filtered Data ─────────────────────────────────────────────────────────
  // getRawValue() ensures a disabled schoolId (edit mode) is still readable.
  get filteredAcademicYears(): AcademicYearDto[] {
    if (!this.isSuperAdmin) return this.academicYears;
    const schoolId = this.form?.getRawValue()?.schoolId;
    if (!schoolId) return [];
    return this.academicYears.filter(ay => ay.schoolId === schoolId);
  }

  get filteredTeachers(): TeacherDto[] {
    if (!this.isSuperAdmin) return this.teachers;
    const schoolId = this.form?.getRawValue()?.schoolId;
    if (!schoolId) return [];
    return this.teachers.filter(t => t.schoolId === schoolId);
  }

  // ── Constructor ──────────────────────────────────────────────────────────
  constructor(
    private readonly _fb: FormBuilder,
    private readonly _dialogRef: MatDialogRef<CreateEditClassDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: {
      mode: 'create' | 'edit';
      class?: ClassDto;
      schoolId?: string;
    }
  ) {
    _dialogRef.addPanelClass(['class-dialog', 'responsive-dialog']);
  }

  // ── Lifecycle ────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this._buildForm();
    this.cbcLevels = this._classService.getAllCBCLevels();
    this._loadData();
    this._setupCodeGeneration();
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  // ── Form Setup ───────────────────────────────────────────────────────────
  private _buildForm(): void {
    const initialSchoolId = this.data.schoolId ?? null;

    this.form = this._fb.group({
      schoolId:       [initialSchoolId, this.isSuperAdmin ? [Validators.required] : []],
      name:           ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      code:           ['', [Validators.maxLength(20)]],
      level:          [null, Validators.required],
      description:    ['', Validators.maxLength(500)],
      capacity:       [40, [Validators.required, Validators.min(1), Validators.max(100)]],
      academicYearId: [null, Validators.required],
      teacherId:      [null],
      isActive:       [true]
    });

    // SuperAdmin: react to school selection changes.
    // _isPatchingForm guard prevents this from firing during _patchForm(),
    // which would wipe academicYearId / teacherId just set by the patch.
    if (this.isSuperAdmin) {
      this.form.get('schoolId')?.valueChanges
        .pipe(takeUntil(this._unsubscribe))
        .subscribe(schoolId => {
          if (this._isPatchingForm) return;

          // Genuine user change — reset dependent dropdowns
          this.form.patchValue(
            { academicYearId: null, teacherId: null },
            { emitEvent: false }
          );
          this.codePreview = null;

          if (!this.isEditMode && schoolId) {
            this._loadCodePreview(schoolId);
          }
        });
    }
  }

  // ── Data Loading ─────────────────────────────────────────────────────────
  private _loadData(): void {
    const requests: any = {
      academicYears: this._academicYearService.getAll().pipe(
        catchError(err => {
          console.error('Failed to load academic years:', err);
          return of({ success: false, message: '', data: [] });
        })
      ),
      teachers: this._teacherService.getAll().pipe(
        catchError(err => {
          console.error('Failed to load teachers:', err);
          return of({ success: false, message: '', data: [] });
        })
      )
    };

    if (this.isSuperAdmin) {
      this.isLoadingSchools = true;
      requests.schools = this._schoolService.getAll().pipe(
        catchError(err => {
          console.error('Failed to load schools:', err);
          return of({ success: false, message: '', data: [] });
        })
      );
    }

    this.isLoadingAcademicYears = true;
    this.isLoadingTeachers = true;

    forkJoin(requests).pipe(
      takeUntil(this._unsubscribe),
      finalize(() => {
        this.isLoadingSchools = false;
        this.isLoadingAcademicYears = false;
        this.isLoadingTeachers = false;
        this.isDataLoading = false;
        this._cdr.detectChanges();
      })
    ).subscribe({
      next: (results: any) => {
        if (results.academicYears?.success) {
          this.academicYears = results.academicYears.data || [];
        }
        if (results.teachers?.success) {
          this.teachers = results.teachers.data || [];
        }
        if (results.schools?.success) {
          this.schools = (results.schools.data || []).map((s: SchoolDto) => ({
            id: s.id,
            name: s.name,
            code: (s as any).slug || s.id.substring(0, 8).toUpperCase()
          }));
        }

        // All dropdown data is now loaded — safe to patch the form.
        if (this.isEditMode && this.data.class) {
          this._patchForm(this.data.class);
        } else if (!this.isEditMode) {
          const schoolId = this.form.get('schoolId')?.value;
          if (schoolId) {
            this._loadCodePreview(schoolId);
          }
        }
      },
      error: (err) => {
        console.error('Failed to load data:', err);
        this._alertService.error('Failed to load form data');
        if (this.isEditMode && this.data.class) {
          this._patchForm(this.data.class);
        }
      }
    });
  }

  // ── Form Population (Edit Mode) ───────────────────────────────────────────
  private _patchForm(classData: ClassDto): void {
    // Raise guard — schoolId valueChanges must NOT reset academicYearId/teacherId
    // while we are programmatically filling the form.
    this._isPatchingForm = true;

    // 1. Patch schoolId first so filteredAcademicYears / filteredTeachers
    //    immediately return the correct subset when Angular evaluates the template.
    this.form.patchValue({ schoolId: classData.schoolId }, { emitEvent: false });

    // 2. Disable schoolId — school cannot change after a class is created.
    this.form.get('schoolId')?.disable({ emitEvent: false });

    // 3. Patch all remaining fields. Because academicYears and teachers arrays
    //    are already populated (arrives after forkJoin completes), Angular
    //    mat-select can match academicYearId / teacherId to an <mat-option>.
    this.form.patchValue({
      name:           classData.name           ?? '',
      code:           classData.code           ?? '',
      level:          classData.level,
      description:    classData.description    ?? '',
      capacity:       classData.capacity,
      academicYearId: classData.academicYearId,
      teacherId:      classData.teacherId      ?? null,
      isActive:       classData.isActive
    }, { emitEvent: false });

    // 4. Lower the guard after Angular's internal valueChanges have settled.
    setTimeout(() => {
      this._isPatchingForm = false;
      this._cdr.detectChanges();
    }, 0);
  }

  // ── Code Generation ──────────────────────────────────────────────────────
  private _setupCodeGeneration(): void {
    if (!this.isEditMode) {
      this.form.get('level')?.valueChanges
        .pipe(debounceTime(300), takeUntil(this._unsubscribe))
        .subscribe(() => this._generateCodeIfNeeded());

      this.form.get('name')?.valueChanges
        .pipe(debounceTime(300), takeUntil(this._unsubscribe))
        .subscribe(() => this._generateCodeIfNeeded());

      this.form.get('code')?.valueChanges
        .pipe(takeUntil(this._unsubscribe))
        .subscribe(val => { if (val) this._codeGenerated = true; });
    }
  }

  private _generateCodeIfNeeded(): void {
    if (this._codeGenerated) return;
    const level = this.form.get('level')?.value;
    const name  = this.form.get('name')?.value;
    if (level !== null && level !== '' && name) {
      const code = this._classService.generateClassCode(level, name);
      this.form.patchValue({ code }, { emitEvent: false });
    }
  }

  private _loadCodePreview(schoolId?: string): void {
    const sid = schoolId || this.form.getRawValue().schoolId;
    if (!sid) { this.codePreview = null; return; }

    this.isLoadingPreview = true;
    this._classService.previewNextCode(sid)
      .pipe(takeUntil(this._unsubscribe), finalize(() => this.isLoadingPreview = false))
      .subscribe({
        next: (res) => { if (res.success && res.data) this.codePreview = res.data.nextCode; },
        error: ()    => { this.codePreview = null; }
      });
  }

  generateCode(): void {
    const level = this.form.get('level')?.value;
    const name  = this.form.get('name')?.value;
    if (level !== null && level !== '' && name) {
      this.form.patchValue({ code: this._classService.generateClassCode(level, name) });
      this._codeGenerated = true;
    } else {
      this._alertService.info('Please select a level and enter a name first');
    }
  }

  useGeneratedCode(): void {
    if (this.codePreview) {
      this.form.patchValue({ code: this.codePreview });
      this._codeGenerated = true;
    }
  }

  // ── Submit ────────────────────────────────────────────────────────────────
  submit(): void {
    this.formSubmitted = true;
    this.form.markAllAsTouched();

    if (this.form.invalid) {
      this._alertService.error('Please fix the errors in the form');
      return;
    }

    this.isSaving = true;
    // getRawValue() includes disabled controls (schoolId in edit mode)
    const raw = this.form.getRawValue();

    if (this.isEditMode) {
      const updateRequest: UpdateClassRequest = {
        name:           raw.name?.trim(),
        code:           raw.code?.trim() || undefined,
        level:          Number(raw.level),
        description:    raw.description?.trim() || undefined,
        capacity:       Number(raw.capacity),
        academicYearId: raw.academicYearId,
        teacherId:      raw.teacherId || undefined,
        isActive:       raw.isActive
      };

      this._classService.update(this.data.class!.id, updateRequest)
        .pipe(takeUntil(this._unsubscribe), finalize(() => this.isSaving = false))
        .subscribe({
          next: (res) => {
            if (res.success) {
              // ✅ Close and pass message to parent — parent shows the success alert
              this._dialogRef.close({
                success: true,
                data: res.data,
                message: res.message || 'Class updated successfully'
              });
            } else {
              this._alertService.error(res.message || 'Failed to update class');
            }
          },
          error: (err) => {
            console.error('Update error:', err);
            this._alertService.error(err?.error?.message || 'An error occurred while updating the class');
          }
        });

    } else {
      const createRequest: CreateClassRequest = {
        name:           raw.name?.trim(),
        code:           raw.code?.trim() || '',
        level:          Number(raw.level),
        description:    raw.description?.trim() || undefined,
        capacity:       Number(raw.capacity),
        academicYearId: raw.academicYearId,
        teacherId:      raw.teacherId || undefined,
        isActive:       raw.isActive ?? true
      };

      if (this.isSuperAdmin) {
        createRequest.schoolId = raw.schoolId;
      }

      this._classService.create(createRequest)
        .pipe(takeUntil(this._unsubscribe), finalize(() => this.isSaving = false))
        .subscribe({
          next: (res) => {
            if (res.success) {
              // ✅ Close and pass message to parent — parent shows the success alert
              this._dialogRef.close({
                success: true,
                data: res.data,
                message: res.message || 'Class created successfully'
              });
            } else {
              this._alertService.error(res.message || 'Failed to create class');
            }
          },
          error: (err) => {
            console.error('Create error:', err);
            this._alertService.error(err?.error?.message || 'An error occurred while creating the class');
          }
        });
    }
  }

  // ── Cancel ────────────────────────────────────────────────────────────────
  cancel(): void {
    if (this.form.dirty && !confirm('You have unsaved changes. Are you sure you want to cancel?')) {
      return;
    }
    this._dialogRef.close({ success: false });
  }

  // ── Helpers ──────────────────────────────────────────────────────────────
  getLevelDisplay(level: number): string {
    return this._classService.getCBCLevelDisplay(level);
  }

  hasError(field: string, error: string): boolean {
    const c = this.form.get(field);
    return !!(c && (this.formSubmitted || c.touched) && c.hasError(error));
  }
}