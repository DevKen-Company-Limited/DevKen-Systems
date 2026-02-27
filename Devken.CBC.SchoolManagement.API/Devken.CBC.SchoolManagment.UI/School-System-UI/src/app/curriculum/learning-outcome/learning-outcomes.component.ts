import { Component, OnInit, OnDestroy, inject, TemplateRef, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

import { AuthService } from 'app/core/auth/auth.service';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';
import { DataTableComponent, TableColumn, TableAction, TableHeader, TableEmptyState } from 'app/shared/data-table/data-table.component';
import { LearningAreaService } from 'app/core/DevKenService/curriculum/learning-area.service';
import { LearningOutcomeService } from 'app/core/DevKenService/curriculum/learning-outcome.service';
import { StrandService } from 'app/core/DevKenService/curriculum/strand.service';
import { SubStrandService } from 'app/core/DevKenService/curriculum/substrand.service ';
import { CBCLevelOptions } from '../types/curriculum-enums';
import { LearningAreaResponseDto } from '../types/learning-area.dto ';
import { LearningOutcomeResponseDto } from '../types/learning-outcome.dto';
import { StrandResponseDto } from '../types/strand.dto ';
import { SubStrandResponseDto } from '../types/substrand.dto ';
import { LearningOutcomeFormComponent } from '../../dialog-modals/Curriculum/learning-outcome-form/learning-outcome-form.component';

@Component({
  selector: 'app-learning-outcomes',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatIconModule, MatButtonModule,
    MatProgressSpinnerModule, MatTooltipModule, MatChipsModule, MatDialogModule,
    PageHeaderComponent, FilterPanelComponent, PaginationComponent,
    StatsCardsComponent, DataTableComponent,
  ],
  templateUrl: './learning-outcomes.component.html',
})
export class LearningOutcomesComponent implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild('outcomeCell') outcomeCell!: TemplateRef<any>;
  @ViewChild('codeCell') codeCell!: TemplateRef<any>;
  @ViewChild('hierarchyCell') hierarchyCell!: TemplateRef<any>;
  @ViewChild('levelCell') levelCell!: TemplateRef<any>;
  @ViewChild('coreCell') coreCell!: TemplateRef<any>;
  @ViewChild('statusCell') statusCell!: TemplateRef<any>;

  private _destroy$ = new Subject<void>();
  private _service = inject(LearningOutcomeService);
  private _ssService = inject(SubStrandService);
  private _strandService = inject(StrandService);
  private _laService = inject(LearningAreaService);
  private _authService = inject(AuthService);
  private _alertService = inject(AlertService);
  router = inject(Router);
  private _route = inject(ActivatedRoute);
  private _dialog = inject(MatDialog);

  get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }

  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Curriculum' },
    { label: 'Sub-Strands', url: '/curriculum/sub-strands' },
    { label: 'Learning Outcomes' },
  ];

  allData: LearningOutcomeResponseDto[] = [];
  subStrands: SubStrandResponseDto[] = [];
  strands: StrandResponseDto[] = [];
  learningAreas: LearningAreaResponseDto[] = [];
  isLoading = false;
  showFilterPanel = false;
  currentPage = 1;
  itemsPerPage = 10;
  cellTemplates: { [key: string]: TemplateRef<any> } = {};

  cbcLevelOptions = CBCLevelOptions;

  private _filterValues = {
    search: '', learningAreaId: 'all', strandId: 'all',
    subStrandId: 'all', level: 'all', isCore: 'all', status: 'all',
  };

  get statsCards(): StatCard[] {
    return [
      { label: 'Total Outcomes', value: this.allData.length,                                    icon: 'format_list_bulleted', iconColor: 'violet' },
      { label: 'Core',           value: this.allData.filter(o => o.isCore).length,              icon: 'star',                 iconColor: 'amber'  },
      { label: 'Non-Core',       value: this.allData.filter(o => !o.isCore).length,             icon: 'star_outline',         iconColor: 'blue'   },
      { label: 'Active',         value: this.allData.filter(o => o.status === 'Active').length, icon: 'check_circle',         iconColor: 'green'  },
    ];
  }

  tableColumns: TableColumn<LearningOutcomeResponseDto>[] = [
    { id: 'outcome',   label: 'Learning Outcome', align: 'left',   sortable: true },
    { id: 'code',      label: 'Code',             align: 'left',   hideOnMobile: true },
    { id: 'hierarchy', label: 'Hierarchy',        align: 'left',   hideOnMobile: true },
    { id: 'level',     label: 'Level',            align: 'center', hideOnTablet: true },
    { id: 'core',      label: 'Core',             align: 'center' },
    { id: 'status',    label: 'Status',           align: 'center' },
  ];

  tableActions: TableAction<LearningOutcomeResponseDto>[] = [
    { id: 'edit',   label: 'Edit',   icon: 'edit',   color: 'indigo', handler: r => this.edit(r)   },
    { id: 'delete', label: 'Delete', icon: 'delete', color: 'red',    handler: r => this.delete(r) },
  ];

  tableHeader: TableHeader = {
    title: 'Learning Outcomes', subtitle: '', icon: 'format_list_bulleted',
    iconGradient: 'bg-gradient-to-br from-violet-500 via-purple-600 to-fuchsia-600',
  };

  tableEmptyState: TableEmptyState = {
    icon: 'format_list_bulleted', message: 'No learning outcomes found',
    description: 'Create learning outcomes for your curriculum sub-strands',
    action: { label: 'Add Learning Outcome', icon: 'add', handler: () => this.create() },
  };

  filterFields: FilterField[] = [];

  get filteredData(): LearningOutcomeResponseDto[] {
    const q = this._filterValues.search.toLowerCase();
    return this.allData.filter(o =>
      (!q || o.outcome.toLowerCase().includes(q) || o.code?.toLowerCase().includes(q) || o.subStrandName?.toLowerCase().includes(q)) &&
      (this._filterValues.learningAreaId === 'all' || o.learningAreaId === this._filterValues.learningAreaId) &&
      (this._filterValues.strandId       === 'all' || o.strandId       === this._filterValues.strandId) &&
      (this._filterValues.subStrandId    === 'all' || o.subStrandId    === this._filterValues.subStrandId) &&
      (this._filterValues.level          === 'all' || o.level          === this._filterValues.level ||
        this.cbcLevelOptions.find(l => l.value.toString() === this._filterValues.level)?.label === o.level) &&
      (this._filterValues.isCore         === 'all' || o.isCore.toString() === this._filterValues.isCore) &&
      (this._filterValues.status         === 'all' || o.status === this._filterValues.status)
    );
  }

  get paginatedData(): LearningOutcomeResponseDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  ngOnInit(): void {
    const ssId = this._route.snapshot.queryParamMap.get('subStrandId');
    if (ssId) this._filterValues.subStrandId = ssId;
    this.loadLookups();
    this.loadAll();
  }

  ngAfterViewInit(): void {
    this.cellTemplates = {
      outcome:   this.outcomeCell,
      code:      this.codeCell,
      hierarchy: this.hierarchyCell,
      level:     this.levelCell,
      core:      this.coreCell,
      status:    this.statusCell,
    };
  }

  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  private loadLookups(): void {
    this._laService.getAll().pipe(takeUntil(this._destroy$)).subscribe(d => { this.learningAreas = Array.isArray(d) ? d : []; this.buildFilterFields(); });
    this._strandService.getAll().pipe(takeUntil(this._destroy$)).subscribe(d => { this.strands = Array.isArray(d) ? d : []; this.buildFilterFields(); });
    this._ssService.getAll().pipe(takeUntil(this._destroy$)).subscribe(d => { this.subStrands = Array.isArray(d) ? d : []; this.buildFilterFields(); });
  }

  private buildFilterFields(): void {
    this.filterFields = [
      { id: 'search', label: 'Search', type: 'text', placeholder: 'Search outcomes, codes...', value: '' },
      {
        id: 'learningAreaId', label: 'Learning Area', type: 'select', value: 'all',
        options: [{ label: 'All Learning Areas', value: 'all' }, ...this.learningAreas.map(la => ({ label: la.name, value: la.id }))],
      },
      {
        id: 'strandId', label: 'Strand', type: 'select', value: 'all',
        options: [{ label: 'All Strands', value: 'all' }, ...this.strands.map(s => ({ label: s.name, value: s.id }))],
      },
      {
        id: 'subStrandId', label: 'Sub-Strand', type: 'select', value: this._filterValues.subStrandId,
        options: [{ label: 'All Sub-Strands', value: 'all' }, ...this.subStrands.map(ss => ({ label: ss.name, value: ss.id }))],
      },
      {
        id: 'level', label: 'CBC Level', type: 'select', value: 'all',
        options: [{ label: 'All Levels', value: 'all' }, ...CBCLevelOptions.map(l => ({ label: l.label, value: l.value.toString() }))],
      },
      {
        id: 'isCore', label: 'Type', type: 'select', value: 'all',
        options: [{ label: 'All Types', value: 'all' }, { label: 'Core', value: 'true' }, { label: 'Non-Core', value: 'false' }],
      },
    ];
  }

  loadAll(): void {
    this.isLoading = true;
    const opts = {
      subStrandId: this._filterValues.subStrandId !== 'all' ? this._filterValues.subStrandId : undefined,
      strandId:    this._filterValues.strandId    !== 'all' ? this._filterValues.strandId    : undefined,
    };
    this._service.getAll(opts).pipe(takeUntil(this._destroy$)).subscribe({
      next: data => {
        this.allData = Array.isArray(data) ? data : [];
        this.tableHeader.subtitle = `${this.filteredData.length} learning outcomes`;
        this.isLoading = false;
      },
      error: err => { this._alertService.error(err?.error?.message || 'Failed to load'); this.isLoading = false; },
    });
  }

  toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

  onFilterChange(event: FilterChangeEvent): void {
    (this._filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    if (event.filterId === 'subStrandId' || event.filterId === 'strandId') this.loadAll();
    else this.tableHeader.subtitle = `${this.filteredData.length} learning outcomes`;
  }

  onClearFilters(): void {
    this._filterValues = { search: '', learningAreaId: 'all', strandId: 'all', subStrandId: 'all', level: 'all', isCore: 'all', status: 'all' };
    this.filterFields.forEach(f => { f.value = (this._filterValues as any)[f.id]; });
    this.currentPage = 1;
    this.loadAll();
  }

  onPageChange(page: number): void { this.currentPage = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

  create(): void {
    const ssId = this._filterValues.subStrandId !== 'all' ? this._filterValues.subStrandId : undefined;
    const ref = this._dialog.open(LearningOutcomeFormComponent, {
      width: '700px',
      maxWidth: '95vw',
      data: { defaultSubStrandId: ssId },
    });
    ref.afterClosed().subscribe(result => { if (result?.success) this.loadAll(); });
  }

  edit(row: LearningOutcomeResponseDto): void {
    const ref = this._dialog.open(LearningOutcomeFormComponent, {
      width: '700px',
      maxWidth: '95vw',
      data: { editId: row.id },
    });
    ref.afterClosed().subscribe(result => { if (result?.success) this.loadAll(); });
  }

  delete(row: LearningOutcomeResponseDto): void {
    this._alertService.confirm({
      title: 'Delete Learning Outcome',
      message: `Delete this learning outcome: "${row.outcome}"?`,
      confirmText: 'Delete',
      onConfirm: () => {
        this._service.delete(row.id).pipe(takeUntil(this._destroy$)).subscribe({
          next: () => { this._alertService.success('Learning outcome deleted'); this.loadAll(); },
          error: err => this._alertService.error(err?.error?.message || 'Delete failed'),
        });
      },
    });
  }

  getLevelLabel(level: string | number): string {
    const opt = this.cbcLevelOptions.find(l => l.value.toString() === level?.toString() || l.label === level);
    return opt?.label ?? level?.toString() ?? '—';
  }
}