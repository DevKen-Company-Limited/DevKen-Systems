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
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, forkJoin, of } from 'rxjs';
import { catchError, takeUntil, finalize } from 'rxjs/operators';

import { AuthService } from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { SchoolDto } from 'app/Tenant/types/school';
import { BOOK_CONDITIONS, BookCopyDto, CreateBookCopyRequest, UpdateBookCopyRequest } from 'app/Library/book-copy/Types/book-copy.types';
import { BaseFormDialog } from 'app/shared/dialogs/BaseFormDialog';
import { BookDto } from 'app/Library/book/Types/book.types';
import { LibraryBranchDto } from 'app/Library/library-branch/Types/library-branch.types';
import { LibraryBranchService } from 'app/core/DevKenService/Library/library-branch.service';
import { BookCopyService } from 'app/core/DevKenService/Library/book-copy.service';
import { BookService } from 'app/core/DevKenService/Library/book.service';
import { MatSnackBar } from '@angular/material/snack-bar';

export interface CreateEditBookCopyDialogData {
  mode: 'create' | 'edit';
  copy?: BookCopyDto;
  preselectedBookId?: string;
}

@Component({
  selector: 'app-create-edit-book-copy-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatDatepickerModule,
    MatSlideToggleModule, MatProgressSpinnerModule,
    MatTooltipModule,                         // ← MatSnackBarModule removed
  ],
  templateUrl: './create-edit-book-copy-dialog.component.html',
  styles: [`
    :host ::ng-deep .mat-mdc-dialog-container { --mdc-dialog-container-shape: 12px; }
  `]
})
export class CreateEditBookCopyDialogComponent
  extends BaseFormDialog<CreateBookCopyRequest, UpdateBookCopyRequest, BookCopyDto, CreateEditBookCopyDialogData>
  implements OnInit, OnDestroy {

  private readonly _unsubscribe = new Subject<void>();
  private alertService!: AlertService;
  

  schools:    SchoolDto[]        = [];
  books:      BookDto[]          = [];
  branches:   LibraryBranchDto[] = [];
  conditions  = BOOK_CONDITIONS;
  isLoading   = true;
  formSubmitted = false;

  get isEditMode():   boolean { return this.data.mode === 'edit'; }
  get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }

  get dialogTitle(): string {
    return this.isEditMode ? 'Edit Book Copy' : 'Add Book Copy';
  }

  get dialogSubtitle(): string {
    return this.isEditMode
      ? `Updating copy (${this.data.copy?.accessionNumber || ''})`
      : 'Register a new physical copy of a book';
  }

  get filteredBooks(): BookDto[] {
    if (!this.isSuperAdmin) return this.books;
    const schoolId = this.form.get('schoolId')?.value;
    return schoolId ? this.books.filter(b => b.schoolId === schoolId) : [];
  }

  get filteredBranches(): LibraryBranchDto[] {
    if (!this.isSuperAdmin) return this.branches;
    const schoolId = this.form.get('schoolId')?.value;
    return schoolId ? this.branches.filter(b => b.schoolId === schoolId) : [];
  }

  constructor(
    fb:           FormBuilder,
    snackBar:     MatSnackBar,
    alertService: AlertService,           // ← replaces MatSnackBar
    dialogRef:    MatDialogRef<CreateEditBookCopyDialogComponent>,
    @Inject(MAT_DIALOG_DATA) data: CreateEditBookCopyDialogData,
    private readonly _authService:   AuthService,
    private readonly _schoolService: SchoolService,
    private readonly _bookService:   BookService,
    private readonly _branchService: LibraryBranchService,
    private readonly _cdr:           ChangeDetectorRef,
    copyService: BookCopyService,
  ) {
    super(fb, copyService, snackBar, dialogRef, data);
    this.alertService    = alertService;
    dialogRef.addPanelClass(['book-copy-dialog', 'responsive-dialog']);
  }

  ngOnInit():    void { this.init(); }
  ngOnDestroy(): void { this._unsubscribe.next(); this._unsubscribe.complete(); }

  protected override buildForm(): FormGroup {
    return this.fb.group({
      schoolId:        [null,   this.isSuperAdmin ? [Validators.required] : []],
      bookId:          [this.data.preselectedBookId || null, [Validators.required]],
      libraryBranchId: [null,   [Validators.required]],
      accessionNumber: ['',     [Validators.maxLength(50)]],
      barcode:         ['',     [Validators.maxLength(50)]],
      qrCode:          ['',     [Validators.maxLength(100)]],
      condition:       ['Good', [Validators.required]],
      isAvailable:     [true],
      isLost:          [false],
      isDamaged:       [false],
      acquiredOn:      [null],
    });
  }

  protected override init(): void {
    this.form = this.buildForm();

    if (this.isSuperAdmin) {
      this.form.get('schoolId')?.valueChanges
        .pipe(takeUntil(this._unsubscribe))
        .subscribe(() => {
          this.form.patchValue({ bookId: null, libraryBranchId: null }, { emitEvent: false });
        });
    }

    this.form.get('isLost')?.valueChanges
      .pipe(takeUntil(this._unsubscribe))
      .subscribe(val => { if (val) this.form.patchValue({ isAvailable: false }, { emitEvent: false }); });

    this.form.get('isDamaged')?.valueChanges
      .pipe(takeUntil(this._unsubscribe))
      .subscribe(val => { if (val) this.form.patchValue({ isAvailable: false }, { emitEvent: false }); });

    this._loadDropdowns();
  }

  protected override patchForEdit(item: BookCopyDto): void {
    this.form.patchValue({
      schoolId:        item.schoolId        || null,
      bookId:          item.bookId          || null,
      libraryBranchId: item.libraryBranchId || null,
      accessionNumber: item.accessionNumber || '',
      barcode:         item.barcode         || '',
      qrCode:          item.qrCode          || '',
      condition:       item.condition       || 'Good',
      isAvailable:     item.isAvailable,
      isLost:          item.isLost,
      isDamaged:       item.isDamaged,
      acquiredOn:      item.acquiredOn ? new Date(item.acquiredOn) : null,
    });
    this._cdr.detectChanges();
  }

  private _loadDropdowns(): void {
    this.isLoading = true;

    const requests: any = {
      books:    this._bookService.getAll().pipe(catchError(() => of({ success: false, data: [] }))),
      branches: this._branchService.getAll().pipe(catchError(() => of({ success: false, data: [] }))),
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
        this.books    = res.books?.data    || [];
        this.branches = res.branches?.data || [];
        if (res.schools) this.schools = res.schools?.data || [];
        if (this.isEditMode && this.data.copy) this.patchForEdit(this.data.copy);
      },
      error: () => {
        if (this.isEditMode && this.data.copy) this.patchForEdit(this.data.copy);
      }
    });

    setTimeout(() => {
      if (this.isLoading) { this.isLoading = false; this._cdr.detectChanges(); }
    }, 12000);
  }

  onSubmit(): void {
    this.formSubmitted = true;

    const toIso = (val: any): string | undefined => {
      if (!val) return undefined;
      const d = val instanceof Date ? val : new Date(val);
      return isNaN(d.getTime()) ? undefined : d.toISOString();
    };

    const createMapper = (raw: any): CreateBookCopyRequest => ({
      ...(this.isSuperAdmin ? { schoolId: raw.schoolId } : {}),
      bookId:          raw.bookId,
      libraryBranchId: raw.libraryBranchId,
      accessionNumber: raw.accessionNumber?.trim() || undefined,
      barcode:         raw.barcode?.trim()         || undefined,
      qrCode:          raw.qrCode?.trim()          || undefined,
      condition:       raw.condition,
      isAvailable:     raw.isAvailable,
      isLost:          raw.isLost,
      isDamaged:       raw.isDamaged,
      acquiredOn:      toIso(raw.acquiredOn),
    });

    const updateMapper = (raw: any): UpdateBookCopyRequest => ({
      libraryBranchId: raw.libraryBranchId,
      accessionNumber: raw.accessionNumber?.trim(),
      barcode:         raw.barcode?.trim(),
      qrCode:          raw.qrCode?.trim() || undefined,
      condition:       raw.condition,
      isAvailable:     raw.isAvailable,
      isLost:          raw.isLost,
      isDamaged:       raw.isDamaged,
      acquiredOn:      toIso(raw.acquiredOn),
    });

    this.save(createMapper, updateMapper, () => this.data.copy!.id);
  }

  onCancel(): void { this.close({ success: false }); }

  getFieldError(field: string): string {
    const c = this.form.get(field);
    if (!c || !(this.formSubmitted || c.touched)) return '';
    if (c.hasError('required'))  return 'This field is required';
    if (c.hasError('maxlength')) return `Maximum ${c.getError('maxlength').requiredLength} characters`;
    return 'Invalid value';
  }
}