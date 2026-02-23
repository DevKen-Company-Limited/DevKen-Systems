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
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

import { AuthService } from 'app/core/auth/auth.service';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';
import { DataTableComponent, TableColumn, TableAction, TableHeader, TableEmptyState } from 'app/shared/data-table/data-table.component';
import { LearningAreaService } from 'app/core/DevKenService/curriculum/learning-area.service';
import { StrandService } from 'app/core/DevKenService/curriculum/strand.service';
import { LearningAreaResponseDto } from '../types/learning-area.dto ';
import { StrandResponseDto } from '../types/strand.dto ';
import { StrandFormComponent } from './strand-form/strand-form.component';

@Component({
  selector: 'app-strands',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatIconModule, MatButtonModule,
    MatProgressSpinnerModule, MatTooltipModule, MatDialogModule,
    PageHeaderComponent, FilterPanelComponent, PaginationComponent,
    StatsCardsComponent, DataTableComponent,
  ],
  templateUrl: './strands.component.html',
})
export class StrandsComponent implements OnInit, OnDestroy, AfterViewInit {
  private _destroy$ = new Subject<void>();
  private _service = inject(StrandService);
  private _learningAreaService = inject(LearningAreaService);
  private _authService = inject(AuthService);
  private _alertService = inject(AlertService);
  router = inject(Router);
  private _route = inject(ActivatedRoute);
  private _dialog = inject(MatDialog);

  @ViewChild('nameCell', { static: true }) nameCell!: TemplateRef<any>;
  @ViewChild('learningAreaCell', { static: true }) learningAreaCell!: TemplateRef<any>;
  @ViewChild('statusCell', { static: true }) statusCell!: TemplateRef<any>;

  cellTemplates!: Record<string, TemplateRef<any>>;

  ngAfterViewInit(): void {
    this.cellTemplates = {
      name: this.nameCell,
      learningArea: this.learningAreaCell,
      status: this.statusCell,
    };
  }

  get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }

  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Curriculum' },
    { label: 'Learning Areas', url: '/curriculum/learning-areas' },
    { label: 'Strands' },
  ];

  allData: StrandResponseDto[] = [];
  learningAreas: LearningAreaResponseDto[] = [];
  isLoading = false;
  showFilterPanel = false;
  currentPage = 1;
  itemsPerPage = 10;
  preselectedLearningAreaId: string | null = null;

  private _filterValues = { search: '', learningAreaId: 'all', status: 'all' };

  get statsCards(): StatCard[] {
    return [
      { label: 'Total Strands',  value: this.allData.length,                                    icon: 'account_tree', iconColor: 'blue'  },
      { label: 'Active',         value: this.allData.filter(s => s.status === 'Active').length, icon: 'check_circle', iconColor: 'green' },
      { label: 'Learning Areas', value: new Set(this.allData.map(s => s.learningAreaId)).size,  icon: 'menu_book',    iconColor: 'indigo'},
    ];
  }

  tableColumns: TableColumn<StrandResponseDto>[] = [
    { id: 'name',         label: 'Strand Name',   align: 'left', sortable: true },
    { id: 'learningArea', label: 'Learning Area', align: 'left', hideOnMobile: true },
    { id: 'status',       label: 'Status',        align: 'center' },
  ];

  tableActions: TableAction<StrandResponseDto>[] = [
    { id: 'edit',       label: 'Edit',             icon: 'edit',          color: 'indigo', handler: r => this.edit(r)           },
    { id: 'substrands', label: 'View Sub-Strands', icon: 'account_tree',  color: 'blue',   handler: r => this.viewSubStrands(r) },
    { id: 'delete',     label: 'Delete',           icon: 'delete',        color: 'red',    handler: r => this.delete(r)         },
  ];

  tableHeader: TableHeader = {
    title: 'Strands', subtitle: '', icon: 'account_tree',
    iconGradient: 'bg-gradient-to-br from-blue-500 via-cyan-600 to-teal-600',
  };

  tableEmptyState: TableEmptyState = {
    icon: 'account_tree', message: 'No strands found',
    description: 'Create your first strand under a learning area',
    action: { label: 'Add Strand', icon: 'add', handler: () => this.create() },
  };

  filterFields: FilterField[] = [];

  get filteredData(): StrandResponseDto[] {
    const q = this._filterValues.search.toLowerCase();
    return this.allData.filter(s =>
      (!q || s.name.toLowerCase().includes(q) || s.learningAreaName?.toLowerCase().includes(q)) &&
      (this._filterValues.learningAreaId === 'all' || s.learningAreaId === this._filterValues.learningAreaId) &&
      (this._filterValues.status === 'all' || s.status === this._filterValues.status)
    );
  }

  get paginatedData(): StrandResponseDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  ngOnInit(): void {
    this.preselectedLearningAreaId = this._route.snapshot.queryParamMap.get('learningAreaId');
    if (this.preselectedLearningAreaId) {
      this._filterValues.learningAreaId = this.preselectedLearningAreaId;
    }
    this.loadLearningAreas();
    this.loadAll();
  }

  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  private loadLearningAreas(): void {
    this._learningAreaService.getAll()
      .pipe(takeUntil(this._destroy$))
      .subscribe(data => {
        this.learningAreas = Array.isArray(data) ? data : [];
        this.buildFilterFields();
      });
  }

  private buildFilterFields(): void {
    this.filterFields = [
      { id: 'search', label: 'Search', type: 'text', placeholder: 'Search strands...', value: '' },
      {
        id: 'learningAreaId', label: 'Learning Area', type: 'select',
        value: this._filterValues.learningAreaId,
        options: [
          { label: 'All Learning Areas', value: 'all' },
          ...this.learningAreas.map(la => ({ label: la.name, value: la.id })),
        ],
      },
      {
        id: 'status', label: 'Status', type: 'select', value: 'all',
        options: [
          { label: 'All',      value: 'all' },
          { label: 'Active',   value: 'Active' },
          { label: 'Inactive', value: 'Inactive' },
        ],
      },
    ];
  }

  loadAll(): void {
    this.isLoading = true;
    const laId = this._filterValues.learningAreaId !== 'all' ? this._filterValues.learningAreaId : undefined;
    this._service.getAll(null, laId)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: data => {
          this.allData = Array.isArray(data) ? data : [];
          this.tableHeader.subtitle = `${this.filteredData.length} strands`;
          this.isLoading = false;
        },
        error: err => {
          this._alertService.error(err?.error?.message || 'Failed to load strands');
          this.isLoading = false;
        },
      });
  }

  toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

  onFilterChange(event: FilterChangeEvent): void {
    (this._filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    if (event.filterId === 'learningAreaId') this.loadAll();
  }

  onClearFilters(): void {
    this._filterValues = { search: '', learningAreaId: 'all', status: 'all' };
    this.filterFields.forEach(f => { f.value = (this._filterValues as any)[f.id]; });
    this.currentPage = 1;
    this.loadAll();
  }

  onPageChange(page: number): void { this.currentPage = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

  create(): void {
    const ref = this._dialog.open(StrandFormComponent, {
      width: '520px',
      maxWidth: '95vw',
      data: {
        defaultLearningAreaId: this.preselectedLearningAreaId ?? undefined,
      },
    });
    ref.afterClosed().subscribe(result => { if (result?.success) this.loadAll(); });
  }

  edit(row: StrandResponseDto): void {
    const ref = this._dialog.open(StrandFormComponent, {
      width: '520px',
      maxWidth: '95vw',
      data: { editId: row.id },
    });
    ref.afterClosed().subscribe(result => { if (result?.success) this.loadAll(); });
  }

  viewSubStrands(row: StrandResponseDto): void {
    this.router.navigate(['/curriculum/sub-strands'], { queryParams: { strandId: row.id } });
  }

  delete(row: StrandResponseDto): void {
    this._alertService.confirm({
      title: 'Delete Strand',
      message: `Delete "${row.name}"? This will also remove all related sub-strands.`,
      confirmText: 'Delete',
      onConfirm: () => {
        this._service.delete(row.id).pipe(takeUntil(this._destroy$)).subscribe({
          next: () => { this._alertService.success('Strand deleted'); this.loadAll(); },
          error: err => this._alertService.error(err?.error?.message || 'Delete failed'),
        });
      },
    });
  }
}