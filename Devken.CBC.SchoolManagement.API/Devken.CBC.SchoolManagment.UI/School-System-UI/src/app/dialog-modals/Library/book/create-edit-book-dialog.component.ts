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
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
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
import { BaseFormDialog } from 'app/shared/dialogs/BaseFormDialog';
import { BookAuthorResponseDto } from 'app/Library/book-author/Types/book-author.model';
import { BookCategoryResponseDto } from 'app/Library/book-category/Types/book-category.model';
import { BookPublisherResponseDto } from 'app/Library/book-publisher/Types/book-publisher.model';

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
    MatSnackBarModule, MatTooltipModule,
  ],
  templateUrl: './create-edit-book-dialog.component.html',
  styles: [`
    :host ::ng-deep .mat-mdc-dialog-container { --mdc-dialog-container-shape: 12px; }
  `]
})
export class CreateEditBookDialogComponent
  extends BaseFormDialog<CreateBookRequest, UpdateBookRequest, BookDto, CreateEditBookDialogData>
  implements OnInit, OnDestroy {

  private readonly _unsubscribe = new Subject<void>();

  // ── Dropdown data ──────────────────────────────────────────────────────────
  schools: SchoolDto[]       = [];
  authors: BookAuthorResponseDto[]   = [];
  categories: BookCategoryResponseDto[] = [];
  publishers: BookPublisherResponseDto[] = [];
  isLoading = true;
  formSubmitted = false;

  currentYear = new Date().getFullYear();
  yearRange   = Array.from({ length: 75 }, (_, i) => this.currentYear - i);

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
    fb: FormBuilder,
    snackBar: MatSnackBar,
    dialogRef: MatDialogRef<CreateEditBookDialogComponent>,
    @Inject(MAT_DIALOG_DATA) data: CreateEditBookDialogData,
    private readonly _authService: AuthService,
    private readonly _schoolService: SchoolService,
    private readonly _bookAuthorService: BookAuthorService,
    private readonly _bookCategoryService: BookCategoryService,
    private readonly _bookPublisherService: BookPublisherService,
    private readonly _cdr: ChangeDetectorRef,
    bookService: BookService,
  ) {
    super(fb, bookService, snackBar, dialogRef, data);
    dialogRef.addPanelClass(['book-dialog', 'responsive-dialog']);
  }

  // ── Lifecycle ──────────────────────────────────────────────────────────────
  ngOnInit(): void { this.init(); }
  ngOnDestroy(): void { this._unsubscribe.next(); this._unsubscribe.complete(); }

  // ── BaseFormDialog overrides ───────────────────────────────────────────────
  protected override buildForm(): FormGroup {
    return this.fb.group({
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
  }

  /** Override init() so we patch using data.book, not data.school */
  protected override init(): void {
    this.form = this.buildForm();
    this._loadDropdowns();
  }

  protected override patchForEdit(item: BookDto): void {
    this.form.patchValue({
      schoolId:        item.schoolId        || null,
      title:           item.title           || '',
      isbn:            item.isbn            || '',
      categoryId:      item.categoryId      || null,
      authorId:        item.authorId        || null,
      publisherId:     item.publisherId     || null,
      publicationYear: item.publicationYear || this.currentYear,
      language:        item.language        || '',
      description:     item.description     || '',
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

        if (this.isEditMode && this.data.book) {
          this.patchForEdit(this.data.book);
        }
      },
      error: () => {
        if (this.isEditMode && this.data.book) this.patchForEdit(this.data.book);
      }
    });

    setTimeout(() => { if (this.isLoading) { this.isLoading = false; this._cdr.detectChanges(); } }, 12000);
  }

  // ── Submit & Cancel ────────────────────────────────────────────────────────
  onSubmit(): void {
    this.formSubmitted = true;

    const createMapper = (raw: any): CreateBookRequest => ({
      ...(this.isSuperAdmin ? { schoolId: raw.schoolId } : {}),
      title:           raw.title?.trim(),
      isbn:            raw.isbn?.trim(),
      categoryId:      raw.categoryId,
      authorId:        raw.authorId,
      publisherId:     raw.publisherId,
      publicationYear: +raw.publicationYear,
      language:        raw.language?.trim()     || undefined,
      description:     raw.description?.trim()  || undefined,
    });

    const updateMapper = (raw: any): UpdateBookRequest => ({
      title:           raw.title?.trim(),
      isbn:            raw.isbn?.trim(),
      categoryId:      raw.categoryId,
      authorId:        raw.authorId,
      publisherId:     raw.publisherId,
      publicationYear: +raw.publicationYear,
      language:        raw.language?.trim()    || undefined,
      description:     raw.description?.trim() || undefined,
    });

    this.save(createMapper, updateMapper, () => this.data.book!.id);
  }

  onCancel(): void { this.close({ success: false }); }

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

  /** Filter authors/categories/publishers by selected school for SuperAdmin */
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