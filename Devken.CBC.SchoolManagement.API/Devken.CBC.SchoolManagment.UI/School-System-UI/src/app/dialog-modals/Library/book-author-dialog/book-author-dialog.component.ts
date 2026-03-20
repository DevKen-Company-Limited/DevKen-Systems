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
import {
  BookAuthorResponseDto,
  CreateBookAuthorDto,
  UpdateBookAuthorDto,
} from 'app/Library/book-author/Types/book-author.model';
import { BookAuthorService } from 'app/core/DevKenService/Library/book-author.service';

export interface BookAuthorDialogData {
  mode: 'create' | 'edit';
  item?: BookAuthorResponseDto;
}

@Component({
  selector: 'app-book-author-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, MatButtonModule,
    MatIconModule, MatProgressSpinnerModule, MatSelectModule, MatTooltipModule,
  ],
  templateUrl: './book-author-dialog.component.html',
})
export class BookAuthorDialogComponent implements OnInit, OnDestroy {
  private readonly _unsubscribe = new Subject<void>();
  private readonly _authService = inject(AuthService);
  private readonly _alert       = inject(AlertService);

  form!: FormGroup;
  formSubmitted = false;
  isSaving      = false;
  schools: SchoolDto[] = [];

  get isEditMode(): boolean   { return this.data.mode === 'edit'; }
  get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }
  get biographyLength(): number { return this.form?.get('biography')?.value?.length ?? 0; }

  constructor(
    private readonly _fb:            FormBuilder,
    private readonly _service:       BookAuthorService,
    private readonly _dialogRef:     MatDialogRef<BookAuthorDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public  data: BookAuthorDialogData,
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
      schoolId:  ['', this.isSuperAdmin ? [Validators.required] : []],
      name:      ['', [Validators.required, Validators.maxLength(150)]],
      biography: ['', [Validators.maxLength(1000)]],
    });
  }

  private _patchForm(item: BookAuthorResponseDto): void {
    this.form.patchValue({
      schoolId:  item.tenantId  ?? '',
      name:      item.name      ?? '',
      biography: item.biography ?? '',
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
    this.isSaving = true;

    if (this.isEditMode) {
      const payload: UpdateBookAuthorDto = {
        name:      raw.name?.trim(),
        biography: raw.biography?.trim() || undefined,
      };
      this._service.update(this.data.item!.id, payload)
        .pipe(takeUntil(this._unsubscribe), finalize(() => { this.isSaving = false; this._cdr.detectChanges(); }))
        .subscribe({
          next: res => {
            if (res.success) {
              this._alert.success('Author updated successfully');
              this._dialogRef.close({ success: true, data: res.data });
            } else {
              this._alert.error(res.message || 'Failed to update author');
            }
          },
          error: err => this._alert.error(err?.error?.message || 'Failed to update author'),
        });
    } else {
      const payload: CreateBookAuthorDto = {
        tenantId:  this.isSuperAdmin ? raw.schoolId?.trim() : undefined,
        name:      raw.name?.trim(),
        biography: raw.biography?.trim() || undefined,
      };
      this._service.create(payload)
        .pipe(takeUntil(this._unsubscribe), finalize(() => { this.isSaving = false; this._cdr.detectChanges(); }))
        .subscribe({
          next: res => {
            if (res.success) {
              this._alert.success('Author created successfully');
              this._dialogRef.close({ success: true, data: res.data });
            } else {
              this._alert.error(res.message || 'Failed to create author');
            }
          },
          error: err => this._alert.error(err?.error?.message || 'Failed to create author'),
        });
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