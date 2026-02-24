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
import { SubStrandService } from 'app/core/DevKenService/curriculum/substrand.service ';
import { LearningAreaResponseDto } from 'app/curriculum/types/learning-area.dto ';
import { StrandResponseDto } from 'app/curriculum/types/strand.dto ';

export interface SubStrandDialogData {
  editId?: string;
  defaultStrandId?: string;
}

@Component({
  selector: 'app-sub-strand-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
    MatDialogModule, FuseAlertComponent,
  ],
  templateUrl: './sub-strand-form.component.html',
  styles: [`
    :host {
      display: flex;
      flex-direction: column;
      height: 100%;
      overflow: hidden;
    }
  `],
})
export class SubStrandFormComponent implements OnInit, OnDestroy {
  private _destroy$ = new Subject<void>();
  private fb = inject(FormBuilder);
  private _service = inject(SubStrandService);
  private _strandService = inject(StrandService);
  private _laService = inject(LearningAreaService);
  private _authService = inject(AuthService);
  private _alertService = inject(AlertService);
  private _schoolService = inject(SchoolService);
  private _dialogRef = inject(MatDialogRef<SubStrandFormComponent>);

  form!: FormGroup;
  isLoading = false;
  isSaving = false;
  schools: SchoolDto[] = [];
  learningAreas: LearningAreaResponseDto[] = [];
  strands: StrandResponseDto[] = [];
  filteredStrands: StrandResponseDto[] = [];

  get editId(): string | undefined { return this.data?.editId; }
  get isEditMode(): boolean { return !!this.editId; }
  get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }

  constructor(@Inject(MAT_DIALOG_DATA) public data: SubStrandDialogData) {
    this._dialogRef.addPanelClass('no-padding-dialog');
  }

  ngOnInit(): void {
    this.buildForm();
    this.loadLookups();
    if (this.isSuperAdmin) {
      this._schoolService.getAll().pipe(takeUntil(this._destroy$))
        .subscribe(res => { this.schools = res?.data ?? []; });
    }
    if (this.isEditMode && this.editId) this.loadExisting(this.editId);
  }

  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  private buildForm(): void {
    this.form = this.fb.group({
      name:            ['', [Validators.required, Validators.maxLength(150)]],
      learningAreaId:  [''],
      strandId:        [this.data?.defaultStrandId ?? '', Validators.required],
      ...(this.isSuperAdmin ? { tenantId: ['', Validators.required] } : {}),
    });
  }

  private loadLookups(): void {
    this._laService.getAll().pipe(takeUntil(this._destroy$))
      .subscribe(data => { this.learningAreas = Array.isArray(data) ? data : []; });

    this._strandService.getAll().pipe(takeUntil(this._destroy$))
      .subscribe(data => {
        this.strands = Array.isArray(data) ? data : [];
        this.filteredStrands = [...this.strands];

        // If a strand is pre-selected, also pre-select its learning area
        if (this.data?.defaultStrandId) {
          const strand = this.strands.find(s => s.id === this.data.defaultStrandId);
          if (strand?.learningAreaId) {
            this.form.patchValue({ learningAreaId: strand.learningAreaId }, { emitEvent: false });
            this.filteredStrands = this.strands.filter(s => s.learningAreaId === strand.learningAreaId);
          }
        }
      });
  }

  onLearningAreaChange(learningAreaId: string): void {
    this.form.patchValue({ strandId: '' });
    this.filteredStrands = learningAreaId
      ? this.strands.filter(s => s.learningAreaId === learningAreaId)
      : [...this.strands];
  }

  private loadExisting(id: string): void {
    this.isLoading = true;
    this._service.getById(id).pipe(takeUntil(this._destroy$)).subscribe({
      next: sub => {
        // Pre-filter strands by the existing record's learning area
        if (sub.learningAreaId) {
          this.filteredStrands = this.strands.filter(s => s.learningAreaId === sub.learningAreaId);
        }
        this.form.patchValue({
          name:           sub.name,
          learningAreaId: sub.learningAreaId ?? '',
          strandId:       sub.strandId,
          ...(this.isSuperAdmin ? { tenantId: sub.tenantId } : {}),
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
    const dto = { name: val.name, strandId: val.strandId };
    const obs = this.isEditMode && this.editId
      ? this._service.update(this.editId, dto)
      : this._service.create({ ...dto, tenantId: this.isSuperAdmin ? val.tenantId : undefined });

    obs.pipe(takeUntil(this._destroy$)).subscribe({
      next: () => {
        this._alertService.success(`Sub-strand ${this.isEditMode ? 'updated' : 'created'} successfully`);
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