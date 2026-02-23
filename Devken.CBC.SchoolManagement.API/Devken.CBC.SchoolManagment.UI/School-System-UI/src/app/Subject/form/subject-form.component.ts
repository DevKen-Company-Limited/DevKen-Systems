// form/subject-form.component.ts
<<<<<<< HEAD
// KEY FIX: _buildForm() reads data.level (the SubjectDto field name)
// and resolves it via resolveCBCLevel() which handles "3", 3, or "Grade1".

import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule }          from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
=======
import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule }                          from '@angular/common';
import { Router, ActivatedRoute }               from '@angular/router';
>>>>>>> upstream/main
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
<<<<<<< HEAD
import {
  CBCLevelOptions, SubjectTypeOptions,
  resolveCBCLevel, resolveSubjectType,
} from '../Types/SubjectEnums';
=======
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
>>>>>>> upstream/main

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

<<<<<<< HEAD
=======
  // ─── Auth ─────────────────────────────────────────────────────────────────
>>>>>>> upstream/main
  get isSuperAdmin(): boolean {
    return this._authService.authUser?.isSuperAdmin ?? false;
  }

<<<<<<< HEAD
=======
  // ─── Breadcrumbs ──────────────────────────────────────────────────────────
>>>>>>> upstream/main
  get breadcrumbs(): Breadcrumb[] {
    return [
      { label: 'Dashboard', url: '/dashboard'        },
      { label: 'Academic',  url: '/academic'          },
      { label: 'Subjects',  url: '/academic/subjects' },
      { label: this.isEditMode ? 'Edit Subject' : 'Create Subject' },
    ];
  }

<<<<<<< HEAD
=======
  // ─── Lifecycle ────────────────────────────────────────────────────────────
>>>>>>> upstream/main
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

<<<<<<< HEAD
  private _buildForm(data?: any): void {
    // FIX: read data.level (SubjectDto field), NOT data.cbcLevel (which doesn't exist)
    const cbcLevel    = data ? resolveCBCLevel(data.level)       : null;
    const subjectType = data ? resolveSubjectType(data.subjectType) : null;
=======
  // ─── Form ─────────────────────────────────────────────────────────────────
  private _buildForm(data?: any): void {
    // Always resolve to integers — C# expects Core=1, Optional=2, etc.
    const subjectType = data ? resolveSubjectType(data.subjectType) : null;
    const cbcLevel    = data ? resolveCBCLevel(data.cbcLevel ?? data.level) : null;

    console.log('[SubjectForm] _buildForm resolved:', {
      raw_subjectType:      data?.subjectType,
      raw_cbcLevel:         data?.cbcLevel ?? data?.level,
      resolved_subjectType: subjectType,
      resolved_cbcLevel:    cbcLevel,
    });
>>>>>>> upstream/main

    const formConfig: any = {
      name:         [data?.name        ?? '', [Validators.required, Validators.maxLength(200)]],
      code:         [{ value: data?.code ?? '', disabled: true }],
      description:  [data?.description ?? '', Validators.maxLength(500)],
      subjectType:  [subjectType, Validators.required],
      cbcLevel:     [cbcLevel,    Validators.required],
<<<<<<< HEAD
=======
      isCompulsory: [data?.isCompulsory ?? false],
>>>>>>> upstream/main
      isActive:     [data?.isActive     ?? true],
    };

    if (this.isSuperAdmin) {
<<<<<<< HEAD
      formConfig.tenantId = [data?.tenantId ?? '', Validators.required];
=======
      formConfig.tenantId = [
        data?.schoolId ?? data?.tenantId ?? '',
        Validators.required,
      ];
>>>>>>> upstream/main
    }

    this.form = this._fb.group(formConfig);
  }

  private _loadSubject(id: string): void {
    this.isLoading = true;
    this._service.getById(id)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: (subject: any) => {
<<<<<<< HEAD
=======
          console.log('[SubjectForm] Raw API response:', subject);
>>>>>>> upstream/main
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

<<<<<<< HEAD
=======
  // ─── Validation Helpers ───────────────────────────────────────────────────
>>>>>>> upstream/main
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
<<<<<<< HEAD
      name: 'Name', subjectType: 'Subject type', cbcLevel: 'CBC Level', tenantId: 'School',
=======
      name:        'Name',
      subjectType: 'Subject type',
      cbcLevel:    'CBC Level',
      tenantId:    'School',
>>>>>>> upstream/main
    };
    return map[field] ?? field;
  }

<<<<<<< HEAD
  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
=======
  // ─── Submit ───────────────────────────────────────────────────────────────
  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
>>>>>>> upstream/main

    this.isSubmitting = true;
    const raw = this.form.getRawValue();

<<<<<<< HEAD
    const payload: any = {
      name:         raw.name?.trim(),
      description:  raw.description?.trim() || null,
      subjectType:  Number(raw.subjectType),
      cbcLevel:     Number(raw.cbcLevel),
      isActive:     raw.isActive,
    };

    if (this.isSuperAdmin) {
      payload.tenantId = raw.tenantId;
    }
=======
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
>>>>>>> upstream/main

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
<<<<<<< HEAD
        this._alertService.error(err.error?.message || err.error?.title || 'Failed to save subject');
=======
        console.error('[SubjectForm] API error:', err.error);
        this._alertService.error(
          err.error?.message || err.error?.title || 'Failed to save subject'
        );
>>>>>>> upstream/main
      },
    });
  }

  cancel(): void { this._router.navigate(['/academic/subjects']); }
}