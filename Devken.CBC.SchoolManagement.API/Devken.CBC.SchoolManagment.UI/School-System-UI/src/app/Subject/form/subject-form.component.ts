// form/subject-form.component.ts
import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule }                          from '@angular/common';
import { Router, ActivatedRoute }               from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule }   from '@angular/material/form-field';
import { MatInputModule }       from '@angular/material/input';
import { MatSelectModule }      from '@angular/material/select';
import { MatButtonModule }      from '@angular/material/button';
import { MatIconModule }        from '@angular/material/icon';
import { MatCardModule }        from '@angular/material/card';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatDividerModule }     from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FuseAlertComponent }   from '@fuse/components/alert';
import { Subject }              from 'rxjs';
import { takeUntil }            from 'rxjs/operators';
import { AuthService }   from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { AlertService }  from 'app/core/DevKenService/Alert/AlertService';
import { SchoolDto }     from 'app/Tenant/types/school';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { SubjectService } from 'app/core/DevKenService/SubjectService/SubjectService';
import { CBCLevelOptions, SubjectTypeOptions, normalizeSubject } from '../Types/SubjectEnums';


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

  get isSuperAdmin(): boolean {
    return this._authService.authUser?.isSuperAdmin ?? false;
  }

  get breadcrumbs(): Breadcrumb[] {
    return [
      { label: 'Dashboard', url: '/dashboard'        },
      { label: 'Academic',  url: '/academic'          },
      { label: 'Subjects',  url: '/academic/subjects' },
      { label: this.isEditMode ? 'Edit Subject' : 'Create Subject' },
    ];
  }

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

  // ─── Build form with optional pre-populated data ──────────────────────────
  private _buildForm(data?: any): void {
    const d = data ? normalizeSubject(data) : null;

    const formConfig: any = {
      name:        [d?.name        ?? '',  [Validators.required, Validators.maxLength(200)]],
      code:        [{ value: d?.code ?? '', disabled: true }],
      description: [d?.description ?? '',  Validators.maxLength(500)],
      subjectType: [d?.subjectType ?? '',  Validators.required],   // string: "Core", "Optional" etc.
      level:       [d?.level !== '' && d?.level !== null && d?.level !== undefined ? d.level : '', Validators.required],  // number
      isActive:    [d?.isActive ?? true],
    };

    if (this.isSuperAdmin) {
      formConfig.tenantId = [d?.tenantId ?? '', Validators.required];
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
      name: 'Name', subjectType: 'Subject type',
      level: 'CBC Level', tenantId: 'School',
    };
    return map[field] ?? field;
  }

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    this.isSubmitting = true;
    const raw = this.form.getRawValue();

    const payload: any = {
      name:        raw.name?.trim(),
      description: raw.description?.trim() || null,
      subjectType: raw.subjectType,          // string name
      level:       Number(raw.level),        // number
      isActive:    raw.isActive,
      ...(this.isSuperAdmin ? { tenantId: raw.tenantId } : {}),
    };

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
        this._alertService.error(err.error?.message || 'Failed to save subject');
      },
    });
  }

  cancel(): void { this._router.navigate(['/academic/subjects']); }
}