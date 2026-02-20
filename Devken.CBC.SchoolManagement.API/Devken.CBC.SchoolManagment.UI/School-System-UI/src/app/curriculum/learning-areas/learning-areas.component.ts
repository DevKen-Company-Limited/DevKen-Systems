import { Component, OnInit, OnDestroy, inject, AfterViewInit, TemplateRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';

import { AuthService } from 'app/core/auth/auth.service';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';
import { DataTableComponent, TableColumn, TableAction, TableHeader, TableEmptyState } from 'app/shared/data-table/data-table.component';
import { CBCLevelOptions } from '../types/curriculum-enums';
import { LearningAreaService } from 'app/core/DevKenService/curriculum/learning-area.service';
import { LearningAreaResponseDto } from '../types/learning-area.dto ';

@Component({
  selector: 'app-learning-areas',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatChipsModule,
    PageHeaderComponent,
    FilterPanelComponent,
    PaginationComponent,
    StatsCardsComponent,
    DataTableComponent,
  ],
  templateUrl: './learning-areas.component.html',
})
export class LearningAreasComponent implements OnInit, OnDestroy,  AfterViewInit {
  private _destroy$ = new Subject<void>();
  private _service = inject(LearningAreaService);
  private _authService = inject(AuthService);
  private _alertService = inject(AlertService);
  private _router = inject(Router);

  @ViewChild('nameCell', { static: true }) nameCell!: TemplateRef<any>;
  @ViewChild('codeCell', { static: true }) codeCell!: TemplateRef<any>;
  @ViewChild('levelCell', { static: true }) levelCell!: TemplateRef<any>;
  @ViewChild('statusCell', { static: true }) statusCell!: TemplateRef<any>;

  cellTemplates!: Record<string, TemplateRef<any>>;

  ngAfterViewInit(): void {
    this.cellTemplates = {
      name: this.nameCell,
      code: this.codeCell,
      level: this.levelCell,
      status: this.statusCell,
    };
  }

  // ─── Auth ──────────────────────────────────────────────────────────
  get isSuperAdmin(): boolean {
    return this._authService.authUser?.isSuperAdmin ?? false;
  }

  // ─── Breadcrumbs ───────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Curriculum' },
    { label: 'Learning Areas' },
  ];

  // ─── State ─────────────────────────────────────────────────────────
  allData: LearningAreaResponseDto[] = [];
  isLoading = false;
  showFilterPanel = false;
  currentPage = 1;
  itemsPerPage = 10;

  private _filterValues = { search: '', level: 'all', status: 'all' };

  cbcLevelOptions = CBCLevelOptions;

  // ─── Stats ─────────────────────────────────────────────────────────
  get statsCards(): StatCard[] {
    return [
      { label: 'Total Areas',  value: this.allData.length,                                       icon: 'menu_book',     iconColor: 'indigo' },
      { label: 'Active',       value: this.allData.filter(a => a.status === 'Active').length,    icon: 'check_circle',  iconColor: 'green'  },
      { label: 'Inactive',     value: this.allData.filter(a => a.status !== 'Active').length,    icon: 'pause_circle',  iconColor: 'amber'  },
    ];
  }

  // ─── Table ─────────────────────────────────────────────────────────
  tableColumns: TableColumn<LearningAreaResponseDto>[] = [
    { id: 'name',   label: 'Name',     align: 'left',   sortable: true },
    { id: 'code',   label: 'Code',     align: 'left',   hideOnMobile: true },
    { id: 'level',  label: 'CBC Level', align: 'left',  hideOnMobile: true },
    { id: 'status', label: 'Status',   align: 'center' },
  ];

  tableActions: TableAction<LearningAreaResponseDto>[] = [
    { id: 'edit',    label: 'Edit',    icon: 'edit',   color: 'indigo', handler: r => this.edit(r)   },
    { id: 'strands', label: 'View Strands', icon: 'account_tree', color: 'blue', handler: r => this.viewStrands(r) },
    { id: 'delete',  label: 'Delete',  icon: 'delete', color: 'red',    handler: r => this.delete(r) },
  ];

  tableHeader: TableHeader = {
    title:        'Learning Areas',
    subtitle:     '',
    icon:         'menu_book',
    iconGradient: 'bg-gradient-to-br from-indigo-500 via-violet-600 to-purple-700',
  };

  tableEmptyState: TableEmptyState = {
    icon:        'menu_book',
    message:     'No learning areas found',
    description: 'Create your first learning area to get started',
    action: { label: 'Add Learning Area', icon: 'add', handler: () => this.create() },
  };

  // ─── Filter Fields ─────────────────────────────────────────────────
  filterFields: FilterField[] = [
    {
      id: 'search', label: 'Search', type: 'text',
      placeholder: 'Search by name or code...', value: '',
    },
    {
      id: 'level', label: 'CBC Level', type: 'select', value: 'all',
      options: [
        { label: 'All Levels', value: 'all' },
        ...CBCLevelOptions.map(l => ({ label: l.label, value: l.value.toString() })),
      ],
    },
    {
      id: 'status', label: 'Status', type: 'select', value: 'all',
      options: [
        { label: 'All Statuses', value: 'all' },
        { label: 'Active',       value: 'Active' },
        { label: 'Inactive',     value: 'Inactive' },
      ],
    },
  ];

  // ─── Computed ──────────────────────────────────────────────────────
  get filteredData(): LearningAreaResponseDto[] {
    const q = this._filterValues.search.toLowerCase();
    return this.allData.filter(a =>
      (!q || a.name.toLowerCase().includes(q) || a.code?.toLowerCase().includes(q)) &&
      (this._filterValues.level === 'all' || a.level === this._filterValues.level ||
        this.cbcLevelOptions.find(l => l.value.toString() === this._filterValues.level)?.label === a.level) &&
      (this._filterValues.status === 'all' || a.status === this._filterValues.status)
    );
  }

  get paginatedData(): LearningAreaResponseDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  // ─── Lifecycle ─────────────────────────────────────────────────────
  ngOnInit(): void { this.loadAll(); }
  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  // ─── Data ──────────────────────────────────────────────────────────
  loadAll(): void {
    this.isLoading = true;
    this._service.getAll()
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: data => {
          this.allData = Array.isArray(data) ? data : [];
          this.tableHeader.subtitle = `${this.filteredData.length} learning areas`;
          this.isLoading = false;
        },
        error: err => {
          this._alertService.error(err?.error?.message || 'Failed to load learning areas');
          this.isLoading = false;
        },
      });
  }

  // ─── Filter events ─────────────────────────────────────────────────
  toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

  onFilterChange(event: FilterChangeEvent): void {
    (this._filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} learning areas`;
  }

  onClearFilters(): void {
    this._filterValues = { search: '', level: 'all', status: 'all' };
    this.filterFields.forEach(f => { f.value = (this._filterValues as any)[f.id]; });
    this.currentPage = 1;
  }

  onPageChange(page: number): void { this.currentPage = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

  // ─── Actions ───────────────────────────────────────────────────────
  create():                              void { this._router.navigate(['/curriculum/learning-areas/create']); }
  edit(row: LearningAreaResponseDto):    void { this._router.navigate(['/curriculum/learning-areas/edit', row.id]); }
  viewStrands(row: LearningAreaResponseDto): void {
    this._router.navigate(['/curriculum/strands'], { queryParams: { learningAreaId: row.id } });
  }

  delete(row: LearningAreaResponseDto): void {
    this._alertService.confirm({
      title: 'Delete Learning Area',
      message: `Delete "${row.name}"? This will also remove all related strands and sub-strands.`,
      confirmText: 'Delete',
      onConfirm: () => {
        this._service.delete(row.id).pipe(takeUntil(this._destroy$)).subscribe({
          next: () => { this._alertService.success('Learning area deleted'); this.loadAll(); },
          error: err => this._alertService.error(err?.error?.message || 'Failed to delete'),
        });
      },
    });
  }

  getLevelLabel(level: string | number): string {
    const opt = this.cbcLevelOptions.find(l => l.value.toString() === level?.toString() || l.label === level);
    return opt?.label ?? level?.toString() ?? '—';
  }
}