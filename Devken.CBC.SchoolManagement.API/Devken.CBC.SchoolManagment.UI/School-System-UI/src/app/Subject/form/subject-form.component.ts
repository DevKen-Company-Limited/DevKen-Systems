// form/subject-form.component.ts
import {
  Component, OnInit, OnDestroy, HostListener, inject,
} from '@angular/core';
import { CommonModule }                   from '@angular/common';
import { Router, ActivatedRoute }         from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule }             from '@angular/material/form-field';
import { MatInputModule }                 from '@angular/material/input';
import { MatSelectModule }                from '@angular/material/select';
import { MatButtonModule }                from '@angular/material/button';
import { MatIconModule }                  from '@angular/material/icon';
import { MatCardModule }                  from '@angular/material/card';
import { MatSlideToggleModule }           from '@angular/material/slide-toggle';
import { MatDividerModule }               from '@angular/material/divider';
import { FuseAlertComponent }             from '@fuse/components/alert';
import { Subject }                        from 'rxjs';
import { takeUntil }                      from 'rxjs/operators';

import { AuthService }    from 'app/core/auth/auth.service';
import { SchoolService }  from 'app/core/DevKenService/Tenant/SchoolService';
import { AlertService }   from 'app/core/DevKenService/Alert/AlertService';
import { SchoolDto }      from 'app/Tenant/types/school';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { SubjectService } from 'app/core/DevKenService/SubjectService/SubjectService';
import { SubjectDto } from '../Types/subjectdto';
import { CBCLevelOptions, SubjectTypeOptions, normalizeSubject } from '../Types/SubjectEnums';


@Component({
  selector: 'app-subject-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatCardModule,
    MatSlideToggleModule, MatDividerModule,
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

  // ─── State ────────────────────────────────────────────────────────────────
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

  // ─── Breadcrumbs ─────────────────────────────────────────────────────────
  get breadcrumbs(): Breadcrumb[] {
    return [
      { label: 'Dashboard', url: '/dashboard'          },
      { label: 'Academic',  url: '/academic'            },
      { label: 'Subjects',  url: '/academic/subjects'   },
      { label: this.isEditMode ? 'Edit Subject' : 'Create Subject' },
    ];
  }

  // ─── Lifecycle ────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.subjectId  = this._route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.subjectId;
    this._buildForm();

    if (this.isSuperAdmin) {
      this._schoolService.getAll().pipe(takeUntil(this._destroy$)).subscribe(res => {
        this.schools = (res as any).data ?? [];
      });
    }

    if (this.isEditMode) {
      this._loadSubject(this.subjectId!);
    }
  }

  ngOnDestroy(): void {
    this._destroy$.next();
    this._destroy$.complete();
  }

  // ─── Form ─────────────────────────────────────────────────────────────────
  private _buildForm(data?: SubjectDto): void {
    const d = data ? normalizeSubject(data) : null;
    const formConfig: any = {
      name:         [d?.name        ?? '', [Validators.required, Validators.maxLength(200)]],
      code:         [d?.code        ?? '', [Validators.required, Validators.maxLength(20)]],
      description:  [d?.description ?? '', Validators.maxLength(500)],
      subjectType:  [d?.subjectType ?? '', Validators.required],
      cbcLevel:     [d?.cbcLevel    ?? '', Validators.required],
      isCompulsory: [d?.isCompulsory ?? false],
      isActive:     [d?.isActive    ?? true],
    };

    if (this.isSuperAdmin) {
      formConfig.tenantId = [d?.schoolId ?? '', Validators.required];
    }

    this.form = this._fb.group(formConfig);
  }

  private _loadSubject(id: string): void {
    this.isLoading = true;
    this._service.getById(id)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: subject => {
          this.isLoading = false;
          const norm = normalizeSubject(subject);
          this.form.patchValue({
            name:         norm.name,
            code:         norm.code,
            description:  norm.description,
            subjectType:  norm.subjectType,
            cbcLevel:     norm.cbcLevel,
            isCompulsory: norm.isCompulsory,
            isActive:     norm.isActive,
            ...(this.isSuperAdmin ? { tenantId: norm.schoolId } : {}),
          });
        },
        error: err => {
          this.isLoading = false;
          this._alertService.error(err.error?.message || 'Failed to load subject');
          this._router.navigate(['/academic/subjects']);
        },
      });
  }

  // ─── Helpers ─────────────────────────────────────────────────────────────
  isInvalid(field: string): boolean {
    const c = this.form.get(field);
    return !!(c && c.invalid && (c.dirty || c.touched));
  }

  getError(field: string): string {
    const c = this.form.get(field);
    if (!c?.errors) return '';
    if (c.errors['required'])   return `${this._label(field)} is required`;
    if (c.errors['maxlength'])  return `${this._label(field)} is too long`;
    return 'Invalid value';
  }

  private _label(field: string): string {
    const map: Record<string, string> = {
      name: 'Name', code: 'Code', subjectType: 'Subject type',
      cbcLevel: 'CBC Level', tenantId: 'School',
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
    const raw     = this.form.getRawValue();
    const payload = {
      name:        raw.name?.trim(),
      code:        raw.code?.trim().toUpperCase(),
      description: raw.description?.trim() || undefined,
      subjectType: Number(raw.subjectType),
      cbcLevel:    Number(raw.cbcLevel),
      isCompulsory: raw.isCompulsory,
      isActive:    raw.isActive,
      ...(this.isSuperAdmin ? { tenantId: raw.tenantId } : {}),
    };

    const request$ = this.isEditMode
      ? this._service.update(this.subjectId!, payload)
      : this._service.create(payload as any);

    request$.pipe(takeUntil(this._destroy$)).subscribe({
      next: res => {
        this.isSubmitting = false;
        this._alertService.success(
          this.isEditMode ? 'Subject updated successfully!' : 'Subject created successfully!'
        );
        setTimeout(() => this._router.navigate(['/academic/subjects']), 1200);
      },
      error: err => {
        this.isSubmitting = false;
        this._alertService.error(err.error?.message || 'Failed to save subject');
      },
    });
  }

  cancel(): void { this._router.navigate(['/academic/subjects']); }
}