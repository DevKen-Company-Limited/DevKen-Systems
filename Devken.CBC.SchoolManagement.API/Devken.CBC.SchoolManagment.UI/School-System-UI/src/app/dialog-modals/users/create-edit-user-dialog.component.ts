import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
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
import { take } from 'rxjs/operators';

import { UserService } from 'app/core/DevKenService/user/UserService';
import { BaseFormDialog } from 'app/shared/dialogs/BaseFormDialog';
import { UserDto, CreateUserRequest, UpdateUserRequest, RoleDto } from 'app/core/DevKenService/Types/roles';
import { SchoolDto } from 'app/Tenant/types/school';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';

interface DialogData {
  mode: 'create' | 'edit';
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
  hidePassword = true;

  constructor(
    protected fb: FormBuilder,
    protected service: UserService,
    protected schoolService: SchoolService,
    protected snackBar: MatSnackBar,
    protected dialogRef: MatDialogRef<CreateEditUserDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: DialogData
  ) {
    super(fb, service, snackBar, dialogRef, data);
  }

  ngOnInit(): void {
    this.loadRoles();

    if (this.isSuperAdmin && !this.isEditMode) {
      this.loadSchools();
    }

    this.init();
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

  protected patchForEdit(user: UserDto): void {
    if (!user) return;

    // Extract roleIds from user.roles array
    const roleIds = user.roles?.map(r => r.roleId) ?? [];

    this.form.patchValue({
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email,
      phoneNumber: user.phoneNumber,
      isActive: user.isActive,
      roleIds
    });

    // Clear password validators for edit mode
    this.form.get('password')?.clearValidators();
    this.form.get('password')?.updateValueAndValidity();

    this.form.markAsPristine();
  }

  private loadRoles(): void {
    this.isLoadingRoles = true;

    this.service.getAvailableRoles()
      .pipe(take(1))
      .subscribe({
        next: res => {
          this.isLoadingRoles = false;

          if (!res.success) {
            this.snackBar.open(res.message || 'Failed to load roles', 'Close', { duration: 3000 });
            return;
          }

          this.availableRoles = res.data;

          // If editing, patch the role IDs after roles are loaded
          if (this.isEditMode && this.data.user) {
            const roleIds = this.data.user.roles?.map(r => r.roleId) ?? [];
            this.form.patchValue({ roleIds });
          }
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
            this.snackBar.open(res.message || 'Failed to load schools', 'Close', { duration: 3000 });
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

      () => this.data.user!.id
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