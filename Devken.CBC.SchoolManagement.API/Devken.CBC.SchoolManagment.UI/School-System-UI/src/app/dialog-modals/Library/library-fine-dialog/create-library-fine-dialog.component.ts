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
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subject } from 'rxjs';
import { takeUntil, finalize } from 'rxjs/operators';

import { AuthService } from 'app/core/auth/auth.service';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { BaseFormDialog } from 'app/shared/dialogs/BaseFormDialog';
import { CreateLibraryFineRequest, LibraryFineDto } from 'app/Library/library-fines/Types/library-fine.types';
import { LibraryFineService } from 'app/core/DevKenService/Library/library-fine.service';



export interface CreateLibraryFineDialogData {
  mode: 'create' | 'waive';
  fine?: LibraryFineDto;
  /** Pre-fill borrow item if opening from within a borrow detail */
  preselectedBorrowItemId?: string;
}

@Component({
  selector: 'app-create-library-fine-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatDialogModule,
    MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatDatepickerModule,
    MatProgressSpinnerModule, MatTooltipModule,
  ],
  templateUrl: './create-library-fine-dialog.component.html',
  styles: [`:host ::ng-deep .mat-mdc-dialog-container { --mdc-dialog-container-shape: 12px; }`]
})
export class CreateLibraryFineDialogComponent
  extends BaseFormDialog<CreateLibraryFineRequest, never, LibraryFineDto, CreateLibraryFineDialogData>
  implements OnInit, OnDestroy {

  private readonly _unsubscribe = new Subject<void>();
  private readonly _alertService: AlertService;
  private readonly _fineService:  LibraryFineService;

  formSubmitted = false;
  isSaving      = false;

  get isCreateMode(): boolean { return this.data.mode === 'create'; }
  get isWaiveMode():  boolean { return this.data.mode === 'waive'; }
  get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }

  get dialogTitle(): string {
    return this.isWaiveMode ? 'Waive Fine' : 'Issue Library Fine';
  }

  get dialogSubtitle(): string {
    if (this.isWaiveMode && this.data.fine) {
      return `Waiving fine of ${this.formatCurrency(this.data.fine.amount)}`;
    }
    return 'Issue a new fine for a borrow item';
  }

  get headerGradient(): string {
    return this.isWaiveMode
      ? 'bg-gradient-to-r from-amber-500 via-orange-500 to-yellow-500'
      : 'bg-gradient-to-r from-rose-600 via-red-600 to-orange-600';
  }

  constructor(
    fb:           FormBuilder,
    snackBar:     MatSnackBar,
    alertService: AlertService,
    dialogRef:    MatDialogRef<CreateLibraryFineDialogComponent>,
    @Inject(MAT_DIALOG_DATA) data: CreateLibraryFineDialogData,
    private readonly _authService: AuthService,
    fineService:  LibraryFineService,
    private readonly _cdr: ChangeDetectorRef,
  ) {
    super(fb, fineService as any, snackBar, dialogRef, data);
    this._alertService = alertService;
    this._fineService  = fineService;
    dialogRef.addPanelClass(['library-fine-dialog', 'responsive-dialog']);
  }

  ngOnInit():    void { this.init(); }
  ngOnDestroy(): void { this._unsubscribe.next(); this._unsubscribe.complete(); }

  protected override buildForm(): FormGroup {
    if (this.isWaiveMode) {
      return this.fb.group({
        reason: ['', [Validators.required, Validators.maxLength(500)]],
      });
    }
    return this.fb.group({
      borrowItemId: [this.data.preselectedBorrowItemId || '', [Validators.required]],
      amount:       [null, [Validators.required, Validators.min(0.01)]],
      reason:       ['',   [Validators.required, Validators.maxLength(500)]],
      issuedOn:     [new Date()],
    });
  }

  protected override init(): void {
    this.form = this.buildForm();
  }

  onSubmit(): void {
    this.formSubmitted = true;
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    const raw = this.form.value;
    this.isSaving = true;

    if (this.isWaiveMode) {
      this._fineService.waiveFine({ fineId: this.data.fine!.id, reason: raw.reason })
        .pipe(takeUntil(this._unsubscribe), finalize(() => { this.isSaving = false; this._cdr.detectChanges(); }))
        .subscribe({
          next: res => {
            if (res.success) {
              this._alertService.success('Fine waived successfully');
              this.dialogRef.close({ success: true });
            } else {
              this._alertService.error(res.message || 'Failed to waive fine');
            }
          },
          error: err => this._alertService.error(err?.error?.message || 'Failed to waive fine'),
        });
    } else {
      const toIso = (val: any): string | undefined => {
        if (!val) return undefined;
        const d = val instanceof Date ? val : new Date(val);
        return isNaN(d.getTime()) ? undefined : d.toISOString();
      };

      const payload: CreateLibraryFineRequest = {
        borrowItemId: raw.borrowItemId,
        amount:       Number(raw.amount),
        reason:       raw.reason.trim(),
        issuedOn:     toIso(raw.issuedOn),
      };
      this._fineService.create(payload)
        .pipe(takeUntil(this._unsubscribe), finalize(() => { this.isSaving = false; this._cdr.detectChanges(); }))
        .subscribe({
          next: res => {
            if (res.success) {
              this._alertService.success('Fine issued successfully');
              this.dialogRef.close({ success: true, data: res.data });
            } else {
              this._alertService.error(res.message || 'Failed to issue fine');
            }
          },
          error: err => this._alertService.error(err?.error?.message || 'Failed to issue fine'),
        });
    }
  }

  onCancel(): void { this.close({ success: false }); }

  getFieldError(field: string): string {
    const c = this.form.get(field);
    if (!c || !(this.formSubmitted || c.touched)) return '';
    if (c.hasError('required'))  return 'This field is required';
    if (c.hasError('min'))       return 'Amount must be greater than 0';
    if (c.hasError('maxlength')) return `Maximum ${c.getError('maxlength').requiredLength} characters`;
    return 'Invalid value';
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'KES' }).format(amount || 0);
  }
}