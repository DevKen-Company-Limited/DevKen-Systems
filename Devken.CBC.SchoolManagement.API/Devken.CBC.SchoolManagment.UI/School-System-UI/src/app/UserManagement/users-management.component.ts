import { Component, OnInit, OnDestroy, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { Subject } from 'rxjs';
import { takeUntil, take } from 'rxjs/operators';
import { FuseAlertComponent } from '@fuse/components/alert';
import { BaseListComponent } from 'app/shared/Lists/BaseListComponent';
import { UserService } from 'app/core/DevKenService/user/UserService';
import { CreateEditUserDialogComponent } from 'app/dialog-modals/users/create-edit-user-dialog.component';
import { UserDto } from 'app/core/DevKenService/Types/roles';

@Component({
  selector: 'app-users-management',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatIconModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatCardModule,
    MatSnackBarModule,
    MatChipsModule,
    MatTooltipModule,
    MatMenuModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    FuseAlertComponent
  ],
  templateUrl: './users-management.component.html',
  styleUrls: ['./users-management.component.scss']
})
export class UsersManagementComponent 
  extends BaseListComponent<UserDto> 
  implements OnInit, OnDestroy, AfterViewInit {
  
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  private _unsubscribe = new Subject<void>();

  displayedColumns: string[] = [
    'user', 
    'email', 
    'phone', 
    'roles', 
    'status', 
    'lastLogin', 
    'actions'
  ];

  pageSize = 20;
  currentPage = 1;
  total = 0;

  showAlert = false;
  alert = { 
    type: 'success' as 'success' | 'error', 
    message: '' 
  };

  filterValue = '';
  isSuperAdmin = false;

  constructor(
    protected service: UserService,
    protected dialog: MatDialog,
    protected snackBar: MatSnackBar
  ) {
    super(service, dialog, snackBar);
  }

  ngOnInit(): void {
    this.checkSuperAdminStatus();
    
    // Add school column for SuperAdmin
    if (this.isSuperAdmin) {
      this.displayedColumns = [
        'user', 
        'email',
        'school', // Add school column
        'phone', 
        'roles', 
        'status', 
        'lastLogin', 
        'actions'
      ];
    }
    
    this.init();
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  ngAfterViewInit(): void {
    if (this.paginator) {
      this.dataSource.paginator = this.paginator;
    }
    if (this.sort) {
      this.dataSource.sort = this.sort;
    }
  }

  /**
   * Check if current user is SuperAdmin by decoding JWT token
   */
  private checkSuperAdminStatus(): void {
    try {
      // Get token from localStorage or your auth service
      const token = localStorage.getItem('accessToken') || sessionStorage.getItem('accessToken');
      
      if (!token) {
        this.isSuperAdmin = false;
        return;
      }

      // Decode JWT token (simple base64 decode of payload)
      const payload = this.decodeJwtPayload(token);
      
      if (!payload) {
        this.isSuperAdmin = false;
        return;
      }

      // Check if user has SuperAdmin role
      // JWT includes role claim as either a string or array
      const roles = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      
      if (Array.isArray(roles)) {
        this.isSuperAdmin = roles.includes('SuperAdmin');
      } else if (typeof roles === 'string') {
        this.isSuperAdmin = roles === 'SuperAdmin';
      }

      // Also check the is_super_admin claim
      const isSuperAdminClaim = payload['is_super_admin'];
      if (isSuperAdminClaim === 'true' || isSuperAdminClaim === true) {
        this.isSuperAdmin = true;
      }

      console.log('SuperAdmin status:', this.isSuperAdmin);
    } catch (error) {
      console.error('Error checking SuperAdmin status:', error);
      this.isSuperAdmin = false;
    }
  }

  /**
   * Decode JWT payload
   */
  private decodeJwtPayload(token: string): any {
    try {
      const base64Url = token.split('.')[1];
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );
      return JSON.parse(jsonPayload);
    } catch (error) {
      console.error('Error decoding JWT:', error);
      return null;
    }
  }

  getExtraRolesTooltip(user: UserDto): string {
    if (!user?.roles || user.roles.length <= 2) {
      return '';
    }

    return user.roles
      .slice(2)
      .map(r => r.name)
      .join(', ');
  }

  applyFilter(value: string): void {
    this.filterValue = value;
    const v = (value || '').trim().toLowerCase();
    this.dataSource.filter = v;
  }

  clearFilter(): void {
    this.filterValue = '';
    this.dataSource.filter = '';
  }

  openCreate(): void {
    this.openDialog(CreateEditUserDialogComponent, { 
      width: '700px', 
      data: { 
        mode: 'create',
        isSuperAdmin: this.isSuperAdmin 
      } 
    });
  }

  openEdit(user: UserDto): void {
    this.openDialog(CreateEditUserDialogComponent, { 
      width: '700px', 
      data: { 
        mode: 'edit', 
        user,
        isSuperAdmin: this.isSuperAdmin 
      } 
    });
  }

  removeUser(user: UserDto): void {
    if (!confirm(`Are you sure you want to delete ${user.firstName} ${user.lastName}?`)) {
      return;
    }
    this.deleteItem(user.id);
  }

  toggleUserStatus(user: UserDto): void {
    const action = user.isActive ? 'deactivate' : 'activate';
    const confirmMessage = user.isActive 
      ? `Are you sure you want to deactivate ${user.firstName} ${user.lastName}?`
      : `Are you sure you want to activate ${user.firstName} ${user.lastName}?`;

    if (!confirm(confirmMessage)) {
      return;
    }

    this.isLoading = true;

    const toggleObservable = user.isActive 
      ? this.service.deactivateUser(user.id)
      : this.service.activateUser(user.id);

    toggleObservable
      .pipe(take(1))
      .subscribe({
        next: (res) => {
          this.isLoading = false;
          if (res.success) {
            const status = user.isActive ? 'deactivated' : 'activated';
            this.snackBar.open(
              `User ${status} successfully`, 
              'Close', 
              { duration: 2500 }
            );
            this.loadAll();
          } else {
            this.snackBar.open(
              res.message || 'Failed to update status', 
              'Close', 
              { duration: 3000 }
            );
          }
        },
        error: (err) => {
          this.isLoading = false;
          const errorMsg = err?.error?.message || err.message || 'Failed to update status';
          this.snackBar.open(errorMsg, 'Close', { duration: 4000 });
        }
      });
  }

  resendWelcomeEmail(user: UserDto): void {
    if (!confirm(`Send welcome email to ${user.email}?`)) {
      return;
    }

    this.isLoading = true;
    this.service.resendWelcomeEmail(user.id)
      .pipe(take(1))
      .subscribe({
        next: (res) => {
          this.isLoading = false;
          if (res.success) {
            this.snackBar.open(
              'Welcome email sent successfully', 
              'Close', 
              { duration: 2500 }
            );
          } else {
            this.snackBar.open(
              res.message || 'Failed to send email', 
              'Close', 
              { duration: 3000 }
            );
          }
        },
        error: (err) => {
          this.isLoading = false;
          const errorMsg = err?.error?.message || err.message || 'Failed to send email';
          this.snackBar.open(errorMsg, 'Close', { duration: 4000 });
        }
      });
  }

  resetPassword(user: UserDto): void {
    if (!confirm(`Send password reset email to ${user.email}?`)) {
      return;
    }

    this.isLoading = true;
    this.service.resetPassword(user.id)
      .pipe(take(1))
      .subscribe({
        next: (res) => {
          this.isLoading = false;
          if (res.success) {
            this.snackBar.open(
              'Password reset email sent', 
              'Close', 
              { duration: 2500 }
            );
          } else {
            this.snackBar.open(
              res.message || 'Failed to send reset email', 
              'Close', 
              { duration: 3000 }
            );
          }
        },
        error: (err) => {
          this.isLoading = false;
          const errorMsg = err?.error?.message || err.message || 'Failed to send reset email';
          this.snackBar.open(errorMsg, 'Close', { duration: 4000 });
        }
      });
  }

  onPageChange(event: PageEvent): void {
    this.currentPage = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    // If using server-side paging, call loadAll() with pagination params
  }

  getInitials(user: UserDto): string {
    const firstInitial = user.firstName?.charAt(0) || '';
    const lastInitial = user.lastName?.charAt(0) || '';
    return `${firstInitial}${lastInitial}`.toUpperCase();
  }

  formatDate(date: string | undefined): string {
    if (!date) return 'Never';
    return new Date(date).toLocaleDateString('en-US', { 
      month: 'short', 
      day: 'numeric', 
      year: 'numeric' 
    });
  }
}