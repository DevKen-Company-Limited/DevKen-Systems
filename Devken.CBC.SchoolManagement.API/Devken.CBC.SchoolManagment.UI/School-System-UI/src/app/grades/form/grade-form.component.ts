// form/grade-form.component.ts
import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule }                          from '@angular/common';
import { Router, ActivatedRoute }               from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule }       from '@angular/material/form-field';
import { MatInputModule }           from '@angular/material/input';
import { MatSelectModule }          from '@angular/material/select';
import { MatButtonModule }          from '@angular/material/button';
import { MatIconModule }            from '@angular/material/icon';
import { MatCardModule }            from '@angular/material/card';
import { MatSlideToggleModule }     from '@angular/material/slide-toggle';
import { MatDatepickerModule }      from '@angular/material/datepicker';
import { MatNativeDateModule }      from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FuseAlertComponent }       from '@fuse/components/alert';
import { Subject }                  from 'rxjs';
import { takeUntil }                from 'rxjs/operators';

import { AuthService }   from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { AlertService }  from 'app/core/DevKenService/Alert/AlertService';
import { SchoolDto }     from 'app/Tenant/types/school';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { GradeService }  from 'app/core/DevKenService/GradeService/GradeService';
import { GradeLetterOptions, GradeTypeOptions, resolveGradeLetter, resolveGradeType } from '../types/GradeEnums';

@Component({
  selector: 'app-grade-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatCardModule,
    MatSlideToggleModule, MatDatepickerModule, MatNativeDateModule,
    MatProgressSpinnerModule, FuseAlertComponent, PageHeaderComponent,
  ],
  templateUrl: './grade-form.component.html',
})
export class GradeFormComponent implements OnInit, OnDestroy {

  private _destroy$      = new Subject<void>();
  private _router        = inject(Router);
  private _route         = inject(ActivatedRoute);
  private _fb            = inject(FormBuilder);
  private _service       = inject(GradeService);
  private _authService   = inject(AuthService);
  private _schoolService = inject(SchoolService);
  private _alertService  = inject(AlertService);

  form!:       FormGroup;
  isEditMode   = false;
  gradeId:     string | null = null;
  isLoading    = false;
  isSubmitting = false;
  schools:     SchoolDto[] = [];

  gradeLetterOptions = GradeLetterOptions;
  gradeTypeOptions   = GradeTypeOptions;

  // ─── Auth ─────────────────────────────────────────────────────────────────
  get isSuperAdmin(): boolean {
    return this._authService.authUser?.isSuperAdmin ?? false;
  }

  get breadcrumbs(): Breadcrumb[] {
    return [
      { label: 'Dashboard', url: '/dashboard'      },
      { label: 'Academic',  url: '/academic'        },
      { label: 'Grades',    url: '/academic/grades' },
      { label: this.isEditMode ? 'Edit Grade' : 'Add Grade' },
    ];
  }

  // ─── Lifecycle ────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.gradeId    = this._route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.gradeId;

    if (this.isSuperAdmin) {
      this._schoolService.getAll()
        .pipe(takeUntil(this._destroy$))
        .subscribe(res => { this.schools = (res as any).data ?? []; });
    }

    if (this.isEditMode) {
      this._loadGrade(this.gradeId!);
    } else {
      this._buildForm();
    }
  }

  ngOnDestroy(): void {
    this._destroy$.next();
    this._destroy$.complete();
  }

  // ─── Form ─────────────────────────────────────────────────────────────────
  private _buildForm(data?: any): void {
    const gradeLetter = data ? resolveGradeLetter(data.gradeLetter) : null;
    const gradeType   = data ? resolveGradeType(data.gradeType)     : null;
    const assessDate  = data?.assessmentDate ? new Date(data.assessmentDate) : new Date();

    const formConfig: any = {
      studentId:      [data?.studentId  ?? '', Validators.required],
      subjectId:      [data?.subjectId  ?? '', Validators.required],
      termId:         [data?.termId     ?? null],
      assessmentId:   [data?.assessmentId ?? null],
      score:          [data?.score      ?? null, [Validators.min(0)]],
      maximumScore:   [data?.maximumScore ?? null, [Validators.min(0)]],
      gradeLetter:    [gradeLetter],
      gradeType:      [gradeType],
      assessmentDate: [assessDate, Validators.required],
      remarks:        [data?.remarks ?? '', Validators.maxLength(500)],
      isFinalized:    [data?.isFinalized ?? false],
    };

    if (this.isSuperAdmin) {
      formConfig.tenantId = [data?.tenantId ?? data?.schoolId ?? '', Validators.required];
    }

    this.form = this._fb.group(formConfig);

    // Disable studentId/subjectId in edit mode (they define the grade record)
    if (this.isEditMode) {
      this.form.get('studentId')?.disable();
      this.form.get('subjectId')?.disable();
    }
  }

  private _loadGrade(id: string): void {
    this.isLoading = true;
    this._service.getById(id)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: (grade: any) => {
          this._buildForm(grade);
          this.isLoading = false;
        },
        error: err => {
          this.isLoading = false;
          this._alertService.error(err.error?.message || 'Failed to load grade');
          this._router.navigate(['/academic/grades']);
        },
      });
  }

  // ─── Validation ──────────────────────────────────────────────────────────
  isInvalid(field: string): boolean {
    const c = this.form.get(field);
    return !!(c && c.invalid && (c.dirty || c.touched));
  }

  getError(field: string): string {
    const c = this.form.get(field);
    if (!c?.errors) return '';
    if (c.errors['required']) return `${this._label(field)} is required`;
    if (c.errors['min'])      return `${this._label(field)} must be 0 or more`;
    if (c.errors['maxlength'])return `${this._label(field)} is too long`;
    return 'Invalid value';
  }

  private _label(field: string): string {
    const map: Record<string, string> = {
      studentId:      'Student',
      subjectId:      'Subject',
      score:          'Score',
      maximumScore:   'Maximum score',
      assessmentDate: 'Assessment date',
      tenantId:       'School',
    };
    return map[field] ?? field;
  }

  // ─── Submit ───────────────────────────────────────────────────────────────
  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const raw = this.form.getRawValue();

    const dateStr = raw.assessmentDate instanceof Date
      ? raw.assessmentDate.toISOString()
      : raw.assessmentDate;

    if (this.isEditMode) {
      const payload = {
        score:          raw.score         !== '' ? Number(raw.score)       : null,
        maximumScore:   raw.maximumScore  !== '' ? Number(raw.maximumScore): null,
        gradeLetter:    raw.gradeLetter   !== null ? Number(raw.gradeLetter) : null,
        gradeType:      raw.gradeType     !== null ? Number(raw.gradeType)   : null,
        assessmentDate: dateStr,
        remarks:        raw.remarks?.trim() || null,
        isFinalized:    raw.isFinalized,
      };

      this._service.update(this.gradeId!, payload)
        .pipe(takeUntil(this._destroy$))
        .subscribe({
          next: () => {
            this.isSubmitting = false;
            this._alertService.success('Grade updated successfully!');
            setTimeout(() => this._router.navigate(['/academic/grades']), 1200);
          },
          error: err => {
            this.isSubmitting = false;
            this._alertService.error(err.error?.message || 'Failed to update grade');
          },
        });
    } else {
      const payload: any = {
        studentId:      raw.studentId,
        subjectId:      raw.subjectId,
        termId:         raw.termId        || null,
        assessmentId:   raw.assessmentId  || null,
        score:          raw.score         !== '' ? Number(raw.score)       : null,
        maximumScore:   raw.maximumScore  !== '' ? Number(raw.maximumScore): null,
        gradeLetter:    raw.gradeLetter   !== null ? Number(raw.gradeLetter) : null,
        gradeType:      raw.gradeType     !== null ? Number(raw.gradeType)   : null,
        assessmentDate: dateStr,
        remarks:        raw.remarks?.trim() || null,
        isFinalized:    raw.isFinalized,
      };

      if (this.isSuperAdmin) payload.tenantId = raw.tenantId;

      this._service.create(payload)
        .pipe(takeUntil(this._destroy$))
        .subscribe({
          next: () => {
            this.isSubmitting = false;
            this._alertService.success('Grade created successfully!');
            setTimeout(() => this._router.navigate(['/academic/grades']), 1200);
          },
          error: err => {
            this.isSubmitting = false;
            this._alertService.error(err.error?.message || err.error?.title || 'Failed to save grade');
          },
        });
    }
  }

  cancel(): void { this._router.navigate(['/academic/grades']); }
}