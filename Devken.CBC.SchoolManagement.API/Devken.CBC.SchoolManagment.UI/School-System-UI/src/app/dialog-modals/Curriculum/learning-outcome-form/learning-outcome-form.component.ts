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
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { FuseAlertComponent } from '@fuse/components/alert';

import { AuthService } from 'app/core/auth/auth.service';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { SchoolDto } from 'app/Tenant/types/school';
import { LearningAreaService } from 'app/core/DevKenService/curriculum/learning-area.service';
import { LearningOutcomeService } from 'app/core/DevKenService/curriculum/learning-outcome.service';
import { StrandService } from 'app/core/DevKenService/curriculum/strand.service';
import { SubStrandService } from 'app/core/DevKenService/curriculum/substrand.service ';
import { LearningAreaResponseDto } from 'app/curriculum/types/learning-area.dto ';
import { StrandResponseDto } from 'app/curriculum/types/strand.dto ';
import { SubStrandResponseDto } from 'app/curriculum/types/substrand.dto ';
import { CBCLevelOptions } from 'app/curriculum/types/curriculum-enums';

export interface LearningOutcomeDialogData {
  editId?: string;
  defaultSubStrandId?: string;
  defaultStrandId?: string;
  defaultLearningAreaId?: string;
}

@Component({
  selector: 'app-learning-outcome-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatSlideToggleModule,
    MatDialogModule, FuseAlertComponent,
  ],
  templateUrl: './learning-outcome-form.component.html',
})
export class LearningOutcomeFormComponent implements OnInit, OnDestroy {
  private _destroy$ = new Subject<void>();
  private fb = inject(FormBuilder);
  private _service = inject(LearningOutcomeService);
  private _ssService = inject(SubStrandService);
  private _strandService = inject(StrandService);
  private _laService = inject(LearningAreaService);
  private _authService = inject(AuthService);
  private _alertService = inject(AlertService);
  private _schoolService = inject(SchoolService);
  private _dialogRef = inject(MatDialogRef<LearningOutcomeFormComponent>);

  form!: FormGroup;
  isLoading = false;
  isSaving = false;
  schools: SchoolDto[] = [];
  learningAreas: LearningAreaResponseDto[] = [];
  allStrands: StrandResponseDto[] = [];
  allSubStrands: SubStrandResponseDto[] = [];
  filteredStrands: StrandResponseDto[] = [];
  filteredSubStrands: SubStrandResponseDto[] = [];

  cbcLevels = CBCLevelOptions;

  get editId(): string | undefined { return this.data?.editId; }
  get isEditMode(): boolean { return !!this.editId; }
  get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }

  constructor(@Inject(MAT_DIALOG_DATA) public data: LearningOutcomeDialogData) {
    // Remove MatDialog's default padding so our custom header/footer sit flush
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

  /**
   * Resolve level to numeric value from either numeric string or label string.
   */
  private resolveLevelValue(level: string | number): number | string {
    if (!level) return '';
    const asNum = Number(level);
    if (!isNaN(asNum) && asNum > 0) return asNum;
    const opt = this.cbcLevels.find(l => l.label === level || l.label.toLowerCase() === String(level).toLowerCase());
    return opt ? opt.value : '';
  }

  private buildForm(): void {
    this.form = this.fb.group({
      learningAreaId: [this.data?.defaultLearningAreaId ?? '', Validators.required],
      strandId:       [this.data?.defaultStrandId ?? '',       Validators.required],
      subStrandId:    [this.data?.defaultSubStrandId ?? '',    Validators.required],
      outcome:        ['', [Validators.required, Validators.maxLength(250)]],
      code:           ['', Validators.maxLength(50)],
      description:    ['', Validators.maxLength(1000)],
      level:          ['', Validators.required],
      isCore:         [true],
      ...(this.isSuperAdmin ? { tenantId: ['', Validators.required] } : {}),
    });

    // Cascading: LA -> Strand
    this.form.get('learningAreaId')?.valueChanges.subscribe(laId => {
      this.filteredStrands = laId ? this.allStrands.filter(s => s.learningAreaId === laId) : this.allStrands;
      this.form.get('strandId')?.setValue('');
      this.filteredSubStrands = [];
      this.form.get('subStrandId')?.setValue('');
    });

    // Cascading: Strand -> SubStrand
    this.form.get('strandId')?.valueChanges.subscribe(strandId => {
      this.filteredSubStrands = strandId ? this.allSubStrands.filter(ss => ss.strandId === strandId) : this.allSubStrands;
      this.form.get('subStrandId')?.setValue('');
    });
  }

  private loadLookups(): void {
    this._laService.getAll().pipe(takeUntil(this._destroy$))
      .subscribe(d => { this.learningAreas = Array.isArray(d) ? d : []; });
    this._strandService.getAll().pipe(takeUntil(this._destroy$))
      .subscribe(d => {
        this.allStrands = Array.isArray(d) ? d : [];
        // Apply initial filter if default LA provided
        const laId = this.form.get('learningAreaId')?.value;
        this.filteredStrands = laId ? this.allStrands.filter(s => s.learningAreaId === laId) : this.allStrands;
      });
    this._ssService.getAll().pipe(takeUntil(this._destroy$))
      .subscribe(d => {
        this.allSubStrands = Array.isArray(d) ? d : [];
        const strandId = this.form.get('strandId')?.value;
        this.filteredSubStrands = strandId ? this.allSubStrands.filter(ss => ss.strandId === strandId) : this.allSubStrands;
      });
  }

  private loadExisting(id: string): void {
    this.isLoading = true;
    this._service.getById(id).pipe(takeUntil(this._destroy$)).subscribe({
      next: o => {
        // Hydrate cascading lists first
        const strand = this.allStrands.find(s => s.id === o.strandId);
        const laId   = strand?.learningAreaId ?? o.learningAreaId;
        this.filteredStrands    = laId ? this.allStrands.filter(s => s.learningAreaId === laId) : this.allStrands;
        this.filteredSubStrands = o.strandId ? this.allSubStrands.filter(ss => ss.strandId === o.strandId) : this.allSubStrands;

        const levelValue = this.resolveLevelValue(o.level);

        this.form.patchValue({
          learningAreaId: laId,
          strandId:       o.strandId,
          subStrandId:    o.subStrandId,
          outcome:        o.outcome,
          code:           o.code ?? '',
          description:    o.description ?? '',
          level:          levelValue,
          isCore:         o.isCore,
          ...(this.isSuperAdmin ? { tenantId: o.tenantId } : {}),
        }, { emitEvent: false });

        // Manually reset cascade after patchValue with emitEvent:false
        this.filteredStrands    = laId ? this.allStrands.filter(s => s.learningAreaId === laId) : this.allStrands;
        this.filteredSubStrands = o.strandId ? this.allSubStrands.filter(ss => ss.strandId === o.strandId) : this.allSubStrands;

        this.isLoading = false;
      },
      error: err => { this._alertService.error(err?.error?.message || 'Failed to load'); this.isLoading = false; },
    });
  }

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.isSaving = true;
    const val = this.form.value;
    const dto = {
      outcome:        val.outcome,
      code:           val.code || undefined,
      description:    val.description || undefined,
      level:          Number(val.level),
      isCore:         val.isCore,
      learningAreaId: val.learningAreaId,
      strandId:       val.strandId,
      subStrandId:    val.subStrandId,
    };

    const obs = this.isEditMode && this.editId
      ? this._service.update(this.editId, dto as any)
      : this._service.create({ ...dto, tenantId: this.isSuperAdmin ? val.tenantId : undefined } as any);

    obs.pipe(takeUntil(this._destroy$)).subscribe({
      next: () => {
        this._alertService.success(`Learning outcome ${this.isEditMode ? 'updated' : 'created'} successfully`);
        this._dialogRef.close({ success: true });
      },
      error: err => { this._alertService.error(err?.error?.message || 'Save failed'); this.isSaving = false; },
    });
  }

  cancel(): void { this._dialogRef.close(); }

  getError(f: string): string {
    const c = this.form.get(f);
    if (!c?.errors) return '';
    if (c.errors['required']) return 'Required';
    if (c.errors['maxlength']) return 'Too long';
    return 'Invalid';
  }
}