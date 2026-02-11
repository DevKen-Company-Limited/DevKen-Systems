import {
    Component,
    OnInit,
    OnDestroy,
    AfterViewInit,
    ViewChild,
    inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';

import { Subject, forkJoin, takeUntil, finalize, debounceTime, distinctUntilChanged } from 'rxjs';

import { FuseAlertComponent, FuseAlertType } from '@fuse/components/alert';
import { FuseConfirmationService } from '@fuse/services/confirmation';

import { RolePermissionService } from 'app/core/DevKenService/Roles/RolePermissionService';
import {
    RoleWithPermissions,
    UserWithPermission
} from 'app/core/DevKenService/Types/role-permissions';
import { UserWithRoles } from 'app/core/DevKenService/Types/roles';
import { RoleManageDialogComponent } from 'app/dialog-modals/roles/role-manage-dialog-component';
import { RoleUsersDialogComponent } from 'app/dialog-modals/roles/RoleUsersDialogComponent';
import { PermissionUsersDialogComponent } from 'app/dialog-modals/roles/permission/PermissionUsersDialogComponent';



/**
 * Role Assignment Management Component
 * Manages the assignment of roles to users
 */
@Component({
    selector: 'app-role-assignment-management',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        MatButtonModule,
        MatIconModule,
        MatFormFieldModule,
        MatInputModule,
        MatSelectModule,
        MatCardModule,
        MatTableModule,
        MatPaginatorModule,
        MatSortModule,
        MatProgressSpinnerModule,
        MatTooltipModule,
        MatDialogModule,
        MatSnackBarModule,
        MatTabsModule,
        MatMenuModule,
        MatDividerModule,
        FuseAlertComponent
    ],
    templateUrl: './role-assignment.component.html',
    styleUrls: ['./role-assignment.component.scss']
})
export class RoleAssignmentManagementComponent
    implements OnInit, AfterViewInit, OnDestroy {

    @ViewChild(MatPaginator) paginator!: MatPaginator;
    @ViewChild(MatSort) sort!: MatSort;

    private readonly rolePermissionService = inject(RolePermissionService);
    private readonly dialog = inject(MatDialog);
    private readonly snackBar = inject(MatSnackBar);
    private readonly confirmationService = inject(FuseConfirmationService);
    private readonly unsubscribe$ = new Subject<void>();
    
    // Search subject for debouncing
    searchSubject = new Subject<string>();

    // ------------------------------------------------------
    // State
    // ------------------------------------------------------

    allUsers: UserWithRoles[] = [];
    availableRoles: RoleWithPermissions[] = [];
    roleUserCountMap: Map<string, number> = new Map();

    displayedColumns: string[] = ['user', 'email', 'roles', 'actions'];
    usersDataSource = new MatTableDataSource<UserWithRoles>([]);

    isLoading = false;
    selectedTabIndex = 0;

    // Search and filter
    userSearchTerm = '';
    roleFilterValue: string | null = null;

    // Pagination
    currentPage = 1;
    pageSize = 10;
    totalUsers = 0;

    stats = {
        totalRoles: 0,
        totalUsers: 0
    };

    // Alert
    showAlert = false;
    alert: { type: FuseAlertType; message: string } = {
        type: 'success',
        message: ''
    };

    // ------------------------------------------------------
    // Lifecycle
    // ------------------------------------------------------

    ngOnInit(): void {
        this.loadData();
        this.setupTableSorting();
        this.setupSearchDebounce();
    }

    ngAfterViewInit(): void {
        this.usersDataSource.paginator = this.paginator;
        this.usersDataSource.sort = this.sort;
    }

    ngOnDestroy(): void {
        this.unsubscribe$.next();
        this.unsubscribe$.complete();
    }

    // ------------------------------------------------------
    // Setup Methods
    // ------------------------------------------------------

    private setupSearchDebounce(): void {
        this.searchSubject.pipe(
            debounceTime(300),
            distinctUntilChanged(),
            takeUntil(this.unsubscribe$)
        ).subscribe(() => {
            this.applyUserFilter();
        });
    }

    private setupTableSorting(): void {
        this.usersDataSource.sortingDataAccessor = (
            item: UserWithRoles,
            property: string
        ) => {
            switch (property) {
                case 'user':
                    return (item.fullName || '').toLowerCase();
                case 'email':
                    return (item.email || '').toLowerCase();
                case 'roles':
                    return item.roles?.length ?? 0;
                default:
                    return '';
            }
        };
    }

    // ------------------------------------------------------
    // Data Loading
    // ------------------------------------------------------

    loadData(): void {
        this.isLoading = true;

        forkJoin({
            users: this.rolePermissionService.getAllUsers(this.currentPage, this.pageSize),
            roles: this.rolePermissionService.getAvailableRoles()
        })
        .pipe(
            takeUntil(this.unsubscribe$),
            finalize(() => (this.isLoading = false))
        )
        .subscribe({
            next: ({ users, roles }) => {
                // Handle users response
                if (users?.success && users.data) {
                    this.allUsers = users.data.items || [];
                    this.totalUsers = users.data.totalCount || 0;
                    this.stats.totalUsers = this.totalUsers;
                    this.usersDataSource.data = this.allUsers;
                }

                // Handle roles response
                if (roles?.success && roles.data) {
                    this.availableRoles = roles.data || [];
                    this.stats.totalRoles = this.availableRoles.length;
                    
                    // Calculate user counts per role
                    this.calculateRoleUserCounts();
                }

                // Apply initial filter
                this.applyUserFilter();
            },
            error: err => {
                this.showErrorAlert(
                    err?.error?.message || err?.message || 'Failed to load data'
                );
                console.error('Error loading data:', err);
            }
        });
    }

    /**
     * Calculate how many users have each role
     */
    private calculateRoleUserCounts(): void {
        this.roleUserCountMap.clear();
        
        this.allUsers.forEach(user => {
            if (user.roles && Array.isArray(user.roles)) {
                user.roles.forEach(role => {
                    const roleId = role.roleId || role.id;
                    if (roleId) {
                        const currentCount = this.roleUserCountMap.get(roleId) || 0;
                        this.roleUserCountMap.set(roleId, currentCount + 1);
                    }
                });
            }
        });
    }

    /**
     * Load users for a specific page
     */
    private loadUsersPage(pageNumber: number, pageSize: number): void {
        this.isLoading = true;

        this.rolePermissionService.getAllUsers(pageNumber, pageSize)
            .pipe(
                takeUntil(this.unsubscribe$),
                finalize(() => (this.isLoading = false))
            )
            .subscribe({
                next: (response) => {
                    if (response?.success && response.data) {
                        this.allUsers = response.data.items || [];
                        this.totalUsers = response.data.totalCount || 0;
                        this.usersDataSource.data = this.allUsers;
                        this.calculateRoleUserCounts();
                        this.applyUserFilter();
                    }
                },
                error: (err) => {
                    this.showErrorAlert(
                        err?.error?.message || err?.message || 'Failed to load users'
                    );
                    console.error('Error loading users:', err);
                }
            });
    }

    // ------------------------------------------------------
    // Filtering
    // ------------------------------------------------------

    applyUserFilter(): void {
        let data = [...this.allUsers];

        // Apply search term filter
        if (this.userSearchTerm?.trim()) {
            const term = this.userSearchTerm.toLowerCase();
            data = data.filter(u =>
                u.fullName?.toLowerCase().includes(term) ||
                u.email?.toLowerCase().includes(term) ||
                u.userName?.toLowerCase().includes(term)
            );
        }

        // Apply role filter
        if (this.roleFilterValue) {
            data = data.filter(u =>
                u.roles?.some(r => (r.roleId || r.id) === this.roleFilterValue)
            );
        }

        this.usersDataSource.data = data;
    }

    clearUserFilter(): void {
        this.userSearchTerm = '';
        this.roleFilterValue = null;
        this.applyUserFilter();
    }

    // ------------------------------------------------------
    // Pagination
    // ------------------------------------------------------

    onPageChange(event: PageEvent): void {
        this.currentPage = event.pageIndex + 1;
        this.pageSize = event.pageSize;
        this.loadUsersPage(this.currentPage, this.pageSize);
    }

    // ------------------------------------------------------
    // Dialog Actions
    // ------------------------------------------------------

    /**
     * Open dialog to manage roles for a user
     */
    openManageRolesDialog(user: UserWithRoles): void {
        const dialogRef = this.dialog.open(RoleManageDialogComponent, {
            width: '700px',
            maxHeight: '85vh',
            data: {
                user,
                availableRoles: this.availableRoles
            },
            disableClose: false,
            autoFocus: true
        });

        dialogRef.afterClosed().subscribe((roleIds: string[] | undefined) => {
            if (roleIds !== undefined && roleIds !== null) {
                this.updateUserRoles(user.userId, roleIds);
            }
        });
    }

    /**
     * View user details including all assigned roles
     */
    viewUserDetails(user: UserWithRoles): void {
        this.dialog.open(PermissionUsersDialogComponent, {
            width: '650px',
            maxHeight: '85vh',
            data: { user },
            autoFocus: true
        });
    }

    /**
     * View all users assigned to a specific role
     */
    viewRoleUsers(role: RoleWithPermissions | any): void {
        const roleId = role.id || role.roleId;
        const userCount = this.getRoleUserCount(roleId);

        if (userCount === 0) {
            this.snackBar.open(`No users assigned to "${role.name || role.roleName}"`, 'Close', {
                duration: 3000,
                horizontalPosition: 'end',
                verticalPosition: 'top'
            });
            return;
        }

        this.isLoading = true;

        this.rolePermissionService.getUsersByRole(roleId)
            .pipe(
                takeUntil(this.unsubscribe$),
                finalize(() => (this.isLoading = false))
            )
            .subscribe({
                next: (response) => {
                    if (response?.success) {
                        const users = response.data || [];

                        if (users.length === 0) {
                            this.snackBar.open(`No users found for "${role.name || role.roleName}"`, 'Close', {
                                duration: 3000,
                                horizontalPosition: 'end',
                                verticalPosition: 'top'
                            });
                            return;
                        }

                        this.dialog.open(RoleUsersDialogComponent, {
                            width: '700px',
                            maxHeight: '85vh',
                            data: {
                                role: role,
                                users: users,
                                title: `Users with ${role.name || role.roleName} Role`
                            },
                            autoFocus: true,
                            disableClose: false
                        });
                    } else {
                        this.showErrorAlert(response?.message || 'Failed to load users for this role');
                    }
                },
                error: (err) => {
                    this.showErrorAlert(
                        err?.error?.message || err?.message || 'Failed to load users'
                    );
                    console.error('Error loading role users:', err);
                }
            });
    }

    /**
     * Remove all roles from a user
     */
    removeAllRoles(userId: string): void {
        const user = this.allUsers.find(u => u.userId === userId);
        if (!user) return;

        const confirmation = this.confirmationService.open({
            title: 'Remove All Roles',
            message: `Are you sure you want to remove all roles from "${user.fullName}"? This action cannot be undone.`,
            icon: {
                show: true,
                name: 'heroicons_outline:exclamation-triangle',
                color: 'warn'
            },
            actions: {
                confirm: {
                    show: true,
                    label: 'Remove All',
                    color: 'warn'
                },
                cancel: {
                    show: true,
                    label: 'Cancel'
                }
            },
            dismissible: true
        });

        confirmation.afterClosed().subscribe((result) => {
            if (result === 'confirmed') {
                this.executeRemoveAllRoles(userId);
            }
        });
    }

    private executeRemoveAllRoles(userId: string): void {
        this.isLoading = true;

        this.rolePermissionService.removeAllRolesFromUser(userId)
            .pipe(
                takeUntil(this.unsubscribe$),
                finalize(() => (this.isLoading = false))
            )
            .subscribe({
                next: res => {
                    if (res?.success) {
                        this.showSuccessAlert('All roles removed successfully');
                        this.loadData();
                    } else {
                        this.showErrorAlert(res?.message || 'Failed to remove roles');
                    }
                },
                error: err => {
                    this.showErrorAlert(
                        err?.error?.message || err?.message || 'Failed to remove roles'
                    );
                    console.error('Error removing roles:', err);
                }
            });
    }

    /**
     * Update user roles
     */
    private updateUserRoles(userId: string, roleIds: string[]): void {
        this.isLoading = true;

        this.rolePermissionService.updateUserRoles(userId, roleIds)
            .pipe(
                takeUntil(this.unsubscribe$),
                finalize(() => (this.isLoading = false))
            )
            .subscribe({
                next: res => {
                    if (res?.success) {
                        this.showSuccessAlert(res.message || 'User roles updated successfully');
                        this.loadData();
                    } else {
                        this.showErrorAlert(res?.message || 'Update failed');
                    }
                },
                error: err => {
                    this.showErrorAlert(
                        err?.error?.message || err?.message || 'Failed to update user roles'
                    );
                    console.error('Error updating user roles:', err);
                }
            });
    }

    // ------------------------------------------------------
    // Template Helper Methods
    // ------------------------------------------------------

    /**
     * Get initials from full name for avatar display
     */
    getUserInitials(fullName: string): string {
        if (!fullName?.trim()) return '?';

        const names = fullName.trim().split(' ');
        if (names.length >= 2) {
            return (names[0][0] + names[names.length - 1][0]).toUpperCase();
        }
        return fullName.substring(0, 2).toUpperCase();
    }

    /**
     * Get tooltip text for extra roles
     */
    getExtraRolesTooltip(roles: any[]): string {
        if (!roles || roles.length <= 3) return '';
        
        return roles.slice(3)
            .map(r => r.roleName || r.name)
            .join(', ');
    }

    getRoleName(roleId: number | string | null): string {
  if (!roleId || !this.availableRoles?.length) {
    return 'Unknown';
  }

  const role = this.availableRoles.find(
    r => (r.roleId ?? r.roleId) === roleId
  );

  return role?.roleName ?? 'Unknown';
}


    /**
     * Get user count for a specific role
     */
    getRoleUserCount(roleId: string): number {
        return this.roleUserCountMap.get(roleId) || 0;
    }

    /**
     * TrackBy function for roles
     */
    trackByRoleId(index: number, role: RoleWithPermissions): string {
        return role?.roleId || role?.roleId || index.toString();
    }

    /**
     * TrackBy function for users
     */
    trackByUserId(index: number, user: UserWithRoles): string {
        return user?.userId || index.toString();
    }

    // ------------------------------------------------------
    // Alerts & Notifications
    // ------------------------------------------------------

    private showSuccessAlert(message: string): void {
        this.alert = { type: 'success', message };
        this.showAlert = true;

        // Auto-hide success alerts after 5 seconds
        setTimeout(() => {
            if (this.alert.type === 'success') {
                this.showAlert = false;
            }
        }, 5000);

        // Also show snackbar for immediate feedback
        this.snackBar.open(message, 'Close', {
            duration: 3000,
            horizontalPosition: 'end',
            verticalPosition: 'top',
            panelClass: ['success-snackbar']
        });
    }

    private showErrorAlert(message: string): void {
        this.alert = { type: 'error', message };
        this.showAlert = true;

        // Also show snackbar
        this.snackBar.open(message, 'Close', {
            duration: 5000,
            horizontalPosition: 'end',
            verticalPosition: 'top',
            panelClass: ['error-snackbar']
        });
    }
}