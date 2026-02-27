// ═══════════════════════════════════════════════════════════════════
// assessments.component.ts  (List Page)
// ═══════════════════════════════════════════════════════════════════

import {
  Component, OnInit, OnDestroy, AfterViewInit, ViewChild, TemplateRef, inject,
} from '@angular/core';
import { CommonModule }                    from '@angular/common';
import { FormsModule }                     from '@angular/forms';
import { Router }                          from '@angular/router';
import { MatIconModule }                   from '@angular/material/icon';
import { MatButtonModule }                 from '@angular/material/button';
import { MatMenuModule }                   from '@angular/material/menu';
import { MatProgressSpinnerModule }        from '@angular/material/progress-spinner';
import { MatTooltipModule }                from '@angular/material/tooltip';
import { MatDividerModule }                from '@angular/material/divider';
import { Subject, forkJoin, of }           from 'rxjs';
import { takeUntil, catchError, finalize } from 'rxjs/operators';

import { PageHeaderComponent, Breadcrumb }                                            from 'app/shared/Page-Header/page-header.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent }                       from 'app/shared/Filter/filter-panel.component';
import { PaginationComponent }                                                        from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard }                                              from 'app/shared/stats-cards/stats-cards.component';
import { DataTableComponent, TableColumn, TableAction, TableHeader, TableEmptyState } from 'app/shared/data-table/data-table.component';
import { AlertService }                                                               from 'app/core/DevKenService/Alert/AlertService';
import { AuthService }                                                                from 'app/core/auth/auth.service';
import { AssessmentReportService }                                                    from 'app/core/DevKenService/assessments/Assessments/AssessmentReportService';
import { AssessmentService }                                                          from 'app/core/DevKenService/assessments/Assessments/AssessmentService';
import { AssessmentType, AssessmentListItem, getAssessmentTypeLabel, ASSESSMENT_TYPE_COLORS } from '../types/assessments';



@Component({
  selector: 'app-assessments',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatButtonModule, MatMenuModule,
    MatProgressSpinnerModule, MatTooltipModule, MatDividerModule,
    PageHeaderComponent, FilterPanelComponent, PaginationComponent,
    StatsCardsComponent, DataTableComponent,
  ],
  templateUrl: './assessments.component.html',
})
export class AssessmentsComponent implements OnInit, AfterViewInit, OnDestroy {

  @ViewChild('titleCell')   titleCellTemplate!:   TemplateRef<any>;
  @ViewChild('typeCell')    typeCellTemplate!:    TemplateRef<any>;
  @ViewChild('classCell')   classCellTemplate!:   TemplateRef<any>;
  @ViewChild('scoreCell')   scoreCellTemplate!:   TemplateRef<any>;
  @ViewChild('dateCell')    dateCellTemplate!:    TemplateRef<any>;
  @ViewChild('statusCell')  statusCellTemplate!:  TemplateRef<any>;

  private _destroy$      = new Subject<void>();
  private _authService   = inject(AuthService);
  private _router        = inject(Router);
  private _alertService  = inject(AlertService);
  private _reportService = inject(AssessmentReportService);

  get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }

  // ─── Breadcrumbs ──────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Assessments' },
  ];

  // ─── Stats ────────────────────────────────────────────────────────
  get statsCards(): StatCard[] {
    const cards: StatCard[] = [
      {
        label: 'Total',
        value: this.allData.length,
        icon: 'assignment', iconColor: 'indigo',
      },
      {
        label: 'Published',
        value: this.allData.filter(a => a.isPublished).length,
        icon: 'publish', iconColor: 'green',
      },
      {
        label: 'Formative',
        value: this.allData.filter(a => a.assessmentType === AssessmentType.Formative).length,
        icon: 'edit_note', iconColor: 'blue',
      },
      {
        label: 'Summative',
        value: this.allData.filter(a => a.assessmentType === AssessmentType.Summative).length,
        icon: 'fact_check', iconColor: 'violet',
      },
      {
        label: 'Competency',
        value: this.allData.filter(a => a.assessmentType === AssessmentType.Competency).length,
        icon: 'verified_user', iconColor: 'pink',
      },
    ];
    return cards;
  }

  // ─── Table ────────────────────────────────────────────────────────
  get tableColumns(): TableColumn<AssessmentListItem>[] {
    return [
      { id: 'title',  label: 'Assessment',   align: 'left',   sortable: true     },
      { id: 'type',   label: 'Type',         align: 'left',   hideOnMobile: true },
      { id: 'class',  label: 'Class / Term', align: 'left',   hideOnMobile: true },
      { id: 'score',  label: 'Max Score',    align: 'left',   hideOnTablet: true },
      { id: 'date',   label: 'Date',         align: 'left',   hideOnTablet: true },
      { id: 'status', label: 'Status',       align: 'center'                     },
    ];
  }

  tableActions: TableAction<AssessmentListItem>[] = [
    {
      id: 'view',  label: 'View Details', icon: 'visibility', color: 'blue',
      handler: a => this.viewAssessment(a),
    },
    {
      id: 'edit',  label: 'Edit',         icon: 'edit',       color: 'indigo',
      handler: a => this.editAssessment(a),
    },
    {
      id: 'grades', label: 'View Grades', icon: 'grading',    color: 'teal',
      handler: a => this.viewGrades(a),
    },
    {
      id: 'report', label: 'Grades PDF',  icon: 'download',   color: 'green',
      handler: a => this.downloadGradesPdf(a),
    },
    {
      id: 'publish', label: 'Publish', icon: 'publish', color: 'green', divider: true,
      handler: a => this.togglePublish(a),
      visible: a => !a.isPublished,
    },
    {
      id: 'unpublish', label: 'Unpublish', icon: 'unpublished', color: 'amber',
      handler: a => this.togglePublish(a),
      visible: a => a.isPublished,
    },
    {
      id: 'delete', label: 'Delete', icon: 'delete', color: 'red',
      handler: a => this.deleteAssessment(a),
    },
  ];

  tableHeader: TableHeader = {
    title:        'Assessments',
    subtitle:     '',
    icon:         'assignment',
    iconGradient: 'bg-gradient-to-br from-indigo-500 via-violet-600 to-purple-700',
  };

  tableEmptyState: TableEmptyState = {
    icon:        'assignment_late',
    message:     'No assessments found',
    description: 'Create your first assessment or adjust your filters',
    action: { label: 'Add', icon: 'add', handler: () => this.createAssessment() },
  };

  cellTemplates: { [key: string]: TemplateRef<any> } = {};

  // ─── State ────────────────────────────────────────────────────────
  allData:        AssessmentListItem[] = [];
  classes:        any[] = [];
  terms:          any[] = [];
  isLoading       = false;
  isDownloading   = false;
  showFilterPanel = false;
  filterFields:   FilterField[] = [];
  currentPage     = 1;
  itemsPerPage    = 10;

  // ── FIX: Map numeric AssessmentType → human-readable label ────────
  // getAssessmentTypeLabel() returns the string name for a given enum value.
  // We expose it as a method so the HTML template can call it directly,
  // and we also use it in the type-badge ngClass so the template never
  // has to compare raw numbers against strings like 'Formative'.
  readonly AssessmentType = AssessmentType; // expose enum to template

  getTypeName(type: AssessmentType | number): string {
    return getAssessmentTypeLabel(type as AssessmentType) ?? 'Unknown';
  }

  private _filters = {
    search: '', assessmentType: 'all', classId: 'all', published: 'all',
  };

  // ─── Computed ─────────────────────────────────────────────────────
  get filteredData(): AssessmentListItem[] {
    return this.allData.filter(a => {
      const q = this._filters.search.toLowerCase();

      // assessmentType filter stores the numeric enum value as a string ('1','2','3') or 'all'
      const typeMatch = this._filters.assessmentType === 'all'
        || a.assessmentType === Number(this._filters.assessmentType);

      // classId filter matches against className since AssessmentListItem has no classId
      const classMatch = this._filters.classId === 'all'
        || a.className === this._filters.classId;

      const publishMatch = this._filters.published === 'all'
        || (this._filters.published === 'published'   &&  a.isPublished)
        || (this._filters.published === 'unpublished' && !a.isPublished);

      const searchMatch = !q
        || a.title?.toLowerCase().includes(q)
        || a.teacherName?.toLowerCase().includes(q)
        || a.subjectName?.toLowerCase().includes(q)
        || a.className?.toLowerCase().includes(q);

      return searchMatch && typeMatch && classMatch && publishMatch;
    });
  }

  get paginatedData(): AssessmentListItem[] {
    const s = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(s, s + this.itemsPerPage);
  }

  // Expose helpers to template
  getTypeLabel(type: AssessmentType): string { return getAssessmentTypeLabel(type); }
  getTypeColor(type: AssessmentType): string { return ASSESSMENT_TYPE_COLORS[getAssessmentTypeLabel(type)] ?? 'gray'; }
  formatDate(val: string): string {
    if (!val) return '—';
    return new Date(val).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  // ─── Lifecycle ────────────────────────────────────────────────────
  constructor(private _service: AssessmentService) {}

  ngOnInit(): void { this.loadData(); }

  ngAfterViewInit(): void {
    this.cellTemplates = {
      title:  this.titleCellTemplate,
      type:   this.typeCellTemplate,
      class:  this.classCellTemplate,
      score:  this.scoreCellTemplate,
      date:   this.dateCellTemplate,
      status: this.statusCellTemplate,
    };
  }

  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  // ─── Load ─────────────────────────────────────────────────────────
  private loadData(): void {
    this.isLoading = true;
    forkJoin({
      formative:  this._service.getAll(AssessmentType.Formative).pipe(catchError(() => of([] as AssessmentListItem[]))),
      summative:  this._service.getAll(AssessmentType.Summative).pipe(catchError(() => of([] as AssessmentListItem[]))),
      competency: this._service.getAll(AssessmentType.Competency).pipe(catchError(() => of([] as AssessmentListItem[]))),
      classes:    this._service.getClasses().pipe(catchError(() => of([]))),
      terms:      this._service.getTerms().pipe(catchError(() => of([]))),
    })
    .pipe(takeUntil(this._destroy$), finalize(() => this.isLoading = false))
    .subscribe(({ formative, summative, competency, classes, terms }) => {
      this.allData = [...formative, ...summative, ...competency];
      this.classes = classes;
      this.terms   = terms;
      this.buildFilterFields();
      this.tableHeader.subtitle = `${this.filteredData.length} assessments found`;
    });
  }

  loadAll(): void { this.loadData(); }

  private buildFilterFields(): void {
    this.filterFields = [
      {
        id: 'search', label: 'Search', type: 'text',
        placeholder: 'Title, teacher, subject…', value: this._filters.search,
      },
      {
        id: 'assessmentType', label: 'Type', type: 'select', value: this._filters.assessmentType,
        options: [
          { label: 'All Types', value: 'all' },
          // store numeric enum value as the option value so typeMatch comparison works
          { label: 'Formative',  value: String(AssessmentType.Formative)  },
          { label: 'Summative',  value: String(AssessmentType.Summative)  },
          { label: 'Competency', value: String(AssessmentType.Competency) },
        ],
      },
      {
        id: 'classId', label: 'Class', type: 'select', value: this._filters.classId,
        options: [
          { label: 'All Classes', value: 'all' },
          ...this.classes.map(c => ({ label: c.name, value: c.name })), // match by className
        ],
      },
      {
        id: 'published', label: 'Status', type: 'select', value: this._filters.published,
        options: [
          { label: 'All Statuses', value: 'all'         },
          { label: 'Published',    value: 'published'   },
          { label: 'Unpublished',  value: 'unpublished' },
        ],
      },
    ];
  }

  // ─── Filter events ────────────────────────────────────────────────
  toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

  onFilterChange(e: FilterChangeEvent): void {
    (this._filters as any)[e.filterId] = e.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} assessments found`;
  }

  onClearFilters(): void {
    this._filters = { search: '', assessmentType: 'all', classId: 'all', published: 'all' };
    this.filterFields.forEach(f => { f.value = (this._filters as any)[f.id]; });
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} assessments found`;
  }

  onPageChange(p: number):         void { this.currentPage  = p; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

  // ─── Navigation ───────────────────────────────────────────────────
  createAssessment(): void {
    this._router.navigate(['/assessment/assessments/create']);
  }

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

  viewGrades(a: AssessmentListItem): void {
    this._router.navigate(['/assessment/assessments/grades', a.id], {
      queryParams: { type: a.assessmentType },
    });
  }

  // ─── Actions ──────────────────────────────────────────────────────
  togglePublish(a: AssessmentListItem): void {
    this._service.publish(a.id, a.assessmentType)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next:  r   => { this._alertService.success(r.message); this.loadAll(); },
        error: err => this._alertService.error(err.error?.message || 'Failed to update publish status'),
      });
  }

  deleteAssessment(a: AssessmentListItem): void {
    this._alertService.confirm({
      title:       'Delete Assessment',
      message:     `Delete "${a.title}"? This action cannot be undone.`,
      confirmText: 'Delete',
      onConfirm:   () => {
        this._service.delete(a.id, a.assessmentType)
          .pipe(takeUntil(this._destroy$))
          .subscribe({
            next:  () => { this._alertService.success('Assessment deleted'); this.loadAll(); },
            error: err => this._alertService.error(err.error?.message || 'Failed to delete'),
          });
      },
    });
  }

  downloadGradesPdf(a: AssessmentListItem): void {
    if (this.isDownloading) return;
    this.isDownloading = true;
    this._alertService.info('Generating grades PDF…');
    this._reportService.downloadAssessmentGrades(a.id)
      .pipe(takeUntil(this._destroy$), finalize(() => this.isDownloading = false))
      .subscribe({
        next:  r   => r.success
          ? this._alertService.success('Grades report downloaded')
          : this._alertService.error(r.message ?? 'Error generating report'),
        error: err => this._alertService.error(err?.message ?? 'Download failed'),
      });
  }

  downloadListReport(): void {
    if (this.isDownloading) return;
    this.isDownloading = true;
    this._alertService.info('Generating report…');
    this._reportService.downloadAssessmentsList({
      assessmentType: this._filters.assessmentType !== 'all' ? this._filters.assessmentType : null,
      classId:        this._filters.classId        !== 'all' ? this._filters.classId        : null,
    })
    .pipe(takeUntil(this._destroy$), finalize(() => this.isDownloading = false))
    .subscribe({
      next:  r   => r.success
        ? this._alertService.success('Report downloaded')
        : this._alertService.error(r.message ?? 'Error generating report'),
      error: err => this._alertService.error(err?.message ?? 'Download failed'),
    });
  }
}