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
  templateUrl: './create-edit-user-dialog.component.html',
  styleUrl: './create-edit-user-dialog.component.scss'
})
export class CreateEditUserDialogComponent implements OnInit, OnDestroy {

  form!: FormGroup;

  isEditMode       = false;
  isSuperAdmin     = false;
  isSaving         = false;
  hidePassword     = true;
  isLoadingRoles   = false;
  isLoadingSchools = false;

  dialogTitle      = '';
  availableRoles:   RoleDto[]      = [];
  availableSchools: SchoolOption[] = [];

  private _destroy = new Subject<void>();

  /** Show the school picker only when SuperAdmin is creating (not editing) */
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

    if (this.showSchoolSelection) {
      // 1. Load the school list first
      this._loadSchools();

      // 2. Whenever the admin picks a school, reload roles scoped to it
      this.form.get('schoolId')!.valueChanges
        .pipe(takeUntil(this._destroy))
        .subscribe(schoolId => {
          if (schoolId) {
            this._loadRoles(schoolId);
          } else {
            // School cleared — reset roles
            this.availableRoles = [];
            this.form.get('roleIds')?.setValue([]);
          }
        });
    } else {
      // Regular user or edit mode — load roles for their own school immediately
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

  // ── Form ───────────────────────────────────────────────────────────────

  private _buildForm(): void {
    this.form = this._fb.group({
      // Required for SuperAdmin create only
      schoolId: [
        null,
        this.showSchoolSelection ? [Validators.required] : []
      ],

      firstName:   ['', [Validators.required, Validators.minLength(2)]],
      lastName:    ['', [Validators.required, Validators.minLength(2)]],
      email:       ['', [Validators.required, Validators.email]],
      phoneNumber: [''],


      roleIds:          [[]],
      isActive:         [true],
      sendWelcomeEmail: [true]
    });
  }

  // ── Loaders ────────────────────────────────────────────────────────────

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
          } else {
            this._snackBar.open(
              res.message || 'Failed to load schools', 'Close', { duration: 3000 });
          }
        },
        error: err => {
          this.isLoadingSchools = false;
          const msg = err?.error?.message || 'Failed to load schools';
          this._snackBar.open(msg, 'Close', { duration: 3000 });
        }
      });
  }

  /**
   * Load roles from the backend.
   *
   * • SuperAdmin creating a user → pass the chosen `schoolId` so only
   *   that school's roles appear.
   * • Regular user (or edit mode) → call without schoolId; backend returns
   *   the caller's own school's roles automatically.
   *
   * Both paths hit GET /api/user-management/available-roles[?schoolId=...]
   */
  private _loadRoles(schoolId?: string): void {
    this.isLoadingRoles = true;
    this.form.get('roleIds')?.setValue([]);   // clear stale selection

    const roles$ = schoolId
      ? this._userSvc.getAvailableRolesBySchool(schoolId)
      : this._userSvc.getAvailableRoles();

    roles$
      .pipe(take(1), takeUntil(this._destroy))
      .subscribe({
        next: res => {
          this.isLoadingRoles = false;

          if (res.success && res.data) {
            this.availableRoles = res.data;

            if (this.availableRoles.length === 0) {
              this._snackBar.open(
                'No roles found for this school. Please create roles first.',
                'Close', { duration: 4000 });
            }
          } else {
            // Backend returned success:false
            this.availableRoles = [];
            this._snackBar.open(
              res.message || 'Failed to load roles', 'Close', { duration: 3000 });
          }
        },
        error: err => {
          this.isLoadingRoles = false;
          this.availableRoles = [];
          const msg = err?.error?.message || err?.message || 'Failed to load roles';
          this._snackBar.open(msg, 'Close', { duration: 3000 });
          console.error('[UserDialog] roles load error:', err);
        }
      });
  }

  private _loadUser(userId: string): void {
    this._userSvc.getById(userId)
      .pipe(take(1), takeUntil(this._destroy))
      .subscribe({
        next: res => {
          if (!res.success || !res.data) {
            this._snackBar.open('Failed to load user data', 'Close', { duration: 3000 });
            return;
          }

          const user = res.data;

          this.form.patchValue({
            firstName:   user.firstName,
            lastName:    user.lastName,
            email:       user.email,
            phoneNumber: user.phoneNumber ?? '',
            isActive:    user.isActive,
            // Support both roles[] (objects) and roleNames[] (strings)
            roleIds:     user.roles?.map((r: any) => r.id ?? r) ?? []
          });

          // Email is read-only in edit mode
          this.form.get('email')?.disable();
        },
        error: err => {
          const msg = err?.error?.message || 'Failed to load user data';
          this._snackBar.open(msg, 'Close', { duration: 3000 });
        }
      });
  }

  // ── Save ───────────────────────────────────────────────────────────────

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
      roleIds:          v.roleIds ?? [],
      sendWelcomeEmail: v.sendWelcomeEmail,
      // Only set schoolId when SuperAdmin is creating — backend ignores it otherwise
      schoolId:         this.showSchoolSelection ? v.schoolId : undefined
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
            this._snackBar.open(
              res.message || 'Failed to create user', 'Close', { duration: 4000 });
          }
        },
        error: err => {
          this.isSaving = false;
          const msg = err?.error?.message || err?.message || 'Failed to create user';
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
      // schoolId deliberately omitted — school cannot change after creation
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
            this._snackBar.open(
              res.message || 'Failed to update user', 'Close', { duration: 4000 });
          }
        },
        error: err => {
          this.isSaving = false;
          const msg = err?.error?.message || err?.message || 'Failed to update user';
          this._snackBar.open(msg, 'Close', { duration: 4000 });
        }
      });
  }

  onCancel(): void {
    this._dialogRef.close(false);
  }

  // ── Validation ─────────────────────────────────────────────────────────

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