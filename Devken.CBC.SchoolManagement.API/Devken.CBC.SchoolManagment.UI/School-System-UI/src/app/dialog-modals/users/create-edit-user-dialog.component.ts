import { Component, Inject, OnInit, OnDestroy, ChangeDetectorRef, HostBinding } from '@angular/core';
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
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject } from 'rxjs';
import { takeUntil, take } from 'rxjs/operators';
import { UserService } from 'app/core/DevKenService/user/UserService';
import { CreateUserRequest, UpdateUserRequest, UserDto, RoleDto } from 'app/core/DevKenService/Types/roles';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';


// ── Types ──────────────────────────────────────────────────────────────────────

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

type TabId = 'identity' | 'contact' | 'access';

interface TabConfig {
  id: TabId;
  label: string;
  icon: string;
  fields: string[];
}

// ── Component ──────────────────────────────────────────────────────────────────

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
    MatTooltipModule,
  ],
  templateUrl: './create-edit-user-dialog.component.html',
  styleUrl: './create-edit-user-dialog.component.scss'
})
export class CreateEditUserDialogComponent implements OnInit, OnDestroy {

  // ── Host Bindings ────────────────────────────────────────────────────────────
  @HostBinding('class.is-loading')
  get hostIsLoading(): boolean {
    return this.isLoadingRoles || this.isLoadingSchools;
  }

  // ── Form State ───────────────────────────────────────────────────────────────
  form!: FormGroup;
  formSubmitted = false;

  // ── Dialog State ─────────────────────────────────────────────────────────────
  isEditMode       = false;
  isSuperAdmin     = false;
  isSaving         = false;
  isLoadingRoles   = false;
  isLoadingSchools = false;

  dialogTitle      = '';
  availableRoles:   RoleDto[]      = [];
  availableSchools: SchoolOption[] = [];

  // ── Tab State ────────────────────────────────────────────────────────────────
  activeTab: TabId = 'identity';

  readonly tabs: TabConfig[] = [
    {
      id: 'identity',
      label: 'Identity',
      icon: 'badge',
      fields: ['firstName', 'lastName', 'schoolId']
    },
    {
      id: 'contact',
      label: 'Contact',
      icon: 'contact_phone',
      fields: ['email', 'phoneNumber']
    },
    {
      id: 'access',
      label: 'Access',
      icon: 'admin_panel_settings',
      fields: ['roleIds', 'isActive', 'sendWelcomeEmail']
    }
  ];

  private _destroy = new Subject<void>();

  // ── Computed ──────────────────────────────────────────────────────────────────

  get showSchoolSelection(): boolean {
    return this.isSuperAdmin && !this.isEditMode;
  }

  get currentTabIndex(): number {
    return this.tabs.findIndex(t => t.id === this.activeTab);
  }

  get isFirstTab(): boolean { return this.currentTabIndex === 0; }
  get isLastTab():  boolean { return this.currentTabIndex === this.tabs.length - 1; }

  // ── Constructor ───────────────────────────────────────────────────────────────

  constructor(
    private _fb:        FormBuilder,
    private _userSvc:   UserService,
    private _schoolSvc: SchoolService,
    private _alert:     AlertService,
    private _dialogRef: MatDialogRef<CreateEditUserDialogComponent>,
    private _cdr:       ChangeDetectorRef,
    @Inject(MAT_DIALOG_DATA) private _data: UserDialogData
  ) {
    _dialogRef.addPanelClass(['user-management-dialog']);
  }

  // ── Lifecycle ──────────────────────────────────────────────────────────────────


  // ── Lifecycle ──────────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.isEditMode   = this._data.mode === 'edit';
    this.isSuperAdmin = this._data.isSuperAdmin ?? false;
    this.dialogTitle  = this.isEditMode ? 'Edit User' : 'Create New User';

    this._buildForm();

    if (this.showSchoolSelection) {
      this._loadSchools();

      this.form.get('schoolId')!.valueChanges
        .pipe(takeUntil(this._destroy))
        .subscribe(schoolId => {
          if (schoolId) {
            this._loadRoles(schoolId);
          } else {
            this.availableRoles = [];
            this.form.get('roleIds')?.setValue([]);
          }
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

  // ── Form ───────────────────────────────────────────────────────────────────────

  private _buildForm(): void {
    this.form = this._fb.group({
      schoolId:         [null, this.showSchoolSelection ? [Validators.required] : []],
      firstName:        ['', [Validators.required, Validators.minLength(2)]],
      lastName:         ['', [Validators.required, Validators.minLength(2)]],
      email:            ['', [Validators.required, Validators.email]],
      phoneNumber:      [''],
      roleIds:          [[]],
      isActive:         [true],
      sendWelcomeEmail: [true]
    });
  }

  // ── Loaders ────────────────────────────────────────────────────────────────────

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
            this._alert.error(res.message || 'Failed to load schools');
          }
          this._cdr.detectChanges();
        },
        error: err => {
          this.isLoadingSchools = false;
          this._alert.error(err?.error?.message || 'Failed to load schools');
          this._cdr.detectChanges();
        }
      });
  }

  private _loadRoles(schoolId?: string): void {
    this.isLoadingRoles = true;
    this.form.get('roleIds')?.setValue([]);

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
              this._alert.warning('No roles found for this school. Please create roles first.');
            }
          } else {
            this.availableRoles = [];
            this._alert.error(res.message || 'Failed to load roles');
          }
          this._cdr.detectChanges();
        },
        error: () => {
          this.isLoadingRoles = false;
          this.availableRoles = [];
          this._alert.error(err?.error?.message || err?.message || 'Failed to load roles');
          console.error('[UserDialog] roles load error:', err);
          this._cdr.detectChanges();
        }
      });
  }

  private _loadUser(userId: string): void {
    this._userSvc.getById(userId)
      .pipe(take(1), takeUntil(this._destroy))
      .subscribe({
        next: res => {
          if (!res.success || !res.data) {
            this._alert.error('Failed to load user data');
            return;
          }

          const user = res.data;

          this.form.patchValue({
            firstName:   user.firstName,
            lastName:    user.lastName,
            email:       user.email,
            phoneNumber: user.phoneNumber ?? '',
            isActive:    user.isActive,
            roleIds:     user.roles?.map((r: any) => r.id ?? r) ?? []
          });

          this.form.get('email')?.disable();
          this._cdr.detectChanges();
        },
        error: err => {
          this._alert.error(err?.error?.message || 'Failed to load user data');
        }
      });
  }

  // ── Navigation ─────────────────────────────────────────────────────────────────

  setTab(tabId: TabId): void {
    this.activeTab = tabId;
  }

  nextTab(): void {
    const idx = this.currentTabIndex;
    if (idx < this.tabs.length - 1) {
      this.activeTab = this.tabs[idx + 1].id;
    }
  }

  prevTab(): void {
    const idx = this.currentTabIndex;
    if (idx > 0) this.activeTab = this.tabs[idx - 1].id;
  }

  // ── Save ───────────────────────────────────────────────────────────────────────

  onSave(): void {
    this.formSubmitted = true;

    if (this.form.invalid) {
      for (const tab of this.tabs) {
        if (tab.fields.some(f => this.form.get(f)?.invalid)) {
          this.activeTab = tab.id;
          break;
        }
      }
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
      schoolId:         this.showSchoolSelection ? v.schoolId : undefined
    };

    this._userSvc.create(payload)
      .pipe(take(1))
      .subscribe({
        next: res => {
          this.isSaving = false;
          if (res.success) {
            this._alert.success('User created successfully');
            this._dialogRef.close(true);
          } else {
            this._alert.error(res.message || 'Failed to create user');
          }
        },
        error: err => {
          this.isSaving = false;
          this._alert.error(err?.error?.message || err?.message || 'Failed to create user');
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
    };

    this._userSvc.update(this._data.userId!, payload)
      .pipe(take(1))
      .subscribe({
        next: res => {
          this.isSaving = false;
          if (res.success) {
            this._alert.success('User updated successfully');
            this._dialogRef.close(true);
          } else {
            this._alert.error(res.message || 'Failed to update user');
          }
        },
        error: err => {
          this.isSaving = false;
          this._alert.error(err?.error?.message || err?.message || 'Failed to update user');
        }
      });
  }

  onCancel(): void {
    this._dialogRef.close(false);
  }

  // ── Validation Helpers ──────────────────────────────────────────────────────────

  tabHasErrors(tab: TabConfig): boolean {
    return this.formSubmitted && tab.fields.some(f => this.form.get(f)?.invalid);
  }

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