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
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { BookService } from 'app/core/DevKenService/Library/book.service';
import { BookDto } from 'app/Library/book/Types/book.types';
import { BookReservationService } from 'app/core/DevKenService/Library/book-reservation.service';
import { BookReservationDto, CreateBookReservationRequest, UpdateBookReservationRequest } from 'app/Library/book-reservation/Types/book-reservation.types';
import { LibraryMemberDto } from 'app/Library/library-member/Types/library-member.types';
import { LibraryMemberService } from 'app/core/DevKenService/Library/library-member.service';


export interface CreateEditReservationDialogData {
  mode: 'create' | 'edit';
  reservation?: BookReservationDto;
}

@Component({
  selector: 'app-create-edit-reservation-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './create-edit-reservation-dialog.component.html',
  styles: [`
    :host ::ng-deep .mat-mdc-dialog-container { --mdc-dialog-container-shape: 12px; }
  `],
})
export class CreateEditReservationDialogComponent implements OnInit, OnDestroy {

  private readonly _unsubscribe = new Subject<void>();

  // ── Dropdown data ──────────────────────────────────────────────────────────
  schools: SchoolDto[]         = [];
  books:   BookDto[]           = [];
  members: LibraryMemberDto[]  = [];
  isLoading     = true;
  formSubmitted = false;

  // ── Form ───────────────────────────────────────────────────────────────────
  form!: FormGroup;

  // ── Getters ────────────────────────────────────────────────────────────────
  get isEditMode(): boolean    { return this.data.mode === 'edit'; }
  get isSuperAdmin(): boolean  { return this._authService.authUser?.isSuperAdmin ?? false; }
  get isSaving(): boolean      { return this.isLoading; }
  get dialogTitle(): string    { return this.isEditMode ? 'Edit Reservation' : 'New Reservation'; }
  get dialogSubtitle(): string {
    return this.isEditMode
      ? `Updating reservation for "${this.data.reservation?.bookTitle || 'book'}"`
      : 'Reserve a book for a library member';
  }

  get filteredBooks(): BookDto[] {
    if (!this.isSuperAdmin) return this.books;
    const schoolId = this.form.get('schoolId')?.value;
    return schoolId ? this.books.filter(b => b.schoolId === schoolId) : [];
  }

  get filteredMembers(): LibraryMemberDto[] {
    if (!this.isSuperAdmin) return this.members;
    const schoolId = this.form.get('schoolId')?.value;
    return schoolId ? this.members.filter(m => m.schoolId === schoolId) : [];
  }

  constructor(
    private readonly _fb:                 FormBuilder,
    private readonly _dialogRef:          MatDialogRef<CreateEditReservationDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: CreateEditReservationDialogData,
    private readonly _authService:        AuthService,
    private readonly _schoolService:      SchoolService,
    private readonly _bookService:        BookService,
    private readonly _memberService:      LibraryMemberService,
    private readonly _reservationService: BookReservationService,
    private readonly _alertService:       AlertService,
    private readonly _cdr:                ChangeDetectorRef,
  ) {
    _dialogRef.addPanelClass(['reservation-dialog', 'responsive-dialog']);
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
      schoolId:    [null, this.isSuperAdmin ? [Validators.required] : []],
      bookId:      [null, [Validators.required]],
      memberId:    [null, [Validators.required]],
      isFulfilled: [false],
    });

    if (this.isEditMode && this.data.reservation) {
      this._patchForm(this.data.reservation);
    }
  }

  private _patchForm(r: BookReservationDto): void {
    this.form.patchValue({
      schoolId:    r.schoolId    || null,
      bookId:      r.bookId      || null,
      memberId:    r.memberId    || null,
      isFulfilled: r.isFulfilled || false,
    });
    this._cdr.detectChanges();
  }

  // ── Data loading ───────────────────────────────────────────────────────────
  private _loadDropdowns(): void {
    this.isLoading = true;

    const requests: any = {
      books:   this._bookService.getAll().pipe(catchError(() => of({ success: false, data: [] }))),
      members: this._memberService.getAll().pipe(catchError(() => of({ success: false, data: [] }))),
    };

    if (this.isSuperAdmin) {
      requests.schools = this._schoolService.getAll().pipe(
        catchError(() => of({ success: false, data: [] }))
      );
    }

    forkJoin(requests).pipe(
      takeUntil(this._unsubscribe),
      finalize(() => { this.isLoading = false; this._cdr.detectChanges(); })
    ).subscribe({
      next: (res: any) => {
        this.books   = res.books?.data   || [];
        this.members = res.members?.data || [];
        if (res.schools) this.schools = res.schools?.data || [];

        if (this.isEditMode && this.data.reservation && !this.form.get('bookId')?.value) {
          this._patchForm(this.data.reservation);
        }
      },
      error: () => {
        if (this.isEditMode && this.data.reservation) {
          this._patchForm(this.data.reservation);
        }
      },
    });

    // Safety timeout
    setTimeout(() => {
      if (this.isLoading) { this.isLoading = false; this._cdr.detectChanges(); }
    }, 12000);
  }

  // ── Submit & Cancel ────────────────────────────────────────────────────────
  onSubmit(): void {
    this.formSubmitted = true;

    if (this.form.invalid) {
      Object.keys(this.form.controls).forEach(key => {
        this.form.get(key)?.markAsTouched();
      });
      return;
    }

    this.isLoading = true;
    this.isEditMode ? this._updateReservation() : this._createReservation();
  }

  private _createReservation(): void {
    const raw = this.form.value;
    const request: CreateBookReservationRequest = {
      ...(this.isSuperAdmin ? { schoolId: raw.schoolId } : {}),
      bookId:   raw.bookId,
      memberId: raw.memberId,
    };

    this._reservationService.create(request).pipe(
      takeUntil(this._unsubscribe),
      finalize(() => { this.isLoading = false; this._cdr.detectChanges(); })
    ).subscribe({
      next: res => {
        if (res.success) {
          this._alertService.success('Reservation created successfully');
          this._dialogRef.close({ success: true, data: res.data });
        } else {
          this._alertService.error(res.message || 'Failed to create reservation');
        }
      },
      error: err => this._alertService.error(err?.error?.message || 'Failed to create reservation'),
    });
  }

  private _updateReservation(): void {
    if (!this.data.reservation?.id) {
      this._alertService.error('Reservation ID is missing');
      this.isLoading = false;
      return;
    }

    const raw = this.form.value;
    const request: UpdateBookReservationRequest = {
      bookId:      raw.bookId,
      memberId:    raw.memberId,
      isFulfilled: raw.isFulfilled,
    };

    this._reservationService.update(this.data.reservation.id, request).pipe(
      takeUntil(this._unsubscribe),
      finalize(() => { this.isLoading = false; this._cdr.detectChanges(); })
    ).subscribe({
      next: res => {
        if (res.success) {
          this._alertService.success('Reservation updated successfully');
          this._dialogRef.close({ success: true, data: res.data });
        } else {
          this._alertService.error(res.message || 'Failed to update reservation');
        }
      },
      error: err => this._alertService.error(err?.error?.message || 'Failed to update reservation'),
    });
  }

  onCancel(): void {
    this._dialogRef.close({ success: false });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  getFieldError(field: string): string {
    const c = this.form.get(field);
    if (!c || !(this.formSubmitted || c.touched)) return '';
    if (c.hasError('required')) return 'This field is required';
    return 'Invalid value';
  }
}