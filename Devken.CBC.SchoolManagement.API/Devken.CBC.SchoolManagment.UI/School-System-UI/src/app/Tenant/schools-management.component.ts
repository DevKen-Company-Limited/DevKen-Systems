import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
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
import { MatSelectModule } from '@angular/material/select';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil, take } from 'rxjs/operators';
import { FuseAlertComponent } from '@fuse/components/alert';
import { CreateEditSchoolDialogComponent } from 'app/dialog-modals/Tenant/create-edit-school-dialog.component';
import { BaseListComponent } from 'app/shared/Lists/BaseListComponent';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { MatDividerModule } from '@angular/material/divider';
import { DatePipe } from '@angular/common';
import { MatBadgeModule } from '@angular/material/badge';
import { SchoolWithSubscription, SubscriptionStatus } from './types/school';
import { ManageSubscriptionDialogComponent } from 'app/dialog-modals/Tenant/ManageSubscriptionDialogComponent';


@Component({
  selector: 'app-schools-management',
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
    MatProgressSpinnerModule,
    MatSelectModule,
    MatMenuModule,
    MatTooltipModule,
    MatDividerModule,
    MatBadgeModule,
    FuseAlertComponent
  ],
  templateUrl: './schools-management.component.html',
  providers: [DatePipe]
})
export class SchoolsManagementComponent implements OnInit, OnDestroy {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  private _unsubscribe = new Subject<void>();

  displayedColumns: string[] = ['school', 'subscription', 'status', 'created', 'actions'];
  
  // Filter state
  filterValue = '';
  statusFilter: 'all' | 'active' | 'inactive' | 'needsSubscription' = 'all';
  
  // Paging state
  pageSize = 20;
  currentPage = 0;
  total = 0;
  
  // Stats
  stats = {
    totalSchools: 0,
    activeSchools: 0,
    inactiveSchools: 0,
    withActiveSubscription: 0,
    withExpiredSubscription: 0,
    withoutSubscription: 0
  };

  // Alerts
  showAlert = false;
  alert = { type: 'success' as 'success' | 'error', message: '' };

  // Data source
  dataSource = new MatTableDataSource<SchoolWithSubscription>([]);
  isLoading = false;

  constructor(
    private service: SchoolService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private datePipe: DatePipe
  ) {}

  ngOnInit(): void {
    this.init();
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  init(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading = true;
    this.service.getAllWithSubscriptions()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next: (response) => {
          if (response?.success && response.data) {
            this.dataSource.data = response.data;
            this.total = response.data.length;
            this.calculateStats();
            
            // Set up sorting
            this.dataSource.sort = this.sort;
            this.dataSource.paginator = this.paginator;
            
            // Set up custom sorting
            this.setupSorting();
          } else {
            this.showErrorAlert(response?.message || 'Failed to load schools');
          }
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Error loading schools:', error);
          this.showErrorAlert('Failed to load schools');
          this.isLoading = false;
        }
      });
  }

  applyFilter(value: string): void {
    this.filterValue = value;
    this.dataSource.filter = value.trim().toLowerCase();
    this.updateFilteredTotal();
  }

  clearFilter(): void {
    this.filterValue = '';
    this.dataSource.filter = '';
    this.total = this.dataSource.data.length;
  }

  applyStatusFilter(): void {
    this.dataSource.filterPredicate = (data: SchoolWithSubscription, filter: string) => {
      let statusMatch = true;
      
      if (this.statusFilter === 'active') {
        statusMatch = data.isActive;
      } else if (this.statusFilter === 'inactive') {
        statusMatch = !data.isActive;
      } else if (this.statusFilter === 'needsSubscription') {
        statusMatch = data.needsSubscription || data.isSubscriptionExpired;
      }
      
      const searchMatch = !this.filterValue || 
                         data.name.toLowerCase().includes(this.filterValue.toLowerCase()) ||
                         data.email?.toLowerCase().includes(this.filterValue.toLowerCase()) ||
                         data.phoneNumber?.toLowerCase().includes(this.filterValue.toLowerCase());
      
      return statusMatch && searchMatch;
    };
    
    this.dataSource.filter = 'trigger';
    this.updateFilteredTotal();
    
    if (this.paginator) {
      this.paginator.firstPage();
    }
  }

  private updateFilteredTotal(): void {
    if (this.dataSource.filteredData) {
      this.total = this.dataSource.filteredData.length;
    }
  }

  openCreate(): void {
    const dialogRef = this.dialog.open(CreateEditSchoolDialogComponent, { 
      width: '600px', 
      data: { mode: 'create' } 
    });

    dialogRef.afterClosed().pipe(take(1)).subscribe((result) => {
      if (result?.success) {
        this.loadData();
      }
    });
  }

  openEdit(school: SchoolWithSubscription): void {
    const dialogRef = this.dialog.open(CreateEditSchoolDialogComponent, { 
      width: '600px', 
      data: { mode: 'edit', school } 
    });

    dialogRef.afterClosed().pipe(take(1)).subscribe((result) => {
      if (result?.success) {
        this.loadData();
      }
    });
  }

  removeSchool(school: SchoolWithSubscription): void {
    if (confirm(`Are you sure you want to delete "${school.name}"? This will also remove any associated subscription.`)) {
      this.isLoading = true;
      this.service.delete(school.id)
        .pipe(takeUntil(this._unsubscribe))
        .subscribe({
          next: (response) => {
            if (response?.success) {
              this.snackBar.open('School deleted successfully', 'Close', {
                duration: 3000,
                horizontalPosition: 'end',
                verticalPosition: 'top'
              });
              this.loadData();
            } else {
              this.showErrorAlert(response?.message || 'Failed to delete school');
            }
            this.isLoading = false;
          },
          error: (error) => {
            console.error('Error deleting school:', error);
            this.showErrorAlert('Failed to delete school');
            this.isLoading = false;
          }
        });
    }
  }

// Updated manageSubscription method for schools-management.component.ts

    manageSubscription(school: SchoolWithSubscription): void {
      const dialogRef = this.dialog.open(ManageSubscriptionDialogComponent, {
        width: '95vw',           // Responsive width - 95% of viewport
        maxWidth: '1400px',      // Maximum width for large screens
        maxHeight: '95vh',       // Prevent overflow on short screens
        panelClass: 'manage-subscription-dialog',
        data: { school }
      });

      dialogRef.afterClosed().pipe(take(1)).subscribe(result => {
        if (result?.refresh) {
          this.refreshSchoolData(school.id);
        }
      });
    }

  checkSubscriptionStatus(school: SchoolWithSubscription): void {
    this.service.checkSubscriptionStatus(school.id)
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next: (status) => {
          let snackBarRef = this.snackBar.open(
            `${school.name}: ${status.message}`,
            status.needsRenewal ? 'Renew Now' : 'OK',
            {
              duration: 5000,
              horizontalPosition: 'end',
              verticalPosition: 'top',
              panelClass: status.canAccess ? 'success-snackbar' : 'warn-snackbar'
            }
          );

          if (status.needsRenewal) {
            snackBarRef.onAction().subscribe(() => {
              this.manageSubscription(school);
            });
          }
        },
        error: (error) => {
          console.error('Error checking subscription status:', error);
          this.snackBar.open('Failed to check subscription status', 'Close', {
            duration: 3000,
            horizontalPosition: 'end',
            verticalPosition: 'top'
          });
        }
      });
  }

  private refreshSchoolData(schoolId: string): void {
    this.service.getSchoolWithSubscription(schoolId)
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next: (response) => {
          if (response?.success && response.data) {
            const index = this.dataSource.data.findIndex(s => s.id === schoolId);
            if (index > -1) {
              this.dataSource.data[index] = response.data;
              this.dataSource._updateChangeSubscription();
              this.calculateStats();
            }
          }
        },
        error: (error) => {
          console.error('Error refreshing school data:', error);
        }
      });
  }

  toggleSchoolStatus(school: SchoolWithSubscription): void {
    const newStatus = !school.isActive;
    const action = newStatus ? 'activate' : 'deactivate';
    
    if (confirm(`Are you sure you want to ${action} "${school.name}"?`)) {
      this.isLoading = true;
      this.service.updateStatus(school.id, newStatus)
        .pipe(takeUntil(this._unsubscribe))
        .subscribe({
          next: (response) => {
            if (response?.success) {
              this.snackBar.open(`School ${action}d successfully`, 'Close', {
                duration: 3000,
                horizontalPosition: 'end',
                verticalPosition: 'top'
              });
              this.loadData();
            } else {
              this.showErrorAlert(response?.message || `Failed to ${action} school`);
            }
            this.isLoading = false;
          },
          error: (error) => {
            console.error(`Error ${action}ing school:`, error);
            this.showErrorAlert(`Failed to ${action} school`);
            this.isLoading = false;
          }
        });
    }
  }

  viewSchoolDetails(school: SchoolWithSubscription): void {
    this.snackBar.open(`Viewing details for ${school.name}`, 'Close', {
      duration: 3000,
      horizontalPosition: 'end',
      verticalPosition: 'top'
    });
  }

  private calculateStats(): void {
    const schools = this.dataSource.data;
    this.stats.totalSchools = schools.length;
    this.stats.activeSchools = schools.filter(s => s.isActive).length;
    this.stats.inactiveSchools = schools.filter(s => !s.isActive).length;
    this.stats.withActiveSubscription = schools.filter(s => s.isSubscriptionActive).length;
    this.stats.withExpiredSubscription = schools.filter(s => s.isSubscriptionExpired).length;
    this.stats.withoutSubscription = schools.filter(s => !s.subscription || s.needsSubscription).length;
  }

  private setupSorting(): void {
    this.dataSource.sortingDataAccessor = (item: SchoolWithSubscription, property: string) => {
      switch (property) {
        case 'school':
          return item.name.toLowerCase();
        case 'subscription':
          return item.subscription?.status || 'No Subscription';
        case 'status':
          return item.isActive ? 1 : 0;
        case 'created':
          return new Date(item.createdOn).getTime();
        default:
          return '';
      }
    };
  }

  formatDate(dateString: string): string {
    if (!dateString) return '—';
    return this.datePipe.transform(dateString, 'MMM d, y') || '—';
  }

  getSubscriptionBadgeColor(school: SchoolWithSubscription): string {
    if (!school.subscription) return 'warn';
    
    switch (school.subscription.status) {
      case SubscriptionStatus.Active:
        return 'accent';
      case SubscriptionStatus.Expired:
        return 'warn';
      case SubscriptionStatus.Suspended:
        return 'warn';
      case SubscriptionStatus.Cancelled:
        return 'warn';
      case SubscriptionStatus.GracePeriod:
        return 'accent';
      default:
        return 'primary';
    }
  }

  

  getSubscriptionDisplay(school: SchoolWithSubscription): string {
    if (!school.subscription) return 'No Subscription';
    
    const plan = this.service.getSubscriptionPlanDisplay(school.subscription.plan);
    const cycle = this.service.getBillingCycleDisplay(school.subscription.billingCycle);
    const status = school.subscription.status;
    const days = school.daysRemaining > 0 ? ` (${school.daysRemaining}d)` : '';
    
    return `${plan} ${cycle} - ${status}${days}`;
  }

  dismissAlert(): void {
    this.showAlert = false;
  }

  private showSuccessAlert(message: string): void {
    this.alert = { type: 'success', message };
    this.showAlert = true;
    setTimeout(() => this.showAlert = false, 5000);
    
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
    
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      horizontalPosition: 'end',
      verticalPosition: 'top',
      panelClass: ['error-snackbar']
    });
  }

  onPageChange(event: PageEvent): void {
    this.currentPage = event.pageIndex;
    this.pageSize = event.pageSize;
  }

  refreshData(): void {
    this.loadData();
  }

  // Helper method to open dialog following BaseListComponent pattern
  private openDialog(component: any, config: any = {}) {
    const ref = this.dialog.open(component, config);
    ref.afterClosed().pipe(take(1)).subscribe((result) => {
      if (result?.success) {
        this.loadData();
      }
    });
    return ref;
  }
}