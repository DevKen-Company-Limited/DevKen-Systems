// ═══════════════════════════════════════════════════════════════════
// summative-assessments.component.ts  (List Page)
// ═══════════════════════════════════════════════════════════════════

import {
  Component, OnInit, OnDestroy, ViewChild, TemplateRef, inject, AfterViewInit,
} from '@angular/core';
import { CommonModule }                     from '@angular/common';
import { FormsModule }                      from '@angular/forms';
import { Router }                           from '@angular/router';
import { MatIconModule }                    from '@angular/material/icon';
import { MatButtonModule }                  from '@angular/material/button';
import { MatMenuModule }                    from '@angular/material/menu';
import { MatProgressSpinnerModule }         from '@angular/material/progress-spinner';
import { MatTooltipModule }                 from '@angular/material/tooltip';
import { MatDividerModule }                 from '@angular/material/divider';
import { Subject, forkJoin, of }            from 'rxjs';
import { takeUntil, catchError, finalize }  from 'rxjs/operators';

import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PaginationComponent }             from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard }   from 'app/shared/stats-cards/stats-cards.component';
import { DataTableComponent, TableColumn, TableAction, TableHeader, TableEmptyState } from 'app/shared/data-table/data-table.component';
import { AlertService }                    from 'app/core/DevKenService/Alert/AlertService';
import { AuthService }                     from 'app/core/auth/auth.service';
import { SummativeAssessmentReportService } from 'app/core/DevKenService/assessments/Summative/summative-assessment-report.service';
import { SummativeAssessmentService } from 'app/core/DevKenService/assessments/Summative/summative-assessment.service';
import { SummativeAssessmentDto, EXAM_TYPES } from '../types/summative-assessment.types';



@Component({
  selector: 'app-summative-assessments',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatButtonModule, MatMenuModule,
    MatProgressSpinnerModule, MatTooltipModule, MatDividerModule,
    PageHeaderComponent, FilterPanelComponent, PaginationComponent,
    StatsCardsComponent, DataTableComponent,
  ],
  templateUrl: './summative-assessments.component.html',
})
export class SummativeAssessmentsComponent implements OnInit, AfterViewInit, OnDestroy {

  @ViewChild('titleCell')    titleCellTemplate!:    TemplateRef<any>;
  @ViewChild('examTypeCell') examTypeCellTemplate!: TemplateRef<any>;
  @ViewChild('classCell')    classCellTemplate!:    TemplateRef<any>;
  @ViewChild('scoreCell')    scoreCellTemplate!:    TemplateRef<any>;
  @ViewChild('statusCell')   statusCellTemplate!:   TemplateRef<any>;
  @ViewChild('dateCell')     dateCellTemplate!:     TemplateRef<any>;

  private _destroy$       = new Subject<void>();
  private _authService    = inject(AuthService);
  private _router         = inject(Router);
  private _alertService   = inject(AlertService);
  private _reportService  = inject(SummativeAssessmentReportService);

  // ─── Auth ─────────────────────────────────────────────────────────────────
  get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }

  // ─── Breadcrumbs ──────────────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard',    url: '/dashboard'    },
    { label: 'Assessments',  url: '/assessments'  },
    { label: 'Summative' },
  ];

  // ─── Stats ────────────────────────────────────────────────────────────────
  get statsCards(): StatCard[] {
    return [
      { label: 'Total Exams',    value: this.allData.length,                                              icon: 'assignment',      iconColor: 'indigo' },
      { label: 'Published',      value: this.allData.filter(a => a.isPublished).length,                   icon: 'publish',         iconColor: 'green'  },
      { label: 'End Term',       value: this.allData.filter(a => a.examType === 'EndTerm').length,        icon: 'event',           iconColor: 'blue'   },
      { label: 'Has Practical',  value: this.allData.filter(a => a.hasPracticalComponent).length,         icon: 'science',         iconColor: 'amber'  },
    ];
  }

  // ─── Table ────────────────────────────────────────────────────────────────
  get tableColumns(): TableColumn<SummativeAssessmentDto>[] {
    return [
      { id: 'title',    label: 'Assessment',    align: 'left',   sortable: true },
      { id: 'examType', label: 'Exam Type',     align: 'left',   hideOnMobile: true },
      { id: 'class',    label: 'Class / Term',  align: 'left',   hideOnMobile: true },
      { id: 'score',    label: 'Score & Pass',  align: 'left',   hideOnTablet: true },
      { id: 'date',     label: 'Date',          align: 'left',   hideOnTablet: true },
      { id: 'status',   label: 'Status',        align: 'center' },
    ];
  }

  tableActions: TableAction<SummativeAssessmentDto>[] = [
    { id: 'view',      label: 'View & Scores', icon: 'visibility',    color: 'blue',   handler: a => this.viewAssessment(a)    },
    { id: 'edit',      label: 'Edit',          icon: 'edit',          color: 'indigo', handler: a => this.editAssessment(a)    },
    { id: 'grade',     label: 'Grade Students',icon: 'grading',       color: 'teal',   handler: a => this.gradeAssessment(a)   },
    { id: 'positions', label: 'Recalc Positions', icon: 'leaderboard', color: 'violet', handler: a => this.recalcPositions(a) },
    { id: 'report',    label: 'Score Sheet PDF', icon: 'download',    color: 'green',  handler: a => this.downloadReport(a)   },
    {
      id: 'publish', label: 'Publish', icon: 'publish', color: 'green',
      handler: a => this.togglePublish(a), visible: a => !a.isPublished,
    },
    {
      id: 'unpublish', label: 'Unpublish', icon: 'unpublished', color: 'amber', divider: true,
      handler: a => this.togglePublish(a), visible: a => a.isPublished,
    },
    { id: 'delete', label: 'Delete', icon: 'delete', color: 'red', handler: a => this.deleteAssessment(a) },
  ];

  tableHeader: TableHeader = {
    title:        'Summative Assessments',
    subtitle:     '',
    icon:         'assignment',
    iconGradient: 'bg-gradient-to-br from-violet-500 via-purple-600 to-indigo-700',
  };

  tableEmptyState: TableEmptyState = {
    icon:        'assignment_late',
    message:     'No summative assessments found',
    description: 'Create your first exam or adjust your filters',
    action: { label: 'Add', icon: 'add', handler: () => this.createAssessment() },
  };

  cellTemplates: { [key: string]: TemplateRef<any> } = {};

  // ─── State ────────────────────────────────────────────────────────────────
  allData:         SummativeAssessmentDto[] = [];
  classes:         any[] = [];
  terms:           any[] = [];
  isLoading        = false;
  isDownloading    = false;
  showFilterPanel  = false;
  filterFields:    FilterField[] = [];
  currentPage      = 1;
  itemsPerPage     = 10;

  private _filters = { search: '', examType: 'all', classId: 'all', published: 'all' };

  // ─── Computed ─────────────────────────────────────────────────────────────
  get filteredData(): SummativeAssessmentDto[] {
    return this.allData.filter(a => {
      const q = this._filters.search.toLowerCase();
      return (
        (!q || a.title?.toLowerCase().includes(q) ||
               a.teacherName?.toLowerCase().includes(q) ||
               a.subjectName?.toLowerCase().includes(q)) &&
        (this._filters.examType  === 'all' || a.examType === this._filters.examType) &&
        (this._filters.classId   === 'all' || a.classId  === this._filters.classId) &&
        (this._filters.published === 'all' ||
          (this._filters.published === 'published'   &&  a.isPublished) ||
          (this._filters.published === 'unpublished' && !a.isPublished))
      );
    });
  }

  get paginatedData(): SummativeAssessmentDto[] {
    const s = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(s, s + this.itemsPerPage);
  }

  // ─── Exam type badge colors ────────────────────────────────────────────────
  getExamTypeColor(type?: string): string {
    const map: Record<string, string> = {
      EndTerm: 'indigo', MidTerm: 'blue', Final: 'violet', CAT: 'teal', Mock: 'amber',
    };
    return map[type ?? ''] ?? 'gray';
  }

  // ─── Lifecycle ────────────────────────────────────────────────────────────
  constructor(private _service: SummativeAssessmentService) {}

  ngOnInit(): void { this.loadData(); }

  ngAfterViewInit(): void {
    this.cellTemplates = {
      title:    this.titleCellTemplate,
      examType: this.examTypeCellTemplate,
      class:    this.classCellTemplate,
      score:    this.scoreCellTemplate,
      date:     this.dateCellTemplate,
      status:   this.statusCellTemplate,
    };
  }

  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  // ─── Load ─────────────────────────────────────────────────────────────────
  private loadData(): void {
    this.isLoading = true;
    forkJoin({
      assessments: this._service.getAll().pipe(catchError(() => of([]))),
      classes:     this._service.getClasses().pipe(catchError(() => of([]))),
      terms:       this._service.getTerms().pipe(catchError(() => of([]))),
    })
    .pipe(takeUntil(this._destroy$), finalize(() => this.isLoading = false))
    .subscribe(({ assessments, classes, terms }) => {
      this.allData = assessments;
      this.classes = classes;
      this.terms   = terms;
      this.buildFilterFields();
      this.tableHeader.subtitle = `${this.filteredData.length} assessments found`;
    });
  }

  loadAll(): void { this.loadData(); }

  private buildFilterFields(): void {
    this.filterFields = [
      { id: 'search',    label: 'Search',     type: 'text',   placeholder: 'Title, teacher, subject…', value: this._filters.search },
      { id: 'examType',  label: 'Exam Type',  type: 'select', value: this._filters.examType,
        options: [{ label: 'All Types', value: 'all' }, ...EXAM_TYPES.map(t => ({ label: t, value: t }))] },
      { id: 'classId',   label: 'Class',      type: 'select', value: this._filters.classId,
        options: [{ label: 'All Classes', value: 'all' }, ...this.classes.map(c => ({ label: c.name, value: c.id }))] },
      { id: 'published', label: 'Status',     type: 'select', value: this._filters.published,
        options: [{ label: 'All', value: 'all' }, { label: 'Published', value: 'published' }, { label: 'Unpublished', value: 'unpublished' }] },
    ];
  }

  // ─── Filter ───────────────────────────────────────────────────────────────
  toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

  onFilterChange(e: FilterChangeEvent): void {
    (this._filters as any)[e.filterId] = e.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} assessments found`;
  }

  onClearFilters(): void {
    this._filters = { search: '', examType: 'all', classId: 'all', published: 'all' };
    this.filterFields.forEach(f => { f.value = (this._filters as any)[f.id]; });
    this.currentPage = 1;
  }

  onPageChange(p: number): void          { this.currentPage  = p; }
  onItemsPerPageChange(n: number): void  { this.itemsPerPage = n; this.currentPage = 1; }

  // ─── Navigation ───────────────────────────────────────────────────────────
  createAssessment():                          void { this._router.navigate(['/assessment/summative/create']); }
  viewAssessment(a: SummativeAssessmentDto):   void { this._router.navigate(['/assessment/summative/details', a.id]); }
  editAssessment(a: SummativeAssessmentDto):   void { this._router.navigate(['/assessment/summative/edit', a.id]); }
  gradeAssessment(a: SummativeAssessmentDto):  void { this._router.navigate(['/assessment/summative/grade', a.id]); }

  // ─── Actions ─────────────────────────────────────────────────────────────
  togglePublish(a: SummativeAssessmentDto): void {
    const next = !a.isPublished;
    this._service.publish(a.id, next)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next:  r  => { this._alertService.success(r.message); this.loadAll(); },
        error: err => this._alertService.error(err.error?.message || 'Failed to update publish status'),
      });
  }

  recalcPositions(a: SummativeAssessmentDto): void {
    this._alertService.confirm({
      title: 'Recalculate Positions',
      message: `Recalculate class positions for "${a.title}"? This will re-rank all students by total score.`,
      confirmText: 'Recalculate',
      onConfirm: () => {
        this._service.recalculatePositions(a.id)
          .pipe(takeUntil(this._destroy$))
          .subscribe({
            next:  r  => this._alertService.success(r.message || 'Positions recalculated'),
            error: err => this._alertService.error(err.error?.message || 'Failed to recalculate'),
          });
      },
    });
  }

  deleteAssessment(a: SummativeAssessmentDto): void {
    this._alertService.confirm({
      title: 'Delete Assessment',
      message: `Delete "${a.title}" and all its scores? This cannot be undone.`,
      confirmText: 'Delete',
      onConfirm: () => {
        this._service.delete(a.id)
          .pipe(takeUntil(this._destroy$))
          .subscribe({
            next:  () => { this._alertService.success('Assessment deleted'); this.loadAll(); },
            error: err => this._alertService.error(err.error?.message || 'Failed to delete'),
          });
      },
    });
  }

  downloadReport(a: SummativeAssessmentDto): void {
    if (this.isDownloading) return;
    this.isDownloading = true;
    this._alertService.info('Generating score sheet PDF…');
    this._reportService.downloadScoreSheet(a.id)
      .pipe(takeUntil(this._destroy$), finalize(() => this.isDownloading = false))
      .subscribe({
        next:  r  => r.success ? this._alertService.success('Score sheet downloaded') : this._alertService.error(r.message ?? 'Error'),
        error: err => this._alertService.error(err?.message ?? 'Download failed'),
      });
  }

  downloadListReport(): void {
    if (this.isDownloading) return;
    this.isDownloading = true;
    this._alertService.info('Generating report…');
    this._reportService.downloadAssessmentsList()
      .pipe(takeUntil(this._destroy$), finalize(() => this.isDownloading = false))
      .subscribe({
        next:  r  => r.success ? this._alertService.success('Report downloaded') : this._alertService.error(r.message ?? 'Error'),
        error: err => this._alertService.error(err?.message ?? 'Download failed'),
      });
  }
}