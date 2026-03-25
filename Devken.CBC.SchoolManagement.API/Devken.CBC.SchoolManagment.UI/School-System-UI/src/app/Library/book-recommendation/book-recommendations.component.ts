// book-recommendation/book-recommendations.component.ts

import {
  Component, OnInit, OnDestroy, AfterViewInit, ViewChild, TemplateRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subject, forkJoin, of } from 'rxjs';
import { catchError, takeUntil, finalize } from 'rxjs/operators';

import { AuthService } from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { SchoolDto } from 'app/Tenant/types/school';

import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';
import {
  DataTableComponent, TableColumn, TableAction, TableHeader, TableEmptyState,
} from 'app/shared/data-table/data-table.component';
import { BaseListComponent } from 'app/shared/Lists/BaseListComponent';

import { BookRecommendationDto } from './Types/book-recommendation.types';
import { BookRecommendationService } from 'app/core/DevKenService/Library/book-recommendation.service';
import { CreateBookRecommendationDialogComponent } from 'app/dialog-modals/Library/book-recommendation-dialog/create-book-recommendation-dialog.component';

@Component({
  selector: 'app-book-recommendations',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatButtonModule, MatDialogModule, MatProgressSpinnerModule,
    PageHeaderComponent, FilterPanelComponent, PaginationComponent,
    StatsCardsComponent, DataTableComponent,
  ],
  templateUrl: './book-recommendations.component.html',
})
export class BookRecommendationsComponent
  extends BaseListComponent<BookRecommendationDto>
  implements OnInit, OnDestroy, AfterViewInit {

  private _destroy$ = new Subject<void>();
  private _alertService!: AlertService;
  private _recommendationService!: BookRecommendationService;
  private _schoolService!: SchoolService;

  @ViewChild('bookCell',    { static: true }) bookCell!:    TemplateRef<any>;
  @ViewChild('studentCell', { static: true }) studentCell!: TemplateRef<any>;
  @ViewChild('scoreCell',   { static: true }) scoreCell!:   TemplateRef<any>;
  @ViewChild('reasonCell',  { static: true }) reasonCell!:  TemplateRef<any>;
  @ViewChild('schoolCell',  { static: true }) schoolCell!:  TemplateRef<any>;

  cellTemplates!: Record<string, TemplateRef<any>>;

  ngAfterViewInit(): void {
    this.cellTemplates = {
      book:    this.bookCell,
      student: this.studentCell,
      score:   this.scoreCell,
      reason:  this.reasonCell,
      school:  this.schoolCell,
    };
  }

  // ── State ─────────────────────────────────────────────────────────────────
  schools:        SchoolDto[] = [];
  isDataLoading   = true;
  showFilterPanel = false;
  currentPage     = 1;
  itemsPerPage    = 10;

  filterValues = {
    search: '', scoreRange: 'all', schoolId: 'all',
  };

  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Library',   url: '/library'   },
    { label: 'Recommendations' },
  ];

  get isSuperAdmin(): boolean { return this._injectedAuth.authUser?.isSuperAdmin ?? false; }

  // ── Table config ──────────────────────────────────────────────────────────

  get tableColumns(): TableColumn<BookRecommendationDto>[] {
    const cols: TableColumn<BookRecommendationDto>[] = [
      { id: 'book',    label: 'Book',    align: 'left', sortable: true },
      { id: 'student', label: 'Student', align: 'left', hideOnMobile: true },
      { id: 'score',   label: 'Score',   align: 'center', sortable: true },
      { id: 'reason',  label: 'Reason',  align: 'left', hideOnMobile: true },
    ];
    if (this.isSuperAdmin) {
      cols.push({ id: 'school', label: 'School', align: 'left', hideOnMobile: true });
    }
    return cols;
  }

  tableActions: TableAction<BookRecommendationDto>[] = [
    {
      id: 'view', label: 'View Details', icon: 'visibility', color: 'indigo',
      handler: r => this._openView(r),
      divider: true,
    },
    {
      id: 'edit', label: 'Edit', icon: 'edit', color: 'blue',
      handler: r => this._openEdit(r),
    },
    {
      id: 'delete', label: 'Delete', icon: 'delete', color: 'red',
      handler: r => this._confirmDelete(r),
    },
  ];

  tableHeader: TableHeader = {
    title:        'Book Recommendations',
    subtitle:     '',
    icon:         'stars',
    iconGradient: 'bg-gradient-to-br from-amber-500 via-orange-600 to-yellow-600',
  };

  tableEmptyState: TableEmptyState = {
    icon:        'stars',
    message:     'No recommendations found',
    description: 'Try adjusting your filters or create a new recommendation',
    action:      { label: 'New Recommendation', icon: 'add', handler: () => this._openCreate() },
  };

  filterFields: FilterField[] = [];

  // ── Stats ─────────────────────────────────────────────────────────────────

  get statsCards(): StatCard[] {
    const data = this.dataSource.data;
    const avgScore = data.length > 0 ? Math.round(data.reduce((sum, r) => sum + r.score, 0) / data.length) : 0;
    const highScoreCount = data.filter(r => r.score >= 80).length;
    const base: StatCard[] = [
      { label: 'Total Recommendations', value: data.length,       icon: 'stars',       iconColor: 'amber'  },
      { label: 'Average Score',         value: `${avgScore}%`,    icon: 'trending_up', iconColor: 'green'  },
      { label: 'High Scores (≥80)',     value: highScoreCount,    icon: 'grade',       iconColor: 'blue'   },
      { label: 'Students',              value: new Set(data.map(r => r.studentId)).size, icon: 'people', iconColor: 'indigo' },
    ];
    if (this.isSuperAdmin) {
      base.push({ label: 'Schools', value: new Set(data.map(r => r.schoolId)).size, icon: 'school', iconColor: 'violet' });
    }
    return base;
  }

  // ── Filtered & Paginated ──────────────────────────────────────────────────

  get filteredData(): BookRecommendationDto[] {
    const q = this.filterValues.search.toLowerCase();
    return this.dataSource.data.filter(r =>
      (!q || r.bookTitle?.toLowerCase().includes(q)
          || r.studentName?.toLowerCase().includes(q)
          || r.reason?.toLowerCase().includes(q)) &&
      (this.filterValues.scoreRange === 'all'
        || (this.filterValues.scoreRange === 'high'   && r.score >= 80)
        || (this.filterValues.scoreRange === 'medium' && r.score >= 50 && r.score < 80)
        || (this.filterValues.scoreRange === 'low'    && r.score < 50)) &&
      (this.filterValues.schoolId === 'all' || r.schoolId === this.filterValues.schoolId)
    );
  }

  get paginatedData(): BookRecommendationDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  // ── Constructor ───────────────────────────────────────────────────────────

  constructor(
    dialog:                MatDialog,
    snackBar:              MatSnackBar,
    alertService:          AlertService,
    recommendationService: BookRecommendationService,
    private readonly _injectedAuth:  AuthService,
    schoolService:         SchoolService,
  ) {
    super(recommendationService, dialog, snackBar);
    this._alertService          = alertService;
    this._recommendationService = recommendationService;
    this._schoolService         = schoolService;
  }

  ngOnInit():    void { this._loadMeta(); }
  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  // ── Data ──────────────────────────────────────────────────────────────────

  protected override loadAll(): void {
    this.isLoading = true;
    const schoolId = this.filterValues.schoolId !== 'all' ? this.filterValues.schoolId : undefined;
    this._recommendationService.getAll(schoolId).pipe(takeUntil(this._destroy$)).subscribe({
      next: res => {
        if (res.success) {
          this.dataSource.data = res.data;
          this.tableHeader.subtitle = `${this.filteredData.length} recommendations found`;
        } else {
          this._alertService.error(res.message || 'Failed to load recommendations');
        }
        this.isLoading = false;
      },
      error: err => {
        this._alertService.error(err.error?.message || 'Failed to load recommendations');
        this.isLoading = false;
      }
    });
  }

  private _loadMeta(): void {
    this.isDataLoading = true;
    const requests: any = {};

    if (this.isSuperAdmin) {
      requests.schools = this._schoolService.getAll().pipe(catchError(() => of({ success: false, data: [] })));
    }

    if (Object.keys(requests).length === 0) {
      this.isDataLoading = false;
      this._initFilters();
      this.loadAll();
      return;
    }

    forkJoin(requests).pipe(
      takeUntil(this._destroy$),
      finalize(() => { this.isDataLoading = false; })
    ).subscribe({
      next: (res: any) => {
        if (res.schools) this.schools = res.schools?.data || [];
        this._initFilters();
        this.loadAll();
      },
      error: () => { this._initFilters(); this.loadAll(); }
    });
  }

  // ── Filters ───────────────────────────────────────────────────────────────

  private _initFilters(): void {
    this.filterFields = [
      { id: 'search', label: 'Search', type: 'text', placeholder: 'Book, student, or reason...', value: '' },
    ];

    if (this.isSuperAdmin) {
      this.filterFields.push({
        id: 'schoolId', label: 'School', type: 'select', value: 'all',
        options: [
          { label: 'All Schools', value: 'all' },
          ...this.schools.map(s => ({ label: s.name, value: s.id })),
        ],
      });
    }

    this.filterFields.push({
      id: 'scoreRange', label: 'Score Range', type: 'select', value: 'all',
      options: [
        { label: 'All Scores', value: 'all'    },
        { label: 'High (≥80)', value: 'high'   },
        { label: 'Medium (50-79)', value: 'medium' },
        { label: 'Low (<50)',  value: 'low'    },
      ],
    });
  }

  toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

  onFilterChange(event: FilterChangeEvent): void {
    (this.filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} recommendations found`;
    if (event.filterId === 'schoolId' && this.isSuperAdmin) this.loadAll();
  }

  onClearFilters(): void {
    this.filterValues = { search: '', scoreRange: 'all', schoolId: 'all' };
    this.filterFields.forEach(f => { f.value = (this.filterValues as any)[f.id]; });
    this.currentPage = 1;
    this.loadAll();
  }

  onPageChange(page: number):      void { this.currentPage = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

  // ── CRUD & Actions ────────────────────────────────────────────────────────

  openCreate(): void { this._openCreate(); }

  private _openCreate(): void {
    this.openDialog(CreateBookRecommendationDialogComponent, {
      panelClass: ['book-recommendation-dialog', 'no-padding-dialog'],
      width: '700px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: true, autoFocus: 'input',
      data: { mode: 'create' },
    });
  }

  private _openEdit(recommendation: BookRecommendationDto): void {
    this.openDialog(CreateBookRecommendationDialogComponent, {
      panelClass: ['book-recommendation-dialog', 'no-padding-dialog'],
      width: '700px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: true, autoFocus: 'input',
      data: { mode: 'edit', recommendation },
    });
  }

  private _openView(recommendation: BookRecommendationDto): void {
    this.openDialog(CreateBookRecommendationDialogComponent, {
      panelClass: ['book-recommendation-dialog', 'no-padding-dialog'],
      width: '750px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: false,
      data: { mode: 'view', recommendation },
    });
  }

  private _confirmDelete(recommendation: BookRecommendationDto): void {
    this._alertService.confirm({
      title:       'Delete Recommendation',
      message:     `Delete recommendation for "${recommendation.bookTitle}" to "${recommendation.studentName}"? This cannot be undone.`,
      confirmText: 'Delete',
      onConfirm: () => {
        this.isLoading = true;
        this._recommendationService.delete(recommendation.id).pipe(takeUntil(this._destroy$)).subscribe({
          next: res => {
            if (res.success) {
              this._alertService.success('Recommendation deleted successfully');
              if (this.paginatedData.length === 1 && this.currentPage > 1) this.currentPage--;
              this.loadAll();
            } else {
              this._alertService.error(res.message || 'Failed to delete');
            }
            this.isLoading = false;
          },
          error: err => {
            this._alertService.error(err.error?.message || 'Failed to delete');
            this.isLoading = false;
          }
        });
      },
    });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  getScoreColor(score: number): string {
    if (score >= 80) return 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400';
    if (score >= 50) return 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400';
    return 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400';
  }

  getScoreIcon(score: number): string {
    if (score >= 80) return 'grade';
    if (score >= 50) return 'star_half';
    return 'star_outline';
  }

  truncateText(text: string, maxLength: number = 100): string {
    if (!text) return '—';
    return text.length > maxLength ? text.substring(0, maxLength) + '...' : text;
  }
}