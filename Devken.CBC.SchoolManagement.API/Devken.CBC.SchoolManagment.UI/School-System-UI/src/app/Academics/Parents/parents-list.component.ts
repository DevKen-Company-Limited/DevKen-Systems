import { Component, inject, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { ParentService } from 'app/core/DevKenService/Parents/Parent.service';
import { ParentFormDialogComponent } from 'app/dialog-modals/Parent/parent-form-dialog.component';
import { DataTableComponent, TableHeader, TableColumn, TableAction, TableEmptyState } from 'app/shared/data-table/data-table.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { BaseListComponent } from 'app/shared/Lists/BaseListComponent';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';
import { ParentSummaryDto, ParentRelationship, ParentQueryDto } from './Types/Parent.types';
import { Router } from '@angular/router';



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

  private _router        = inject(Router);
  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();

  // ── Header ──────────────────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Home', url: '/dashboard' },
    { label: 'People' },
    { label: 'Parents' },
  ];

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
    { id: 'relationship',   label: 'Relationship',  align: 'center' },
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
    {
      id: 'relationship',
      label: 'Relationship',
      type: 'select',
      value: 'all',
      options: [
        { label: 'All Relationships', value: 'all' },
        { label: 'Father', value: ParentRelationship.Father },
        { label: 'Mother', value: ParentRelationship.Mother },
        { label: 'Guardian', value: ParentRelationship.Guardian },
        { label: 'Sibling', value: ParentRelationship.Sibling },
        { label: 'Grandparent', value: ParentRelationship.Grandparent },
        { label: 'Uncle', value: ParentRelationship.Uncle },
        { label: 'Aunt', value: ParentRelationship.Aunt },
        { label: 'Other', value: ParentRelationship.Other },
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
    this.init();

    // debounce search
    this.searchSubject.pipe(
      debounceTime(400),
      distinctUntilChanged(),
      takeUntil(this.destroy$),
    ).subscribe(() => this.applyFilters());
  }

  ngAfterViewInit(): void {
    this.cellTemplates = {
      status:       this.statusTpl,
      badges:       this.badgesTpl,
      relationship: this.relationshipTpl,
    };
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  protected override loadAll(): void {
    this.isLoading = true;
    this.parentService.query(this.activeFilters).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success) {
          this.dataSource.data = res.data;
          this.buildStats(res.data);
          this.currentPage = 1;
        }
      },
      error: () => { this.isLoading = false; },
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
    }
  }

  onClearFilters(): void {
    this.activeFilters = {};
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

  // ── Pagination ────────────────────────────────────────────────────────────
  onPageChange(page: number): void { this.currentPage = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }
}