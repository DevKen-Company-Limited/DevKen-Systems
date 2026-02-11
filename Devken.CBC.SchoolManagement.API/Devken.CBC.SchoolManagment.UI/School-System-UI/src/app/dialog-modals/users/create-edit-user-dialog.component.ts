import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { take, forkJoin } from 'rxjs';

import { API_BASE_URL } from 'app/app.config';
import { UserService } from 'app/core/DevKenService/user/UserService';
import { BaseFormDialog } from 'app/shared/dialogs/BaseFormDialog';
import { UserDto, CreateUserRequest, UpdateUserRequest, RoleDto } from 'app/core/DevKenService/Types/roles';
import { SchoolDto, ApiResponse } from 'app/Tenant/types/school';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';

interface DialogData {
  mode: 'create' | 'edit';
  userId?: string;
  user?: UserDto;
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
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatDividerModule
  ],
  templateUrl: './create-edit-user-dialog.component.html',
  styleUrls: ['./create-edit-user-dialog.component.scss']
})
export class CreateEditUserDialogComponent
  extends BaseFormDialog<CreateUserRequest, UpdateUserRequest, UserDto, DialogData>
  implements OnInit {

  availableRoles: RoleDto[] = [];
  availableSchools: SchoolDto[] = [];
  isLoadingRoles = false;
  isLoadingSchools = false;
  isLoadingUser = false;
  hidePassword = true;
  currentUser: UserDto | null = null;

  constructor(
    protected fb: FormBuilder,
    protected http: HttpClient,
    protected service: UserService,
    protected schoolService: SchoolService,
    protected snackBar: MatSnackBar,
    protected dialogRef: MatDialogRef<CreateEditUserDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: DialogData,
    @Inject(API_BASE_URL) private _apiBase: string
  ) {
    super(fb, service, snackBar, dialogRef, data);
  }

  ngOnInit(): void {
    // Build the form first
    this.form = this.buildForm();

    // Load data based on mode
    if (this.isEditMode) {
      this.loadUserAndRoles();
    } else {
      // Create mode
      this.loadRoles();
      if (this.isSuperAdmin) {
        this.loadSchools();
      }
    }
  }

  protected buildForm(): FormGroup {
    const isEdit = this.isEditMode;

    return this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: [''],
      password: [
        '',
        isEdit ? [] : [Validators.required, Validators.minLength(8)]
      ],
      roleIds: [[] as string[]],
      isActive: [true],
      sendWelcomeEmail: [true],
      schoolId: [
        '',
        this.isSuperAdmin && !isEdit ? [Validators.required] : []
      ]
    });
  }

  /**
   * Load user data and roles in parallel for edit mode
   */
  private loadUserAndRoles(): void {
    const userId = this.data.userId || this.data.user?.id;
    
    if (!userId) {
      this.snackBar.open('User ID is required for editing', 'Close', { duration: 3000 });
      this.close();
      return;
    }

    this.isLoadingUser = true;
    this.isLoadingRoles = true;

    // Load user data and available roles in parallel
    forkJoin({
      user: this.service.getById(userId),
      roles: this.loadRolesObservable()
    })
    .pipe(take(1))
    .subscribe({
      next: ({ user, roles }) => {
        this.isLoadingUser = false;
        this.isLoadingRoles = false;

        // Handle user response
        if (!user.success || !user.data) {
          this.snackBar.open(
            user.message || 'Failed to load user details',
            'Close',
            { duration: 3000 }
          );
          this.close();
          return;
        }

        // Handle roles response
        if (!roles.success || !roles.data) {
          this.snackBar.open(
            roles.message || 'Failed to load roles',
            'Close',
            { duration: 3000 }
          );
          // Don't close, allow editing without role changes
        } else {
          this.availableRoles = roles.data;
        }

        // Store current user and patch form
        this.currentUser = user.data;
        this.patchForEdit(this.currentUser);
      },
      error: (err) => {
        this.isLoadingUser = false;
        this.isLoadingRoles = false;
        
        const errorMsg = err?.error?.message || err.message || 'Failed to load user data';
        this.snackBar.open(errorMsg, 'Close', { duration: 4000 });
        this.close();
      }
    });
  }

  protected patchForEdit(user: UserDto): void {
    if (!user) return;

    // Extract roleIds - handle multiple possible response formats
    let roleIds: string[] = [];

    // Format 1: user.roles is array of objects with roleId property
    if (user.roles && Array.isArray(user.roles) && user.roles.length > 0) {
      if (typeof user.roles[0] === 'object' && 'roleId' in user.roles[0]) {
        roleIds = user.roles.map(r => r.roleId);
      }
    }

    // Format 2: user.roleNames is array of role names - need to match with availableRoles
    if (roleIds.length === 0 && (user as any).roleNames && Array.isArray((user as any).roleNames)) {
      const roleNames = (user as any).roleNames as string[];
      roleIds = this.availableRoles
        .filter(role => roleNames.includes(role.name))
        .map(role => role.id);
    }

    // Format 3: user.roleIds is already an array of IDs
    if (roleIds.length === 0 && (user as any).roleIds && Array.isArray((user as any).roleIds)) {
      roleIds = (user as any).roleIds;
    }

    console.log('Patching user with roleIds:', roleIds);

    this.form.patchValue({
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email,
      phoneNumber: user.phoneNumber || '',
      isActive: user.isActive,
      roleIds: roleIds
    });

    // Clear password validators for edit mode
    this.form.get('password')?.clearValidators();
    this.form.get('password')?.updateValueAndValidity();

    this.form.markAsPristine();
  }

  /**
   * Load roles - returns observable for use in forkJoin
   * Uses role-assignments endpoint which handles SuperAdmin vs School context
   */
  private loadRolesObservable() {
    return this.http.get<ApiResponse<RoleDto[]>>(
      `${this._apiBase}/api/role-assignments/available-roles`
    );
  }

  /**
   * Load roles for create mode
   * Uses role-assignments endpoint which handles SuperAdmin vs School context
   */
  private loadRoles(): void {
    this.isLoadingRoles = true;

    this.http.get<ApiResponse<RoleDto[]>>(
      `${this._apiBase}/api/role-assignments/available-roles`
    )
      .pipe(take(1))
      .subscribe({
        next: res => {
          this.isLoadingRoles = false;

          if (!res.success) {
            this.snackBar.open(
              res.message || 'Failed to load roles',
              'Close',
              { duration: 3000 }
            );
            return;
          }

          this.availableRoles = res.data;
        },
        error: err => {
          this.isLoadingRoles = false;
          this.snackBar.open(
            err?.error?.message || err.message || 'Failed to load roles',
            'Close',
            { duration: 4000 }
          );
        }
      });
  }

  private loadSchools(): void {
    this.isLoadingSchools = true;

    this.schoolService.getAll()
      .pipe(take(1))
      .subscribe({
        next: res => {
          this.isLoadingSchools = false;

          if (!res.success) {
            this.snackBar.open(
              res.message || 'Failed to load schools',
              'Close',
              { duration: 3000 }
            );
            return;
          }

          this.availableSchools = res.data;
        },
        error: err => {
          this.isLoadingSchools = false;
          this.snackBar.open(
            err?.error?.message || err.message || 'Failed to load schools',
            'Close',
            { duration: 4000 }
          );
        }
      });
  }

  onSave(): void {
    if (!this.currentUser && this.isEditMode) {
      this.snackBar.open('User data not loaded', 'Close', { duration: 3000 });
      return;
    }

    this.save(
      // CREATE
      raw => {
        const payload: CreateUserRequest = {
          firstName: raw.firstName.trim(),
          lastName: raw.lastName.trim(),
          email: raw.email.trim(),
          phoneNumber: raw.phoneNumber?.trim() || undefined,
          password: raw.password,
          roleIds: raw.roleIds ?? [],
          sendWelcomeEmail: raw.sendWelcomeEmail ?? false
        };

        // SuperAdmin must specify schoolId when creating users
        if (this.isSuperAdmin && raw.schoolId) {
          payload.schoolId = raw.schoolId;
        }

        return payload;
      },

      // UPDATE
      raw => ({
        firstName: raw.firstName.trim(),
        lastName: raw.lastName.trim(),
        email: raw.email.trim(),
        phoneNumber: raw.phoneNumber?.trim() || undefined,
        isActive: raw.isActive,
        roleIds: raw.roleIds ?? []
      }),

      () => this.currentUser!.id
    );
  }

  onCancel(): void {
    this.close();
  }

  get isEditMode(): boolean {
    return this.data.mode === 'edit';
  }

  get isSuperAdmin(): boolean {
    return !!this.data.isSuperAdmin;
  }

  get dialogTitle(): string {
    return this.isEditMode ? 'Edit User' : 'Create New User';
  }

  get showSchoolSelection(): boolean {
    return this.isSuperAdmin && !this.isEditMode;
  }

  getErrorMessage(fieldName: string): string {
    const control = this.form.get(fieldName);
    if (!control?.errors) return '';

    if (control.hasError('required')) return 'This field is required';
    if (control.hasError('email')) return 'Please enter a valid email address';
    if (control.hasError('minlength')) {
      return `Must be at least ${control.errors['minlength'].requiredLength} characters`;
    }

    return 'Invalid value';
  }
}