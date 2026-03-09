import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule, AbstractControl } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { take } from 'rxjs/operators';
import { InvoiceDialogData, CreateInvoiceDto, UpdateInvoiceDto } from 'app/Finance/Invoice/Types/Invoice.types';
import { InvoiceService } from 'app/core/DevKenService/Invoice/Invoice.service ';
import { FormDialogComponent, DialogHeader, DialogTab, DialogFooter } from 'app/shared/dialogs/form/form-dialog.component';


@Component({
  selector: 'app-invoice-form-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatIconModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatCheckboxModule,
    FormDialogComponent,
  ],
  templateUrl: './invoice-form-dialog.component.html',
})
export class InvoiceFormDialogComponent implements OnInit {
  form!: FormGroup;
  isSaving = false;
  formSubmitted = false;
  activeTab = 0;

  dialogHeader: DialogHeader = {
    title: this.data.mode === 'create' ? 'Create Invoice' : 'Edit Invoice',
    subtitle: this.data.mode === 'edit' ? `Editing: ${this.data.invoice?.invoiceNumber}` : 'Fill in invoice details',
    icon: 'receipt_long',
    gradient: 'bg-gradient-to-r from-blue-600 via-indigo-600 to-violet-700',
  };

  tabs: DialogTab[] = [
    { id: 'details', label: 'Invoice Details', icon: 'info',           fields: ['studentId', 'academicYearId', 'invoiceDate', 'dueDate'] },
    { id: 'items',   label: 'Line Items',       icon: 'list_alt',       fields: [] },
    { id: 'notes',   label: 'Notes',            icon: 'sticky_note_2',  fields: [] },
  ];

  get footerConfig(): DialogFooter {
    return {
      cancelText: 'Cancel',
      submitText: this.data.mode === 'create' ? 'Create Invoice' : 'Save Changes',
      submitIcon: 'receipt',
      loading: this.isSaving,
      loadingText: 'Saving...',
      showError: true,
    };
  }

  get itemsArray(): FormArray {
    return this.form.get('items') as FormArray;
  }

  constructor(
    private fb: FormBuilder,
    private invoiceService: InvoiceService,
    private snackBar: MatSnackBar,
    private dialogRef: MatDialogRef<InvoiceFormDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: InvoiceDialogData,
  ) {}

  ngOnInit(): void {
    this.form = this.buildForm();
    if (this.data.mode === 'edit' && this.data.invoice) {
      this.patchForm(this.data.invoice);
    } else {
      // Add one empty item by default
      this.addItem();
    }
  }

  private buildForm(): FormGroup {
    return this.fb.group({
      studentId:      ['', Validators.required],
      academicYearId: ['', Validators.required],
      termId:         [''],
      parentId:       [''],
      invoiceDate:    [new Date(), Validators.required],
      dueDate:        ['', Validators.required],
      description:    ['', Validators.maxLength(500)],
      notes:          ['', Validators.maxLength(1000)],
      items:          this.fb.array([], Validators.required),
    });
  }

  private buildItemGroup(item?: any): FormGroup {
    return this.fb.group({
      description: [item?.description || '', [Validators.required, Validators.maxLength(200)]],
      itemType:    [item?.itemType || ''],
      quantity:    [item?.quantity || 1, [Validators.required, Validators.min(1)]],
      unitPrice:   [item?.unitPrice || 0, [Validators.required, Validators.min(0)]],
      discount:    [item?.discount || 0, Validators.min(0)],
      isTaxable:   [item?.isTaxable || false],
      taxRate:     [item?.taxRate || null, [Validators.min(0), Validators.max(100)]],
      glCode:      [item?.glCode || ''],
      notes:       [item?.notes || ''],
    });
  }

  private patchForm(invoice: any): void {
    this.form.patchValue({
      studentId:      invoice.studentId,
      academicYearId: invoice.academicYearId,
      termId:         invoice.termId || '',
      parentId:       invoice.parentId || '',
      invoiceDate:    new Date(invoice.invoiceDate),
      dueDate:        new Date(invoice.dueDate),
      description:    invoice.description || '',
      notes:          invoice.notes || '',
    });
  }

  addItem(): void {
    this.itemsArray.push(this.buildItemGroup());
  }

  removeItem(index: number): void {
    if (this.itemsArray.length > 1) {
      this.itemsArray.removeAt(index);
    }
  }

  getItemTotal(control: AbstractControl): number {
    const qty    = control.get('quantity')?.value || 0;
    const price  = control.get('unitPrice')?.value || 0;
    const disc   = control.get('discount')?.value || 0;
    const tax    = control.get('isTaxable')?.value ? (control.get('taxRate')?.value || 0) : 0;
    const sub    = qty * price - disc;
    return sub + (sub * tax / 100);
  }

  get grandTotal(): number {
    return this.itemsArray.controls.reduce((sum, ctrl) => sum + this.getItemTotal(ctrl), 0);
  }

  onTabChange(i: number): void { this.activeTab = i; }

  onSubmit(): void {
    this.formSubmitted = true;

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.value;
    this.isSaving = true;

    if (this.data.mode === 'create') {
      const payload: CreateInvoiceDto = {
        ...raw,
        invoiceDate: new Date(raw.invoiceDate).toISOString(),
        dueDate:     new Date(raw.dueDate).toISOString(),
      };
      this.invoiceService.create(payload).pipe(take(1)).subscribe({
        next: (res) => {
          this.isSaving = false;
          if (res.success) {
            this.dialogRef.close({ success: true, data: res.data });
          } else {
            this.snackBar.open(res.message || 'Failed', 'Close', { duration: 3000 });
          }
        },
        error: (err) => {
          this.isSaving = false;
          this.snackBar.open(err?.error?.message || 'Error', 'Close', { duration: 4000 });
        },
      });
    } else {
      const payload: UpdateInvoiceDto = {
        parentId:    raw.parentId || undefined,
        dueDate:     new Date(raw.dueDate).toISOString(),
        description: raw.description,
        notes:       raw.notes,
      };
      this.invoiceService.update(this.data.invoice!.id, payload).pipe(take(1)).subscribe({
        next: (res) => {
          this.isSaving = false;
          if (res.success) {
            this.dialogRef.close({ success: true, data: res.data });
          } else {
            this.snackBar.open(res.message || 'Failed', 'Close', { duration: 3000 });
          }
        },
        error: (err) => {
          this.isSaving = false;
          this.snackBar.open(err?.error?.message || 'Error', 'Close', { duration: 4000 });
        },
      });
    }
  }

  onCancel(): void {
    this.dialogRef.close({ success: false });
  }

  formatCurrency(val: number): string {
    return new Intl.NumberFormat('en-KE', { style: 'currency', currency: 'KES', maximumFractionDigits: 0 }).format(val);
  }
}