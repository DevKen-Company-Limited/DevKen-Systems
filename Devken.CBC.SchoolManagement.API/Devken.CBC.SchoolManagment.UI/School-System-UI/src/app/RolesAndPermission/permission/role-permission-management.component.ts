import {
    Component,
    OnInit,
    OnDestroy,
    AfterViewInit,
    ViewChild,
    inject,
    HostListener
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
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';

import { Subject, forkJoin, takeUntil, finalize, debounceTime, distinctUntilChanged } from 'rxjs';

import { FuseAlertComponent, FuseAlertType } from '@fuse/components/alert';
import { FuseConfirmationService } from '@fuse/services/confirmation';

import { RolePermissionService } from 'app/core/DevKenService/Roles/RolePermissionService';
import {
    RoleWithPermissions,
    Permission,
    UpdateRolePermissionsRequest,
    ClonePermissionsRequest,
    UserWithPermission
} from 'app/core/DevKenService/Types/role-permissions';

import { RolePermissionDetailsDialogComponent } from 'app/dialog-modals/roles/permission/details/role-permission-details-dialog.component';
import { PermissionManageDialogComponent } from 'app/dialog-modals/roles/permission/permission-manage-dialog.component';
import { PermissionUsersDialogComponent } from 'app/dialog-modals/roles/permission/PermissionUsersDialogComponent';

@Component({
    selector: 'app-role-permission-management',
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
        MatChipsModule,
        MatExpansionModule,
        FuseAlertComponent
    ],
    templateUrl: './role-permission-management.component.html',
    styleUrls: ['./role-permission-management.component.scss']
})
export class RolePermissionManagementComponent
    implements OnInit, AfterViewInit, OnDestroy {

    // Expose Object to template
    Object = Object;

    @ViewChild(MatPaginator) paginator!: MatPaginator;
    @ViewChild(MatSort) sort!: MatSort;

    private readonly rolePermissionService = inject(RolePermissionService);
    private readonly dialog = inject(MatDialog);
    private readonly snackBar = inject(MatSnackBar);
    private readonly confirmationService = inject(FuseConfirmationService);
    private readonly unsubscribe$ = new Subject<void>();
    private readonly searchSubject$ = new Subject<string>();

    // ------------------------------------------------------
    // State
    // ------------------------------------------------------

    allRoles: RoleWithPermissions[] = [];
    allPermissions: Permission[] = [];
    groupedPermissions: Record<string, Permission[]> = {};

    displayedColumns: string[] = ['role', 'permissions', 'users', 'actions'];
    rolesDataSource = new MatTableDataSource<RoleWithPermissions>([]);

    isLoading = false;
    selectedTabIndex = 0;

    roleSearchTerm = '';
    permissionGroupFilter: string | null = null;

    permissionGroups: string[] = [];
    cloneSourceRole: RoleWithPermissions | null = null;

    stats = {
        totalRoles: 0,
        totalPermissions: 0,
        totalPermissionGroups: 0,
        totalUsers: 0  // Add this
    };

    // Alert
    showAlert = false;
    alert: { type: FuseAlertType; message: string } = {
        type: 'success',
        message: ''
    };

    // Badge color mapping for permission groups
    private readonly groupColorMap: Map<string, string> = new Map();
    private readonly badgeColors = [
        'bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300',
        'bg-green-100 dark:bg-green-900 text-green-700 dark:text-green-300',
        'bg-purple-100 dark:bg-purple-900 text-purple-700 dark:text-purple-300',
        'bg-orange-100 dark:bg-orange-900 text-orange-700 dark:text-orange-300',
        'bg-pink-100 dark:bg-pink-900 text-pink-700 dark:text-pink-300',
        'bg-indigo-100 dark:bg-indigo-900 text-indigo-700 dark:text-indigo-300',
        'bg-teal-100 dark:bg-teal-900 text-teal-700 dark:text-teal-300',
        'bg-cyan-100 dark:bg-cyan-900 text-cyan-700 dark:text-cyan-300'
    ];

    // ------------------------------------------------------
    // Lifecycle
    // ------------------------------------------------------

    ngOnInit(): void {
        this.loadData();
        this.setupTableSorting();
        this.setupSearchDebounce();
    }

    ngAfterViewInit(): void {
        this.rolesDataSource.paginator = this.paginator;
        this.rolesDataSource.sort = this.sort;
    }

    ngOnDestroy(): void {
        this.unsubscribe$.next();
        this.unsubscribe$.complete();
    }

    // Keyboard shortcuts
    @HostListener('document:keydown.escape')
    onEscape(): void {
        if (this.cloneSourceRole) {
            this.cancelClone();
        }
    }

    // ------------------------------------------------------
    // Setup Methods
    // ------------------------------------------------------

    private setupSearchDebounce(): void {
        this.searchSubject$.pipe(
            debounceTime(300),
            distinctUntilChanged(),
            takeUntil(this.unsubscribe$)
        ).subscribe(() => {
            this.applyRoleFilter();
        });
    }

    private setupTableSorting(): void {
        this.rolesDataSource.sortingDataAccessor = (
            item: RoleWithPermissions,
            property: string
        ) => {
            switch (property) {
                case 'role':
                    return (item.roleName || '').toLowerCase();
                case 'permissions':
                    return item.totalPermissions ?? 0;
                case 'users':
                    return item.userCount ?? 0; // Updated to use actual user count
                default:
                    return '';
            }
        };
    }

    // ------------------------------------------------------
    // Data loading
    // ------------------------------------------------------

    loadData(): void {
        this.isLoading = true;

        forkJoin({
            permissions: this.rolePermissionService.getAllPermissionsGrouped(),
            roles: this.rolePermissionService.getAllRolesWithPermissions()
        })
        .pipe(
            takeUntil(this.unsubscribe$),
            finalize(() => (this.isLoading = false))
        )
        .subscribe({
            next: ({ permissions, roles }) => {
                if (permissions?.success) {
                    this.groupedPermissions = permissions.data || {};
                    this.permissionGroups = Object.keys(this.groupedPermissions).sort();
                    this.allPermissions = Object.values(this.groupedPermissions).flat();

                    this.stats.totalPermissions = this.allPermissions.length;
                    this.stats.totalPermissionGroups = this.permissionGroups.length;

                    // Initialize group color mapping
                    this.initializeGroupColors();
                }

                if (roles?.success) {
                    this.allRoles = roles.data || [];
                    this.stats.totalRoles = this.allRoles.length;

                    // Load user counts for each role
                    this.loadRoleUserCounts();
                }
            },
            error: err => {
                this.showErrorAlert(
                    err?.error?.message || err?.message || 'Failed to load data'
                );
                console.error('Error loading data:', err);
            }
        });
    }




    // /**
    //  * Load user counts for all roles
    //  */
    // private loadRoleUserCounts(): void {
    //     this.isLoading = true;
        
    //     this.rolePermissionService.getUserCountsForAllRoles()
    //         .pipe(
    //             takeUntil(this.unsubscribe$),
    //             finalize(() => (this.isLoading = false))
    //         )
    //         .subscribe({
    //             next: (response) => {
    //                 if (response?.success && response.data) {
    //                     // Map user counts to roles
    //                     this.allRoles = this.allRoles.map(role => {
    //                         const userCount = response.data[role.roleId] || 0;
    //                         return {
    //                             ...role,
    //                             userCount: userCount,
    //                             userCountDisplay: userCount.toLocaleString()
    //                         };
    //                     });
                        
    //                     // Calculate total users
    //                     this.calculateTotalUsers();
                        
    //                     // Update the data source
    //                     this.rolesDataSource.data = this.allRoles;
    //                     this.applyRoleFilter();
    //                 }
    //             },
    //             error: (err) => {
    //                 console.error('Error loading user counts:', err);
    //                 // Fallback: If API fails, set all user counts to 0
    //                 this.allRoles = this.allRoles.map(role => ({
    //                     ...role,
    //                     userCount: 0,
    //                     userCountDisplay: '0'
    //                 }));
    //                 this.rolesDataSource.data = this.allRoles;
    //                 this.applyRoleFilter();
    //             }
    //         });
    // }

 

    private loadRoleUserCounts(): void {
    this.isLoading = true;
    
    // Get all users with their roles and count manually
    this.rolePermissionService.getAllUsers(1, 1000)
        .pipe(
            takeUntil(this.unsubscribe$),
            finalize(() => (this.isLoading = false))
        )
        .subscribe({
            next: (response) => {
                if (response?.success && response.data?.items) {
                    // Count users per role
                    const roleUserCounts: Record<string, number> = {};
                    
                    response.data.items.forEach((user: any) => {
                        if (user.roles && user.roles.length > 0) {
                            user.roles.forEach((role: any) => {
                                const roleId = role.roleId || role.id;
                                if (roleId) {
                                    roleUserCounts[roleId] = (roleUserCounts[roleId] || 0) + 1;
                                }
                            });
                        }
                    });
                    
                    // Map user counts to roles
                    this.allRoles = this.allRoles.map(role => {
                        const userCount = roleUserCounts[role.roleId] || 0;
                        return {
                            ...role,
                            userCount: userCount,
                            userCountDisplay: userCount.toLocaleString()
                        };
                    });
                    
                    // Calculate total users
                    this.calculateTotalUsers();
                    
                    this.rolesDataSource.data = this.allRoles;
                    this.applyRoleFilter();
                }
            },
            error: (err) => {
                console.error('Error loading users:', err);
                // Fallback: Set all user counts to 0
                this.allRoles = this.allRoles.map(role => ({
                    ...role,
                    userCount: 0,
                    userCountDisplay: '0'
                }));
                this.rolesDataSource.data = this.allRoles;
                this.applyRoleFilter();
            }
        });
}

    /**
     * Calculate total users across all roles
     */
    private calculateTotalUsers(): void {
        this.stats.totalUsers = this.allRoles.reduce((sum, role) => {
            return sum + (role.userCount || 0);
        }, 0);
    }

    // ------------------------------------------------------
    // Filtering
    // ------------------------------------------------------

    applyRoleFilter(): void {
        let data = [...this.allRoles];

        // Apply search term filter
        if (this.roleSearchTerm?.trim()) {
            const term = this.roleSearchTerm.toLowerCase();
            data = data.filter(r =>
                r.roleName?.toLowerCase().includes(term) ||
                r.description?.toLowerCase().includes(term)
            );
        }

        // Apply permission group filter
        if (this.permissionGroupFilter) {
            data = data.filter(r =>
                r.permissions?.some(
                    p => p.groupName === this.permissionGroupFilter
                )
            );
        }

        this.rolesDataSource.data = data;
    }

    onSearchChange(value: string): void {
        this.roleSearchTerm = value;
        this.searchSubject$.next(value);
    }

    clearRoleFilter(): void {
        this.roleSearchTerm = '';
        this.permissionGroupFilter = null;
        this.applyRoleFilter();
    }

    // ------------------------------------------------------
    // Dialogs & Actions
    // ------------------------------------------------------

    openManagePermissionsDialog(role: RoleWithPermissions): void {
        const dialogRef = this.dialog.open(PermissionManageDialogComponent, {
            width: '900px',
            height: '85vh',
            data: {
                role,
                groupedPermissions: this.groupedPermissions,
                allRoles: this.allRoles
            },
            disableClose: false,
            autoFocus: true
        });

        dialogRef.afterClosed().subscribe((permissionIds: string[] | undefined) => {
            // Allow empty arrays to remove all permissions
            if (permissionIds !== undefined && permissionIds !== null) {
                this.updatePermissions(role.roleId, permissionIds);
            }
        });
    }

    viewRoleDetails(role: RoleWithPermissions): void {
        this.dialog.open(RolePermissionDetailsDialogComponent, {
            width: '750px',
            data: { role, groupedPermissions: this.groupedPermissions },
            autoFocus: true
        });
    }

    /**
     * View users assigned to a specific role
     */
    viewRoleUsers(role: RoleWithPermissions): void {
        if (!role.userCount || role.userCount === 0) {
            this.snackBar.open(`No users assigned to "${role.roleName}"`, 'Close', {
                duration: 3000,
                horizontalPosition: 'end',
                verticalPosition: 'top'
            });
            return;
        }

        this.isLoading = true;
        
        this.rolePermissionService.getUsersByRole(role.roleId)
            .pipe(
                takeUntil(this.unsubscribe$),
                finalize(() => (this.isLoading = false))
            )
            .subscribe({
                next: (response) => {
                    if (response?.success && response.data) {
                        // Open dialog with users in this role
                        this.dialog.open(PermissionUsersDialogComponent, {
                            width: '700px',
                            maxHeight: '85vh',
                            data: {
                                permission: null,
                                role: role,
                                users: response.data,
                                title: `Users in ${role.roleName} Role`
                            },
                            autoFocus: true,
                            disableClose: false
                        });
                    } else {
                        this.showErrorAlert('Failed to load users for this role');
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

    private updatePermissions(roleId: string, permissionIds: string[]): void {
        const request: UpdateRolePermissionsRequest = { roleId, permissionIds };
        this.isLoading = true;

        this.rolePermissionService.updateRolePermissions(request)
            .pipe(
                takeUntil(this.unsubscribe$),
                finalize(() => (this.isLoading = false))
            )
            .subscribe({
                next: res => {
                    if (res?.success) {
                        this.showSuccessAlert(res.message || 'Permissions updated successfully');
                        this.loadData();
                    } else {
                        this.showErrorAlert(res?.message || 'Update failed');
                    }
                },
                error: err => {
                    this.showErrorAlert(
                        err?.error?.message || err?.message || 'Failed to update permissions'
                    );
                    console.error('Error updating permissions:', err);
                }
            });
    }

    removeAllPermissions(roleId: string, roleName: string): void {
        const confirmation = this.confirmationService.open({
            title: 'Remove All Permissions',
            message: `Are you sure you want to remove all permissions from "${roleName}"? This action cannot be undone.`,
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
                this.executeRemoveAllPermissions(roleId);
            }
        });
    }

    private executeRemoveAllPermissions(roleId: string): void {
        this.isLoading = true;

        this.rolePermissionService.removeAllPermissions(roleId)
            .pipe(
                takeUntil(this.unsubscribe$),
                finalize(() => (this.isLoading = false))
            )
            .subscribe({
                next: res => {
                    if (res?.success) {
                        this.showSuccessAlert('All permissions removed successfully');
                        this.loadData();
                    } else {
                        this.showErrorAlert(res?.message || 'Failed to remove permissions');
                    }
                },
                error: err => {
                    this.showErrorAlert(
                        err?.error?.message || err?.message || 'Failed to remove permissions'
                    );
                    console.error('Error removing permissions:', err);
                }
            });
    }

    // ------------------------------------------------------
    // Clone logic
    // ------------------------------------------------------

    setCloneSource(role: RoleWithPermissions): void {
        this.cloneSourceRole = role;
        this.showSuccessAlert(`Clone source set to "${role.roleName}"`);
    }

    clonePermissionsTo(target: RoleWithPermissions): void {
        if (!this.cloneSourceRole) {
            this.showErrorAlert('No clone source selected');
            return;
        }

        if (this.cloneSourceRole.roleId === target.roleId) {
            this.showErrorAlert('Cannot clone permissions to the same role');
            return;
        }

        const confirmation = this.confirmationService.open({
            title: 'Clone Permissions',
            message: `Clone ${this.cloneSourceRole.totalPermissions || 0} permissions from "${this.cloneSourceRole.roleName}" to "${target.roleName}"? This will replace all existing permissions in "${target.roleName}".`,
            icon: {
                show: true,
                name: 'heroicons_outline:clipboard-document-check',
                color: 'primary'
            },
            actions: {
                confirm: {
                    show: true,
                    label: 'Clone Permissions',
                    color: 'primary'
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
                this.executeClonePermissions(target);
            }
        });
    }

    private executeClonePermissions(target: RoleWithPermissions): void {
        if (!this.cloneSourceRole) return;

        const request: ClonePermissionsRequest = {
            sourceRoleId: this.cloneSourceRole.roleId,
            targetRoleId: target.roleId
        };

        this.isLoading = true;

        this.rolePermissionService.cloneRolePermissions(request)
            .pipe(
                takeUntil(this.unsubscribe$),
                finalize(() => (this.isLoading = false))
            )
            .subscribe({
                next: res => {
                    if (res?.success) {
                        this.showSuccessAlert(
                            `Permissions successfully cloned to "${target.roleName}"`
                        );
                        this.cloneSourceRole = null;
                        this.loadData();
                    } else {
                        this.showErrorAlert(res?.message || 'Clone operation failed');
                    }
                },
                error: err => {
                    this.showErrorAlert(
                        err?.error?.message || err?.message || 'Failed to clone permissions'
                    );
                    console.error('Error cloning permissions:', err);
                }
            });
    }

    cancelClone(): void {
        this.cloneSourceRole = null;
        this.showSuccessAlert('Clone mode cancelled');
    }

    // ------------------------------------------------------
    // Template Helper Methods
    // ------------------------------------------------------

    /**
     * Get initials from role name for avatar display
     */
    getRoleInitials(roleName: string): string {
        if (!roleName?.trim()) return '?';

        const words = roleName.trim().split(/\s+/).filter(w => w.length > 0);
        if (words.length === 0) return '?';
        if (words.length === 1) {
            return words[0].substring(0, 2).toUpperCase();
        }
        return (words[0][0] + words[1][0]).toUpperCase();
    }

    /**
     * Get unique permission groups for a role
     */
    getRolePermissionGroups(role: RoleWithPermissions): string[] {
        if (!role?.permissions?.length) {
            return [];
        }

        const groups = new Set<string>();
        role.permissions.forEach(p => {
            if (p?.groupName) {
                groups.add(p.groupName);
            }
        });

        return Array.from(groups).sort();
    }

    /**
     * Get permissions grouped by category for a specific role
     */
    getRolePermissionsGrouped(role: RoleWithPermissions): Record<string, Permission[]> {
        if (!role?.permissions || role.permissions.length === 0) {
            return {};
        }

        const grouped: Record<string, Permission[]> = {};

        role.permissions.forEach(permission => {
            const group = permission.groupName || 'Other';
            if (!grouped[group]) {
                grouped[group] = [];
            }
            grouped[group].push(permission);
        });

        return grouped;
    }

    /**
     * Get badge color class for a permission group
     */
    getGroupBadgeColor(groupName: string): string {
        if (!groupName) return this.badgeColors[0];

        if (!this.groupColorMap.has(groupName)) {
            this.initializeGroupColors();
        }
        return this.groupColorMap.get(groupName) || this.badgeColors[0];
    }

    /**
     * Get permission count for a specific group
     */
    getGroupPermissionCount(groupName: string): number {
        return this.groupedPermissions?.[groupName]?.length ?? 0;
    }

    /**
     * Initialize color mapping for permission groups
     */
    private initializeGroupColors(): void {
        if (!this.permissionGroups || this.permissionGroups.length === 0) {
            return;
        }

        this.permissionGroups.forEach((group, index) => {
            if (!this.groupColorMap.has(group)) {
                const colorIndex = index % this.badgeColors.length;
                this.groupColorMap.set(group, this.badgeColors[colorIndex]);
            }
        });
    }

    /**
     * Get total user count for all permissions in a group
     */
    getTotalGroupUserCount(groupName: string): number {
        if (!this.groupedPermissions?.[groupName]) {
            return 0;
        }
        
        return this.groupedPermissions[groupName].reduce(
            (total, permission) => total + (permission.userCount || 0),
            0
        );
    }

    /**
     * View users who have a specific permission
     */
    viewPermissionUsers(permission: Permission): void {
        if (!permission.userCount || permission.userCount === 0) {
            this.snackBar.open('No users have this permission', 'Close', {
                duration: 3000,
                horizontalPosition: 'end',
                verticalPosition: 'top'
            });
            return;
        }

        // Call the service to get users with this permission
        this.isLoading = true;
        
        this.rolePermissionService.getUsersWithPermission(permission.id || permission.permissionId!)
            .pipe(
                takeUntil(this.unsubscribe$),
                finalize(() => (this.isLoading = false))
            )
            .subscribe({
                next: (response) => {
                    if (response?.success && response.data) {
                        // Open dialog with users
                        this.dialog.open(PermissionUsersDialogComponent, {
                            width: '700px',
                            maxHeight: '85vh',
                            data: {
                                permission: permission,
                                users: response.data,
                                title: `Users with "${permission.displayName}" Permission`
                            },
                            autoFocus: true,
                            disableClose: false
                        });
                    } else {
                        this.showErrorAlert('Failed to load users with this permission');
                    }
                },
                error: (err) => {
                    this.showErrorAlert(
                        err?.error?.message || err?.message || 'Failed to load users'
                    );
                    console.error('Error loading users with permission:', err);
                }
            });
    }

    /**
     * Get permission user count statistics for display
     */
    getPermissionUserCountStats(): { 
        totalPermissions: number; 
        permissionsWithUsers: number; 
        totalUserAssignments: number;
    } {
        const stats = {
            totalPermissions: this.allPermissions.length,
            permissionsWithUsers: 0,
            totalUserAssignments: 0
        };

        this.allPermissions.forEach(permission => {
            const userCount = permission.userCount || 0;
            if (userCount > 0) {
                stats.permissionsWithUsers++;
            }
            stats.totalUserAssignments += userCount;
        });

        return stats;
    }

    /**
     * TrackBy function for table rows performance
     */
    trackByRoleId(index: number, role: RoleWithPermissions): string {
        return role?.roleId || index.toString();
    }

    /**
     * TrackBy function for permission groups
     */
    trackByGroup(index: number, group: string): string {
        return group || index.toString();
    }

    /**
     * TrackBy function for permissions
     */
    trackByPermissionId(index: number, permission: Permission): string {
        return permission?.permissionId || index.toString();
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

        // Error alerts remain visible until dismissed
        // Also show snackbar
        this.snackBar.open(message, 'Close', {
            duration: 5000,
            horizontalPosition: 'end',
            verticalPosition: 'top',
            panelClass: ['error-snackbar']
        });
    }

    dismissAlert(): void {
        this.showAlert = false;
    }
}