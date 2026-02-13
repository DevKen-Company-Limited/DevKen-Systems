import { Component, OnInit, OnDestroy, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, FormGroup } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, takeUntil, debounceTime, forkJoin } from 'rxjs';

import { BaseFormDialog } from 'app/shared/dialogs/BaseFormDialog';
import { ClassService } from 'app/core/DevKenService/ClassService';
                

import {
  CreateClassRequest,
  UpdateClassRequest,
  ClassDto,
  AcademicYearOption,
  TeacherOption
} from 'app/Classes/Types/Class';
import { AcademicYearService } from 'app/core/DevKenService/AcademicYearService/AcademicYearService';
import { TeacherService } from 'app/core/DevKenService/Teacher/TeacherService';

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
    MatSnackBarModule,
    MatSelectModule,
    MatIconModule,
    MatSlideToggleModule,
    MatTooltipModule
  ],
  templateUrl: './create-edit-class-dialog.component.html',
  styles: [`
    :host ::ng-deep .mat-mdc-dialog-container {
      --mdc-dialog-container-shape: 12px;
    }
  `]
})
export class CreateEditClassDialogComponent
  extends BaseFormDialog<CreateClassRequest, UpdateClassRequest, ClassDto, { mode: 'create' | 'edit'; class?: ClassDto; schoolId?: string }>
  implements OnInit, OnDestroy
{
  private _unsubscribe = new Subject<void>();
  private _codeGenerated = false;

  // Dropdown options
  cbcLevels: { value: number; label: string }[] = [];
  academicYears: AcademicYearOption[] = [];
  teachers: TeacherOption[] = [];

  // Loading states
  isLoadingAcademicYears = false;
  isLoadingTeachers = false;

  constructor(
    fb: FormBuilder,
    protected service: ClassService,
    snackBar: MatSnackBar,
    dialogRef: MatDialogRef<CreateEditClassDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public override data: { mode: 'create' | 'edit'; class?: ClassDto; schoolId?: string },
    private academicYearService: AcademicYearService,   // <-- INJECTED
    private teacherService: TeacherService              // <-- INJECTED
  ) {
    super(fb, service, snackBar, dialogRef, data);
  }

  protected buildForm(): FormGroup {
    return this.fb.group({
      schoolId: [this.data.schoolId || '', Validators.required],
      name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      code: ['', [Validators.required, Validators.pattern(/^[A-Z0-9-]+$/), Validators.minLength(2), Validators.maxLength(10)]],
      level: ['', Validators.required],
      description: ['', Validators.maxLength(500)],
      capacity: [40, [Validators.required, Validators.min(1), Validators.max(100)]],
      academicYearId: ['', Validators.required],
      teacherId: [''],
      isActive: [true]
    });
  }

  ngOnInit(): void {
    this.init();
    this.cbcLevels = this.service.getAllCBCLevels();
    this.loadDropdownData();
    this.setupCodeGeneration();
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  /** Load academic years and teachers using dedicated services */
  private loadDropdownData(): void {
    const schoolId = this.data.schoolId || this.data.class?.schoolId;

    if (!schoolId) {
      this.snackBar.open('School ID is missing. Cannot load dropdowns.', 'Close', { duration: 5000 });
      return;
    }

    this.isLoadingAcademicYears = true;
    this.isLoadingTeachers = true;

    forkJoin({
      academicYears: this.academicYearService.getAll(schoolId),
      teachers: this.teacherService.getAll(schoolId)   // adjust method name if needed
    })
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next: ({ academicYears, teachers }) => {
          // --- Academic Years ---
          if (academicYears.success && academicYears.data) {
            this.academicYears = academicYears.data.map(ay => ({
              id: ay.id,
              name: ay.name,
              code: ay.code
            }));
          } else {
            console.warn('Academic years API error:', academicYears.message);
            this.snackBar.open(`Failed to load academic years: ${academicYears.message || 'Unknown error'}`, 'Close', { duration: 4000 });
          }

          // --- Teachers ---
          if (teachers.success && teachers.data) {
            this.teachers = teachers.data.map(t => ({
              id: t.id,
              name: `${t.firstName} ${t.lastName}`,
              teacherNumber: t.teacherNumber
            }));
          } else {
            console.warn('Teachers API error:', teachers.message);
            this.snackBar.open(`Failed to load teachers: ${teachers.message || 'Unknown error'}`, 'Close', { duration: 4000 });
          }

          this.isLoadingAcademicYears = false;
          this.isLoadingTeachers = false;
        },
        error: (error) => {
          console.error('Error loading dropdown data:', error);
          this.snackBar.open('Failed to load dropdown data. Please try again.', 'Close', { duration: 5000 });
          this.isLoadingAcademicYears = false;
          this.isLoadingTeachers = false;
        }
      });
  }

  /** Auto-generate code from level and name */
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
          this._codeGenerated = true;   // user manually edited code
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
      this.snackBar.open('Please select a level and enter a name first', 'Close', {
        duration: 3000,
        horizontalPosition: 'end',
        verticalPosition: 'top'
      });
    }
  }

  submit(): void {
    this.form.markAllAsTouched();

    if (this.form.invalid) {
      const firstError = Object.keys(this.form.controls)
        .find(key => this.form.get(key)?.invalid);
      if (firstError) {
        const element = document.querySelector(`[formControlName="${firstError}"]`);
        element?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }

      this.snackBar.open('Please fix the errors in the form', 'Close', {
        duration: 3000,
        horizontalPosition: 'end',
        verticalPosition: 'top'
      });
      return;
    }

    this.save(
      // createMapper
      (raw) => ({
        schoolId: raw.schoolId,
        name: raw.name?.trim(),
        code: raw.code?.trim().toUpperCase(),
        level: Number(raw.level),
        description: raw.description?.trim() || undefined,
        capacity: Number(raw.capacity),
        academicYearId: raw.academicYearId,
        teacherId: raw.teacherId || undefined,
        isActive: raw.isActive ?? true
      } as CreateClassRequest),

      // updateMapper
      (raw) => ({
        name: raw.name?.trim(),
        code: raw.code?.trim().toUpperCase(),
        level: Number(raw.level),
        description: raw.description?.trim() || undefined,
        capacity: Number(raw.capacity),
        academicYearId: raw.academicYearId,
        teacherId: raw.teacherId || undefined,
        isActive: raw.isActive
      } as UpdateClassRequest),

      // getId
      () => this.data.class?.id ?? ''
    );
  }

  cancel(): void {
    if (this.form.dirty && !confirm('You have unsaved changes. Are you sure you want to cancel?')) {
      return;
    }
    this.close({ success: false });
  }

  getLevelDisplay(level: number): string {
    return this.service.getCBCLevelDisplay(level);
  }
}