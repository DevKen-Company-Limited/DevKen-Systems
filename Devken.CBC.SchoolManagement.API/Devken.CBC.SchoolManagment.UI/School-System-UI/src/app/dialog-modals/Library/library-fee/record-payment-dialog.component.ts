// app/dialog-modals/Library/library-fee/record-payment-dialog.component.ts
import {
  Component, OnInit, OnDestroy, Inject, ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  MatDialogRef, MAT_DIALOG_DATA, MatDialogModule,
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { Subject } from 'rxjs';
import { takeUntil, finalize } from 'rxjs/operators';

import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { LibraryFeeService } from 'app/core/DevKenService/Library/library-fee.service';
import { LibraryFeeDto } from 'app/Library/library-fee/Types/library-fee.types';

export interface RecordPaymentDialogData {
  fee: LibraryFeeDto;
}

@Component({
  selector: 'app-record-payment-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatDialogModule,
    MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
    MatDatepickerModule, MatNativeDateModule,
  ],
  templateUrl: './record-payment-dialog.component.html',
})
export class RecordPaymentDialogComponent implements OnInit, OnDestroy {
  private readonly _unsubscribe = new Subject<void>();

  form!: FormGroup;
  isSaving      = false;
  formSubmitted = false;

  get balance(): number {
    return this.data.fee.amount - this.data.fee.amountPaid;
  }

  constructor(
    private readonly _fb:         FormBuilder,
    private readonly _dialogRef:  MatDialogRef<RecordPaymentDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: RecordPaymentDialogData,
    private readonly _feeService: LibraryFeeService,
    private readonly _alertService: AlertService,
    private readonly _cdr:        ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.form = this._fb.group({
      amountPaid: [this.balance, [
        Validators.required,
        Validators.min(0.01),
        Validators.max(this.balance),
      ]],
      paidOn: [new Date()],
    });
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  onPayFull(): void {
    this.form.get('amountPaid')?.setValue(this.balance);
  }

  onSubmit(): void {
    this.formSubmitted = true;
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    this.isSaving = true;
    const raw = this.form.value;

    this._feeService.recordPayment(this.data.fee.id, {
      amountPaid: +raw.amountPaid,
      paidOn: raw.paidOn ? new Date(raw.paidOn).toISOString() : undefined,
    }).pipe(
      takeUntil(this._unsubscribe),
      finalize(() => { this.isSaving = false; this._cdr.detectChanges(); }),
    ).subscribe({
      next: res => {
        if (res.success) {
          this._alertService.success('Payment recorded successfully');
          this._dialogRef.close({ success: true, data: res.data });
        } else {
          this._alertService.error(res.message || 'Failed to record payment');
        }
      },
      error: err => this._alertService.error(err?.error?.message || 'Failed to record payment'),
    });
  }

  onCancel(): void { this._dialogRef.close({ success: false }); }

  getFieldError(field: string): string {
    const c = this.form.get(field);
    if (!c || !(this.formSubmitted || c.touched)) return '';
    if (c.hasError('required')) return 'This field is required';
    if (c.hasError('min'))      return `Minimum amount is ${c.getError('min').min}`;
    if (c.hasError('max'))      return `Cannot exceed outstanding balance of KES ${this.balance}`;
    return 'Invalid value';
  }
}