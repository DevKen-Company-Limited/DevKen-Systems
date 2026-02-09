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
import { takeUntil } from 'rxjs/operators';
import { FuseAlertComponent } from '@fuse/components/alert';
import { CreateEditSchoolDialogComponent } from 'app/dialog-modals/Tenant/create-edit-school-dialog.component';
import { BaseListComponent } from 'app/shared/Lists/BaseListComponent';
import { SchoolDto } from './types/school';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { MatDividerModule } from '@angular/material/divider';
import { DatePipe } from '@angular/common';

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
    FuseAlertComponent
  ],
  templateUrl: './schools-management.component.html',
  providers: [DatePipe]
})
export class SchoolsManagementComponent extends BaseListComponent<SchoolDto> implements OnInit, OnDestroy {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  private _unsubscribe = new Subject<void>();

  displayedColumns: string[] = ['school', 'email', 'phone', 'status', 'created', 'actions'];
  
  // Filter state
  filterValue = '';
  statusFilter: 'all' | 'active' | 'inactive' = 'all';
  
  // Paging state (add these properties)
  pageSize = 20;
  currentPage = 0;
  total = 0;
  
  // Stats
  stats = {
    totalSchools: 0,
    activeSchools: 0,
    inactiveSchools: 0
  };

  // Alerts
  showAlert = false;
  alert = { type: 'success' as 'success' | 'error', message: '' };

  constructor(
    protected service: SchoolService, 
    protected dialog: MatDialog,
    protected snackBar: MatSnackBar,
    private datePipe: DatePipe
  ) {
    super(service, dialog, snackBar);
    this.dataSource = new MatTableDataSource<SchoolDto>();
  }

  ngOnInit(): void {
    this.init();
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  override init(): void {
    this.isLoading = true;
    this.service.getAll()
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
            
            // Update stats
            this.updateStats();
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
    
    // Update total based on filtered results
    if (this.dataSource.filteredData) {
      this.total = this.dataSource.filteredData.length;
    }
  }

  clearFilter(): void {
    this.filterValue = '';
    this.dataSource.filter = '';
    this.total = this.dataSource.data.length;
  }

  applyStatusFilter(): void {
    this.dataSource.filterPredicate = (data: SchoolDto, filter: string) => {
      const statusMatch = this.statusFilter === 'all' || 
                         (this.statusFilter === 'active' && data.isActive) ||
                         (this.statusFilter === 'inactive' && !data.isActive);
      
      const searchMatch = !this.filterValue || 
                         data.name.toLowerCase().includes(this.filterValue.toLowerCase()) ||
                         data.email?.toLowerCase().includes(this.filterValue.toLowerCase()) ||
                         data.phoneNumber?.toLowerCase().includes(this.filterValue.toLowerCase());
      
      return statusMatch && searchMatch;
    };
    
    this.dataSource.filter = 'trigger';
    
    // Update total based on filtered results
    if (this.dataSource.filteredData) {
      this.total = this.dataSource.filteredData.length;
    }
    
    // Reset to first page when filter changes
    if (this.paginator) {
      this.paginator.firstPage();
    }
  }

  openCreate(): void {
    this.openDialog(CreateEditSchoolDialogComponent, { 
      width: '600px', 
      data: { mode: 'create' } 
    });
  }

  openEdit(school: SchoolDto): void {
    this.openDialog(CreateEditSchoolDialogComponent, { 
      width: '600px', 
      data: { mode: 'edit', school } 
    });
  }

  removeSchool(school: SchoolDto): void {
    if (confirm(`Are you sure you want to delete "${school.name}"?`)) {
      this.isLoading = true;
      this.service.delete(school.id)
        .pipe(takeUntil(this._unsubscribe))
        .subscribe({
          next: (response) => {
            if (response?.success) {
              // Remove from data source
              const index = this.dataSource.data.findIndex(s => s.id === school.id);
              if (index > -1) {
                this.dataSource.data.splice(index, 1);
                this.dataSource._updateChangeSubscription();
                this.total = this.dataSource.data.length;
                this.calculateStats();
                this.showSuccessAlert('School deleted successfully');
              }
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

  onPageChange(event: PageEvent): void {
    this.currentPage = event.pageIndex;
    this.pageSize = event.pageSize;
    
    // If you want to implement server-side pagination later:
    // this.loadPaginatedData(event.pageIndex + 1, event.pageSize);
  }

  toggleSchoolStatus(school: SchoolDto): void {
    const newStatus = !school.isActive;
    const action = newStatus ? 'activate' : 'deactivate';
    
    if (confirm(`Are you sure you want to ${action} "${school.name}"?`)) {
      this.isLoading = true;
      this.service.updateStatus(school.id, newStatus)
        .pipe(takeUntil(this._unsubscribe))
        .subscribe({
          next: (response) => {
            if (response?.success) {
              // Update the school in the data source
              const index = this.dataSource.data.findIndex(s => s.id === school.id);
              if (index > -1) {
                this.dataSource.data[index].isActive = newStatus;
                this.dataSource._updateChangeSubscription();
                this.calculateStats();
                this.showSuccessAlert(`School ${action}d successfully`);
              }
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

  viewSchoolDetails(school: SchoolDto): void {
    // You can implement a details view dialog here
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
    this.stats.inactiveSchools = this.stats.totalSchools - this.stats.activeSchools;
  }

  private updateStats(): void {
    // If you need to update stats separately
    this.calculateStats();
  }

  private setupSorting(): void {
    this.dataSource.sortingDataAccessor = (item: SchoolDto, property: string) => {
      switch (property) {
        case 'school':
          return item.name.toLowerCase();
        case 'email':
          return item.email?.toLowerCase() || '';
        case 'phone':
          return item.phoneNumber || '';
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

  dismissAlert(): void {
    this.showAlert = false;
  }

  private showSuccessAlert(message: string): void {
    this.alert = { type: 'success', message };
    this.showAlert = true;
    setTimeout(() => this.showAlert = false, 5000);
    
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

  // Optional: Method for server-side pagination (if needed later)
  private loadPaginatedData(pageNumber: number, pageSize: number): void {
    this.isLoading = true;
    this.service.getPaginated(pageNumber, pageSize)
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next: (response) => {
          if (response?.success && response.data) {
            this.dataSource.data = response.data.items || [];
            this.total = response.data.totalCount || 0;
            this.calculateStats();
          }
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Error loading paginated schools:', error);
          this.showErrorAlert('Failed to load schools');
          this.isLoading = false;
        }
      });
  }

  // Refresh data
  refreshData(): void {
    this.init();
  }
}