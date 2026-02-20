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
import { StrandService } from 'app/core/DevKenService/curriculum/strand.service';
import { LearningAreaResponseDto } from 'app/curriculum/types/learning-area.dto ';

export interface StrandDialogData {
  editId?: string;
  /** Pre-select a learning area when opening from strands list filtered by LA */
  defaultLearningAreaId?: string;
}

@Component({
  selector: 'app-strand-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
    MatDialogModule, FuseAlertComponent,
  ],
  templateUrl: './strand-form.component.html',
})
export class StrandFormComponent implements OnInit, OnDestroy {
  private _destroy$ = new Subject<void>();
  private fb = inject(FormBuilder);
  private _service = inject(StrandService);
  private _laService = inject(LearningAreaService);
  private _authService = inject(AuthService);
  private _alertService = inject(AlertService);
  private _schoolService = inject(SchoolService);
  private _dialogRef = inject(MatDialogRef<StrandFormComponent>);

  form!: FormGroup;
  isLoading = false;
  isSaving = false;
  schools: SchoolDto[] = [];
  learningAreas: LearningAreaResponseDto[] = [];

  get editId(): string | undefined { return this.data?.editId; }
  get isEditMode(): boolean { return !!this.editId; }
  get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }

  constructor(@Inject(MAT_DIALOG_DATA) public data: StrandDialogData) {}

  ngOnInit(): void {
    this.buildForm();
    this.loadLearningAreas();
    if (this.isSuperAdmin) {
      this._schoolService.getAll().pipe(takeUntil(this._destroy$))
        .subscribe(res => { this.schools = res?.data ?? []; });
    }
    if (this.isEditMode && this.editId) this.loadExisting(this.editId);
  }

  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  private buildForm(): void {
    this.form = this.fb.group({
      name:           ['', [Validators.required, Validators.maxLength(150)]],
      learningAreaId: [this.data?.defaultLearningAreaId ?? '', Validators.required],
      ...(this.isSuperAdmin ? { tenantId: ['', Validators.required] } : {}),
    });
  }

  private loadLearningAreas(): void {
    this._laService.getAll().pipe(takeUntil(this._destroy$))
      .subscribe(data => { this.learningAreas = Array.isArray(data) ? data : []; });
  }

  private loadExisting(id: string): void {
    this.isLoading = true;
    this._service.getById(id).pipe(takeUntil(this._destroy$)).subscribe({
      next: strand => {
        this.form.patchValue({
          name: strand.name,
          learningAreaId: strand.learningAreaId,
          ...(this.isSuperAdmin ? { tenantId: strand.tenantId } : {}),
        });
        this.isLoading = false;
      },
      error: err => { this._alertService.error(err?.error?.message || 'Failed to load'); this.isLoading = false; },
    });
  }

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.isSaving = true;
    const val = this.form.value;
    const dto = { name: val.name, learningAreaId: val.learningAreaId };
    const obs = this.isEditMode && this.editId
      ? this._service.update(this.editId, dto)
      : this._service.create({ ...dto, tenantId: this.isSuperAdmin ? val.tenantId : undefined });

    obs.pipe(takeUntil(this._destroy$)).subscribe({
      next: () => {
        this._alertService.success(`Strand ${this.isEditMode ? 'updated' : 'created'} successfully`);
        this._dialogRef.close({ success: true });
      },
      error: err => { this._alertService.error(err?.error?.message || 'Save failed'); this.isSaving = false; },
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