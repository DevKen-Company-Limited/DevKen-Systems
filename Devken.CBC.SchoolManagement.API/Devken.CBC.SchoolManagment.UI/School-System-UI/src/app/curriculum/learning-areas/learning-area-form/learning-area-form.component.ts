import { Component, OnInit, OnDestroy, inject, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { FuseAlertComponent } from '@fuse/components/alert';

import { AuthService } from 'app/core/auth/auth.service';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { SchoolDto } from 'app/Tenant/types/school';
import { LearningAreaService } from 'app/core/DevKenService/curriculum/learning-area.service';
import { LearningAreaResponseDto } from 'app/curriculum/types/learning-area.dto ';
import { CBCLevelOptions } from 'app/Subject/Types/SubjectEnums';

export interface LearningAreaDialogData {
  editId?: string;
}

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
    MatProgressSpinnerModule,
    MatDialogModule,
    FuseAlertComponent,
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
  private _dialogRef = inject(MatDialogRef<LearningAreaFormComponent>);

  form!: FormGroup;
  isLoading = false;
  isSaving = false;
  schools: SchoolDto[] = [];
  cbcLevels = CBCLevelOptions;

  get editId(): string | undefined { return this.data?.editId; }
  get isEditMode(): boolean { return !!this.editId; }

  get isSuperAdmin(): boolean {
    return this._authService.authUser?.isSuperAdmin ?? false;
  }

  constructor(@Inject(MAT_DIALOG_DATA) public data: LearningAreaDialogData) {}

  ngOnInit(): void {
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
        next: (area: LearningAreaResponseDto) => {
          // Find numeric level value from CBCLevelOptions by matching label or value
          const levelValue = this.resolveLevelValue(area.level);
          this.form.patchValue({
            name:  area.name,
            code:  area.code ?? '',
            level: levelValue,
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

  /**
   * Resolve level to numeric value from either numeric string or label string.
   * The API may return the label (e.g. "Grade 1") or the numeric value (e.g. 3).
   */
  private resolveLevelValue(level: string | number): number | string {
    if (!level) return '';
    const asNum = Number(level);
    if (!isNaN(asNum) && asNum > 0) return asNum;
    // Try matching by label
    const opt = this.cbcLevels.find(l => l.label === level || l.label.toLowerCase() === String(level).toLowerCase());
    return opt ? opt.value : '';
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
        this._dialogRef.close({ success: true });
      },
      error: err => {
        this._alertService.error(err?.error?.message || 'Save failed');
        this.isSaving = false;
      },
    });
  }

  cancel(): void { this._dialogRef.close(); }

  getError(field: string): string {
    const c = this.form.get(field);
    if (!c?.errors) return '';
    if (c.errors['required']) return 'This field is required';
    if (c.errors['maxlength']) return 'Too long';
    return 'Invalid';
  }
}