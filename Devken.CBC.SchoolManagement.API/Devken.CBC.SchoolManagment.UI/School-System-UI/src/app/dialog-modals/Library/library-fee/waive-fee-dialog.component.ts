// app/dialog-modals/Library/library-fee/waive-fee-dialog.component.ts
import {
  Component, OnDestroy, Inject, ChangeDetectorRef,
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
import { Subject } from 'rxjs';
import { takeUntil, finalize } from 'rxjs/operators';

import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { LibraryFeeService } from 'app/core/DevKenService/Library/library-fee.service';
import { LibraryFeeDto } from 'app/Library/library-fee/Types/library-fee.types';

export interface WaiveFeeDialogData { fee: LibraryFeeDto; }

@Component({
  selector: 'app-waive-fee-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatDialogModule,
    MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
  ],
  templateUrl: './waive-fee-dialog.component.html',
})
export class WaiveFeeDialogComponent implements OnDestroy {
  private readonly _unsubscribe = new Subject<void>();

  form: FormGroup;
  isSaving      = false;
  formSubmitted = false;

  get reasonLength(): number { return this.form.get('reason')?.value?.length || 0; }

  constructor(
    fb: FormBuilder,
    private readonly _dialogRef:    MatDialogRef<WaiveFeeDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: WaiveFeeDialogData,
    private readonly _feeService:   LibraryFeeService,
    private readonly _alertService: AlertService,
    private readonly _cdr:          ChangeDetectorRef,
  ) {
    this.form = fb.group({
      reason: ['', [Validators.required, Validators.maxLength(500)]],
    });
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  onSubmit(): void {
    this.formSubmitted = true;
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    this.isSaving = true;
    this._feeService.waive(this.data.fee.id, { reason: this.form.value.reason.trim() }).pipe(
      takeUntil(this._unsubscribe),
      finalize(() => { this.isSaving = false; this._cdr.detectChanges(); }),
    ).subscribe({
      next: res => {
        if (res.success) {
          this._alertService.success('Fee waived successfully');
          this._dialogRef.close({ success: true, data: res.data });
        } else {
          this._alertService.error(res.message || 'Failed to waive fee');
        }
      },
      error: err => this._alertService.error(err?.error?.message || 'Failed to waive fee'),
    });
  }

  onCancel(): void { this._dialogRef.close({ success: false }); }

  getFieldError(field: string): string {
    const c = this.form.get(field);
    if (!c || !(this.formSubmitted || c.touched)) return '';
    if (c.hasError('required'))  return 'Reason is required';
    if (c.hasError('maxlength')) return `Maximum ${c.getError('maxlength').requiredLength} characters`;
    return 'Invalid value';
  }
}