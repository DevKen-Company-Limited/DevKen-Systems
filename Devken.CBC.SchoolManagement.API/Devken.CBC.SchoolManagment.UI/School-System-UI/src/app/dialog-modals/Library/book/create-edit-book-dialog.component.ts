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
import { SchoolDto } from 'app/Tenant/types/school';
import { BookDto, CreateBookRequest, UpdateBookRequest } from 'app/Library/book/Types/book.types';
import { BookAuthorService } from 'app/core/DevKenService/Library/book-author.service';
import { BookCategoryService } from 'app/core/DevKenService/Library/book-category.service';
import { BookPublisherService } from 'app/core/DevKenService/Library/book-publisher.service';
import { BookService } from 'app/core/DevKenService/Library/book.service';
import { BookAuthorResponseDto } from 'app/Library/book-author/Types/book-author.model';
import { BookCategoryResponseDto } from 'app/Library/book-category/Types/book-category.model';
import { BookPublisherResponseDto } from 'app/Library/book-publisher/Types/book-publisher.model';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';   // <-- new import

export interface CreateEditBookDialogData {
  mode: 'create' | 'edit';
  book?: BookDto;
}

@Component({
  selector: 'app-create-edit-book-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
    MatTooltipModule,                          // MatSnackBarModule removed
  ],
  templateUrl: './create-edit-book-dialog.component.html',
  styles: [`
    :host ::ng-deep .mat-mdc-dialog-container { --mdc-dialog-container-shape: 12px; }
  `]
})
export class CreateEditBookDialogComponent implements OnInit, OnDestroy {   // removed extends

  private readonly _unsubscribe = new Subject<void>();

  // ── Dropdown data ──────────────────────────────────────────────────────────
  schools: SchoolDto[]       = [];
  authors: BookAuthorResponseDto[]   = [];
  categories: BookCategoryResponseDto[] = [];
  publishers: BookPublisherResponseDto[] = [];
  isLoading = true;          // also used for save operation
  formSubmitted = false;

  currentYear = new Date().getFullYear();
  yearRange   = Array.from({ length: 75 }, (_, i) => this.currentYear - i);

  // ── Form ───────────────────────────────────────────────────────────────────
  form!: FormGroup;

  // ── Getters ────────────────────────────────────────────────────────────────
  get isEditMode(): boolean { return this.data.mode === 'edit'; }
  get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }
  get dialogTitle(): string { return this.isEditMode ? 'Edit Book' : 'Add New Book'; }
  get dialogSubtitle(): string {
    return this.isEditMode
      ? `Updating "${this.data.book?.title || 'book'}" details`
      : 'Fill in the details to add a new book to the library';
  }
  get descriptionLength(): number { return this.form.get('description')?.value?.length || 0; }

  constructor(
    private readonly _fb: FormBuilder,
    private readonly _dialogRef: MatDialogRef<CreateEditBookDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: CreateEditBookDialogData,
    private readonly _authService: AuthService,
    private readonly _schoolService: SchoolService,
    private readonly _bookAuthorService: BookAuthorService,
    private readonly _bookCategoryService: BookCategoryService,
    private readonly _bookPublisherService: BookPublisherService,
    private readonly _bookService: BookService,
    private readonly _alertService: AlertService,        // <-- injected instead of MatSnackBar
    private readonly _cdr: ChangeDetectorRef,
  ) {
    _dialogRef.addPanelClass(['book-dialog', 'responsive-dialog']);
  }

  // ── Lifecycle ──────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this._buildForm();
    this._loadDropdowns();
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  // ── Form building ──────────────────────────────────────────────────────────
  private _buildForm(): void {
    this.form = this._fb.group({
      schoolId:        [null, this.isSuperAdmin ? [Validators.required] : []],
      title:           ['',   [Validators.required, Validators.maxLength(255)]],
      isbn:            ['',   [Validators.required, Validators.maxLength(20)]],
      categoryId:      [null, [Validators.required]],
      authorId:        [null, [Validators.required]],
      publisherId:     [null, [Validators.required]],
      publicationYear: [this.currentYear, [Validators.required, Validators.min(1000), Validators.max(9999)]],
      language:        [''],
      description:     ['', [Validators.maxLength(2000)]],
    });

    if (this.isEditMode && this.data.book) {
      this._patchForm(this.data.book);
    }
  }

  private _patchForm(book: BookDto): void {
    this.form.patchValue({
      schoolId:        book.schoolId        || null,
      title:           book.title           || '',
      isbn:            book.isbn            || '',
      categoryId:      book.categoryId      || null,
      authorId:        book.authorId        || null,
      publisherId:     book.publisherId     || null,
      publicationYear: book.publicationYear || this.currentYear,
      language:        book.language        || '',
      description:     book.description     || '',
    });
    this._cdr.detectChanges();
  }

  // ── Data loading ───────────────────────────────────────────────────────────
  private _loadDropdowns(): void {
    this.isLoading = true;

    const requests: any = {
      authors:    this._bookAuthorService.getAll().pipe(catchError(() => of({ success: false, data: [] }))),
      categories: this._bookCategoryService.getAll().pipe(catchError(() => of({ success: false, data: [] }))),
      publishers: this._bookPublisherService.getAll().pipe(catchError(() => of({ success: false, data: [] }))),
    };

    if (this.isSuperAdmin) {
      requests.schools = this._schoolService.getAll().pipe(catchError(() => of({ success: false, data: [] })));
    }

    forkJoin(requests).pipe(
      takeUntil(this._unsubscribe),
      finalize(() => { this.isLoading = false; this._cdr.detectChanges(); })
    ).subscribe({
      next: (res: any) => {
        this.authors    = res.authors?.data    || [];
        this.categories = res.categories?.data || [];
        this.publishers = res.publishers?.data || [];
        if (res.schools) this.schools = res.schools?.data || [];

        // If we already patched in _buildForm, no need to patch again.
        // But if we are in edit mode and the book was not patched because
        // the book was not yet available, we patch now.
        if (this.isEditMode && this.data.book && !this.form.get('title')?.value) {
          this._patchForm(this.data.book);
        }
      },
      error: () => {
        if (this.isEditMode && this.data.book) this._patchForm(this.data.book);
      }
    });

    // Safety timeout
    setTimeout(() => {
      if (this.isLoading) {
        this.isLoading = false;
        this._cdr.detectChanges();
      }
    }, 12000);
  }

  // ── Submit & Cancel ────────────────────────────────────────────────────────
  onSubmit(): void {
    this.formSubmitted = true;

    if (this.form.invalid) {
      // Mark all fields as touched to trigger validation messages
      Object.keys(this.form.controls).forEach(key => {
        const control = this.form.get(key);
        control?.markAsTouched();
      });
      return;
    }

    this.isLoading = true;

    if (this.isEditMode) {
      this._updateBook();
    } else {
      this._createBook();
    }
  }

  private _createBook(): void {
    const raw = this.form.value;
    const request: CreateBookRequest = {
      ...(this.isSuperAdmin ? { schoolId: raw.schoolId } : {}),
      title:           raw.title?.trim(),
      isbn:            raw.isbn?.trim(),
      categoryId:      raw.categoryId,
      authorId:        raw.authorId,
      publisherId:     raw.publisherId,
      publicationYear: +raw.publicationYear,
      language:        raw.language?.trim()    || undefined,
      description:     raw.description?.trim() || undefined,
    };

    this._bookService.create(request).pipe(
      takeUntil(this._unsubscribe),
      finalize(() => { this.isLoading = false; this._cdr.detectChanges(); })
    ).subscribe({
      next: (response) => {
        if (response.success) {
          this._alertService.success('Book created successfully', 'Success');
          this._dialogRef.close({ success: true, data: response.data });
        } else {
          this._alertService.error(response.message || 'Failed to create book', 'Error');
        }
      },
      error: (err) => {
        console.error('Create book error:', err);
        this._alertService.error('An unexpected error occurred', 'Error');
      }
    });
  }

  private _updateBook(): void {
    if (!this.data.book?.id) {
      this._alertService.error('Book ID is missing', 'Error');
      this.isLoading = false;
      return;
    }

    const raw = this.form.value;
    const request: UpdateBookRequest = {
      title:           raw.title?.trim(),
      isbn:            raw.isbn?.trim(),
      categoryId:      raw.categoryId,
      authorId:        raw.authorId,
      publisherId:     raw.publisherId,
      publicationYear: +raw.publicationYear,
      language:        raw.language?.trim()    || undefined,
      description:     raw.description?.trim() || undefined,
    };

    this._bookService.update(this.data.book.id, request).pipe(
      takeUntil(this._unsubscribe),
      finalize(() => { this.isLoading = false; this._cdr.detectChanges(); })
    ).subscribe({
      next: (response) => {
        if (response.success) {
          this._alertService.success('Book updated successfully', 'Success');
          this._dialogRef.close({ success: true, data: response.data });
        } else {
          this._alertService.error(response.message || 'Failed to update book', 'Error');
        }
      },
      error: (err) => {
        console.error('Update book error:', err);
        this._alertService.error('An unexpected error occurred', 'Error');
      }
    });
  }

  onCancel(): void {
    this._dialogRef.close({ success: false });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  getFieldError(field: string): string {
    const c = this.form.get(field);
    if (!c || !(this.formSubmitted || c.touched)) return '';
    if (c.hasError('required'))  return 'This field is required';
    if (c.hasError('maxlength')) return `Maximum ${c.getError('maxlength').requiredLength} characters`;
    if (c.hasError('min'))       return `Minimum value is ${c.getError('min').min}`;
    if (c.hasError('max'))       return `Maximum value is ${c.getError('max').max}`;
    return 'Invalid value';
  }

  // ── Filtered dropdowns for SuperAdmin ──────────────────────────────────────
  get filteredAuthors(): BookAuthorResponseDto[] {
    if (!this.isSuperAdmin) return this.authors;
    const schoolId = this.form.get('schoolId')?.value;
    return schoolId ? this.authors.filter(a => a.tenantId === schoolId) : [];
  }

  get filteredCategories(): BookCategoryResponseDto[] {
    if (!this.isSuperAdmin) return this.categories;
    const schoolId = this.form.get('schoolId')?.value;
    return schoolId ? this.categories.filter(c => c.tenantId === schoolId) : [];
  }

  get filteredPublishers(): BookPublisherResponseDto[] {
    if (!this.isSuperAdmin) return this.publishers;
    const schoolId = this.form.get('schoolId')?.value;
    return schoolId ? this.publishers.filter(p => p.tenantId === schoolId) : [];
  }
}