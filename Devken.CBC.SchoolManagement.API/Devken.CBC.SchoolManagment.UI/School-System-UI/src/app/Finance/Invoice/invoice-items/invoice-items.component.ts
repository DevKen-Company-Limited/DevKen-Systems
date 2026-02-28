import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule, AbstractControl } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { FuseAlertComponent } from '@fuse/components/alert';
import { MatTooltipModule } from '@angular/material/tooltip';

@Component({
  selector: 'app-invoice-items',
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
    FuseAlertComponent,
    MatTooltipModule,
  ],
  templateUrl: './invoice-items.component.html',
})
export class InvoiceItemsComponent implements OnInit, OnChanges {
  @Input() formData: any[] = [];
  @Input() isEditMode = false;
  @Output() formChanged = new EventEmitter<any[]>();
  @Output() formValid   = new EventEmitter<boolean>();

  private fb = inject(FormBuilder);
  form!: FormGroup;

  ngOnInit(): void {
    this.buildForm();
    this.setupListeners();
    this.emitValid();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['formData'] && this.form) {
      this.setItems(this.formData || []);
    }
  }

  private buildForm(): void {
    this.form = this.fb.group({
      items: this.fb.array([], Validators.required),
    });
    if (this.formData && this.formData.length > 0) {
      this.setItems(this.formData);
    } else {
      this.addItem(); // start with one empty item
    }
  }

  private setupListeners(): void {
    this.form.valueChanges.subscribe(() => {
      this.formChanged.emit(this.itemsArray.getRawValue());
      this.emitValid();
    });
  }

  private emitValid(): void {
    const valid = this.itemsArray.length > 0 && this.itemsArray.controls.every(c => c.valid);
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
      description: [item?.description || '', [Validators.required, Validators.maxLength(200)]],
      itemType:    [item?.itemType || ''],
      quantity:    [item?.quantity || 1, [Validators.required, Validators.min(1)]],
      unitPrice:   [item?.unitPrice || 0, [Validators.required, Validators.min(0)]],
      discount:    [item?.discount || 0, [Validators.min(0)]],
      isTaxable:   [item?.isTaxable || false],
      taxRate:     [{ value: item?.taxRate || 0, disabled: !(item?.isTaxable) }, [Validators.min(0), Validators.max(100)]],
      glCode:      [item?.glCode || ''],
      notes:       [item?.notes || ''],
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
    const control = this.itemsArray.at(index).get('taxRate');
    const isTaxable = this.itemsArray.at(index).get('isTaxable')?.value;
    if (isTaxable) {
      control?.enable();
    } else {
      control?.disable();
      control?.setValue(0);
    }
  }

  getItemTotal(ctrl: AbstractControl): number {
    const qty = ctrl.get('quantity')?.value || 0;
    const price = ctrl.get('unitPrice')?.value || 0;
    const disc = ctrl.get('discount')?.value || 0;
    const subtotal = qty * price - disc;
    if (ctrl.get('isTaxable')?.value) {
      const taxRate = ctrl.get('taxRate')?.value || 0;
      return subtotal + (subtotal * taxRate / 100);
    }
    return subtotal;
  }

  get grandTotal(): number {
    return this.itemsArray.controls.reduce((sum, ctrl) => sum + this.getItemTotal(ctrl), 0);
  }

  formatCurrency(val: number): string {
    return new Intl.NumberFormat('en-KE', { style: 'currency', currency: 'KES', maximumFractionDigits: 0 }).format(val);
  }
}