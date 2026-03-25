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
import { MatSelectModule } from '@angular/material/select';
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
import { LibraryFineService } from 'app/core/DevKenService/Library/library-fine.service';
import { BookBorrowService } from 'app/core/DevKenService/Library/book-borrow.service';
import { BookBorrowItemDto } from 'app/Library/book-borrow/Types/book-borrow.types';
import { LibraryFineDto, CreateLibraryFineRequest } from 'app/Library/library-fines/Types/library-fine.types';

export interface CreateLibraryFineDialogData {
  mode: 'create' | 'waive';
  fine?: LibraryFineDto;
  preselectedBorrowItemId?: string;
}

@Component({
  selector: 'app-create-library-fine-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatDatepickerModule,
    MatProgressSpinnerModule, MatTooltipModule,
  ],
  templateUrl: './create-library-fine-dialog.component.html',
  styles: [':host ::ng-deep .mat-mdc-dialog-container { --mdc-dialog-container-shape: 12px; }']
})
export class CreateLibraryFineDialogComponent
  extends BaseFormDialog<CreateLibraryFineRequest, never, LibraryFineDto, CreateLibraryFineDialogData>
  implements OnInit, OnDestroy {

  private readonly _unsubscribe   = new Subject<void>();
  private readonly _alertService:  AlertService;
  private readonly _fineService:   LibraryFineService;
  private readonly _borrowService: BookBorrowService;

  borrowItems:  BookBorrowItemDto[] = [];
  isLoading     = false;
  formSubmitted = false;
  isSaving      = false;

  get isCreateMode(): boolean { return this.data.mode === 'create'; }
  get isWaiveMode():  boolean { return this.data.mode === 'waive';  }
  get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }

  get dialogTitle(): string {
    return this.isWaiveMode ? 'Waive Fine' : 'Issue Library Fine';
  }

  get dialogSubtitle(): string {
    if (this.isWaiveMode && this.data.fine) {
      return 'Waiving fine of ' + this.formatCurrency(this.data.fine.amount);
    }
    return 'Issue a new fine for a borrow item';
  }

  get headerGradient(): string {
    return this.isWaiveMode
      ? 'bg-gradient-to-r from-amber-500 via-orange-500 to-yellow-500'
      : 'bg-gradient-to-r from-rose-600 via-red-600 to-orange-600';
  }

  get selectedBorrowItem(): any {
    const id = this.form.get('borrowItemId')?.value;
    return id ? this.borrowItems.find((i: any) => i.id === id) : undefined;
  }

  constructor(
    fb:            FormBuilder,
    snackBar:      MatSnackBar,
    alertService:  AlertService,
    dialogRef:     MatDialogRef<CreateLibraryFineDialogComponent>,
    @Inject(MAT_DIALOG_DATA) data: CreateLibraryFineDialogData,
    private readonly _authService:  AuthService,
    fineService:   LibraryFineService,
    borrowService: BookBorrowService,
    private readonly _cdr: ChangeDetectorRef,
  ) {
    super(fb, fineService as any, snackBar, dialogRef, data);
    this._alertService  = alertService;
    this._fineService   = fineService;
    this._borrowService = borrowService;
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
      borrowItemId: [this.data.preselectedBorrowItemId || null, [Validators.required]],
      amount:       [null, [Validators.required, Validators.min(0.01)]],
      reason:       ['',   [Validators.required, Validators.maxLength(500)]],
      issuedOn:     [new Date()],
    });
  }

  protected override init(): void {
    this.form = this.buildForm();
    if (this.isCreateMode) this._loadBorrowItems();
  }

  private _loadBorrowItems(): void {
    this.isLoading = true;
    this._borrowService.getActive().pipe(
      takeUntil(this._unsubscribe),
      finalize(() => { this.isLoading = false; this._cdr.detectChanges(); })
    ).subscribe({
      next: res => {
        if (res.success) {
          // Flatten all active borrows to their unreturned items,
          // carrying the parent member info for display in the dropdown.
          this.borrowItems = res.data.flatMap(borrow =>
            borrow.items
              .filter(item => !item.isReturned)
              .map(item => ({
                ...item,
                _memberName:   borrow.memberName,
                _memberNumber: borrow.memberNumber,
              }))
          ) as any[];

          // Validate preselected id still exists in the list
          if (this.data.preselectedBorrowItemId) {
            const exists = this.borrowItems.some((i: any) => i.id === this.data.preselectedBorrowItemId);
            if (!exists) this.form.patchValue({ borrowItemId: null });
          }
        }
      },
      error: () => this._alertService.error('Failed to load borrow items'),
    });
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
    if (c.hasError('maxlength')) return 'Maximum ' + c.getError('maxlength').requiredLength + ' characters';
    return 'Invalid value';
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'KES' }).format(amount || 0);
  }
}