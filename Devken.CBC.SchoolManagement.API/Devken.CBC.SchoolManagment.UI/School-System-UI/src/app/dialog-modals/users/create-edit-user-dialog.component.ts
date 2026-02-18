import { Component, Inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Subject } from 'rxjs';
import { takeUntil, take } from 'rxjs/operators';
import { UserService } from 'app/core/DevKenService/user/UserService';

import { CreateUserRequest, UpdateUserRequest, UserDto, RoleDto } from 'app/core/DevKenService/Types/roles';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';

export interface SchoolOption {
  id: string;
  name: string;
  location?: string;
}

export interface UserDialogData {
  mode: 'create' | 'edit';
  userId?: string;
  isSuperAdmin?: boolean;
}

@Component({
  selector: 'app-create-edit-user-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatCheckboxModule,
    MatDividerModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './create-edit-user-dialog.component.html'
})
export class CreateEditUserDialogComponent implements OnInit, OnDestroy {

  form!: FormGroup;

  isEditMode       = false;
  isSuperAdmin     = false;
  isSaving         = false;
  hidePassword     = true;
  isLoadingRoles   = false;
  isLoadingSchools = false;

  dialogTitle    = '';
  availableRoles:   RoleDto[]     = [];
  availableSchools: SchoolOption[] = [];

  private _destroy = new Subject<void>();

  // ── Computed ───────────────────────────────────────────────────────────
  /** Only show the school picker when a SuperAdmin is creating a new user */
  get showSchoolSelection(): boolean {
    return this.isSuperAdmin && !this.isEditMode;
  }

  constructor(
    private _fb:        FormBuilder,
    private _userSvc:   UserService,
    private _schoolSvc: SchoolService,
    private _snackBar:  MatSnackBar,
    private _dialogRef: MatDialogRef<CreateEditUserDialogComponent>,
    @Inject(MAT_DIALOG_DATA) private _data: UserDialogData
  ) {}

  // ── Lifecycle ──────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.isEditMode   = this._data.mode === 'edit';
    this.isSuperAdmin = this._data.isSuperAdmin ?? false;
    this.dialogTitle  = this.isEditMode ? 'Edit User' : 'Create New User';

    this._buildForm();

    // When SuperAdmin picks a school, reload roles for that school
    if (this.showSchoolSelection) {
      this._loadSchools();

      this.form.get('schoolId')!.valueChanges
        .pipe(takeUntil(this._destroy))
        .subscribe(schoolId => {
          if (schoolId) this._loadRoles(schoolId);
        });
    } else {
      this._loadRoles();
    }

    if (this.isEditMode && this._data.userId) {
      this._loadUser(this._data.userId);
    }
  }

  ngOnDestroy(): void {
    this._destroy.next();
    this._destroy.complete();
  }

  // ── Form builder ───────────────────────────────────────────────────────
  private _buildForm(): void {
    this.form = this._fb.group({
      // School — required only when SuperAdmin is creating
      schoolId: [
        null,
        this.showSchoolSelection ? [Validators.required] : []
      ],

      // Personal info
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName:  ['', [Validators.required, Validators.minLength(2)]],

      // Contact
      email:       ['', [Validators.required, Validators.email]],
      phoneNumber: [''],

      // Security (create only)
      password: [
        '',
        !this.isEditMode
          ? [Validators.required, Validators.minLength(8)]
          : []
      ],

      // Roles
      roleIds: [[]],

      // Edit-only fields
      isActive: [true],

      // Create-only options
      sendWelcomeEmail: [true]
    });
  }

  // ── Data loaders ───────────────────────────────────────────────────────
  private _loadSchools(): void {
    this.isLoadingSchools = true;

    this._schoolSvc.getAll()
      .pipe(take(1), takeUntil(this._destroy))
      .subscribe({
        next: res => {
          this.isLoadingSchools = false;
          if (res.success && res.data) {
            this.availableSchools = res.data.map(s => ({
              id:       s.id,
              name:     s.name,
              location: s.county ?? s.address ?? undefined
            }));
          }
        },
        error: () => {
          this.isLoadingSchools = false;
          this._snackBar.open('Failed to load schools', 'Close', { duration: 3000 });
        }
      });
  }

  /**
   * Load roles scoped to a specific school, or load the current tenant's roles
   * when no schoolId is provided (regular non-SuperAdmin context).
   */
  private _loadRoles(schoolId?: string): void {
    this.isLoadingRoles = true;
    this.form.get('roleIds')?.setValue([]);   // clear stale role selection

    const roles$ = schoolId
      ? this._userSvc.getAvailableRolesBySchool(schoolId)
      : this._userSvc.getAvailableRoles();

    roles$
      .pipe(take(1), takeUntil(this._destroy))
      .subscribe({
        next: res => {
          this.isLoadingRoles = false;
          this.availableRoles = res.data ?? [];
        },
        error: () => {
          this.isLoadingRoles = false;
          this._snackBar.open('Failed to load roles', 'Close', { duration: 3000 });
        }
      });
  }

  private _loadUser(userId: string): void {
    this._userSvc.getById(userId)
      .pipe(take(1), takeUntil(this._destroy))
      .subscribe({
        next: res => {
          if (!res.success || !res.data) return;
          const user = res.data;

          this.form.patchValue({
            firstName:   user.firstName,
            lastName:    user.lastName,
            email:       user.email,
            phoneNumber: user.phoneNumber ?? '',
            isActive:    user.isActive,
            // Map role objects or role name strings → ids
            roleIds: user.roles?.map((r: any) => r.id ?? r) ?? []
          });

          // Email must not change in edit mode
          this.form.get('email')?.disable();
        },
        error: () => {
          this._snackBar.open('Failed to load user data', 'Close', { duration: 3000 });
        }
      });
  }

  // ── Actions ────────────────────────────────────────────────────────────
  onSave(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isEditMode ? this._update() : this._create();
  }

  private _create(): void {
    this.isSaving = true;
    const v = this.form.getRawValue();

    const payload: CreateUserRequest = {
      firstName:        v.firstName.trim(),
      lastName:         v.lastName.trim(),
      email:            v.email.trim().toLowerCase(),
      phoneNumber:      v.phoneNumber?.trim() || undefined,
      password:         v.password,
      roleIds:          v.roleIds ?? [],
      sendWelcomeEmail: v.sendWelcomeEmail,
      // ✅ Key fix: always include schoolId when SuperAdmin picks a school.
      // For regular users this will be undefined and the backend will use
      // the calling user's own TenantId instead.
      schoolId: this.showSchoolSelection ? v.schoolId : undefined
    };

    this._userSvc.create(payload)
      .pipe(take(1))
      .subscribe({
        next: res => {
          this.isSaving = false;
          if (res.success) {
            this._snackBar.open('User created successfully', 'Close', { duration: 2500 });
            this._dialogRef.close(true);
          } else {
            this._snackBar.open(res.message || 'Failed to create user', 'Close', { duration: 4000 });
          }
        },
        error: err => {
          this.isSaving = false;
          const msg = err?.error?.message || err.message || 'Failed to create user';
          this._snackBar.open(msg, 'Close', { duration: 4000 });
        }
      });
  }

  private _update(): void {
    this.isSaving = true;
    const v = this.form.getRawValue();

    const payload: UpdateUserRequest = {
      firstName:   v.firstName.trim(),
      lastName:    v.lastName.trim(),
      phoneNumber: v.phoneNumber?.trim() || undefined,
      roleIds:     v.roleIds ?? [],
      isActive:    v.isActive
      // schoolId deliberately omitted — school cannot change on edit
    };

    this._userSvc.update(this._data.userId!, payload)
      .pipe(take(1))
      .subscribe({
        next: res => {
          this.isSaving = false;
          if (res.success) {
            this._snackBar.open('User updated successfully', 'Close', { duration: 2500 });
            this._dialogRef.close(true);
          } else {
            this._snackBar.open(res.message || 'Failed to update user', 'Close', { duration: 4000 });
          }
        },
        error: err => {
          this.isSaving = false;
          const msg = err?.error?.message || err.message || 'Failed to update user';
          this._snackBar.open(msg, 'Close', { duration: 4000 });
        }
      });
  }

  onCancel(): void {
    this._dialogRef.close(false);
  }

  // ── Validation helpers ─────────────────────────────────────────────────
  getErrorMessage(field: string): string {
    const ctrl = this.form.get(field);
    if (!ctrl?.errors) return '';

    if (ctrl.hasError('required'))  return 'This field is required';
    if (ctrl.hasError('email'))     return 'Enter a valid email address';
    if (ctrl.hasError('minlength')) {
      const min = ctrl.errors['minlength'].requiredLength;
      return `Minimum ${min} characters required`;
    }
    return 'Invalid value';
  }
}