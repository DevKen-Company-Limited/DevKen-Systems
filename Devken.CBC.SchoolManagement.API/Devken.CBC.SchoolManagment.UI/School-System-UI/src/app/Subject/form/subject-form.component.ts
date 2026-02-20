// form/subject-form.component.ts
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
import { MatDividerModule }         from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FuseAlertComponent }       from '@fuse/components/alert';
import { Subject }                  from 'rxjs';
import { takeUntil }                from 'rxjs/operators';

import { AuthService }   from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { AlertService }  from 'app/core/DevKenService/Alert/AlertService';
import { SchoolDto }     from 'app/Tenant/types/school';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { SubjectService } from 'app/core/DevKenService/SubjectService/SubjectService';
import { CBCLevelOptions, SubjectTypeOptions } from '../Types/SubjectEnums';

// ── Helpers ───────────────────────────────────────────────────────────────────

/**
 * Resolve subjectType to the integer the C# API expects.
 * C# enum: Core=1, Optional=2, Elective=3, CoCurricular=4
 * API may return the integer directly or the string name.
 */
function resolveSubjectType(val: any): number | null {
  if (val === null || val === undefined || val === '') return null;
  const n = Number(val);
  if (!isNaN(n) && n > 0) return n;
  // Fallback: string name → int (handles legacy API responses)
  const map: Record<string, number> = {
    core: 1, optional: 2, elective: 3,
    cocurricular: 4, extracurricular: 4,
  };
  return map[String(val).toLowerCase()] ?? null;
}

/**
 * Resolve cbcLevel to the integer the C# API expects.
 * C# enum: PP1=1, PP2=2, Grade1=3 … Grade12=14
 * API may return the integer directly or the string name.
 */
function resolveCBCLevel(val: any): number | null {
  if (val === null || val === undefined || val === '') return null;
  const n = Number(val);
  if (!isNaN(n) && n > 0) return n;
  // Fallback: string name → int
  const map: Record<string, number> = {
    pp1: 1, preprimary1: 1,
    pp2: 2, preprimary2: 2,
    grade1: 3,  grade2: 4,  grade3: 5,  grade4: 6,  grade5: 7,
    grade6: 8,  grade7: 9,  grade8: 10, grade9: 11, grade10: 12,
    grade11: 13, grade12: 14,
  };
  return map[String(val).toLowerCase()] ?? null;
}

@Component({
  selector: 'app-subject-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatCardModule,
    MatSlideToggleModule, MatDividerModule, MatProgressSpinnerModule,
    FuseAlertComponent, PageHeaderComponent,
  ],
  templateUrl: './subject-form.component.html',
})
export class SubjectFormComponent implements OnInit, OnDestroy {

  private _destroy$      = new Subject<void>();
  private _router        = inject(Router);
  private _route         = inject(ActivatedRoute);
  private _fb            = inject(FormBuilder);
  private _service       = inject(SubjectService);
  private _authService   = inject(AuthService);
  private _schoolService = inject(SchoolService);
  private _alertService  = inject(AlertService);

  form!:       FormGroup;
  isEditMode   = false;
  subjectId:   string | null = null;
  isLoading    = false;
  isSubmitting = false;
  schools:     SchoolDto[] = [];

  cbcLevels    = CBCLevelOptions;
  subjectTypes = SubjectTypeOptions;

  // ─── Auth ─────────────────────────────────────────────────────────────────
  get isSuperAdmin(): boolean {
    return this._authService.authUser?.isSuperAdmin ?? false;
  }

  // ─── Breadcrumbs ──────────────────────────────────────────────────────────
  get breadcrumbs(): Breadcrumb[] {
    return [
      { label: 'Dashboard', url: '/dashboard'        },
      { label: 'Academic',  url: '/academic'          },
      { label: 'Subjects',  url: '/academic/subjects' },
      { label: this.isEditMode ? 'Edit Subject' : 'Create Subject' },
    ];
  }

  // ─── Lifecycle ────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.subjectId  = this._route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.subjectId;

    if (this.isSuperAdmin) {
      this._schoolService.getAll()
        .pipe(takeUntil(this._destroy$))
        .subscribe(res => { this.schools = (res as any).data ?? []; });
    }

    if (this.isEditMode) {
      this._loadSubject(this.subjectId!);
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
  const subjectType = data ? resolveSubjectType(data.subjectType) : null;
  const cbcLevel    = data ? resolveCBCLevel(data.level) : null;   // use data.level not data.cbcLevel

  const formConfig: any = {
    name:         [data?.name        ?? '', [Validators.required, Validators.maxLength(200)]],
    code:         [{ value: data?.code ?? '', disabled: true }],
    description:  [data?.description ?? '', Validators.maxLength(500)],
    subjectType:  [subjectType, Validators.required],
    cbcLevel:     [cbcLevel,    Validators.required],
    isCompulsory: [data?.isCompulsory ?? false],
    isActive:     [data?.isActive     ?? true],
  };

  if (this.isSuperAdmin) {
    formConfig.tenantId = [
      data?.tenantId ?? '',   // backend returns tenantId, not schoolId
      Validators.required,
    ];
  }

  this.form = this._fb.group(formConfig);
}

  private _loadSubject(id: string): void {
  this.isLoading = true;
  this._service.getById(id)
    .pipe(takeUntil(this._destroy$))
    .subscribe({
      next: (subject: any) => {
        this._buildForm(subject);
        this.isLoading = false;
      },
      error: err => {
        this.isLoading = false;
        this._alertService.error(err.error?.message || 'Failed to load subject');
        this._router.navigate(['/academic/subjects']);
      },
    });
}

  // ─── Validation Helpers ───────────────────────────────────────────────────
  isInvalid(field: string): boolean {
    const c = this.form.get(field);
    return !!(c && c.invalid && (c.dirty || c.touched));
  }

  getError(field: string): string {
    const c = this.form.get(field);
    if (!c?.errors) return '';
    if (c.errors['required'])  return `${this._label(field)} is required`;
    if (c.errors['maxlength']) return `${this._label(field)} is too long`;
    return 'Invalid value';
  }

  private _label(field: string): string {
    const map: Record<string, string> = {
      name:        'Name',
      subjectType: 'Subject type',
      cbcLevel:    'CBC Level',
      tenantId:    'School',
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

    const payload = {
      name:         raw.name?.trim(),
      description:  raw.description?.trim() || null,
      subjectType:  Number(raw.subjectType),  // 1=Core, 2=Optional, 3=Elective, 4=CoCurricular
      cbcLevel:     Number(raw.cbcLevel),     // 1=PP1, 2=PP2, 3=Grade1 … 14=Grade12
      isCompulsory: raw.isCompulsory,
      isActive:     raw.isActive,
      ...(this.isSuperAdmin ? { tenantId: raw.tenantId } : {}),
    };

    console.log('[SubjectForm] Submitting payload:', payload);

    const request$ = this.isEditMode
      ? this._service.update(this.subjectId!, payload)
      : this._service.create(payload);

    request$.pipe(takeUntil(this._destroy$)).subscribe({
      next: () => {
        this.isSubmitting = false;
        this._alertService.success(
          this.isEditMode ? 'Subject updated successfully!' : 'Subject created successfully!'
        );
        setTimeout(() => this._router.navigate(['/academic/subjects']), 1200);
      },
      error: err => {
        this.isSubmitting = false;
        console.error('[SubjectForm] API error:', err.error);
        this._alertService.error(
          err.error?.message || err.error?.title || 'Failed to save subject'
        );
      },
    });
  }

  cancel(): void { this._router.navigate(['/academic/subjects']); }
}