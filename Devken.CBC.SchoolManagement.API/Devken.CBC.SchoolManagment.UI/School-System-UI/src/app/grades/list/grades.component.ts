// list/grades.component.ts
import {
  Component, OnInit, OnDestroy,
  ViewChild, TemplateRef, inject,
} from '@angular/core';
import { CommonModule }             from '@angular/common';
import { FormsModule }              from '@angular/forms';
import { Router }                   from '@angular/router';
import { MatIconModule }            from '@angular/material/icon';
import { MatButtonModule }          from '@angular/material/button';
import { MatMenuModule }            from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule }         from '@angular/material/tooltip';
import { Subject, of }              from 'rxjs';
import { catchError, takeUntil }    from 'rxjs/operators';

import { AuthService }   from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { AlertService }  from 'app/core/DevKenService/Alert/AlertService';
import { SchoolDto }     from 'app/Tenant/types/school';
import { GradeService }  from 'app/core/DevKenService/GradeService/GradeService';
import { GradeDto }      from '../types/gradedto';
import {
  GradeLetterOptions, GradeTypeOptions,
  getGradeLetterLabel, getGradeTypeLabel,
  getGradeLetterColor, getPercentageColor, getPerformanceLabel,
} from '../types/GradeEnums';

import { PageHeaderComponent, Breadcrumb }                            from 'app/shared/Page-Header/page-header.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent }       from 'app/shared/Filter/filter-panel.component';
import { PaginationComponent }                                        from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard }                              from 'app/shared/stats-cards/stats-cards.component';
import { DataTableComponent, TableColumn, TableAction, TableHeader, TableEmptyState } from 'app/shared/data-table/data-table.component';

@Component({
  selector: 'app-grades',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatButtonModule, MatMenuModule,
    MatProgressSpinnerModule, MatTooltipModule,
    PageHeaderComponent, FilterPanelComponent, PaginationComponent,
    StatsCardsComponent, DataTableComponent,
  ],
  templateUrl: './grades.component.html',
})
export class GradesComponent implements OnInit, OnDestroy {

  @ViewChild('studentCell')   studentCellTemplate!:   TemplateRef<any>;
  @ViewChild('subjectCell')   subjectCellTemplate!:   TemplateRef<any>;
  @ViewChild('scoreCell')     scoreCellTemplate!:     TemplateRef<any>;
  @ViewChild('gradeCell')     gradeCellTemplate!:     TemplateRef<any>;
  @ViewChild('typeCell')      typeCellTemplate!:      TemplateRef<any>;
  @ViewChild('statusCell')    statusCellTemplate!:    TemplateRef<any>;
  @ViewChild('schoolCell')    schoolCellTemplate!:    TemplateRef<any>;

  private _unsubscribe   = new Subject<void>();
  private _router        = inject(Router);
  private _authService   = inject(AuthService);
  private _schoolService = inject(SchoolService);
  private _alertService  = inject(AlertService);

  // ─── Breadcrumbs ─────────────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Academic',  url: '/academic'  },
    { label: 'Grades' },
  ];

  // ─── Auth ─────────────────────────────────────────────────────────────────
  get isSuperAdmin(): boolean {
    return this._authService.authUser?.isSuperAdmin ?? false;
  }

  // ─── Schools ──────────────────────────────────────────────────────────────
  schools: SchoolDto[] = [];

  // ─── Stats ────────────────────────────────────────────────────────────────
  get statsCards(): StatCard[] {
    const cards: StatCard[] = [
      { label: 'Total Grades',   value: this.total,          icon: 'grade',        iconColor: 'indigo' },
      { label: 'Finalized',      value: this.finalizedCount, icon: 'check_circle', iconColor: 'green'  },
      { label: 'Pending',        value: this.pendingCount,   icon: 'hourglass_top',iconColor: 'amber'  },
      { label: 'Avg Percentage', value: this.avgPercentage,  icon: 'percent',      iconColor: 'violet' },
    ];
    if (this.isSuperAdmin) {
      cards.push({ label: 'Schools', value: this.schoolsCount, icon: 'school', iconColor: 'blue' });
    }
    return cards;
  }

  // ─── Table Columns ────────────────────────────────────────────────────────
  get tableColumns(): TableColumn<GradeDto>[] {
    const cols: TableColumn<GradeDto>[] = [
      { id: 'student', label: 'Student',      align: 'left', sortable: true },
      { id: 'subject', label: 'Subject',      align: 'left' },
    ];
    if (this.isSuperAdmin) {
      cols.push({ id: 'school', label: 'School', align: 'left', hideOnMobile: true });
    }
    cols.push(
      { id: 'score',  label: 'Score',        align: 'center' },
      { id: 'grade',  label: 'Grade',        align: 'center' },
      { id: 'type',   label: 'Type',         align: 'left',   hideOnMobile: true },
      { id: 'status', label: 'Status',       align: 'center' },
    );
    return cols;
  }

  // ─── Table Actions ────────────────────────────────────────────────────────
  tableActions: TableAction<GradeDto>[] = [
    { id: 'view',     label: 'View Details', icon: 'visibility',   color: 'blue',   handler: g => this.viewGrade(g)     },
    { id: 'edit',     label: 'Edit',         icon: 'edit',         color: 'indigo', handler: g => this.editGrade(g),    visible: g => !g.isFinalized },
    {
      id: 'finalize', label: 'Finalize',     icon: 'lock',         color: 'green',
      handler:  g => this.finalizeGrade(g),
      visible:  g => !g.isFinalized,
    },
    { id: 'delete',   label: 'Delete',       icon: 'delete',       color: 'red',    handler: g => this.removeGrade(g),  visible: g => !g.isFinalized },
  ];

  tableHeader: TableHeader = {
    title:        'Grades List',
    subtitle:     '',
    icon:         'grade',
    iconGradient: 'bg-gradient-to-br from-indigo-500 via-violet-600 to-purple-700',
  };

  tableEmptyState: TableEmptyState = {
    icon:        'school',
    message:     'No grades found',
    description: 'Try adjusting your filters or add a new grade',
    action: { label: 'Add Grade', icon: 'add', handler: () => this.createGrade() },
  };

  // ─── State ────────────────────────────────────────────────────────────────
  cellTemplates: { [key: string]: TemplateRef<any> } = {};
  filterFields:  FilterField[] = [];
  showFilterPanel = false;
  allData:   GradeDto[] = [];
  isLoading  = false;

  private _filterValues = {
    search: '', isFinalized: 'all', gradeType: 'all', schoolId: 'all',
  };

  currentPage  = 1;
  itemsPerPage = 10;

  // ─── Helpers ─────────────────────────────────────────────────────────────
  getGradeLetterLabel  = getGradeLetterLabel;
  getGradeTypeLabel    = getGradeTypeLabel;
  getGradeLetterColor  = getGradeLetterColor;
  getPercentageColor   = getPercentageColor;
  getPerformanceLabel  = getPerformanceLabel;

  // ─── Computed ─────────────────────────────────────────────────────────────
  get total():          number { return this.allData.length; }
  get finalizedCount(): number { return this.allData.filter(g => g.isFinalized).length; }
  get pendingCount():   number { return this.allData.filter(g => !g.isFinalized).length; }
  get schoolsCount():   number { return new Set(this.allData.map(g => g.schoolId)).size; }

  get avgPercentage(): string {
    const withPct = this.allData.filter(g => g.percentage !== null);
    if (!withPct.length) return '—';
    const avg = withPct.reduce((s, g) => s + (g.percentage ?? 0), 0) / withPct.length;
    return `${Math.round(avg)}%`;
  }

  get filteredData(): GradeDto[] {
    return this.allData.filter(g => {
      const q        = this._filterValues.search.toLowerCase();
      const typeName = getGradeTypeLabel(g.gradeType);

      return (
        (!q || g.studentName?.toLowerCase().includes(q) ||
               g.subjectName?.toLowerCase().includes(q) ||
               g.schoolName?.toLowerCase().includes(q)) &&
        (this._filterValues.isFinalized === 'all' ||
          (this._filterValues.isFinalized === 'finalized' &&  g.isFinalized) ||
          (this._filterValues.isFinalized === 'pending'   && !g.isFinalized)) &&
        (this._filterValues.gradeType === 'all' || typeName === this._filterValues.gradeType) &&
        (this._filterValues.schoolId  === 'all' || g.schoolId === this._filterValues.schoolId)
      );
    });
  }

  get paginatedData(): GradeDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  constructor(private _service: GradeService) {}

  // ─── Lifecycle ────────────────────────────────────────────────────────────
  ngOnInit():      void { this._loadInit(); }
  ngAfterViewInit(): void {
    this.cellTemplates = {
      student: this.studentCellTemplate,
      subject: this.subjectCellTemplate,
      school:  this.schoolCellTemplate,
      score:   this.scoreCellTemplate,
      grade:   this.gradeCellTemplate,
      type:    this.typeCellTemplate,
      status:  this.statusCellTemplate,
    };
  }
  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  // ─── Init ─────────────────────────────────────────────────────────────────
  private _loadInit(): void {
    if (this.isSuperAdmin) {
      this._schoolService.getAll().pipe(
        catchError(() => of({ success: false, message: '', data: [] as SchoolDto[] })),
        takeUntil(this._unsubscribe),
      ).subscribe(res => {
        this.schools = (res as any).data ?? [];
        this._initFilterFields();
        this.loadAll();
      });
    } else {
      this._initFilterFields();
      this.loadAll();
    }
  }

  private _initFilterFields(): void {
    this.filterFields = [
      {
        id: 'search', label: 'Search', type: 'text',
        placeholder: 'Student name, subject…', value: this._filterValues.search,
      },
    ];

    if (this.isSuperAdmin) {
      this.filterFields.push({
        id: 'schoolId', label: 'School', type: 'select', value: this._filterValues.schoolId,
        options: [
          { label: 'All Schools', value: 'all' },
          ...this.schools.map(s => ({ label: s.name, value: s.id })),
        ],
      });
    }

    this.filterFields.push(
      {
        id: 'isFinalized', label: 'Status', type: 'select', value: this._filterValues.isFinalized,
        options: [
          { label: 'All Statuses', value: 'all'       },
          { label: 'Finalized',    value: 'finalized' },
          { label: 'Pending',      value: 'pending'   },
        ],
      },
      {
        id: 'gradeType', label: 'Grade Type', type: 'select', value: this._filterValues.gradeType,
        options: [
          { label: 'All Types', value: 'all' },
          ...GradeTypeOptions.map(o => ({ label: o.label, value: o.label })),
        ],
      },
    );
  }

  // ─── Filter events ────────────────────────────────────────────────────────
  toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

  onFilterChange(event: FilterChangeEvent): void {
    (this._filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} grades found`;
    if (event.filterId === 'schoolId' && this.isSuperAdmin) {
      this.loadAll(event.value === 'all' ? null : event.value);
    }
  }

  onClearFilters(): void {
    this._filterValues = { search: '', isFinalized: 'all', gradeType: 'all', schoolId: 'all' };
    this.filterFields.forEach(f => { f.value = (this._filterValues as any)[f.id]; });
    this.currentPage = 1;
    this.loadAll();
  }

  onPageChange(page: number):      void { this.currentPage  = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

  // ─── Data ─────────────────────────────────────────────────────────────────
  loadAll(schoolId?: string | null): void {
    this.isLoading = true;
    this._service.getAll(schoolId)
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next: res => {
          this.allData = Array.isArray(res) ? res : [];
          this.tableHeader.subtitle = `${this.filteredData.length} grades found`;
          this.isLoading = false;
        },
        error: err => {
          this.isLoading = false;
          this._alertService.error(err.error?.message || 'Failed to load grades');
        },
      });
  }

  // ─── Actions ─────────────────────────────────────────────────────────────
  createGrade(): void { this._router.navigate(['/academic/grades/create']); }
  viewGrade(g: GradeDto):  void { this._router.navigate(['/academic/grades/details', g.id]); }
  editGrade(g: GradeDto):  void { this._router.navigate(['/academic/grades/edit',    g.id]); }

  finalizeGrade(grade: GradeDto): void {
    this._alertService.confirm({
      title:       'Finalize Grade',
      message:     `Finalize the grade for "${grade.studentName}"? This cannot be undone.`,
      confirmText: 'Finalize',
      cancelText:  'Cancel',
      onConfirm: () => {
        this._service.finalize(grade.id).subscribe({
          next:  () => { this._alertService.success('Grade finalized successfully'); this.loadAll(); },
          error: err => this._alertService.error(err.error?.message || 'Failed to finalize grade'),
        });
      },
    });
  }

  removeGrade(grade: GradeDto): void {
    this._alertService.confirm({
      title:       'Delete Grade',
      message:     `Delete grade for "${grade.studentName}" in "${grade.subjectName}"? This cannot be undone.`,
      confirmText: 'Delete',
      cancelText:  'Cancel',
      onConfirm: () => {
        this._service.delete(grade.id).subscribe({
          next:  () => {
            this._alertService.success('Grade deleted successfully');
            if (this.paginatedData.length === 0 && this.currentPage > 1) this.currentPage--;
            this.loadAll();
          },
          error: err => this._alertService.error(err.error?.message || 'Failed to delete grade'),
        });
      },
    });
  }
}