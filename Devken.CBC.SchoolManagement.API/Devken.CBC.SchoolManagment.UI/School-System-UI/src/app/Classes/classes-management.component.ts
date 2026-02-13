import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
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
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil, take } from 'rxjs';
import { FuseAlertComponent } from '@fuse/components/alert';
import { ClassService } from 'app/core/DevKenService/ClassService';
import { CreateEditClassDialogComponent } from 'app/dialog-modals/Classes/create-edit-class-dialog.component';
import { ClassDto } from './Types/Class';

@Component({
  selector: 'app-classes-management',
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
    MatBadgeModule,
    MatDividerModule,
    MatProgressBarModule,
    FuseAlertComponent
  ],
  templateUrl: './classes-management.component.html',
  providers: [DatePipe]
})
export class ClassesManagementComponent implements OnInit, OnDestroy {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  private _unsubscribe = new Subject<void>();

  // All data
  allData: ClassDto[] = [];
  
  // Filter state
  filterValue = '';
  levelFilter: number | 'all' = 'all';
  statusFilter: 'all' | 'active' | 'inactive' | 'full' = 'all';
  showFilterPanel = false;
  
  // Paging state
  pageSize = 20;
  currentPage = 0;
  
  // Stats
  stats = {
    totalClasses: 0,
    activeClasses: 0,
    inactiveClasses: 0,
    fullClasses: 0,
    totalCapacity: 0,
    totalEnrollment: 0
  };

  // Alerts
  showAlert = false;
  alert = { type: 'success' as 'success' | 'error', message: '' };

  // Data source
  dataSource = new MatTableDataSource<ClassDto>([]);
  isLoading = false;

  // CBC Levels for filter
  cbcLevels: { value: number; label: string }[] = [];

  // Computed property for total
  get total(): number {
    return this.dataSource.filteredData.length;
  }

  constructor(
    private service: ClassService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private datePipe: DatePipe
  ) {}

  ngOnInit(): void {
    this.cbcLevels = this.service.getAllCBCLevels();
    this.loadAll();
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  loadAll(): void {
    this.isLoading = true;
    this.service.getAll()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next: (response) => {
          if (response?.success && response.data) {
            this.allData = response.data;
            this.dataSource.data = response.data;
            this.calculateStats();
            
            // Set up sorting and pagination
            this.dataSource.sort = this.sort;
            this.dataSource.paginator = this.paginator;
            
            // Set up custom sorting
            this.setupSorting();
            
            // Apply current filters
            this.applyFilters();
          } else {
            this.showErrorAlert(response?.message || 'Failed to load classes');
          }
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Error loading classes:', error);
          this.showErrorAlert('Failed to load classes');
          this.isLoading = false;
        }
      });
  }

  applyFilter(value: string): void {
    this.filterValue = value;
    this.applyFilters();
  }

  clearFilter(): void {
    this.filterValue = '';
    this.levelFilter = 'all';
    this.statusFilter = 'all';
    this.applyFilters();
  }

  applyFilters(): void {
    this.dataSource.filterPredicate = (data: ClassDto, filter: string) => {
      // Level filter
      let levelMatch = true;
      if (this.levelFilter !== 'all') {
        levelMatch = data.level === this.levelFilter;
      }
      
      // Status filter
      let statusMatch = true;
      if (this.statusFilter === 'active') {
        statusMatch = data.isActive;
      } else if (this.statusFilter === 'inactive') {
        statusMatch = !data.isActive;
      } else if (this.statusFilter === 'full') {
        statusMatch = data.isFull;
      }
      
      // Search filter
      const searchMatch = !this.filterValue || 
                         data.name.toLowerCase().includes(this.filterValue.toLowerCase()) ||
                         data.code.toLowerCase().includes(this.filterValue.toLowerCase()) ||
                         data.levelName?.toLowerCase().includes(this.filterValue.toLowerCase()) ||
                         data.teacherName?.toLowerCase().includes(this.filterValue.toLowerCase());
      
      return levelMatch && statusMatch && searchMatch;
    };
    
    this.dataSource.filter = 'trigger';
    
    if (this.paginator) {
      this.paginator.firstPage();
    }
  }

  openCreate(): void {
    const dialogRef = this.dialog.open(CreateEditClassDialogComponent, { 
      width: '800px', 
      data: { mode: 'create' } 
    });

    dialogRef.afterClosed().pipe(take(1)).subscribe((result) => {
      if (result?.success) {
        this.loadAll();
        this.showSuccessAlert(result.message || 'Class created successfully');
      }
    });
  }

  openEdit(classItem: ClassDto): void {
    const dialogRef = this.dialog.open(CreateEditClassDialogComponent, { 
      width: '800px', 
      data: { mode: 'edit', class: classItem } 
    });

    dialogRef.afterClosed().pipe(take(1)).subscribe((result) => {
      if (result?.success) {
        this.loadAll();
        this.showSuccessAlert(result.message || 'Class updated successfully');
      }
    });
  }

  removeClass(classItem: ClassDto): void {
    if (classItem.currentEnrollment > 0) {
      this.snackBar.open(
        `Cannot delete "${classItem.name}" - it has ${classItem.currentEnrollment} enrolled students. Please reassign students first.`,
        'Close',
        {
          duration: 5000,
          horizontalPosition: 'end',
          verticalPosition: 'top',
          panelClass: ['warn-snackbar']
        }
      );
      return;
    }

    if (confirm(`Are you sure you want to delete "${classItem.name}"?`)) {
      this.isLoading = true;
      this.service.delete(classItem.id)
        .pipe(takeUntil(this._unsubscribe))
        .subscribe({
          next: (response) => {
            if (response?.success) {
              this.loadAll();
              this.showSuccessAlert('Class deleted successfully');
            } else {
              this.showErrorAlert(response?.message || 'Failed to delete class');
            }
            this.isLoading = false;
          },
          error: (error) => {
            console.error('Error deleting class:', error);
            this.showErrorAlert('Failed to delete class');
            this.isLoading = false;
          }
        });
    }
  }

  viewClassDetails(classItem: ClassDto): void {
    this.service.getById(classItem.id, true)
      .pipe(take(1))
      .subscribe({
        next: (response) => {
          if (response?.success && response.data) {
            this.snackBar.open(`Viewing details for ${classItem.name}`, 'Close', {
              duration: 3000,
              horizontalPosition: 'end',
              verticalPosition: 'top'
            });
          }
        },
        error: (error) => {
          console.error('Error loading class details:', error);
        }
      });
  }

  private calculateStats(): void {
    const classes = this.allData;
    this.stats.totalClasses = classes.length;
    this.stats.activeClasses = classes.filter(c => c.isActive).length;
    this.stats.inactiveClasses = classes.filter(c => !c.isActive).length;
    this.stats.fullClasses = classes.filter(c => c.isFull).length;
    this.stats.totalCapacity = classes.reduce((sum, c) => sum + c.capacity, 0);
    this.stats.totalEnrollment = classes.reduce((sum, c) => sum + c.currentEnrollment, 0);
  }

  private setupSorting(): void {
    this.dataSource.sortingDataAccessor = (item: ClassDto, property: string) => {
      switch (property) {
        case 'class':
          return item.name.toLowerCase();
        case 'level':
          return item.level;
        case 'capacity':
          return item.currentEnrollment;
        case 'teacher':
          return item.teacherName?.toLowerCase() || '';
        case 'academicYear':
          return item.academicYearName?.toLowerCase() || '';
        case 'status':
          return item.isActive ? 1 : 0;
        default:
          return '';
      }
    };
  }

  formatDate(dateString: Date): string {
    if (!dateString) return '—';
    return this.datePipe.transform(dateString, 'MMM d, y') || '—';
  }

  getCapacityUtilization(classItem: ClassDto): number {
    return this.service.getCapacityUtilization(classItem.currentEnrollment, classItem.capacity);
  }

  getCapacityColor(classItem: ClassDto): string {
    return this.service.getCapacityStatusColor(classItem.currentEnrollment, classItem.capacity);
  }

  getLevelDisplay(level: number): string {
    return this.service.getCBCLevelDisplay(level);
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
    this.loadAll();
  }
}