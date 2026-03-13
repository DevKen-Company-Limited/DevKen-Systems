import {
  Component, Input, Output, EventEmitter,
  OnInit, OnChanges, OnDestroy, SimpleChanges, inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder, FormGroup, FormArray,
  Validators, ReactiveFormsModule, AbstractControl
} from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { FuseAlertComponent } from '@fuse/components/alert';
import { FeeItemService } from 'app/core/DevKenService/Finance/fee-item.service';
import {
  FeeItemResponseDto,
  FEE_TYPE_OPTIONS,
  resolveFeeTypeLabel,
} from 'app/Finance/fee-item/Types/fee-item.model';
import { catchError, takeUntil } from 'rxjs/operators';
import { of, Subject } from 'rxjs';

@Component({
  selector: 'app-invoice-line-items',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatIconModule,
    MatButtonModule,
    MatCardModule,
    MatTooltipModule,
    MatDividerModule,
    FuseAlertComponent,
  ],
  templateUrl: './invoice-line-items.component.html',
})
export class InvoiceLineItemsComponent implements OnInit, OnChanges, OnDestroy {
  @Input() formData: any[]    = [];
  @Input() isEditMode         = false;
  @Input() schoolId?: string;

  @Output() formChanged = new EventEmitter<any[]>();
  @Output() formValid   = new EventEmitter<boolean>();

  private fb         = inject(FormBuilder);
  private feeItemSvc = inject(FeeItemService);
  private destroy$   = new Subject<void>();

  form!: FormGroup;
  feeItems: FeeItemResponseDto[] = [];
  feeTypeOptions = FEE_TYPE_OPTIONS;

  private _lastEmitted = '';

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.buildForm();
    this.setupListeners();
    this.loadFeeItems();
    this.emitValid();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['schoolId'] && !changes['schoolId'].firstChange) {
      this.loadFeeItems();
    }
    if (changes['formData'] && this.form) {
      const incoming = JSON.stringify(this.formData || []);
      if (incoming !== this._lastEmitted) {
        this.setItems(this.formData || []);
      }
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Fee Catalog ───────────────────────────────────────────────────────────
  private loadFeeItems(): void {
    this.feeItemSvc
      .getAll(this.schoolId ? ({ schoolId: this.schoolId } as any) : undefined)
      .pipe(
        catchError(() => of({ success: false, data: [] })),
        takeUntil(this.destroy$),
      )
      .subscribe((res: any) => {
        this.feeItems = (res?.data ?? []).filter((f: FeeItemResponseDto) => f.isActive);
      });
  }

  onFeeItemSelected(index: number, feeItemId: string): void {
    if (!feeItemId) return;

    const fee = this.feeItems.find(f => f.id === feeItemId);
    if (!fee) return;

    const group = this.itemsArray.at(index) as FormGroup;
    group.patchValue({
      feeItemId:   fee.id,
      description: fee.name,
      itemType:    fee.feeType != null ? resolveFeeTypeLabel(fee.feeType) : '',
      unitPrice:   fee.defaultAmount,
      isTaxable:   fee.isTaxable,
      taxRate:     fee.taxRate ?? 0,
      glCode:      fee.glCode  ?? '',
    }, { emitEvent: true });

    const taxRateCtrl = group.get('taxRate');
    if (fee.isTaxable) {
      taxRateCtrl?.enable();
    } else {
      taxRateCtrl?.disable();
      taxRateCtrl?.setValue(0);
    }
  }

  clearFeeItem(index: number): void {
    const group = this.itemsArray.at(index) as FormGroup;
    group.patchValue({ feeItemId: '' }, { emitEvent: false });
  }

  getFeeItemName(feeItemId: string): string {
    return this.feeItems.find(f => f.id === feeItemId)?.name ?? '';
  }

  // ── Form ──────────────────────────────────────────────────────────────────
  private buildForm(): void {
    this.form = this.fb.group({
      items: this.fb.array([], Validators.required),
    });
    if (this.formData?.length > 0) {
      this.setItems(this.formData);
    } else {
      this.addItem();
    }
  }

  private setupListeners(): void {
    this.form.valueChanges.subscribe(() => {
      const raw = this.itemsArray.getRawValue();
      this._lastEmitted = JSON.stringify(raw);
      this.formChanged.emit(raw);
      this.emitValid();
    });
  }

  private emitValid(): void {
    const valid =
      this.itemsArray.length > 0 &&
      this.itemsArray.controls.every(c => c.valid);
    this.formValid.emit(valid);
  }

  get itemsArray(): FormArray {
    return this.form.get('items') as FormArray;
  }

  private setItems(items: any[]): void {
    this.itemsArray.clear();
    items.forEach(item => this.itemsArray.push(this.createItemGroup(item)));
  }

  private createItemGroup(item?: any): FormGroup {
    return this.fb.group({
      feeItemId:   [item?.feeItemId   || ''],
      description: [item?.description || '', [Validators.required, Validators.maxLength(200)]],
      itemType:    [item?.itemType    || ''],
      quantity:    [item?.quantity    || 1,  [Validators.required, Validators.min(1)]],
      unitPrice:   [item?.unitPrice   || 0,  [Validators.required, Validators.min(0)]],
      discount:    [item?.discount    || 0,  [Validators.min(0)]],
      isTaxable:   [item?.isTaxable   || false],
      taxRate:     [
        { value: item?.taxRate || 0, disabled: !(item?.isTaxable) },
        [Validators.min(0), Validators.max(100)],
      ],
      glCode:      [item?.glCode || ''],
      notes:       [item?.notes  || ''],
    });
  }

  addItem(): void {
    this.itemsArray.push(this.createItemGroup());
    this.emitValid();
  }

  removeItem(index: number): void {
    if (this.itemsArray.length > 1) {
      this.itemsArray.removeAt(index);
      this.emitValid();
    }
  }

  onTaxableToggle(index: number): void {
    const group   = this.itemsArray.at(index) as FormGroup;
    const taxCtrl = group.get('taxRate');
    if (group.get('isTaxable')?.value) {
      taxCtrl?.enable();
    } else {
      taxCtrl?.disable();
      taxCtrl?.setValue(0);
    }
  }

  // ── Totals ────────────────────────────────────────────────────────────────
  getItemTotal(ctrl: AbstractControl): number {
    const qty      = ctrl.get('quantity')?.value  || 0;
    const price    = ctrl.get('unitPrice')?.value || 0;
    const disc     = ctrl.get('discount')?.value  || 0;
    const subtotal = qty * price - disc;
    if (ctrl.get('isTaxable')?.value) {
      const rate = ctrl.get('taxRate')?.value || 0;
      return subtotal + (subtotal * rate / 100);
    }
    return subtotal;
  }

  get grandTotal(): number {
    return this.itemsArray.controls.reduce(
      (sum, ctrl) => sum + this.getItemTotal(ctrl), 0,
    );
  }

  formatCurrency(val: number): string {
    return new Intl.NumberFormat('en-KE', {
      style: 'currency', currency: 'KES', maximumFractionDigits: 0,
    }).format(val);
  }
}