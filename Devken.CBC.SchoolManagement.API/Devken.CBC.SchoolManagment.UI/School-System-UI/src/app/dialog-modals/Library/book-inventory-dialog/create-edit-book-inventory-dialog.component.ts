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
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, forkJoin, of } from 'rxjs';
import { catchError, takeUntil, finalize } from 'rxjs/operators';

import { AuthService } from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { SchoolDto } from 'app/Tenant/types/school';
import { BookInventoryService } from 'app/core/DevKenService/Library/book-inventory.service';
import { BookService } from 'app/core/DevKenService/Library/book.service';
import { BookInventoryDto, CreateBookInventoryRequest, UpdateBookInventoryRequest } from 'app/Library/book-inventory/Types/book-inventory.types';
import { BookDto } from 'app/Library/book/Types/book.types';
import { BaseFormDialog } from 'app/shared/dialogs/BaseFormDialog';
import { MatSnackBar } from '@angular/material/snack-bar';

export interface CreateEditBookInventoryDialogData {
  mode: 'create' | 'edit';
  inventory?: BookInventoryDto;
  preselectedBookId?: string;
}

@Component({
  selector: 'app-create-edit-book-inventory-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
    MatTooltipModule,                         // ← MatSnackBarModule removed
  ],
  templateUrl: './create-edit-book-inventory-dialog.component.html',
  styles: [`
    :host ::ng-deep .mat-mdc-dialog-container { --mdc-dialog-container-shape: 12px; }
  `]
})
export class CreateEditBookInventoryDialogComponent
  extends BaseFormDialog<CreateBookInventoryRequest, UpdateBookInventoryRequest, BookInventoryDto, CreateEditBookInventoryDialogData>
  implements OnInit, OnDestroy {

  private readonly _unsubscribe = new Subject<void>();

  schools:  SchoolDto[] = [];
  books:    BookDto[]   = [];
  isLoading     = true;
  formSubmitted = false;

  get isEditMode():   boolean { return this.data.mode === 'edit'; }
  get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }

  get dialogTitle(): string {
    return this.isEditMode ? 'Edit Inventory Record' : 'Create Inventory Record';
  }

  get dialogSubtitle(): string {
    return this.isEditMode
      ? `Updating inventory for "${this.data.inventory?.bookTitle || ''}"`
      : 'Manually create an inventory record for a book';
  }

  get filteredBooks(): BookDto[] {
    if (!this.isSuperAdmin) return this.books;
    const schoolId = this.form.get('schoolId')?.value;
    return schoolId ? this.books.filter(b => b.schoolId === schoolId) : [];
  }

  get copiesSum(): number {
    const v = this.form.value;
    return (v.availableCopies || 0)
         + (v.borrowedCopies  || 0)
         + (v.lostCopies      || 0)
         + (v.damagedCopies   || 0);
  }

  get sumExceedsTotal(): boolean {
    return this.copiesSum > (this.form.get('totalCopies')?.value || 0);
  }

  constructor(
    fb:           FormBuilder,
    snackBar: MatSnackBar,
    alertService: AlertService,          
    dialogRef:    MatDialogRef<CreateEditBookInventoryDialogComponent>,
    @Inject(MAT_DIALOG_DATA) data: CreateEditBookInventoryDialogData,
    private readonly _authService:   AuthService,
    private readonly _schoolService: SchoolService,
    private readonly _bookService:   BookService,
    private readonly _cdr:           ChangeDetectorRef,
    inventoryService: BookInventoryService,
  ) {
    super(fb, inventoryService,snackBar, dialogRef, data);
    dialogRef.addPanelClass(['book-inventory-dialog', 'responsive-dialog']);
  }

  ngOnInit():    void { this.init(); }
  ngOnDestroy(): void { this._unsubscribe.next(); this._unsubscribe.complete(); }

  protected override buildForm(): FormGroup {
    return this.fb.group({
      schoolId:        [null, this.isSuperAdmin ? [Validators.required] : []],
      bookId:          [this.data.preselectedBookId || null, [Validators.required]],
      totalCopies:     [0, [Validators.required, Validators.min(0)]],
      availableCopies: [0, [Validators.required, Validators.min(0)]],
      borrowedCopies:  [0, [Validators.required, Validators.min(0)]],
      lostCopies:      [0, [Validators.required, Validators.min(0)]],
      damagedCopies:   [0, [Validators.required, Validators.min(0)]],
    });
  }

  protected override init(): void {
    this.form = this.buildForm();

    if (this.isSuperAdmin) {
      this.form.get('schoolId')?.valueChanges
        .pipe(takeUntil(this._unsubscribe))
        .subscribe(() => {
          this.form.patchValue({ bookId: null }, { emitEvent: false });
        });
    }

    this._loadDropdowns();
  }

  protected override patchForEdit(item: BookInventoryDto): void {
    this.form.patchValue({
      schoolId:        item.schoolId         || null,
      bookId:          item.bookId           || null,
      totalCopies:     item.totalCopies,
      availableCopies: item.availableCopies,
      borrowedCopies:  item.borrowedCopies,
      lostCopies:      item.lostCopies,
      damagedCopies:   item.damagedCopies,
    });
    this._cdr.detectChanges();
  }

  private _loadDropdowns(): void {
    this.isLoading = true;

    const requests: any = {
      books: this._bookService.getAll().pipe(catchError(() => of({ success: false, data: [] }))),
    };

    if (this.isSuperAdmin) {
      requests.schools = this._schoolService.getAll()
        .pipe(catchError(() => of({ success: false, data: [] })));
    }

    forkJoin(requests).pipe(
      takeUntil(this._unsubscribe),
      finalize(() => { this.isLoading = false; this._cdr.detectChanges(); })
    ).subscribe({
      next: (res: any) => {
        this.books = res.books?.data || [];
        if (res.schools) this.schools = res.schools?.data || [];
        if (this.isEditMode && this.data.inventory) this.patchForEdit(this.data.inventory);
      },
      error: () => {
        if (this.isEditMode && this.data.inventory) this.patchForEdit(this.data.inventory);
      }
    });

    setTimeout(() => {
      if (this.isLoading) { this.isLoading = false; this._cdr.detectChanges(); }
    }, 12000);
  }

  onSubmit(): void {
    this.formSubmitted = true;
    if (this.sumExceedsTotal) return;

    const createMapper = (raw: any): CreateBookInventoryRequest => ({
      ...(this.isSuperAdmin ? { schoolId: raw.schoolId } : {}),
      bookId:          raw.bookId,
      totalCopies:     +raw.totalCopies,
      availableCopies: +raw.availableCopies,
      borrowedCopies:  +raw.borrowedCopies,
      lostCopies:      +raw.lostCopies,
      damagedCopies:   +raw.damagedCopies,
    });

    const updateMapper = (raw: any): UpdateBookInventoryRequest => ({
      totalCopies:     +raw.totalCopies,
      availableCopies: +raw.availableCopies,
      borrowedCopies:  +raw.borrowedCopies,
      lostCopies:      +raw.lostCopies,
      damagedCopies:   +raw.damagedCopies,
    });

    this.save(createMapper, updateMapper, () => this.data.inventory!.id);
  }

  onCancel(): void { this.close({ success: false }); }

  getFieldError(field: string): string {
    const c = this.form.get(field);
    if (!c || !(this.formSubmitted || c.touched)) return '';
    if (c.hasError('required')) return 'This field is required';
    if (c.hasError('min'))      return 'Value cannot be negative';
    return 'Invalid value';
  }
}