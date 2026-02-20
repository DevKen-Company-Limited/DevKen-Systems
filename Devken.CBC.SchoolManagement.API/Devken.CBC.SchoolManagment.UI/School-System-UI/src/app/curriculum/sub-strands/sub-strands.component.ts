import { Component, OnInit, OnDestroy, inject, AfterViewInit, TemplateRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';

import { AuthService } from 'app/core/auth/auth.service';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';
import { DataTableComponent, TableColumn, TableAction, TableHeader, TableEmptyState } from 'app/shared/data-table/data-table.component';
import { LearningAreaService } from 'app/core/DevKenService/curriculum/learning-area.service';
import { StrandService } from 'app/core/DevKenService/curriculum/strand.service';
import { SubStrandService } from 'app/core/DevKenService/curriculum/substrand.service ';
import { LearningAreaResponseDto } from '../types/learning-area.dto ';
import { StrandResponseDto } from '../types/strand.dto ';
import { SubStrandResponseDto } from '../types/substrand.dto ';

@Component({
  selector: 'app-sub-strands',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatIconModule, MatButtonModule,
    MatProgressSpinnerModule, MatTooltipModule,
    PageHeaderComponent, FilterPanelComponent, PaginationComponent,
    StatsCardsComponent, DataTableComponent,
  ],
  templateUrl: './sub-strands.component.html',
})
export class SubStrandsComponent implements OnInit, OnDestroy, AfterViewInit {
  private _destroy$ = new Subject<void>();
  private _service = inject(SubStrandService);
  private _strandService = inject(StrandService);
  private _laService = inject(LearningAreaService);
  private _authService = inject(AuthService);
  private _alertService = inject(AlertService);
  private _router = inject(Router);
  private _route = inject(ActivatedRoute);

   // ViewChild queries for the cell templates defined in the HTML
  @ViewChild('nameCell', { static: true }) nameCell!: TemplateRef<any>;
  @ViewChild('strandCell', { static: true }) strandCell!: TemplateRef<any>;
  @ViewChild('learningAreaCell', { static: true }) learningAreaCell!: TemplateRef<any>;
  @ViewChild('statusCell', { static: true }) statusCell!: TemplateRef<any>;

  // Property bound to [cellTemplates] in the template
  cellTemplates!: Record<string, TemplateRef<any>>;

   ngAfterViewInit(): void {
    // Build the mapping after the templates are available
    this.cellTemplates = {
      name: this.nameCell,
      strand: this.strandCell,
      learningArea: this.learningAreaCell,
      status: this.statusCell,
    };
  }
  
  get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }

  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Curriculum' },
    { label: 'Strands', url: '/curriculum/strands' },
    { label: 'Sub-Strands' },
  ];

  allData: SubStrandResponseDto[] = [];
  strands: StrandResponseDto[] = [];
  learningAreas: LearningAreaResponseDto[] = [];
  isLoading = false;
  showFilterPanel = false;
  currentPage = 1;
  itemsPerPage = 10;

  preselectedStrandId: string | null = null;
  private _filterValues = { search: '', strandId: 'all', learningAreaId: 'all', status: 'all' };

  get statsCards(): StatCard[] {
    return [
      { label: 'Total Sub-Strands', value: this.allData.length,                                    icon: 'format_list_bulleted', iconColor: 'indigo'  },
      { label: 'Active',            value: this.allData.filter(s => s.status === 'Active').length,  icon: 'check_circle',         iconColor: 'green' },
      { label: 'Strands',           value: new Set(this.allData.map(s => s.strandId)).size,         icon: 'account_tree',         iconColor: 'blue'  },
    ];
  }

  tableColumns: TableColumn<SubStrandResponseDto>[] = [
    { id: 'name',         label: 'Sub-Strand Name', align: 'left', sortable: true },
    { id: 'strand',       label: 'Strand',          align: 'left', hideOnMobile: true },
    { id: 'learningArea', label: 'Learning Area',   align: 'left', hideOnTablet: true },
    { id: 'status',       label: 'Status',          align: 'center' },
  ];

  tableActions: TableAction<SubStrandResponseDto>[] = [
    { id: 'edit',     label: 'Edit',            icon: 'edit',              color: 'indigo', handler: r => this.edit(r)          },
    { id: 'outcomes', label: 'View Outcomes',   icon: 'format_list_bulleted', color: 'teal', handler: r => this.viewOutcomes(r) },
    { id: 'delete',   label: 'Delete',          icon: 'delete',            color: 'red',    handler: r => this.delete(r)         },
  ];

  tableHeader: TableHeader = {
    title: 'Sub-Strands', subtitle: '', icon: 'format_list_bulleted',
    iconGradient: 'bg-gradient-to-br from-teal-500 via-cyan-600 to-blue-600',
  };

  tableEmptyState: TableEmptyState = {
    icon: 'format_list_bulleted', message: 'No sub-strands found',
    description: 'Create your first sub-strand within a strand',
    action: { label: 'Add Sub-Strand', icon: 'add', handler: () => this.create() },
  };

  filterFields: FilterField[] = [];

  get filteredData(): SubStrandResponseDto[] {
    const q = this._filterValues.search.toLowerCase();
    return this.allData.filter(s =>
      (!q || s.name.toLowerCase().includes(q) || s.strandName?.toLowerCase().includes(q) || s.learningAreaName?.toLowerCase().includes(q)) &&
      (this._filterValues.strandId === 'all' || s.strandId === this._filterValues.strandId) &&
      (this._filterValues.learningAreaId === 'all' || s.learningAreaId === this._filterValues.learningAreaId) &&
      (this._filterValues.status === 'all' || s.status === this._filterValues.status)
    );
  }

  get paginatedData(): SubStrandResponseDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  ngOnInit(): void {
    this.preselectedStrandId = this._route.snapshot.queryParamMap.get('strandId');
    if (this.preselectedStrandId) this._filterValues.strandId = this.preselectedStrandId;
    this.loadLookups();
    this.loadAll();
  }

  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  private loadLookups(): void {
    this._laService.getAll().pipe(takeUntil(this._destroy$))
      .subscribe(data => { this.learningAreas = Array.isArray(data) ? data : []; this.buildFilterFields(); });
    this._strandService.getAll().pipe(takeUntil(this._destroy$))
      .subscribe(data => { this.strands = Array.isArray(data) ? data : []; this.buildFilterFields(); });
  }

  private buildFilterFields(): void {
    this.filterFields = [
      { id: 'search', label: 'Search', type: 'text', placeholder: 'Search sub-strands...', value: '' },
      {
        id: 'learningAreaId', label: 'Learning Area', type: 'select', value: 'all',
        options: [{ label: 'All Learning Areas', value: 'all' }, ...this.learningAreas.map(la => ({ label: la.name, value: la.id }))],
      },
      {
        id: 'strandId', label: 'Strand', type: 'select', value: this._filterValues.strandId,
        options: [{ label: 'All Strands', value: 'all' }, ...this.strands.map(s => ({ label: s.name, value: s.id }))],
      },
      {
        id: 'status', label: 'Status', type: 'select', value: 'all',
        options: [{ label: 'All', value: 'all' }, { label: 'Active', value: 'Active' }, { label: 'Inactive', value: 'Inactive' }],
      },
    ];
  }

  loadAll(): void {
    this.isLoading = true;
    const strandId = this._filterValues.strandId !== 'all' ? this._filterValues.strandId : undefined;
    this._service.getAll(null, strandId).pipe(takeUntil(this._destroy$)).subscribe({
      next: data => {
        this.allData = Array.isArray(data) ? data : [];
        this.tableHeader.subtitle = `${this.filteredData.length} sub-strands`;
        this.isLoading = false;
      },
      error: err => { this._alertService.error(err?.error?.message || 'Failed to load'); this.isLoading = false; },
    });
  }

  toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

  onFilterChange(event: FilterChangeEvent): void {
    (this._filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    if (event.filterId === 'strandId') this.loadAll();
  }

  onClearFilters(): void {
    this._filterValues = { search: '', strandId: 'all', learningAreaId: 'all', status: 'all' };
    this.filterFields.forEach(f => { f.value = (this._filterValues as any)[f.id]; });
    this.currentPage = 1;
    this.loadAll();
  }

  onPageChange(page: number): void { this.currentPage = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

  create():                              void { this._router.navigate(['/curriculum/sub-strands/create']); }
  edit(row: SubStrandResponseDto):       void { this._router.navigate(['/curriculum/sub-strands/edit', row.id]); }
  viewOutcomes(row: SubStrandResponseDto): void {
    this._router.navigate(['/curriculum/learning-outcomes'], { queryParams: { subStrandId: row.id } });
  }

  delete(row: SubStrandResponseDto): void {
    this._alertService.confirm({
      title: 'Delete Sub-Strand',
      message: `Delete "${row.name}"? This will also remove all related learning outcomes.`,
      confirmText: 'Delete',
      onConfirm: () => {
        this._service.delete(row.id).pipe(takeUntil(this._destroy$)).subscribe({
          next: () => { this._alertService.success('Sub-strand deleted'); this.loadAll(); },
          error: err => this._alertService.error(err?.error?.message || 'Delete failed'),
        });
      },
    });
  }
}