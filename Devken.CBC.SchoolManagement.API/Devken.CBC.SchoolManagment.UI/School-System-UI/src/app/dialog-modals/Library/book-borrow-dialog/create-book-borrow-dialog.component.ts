import {
  Component, OnInit, OnDestroy, Inject, ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, FormArray, FormControl } from '@angular/forms';
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
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subject, forkJoin, of } from 'rxjs';
import { catchError, takeUntil, finalize } from 'rxjs/operators';

import { AuthService } from 'app/core/auth/auth.service';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { BookCopyService } from 'app/core/DevKenService/Library/book-copy.service';
import { BaseFormDialog } from 'app/shared/dialogs/BaseFormDialog';
import { BookCopyDto } from 'app/Library/book-copy/Types/book-copy.types';
import {
  BookBorrowDto,
  BookBorrowItemDto,
  CreateBookBorrowRequest,
  UpdateBookBorrowRequest,
} from 'app/Library/book-borrow/Types/book-borrow.types';
import { BookBorrowService } from 'app/core/DevKenService/Library/book-borrow.service';


export interface CreateBookBorrowDialogData {
  mode: 'create' | 'edit' | 'view';
  borrow?: BookBorrowDto;
}

@Component({
  selector: 'app-create-book-borrow-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatDatepickerModule,
    MatProgressSpinnerModule, MatTooltipModule, MatCheckboxModule,
    MatDividerModule,
  ],
  templateUrl: './create-book-borrow-dialog.component.html',
  styles: [`:host ::ng-deep .mat-mdc-dialog-container { --mdc-dialog-container-shape: 12px; }`]
})
export class CreateBookBorrowDialogComponent
  extends BaseFormDialog<CreateBookBorrowRequest, UpdateBookBorrowRequest, BookBorrowDto, CreateBookBorrowDialogData>
  implements OnInit, OnDestroy {

  private readonly _unsubscribe = new Subject<void>();
  private readonly _alertService: AlertService;
  private readonly _borrowService: BookBorrowService;

  availableCopies:  BookCopyDto[] = [];
  selectedCopyIds:  Set<string>   = new Set();
  isLoading         = false;
  formSubmitted     = false;
  isSaving          = false;

  get isEditMode(): boolean { return this.data.mode === 'edit'; }
  get isViewMode(): boolean { return this.data.mode === 'view'; }
  get isCreateMode(): boolean { return this.data.mode === 'create'; }
  get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }

  get dialogTitle(): string {
    const map = { create: 'New Borrow Transaction', edit: 'Edit Borrow', view: 'Borrow Details' };
    return map[this.data.mode] || 'Borrow';
  }

  get dialogSubtitle(): string {
    if (this.isViewMode && this.data.borrow) {
      return `Transaction for ${this.data.borrow.memberName}`;
    }
    if (this.isEditMode && this.data.borrow) {
      return `Updating due date for ${this.data.borrow.memberName}`;
    }
    return 'Issue books to a library member';
  }

  get headerGradient(): string {
    if (this.isViewMode) return 'bg-gradient-to-r from-indigo-600 via-violet-600 to-purple-600';
    return 'bg-gradient-to-r from-violet-600 via-indigo-600 to-blue-600';
  }

  get borrow(): BookBorrowDto | undefined { return this.data.borrow; }

  constructor(
    fb:            FormBuilder,
    snackBar:      MatSnackBar,
    alertService:  AlertService,
    dialogRef:     MatDialogRef<CreateBookBorrowDialogComponent>,
    @Inject(MAT_DIALOG_DATA) data: CreateBookBorrowDialogData,
    private readonly _authService:   AuthService,
    private readonly _copyService:   BookCopyService,
    borrowService: BookBorrowService,
    private readonly _cdr: ChangeDetectorRef,
  ) {
    super(fb, borrowService, snackBar, dialogRef, data);
    this._alertService  = alertService;
    this._borrowService = borrowService;
    dialogRef.addPanelClass(['book-borrow-dialog', 'responsive-dialog']);
  }

  ngOnInit():    void { this.init(); }
  ngOnDestroy(): void { this._unsubscribe.next(); this._unsubscribe.complete(); }

  protected override buildForm(): FormGroup {
    if (this.isEditMode) {
      return this.fb.group({
        dueDate: [null, [Validators.required]],
      });
    }
    return this.fb.group({
      memberId:   ['', [Validators.required]],
      borrowDate: [new Date(), [Validators.required]],
      dueDate:    [null, [Validators.required]],
    });
  }

  protected override init(): void {
    this.form = this.buildForm();
    if (!this.isViewMode) this._loadDropdowns();
    if (this.isEditMode && this.data.borrow) this.patchForEdit(this.data.borrow);
  }

  protected override patchForEdit(item: BookBorrowDto): void {
    this.form.patchValue({
      dueDate: item.dueDate ? new Date(item.dueDate) : null,
    });
    this._cdr.detectChanges();
  }

  private _loadDropdowns(): void {
    if (this.isEditMode) return;
    this.isLoading = true;
    this._copyService.getAll().pipe(
      takeUntil(this._unsubscribe),
      finalize(() => { this.isLoading = false; this._cdr.detectChanges(); })
    ).subscribe({
      next: res => {
        this.availableCopies = (res.data || []).filter(c => c.isAvailable);
      },
      error: () => {}
    });
  }

  toggleCopySelection(copyId: string): void {
    if (this.selectedCopyIds.has(copyId)) {
      this.selectedCopyIds.delete(copyId);
    } else {
      this.selectedCopyIds.add(copyId);
    }
  }

  isCopySelected(copyId: string): boolean {
    return this.selectedCopyIds.has(copyId);
  }

  onSubmit(): void {
    this.formSubmitted = true;
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    if (this.isCreateMode && this.selectedCopyIds.size === 0) {
      this._alertService.error('Please select at least one book copy to borrow');
      return;
    }

    const toIso = (val: any): string => {
      const d = val instanceof Date ? val : new Date(val);
      return d.toISOString();
    };

    const raw = this.form.value;
    this.isSaving = true;

    if (this.isEditMode) {
      const payload: UpdateBookBorrowRequest = {
        dueDate: toIso(raw.dueDate),
      };
      this._borrowService.update(this.data.borrow!.id, payload)
        .pipe(takeUntil(this._unsubscribe), finalize(() => { this.isSaving = false; this._cdr.detectChanges(); }))
        .subscribe({
          next: res => {
            if (res.success) {
              this._alertService.success('Due date updated successfully');
              this.dialogRef.close({ success: true, data: res.data });
            } else {
              this._alertService.error(res.message || 'Failed to update');
            }
          },
          error: err => this._alertService.error(err?.error?.message || 'Failed to update'),
        });
    } else {
      const payload: CreateBookBorrowRequest = {
        memberId:    raw.memberId,
        borrowDate:  toIso(raw.borrowDate),
        dueDate:     toIso(raw.dueDate),
        bookCopyIds: Array.from(this.selectedCopyIds),
        ...(this.isSuperAdmin ? {} : {}),
      };
      this._borrowService.create(payload)
        .pipe(takeUntil(this._unsubscribe), finalize(() => { this.isSaving = false; this._cdr.detectChanges(); }))
        .subscribe({
          next: res => {
            if (res.success) {
              this._alertService.success('Borrow transaction created successfully');
              this.dialogRef.close({ success: true, data: res.data });
            } else {
              this._alertService.error(res.message || 'Failed to create borrow');
            }
          },
          error: err => this._alertService.error(err?.error?.message || 'Failed to create borrow'),
        });
    }
  }

  returnItem(item: BookBorrowItemDto): void {
    this._alertService.confirm({
      title:       'Return Book',
      message:     `Return "${item.bookTitle}"?`,
      confirmText: 'Return',
      onConfirm: () => {
        this._borrowService.returnBook({ borrowItemId: item.id }).pipe(takeUntil(this._unsubscribe)).subscribe({
          next: res => {
            if (res.success) {
              this._alertService.success(`"${item.bookTitle}" returned successfully`);
              this.dialogRef.close({ success: true });
            } else {
              this._alertService.error(res.message || 'Failed to return');
            }
          },
          error: err => this._alertService.error(err?.error?.message || 'Failed'),
        });
      },
    });
  }

  onCancel(): void { this.close({ success: false }); }

  getFieldError(field: string): string {
    const c = this.form.get(field);
    if (!c || !(this.formSubmitted || c.touched)) return '';
    if (c.hasError('required')) return 'This field is required';
    return 'Invalid value';
  }

  getItemStatusColor(item: BookBorrowItemDto): string {
    if (item.isReturned) return 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400';
    if (item.isOverdue)  return 'bg-red-100   dark:bg-red-900/30   text-red-700   dark:text-red-400';
    return 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400';
  }

  formatDate(dateStr?: string): string {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'KES' }).format(amount);
  }
}