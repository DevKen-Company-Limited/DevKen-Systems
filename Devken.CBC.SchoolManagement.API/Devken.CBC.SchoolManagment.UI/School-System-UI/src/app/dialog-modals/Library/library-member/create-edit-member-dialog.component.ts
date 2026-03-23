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
import { SchoolDto } from 'app/Tenant/types/school';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';


import {
  LibraryMemberDto,
  LibraryMemberType,
  CreateLibraryMemberRequest,
  UpdateLibraryMemberRequest,
} from 'app/Library/library-member/Types/library-member.types';
import { UserDto } from 'app/core/DevKenService/Types/roles';
import { UserService } from 'app/core/DevKenService/user/UserService';
import { LibraryMemberService } from 'app/core/DevKenService/Library/library-member.service';

export interface CreateEditMemberDialogData {
  mode: 'create' | 'edit';
  member?: LibraryMemberDto;
}

@Component({
  selector: 'app-create-edit-member-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
    MatTooltipModule, MatDatepickerModule, MatNativeDateModule,
  ],
  templateUrl: './create-edit-member-dialog.component.html',
  styles: [`
    :host ::ng-deep .mat-mdc-dialog-container { --mdc-dialog-container-shape: 12px; }
  `],
})
export class CreateEditMemberDialogComponent implements OnInit, OnDestroy {

  private readonly _unsubscribe = new Subject<void>();

  // ── Dropdown data ──────────────────────────────────────────────────────────
  schools: SchoolDto[] = [];
  users:   UserDto[]   = [];
  isLoading     = true;
  formSubmitted = false;

  readonly memberTypes: { label: string; value: LibraryMemberType; icon: string }[] = [
    { label: 'Student', value: 'Student', icon: 'school'      },
    { label: 'Teacher', value: 'Teacher', icon: 'person'       },
    { label: 'Staff',   value: 'Staff',   icon: 'badge'        },
    { label: 'Other',   value: 'Other',   icon: 'person_outline'},
  ];

  readonly maxDate = new Date();

  // ── Form ───────────────────────────────────────────────────────────────────
  form!: FormGroup;

  // ── Getters ────────────────────────────────────────────────────────────────
  get isEditMode(): boolean    { return this.data.mode === 'edit'; }
  get isSuperAdmin(): boolean  { return this._authService.authUser?.isSuperAdmin ?? false; }
  get isSaving(): boolean      { return this.isLoading; }
  get dialogTitle(): string    { return this.isEditMode ? 'Edit Member' : 'Add Library Member'; }
  get dialogSubtitle(): string {
    return this.isEditMode
      ? `Editing member "${this.data.member?.memberNumber || ''}"`
      : 'Register a user as a library member';
  }

  get filteredUsers(): UserDto[] {
    if (!this.isSuperAdmin) return this.users;
    const schoolId = this.form.get('schoolId')?.value;
    return schoolId ? this.users.filter(u => u.schoolId === schoolId) : [];
  }

  constructor(
    private readonly _fb:            FormBuilder,
    private readonly _dialogRef:     MatDialogRef<CreateEditMemberDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: CreateEditMemberDialogData,
    private readonly _authService:   AuthService,
    private readonly _schoolService: SchoolService,
    private readonly _userService:   UserService,
    private readonly _memberService: LibraryMemberService,
    private readonly _alertService:  AlertService,
    private readonly _cdr:           ChangeDetectorRef,
  ) {
    _dialogRef.addPanelClass(['member-dialog', 'responsive-dialog']);
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
      schoolId:     [null,       this.isSuperAdmin ? [Validators.required] : []],
      userId:       [null,       [Validators.required]],
      // Required only in edit mode — generated by the server on create
      memberNumber: ['',        this.isEditMode
                                ? [Validators.required, Validators.maxLength(50)]
                                : [Validators.maxLength(50)]],memberType:   ['Student',  [Validators.required]],
      joinedOn:     [new Date()],
      isActive:     [true],
    });

    // userId is read-only in edit mode
    if (this.isEditMode) {
      this.form.get('userId')?.disable();
      if (this.data.member) this._patchForm(this.data.member);
    }
  }

  private _patchForm(m: LibraryMemberDto): void {
    this.form.patchValue({
      schoolId:     m.schoolId     || null,
      userId:       m.userId       || null,
      memberNumber: m.memberNumber || '',
      memberType:   m.memberType   || 'Student',
      joinedOn:     m.joinedOn     ? new Date(m.joinedOn) : new Date(),
      isActive:     m.isActive     ?? true,
    });
    this._cdr.detectChanges();
  }

  // ── Data loading ───────────────────────────────────────────────────────────
  private _loadDropdowns(): void {
    this.isLoading = true;

    const requests: any = {
      users: this._userService.getAll().pipe(catchError(() => of({ success: false, data: [] }))),
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
        this.users = res.users?.data || [];
        if (res.schools) this.schools = res.schools?.data || [];

        if (this.isEditMode && this.data.member && !this.form.get('memberNumber')?.value) {
          this._patchForm(this.data.member);
        }
      },
      error: () => {
        if (this.isEditMode && this.data.member) this._patchForm(this.data.member);
      },
    });

    setTimeout(() => {
      if (this.isLoading) { this.isLoading = false; this._cdr.detectChanges(); }
    }, 12000);
  }

  // ── Submit & Cancel ────────────────────────────────────────────────────────
  onSubmit(): void {
    this.formSubmitted = true;

    if (this.form.invalid) {
      Object.keys(this.form.controls).forEach(k => this.form.get(k)?.markAsTouched());
      return;
    }

    this.isLoading = true;
    this.isEditMode ? this._updateMember() : this._createMember();
  }

    private _createMember(): void {
    const raw = this.form.value;

    const request: CreateLibraryMemberRequest = {
      ...(this.isSuperAdmin ? { schoolId: raw.schoolId } : {}),
      userId:     raw.userId,
      memberType: raw.memberType,
      joinedOn:   raw.joinedOn ? (raw.joinedOn as Date).toISOString() : undefined,
      // isActive:   raw.isActive ?? true,
      // Send memberNumber only if the user typed one — otherwise server auto-generates
    ...(raw.memberNumber?.trim() ? { memberNumber: raw.memberNumber.trim() } : {}),
    };

    this._memberService.create(request).pipe(
      takeUntil(this._unsubscribe),
      finalize(() => { this.isLoading = false; this._cdr.detectChanges(); })
    ).subscribe({
      next: res => {
        if (res.success) {
          this._alertService.success(
            `Library member ${res.data.memberNumber} created successfully`);
          this._dialogRef.close({ success: true, data: res.data });
        } else {
          this._alertService.error(res.message || 'Failed to add member');
        }
      },
      error: err => this._alertService.error(err?.error?.message || 'Failed to add member'),
    });
  }
  // private _createMember(): void {
  //   const raw = this.form.value;
  //   const request: CreateLibraryMemberRequest = {
  //     ...(this.isSuperAdmin ? { schoolId: raw.schoolId } : {}),
  //     userId:       raw.userId,
  //     //  // memberNumber intentionally omitted → auto-generated by server
  //     // memberNumber: raw.memberNumber?.trim(),
  //     memberType:   raw.memberType,
  //     joinedOn:     raw.joinedOn ? (raw.joinedOn as Date).toISOString() : undefined,
  //   };

  //   this._memberService.create(request).pipe(
  //     takeUntil(this._unsubscribe),
  //     finalize(() => { this.isLoading = false; this._cdr.detectChanges(); })
  //   ).subscribe({
  //     next: res => {
  //       if (res.success) {
  //         this._alertService.success('Library member added successfully');
  //         this._dialogRef.close({ success: true, data: res.data });
  //       } else {
  //         this._alertService.error(res.message || 'Failed to add member');
  //       }
  //     },
  //     error: err => this._alertService.error(err?.error?.message || 'Failed to add member'),
  //   });
  // }

  private _updateMember(): void {
    if (!this.data.member?.id) {
      this._alertService.error('Member ID is missing');
      this.isLoading = false;
      return;
    }

    const raw = this.form.getRawValue(); // include disabled userId
    const request: UpdateLibraryMemberRequest = {
      memberNumber: raw.memberNumber?.trim(),
      memberType:   raw.memberType,
      isActive:     raw.isActive,
    };

    this._memberService.update(this.data.member.id, request).pipe(
      takeUntil(this._unsubscribe),
      finalize(() => { this.isLoading = false; this._cdr.detectChanges(); })
    ).subscribe({
      next: res => {
        if (res.success) {
          this._alertService.success('Member updated successfully');
          this._dialogRef.close({ success: true, data: res.data });
        } else {
          this._alertService.error(res.message || 'Failed to update member');
        }
      },
      error: err => this._alertService.error(err?.error?.message || 'Failed to update member'),
    });
  }

  onCancel(): void {
    this._dialogRef.close({ success: false });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  getFieldError(field: string): string {
    const c = this.form.get(field);
    if (!c || !(this.formSubmitted || c.touched)) return '';
    if (c.hasError('required'))   return 'This field is required';
    if (c.hasError('maxlength'))  return `Maximum ${c.getError('maxlength').requiredLength} characters`;
    return 'Invalid value';
  }
}