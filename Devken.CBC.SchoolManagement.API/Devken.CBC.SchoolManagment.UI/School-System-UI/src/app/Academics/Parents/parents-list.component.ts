import { Component, inject, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { ParentService } from 'app/core/DevKenService/Parents/Parent.service';
import { DataTableComponent, TableHeader, TableColumn, TableAction, TableEmptyState } from 'app/shared/data-table/data-table.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { BaseListComponent } from 'app/shared/Lists/BaseListComponent';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';
import { ParentSummaryDto, ParentRelationship, ParentQueryDto } from './Types/Parent.types';
import { Router } from '@angular/router';
import { AuthService } from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { SchoolDto } from 'app/Tenant/types/school';

@Component({
  selector: 'app-parents-list',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatButtonModule,
    PageHeaderComponent,
    StatsCardsComponent,
    DataTableComponent,
    FilterPanelComponent,
    PaginationComponent,
  ],
  templateUrl: './parents-list.component.html',
})
export class ParentsListComponent extends BaseListComponent<ParentSummaryDto> implements OnInit {
  @ViewChild('statusTpl', { static: true }) statusTpl!: TemplateRef<any>;
  @ViewChild('badgesTpl', { static: true }) badgesTpl!: TemplateRef<any>;
  @ViewChild('relationshipTpl', { static: true }) relationshipTpl!: TemplateRef<any>;
  @ViewChild('actionsTpl', { static: true }) actionsTpl!: TemplateRef<any>;

  private _router = inject(Router);
  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();
  private _authService = inject(AuthService);
  private _schoolService = inject(SchoolService);

  // ── Header ──────────────────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Home', url: '/dashboard' },
    { label: 'People' },
    { label: 'Parents' },
  ];

  // ── SuperAdmin State ──────────────────────────────────────────────────────────
  get isSuperAdmin(): boolean {
    return this._authService.authUser?.isSuperAdmin ?? false;
  }

  // ✅ Schools for SuperAdmin filter
  schools: SchoolDto[] = [];
  selectedSchoolId: string | null = null;

  // ── Stats ────────────────────────────────────────────────────────────────
  statsCards: StatCard[] = [];

  // ── Table ────────────────────────────────────────────────────────────────
  tableHeader: TableHeader = {
    title: 'Parents & Guardians',
    subtitle: 'All registered parents and guardians',
    icon: 'family_restroom',
    iconGradient: 'bg-gradient-to-br from-teal-500 via-emerald-600 to-green-700',
  };

  columns: TableColumn<ParentSummaryDto>[] = [
    { id: 'fullName',       label: 'Name',         sortable: true },
    { id: 'relationshipDisplay',   label: 'Relationship',  align: 'center' },
    { id: 'phoneNumber',    label: 'Phone',         hideOnMobile: true },
    { id: 'email',          label: 'Email',         hideOnMobile: true, hideOnTablet: true },
    { id: 'studentCount',   label: 'Students',      align: 'center' },
    { id: 'badges',         label: 'Flags',         align: 'center', hideOnMobile: true },
    { id: 'status',         label: 'Status',        align: 'center' },
  ];

  actions: TableAction<ParentSummaryDto>[] = [
    {
      id: 'edit',
      label: 'Edit',
      icon: 'edit',
      color: 'indigo',
      handler: (row) => this.openEdit(row),
    },
    {
      id: 'delete',
      label: 'Delete',
      icon: 'delete',
      color: 'red',
      divider: true,
      handler: (row) => this.onDelete(row),
    },
  ];

  emptyState: TableEmptyState = {
    icon: 'family_restroom',
    message: 'No parents found',
    description: 'Add a parent or guardian to get started.',
    action: {
      label: 'Add Parent',
      icon: 'add',
      handler: () => this.openCreate(),
    },
  };

  cellTemplates: { [key: string]: TemplateRef<any> } = {};

  // ── Filter ───────────────────────────────────────────────────────────────
  showFilters = false;
  filterFields: FilterField[] = [
    {
      id: 'searchTerm',
      label: 'Search',
      type: 'text',
      placeholder: 'Name, email or phone...',
      value: '',
    },
    // School filter will be inserted dynamically for SuperAdmin after schools load
    {
      id: 'relationship',
      label: 'Relationship',
      type: 'select',
      value: 'all',
      options: [
        { label: 'All Relationships', value: 'all' },
        { label: 'Father',      value: ParentRelationship.Father      },
        { label: 'Mother',      value: ParentRelationship.Mother      },
        { label: 'Guardian',    value: ParentRelationship.Guardian    },
        { label: 'Sponsor',     value: ParentRelationship.Sponsor     }, // ← replaces Sibling
        { label: 'Grandparent', value: ParentRelationship.Grandparent },
        { label: 'Other',       value: ParentRelationship.Other       },
      ],
    },
    {
      id: 'isActive',
      label: 'Status',
      type: 'select',
      value: 'all',
      options: [
        { label: 'All', value: 'all' },
        { label: 'Active', value: 'true' },
        { label: 'Inactive', value: 'false' },
      ],
    },
    {
      id: 'hasPortalAccess',
      label: 'Portal Access',
      type: 'select',
      value: 'all',
      options: [
        { label: 'All', value: 'all' },
        { label: 'Has Access', value: 'true' },
        { label: 'No Access', value: 'false' },
      ],
    },
  ];

  activeFilters: ParentQueryDto = {};

  // ── Pagination ───────────────────────────────────────────────────────────
  currentPage = 1;
  itemsPerPage = 10;
  allData: ParentSummaryDto[] = [];

  get filteredData(): ParentSummaryDto[] {
    return this.dataSource.data;
  }

  get paginatedData(): ParentSummaryDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  constructor(
    private parentService: ParentService,
    dialog: MatDialog,
    snackBar: MatSnackBar,
    private alertService: AlertService,
  ) {
    super(parentService, dialog, snackBar);
  }

  ngOnInit(): void {
     this.cellTemplates = {
    status:              this.statusTpl,
    badges:              this.badgesTpl,
    relationshipDisplay: this.relationshipTpl,
  };
    // For non‑SuperAdmin, we can init immediately
    if (!this.isSuperAdmin) {
      this.init();
    }

    // debounce search
    this.searchSubject.pipe(
      debounceTime(400),
      distinctUntilChanged(),
      takeUntil(this.destroy$),
    ).subscribe(() => this.applyFilters());

    // ✅ Load schools for SuperAdmin (this will also trigger data load)
    if (this.isSuperAdmin) {
      this.loadSchools();
    }
  }

  ngAfterViewInit(): void {
    this.cellTemplates = {
      status:       this.statusTpl,
      badges:       this.badgesTpl,
      relationshipDisplay: this.relationshipTpl, 
    };
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ✅ Load schools and add school filter
  private loadSchools(): void {
    this._schoolService.getAll().pipe(takeUntil(this.destroy$)).subscribe({
      next: (res: any) => {
        this.schools = res.data ?? [];
        if (this.schools.length > 0) {
          // Default to first school (required by backend)
          this.selectedSchoolId = this.schools[0].id;
          this.addSchoolFilter();
          // Now that schoolId is set, we can load data
          this.init(); // This will call loadAll() with selectedSchoolId
        } else {
          // No schools available – show empty state or disable
          this.isLoading = false;
        }
      },
      error: (err) => {
        console.error('Failed to load schools', err);
        this.alertService.error('Could not load schools. Please refresh.');
        this.isLoading = false;
      }
    });
  }

  // ✅ Add school filter to filterFields with a new array reference
  private addSchoolFilter(): void {
    const schoolFilter: FilterField = {
      id: 'schoolId',
      label: 'School',
      type: 'select',
      value: this.selectedSchoolId,
      options: this.schools.map(s => ({ label: s.name, value: s.id })),
    };
    // Insert after search filter (index 1) – create new array to trigger change detection
    this.filterFields = [
      this.filterFields[0],        // search filter
      schoolFilter,
      ...this.filterFields.slice(1) // the rest
    ];
  }

  protected override loadAll(): void {
    // For SuperAdmin, wait until a school is selected
    if (this.isSuperAdmin && !this.selectedSchoolId) {
      this.isLoading = false;
      return;
    }

    this.isLoading = true;
    // For SuperAdmin, always pass selectedSchoolId; for others, undefined
    const schoolId = this.isSuperAdmin ? this.selectedSchoolId : undefined;
    this.parentService.query(this.activeFilters, schoolId).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success) {
          this.dataSource.data = res.data;
          this.buildStats(res.data);
          this.currentPage = 1;
        }
      },
      error: () => {
        this.isLoading = false;
      },
    });
  }

  private buildStats(data: ParentSummaryDto[]): void {
    const total     = data.length;
    const primary   = data.filter(p => p.isPrimaryContact).length;
    const emergency = data.filter(p => p.isEmergencyContact).length;
    const portal    = data.filter(p => p.hasPortalAccess).length;

    this.statsCards = [
      { label: 'Total Parents',      value: total,     icon: 'family_restroom', iconColor: 'indigo' },
      { label: 'Primary Contacts',   value: primary,   icon: 'star',            iconColor: 'amber' },
      { label: 'Emergency Contacts', value: emergency, icon: 'emergency',       iconColor: 'red' },
      { label: 'Portal Access',      value: portal,    icon: 'manage_accounts', iconColor: 'green' },
    ];
  }

  // ── Filters ──────────────────────────────────────────────────────────────
  onFilterChange(event: FilterChangeEvent): void {
    if (event.filterId === 'searchTerm') {
      this.activeFilters.searchTerm = event.value || undefined;
      this.searchSubject.next(event.value);
    } else if (event.filterId === 'relationship') {
      this.activeFilters.relationship = event.value === 'all' ? undefined : Number(event.value);
      this.applyFilters();
    } else if (event.filterId === 'isActive') {
      this.activeFilters.isActive = event.value === 'all' ? undefined : event.value === 'true';
      this.applyFilters();
    } else if (event.filterId === 'hasPortalAccess') {
      this.activeFilters.hasPortalAccess = event.value === 'all' ? undefined : event.value === 'true';
      this.applyFilters();
    } else if (event.filterId === 'schoolId') {
      this.selectedSchoolId = event.value; // value is school id (no 'all' option)
      this.applyFilters();
    }
  }

  onClearFilters(): void {
    this.activeFilters = {};

    if (this.isSuperAdmin && this.schools.length > 0) {
      this.selectedSchoolId = this.schools[0].id; // reset to default school

      // Update the school filter's value in the filterFields array (create new array)
      const schoolIndex = this.filterFields.findIndex(f => f.id === 'schoolId');
      if (schoolIndex !== -1) {
        this.filterFields = [
          ...this.filterFields.slice(0, schoolIndex),
          { ...this.filterFields[schoolIndex], value: this.selectedSchoolId },
          ...this.filterFields.slice(schoolIndex + 1)
        ];
      }
    }

    this.applyFilters();
  }

  private applyFilters(): void {
    this.loadAll();
  }

  // ── CRUD ─────────────────────────────────────────────────────────────────
  openCreate(): void {
    this._router.navigate(['/academic/parents/create']);
  }

  openEdit(row: ParentSummaryDto): void {
    this._router.navigate(['/academic/parents/edit', row.id]);
  }

  onDelete(row: ParentSummaryDto): void {
    this.alertService.confirm({
      title: 'Delete Parent',
      message: `Are you sure you want to delete ${row.fullName}? This action cannot be undone.`,
      confirmText: 'Delete',
      cancelText: 'Cancel',
      onConfirm: () => this.deleteItem(row.id),
    });
  }

  protected override deleteItem(id: string): void {
  this.parentService.delete(id).subscribe({
    next: (res) => {
      if (res.success) {
        // Remove from local data immediately (optimistic update)
        this.dataSource.data = this.dataSource.data.filter(item => item.id !== id);
        this.buildStats(this.dataSource.data);

        this.snackBar.open('Parent deleted successfully', 'Close', { duration: 3000 });

        // Reload from backend to stay in sync
        this.loadAll();
      } else {
        this.alertService.error(res.message || 'Failed to delete parent');
      }
    },
    error: (err) => {
      const status = err?.status;
      if (status === 409) {
        this.alertService.error(
          'Cannot delete this parent because they are still linked to students. Remove the associations first.'
        );
      } else if (status === 403) {
        this.alertService.error('You do not have permission to delete this parent.');
      } else if (status === 404) {
        this.alertService.error('Parent not found. It may have already been deleted.');
        this.loadAll(); // Refresh to remove stale entry
      } else {
        console.error('Delete error', err);
        this.alertService.error('An error occurred while deleting the parent.');
      }
    }
  });
}

  // ── Pagination ────────────────────────────────────────────────────────────
  onPageChange(page: number): void { this.currentPage = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }
}