import { Component, OnInit, OnDestroy, ViewChild, inject } from '@angular/core';
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
import { Subject, takeUntil, finalize, debounceTime } from 'rxjs';
import { FuseAlertComponent, FuseAlertType } from '@fuse/components/alert';
import { RoleAssignmentService } from 'app/core/DevKenService/Roles/RoleAssignmentService';
import { UpdateUserRolesRequest, UserRole, UserWithRoles } from 'app/core/DevKenService/Types/roles';
import { RoleManageDialogComponent } from 'app/dialog-modals/roles/role-manage-dialog-component';
import { UserDetailsDialogComponent } from 'app/dialog-modals/roles/user-details-dialog-component';

@Component({
    selector: 'app-role-assignment-enhanced',
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
export class RoleAssignmentEnhancedComponent implements OnInit, OnDestroy {
    @ViewChild(MatPaginator) paginator!: MatPaginator;
    @ViewChild(MatSort) sort!: MatSort;

    private _roleAssignmentService = inject(RoleAssignmentService);
    private _dialog = inject(MatDialog);
    private _snackBar = inject(MatSnackBar);
    private _unsubscribeAll = new Subject<void>();

    // State
    availableRoles: UserRole[] = [];
    allUsers: UserWithRoles[] = [];
    isLoading = false;
    selectedTabIndex = 0;
    
    // Alert
    showAlert = false;
    alert: { type: FuseAlertType; message: string } = {
        type: 'success',
        message: ''
    };

    // Users Table
    displayedColumns: string[] = ['user', 'email', 'roles', 'actions'];
    usersDataSource = new MatTableDataSource<UserWithRoles>([]);
    userSearchTerm = '';
    roleFilterValue: string | null = null;
    
    // Pagination
    totalUsers = 0;
    currentPage = 1;
    pageSize = 20;

    // Statistics
    stats = {
        totalRoles: 0,
        totalUsers: 0
    };

    private searchSubject = new Subject<string>();

    ngOnInit(): void {
        this.loadData();
        this.setupSearch();
    }

    ngOnDestroy(): void {
        this._unsubscribeAll.next();
        this._unsubscribeAll.complete();
    }

    /**
     * Setup search with debounce
     */
    private setupSearch(): void {
        this.searchSubject
            .pipe(
                debounceTime(300),
                takeUntil(this._unsubscribeAll)
            )
            .subscribe(() => {
                this.applyUserFilter();
            });
    }

    /**
     * Load all data
     */
    loadData(): void {
        this.loadAvailableRoles();
        this.loadAllUsers();
    }

    /**
     * Load available roles
     */
    private loadAvailableRoles(): void {
        this._roleAssignmentService.getAvailableRoles()
            .pipe(
                takeUntil(this._unsubscribeAll),
                finalize(() => {})
            )
            .subscribe({
                next: (response) => {
                    if (response.success) {
                        this.availableRoles = response.data;
                        this.stats.totalRoles = response.data.length;
                        this.loadRoleUserCounts();
                    }
                },
                error: (error) => {
                    this.showErrorAlert('Failed to load roles: ' + (error.error?.message || error.message));
                }
            });
    }

    /**
     * Load all users with pagination
     */
    loadAllUsers(): void {
        this.isLoading = true;

        this._roleAssignmentService.getAllUsersWithRoles(this.currentPage, this.pageSize)
            .pipe(
                takeUntil(this._unsubscribeAll),
                finalize(() => this.isLoading = false)
            )
            .subscribe({
                next: (response) => {
                    if (response.success) {
                        this.allUsers = response.data.items;
                        this.totalUsers = response.data.totalCount;
                        this.stats.totalUsers = response.data.totalCount;
                        this.applyUserFilter();
                    }
                },
                error: (error) => {
                    this.showErrorAlert('Failed to load users: ' + (error.error?.message || error.message));
                }
            });
    }

    /**
     * Load user counts for each role
     */
    private loadRoleUserCounts(): void {
        this.availableRoles.forEach(role => {
            this._roleAssignmentService.getUsersByRole(role.roleId, 1, 1)
                .pipe(takeUntil(this._unsubscribeAll))
                .subscribe({
                    next: (response) => {
                        if (response.success) {
                            role.userCount = response.data.totalCount;
                        }
                    }
                });
        });
    }

    /**
     * Apply filters to user table
     */
    applyUserFilter(): void {
        let filteredData = [...this.allUsers];

        // Apply search filter
        if (this.userSearchTerm && this.userSearchTerm.trim()) {
            const searchLower = this.userSearchTerm.toLowerCase().trim();
            filteredData = filteredData.filter(user =>
                user.fullName.toLowerCase().includes(searchLower) ||
                user.email.toLowerCase().includes(searchLower) ||
                user.userName.toLowerCase().includes(searchLower)
            );
        }

        // Apply role filter
        if (this.roleFilterValue) {
            filteredData = filteredData.filter(user =>
                user.roles.some(role => role.roleId === this.roleFilterValue)
            );
        }

        this.usersDataSource.data = filteredData;
    }

    /**
     * Clear user filter
     */
    clearUserFilter(): void {
        this.userSearchTerm = '';
        this.roleFilterValue = null;
        this.applyUserFilter();
    }

    /**
     * Handle page change
     */
    onPageChange(event: PageEvent): void {
        this.currentPage = event.pageIndex + 1;
        this.pageSize = event.pageSize;
        this.loadAllUsers();
    }

    /**
     * Open manage roles dialog
     */
    openManageRolesDialog(user: UserWithRoles): void {
        const dialogRef = this._dialog.open(RoleManageDialogComponent, {
            width: '700px',
            maxHeight: '90vh',
            data: {
                user: user,
                availableRoles: this.availableRoles
            }
        });

        dialogRef.afterClosed().subscribe(result => {
            if (result) {
                this.handleRoleUpdate(user.userId, result);
            }
        });
    }

    /**
     * Handle role update from dialog
     */
/**
 * Handle role update from dialog
 */
private handleRoleUpdate(userId: string, selectedRoleIds: string[]): void {
    this.isLoading = true;

    const request: UpdateUserRolesRequest = {
        userId: userId,
        roleIds: selectedRoleIds
    };

    this._roleAssignmentService.updateUserRoles(request)
        .pipe(
            takeUntil(this._unsubscribeAll),
            finalize(() => this.isLoading = false)
        )
        .subscribe({
            next: (response) => {
                if (response.success) {
                    this.showSuccessAlert(response.message);
                    this.loadAllUsers();
                    this.loadRoleUserCounts(); // Refresh role counts
                } else {
                    this.showErrorAlert(response.message);
                }
            },
            error: (error) => {
                this.showErrorAlert('Failed to update roles: ' + (error.error?.message || error.message));
            }
        });
}

    /**
     * View user details
     */
    viewUserDetails(user: UserWithRoles): void {
        this._dialog.open(UserDetailsDialogComponent, {
            width: '600px',
            maxHeight: '90vh',
            data: { user: user }
        });
    }

    /**
     * View users for a specific role
     */
    viewRoleUsers(role: UserRole): void {
        this.selectedTabIndex = 0;
        this.roleFilterValue = role.roleId;
        setTimeout(() => {
            this.applyUserFilter();
        }, 100);
    }

    /**
     * Remove all roles from user
     */
    removeAllRoles(userId: string): void {
        if (!confirm('Are you sure you want to remove all roles from this user?')) {
            return;
        }

        this.isLoading = true;

        this._roleAssignmentService.removeAllRoles(userId)
            .pipe(
                takeUntil(this._unsubscribeAll),
                finalize(() => this.isLoading = false)
            )
            .subscribe({
                next: (response) => {
                    if (response.success) {
                        this.showSuccessAlert('All roles removed successfully');
                        this.loadAllUsers();
                    } else {
                        this.showErrorAlert(response.message);
                    }
                },
                error: (error) => {
                    this.showErrorAlert('Failed to remove roles: ' + (error.error?.message || error.message));
                }
            });
    }

    /**
     * Get user initials for avatar
     */
    getUserInitials(fullName: string): string {
        if (!fullName) return '??';
        const names = fullName.split(' ');
        if (names.length >= 2) {
            return (names[0][0] + names[names.length - 1][0]).toUpperCase();
        }
        return fullName.substring(0, 2).toUpperCase();
    }

    /**
     * Show success alert
     */
    private showSuccessAlert(message: string): void {
        this.alert = {
            type: 'success',
            message: message
        };
        this.showAlert = true;
        setTimeout(() => this.showAlert = false, 5000);
    }

    /**
     * Show error alert
     */
    private showErrorAlert(message: string): void {
        this.alert = {
            type: 'error',
            message: message
        };
        this.showAlert = true;
    }
}