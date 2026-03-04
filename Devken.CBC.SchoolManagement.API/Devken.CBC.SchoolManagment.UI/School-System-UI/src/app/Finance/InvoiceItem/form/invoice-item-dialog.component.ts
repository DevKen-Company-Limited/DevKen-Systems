import {
  Component,
  Inject,
  OnInit,
  OnDestroy,
  inject,
  ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule }       from '@angular/material/form-field';
import { MatInputModule }           from '@angular/material/input';
import { MatSelectModule }          from '@angular/material/select';
import { MatButtonModule }          from '@angular/material/button';
import { MatIconModule }            from '@angular/material/icon';
import { MatSlideToggleModule }     from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule }         from '@angular/material/divider';
import { MatTooltipModule }         from '@angular/material/tooltip';
import { Subject } from 'rxjs';
import { takeUntil, finalize } from 'rxjs/operators';

import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { InvoiceItemService } from 'app/core/DevKenService/Finance/InvoiceItem/invoice-item.service';
import {
  InvoiceItemDialogData,
  InvoiceItemResponseDto,
  CreateInvoiceItemDto,
  UpdateInvoiceItemDto,
  ITEM_TYPE_OPTIONS,
} from '../Types/invoice-item.types';

@Component({
  selector: 'app-invoice-item-dialog',
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
  templateUrl: './invoice-item-dialog.component.html',
  styles: [`
    :host ::ng-deep .mat-mdc-dialog-container {
      --mdc-dialog-container-shape: 12px;
    }
  `],
})
export class InvoiceItemDialogComponent implements OnInit, OnDestroy {
  private readonly _unsubscribe = new Subject<void>();
  private readonly _alert       = inject(AlertService);

  // ── State ──────────────────────────────────────────────────────────────────
  form!: FormGroup;
  formSubmitted = false;
  isSaving      = false;

  readonly itemTypeOptions = ITEM_TYPE_OPTIONS;

  // ── Getters ────────────────────────────────────────────────────────────────
  get isEditMode(): boolean  { return this.data.mode === 'edit'; }
  get dialogTitle(): string  { return this.isEditMode ? 'Edit Line Item' : 'Add Line Item'; }
  get dialogSubtitle(): string {
    return this.isEditMode
      ? `Editing: ${this.data.item?.description ?? ''}`
      : 'Fill in the details below';
  }
  get showTaxRate(): boolean { return !!this.form?.get('isTaxable')?.value; }

  get previewNet(): number {
    const qty   = +this.form.get('quantity')?.value  || 0;
    const price = +this.form.get('unitPrice')?.value || 0;
    const disc  = +this.form.get('discount')?.value  || 0;
    const rate  = +this.form.get('taxRate')?.value   || 0;
    const base  = qty * price - disc;
    return base + (this.showTaxRate ? base * (rate / 100) : 0);
  }

  get previewTax(): number {
    const qty   = +this.form.get('quantity')?.value  || 0;
    const price = +this.form.get('unitPrice')?.value || 0;
    const disc  = +this.form.get('discount')?.value  || 0;
    const rate  = +this.form.get('taxRate')?.value   || 0;
    return this.showTaxRate ? (qty * price - disc) * (rate / 100) : 0;
  }

  constructor(
    private readonly _fb:        FormBuilder,
    private readonly _service:   InvoiceItemService,
    private readonly _dialogRef: MatDialogRef<InvoiceItemDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: InvoiceItemDialogData,
    private readonly _cdr:       ChangeDetectorRef,
  ) {
    _dialogRef.addPanelClass('no-padding-dialog');
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
      description:      ['', [Validators.required, Validators.maxLength(200)]],
      itemType:         [''],
      quantity:         [1,    [Validators.required, Validators.min(1)]],
      unitPrice:        [0,    [Validators.required, Validators.min(0)]],
      discount:         [0,    [Validators.min(0)]],
      isTaxable:        [false],
      taxRate:          [null],
      glCode:           ['',   [Validators.maxLength(100)]],
      notes:            ['',   [Validators.maxLength(500)]],
      discountOverride: [null],
    });

    // Manage taxRate validators reactively
    this.form.get('isTaxable')!.valueChanges
      .pipe(takeUntil(this._unsubscribe))
      .subscribe((taxable: boolean) => {
        const ctrl = this.form.get('taxRate')!;
        if (taxable) {
          ctrl.setValidators([Validators.required, Validators.min(0), Validators.max(100)]);
        } else {
          ctrl.clearValidators();
          ctrl.setValue(null, { emitEvent: false });
        }
        ctrl.updateValueAndValidity();
        this._cdr.detectChanges();
      });
  }

  private _patchForm(item: InvoiceItemResponseDto): void {
    this.form.patchValue({
      description:      item.description,
      itemType:         item.itemType    ?? '',
      quantity:         item.quantity,
      unitPrice:        item.unitPrice,
      discount:         item.discount,
      isTaxable:        item.isTaxable,
      taxRate:          item.taxRate     ?? null,
      glCode:           item.glCode      ?? '',
      notes:            item.notes       ?? '',
      discountOverride: null,
    });
    this._cdr.detectChanges();
  }

  // ── Submit ─────────────────────────────────────────────────────────────────

  onSubmit(): void {
    this.formSubmitted = true;
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    const raw = this.form.getRawValue();
    this.isSaving = true;

    if (this.isEditMode) {
      const payload: UpdateInvoiceItemDto = {
        description:      raw.description,
        itemType:         raw.itemType         || null,
        quantity:         +raw.quantity,
        unitPrice:        +raw.unitPrice,
        discount:         +raw.discount        || 0,
        isTaxable:        raw.isTaxable,
        taxRate:          raw.isTaxable ? +raw.taxRate : null,
        glCode:           raw.glCode           || null,
        notes:            raw.notes            || null,
        discountOverride: raw.discountOverride ?? null,
      };

      this._service
        .update(this.data.invoiceId, this.data.item!.id, payload)
        .pipe(takeUntil(this._unsubscribe), finalize(() => { this.isSaving = false; this._cdr.detectChanges(); }))
        .subscribe({
          next: res => {
            if (res.success) this._dialogRef.close({ success: true, data: res.data });
            else this._alert.error('Failed', res.message || 'Could not update item.');
          },
          error: err => this._alert.error('Error', err?.error?.message || 'Failed to update item.'),
        });

    } else {
      const payload: CreateInvoiceItemDto = {
        invoiceId:        this.data.invoiceId,
        description:      raw.description,
        itemType:         raw.itemType         || null,
        quantity:         +raw.quantity,
        unitPrice:        +raw.unitPrice,
        discount:         +raw.discount        || 0,
        isTaxable:        raw.isTaxable,
        taxRate:          raw.isTaxable ? +raw.taxRate : null,
        glCode:           raw.glCode           || null,
        notes:            raw.notes            || null,
        discountOverride: raw.discountOverride ?? null,
      };

      this._service
        .create(payload)
        .pipe(takeUntil(this._unsubscribe), finalize(() => { this.isSaving = false; this._cdr.detectChanges(); }))
        .subscribe({
          next: res => {
            if (res.success) this._dialogRef.close({ success: true, data: res.data });
            else this._alert.error('Failed', res.message || 'Could not add item.');
          },
          error: err => this._alert.error('Error', err?.error?.message || 'Failed to add item.'),
        });
    }
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

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-KE', {
      style: 'currency', currency: 'KES', minimumFractionDigits: 2,
    }).format(amount ?? 0);
  }
}