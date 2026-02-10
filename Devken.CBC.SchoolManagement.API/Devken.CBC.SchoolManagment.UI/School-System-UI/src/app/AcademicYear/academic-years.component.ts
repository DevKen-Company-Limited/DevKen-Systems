import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
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
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FuseAlertComponent } from '@fuse/components/alert';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { FuseConfirmationService } from '@fuse/services/confirmation';
import { CreateEditAcademicYearDialogComponent } from 'app/dialog-modals/Academic Year/create-edit-academic-year-dialog.component';
import { AcademicYearService } from 'app/core/DevKenService/AcademicYearService/AcademicYearService';
import { AcademicYearDto } from './Types/AcademicYear';

@Component({
  selector: 'app-academic-years',
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
    MatMenuModule,
    MatProgressSpinnerModule,
    //FuseAlertComponent
  ],
  templateUrl: './academic-years.component.html',
})
export class AcademicYearsComponent implements OnInit, OnDestroy {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  private _unsubscribe = new Subject<void>();

  // All data
  allData: AcademicYearDto[] = [];
  
  // Filter properties
  searchQuery: string = '';
  selectedStatus: string = 'all';
  selectedYear: string = 'all';
  showFilterPanel: boolean = false;

  // Available filter options
  availableYears: string[] = [];

  isLoading = false;
  
  // Pagination
  currentPage: number = 1;
  itemsPerPage: number = 10;

  showAlert = false;
  alert = { type: 'success' as 'success' | 'error', message: '' };

  // Computed properties for stats
  get total(): number {
    return this.allData.length;
  }

  get currentYearName(): string {
    const current = this.allData.find(ay => ay.isCurrent);
    return current ? current.name : 'None';
  }

  get openYearsCount(): number {
    return this.allData.filter(ay => !ay.isClosed).length;
  }

  // Filtered data based on search and filters
  get filteredData(): AcademicYearDto[] {
    return this.allData.filter(ay => {
      const matchesSearch = this.searchQuery === '' ||
        ay.name.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        ay.code.toLowerCase().includes(this.searchQuery.toLowerCase());

      const matchesStatus = this.selectedStatus === 'all' ||
        (this.selectedStatus === 'current' && ay.isCurrent) ||
        (this.selectedStatus === 'closed' && ay.isClosed) ||
        (this.selectedStatus === 'open' && !ay.isClosed && !ay.isCurrent);

      let matchesYear = true;
      if (this.selectedYear !== 'all') {
        const startYear = new Date(ay.startDate).getFullYear().toString();
        matchesYear = startYear === this.selectedYear;
      }

      return matchesSearch && matchesStatus && matchesYear;
    });
  }

  // Paginated data
  get paginatedData(): AcademicYearDto[] {
    if (this.currentPage > this.totalPages) {
      this.currentPage = Math.max(1, this.totalPages);
    }
    const startIndex = (this.currentPage - 1) * this.itemsPerPage;
    const endIndex = startIndex + this.itemsPerPage;
    return this.filteredData.slice(startIndex, endIndex);
  }

  get totalPages(): number {
    return Math.ceil(this.filteredData.length / this.itemsPerPage);
  }

  get showingFrom(): number {
    return this.filteredData.length === 0 ? 0 : (this.currentPage - 1) * this.itemsPerPage + 1;
  }

  get showingTo(): number {
    const to = this.currentPage * this.itemsPerPage;
    return to > this.filteredData.length ? this.filteredData.length : to;
  }

  constructor(
    private _service: AcademicYearService,
    private _dialog: MatDialog,
    private _snackBar: MatSnackBar,
    private _confirmationService: FuseConfirmationService
  ) {}

  ngOnInit(): void {
    this.loadAll();
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
        next: (response) => {
          if (response.success) {
            this.allData = response.data;
            this.extractAvailableYears();
          }
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Failed to load academic years', error);
          this.showErrorAlert('Failed to load academic years');
          this.isLoading = false;
        }
      });
  }

  extractAvailableYears(): void {
    const years = new Set<string>();
    this.allData.forEach(ay => {
      const year = new Date(ay.startDate).getFullYear().toString();
      years.add(year);
    });
    this.availableYears = Array.from(years).sort((a, b) => b.localeCompare(a));
  }

  toggleFilterPanel(): void {
    this.showFilterPanel = !this.showFilterPanel;
  }

  applyFilters(): void {
    this.currentPage = 1; // Reset to first page when filters change
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.selectedStatus = 'all';
    this.selectedYear = 'all';
    this.currentPage = 1;
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
    }
  }

  openCreate(): void {
    const dialogRef = this._dialog.open(CreateEditAcademicYearDialogComponent, {
      width: '600px',
      data: { mode: 'create' }
    });

    dialogRef.afterClosed()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe((result) => {
        if (result) {
          this._service.create(result)
            .pipe(takeUntil(this._unsubscribe))
            .subscribe({
              next: (response) => {
                if (response.success) {
                  this.showSuccessAlert('Academic year created successfully');
                  this.loadAll();
                }
              },
              error: (error) => {
                console.error('Failed to create academic year', error);
                this.showErrorAlert(error.error?.message || 'Failed to create academic year');
              }
            });
        }
      });
  }

  openEdit(academicYear: AcademicYearDto): void {
    if (academicYear.isClosed) {
      this.showErrorAlert('Cannot edit a closed academic year');
      return;
    }

    const dialogRef = this._dialog.open(CreateEditAcademicYearDialogComponent, {
      width: '600px',
      data: { mode: 'edit', academicYear }
    });

    dialogRef.afterClosed()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe((result) => {
        if (result) {
          this._service.update(academicYear.id, result)
            .pipe(takeUntil(this._unsubscribe))
            .subscribe({
              next: (response) => {
                if (response.success) {
                  this.showSuccessAlert('Academic year updated successfully');
                  this.loadAll();
                }
              },
              error: (error) => {
                console.error('Failed to update academic year', error);
                this.showErrorAlert(error.error?.message || 'Failed to update academic year');
              }
            });
        }
      });
  }

  setAsCurrent(academicYear: AcademicYearDto): void {
    const confirmation = this._confirmationService.open({
      title: 'Set as Current',
      message: `Are you sure you want to set "${academicYear.name}" as the current academic year? This will unset any other current academic year.`,
      actions: {
        confirm: {
          label: 'Set as Current'
        }
      }
    });

    confirmation.afterClosed()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe((result) => {
        if (result === 'confirmed') {
          this._service.setAsCurrent(academicYear.id)
            .pipe(takeUntil(this._unsubscribe))
            .subscribe({
              next: (response) => {
                if (response.success) {
                  this.showSuccessAlert('Academic year set as current');
                  this.loadAll();
                }
              },
              error: (error) => {
                console.error('Failed to set academic year as current', error);
                this.showErrorAlert(error.error?.message || 'Failed to set as current');
              }
            });
        }
      });
  }

  closeYear(academicYear: AcademicYearDto): void {
    const confirmation = this._confirmationService.open({
      title: 'Close Academic Year',
      message: `Are you sure you want to close "${academicYear.name}"? This action cannot be undone and the year will no longer be editable.`,
      actions: {
        confirm: {
          label: 'Close Year',
          color: 'warn'
        }
      }
    });

    confirmation.afterClosed()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe((result) => {
        if (result === 'confirmed') {
          this._service.close(academicYear.id)
            .pipe(takeUntil(this._unsubscribe))
            .subscribe({
              next: (response) => {
                if (response.success) {
                  this.showSuccessAlert('Academic year closed successfully');
                  this.loadAll();
                }
              },
              error: (error) => {
                console.error('Failed to close academic year', error);
                this.showErrorAlert(error.error?.message || 'Failed to close academic year');
              }
            });
        }
      });
  }

  removeAcademicYear(academicYear: AcademicYearDto): void {
    const confirmation = this._confirmationService.open({
      title: 'Delete Academic Year',
      message: `Are you sure you want to delete "${academicYear.name}"? This action cannot be undone.`,
      actions: {
        confirm: {
          label: 'Delete',
          color: 'warn'
        }
      }
    });

    confirmation.afterClosed()
      .pipe(takeUntil(this._unsubscribe))
      .subscribe((result) => {
        if (result === 'confirmed') {
          this._service.delete(academicYear.id)
            .pipe(takeUntil(this._unsubscribe))
            .subscribe({
              next: (response) => {
                if (response.success) {
                  this.showSuccessAlert('Academic year deleted successfully');
                  this.loadAll();
                }
              },
              error: (error) => {
                console.error('Failed to delete academic year', error);
                this.showErrorAlert(error.error?.message || 'Failed to delete academic year');
              }
            });
        }
      });
  }

  private showSuccessAlert(message: string): void {
    this.alert = { type: 'success', message };
    this.showAlert = true;
    setTimeout(() => this.showAlert = false, 5000);
    this._snackBar.open(message, 'Close', { duration: 3000 });
  }

  private showErrorAlert(message: string): void {
    this.alert = { type: 'error', message };
    this.showAlert = true;
    setTimeout(() => this.showAlert = false, 5000);
    this._snackBar.open(message, 'Close', { duration: 5000 });
  }
}