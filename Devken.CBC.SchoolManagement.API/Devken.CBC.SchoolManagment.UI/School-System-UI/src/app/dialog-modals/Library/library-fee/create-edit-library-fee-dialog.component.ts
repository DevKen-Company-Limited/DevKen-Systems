// app/dialog-modals/Library/library-fee/create-edit-library-fee-dialog.component.ts
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
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { Subject, forkJoin, of } from 'rxjs';
import { catchError, takeUntil, finalize } from 'rxjs/operators';

import { AuthService } from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { LibraryFeeService } from 'app/core/DevKenService/Library/library-fee.service';
import { LibraryMemberService } from 'app/core/DevKenService/Library/library-member.service';
import { SchoolDto } from 'app/Tenant/types/school';
import {
  LibraryFeeDto,
  LibraryFeeType,
  CreateLibraryFeeRequest,
  UpdateLibraryFeeRequest,
} from 'app/Library/library-fee/Types/library-fee.types';
import { LibraryMemberDto } from 'app/Library/library-member/Types/library-member.types';

export interface CreateEditLibraryFeeDialogData {
  mode:    'create' | 'edit';
  fee?:    LibraryFeeDto;
  /** Pre-select a member when opening from member detail page */
  memberId?: string;
}

@Component({
  selector: 'app-create-edit-library-fee-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
    MatTooltipModule, MatDatepickerModule, MatNativeDateModule,
  ],
  templateUrl: './create-edit-library-fee-dialog.component.html',
  styles: [`
    :host ::ng-deep .mat-mdc-dialog-container { --mdc-dialog-container-shape: 12px; }
  `],
})
export class CreateEditLibraryFeeDialogComponent implements OnInit, OnDestroy {

  private readonly _unsubscribe = new Subject<void>();

  // ── Dropdown data ──────────────────────────────────────────────────────────
  schools: SchoolDto[]          = [];
  members: LibraryMemberDto[]   = [];
  isLoading     = true;
  formSubmitted = false;

  readonly feeTypes = [
    { value: LibraryFeeType.MembershipFee, label: 'Membership Fee',  icon: 'card_membership' },
    { value: LibraryFeeType.LateFine,      label: 'Late Fine',        icon: 'schedule'        },
    { value: LibraryFeeType.DamageFee,     label: 'Damage Fee',       icon: 'broken_image'    },
    { value: LibraryFeeType.LostBookFee,   label: 'Lost Book Fee',    icon: 'search_off'      },
    { value: LibraryFeeType.ProcessingFee, label: 'Processing Fee',   icon: 'receipt_long'    },
    { value: LibraryFeeType.Other,         label: 'Other',            icon: 'more_horiz'      },
  ];

  // ── Form ───────────────────────────────────────────────────────────────────
  form!: FormGroup;

  // ── Getters ────────────────────────────────────────────────────────────────
  get isEditMode(): boolean   { return this.data.mode === 'edit'; }
  get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }
  get isSaving(): boolean     { return this.isLoading && this.formSubmitted; }
  get dialogTitle(): string   { return this.isEditMode ? 'Edit Library Fee' : 'Add Library Fee'; }
  get dialogSubtitle(): string {
    return this.isEditMode
      ? `Editing fee for member "${this.data.fee?.memberNumber || ''}"`
      : 'Create a new fee for a library member';
  }
  get descriptionLength(): number {
    return this.form.get('description')?.value?.length || 0;
  }
  get filteredMembers(): LibraryMemberDto[] {
    if (!this.isSuperAdmin) return this.members;
    const schoolId = this.form.get('schoolId')?.value;
    return schoolId
      ? this.members.filter(m => m.schoolId === schoolId)
      : [];
  }

  constructor(
    private readonly _fb:           FormBuilder,
    private readonly _dialogRef:    MatDialogRef<CreateEditLibraryFeeDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: CreateEditLibraryFeeDialogData,
    private readonly _authService:  AuthService,
    private readonly _schoolService: SchoolService,
    private readonly _memberService: LibraryMemberService,
    private readonly _feeService:   LibraryFeeService,
    private readonly _alertService: AlertService,
    private readonly _cdr:          ChangeDetectorRef,
  ) {
    _dialogRef.addPanelClass(['fee-dialog', 'responsive-dialog']);
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

  // ── Form ───────────────────────────────────────────────────────────────────
  private _buildForm(): void {
    this.form = this._fb.group({
      schoolId:    [null, this.isSuperAdmin ? [Validators.required] : []],
      memberId:    [this.data.memberId ?? null, [Validators.required]],
      feeType:     [LibraryFeeType.LateFine,    [Validators.required]],
      amount:      [null, [Validators.required, Validators.min(0.01)]],
      description: ['',  [Validators.maxLength(500)]],
      feeDate:     [new Date(), [Validators.required]],
    });

    if (this.isEditMode && this.data.fee) {
      this._patchForm(this.data.fee);
    }
  }

  private _patchForm(fee: LibraryFeeDto): void {
    this.form.patchValue({
      schoolId:    fee.schoolId   || null,
      memberId:    fee.memberId   || null,
      feeType:     fee.feeType,
      amount:      fee.amount,
      description: fee.description || '',
      feeDate:     fee.feeDate ? new Date(fee.feeDate) : new Date(),
    });
    this._cdr.detectChanges();
  }

  // ── Data loading ───────────────────────────────────────────────────────────
  private _loadDropdowns(): void {
    this.isLoading = true;

    const requests: any = {
      members: this._memberService.getAll().pipe(
        catchError(() => of({ success: false, data: [] }))
      ),
    };

    if (this.isSuperAdmin) {
      requests.schools = this._schoolService.getAll().pipe(
        catchError(() => of({ success: false, data: [] }))
      );
    }

    forkJoin(requests).pipe(
      takeUntil(this._unsubscribe),
      finalize(() => { this.isLoading = false; this._cdr.detectChanges(); }),
    ).subscribe({
      next: (res: any) => {
        this.members = res.members?.data ?? [];
        if (res.schools) this.schools = res.schools?.data ?? [];
        if (this.isEditMode && this.data.fee) this._patchForm(this.data.fee);
      },
      error: () => {
        if (this.isEditMode && this.data.fee) this._patchForm(this.data.fee);
      },
    });

    setTimeout(() => {
      if (this.isLoading) { this.isLoading = false; this._cdr.detectChanges(); }
    }, 12000);
  }

  // ── Submit ─────────────────────────────────────────────────────────────────
  onSubmit(): void {
    this.formSubmitted = true;

    if (this.form.invalid) {
      Object.keys(this.form.controls).forEach(k => this.form.get(k)?.markAsTouched());
      return;
    }

    this.isLoading = true;
    this.isEditMode ? this._update() : this._create();
  }

  private _create(): void {
    const raw = this.form.value;
    const payload: CreateLibraryFeeRequest = {
      ...(this.isSuperAdmin ? { schoolId: raw.schoolId } : {}),
      memberId:    raw.memberId,
      feeType:     raw.feeType,
      amount:      +raw.amount,
      description: raw.description?.trim() || '',
      feeDate:     raw.feeDate ? new Date(raw.feeDate).toISOString() : undefined,
    };

    this._feeService.create(payload).pipe(
      takeUntil(this._unsubscribe),
      finalize(() => { this.isLoading = false; this._cdr.detectChanges(); }),
    ).subscribe({
      next: res => {
        if (res.success) {
          this._alertService.success('Library fee created successfully');
          this._dialogRef.close({ success: true, data: res.data });
        } else {
          this._alertService.error(res.message || 'Failed to create fee');
        }
      },
      error: err => this._alertService.error(err?.error?.message || 'Failed to create fee'),
    });
  }

  private _update(): void {
    if (!this.data.fee?.id) {
      this._alertService.error('Fee ID is missing');
      this.isLoading = false;
      return;
    }
    const raw = this.form.value;
    const payload: UpdateLibraryFeeRequest = {
      feeType:     raw.feeType,
      amount:      +raw.amount,
      description: raw.description?.trim() || '',
      feeDate:     raw.feeDate ? new Date(raw.feeDate).toISOString() : undefined,
    };

    this._feeService.update(this.data.fee.id, payload).pipe(
      takeUntil(this._unsubscribe),
      finalize(() => { this.isLoading = false; this._cdr.detectChanges(); }),
    ).subscribe({
      next: res => {
        if (res.success) {
          this._alertService.success('Library fee updated successfully');
          this._dialogRef.close({ success: true, data: res.data });
        } else {
          this._alertService.error(res.message || 'Failed to update fee');
        }
      },
      error: err => this._alertService.error(err?.error?.message || 'Failed to update fee'),
    });
  }

  onCancel(): void { this._dialogRef.close({ success: false }); }

  // ── Helpers ────────────────────────────────────────────────────────────────
  getFieldError(field: string): string {
    const c = this.form.get(field);
    if (!c || !(this.formSubmitted || c.touched)) return '';
    if (c.hasError('required'))  return 'This field is required';
    if (c.hasError('min'))       return `Minimum value is ${c.getError('min').min}`;
    if (c.hasError('maxlength')) return `Maximum ${c.getError('maxlength').requiredLength} characters`;
    return 'Invalid value';
  }
}