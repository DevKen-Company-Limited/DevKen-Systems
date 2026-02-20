import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FuseAlertComponent } from '@fuse/components/alert';

import { AuthService } from 'app/core/auth/auth.service';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { SchoolDto } from 'app/Tenant/types/school';
import { LearningAreaService } from 'app/core/DevKenService/curriculum/learning-area.service';
import { CBCLevelOptions } from 'app/Subject/Types/SubjectEnums';


@Component({
  selector: 'app-learning-area-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatProgressSpinnerModule,
    FuseAlertComponent,
    PageHeaderComponent,
  ],
  templateUrl: './learning-area-form.component.html',
})
export class LearningAreaFormComponent implements OnInit, OnDestroy {
  private _destroy$ = new Subject<void>();
  private fb = inject(FormBuilder);
  private _service = inject(LearningAreaService);
  private _authService = inject(AuthService);
  private _alertService = inject(AlertService);
  private _schoolService = inject(SchoolService);
  private _router = inject(Router);
  private _route = inject(ActivatedRoute);

  form!: FormGroup;
  isEditMode = false;
  editId: string | null = null;
  isLoading = false;
  isSaving = false;
  schools: SchoolDto[] = [];

  cbcLevels = CBCLevelOptions;

  get isSuperAdmin(): boolean {
    return this._authService.authUser?.isSuperAdmin ?? false;
  }

  get breadcrumbs(): Breadcrumb[] {
    return [
      { label: 'Dashboard', url: '/dashboard' },
      { label: 'Curriculum' },
      { label: 'Learning Areas', url: '/curriculum/learning-areas' },
      { label: this.isEditMode ? 'Edit' : 'Create' },
    ];
  }

  get title(): string {
    return this.isEditMode ? 'Edit Learning Area' : 'Create Learning Area';
  }

  ngOnInit(): void {
    this.editId = this._route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.editId;

    this.buildForm();

    if (this.isSuperAdmin) {
      this._schoolService.getAll()
        .pipe(takeUntil(this._destroy$))
        .subscribe(res => { this.schools = res?.data ?? []; });
    }

    if (this.isEditMode && this.editId) {
      this.loadExisting(this.editId);
    }
  }

  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  private buildForm(): void {
    this.form = this.fb.group({
      name:  ['', [Validators.required, Validators.maxLength(150)]],
      code:  ['', Validators.maxLength(20)],
      level: ['', Validators.required],
      ...(this.isSuperAdmin ? { tenantId: ['', Validators.required] } : {}),
    });
  }

  private loadExisting(id: string): void {
    this.isLoading = true;
    this._service.getById(id)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: area => {
          this.form.patchValue({
            name:  area.name,
            code:  area.code ?? '',
            level: area.level,
            ...(this.isSuperAdmin ? { tenantId: area.tenantId } : {}),
          });
          this.isLoading = false;
        },
        error: err => {
          this._alertService.error(err?.error?.message || 'Failed to load learning area');
          this.isLoading = false;
        },
      });
  }

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    this.isSaving = true;
    const val = this.form.value;
    const dto = { name: val.name, code: val.code || undefined, level: Number(val.level) };

    const obs = this.isEditMode && this.editId
      ? this._service.update(this.editId, dto as any)
      : this._service.create({ ...dto, tenantId: this.isSuperAdmin ? val.tenantId : undefined } as any);

    obs.pipe(takeUntil(this._destroy$)).subscribe({
      next: () => {
        this._alertService.success(`Learning area ${this.isEditMode ? 'updated' : 'created'} successfully`);
        this._router.navigate(['/curriculum/learning-areas']);
      },
      error: err => {
        this._alertService.error(err?.error?.message || 'Save failed');
        this.isSaving = false;
      },
    });
  }

  cancel(): void { this._router.navigate(['/curriculum/learning-areas']); }

  isInvalid(field: string): boolean {
    const c = this.form.get(field);
    return !!(c && c.invalid && (c.dirty || c.touched));
  }

  getError(field: string): string {
    const c = this.form.get(field);
    if (!c?.errors) return '';
    if (c.errors['required']) return `This field is required`;
    if (c.errors['maxlength']) return `Too long`;
    return 'Invalid';
  }
}