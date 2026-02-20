import { Component, OnInit, OnDestroy, AfterViewInit, ViewChild, TemplateRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { FuseConfirmationService } from '@fuse/services/confirmation';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { AuthService } from 'app/core/auth/auth.service';
import { UserService } from 'app/core/DevKenService/user/UserService';
import { UserDto } from 'app/core/DevKenService/Types/roles';
import { CreateEditUserDialogComponent } from 'app/dialog-modals/users/create-edit-user-dialog.component';

// Reusable components
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';
import {
  DataTableComponent,
  TableColumn,
  TableAction,
  TableHeader,
  TableEmptyState,
} from 'app/shared/data-table/data-table.component';

@Component({
  selector: 'app-users-management',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatButtonModule,
    MatDialogModule,
    MatSnackBarModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
    // Reusable components
    PageHeaderComponent,
    FilterPanelComponent,
    PaginationComponent,
    StatsCardsComponent,
    DataTableComponent,
  ],
  templateUrl: './users-management.component.html',
})
export class UsersManagementComponent implements OnInit, OnDestroy, AfterViewInit {

  // ── Cell template refs ───────────────────────────────────────────────────────
  @ViewChild('userCell')    userCellTemplate!:    TemplateRef<any>;
  @ViewChild('emailCell')   emailCellTemplate!:   TemplateRef<any>;
  @ViewChild('schoolCell')  schoolCellTemplate!:  TemplateRef<any>;
  @ViewChild('phoneCell')   phoneCellTemplate!:   TemplateRef<any>;
  @ViewChild('rolesCell')   rolesCellTemplate!:   TemplateRef<any>;
  @ViewChild('statusCell')  statusCellTemplate!:  TemplateRef<any>;
  @ViewChild('updatedCell') updatedCellTemplate!: TemplateRef<any>;

  private _unsubscribe = new Subject<void>();
  private _authService  = inject(AuthService);
  private _alert        = inject(AlertService);

  // ── Breadcrumbs ──────────────────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard',       url: '/dashboard' },
    { label: 'Administration',  url: '/admin' },
    { label: 'User Management' },
  ];

  // ── SuperAdmin ───────────────────────────────────────────────────────────────
  get isSuperAdmin(): boolean {
    return this._authService.authUser?.isSuperAdmin ?? false;
  }

  // ── State ────────────────────────────────────────────────────────────────────
  allData:   UserDto[] = [];
  isLoading  = false;
  showFilterPanel = false;
  cellTemplates: { [key: string]: TemplateRef<any> } = {};

  // ── Filters ──────────────────────────────────────────────────────────────────
  private _filterValues = {
    search:   '',
    status:   'all',
    schoolId: 'all',
  };

  // ── Pagination ───────────────────────────────────────────────────────────────
  currentPage  = 1;
  itemsPerPage = 10;

  // ── Stats ────────────────────────────────────────────────────────────────────
  get total():         number { return this.allData.length; }
  get activeCount():   number { return this.allData.filter(u => u.isActive).length; }
  get inactiveCount(): number { return this.allData.filter(u => !u.isActive).length; }
  get verifiedCount(): number { return this.allData.filter(u => u.isEmailVerified).length; }

  get statsCards(): StatCard[] {
    const cards: StatCard[] = [
      { label: 'Total Users',      value: this.total,         icon: 'group',          iconColor: 'indigo' },
      { label: 'Active',           value: this.activeCount,   icon: 'check_circle',   iconColor: 'green'  },
      { label: 'Inactive',         value: this.inactiveCount, icon: 'block',          iconColor: 'red'    },
      { label: 'Email Verified',   value: this.verifiedCount, icon: 'verified',       iconColor: 'violet' },
    ];
    return cards;
  }

  // ── Filtered + Paginated Data ─────────────────────────────────────────────────
  get filteredData(): UserDto[] {
    return this.allData.filter(u => {
      const q = this._filterValues.search.toLowerCase();
      const matchText =
        !q ||
        u.firstName?.toLowerCase().includes(q) ||
        u.lastName?.toLowerCase().includes(q)  ||
        u.email?.toLowerCase().includes(q)     ||
        u.phoneNumber?.toLowerCase().includes(q);

      const matchStatus =
        this._filterValues.status === 'all' ||
        (this._filterValues.status === 'active'   && u.isActive)  ||
        (this._filterValues.status === 'inactive' && !u.isActive);

      const matchSchool =
        this._filterValues.schoolId === 'all' ||
        u.schoolId === this._filterValues.schoolId;

      return matchText && matchStatus && matchSchool;
    });
  }

  get paginatedData(): UserDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  // ── Table Columns ────────────────────────────────────────────────────────────
  get tableColumns(): TableColumn<UserDto>[] {
    const cols: TableColumn<UserDto>[] = [
      { id: 'user',    label: 'User',         align: 'left', sortable: true },
      { id: 'email',   label: 'Email',        align: 'left', hideOnMobile: true },
    ];

    if (this.isSuperAdmin) {
      cols.push({ id: 'school', label: 'School', align: 'left', hideOnMobile: true });
    }

    cols.push(
      { id: 'phone',   label: 'Phone',        align: 'left', hideOnMobile: true },
      { id: 'roles',   label: 'Roles',        align: 'left', hideOnTablet: false },
      { id: 'status',  label: 'Status',       align: 'center' },
      { id: 'updated', label: 'Last Updated', align: 'left', hideOnTablet: true },
    );

    return cols;
  }

  tableActions: TableAction<UserDto>[] = [
    {
      id:      'edit',
      label:   'Edit',
      icon:    'edit',
      color:   'blue',
      handler: (user) => this.openEdit(user),
    },
    {
      id:      'resetPassword',
      label:   'Reset Password',
      icon:    'lock_reset',
      color:   'violet',
      handler: (user) => this.resetPassword(user),
    },
    {
      id:      'deactivate',
      label:   'Deactivate',
      icon:    'block',
      color:   'amber',
      handler: (user) => this.toggleUserStatus(user),
      visible: (user) => user.isActive,
    },
    {
      id:      'activate',
      label:   'Activate',
      icon:    'check_circle',
      color:   'green',
      handler: (user) => this.toggleUserStatus(user),
      visible: (user) => !user.isActive,
      divider: true,
    },
    {
      id:      'delete',
      label:   'Delete',
      icon:    'delete',
      color:   'red',
      handler: (user) => this.removeUser(user),
    },
  ];

  tableHeader: TableHeader = {
    title:         'All Users',
    subtitle:      '',
    icon:          'table_chart',
    iconGradient:  'bg-gradient-to-br from-indigo-500 via-violet-600 to-purple-700',
  };

  tableEmptyState: TableEmptyState = {
    icon:        'person_search',
    message:     'No users found',
    description: 'Try adjusting your filters or create a new user',
    action: {
      label:   'Add First User',
      icon:    'person_add',
      handler: () => this.openCreate(),
    },
  };

  // ── Filter Fields ────────────────────────────────────────────────────────────
  filterFields: FilterField[] = [];

  private initFilterFields(): void {
    this.filterFields = [
      {
        id:          'search',
        label:       'Search',
        type:        'text',
        placeholder: 'Name, email, or phone...',
        value:       this._filterValues.search,
      },
      {
        id:      'status',
        label:   'Status',
        type:    'select',
        value:   this._filterValues.status,
        options: [
          { label: 'All Statuses', value: 'all' },
          { label: 'Active',       value: 'active' },
          { label: 'Inactive',     value: 'inactive' },
        ],
      },
    ];
  }

  constructor(
    private _service:      UserService,
    private _dialog:       MatDialog,
    private _confirmation: FuseConfirmationService,
  ) {}

  ngOnInit(): void {
    this.initFilterFields();
    this.loadAll();
  }

  ngAfterViewInit(): void {
    this.cellTemplates = {
      user:    this.userCellTemplate,
      email:   this.emailCellTemplate,
      school:  this.schoolCellTemplate,
      phone:   this.phoneCellTemplate,
      roles:   this.rolesCellTemplate,
      status:  this.statusCellTemplate,
      updated: this.updatedCellTemplate,
    };
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

loadAll(): void {
  this.isLoading = true;

  this._service.getAll()
    .pipe(takeUntil(this._unsubscribe))
    .subscribe({
      next: (res) => {
        if (res.success) {
          this.allData = res.data ?? [];
          this.tableHeader.subtitle =
            `${this.filteredData.length} users found`;
        }
        this.isLoading = false;
      },
      error: (err) => {
        this.isLoading = false;
        this._alert.error(
          err?.error?.message || 'Failed to load users'
        );
      },
    });
}

  // ── CRUD ──────────────────────────────────────────────────────────────────────
// ── CRUD ──────────────────────────────────────────────────────────────────────
  openCreate(): void {
    const dialogRef = this._dialog.open(CreateEditUserDialogComponent, {
      panelClass: 'user-management-dialog',
      disableClose: true,
      data: { mode: 'create', isSuperAdmin: this.isSuperAdmin },
    });

    dialogRef.afterClosed()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe(result => {
        if (result) {
          //this._alert.success('User created successfully');
          this.loadAll();
        }
      });
  }

  openEdit(user: UserDto): void {
    const dialogRef = this._dialog.open(CreateEditUserDialogComponent, {
      panelClass: 'user-management-dialog',
      disableClose: true,
      data: { mode: 'edit', userId: user.id, isSuperAdmin: this.isSuperAdmin },
    });

    dialogRef.afterClosed()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe(result => {
        if (result) {
         // this._alert.success('User updated successfully');
          this.loadAll();
        }
      });
  }

  removeUser(user: UserDto): void {
    const confirmation = this._confirmation.open({
      title:   'Delete User',
      message: `Are you sure you want to delete ${user.firstName} ${user.lastName}? This cannot be undone.`,
      icon:    { name: 'delete', color: 'warn' },
      actions: {
        confirm: { label: 'Delete', color: 'warn' },
        cancel:  { label: 'Cancel' },
      },
    });

    confirmation.afterClosed()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe(result => {
        if (result === 'confirmed') {
          this._service.deleteUser(user.id)
            .pipe(takeUntil(this._unsubscribe))
            .subscribe({
              next: (res) => {
                if (res.success) {
                  this._alert.success('User deleted successfully');
                  if (this.paginatedData.length === 1 && this.currentPage > 1) {
                    this.currentPage--;
                  }
                  this.loadAll();
                }
              },
              error: (err) => {
                this._alert.error(err?.error?.message || 'Failed to delete user');
              },
            });
        }
      });
  }

  toggleUserStatus(user: UserDto): void {
    const action = user.isActive ? 'deactivate' : 'activate';

    this._alert.confirm({
      title:       `${user.isActive ? 'Deactivate' : 'Activate'} User`,
      message:     `Are you sure you want to ${action} ${user.firstName} ${user.lastName}?`,
      confirmText: user.isActive ? 'Deactivate' : 'Activate',
      cancelText:  'Cancel',
      onConfirm: () => {
        const req$ = user.isActive
          ? this._service.deactivateUser(user.id)
          : this._service.activateUser(user.id);

        req$.pipe(takeUntil(this._unsubscribe)).subscribe({
          next: (res) => {
            if (res.success) {
              this._alert.success(`User ${action}d successfully`);
              this.loadAll();
            }
          },
          error: (err) => {
            this._alert.error(err?.error?.message || `Failed to ${action} user`);
          },
        });
      },
      onCancel: () => this._alert.info('Action cancelled'),
    });
  }

  resetPassword(user: UserDto): void {
    this._alert.confirm({
      title:       'Reset Password',
      message:     `Reset password for ${user.firstName} ${user.lastName}? A new temporary password will be generated.`,
      confirmText: 'Reset',
      cancelText:  'Cancel',
      onConfirm: () => {
        this._service.resetPassword(user.id)
          .pipe(takeUntil(this._unsubscribe))
          .subscribe({
            next: (res) => {
              if (res.success) {
                const tmpPassword = res.data?.temporaryPassword;
                const msg = tmpPassword
                  ? `Password reset. Temporary password: ${tmpPassword}`
                  : 'Password reset successfully';
                this._alert.success(msg);
                this.loadAll();
              }
            },
            error: (err) => {
              this._alert.error(err?.error?.message || 'Failed to reset password');
            },
          });
      },
      onCancel: () => {},
    });
  }

  // ── Filter Handlers ──────────────────────────────────────────────────────────
  toggleFilterPanel(): void {
    this.showFilterPanel = !this.showFilterPanel;
  }

  onFilterChange(event: FilterChangeEvent): void {
    (this._filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} users found`;
  }

  onClearFilters(): void {
    this._filterValues = { search: '', status: 'all', schoolId: 'all' };
    this.filterFields.forEach(f => { f.value = (this._filterValues as any)[f.id]; });
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} users found`;
  }

  // ── Pagination ───────────────────────────────────────────────────────────────
  onPageChange(page: number): void {
    this.currentPage = page;
  }

  onItemsPerPageChange(itemsPerPage: number): void {
    this.itemsPerPage = itemsPerPage;
    this.currentPage  = 1;
  }

  // ── Helpers ──────────────────────────────────────────────────────────────────
  getInitials(user: UserDto): string {
    return `${user.firstName?.charAt(0) ?? ''}${user.lastName?.charAt(0) ?? ''}`.toUpperCase();
  }

  getRoles(user: UserDto): string[] {
    if (user.roleNames?.length) return user.roleNames;
    if ((user as any).roles?.length) return (user as any).roles.map((r: any) => r.name ?? r);
    return [];
  }

  formatDate(date: string | undefined): string {
    if (!date) return 'Never';
    const d    = new Date(date);
    const now  = new Date();
    const days = Math.floor((now.getTime() - d.getTime()) / 86_400_000);
    if (days === 0) return 'Today';
    if (days === 1) return 'Yesterday';
    if (days <  7)  return `${days} days ago`;
    return d.toLocaleDateString('en-US', {
      month: 'short',
      day:   'numeric',
      year:  d.getFullYear() !== now.getFullYear() ? 'numeric' : undefined,
    });
  }
}