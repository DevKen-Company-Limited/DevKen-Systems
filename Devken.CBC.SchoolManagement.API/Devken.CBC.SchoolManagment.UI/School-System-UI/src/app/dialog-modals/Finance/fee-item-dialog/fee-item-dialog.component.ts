import {
  Component, Inject, OnInit, OnDestroy, inject, ChangeDetectorRef
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder, FormGroup, ReactiveFormsModule, Validators
} from '@angular/forms';
import {
  MatDialogRef, MAT_DIALOG_DATA, MatDialogModule
} from '@angular/material/dialog';
import { MatFormFieldModule }      from '@angular/material/form-field';
import { MatInputModule }          from '@angular/material/input';
import { MatSelectModule }         from '@angular/material/select';
import { MatButtonModule }         from '@angular/material/button';
import { MatIconModule }           from '@angular/material/icon';
import { MatSlideToggleModule }    from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule }from '@angular/material/progress-spinner';
import { MatDividerModule }        from '@angular/material/divider';
import { MatTooltipModule }        from '@angular/material/tooltip';
import { Subject }                 from 'rxjs';
import { takeUntil, finalize }     from 'rxjs/operators';

import { AuthService }             from 'app/core/auth/auth.service';
import { FeeItemService } from 'app/core/DevKenService/Finance/fee-item.service';
import { FeeItemResponseDto, FEE_TYPE_OPTIONS, RECURRENCE_OPTIONS, CBC_LEVEL_OPTIONS, APPLICABLE_TO_OPTIONS, CreateFeeItemDto, UpdateFeeItemDto } from 'app/Finance/fee-item/Types/fee-item.model';

export interface FeeItemDialogData {
  mode: 'create' | 'edit';
  item?: FeeItemResponseDto;
}

@Component({
  selector: 'app-fee-item-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatSlideToggleModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    MatTooltipModule,
  ],
  templateUrl: './fee-item-dialog.component.html',
  styles: [`
    :host ::ng-deep .mat-mdc-dialog-container {
      --mdc-dialog-container-shape: 12px;
    }
  `]
})
export class FeeItemDialogComponent implements OnInit, OnDestroy {
  private readonly _unsubscribe = new Subject<void>();
  private readonly _authService = inject(AuthService);

  // ── Form State ─────────────────────────────────────────────────────────────
  form!: FormGroup;
  formSubmitted = false;
  isLoading = false;
  isSaving  = false;

  // ── Options ────────────────────────────────────────────────────────────────
  feeTypeOptions      = FEE_TYPE_OPTIONS;
  recurrenceOptions   = RECURRENCE_OPTIONS;
  levelOptions        = CBC_LEVEL_OPTIONS;
  applicableToOptions = APPLICABLE_TO_OPTIONS;

  // ── Getters ────────────────────────────────────────────────────────────────
  get isEditMode(): boolean { return this.data.mode === 'edit'; }
  get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }
  get dialogTitle(): string { return this.isEditMode ? 'Edit Fee Item' : 'Add New Fee Item'; }
  get dialogSubtitle(): string {
    return this.isEditMode
      ? `Updating: ${this.data.item?.displayName ?? this.data.item?.name}`
      : 'Fill in the details to create a new fee item';
  }
  get descriptionLength(): number { return this.form?.get('description')?.value?.length ?? 0; }
  get showTaxRate(): boolean       { return !!this.form?.get('isTaxable')?.value; }
  get showRecurrence(): boolean    { return !!this.form?.get('isRecurring')?.value; }

  constructor(
    private readonly _fb: FormBuilder,
    private readonly _service: FeeItemService,
    private readonly _dialogRef: MatDialogRef<FeeItemDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: FeeItemDialogData,
    private readonly _cdr: ChangeDetectorRef,
  ) {
    _dialogRef.addPanelClass(['fee-item-dialog', 'responsive-dialog']);
  }

  ngOnInit(): void {
    this._buildForm();
    if (this.isEditMode && this.data.item) {
      this._patchForm(this.data.item);
    }
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  // ── Form Setup ─────────────────────────────────────────────────────────────
  private _buildForm(): void {
    this.form = this._fb.group({
      name:            ['', [Validators.required, Validators.maxLength(100)]],
      description:     ['', [Validators.maxLength(500)]],
      defaultAmount:   [0,  [Validators.required, Validators.min(0)]],
      feeType:         ['', [Validators.required]],
      isMandatory:     [true],
      isRecurring:     [false],
      recurrence:      [0],
      isTaxable:       [false],
      taxRate:         [null, [Validators.min(0), Validators.max(100)]],
      glCode:          ['', [Validators.maxLength(100)]],
      isActive:        [true],
      applicableLevel: [null],
      applicableTo:    [0],
    });

    // Clear tax rate when isTaxable is toggled off
    this.form.get('isTaxable')?.valueChanges
      .pipe(takeUntil(this._unsubscribe))
      .subscribe(val => { if (!val) this.form.patchValue({ taxRate: null }, { emitEvent: false }); });

    // Clear recurrence when isRecurring is toggled off
    this.form.get('isRecurring')?.valueChanges
      .pipe(takeUntil(this._unsubscribe))
      .subscribe(val => { if (!val) this.form.patchValue({ recurrence: 0 }, { emitEvent: false }); });
  }

  private _patchForm(item: FeeItemResponseDto): void {
    this.form.patchValue({
      name:            item.name            ?? '',
      description:     item.description     ?? '',
      defaultAmount:   item.defaultAmount   ?? 0,
      feeType:         item.feeType != null ? parseInt(item.feeType) : '',
      isMandatory:     item.isMandatory     ?? true,
      isRecurring:     item.isRecurring     ?? false,
      recurrence:      item.recurrence != null ? parseInt(item.recurrence) : 0,
      isTaxable:       item.isTaxable       ?? false,
      taxRate:         item.taxRate         ?? null,
      glCode:          item.glCode          ?? '',
      isActive:        item.isActive        ?? true,
      applicableLevel: item.applicableLevel != null ? parseInt(item.applicableLevel) : null,
      applicableTo:    item.applicableTo    != null ? parseInt(item.applicableTo)    : 0,
    });
    this._cdr.detectChanges();
  }

  // ── Submit & Cancel ────────────────────────────────────────────────────────
  onSubmit(): void {
    this.formSubmitted = true;
    if (this.form.invalid) return;

    const raw = this.form.getRawValue();
    const payload: CreateFeeItemDto | UpdateFeeItemDto = {
      name:            raw.name?.trim(),
      description:     raw.description?.trim() || undefined,
      defaultAmount:   +raw.defaultAmount,
      feeType:         +raw.feeType,
      isMandatory:     raw.isMandatory,
      isRecurring:     raw.isRecurring,
      recurrence:      raw.isRecurring ? +raw.recurrence : undefined,
      isTaxable:       raw.isTaxable,
      taxRate:         raw.isTaxable ? +raw.taxRate : undefined,
      glCode:          raw.glCode?.trim() || undefined,
      isActive:        raw.isActive,
      applicableLevel: raw.applicableLevel != null ? +raw.applicableLevel : undefined,
      applicableTo:    raw.applicableTo    != null ? +raw.applicableTo    : undefined,
    };

    this.isSaving = true;
    const obs$ = this.isEditMode
      ? this._service.update(this.data.item!.id, payload as UpdateFeeItemDto)
      : this._service.create(payload as CreateFeeItemDto);

    obs$.pipe(takeUntil(this._unsubscribe), finalize(() => { this.isSaving = false; this._cdr.detectChanges(); }))
      .subscribe({
        next: res => {
          if (res.success) {
            this._dialogRef.close({ success: true, data: res.data });
          }
          // Let the parent handle error toasts — or add one here if preferred
        },
        error: () => { /* parent handles */ }
      });
  }

  onCancel(): void {
    this._dialogRef.close(null);
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  getFieldError(field: string): string {
    const c = this.form.get(field);
    if (!c || !(this.formSubmitted || c.touched)) return '';
    if (c.hasError('required'))  return 'This field is required';
    if (c.hasError('min'))       return `Value must be at least ${c.getError('min').min}`;
    if (c.hasError('max'))       return `Value must be at most ${c.getError('max').max}`;
    if (c.hasError('maxlength')) return `Maximum ${c.getError('maxlength').requiredLength} characters`;
    return 'Invalid value';
  }
}