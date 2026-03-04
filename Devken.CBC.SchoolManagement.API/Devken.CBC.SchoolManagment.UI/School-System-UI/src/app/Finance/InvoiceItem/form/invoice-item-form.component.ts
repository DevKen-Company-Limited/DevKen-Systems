import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnChanges,
  OnDestroy,
  SimpleChanges,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Subject, takeUntil } from 'rxjs';

import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { InvoiceItemService } from 'app/core/DevKenService/Finance/InvoiceItem/invoice-item.service';
import { PanelMode, InvoiceItemResponseDto, CreateInvoiceItemDto, UpdateInvoiceItemDto } from '../Types/invoice-item.types';

@Component({
  selector: 'app-invoice-item-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './invoice-item-form.component.html',
})
export class InvoiceItemFormComponent implements OnChanges, OnDestroy {
  // ── Inputs ────────────────────────────────────────────────────────────────
  @Input({ required: true }) invoiceId!: string;
  @Input({ required: true }) mode!: PanelMode;   // 'create' | 'edit'
  @Input() editItem?: InvoiceItemResponseDto;     // provided when mode === 'edit'

  // ── Outputs ───────────────────────────────────────────────────────────────
  /** Emitted after a successful create or update — parent refreshes the list */
  @Output() saved     = new EventEmitter<InvoiceItemResponseDto>();
  /** Emitted when the user clicks Cancel or the X button */
  @Output() cancelled = new EventEmitter<void>();

  // ── State ─────────────────────────────────────────────────────────────────
  form!: FormGroup;
  isSaving      = false;
  formSubmitted = false;

  readonly itemTypeOptions = [
    'Tuition', 'Transport', 'Meals', 'Accommodation',
    'Activity', 'Uniform', 'Books', 'Exam', 'Medical', 'Other',
  ];

  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private service: InvoiceItemService,
    private alertService: AlertService
  ) {
    this.buildForm();
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnChanges(changes: SimpleChanges): void {
    // Re-patch whenever mode or editItem changes (e.g. user clicks Edit on a row)
    if (changes['mode'] || changes['editItem']) {
      this.formSubmitted = false;

      if (this.mode === 'create') {
        this.form.reset({
          description: '', itemType: '', quantity: 1, unitPrice: 0,
          discount: 0, isTaxable: false, taxRate: null,
          glCode: '', notes: '', discountOverride: null,
        });
      } else if (this.mode === 'edit' && this.editItem) {
        this.form.patchValue({
          description:      this.editItem.description,
          itemType:         this.editItem.itemType ?? '',
          quantity:         this.editItem.quantity,
          unitPrice:        this.editItem.unitPrice,
          discount:         this.editItem.discount,
          isTaxable:        this.editItem.isTaxable,
          taxRate:          this.editItem.taxRate ?? null,
          glCode:           this.editItem.glCode ?? '',
          notes:            this.editItem.notes ?? '',
          discountOverride: null,
        });
      }
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Form builder ──────────────────────────────────────────────────────────

  private buildForm(): void {
    this.form = this.fb.group({
      description:      ['', [Validators.required, Validators.maxLength(200)]],
      itemType:         [''],
      quantity:         [1,  [Validators.required, Validators.min(1)]],
      unitPrice:        [0,  [Validators.required, Validators.min(0)]],
      discount:         [0,  [Validators.min(0)]],
      isTaxable:        [false],
      taxRate:          [null],
      glCode:           ['', Validators.maxLength(100)],
      notes:            ['', Validators.maxLength(500)],
      discountOverride: [null],
    });

    this.form.get('isTaxable')!
      .valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((taxable: boolean) => {
        const ctrl = this.form.get('taxRate')!;
        if (taxable) {
          ctrl.setValidators([Validators.required, Validators.min(0), Validators.max(100)]);
        } else {
          ctrl.clearValidators();
          ctrl.setValue(null);
        }
        ctrl.updateValueAndValidity();
      });
  }

  // ── Save ──────────────────────────────────────────────────────────────────

  save(): void {
    this.formSubmitted = true;

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.value;
    this.isSaving = true;

    if (this.mode === 'create') {
      const payload: CreateInvoiceItemDto = {
        invoiceId:        this.invoiceId,
        description:      raw.description,
        itemType:         raw.itemType  || null,
        quantity:         raw.quantity,
        unitPrice:        raw.unitPrice,
        discount:         raw.discount ?? 0,
        isTaxable:        raw.isTaxable,
        taxRate:          raw.isTaxable ? raw.taxRate : null,
        glCode:           raw.glCode    || null,
        notes:            raw.notes     || null,
        discountOverride: raw.discountOverride ?? null,
      };

      this.service.create(payload)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (res) => {
            this.isSaving = false;
            if (res.success) {
              this.alertService.success('Added', `"${res.data.description}" added to invoice.`);
              this.saved.emit(res.data);
            } else {
              this.alertService.error('Failed', res.message || 'Could not add item.');
            }
          },
          error: (err) => {
            this.isSaving = false;
            this.alertService.error('Error', err?.error?.message || 'Failed to save item.');
          },
        });

    } else if (this.mode === 'edit' && this.editItem) {
      const payload: UpdateInvoiceItemDto = {
        description:      raw.description,
        itemType:         raw.itemType  || null,
        quantity:         raw.quantity,
        unitPrice:        raw.unitPrice,
        discount:         raw.discount ?? 0,
        isTaxable:        raw.isTaxable,
        taxRate:          raw.isTaxable ? raw.taxRate : null,
        glCode:           raw.glCode    || null,
        notes:            raw.notes     || null,
        discountOverride: raw.discountOverride ?? null,
      };

      this.service.update(this.editItem.id, payload, this.invoiceId)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (res) => {
            this.isSaving = false;
            if (res.success) {
              this.alertService.success('Updated', `"${res.data.description}" updated.`);
              this.saved.emit(res.data);
            } else {
              this.alertService.error('Failed', res.message || 'Could not update item.');
            }
          },
          error: (err) => {
            this.isSaving = false;
            this.alertService.error('Error', err?.error?.message || 'Failed to update item.');
          },
        });
    }
  }

  cancel(): void {
    this.cancelled.emit();
  }

  // ── Template helpers ──────────────────────────────────────────────────────

  get isTaxable(): boolean {
    return !!this.form.get('isTaxable')?.value;
  }

  get panelTitle(): string {
    return this.mode === 'create' ? 'Add Line Item' : 'Edit Line Item';
  }

  hasError(field: string): boolean {
    const ctrl = this.form.get(field);
    return !!ctrl && ctrl.invalid && (ctrl.touched || ctrl.dirty || this.formSubmitted);
  }

  getError(field: string): string {
    const ctrl = this.form.get(field);
    if (!ctrl?.errors) return '';
    if (ctrl.errors['required'])  return 'This field is required.';
    if (ctrl.errors['min'])       return `Minimum value is ${ctrl.errors['min'].min}.`;
    if (ctrl.errors['max'])       return `Maximum value is ${ctrl.errors['max'].max}.`;
    if (ctrl.errors['maxlength']) return `Max ${ctrl.errors['maxlength'].requiredLength} characters.`;
    return 'Invalid value.';
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-KE', {
      style: 'currency', currency: 'KES', minimumFractionDigits: 2,
    }).format(amount);
  }

  get previewNet(): number {
    const qty      = this.form.get('quantity')?.value    || 0;
    const price    = this.form.get('unitPrice')?.value   || 0;
    const disc     = this.form.get('discount')?.value    || 0;
    const taxRate  = this.form.get('taxRate')?.value     || 0;
    const discounted = qty * price - disc;
    const tax      = this.isTaxable ? discounted * (taxRate / 100) : 0;
    return discounted + tax;
  }

  get previewTax(): number {
    const qty     = this.form.get('quantity')?.value   || 0;
    const price   = this.form.get('unitPrice')?.value  || 0;
    const disc    = this.form.get('discount')?.value   || 0;
    const taxRate = this.form.get('taxRate')?.value    || 0;
    return this.isTaxable ? (qty * price - disc) * (taxRate / 100) : 0;
  }
}