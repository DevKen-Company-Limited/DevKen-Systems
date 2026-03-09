import {
  Component, Inject, OnInit, OnDestroy, inject, ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule }       from '@angular/material/form-field';
import { MatInputModule }           from '@angular/material/input';
import { MatButtonModule }          from '@angular/material/button';
import { MatIconModule }            from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule }          from '@angular/material/select';
import { MatTooltipModule }         from '@angular/material/tooltip';
import { Subject }                  from 'rxjs';
import { takeUntil, finalize }      from 'rxjs/operators';
import { AuthService }              from 'app/core/auth/auth.service';
import { AlertService }             from 'app/core/DevKenService/Alert/AlertService';
import { SchoolService }            from 'app/core/DevKenService/Tenant/SchoolService';
import { SchoolDto }                from 'app/Tenant/types/school';
import { BookCategoryService }      from 'app/core/DevKenService/Library/book-category.service';
import {
  BookCategoryResponseDto,
  CreateBookCategoryDto,
  UpdateBookCategoryDto,
} from 'app/Library/book-category/Types/book-category.model';

export interface BookCategoryDialogData {
  mode: 'create' | 'edit';
  item?: BookCategoryResponseDto;
}

@Component({
  selector: 'app-book-category-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, MatButtonModule,
    MatIconModule, MatProgressSpinnerModule, MatSelectModule, MatTooltipModule,
  ],
  template: `
    <!-- Header -->
    <div class="bg-gradient-to-r from-violet-600 via-purple-600 to-fuchsia-600 relative overflow-hidden rounded-t-xl px-6 py-5">
      <div class="absolute inset-0 opacity-10 pointer-events-none">
        <div class="absolute top-0 right-0 w-32 h-32 bg-pink-200 rounded-full blur-2xl"></div>
        <div class="absolute bottom-0 left-0 w-40 h-40 bg-violet-300 rounded-full blur-2xl"></div>
      </div>
      <div class="relative z-10 flex items-center gap-3 pr-10">
        <div class="w-10 h-10 bg-white/20 backdrop-blur-sm rounded-xl flex items-center justify-center shrink-0">
          <mat-icon class="text-white icon-size-6">category</mat-icon>
        </div>
        <div>
          <h2 class="text-xl font-bold text-white m-0 leading-tight">{{ isEditMode ? 'Edit Category' : 'Add New Category' }}</h2>
          <p class="text-white/80 text-sm mt-0.5 mb-0">
            {{ isEditMode ? 'Updating: ' + data.item?.name : 'Fill in the details to create a new book category' }}
          </p>
        </div>
      </div>
      <button mat-icon-button class="absolute top-3 right-3 text-white z-20" (click)="onCancel()" matTooltip="Close">
        <mat-icon>close</mat-icon>
      </button>
    </div>

    <!-- Saving Overlay -->
    <div *ngIf="isSaving" class="absolute inset-0 bg-white/95 dark:bg-gray-900/95 z-50 flex items-center justify-center backdrop-blur-sm">
      <div class="text-center p-8">
        <div class="relative w-16 h-16 mx-auto mb-4">
          <mat-spinner diameter="60"></mat-spinner>
          <mat-icon class="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 text-violet-600">category</mat-icon>
        </div>
        <p class="text-lg font-semibold text-gray-800 dark:text-gray-200">{{ isEditMode ? 'Updating...' : 'Creating...' }}</p>
      </div>
    </div>

    <!-- Form -->
    <form [formGroup]="form" (ngSubmit)="onSubmit()" mat-dialog-content class="p-5 space-y-4">

      <!-- School (SuperAdmin only) -->
      <mat-form-field appearance="outline" class="w-full" *ngIf="isSuperAdmin">
        <mat-label>School <span class="text-red-500">*</span></mat-label>
        <mat-select formControlName="schoolId">
          <mat-option *ngFor="let s of schools" [value]="s.id">{{ s.name }}</mat-option>
        </mat-select>
        <mat-error *ngIf="form.get('schoolId')?.hasError('required')">School is required</mat-error>
      </mat-form-field>

      <!-- Name -->
      <mat-form-field appearance="outline" class="w-full">
        <mat-label>Category Name <span class="text-red-500">*</span></mat-label>
        <input matInput formControlName="name" placeholder="e.g. Science Fiction" autocomplete="off">
        <mat-icon matPrefix>category</mat-icon>
        <mat-error>{{ getFieldError('name') }}</mat-error>
      </mat-form-field>

      <!-- Description -->
      <mat-form-field appearance="outline" class="w-full">
        <mat-label>Description</mat-label>
        <textarea matInput formControlName="description" placeholder="Optional description..." rows="3" maxlength="500"></textarea>
        <mat-icon matPrefix>description</mat-icon>
        <mat-hint align="end">{{ descriptionLength }}/500</mat-hint>
        <mat-error>{{ getFieldError('description') }}</mat-error>
      </mat-form-field>

      <div *ngIf="formSubmitted && form.invalid"
           class="p-3 bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-700 rounded-lg">
        <div class="flex items-center gap-2 text-red-800 dark:text-red-200">
          <mat-icon class="icon-size-5">error_outline</mat-icon>
          <span class="font-medium">Please fix the errors above.</span>
        </div>
      </div>
    </form>

    <!-- Actions -->
    <div mat-dialog-actions class="flex justify-end gap-3 px-5 py-4 border-t border-gray-200 dark:border-gray-700">
      <button mat-button type="button" (click)="onCancel()" [disabled]="isSaving">Cancel</button>
      <button mat-flat-button color="primary" type="button" (click)="onSubmit()" [disabled]="isSaving">
        <mat-icon class="icon-size-5 mr-1">{{ isEditMode ? 'save' : 'check_circle' }}</mat-icon>
        {{ isSaving ? 'Saving...' : (isEditMode ? 'Update' : 'Submit') }}
      </button>
    </div>
  `,
})
export class BookCategoryDialogComponent implements OnInit, OnDestroy {
  private readonly _unsubscribe = new Subject<void>();
  private readonly _authService = inject(AuthService);
  private readonly _alert       = inject(AlertService);

  form!: FormGroup;
  formSubmitted = false;
  isSaving      = false;
  schools: SchoolDto[] = [];

  get isEditMode(): boolean     { return this.data.mode === 'edit'; }
  get isSuperAdmin(): boolean   { return this._authService.authUser?.isSuperAdmin ?? false; }
  get descriptionLength(): number { return this.form?.get('description')?.value?.length ?? 0; }

  constructor(
    private readonly _fb:            FormBuilder,
    private readonly _service:       BookCategoryService,
    private readonly _dialogRef:     MatDialogRef<BookCategoryDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public  data: BookCategoryDialogData,
    private readonly _cdr:           ChangeDetectorRef,
    private readonly _schoolService: SchoolService,
  ) {
    _dialogRef.addPanelClass('no-padding-dialog');
  }

  ngOnInit(): void {
    this._buildForm();
    if (this.isSuperAdmin) this._loadSchools();
    if (this.isEditMode && this.data.item) this._patchForm(this.data.item);
  }

  ngOnDestroy(): void { this._unsubscribe.next(); this._unsubscribe.complete(); }

  private _buildForm(): void {
    this.form = this._fb.group({
      schoolId:    ['', this.isSuperAdmin ? [Validators.required] : []],
      name:        ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', [Validators.maxLength(500)]],
    });
  }

  private _patchForm(item: BookCategoryResponseDto): void {
    this.form.patchValue({
      schoolId:    item.tenantId     ?? '',
      name:        item.name         ?? '',
      description: item.description  ?? '',
    });
    this._cdr.detectChanges();
  }

  private _loadSchools(): void {
    this._schoolService.getAll().subscribe({
      next: res => { if (res.success) this.schools = res.data; },
      error: () => this._alert.error('Failed to load schools'),
    });
  }

  onSubmit(): void {
    this.formSubmitted = true;
    if (this.form.invalid) return;
    const raw = this.form.getRawValue();

    if (this.isEditMode) {
      const payload: UpdateBookCategoryDto = {
        name:        raw.name?.trim(),
        description: raw.description?.trim() || undefined,
      };
      this.isSaving = true;
      this._service.update(this.data.item!.id, payload)
        .pipe(takeUntil(this._unsubscribe), finalize(() => { this.isSaving = false; this._cdr.detectChanges(); }))
        .subscribe({ next: res => { if (res.success) this._dialogRef.close({ success: true, data: res.data }); }, error: () => {} });
    } else {
      const payload: CreateBookCategoryDto = {
        tenantId:    this.isSuperAdmin ? raw.schoolId?.trim() : undefined,
        name:        raw.name?.trim(),
        description: raw.description?.trim() || undefined,
      };
      this.isSaving = true;
      this._service.create(payload)
        .pipe(takeUntil(this._unsubscribe), finalize(() => { this.isSaving = false; this._cdr.detectChanges(); }))
        .subscribe({ next: res => { if (res.success) this._dialogRef.close({ success: true, data: res.data }); }, error: () => {} });
    }
  }

  onCancel(): void { this._dialogRef.close(null); }

  getFieldError(field: string): string {
    const c = this.form.get(field);
    if (!c || !(this.formSubmitted || c.touched)) return '';
    if (c.hasError('required'))  return 'This field is required';
    if (c.hasError('maxlength')) return `Maximum ${c.getError('maxlength').requiredLength} characters`;
    return 'Invalid value';
  }
}