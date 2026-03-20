import {
  Component, OnInit, OnDestroy, AfterViewInit, ViewChild, TemplateRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subject, of } from 'rxjs';
import { catchError, takeUntil, finalize } from 'rxjs/operators';

import { AuthService } from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { SchoolDto } from 'app/Tenant/types/school';
import { LibraryBranchDto } from './Types/library-branch.types';

import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';
import { DataTableComponent, TableColumn, TableAction, TableHeader, TableEmptyState } from 'app/shared/data-table/data-table.component';
import { LibraryBranchService } from 'app/core/DevKenService/Library/library-branch.service';
import { CreateEditLibraryBranchDialogComponent } from 'app/dialog-modals/Library/library-branch/create-edit-library-branch-dialog.component';
import { BaseListComponent } from 'app/shared/Lists/BaseListComponent';

@Component({
  selector: 'app-library-branches',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatButtonModule, MatDialogModule, MatProgressSpinnerModule,
    PageHeaderComponent, FilterPanelComponent, PaginationComponent,
    StatsCardsComponent, DataTableComponent,
  ],
  templateUrl: './library-branches.component.html',
})
export class LibraryBranchesComponent
  extends BaseListComponent<LibraryBranchDto>
  implements OnInit, OnDestroy, AfterViewInit {

  private _destroy$ = new Subject<void>();
  private _alertService!: AlertService;
  private _branchService!: LibraryBranchService;
  private _schoolService!: SchoolService;

  @ViewChild('nameCell',    { static: true }) nameCell!:    TemplateRef<any>;
  @ViewChild('locationCell',{ static: true }) locationCell!: TemplateRef<any>;
  @ViewChild('copiesCell',  { static: true }) copiesCell!:  TemplateRef<any>;
  @ViewChild('schoolCell',  { static: true }) schoolCell!:  TemplateRef<any>;

  cellTemplates!: Record<string, TemplateRef<any>>;

  ngAfterViewInit(): void {
    this.cellTemplates = {
      name:     this.nameCell,
      location: this.locationCell,
      copies:   this.copiesCell,
      school:   this.schoolCell,
    };
  }

  // ── State ──────────────────────────────────────────────────────────────────
  schools: SchoolDto[] = [];
  isDataLoading   = true;
  showFilterPanel = false;
  currentPage     = 1;
  itemsPerPage    = 10;

  filterValues = { search: '', schoolId: 'all' };

  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Library',   url: '/library'   },
    { label: 'Branches' },
  ];

  get isSuperAdmin(): boolean { return this._injectedAuth.authUser?.isSuperAdmin ?? false; }

  get tableColumns(): TableColumn<LibraryBranchDto>[] {
    const cols: TableColumn<LibraryBranchDto>[] = [
      { id: 'name',     label: 'Branch Name', align: 'left', sortable: true },
      { id: 'location', label: 'Location',    align: 'left', hideOnMobile: true },
    ];
    if (this.isSuperAdmin) {
      cols.push({ id: 'school', label: 'School', align: 'left', hideOnMobile: true });
    }
    cols.push({ id: 'copies', label: 'Copies', align: 'center' });
    return cols;
  }

  tableActions: TableAction<LibraryBranchDto>[] = [
    {
      id: 'edit', label: 'Edit', icon: 'edit', color: 'blue',
      handler: r => this._openEdit(r),
    },
    {
      id: 'delete', label: 'Delete', icon: 'delete', color: 'red',
      handler: r => this._confirmDelete(r),
      divider: true,
    },
  ];

  tableHeader: TableHeader = {
    title: 'Library Branches',
    subtitle: '',
    icon: 'table_chart',
    iconGradient: 'bg-gradient-to-br from-teal-500 via-emerald-600 to-green-700',
  };

  tableEmptyState: TableEmptyState = {
    icon: 'store',
    message: 'No library branches found',
    description: 'Add your first branch to get started',
    action: { label: 'Add Branch', icon: 'add', handler: () => this._openCreate() },
  };

  filterFields: FilterField[] = [];

  // ── Stats ──────────────────────────────────────────────────────────────────
  get statsCards(): StatCard[] {
    const data = this.dataSource.data;
    const base: StatCard[] = [
      { label: 'Total Branches', value: data.length, icon: 'store', iconColor: 'teal' } as any,
      { label: 'Total Copies',   value: data.reduce((s, b) => s + b.totalCopies, 0),   icon: 'library_books', iconColor: 'blue'  },
      { label: 'Available',      value: data.reduce((s, b) => s + b.availableCopies, 0), icon: 'check_circle', iconColor: 'green' },
      { label: 'Checked Out',    value: data.reduce((s, b) => s + (b.totalCopies - b.availableCopies), 0), icon: 'assignment_return', iconColor: 'amber' },
    ];
    if (this.isSuperAdmin) {
      base.push({ label: 'Schools', value: new Set(data.map(b => b.schoolId)).size, icon: 'school', iconColor: 'violet' });
    }
    return base;
  }

  get filteredData(): LibraryBranchDto[] {
    const q = this.filterValues.search.toLowerCase();
    return this.dataSource.data.filter(b =>
      (!q || b.name?.toLowerCase().includes(q) || b.location?.toLowerCase().includes(q)) &&
      (this.filterValues.schoolId === 'all' || b.schoolId === this.filterValues.schoolId)
    );
  }

  get paginatedData(): LibraryBranchDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  constructor(
    dialog: MatDialog,
    snackBar: MatSnackBar,
    branchService: LibraryBranchService,
    private readonly _injectedAuth: AuthService,
    alertService: AlertService,
    schoolService: SchoolService,
  ) {
    super(branchService, dialog, snackBar);
    this._alertService  = alertService;
    this._branchService = branchService;
    this._schoolService = schoolService;
  }

  ngOnInit(): void    { this._loadMeta(); }
  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  protected override loadAll(): void {
    this.isLoading = true;
    const schoolId = this.filterValues.schoolId !== 'all' ? this.filterValues.schoolId : undefined;
    this._branchService.getAll(schoolId)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: res => {
          if (res.success) {
            this.dataSource.data = res.data;
            this.tableHeader.subtitle = `${this.filteredData.length} branches found`;
          } else {
            this._alertService.error(res.message || 'Failed to load branches');
          }
          this.isLoading = false;
        },
        error: err => {
          this._alertService.error(err.error?.message || 'Failed to load branches');
          this.isLoading = false;
        }
      });
  }

  private _loadMeta(): void {
    this.isDataLoading = true;
    if (!this.isSuperAdmin) {
      this.isDataLoading = false;
      this._initFilters();
      this.loadAll();
      return;
    }

    this._schoolService.getAll()
      .pipe(
        catchError(() => of({ success: false, data: [] })),
        takeUntil(this._destroy$),
        finalize(() => { this.isDataLoading = false; })
      )
      .subscribe((res: any) => {
        this.schools = res.data || [];
        this._initFilters();
        this.loadAll();
      });
  }

  private _initFilters(): void {
    this.filterFields = [
      { id: 'search', label: 'Search', type: 'text', placeholder: 'Branch name or location...', value: '' },
    ];
    if (this.isSuperAdmin) {
      this.filterFields.push({
        id: 'schoolId', label: 'School', type: 'select', value: 'all',
        options: [
          { label: 'All Schools', value: 'all' },
          ...this.schools.map(s => ({ label: s.name, value: s.id })),
        ],
      });
    }
  }

  toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

  onFilterChange(event: FilterChangeEvent): void {
    (this.filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} branches found`;
    if (event.filterId === 'schoolId' && this.isSuperAdmin) this.loadAll();
  }

  onClearFilters(): void {
    this.filterValues = { search: '', schoolId: 'all' };
    this.filterFields.forEach(f => { f.value = (this.filterValues as any)[f.id]; });
    this.currentPage = 1;
    this.loadAll();
  }

  onPageChange(page: number): void   { this.currentPage = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

  openCreate(): void { this._openCreate(); }

  private _openCreate(): void {
    this.openDialog(CreateEditLibraryBranchDialogComponent, {
      panelClass: ['library-branch-dialog', 'no-padding-dialog'],
      width: '520px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: true, autoFocus: 'input',
      data: { mode: 'create' },
    });
  }

  private _openEdit(branch: LibraryBranchDto): void {
    this.openDialog(CreateEditLibraryBranchDialogComponent, {
      panelClass: ['library-branch-dialog', 'no-padding-dialog'],
      width: '520px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: true, autoFocus: 'input',
      data: { mode: 'edit', branch },
    });
  }

  private _confirmDelete(branch: LibraryBranchDto): void {
    this._alertService.confirm({
      title:       'Delete Branch',
      message:     `Delete "${branch.name}"? This cannot be undone and will fail if the branch holds copies.`,
      confirmText: 'Delete',
      onConfirm:   () => this._doDelete(branch),
    });
  }

  private _doDelete(branch: LibraryBranchDto): void {
    this.isLoading = true;
    this._branchService.delete(branch.id).pipe(takeUntil(this._destroy$)).subscribe({
      next: res => {
        if (res.success) {
          this._alertService.success('Branch deleted successfully');
          if (this.paginatedData.length === 1 && this.currentPage > 1) this.currentPage--;
          this.loadAll();
        } else {
          this._alertService.error(res.message || 'Failed to delete');
        }
        this.isLoading = false;
      },
      error: err => {
        this._alertService.error(err.error?.message || 'Failed to delete branch');
        this.isLoading = false;
      }
    });
  }
}