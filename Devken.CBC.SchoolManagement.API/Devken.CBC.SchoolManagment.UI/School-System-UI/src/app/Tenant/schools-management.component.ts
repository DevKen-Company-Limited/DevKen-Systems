import { Component, OnInit, OnDestroy, ViewChild, inject } from '@angular/core';
import { CommonModule, DatePipe, AsyncPipe } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { MatSelectModule } from '@angular/material/select';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil, take } from 'rxjs/operators';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { SchoolDto, SchoolType, SchoolCategory } from './types/school';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { API_BASE_URL } from 'app/app.config';
import { SecureImagePipe } from './pipe/SecureImagePipe';
import { CreateEditSchoolDialogComponent } from 'app/dialog-modals/Tenant/create-edit-school-dialog.component';
import { SchoolViewDialogComponent } from 'app/dialog-modals/Student/school-view-dialog.component';



@Component({
  selector: 'app-schools-management',
  standalone: true,
  imports: [
    CommonModule,
    AsyncPipe,
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
    MatSelectModule,
    MatMenuModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    SecureImagePipe,
  ],
  templateUrl: './schools-management.component.html',
  providers: [DatePipe]
})
export class SchoolsManagementComponent implements OnInit, OnDestroy {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  private baseUrl = inject(API_BASE_URL);
  private _unsubscribe = new Subject<void>();

  displayedColumns: string[] = ['school', 'type', 'status', 'created', 'actions'];

  filterValue  = '';
  statusFilter: 'all' | 'active' | 'inactive' = 'all';
  pageSize    = 20;
  currentPage = 0;
  total       = 0;

  stats = { totalSchools: 0, activeSchools: 0, inactiveSchools: 0 };
  dataSource = new MatTableDataSource<SchoolDto>([]);
  isLoading  = false;

  readonly schoolTypeLabels: Record<number, string> = {
    [SchoolType.Public]:        'Public School',
    [SchoolType.Private]:       'Private School',
    [SchoolType.International]: 'International School',
    [SchoolType.NGO]:           'NGO / Mission School'
  };
  readonly categoryLabels: Record<number, string> = {
    [SchoolCategory.Day]:      'Day School',
    [SchoolCategory.Boarding]: 'Boarding School',
    [SchoolCategory.Mixed]:    'Mixed (Day & Boarding)'
  };

  constructor(
    private service:      SchoolService,
    private dialog:       MatDialog,
    private alertService: AlertService,
    private datePipe:     DatePipe,
  ) {}

  ngOnInit(): void  { this.loadData(); }
  ngOnDestroy(): void { this._unsubscribe.next(); this._unsubscribe.complete(); }

  // ─── Data ─────────────────────────────────────────────────────────────────
  loadData(): void {
    this.isLoading = true;
    this.service.getAll().pipe(takeUntil(this._unsubscribe)).subscribe({
      next: (response) => {
        if (response?.success && response.data) {
          this.dataSource.data = response.data;
          this.total = response.data.length;
          this.calculateStats();
          this.dataSource.sort      = this.sort;
          this.dataSource.paginator = this.paginator;
          this.setupSorting();
        } else {
          this.alertService.error(response?.message || 'Failed to load schools', 'Load Error');
        }
        this.isLoading = false;
      },
      error: () => {
        this.alertService.error('An unexpected error occurred while loading schools.', 'Load Error');
        this.isLoading = false;
      }
    });
  }

  refreshData(): void { this.loadData(); }

  getLogoUrl(logoUrl: string | null | undefined): string | null {
    if (!logoUrl) return null;
    if (logoUrl.startsWith('http://') || logoUrl.startsWith('https://')) return logoUrl;
    const base = this.baseUrl.replace(/\/$/, '');
    const path = logoUrl.startsWith('/') ? logoUrl : `/${logoUrl}`;
    return `${base}${path}`;
  }

  getSchoolTypeLabel(schoolType: number | string | null | undefined): string {
    if (schoolType == null) return '—';
    const key = typeof schoolType === 'string' ? parseInt(schoolType, 10) : schoolType;
    return this.schoolTypeLabels[key] ?? `Type ${schoolType}`;
  }

  getCategoryLabel(category: number | string | null | undefined): string {
    if (category == null) return '—';
    const key = typeof category === 'string' ? parseInt(category, 10) : category;
    return this.categoryLabels[key] ?? `Category ${category}`;
  }

  // ─── Filtering ────────────────────────────────────────────────────────────
  applyFilter(value: string): void { this.filterValue = value; this.applyFilters(); }
  clearFilter(): void {
    this.filterValue = ''; this.statusFilter = 'all';
    this.applyFilters();
    if (this.paginator) this.paginator.firstPage();
  }
  applyStatusFilter(): void { this.applyFilters(); if (this.paginator) this.paginator.firstPage(); }

  private applyFilters(): void {
    this.dataSource.filterPredicate = (data: SchoolDto, _filter: string) => {
      let statusMatch = true;
      if (this.statusFilter === 'active')   statusMatch =  data.isActive;
      if (this.statusFilter === 'inactive') statusMatch = !data.isActive;
      const q = this.filterValue.toLowerCase();
      const searchMatch = !q ||
        data.name.toLowerCase().includes(q) ||
        (data.email       ?? '').toLowerCase().includes(q) ||
        (data.phoneNumber ?? '').toLowerCase().includes(q) ||
        (data.slugName    ?? '').toLowerCase().includes(q);
      return statusMatch && searchMatch;
    };
    this.dataSource.filter = this.filterValue || (this.statusFilter !== 'all' ? '_' : '');
    this.updateFilteredTotal();
  }

  private updateFilteredTotal(): void {
    this.total = this.dataSource.filteredData?.length ?? this.dataSource.data.length;
  }

  // ─── CRUD ─────────────────────────────────────────────────────────────────

  /** Read-only viewer. Clicking "Edit School" inside the viewer chains to openEdit(). */
  openView(school: SchoolDto): void {
    const ref = this.dialog.open(SchoolViewDialogComponent, {
      data: { school }
    });
    ref.afterClosed().pipe(take(1)).subscribe((result) => {
      if (result?.action === 'edit') this.openEdit(result.school);
    });
  }

  openCreate(): void {
    const ref = this.dialog.open(CreateEditSchoolDialogComponent, {
      data: { mode: 'create' }
    });
    ref.afterClosed().pipe(take(1)).subscribe((result) => {
      if (result?.success) {
        this.loadData();
        this.alertService.success('School created successfully.', 'Created');
      }
    });
  }

  openEdit(school: SchoolDto): void {
    const ref = this.dialog.open(CreateEditSchoolDialogComponent, {
      data: { mode: 'edit', school }
    });
    ref.afterClosed().pipe(take(1)).subscribe((result) => {
      if (result?.success) {
        this.loadData();
        this.alertService.success('School updated successfully.', 'Updated');
      }
    });
  }

  removeSchool(school: SchoolDto): void {
    this.alertService.confirm({
      title: 'Delete School',
      message: `Are you sure you want to delete "${school.name}"? This action cannot be undone.`,
      confirmText: 'Delete', cancelText: 'Cancel',
      onConfirm: () => {
        this.isLoading = true;
        this.service.delete(school.id).pipe(takeUntil(this._unsubscribe)).subscribe({
          next: (response) => {
            if (response?.success) {
              this.loadData();
              this.alertService.success(`"${school.name}" was deleted successfully.`, 'Deleted');
            } else {
              this.alertService.error(response?.message || 'Failed to delete school.', 'Error');
            }
            this.isLoading = false;
          },
          error: () => { this.alertService.error('An unexpected error occurred.', 'Error'); this.isLoading = false; }
        });
      }
    });
  }

  toggleSchoolStatus(school: SchoolDto): void {
    const newStatus = !school.isActive;
    const action    = newStatus ? 'activate' : 'deactivate';
    this.alertService.confirm({
      title: newStatus ? 'Activate School' : 'Deactivate School',
      message: `Are you sure you want to ${action} "${school.name}"?`,
      confirmText: newStatus ? 'Activate' : 'Deactivate', cancelText: 'Cancel',
      onConfirm: () => {
        this.isLoading = true;
        this.service.updateStatus(school.id, newStatus).pipe(takeUntil(this._unsubscribe)).subscribe({
          next: (response) => {
            if (response?.success) {
              this.loadData();
              this.alertService.success(`School ${action}d successfully.`, 'Status Updated');
            } else {
              this.alertService.error(response?.message || `Failed to ${action} school.`, 'Error');
            }
            this.isLoading = false;
          },
          error: () => { this.alertService.error(`An error occurred while trying to ${action} the school.`, 'Error'); this.isLoading = false; }
        });
      }
    });
  }

  // ─── Stats & Sorting ──────────────────────────────────────────────────────
  private calculateStats(): void {
    const schools = this.dataSource.data;
    this.stats.totalSchools    = schools.length;
    this.stats.activeSchools   = schools.filter(s =>  s.isActive).length;
    this.stats.inactiveSchools = schools.filter(s => !s.isActive).length;
  }

  private setupSorting(): void {
    this.dataSource.sortingDataAccessor = (item: SchoolDto, property: string) => {
      switch (property) {
        case 'school':  return item.name.toLowerCase();
        case 'type':    return this.getSchoolTypeLabel(item.schoolType);
        case 'status':  return item.isActive ? 1 : 0;
        case 'created': return new Date(item.createdOn).getTime();
        default:        return '';
      }
    };
  }

  formatDate(dateString: string): string {
    if (!dateString) return '—';
    return this.datePipe.transform(dateString, 'MMM d, y') || '—';
  }

  onPageChange(event: PageEvent): void {
    this.currentPage = event.pageIndex;
    this.pageSize    = event.pageSize;
  }
}