// list/assessments.component.ts
import {
  Component, OnInit, OnDestroy,
  ViewChild, TemplateRef, AfterViewInit, inject,
} from '@angular/core';
import { CommonModule }            from '@angular/common';
import { FormsModule }             from '@angular/forms';
import { Router }                  from '@angular/router';
import { MatIconModule }           from '@angular/material/icon';
import { MatButtonModule }         from '@angular/material/button';
import { MatMenuModule }           from '@angular/material/menu';
import { MatTooltipModule }        from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Subject, forkJoin }       from 'rxjs';
import { takeUntil, catchError }   from 'rxjs/operators';
import { of }                      from 'rxjs';

import { AlertService }   from 'app/core/DevKenService/Alert/AlertService';
import { AuthService }    from 'app/core/auth/auth.service';
import { PageHeaderComponent, Breadcrumb }                          from 'app/shared/Page-Header/page-header.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent }     from 'app/shared/Filter/filter-panel.component';
import { PaginationComponent }                                      from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard }                            from 'app/shared/stats-cards/stats-cards.component';
import { DataTableComponent, TableColumn, TableAction, TableHeader, TableEmptyState } from 'app/shared/data-table/data-table.component';
import { AssessmentService } from 'app/core/DevKenService/assessments/Assessments/AssessmentService';
import {
  getAssessmentTypeLabel,
  getAssessmentTypeColor,
  getAssessmentTypeIcon,
  AssessmentType,
  AssessmentListItem,
  AssessmentTypeOptions,
} from '../types/AssessmentDtos';


@Component({
  selector: 'app-assessments',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatButtonModule, MatMenuModule, MatTooltipModule, MatProgressSpinnerModule,
    PageHeaderComponent, FilterPanelComponent, PaginationComponent,
    StatsCardsComponent, DataTableComponent,
  ],
  templateUrl: './assessments.component.html',
})
export class AssessmentsComponent implements OnInit, AfterViewInit, OnDestroy {

  @ViewChild('titleCell')   titleCellTemplate!:  TemplateRef<any>;
  @ViewChild('typeCell')    typeCellTemplate!:   TemplateRef<any>;
  @ViewChild('scoreCell')   scoreCellTemplate!:  TemplateRef<any>;
  @ViewChild('statusCell')  statusCellTemplate!: TemplateRef<any>;
  @ViewChild('dateCell')    dateCellTemplate!:   TemplateRef<any>;

  private _destroy$  = new Subject<void>();
  private _router    = inject(Router);
  private _auth      = inject(AuthService);
  private _alert     = inject(AlertService);
  private _service   = inject(AssessmentService);

  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard',   url: '/dashboard'   },
    { label: 'Assessment',  url: '/assessments' },
    { label: 'Assessments'                      },
  ];

  get isSuperAdmin(): boolean { return this._auth.authUser?.isSuperAdmin ?? false; }

  // ── Type helpers ──────────────────────────────────────────────────────
  getTypeName  = getAssessmentTypeLabel;
  getTypeColor = getAssessmentTypeColor;
  getTypeIcon  = getAssessmentTypeIcon;
  readonly AssessmentType = AssessmentType;

  // ── State ─────────────────────────────────────────────────────────────
  allData:   AssessmentListItem[] = [];
  isLoading  = false;
  cellTemplates: Record<string, TemplateRef<any>> = {};
  showFilterPanel = false;
  filterFields: FilterField[] = [];

  private _filters = {
    search: '', type: 'all', isPublished: 'all', subjectId: 'all', classId: 'all',
  };

  currentPage  = 1;
  itemsPerPage = 10;

  // ── Computed ──────────────────────────────────────────────────────────
  get total():       number { return this.allData.length; }
  get published():   number { return this.allData.filter(a => a.isPublished).length; }
  get drafts():      number { return this.allData.filter(a => !a.isPublished).length; }
  get formativeC():  number { return this.allData.filter(a => a.assessmentType === AssessmentType.Formative).length; }
  get summativeC():  number { return this.allData.filter(a => a.assessmentType === AssessmentType.Summative).length; }
  get competencyC(): number { return this.allData.filter(a => a.assessmentType === AssessmentType.Competency).length; }

  get statsCards(): StatCard[] {
    return [
      { label: 'Total',      value: this.total,       icon: 'assignment',    iconColor: 'indigo' },
      { label: 'Published',  value: this.published,   icon: 'public',        iconColor: 'green'  },
      { label: 'Drafts',     value: this.drafts,       icon: 'edit',          iconColor: 'amber'  },
      { label: 'Formative',  value: this.formativeC,  icon: 'assignment',    iconColor: 'blue'   },
      { label: 'Summative',  value: this.summativeC,  icon: 'school',        iconColor: 'violet' },
      { label: 'Competency', value: this.competencyC, icon: 'verified_user', iconColor: 'pink'   },
    ];
  }

  get tableColumns(): TableColumn<AssessmentListItem>[] {
    return [
      { id: 'title',  label: 'Assessment', align: 'left',   sortable: true },
      { id: 'type',   label: 'Type',       align: 'left'                   },
      { id: 'score',  label: 'Max Score',  align: 'center', hideOnTablet: true },
      { id: 'date',   label: 'Date',       align: 'left',   hideOnMobile: true },
      { id: 'status', label: 'Status',     align: 'center'                 },
    ];
  }

  tableActions: TableAction<AssessmentListItem>[] = [
    { id: 'view',    label: 'View Details', icon: 'visibility', color: 'blue',   handler: a => this.viewAssessment(a) },
    { id: 'edit',    label: 'Edit',         icon: 'edit',       color: 'indigo', handler: a => this.editAssessment(a) },
    { id: 'scores',  label: 'Enter Scores', icon: 'grade',      color: 'teal',   handler: a => this.goToScores(a)     },
    {
      id: 'publish', label: 'Publish',      icon: 'public',     color: 'green',
      handler: a => this.publishAssessment(a),
      visible: a => !a.isPublished,
    },
    {
      id: 'delete',  label: 'Delete',       icon: 'delete',     color: 'red',
      handler: a => this.deleteAssessment(a),
      visible: a => !a.isPublished,
    },
  ];

  tableHeader: TableHeader = {
    title:        'Assessments',
    subtitle:     '',
    icon:         'assignment',
    iconGradient: 'bg-gradient-to-br from-indigo-500 via-violet-600 to-teal-600',
  };

  tableEmptyState: TableEmptyState = {
    icon:        'assignment',
    message:     'No assessments found',
    description: 'Create a formative, summative or competency assessment to get started',
    action: { label: 'Create Assessment', icon: 'add', handler: () => this.createAssessment() },
  };

  get filteredData(): AssessmentListItem[] {
    const q = this._filters.search.toLowerCase();
    return this.allData.filter(a => {
      const typeName = getAssessmentTypeLabel(a.assessmentType);
      return (
        (!q || a.title.toLowerCase().includes(q)
              || a.teacherName.toLowerCase().includes(q)
              || a.subjectName.toLowerCase().includes(q)
              || a.className.toLowerCase().includes(q)) &&
        (this._filters.type        === 'all' || typeName === this._filters.type) &&
        (this._filters.isPublished === 'all'
          || (this._filters.isPublished === 'published' &&  a.isPublished)
          || (this._filters.isPublished === 'draft'     && !a.isPublished))
      );
    });
  }

  get paginatedData(): AssessmentListItem[] {
    const s = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(s, s + this.itemsPerPage);
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────
  ngOnInit(): void {
    this._initFilterFields();
    this.loadAll();
  }

  ngAfterViewInit(): void {
    this.cellTemplates = {
      title:  this.titleCellTemplate,
      type:   this.typeCellTemplate,
      score:  this.scoreCellTemplate,
      status: this.statusCellTemplate,
      date:   this.dateCellTemplate,
    };
  }

  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  private _initFilterFields(): void {
    this.filterFields = [
      { id: 'search', label: 'Search', type: 'text', placeholder: 'Title, teacher, subject…', value: '' },
      {
        id: 'type', label: 'Type', type: 'select', value: 'all',
        options: [
          { label: 'All Types', value: 'all' },
          ...AssessmentTypeOptions.map(o => ({ label: o.label, value: o.label })),
        ],
      },
      {
        id: 'isPublished', label: 'Status', type: 'select', value: 'all',
        options: [
          { label: 'All Statuses', value: 'all'       },
          { label: 'Published',    value: 'published'  },
          { label: 'Draft',        value: 'draft'      },
        ],
      },
    ];
  }

  // ── Data ──────────────────────────────────────────────────────────────
  /**
   * Fetch all three assessment types in parallel and merge into one array.
   * Each individual request fails gracefully so a 404 on one type
   * doesn't block the other two.
   */
  loadAll(): void {
    this.isLoading = true;

    forkJoin({
      formative:  this._service.getAll(AssessmentType.Formative).pipe(catchError(() => of([] as AssessmentListItem[]))),
      summative:  this._service.getAll(AssessmentType.Summative).pipe(catchError(() => of([] as AssessmentListItem[]))),
      competency: this._service.getAll(AssessmentType.Competency).pipe(catchError(() => of([] as AssessmentListItem[]))),
    })
    .pipe(takeUntil(this._destroy$))
    .subscribe({
      next: ({ formative, summative, competency }) => {
        this.allData = [...formative, ...summative, ...competency];
        this.tableHeader.subtitle = `${this.filteredData.length} assessments found`;
        this.isLoading = false;
      },
      error: () => {
        this.allData  = [];
        this.isLoading = false;
      },
    });
  }

  // ── Filter events ─────────────────────────────────────────────────────
  toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

  onFilterChange(e: FilterChangeEvent): void {
    (this._filters as any)[e.filterId] = e.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} assessments found`;
  }

  onClearFilters(): void {
    this._filters = { search: '', type: 'all', isPublished: 'all', subjectId: 'all', classId: 'all' };
    this.filterFields.forEach(f => f.value = (this._filters as any)[f.id] ?? '');
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} assessments found`;
  }

  onPageChange(p: number): void { this.currentPage = p; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

  // ── Actions ───────────────────────────────────────────────────────────
  createAssessment(): void { this._router.navigate(['/assessment/assessments/create']); }

  viewAssessment(a: AssessmentListItem): void {
    this._router.navigate(['/assessment/assessments/details', a.id], {
      queryParams: { type: a.assessmentType },
    });
  }

  editAssessment(a: AssessmentListItem): void {
    this._router.navigate(['/assessment/assessments/edit', a.id], {
      queryParams: { type: a.assessmentType },
    });
  }

  goToScores(a: AssessmentListItem): void {
    this._router.navigate(['/assessment/assessments/scores', a.id], {
      queryParams: { type: a.assessmentType },
    });
  }

  publishAssessment(a: AssessmentListItem): void {
    this._alert.confirm({
      title:       'Publish Assessment',
      message:     `Publish "${a.title}"? Students and teachers will be able to see it.`,
      confirmText: 'Publish',
      onConfirm: () => {
        this._service.publish(a.id, a.assessmentType)
          .pipe(takeUntil(this._destroy$))
          .subscribe({
            next:  () => { this._alert.success('Assessment published.'); this.loadAll(); },
            error: e  => this._alert.error(e?.error?.message || 'Failed to publish'),
          });
      },
    });
  }

  deleteAssessment(a: AssessmentListItem): void {
    this._alert.confirm({
      title:       'Delete Assessment',
      message:     `Delete "${a.title}"? This cannot be undone.`,
      confirmText: 'Delete',
      onConfirm: () => {
        this._service.delete(a.id, a.assessmentType)
          .pipe(takeUntil(this._destroy$))
          .subscribe({
            next:  () => { this._alert.success('Assessment deleted.'); this.loadAll(); },
            error: e  => this._alert.error(e?.error?.message || 'Failed to delete'),
          });
      },
    });
  }

  formatDate(val: string): string {
    if (!val) return '—';
    return new Date(val).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }
}