import {
  Component, OnInit, OnDestroy, AfterViewInit,
  ViewChild, TemplateRef, inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Subject } from 'rxjs';
import { takeUntil, finalize } from 'rxjs/operators';

import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { AuthService } from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { SchoolDto } from 'app/Tenant/types/school';

import { FuseConfirmationService } from '@fuse/services/confirmation';

import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';
import {
  DataTableComponent, TableColumn, TableAction, TableHeader, TableEmptyState
} from 'app/shared/data-table/data-table.component';
import { AssessmentService } from 'app/core/DevKenService/assessments/Assessments/AssessmentService';
import { AssessmentDto } from '../types/AssessmentDtos';
import { CreateEditAssessmentDialogComponent, CreateEditAssessmentDialogData } from 'app/dialog-modals/assessments/Assessments/create-edit-assessment-dialog.component';

@Component({
  selector: 'app-assessments',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatButtonModule, MatDialogModule, MatProgressSpinnerModule,
    PageHeaderComponent, FilterPanelComponent,
    PaginationComponent, StatsCardsComponent, DataTableComponent,
  ],
  templateUrl: './assessments.component.html',
})
export class AssessmentsComponent implements OnInit, AfterViewInit, OnDestroy {

  // ── ViewChild cell templates ─────────────────────────────────────────────
  @ViewChild('titleCell')       titleCellTemplate!:       TemplateRef<any>;
  @ViewChild('typeCell')        typeCellTemplate!:        TemplateRef<any>;
  @ViewChild('classSubjectCell') classSubjectCellTemplate!: TemplateRef<any>;
  @ViewChild('teacherCell')     teacherCellTemplate!:     TemplateRef<any>;
  @ViewChild('dateScoreCell')   dateScoreCellTemplate!:   TemplateRef<any>;
  @ViewChild('schoolCell')      schoolCellTemplate!:      TemplateRef<any>;
  @ViewChild('publishedCell')   publishedCellTemplate!:   TemplateRef<any>;

  private _unsubscribe = new Subject<void>();

  // ── DI ──────────────────────────────────────────────────────────────────
  private _service      = inject(AssessmentService);
  private _alert        = inject(AlertService);
  private _auth         = inject(AuthService);
  private _schoolSvc    = inject(SchoolService);
  private _dialog       = inject(MatDialog);
  private _confirmation = inject(FuseConfirmationService);

  // ── Auth helpers ─────────────────────────────────────────────────────────
  get isSuperAdmin(): boolean { return this._auth.authUser?.isSuperAdmin ?? false; }

  // ── State ────────────────────────────────────────────────────────────────
  allData:   AssessmentDto[] = [];
  schools:   SchoolDto[]     = [];
  isLoading  = false;

  // ── Filter state ─────────────────────────────────────────────────────────
  showFilterPanel = false;
  filterFields:   FilterField[] = [];

  private _filters = {
    search:         '',
    assessmentType: 'all',
    published:      'all',
    schoolId:       'all',
  };

  // ── Pagination ───────────────────────────────────────────────────────────
  currentPage  = 1;
  itemsPerPage = 10;

  // ── Cell templates map ───────────────────────────────────────────────────
  cellTemplates: { [key: string]: TemplateRef<any> } = {};

  // ── Breadcrumbs ──────────────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Academic', url: '/academic' },
    { label: 'Assessments' },
  ];

  // ── Stats ────────────────────────────────────────────────────────────────
  get statsCards(): StatCard[] {
    const cards: StatCard[] = [
      { label: 'Total',      value: this.allData.length,                                   icon: 'assignment',   iconColor: 'indigo' },
      { label: 'Published',  value: this.allData.filter(a => a.isPublished).length,        icon: 'check_circle', iconColor: 'green'  },
      { label: 'Draft',      value: this.allData.filter(a => !a.isPublished).length,       icon: 'edit_note',    iconColor: 'amber'  },
      { label: 'Formative',  value: this.allData.filter(a => a.assessmentType === 'Formative').length,  icon: 'quiz', iconColor: 'blue' },
    ];

    if (this.isSuperAdmin) {
      const uniqueSchools = new Set(this.allData.map(a => a.schoolId)).size;
      cards.push({ label: 'Schools', value: uniqueSchools, icon: 'school', iconColor: 'blue' });
    }

    return cards;
  }

  // ── Table columns ────────────────────────────────────────────────────────
  get tableColumns(): TableColumn<AssessmentDto>[] {
    const cols: TableColumn<AssessmentDto>[] = [
      { id: 'title',        label: 'Assessment',   align: 'left', sortable: true },
      { id: 'type',         label: 'Type',         align: 'left' },
      { id: 'classSubject', label: 'Class / Subject', align: 'left', hideOnMobile: true },
      { id: 'teacher',      label: 'Teacher',      align: 'left', hideOnMobile: true },
    ];

    if (this.isSuperAdmin) {
      cols.push({ id: 'school', label: 'School', align: 'left', hideOnMobile: true });
    }

    cols.push(
      { id: 'dateScore',  label: 'Date / Max Score', align: 'left', hideOnTablet: true },
      { id: 'published',  label: 'Status',           align: 'center' },
    );

    return cols;
  }

  // ── Table actions ─────────────────────────────────────────────────────────
  tableActions: TableAction<AssessmentDto>[] = [
    {
      id: 'edit',
      label: 'Edit',
      icon: 'edit',
      color: 'blue',
      handler: (a) => this.openEdit(a),
    },
    {
      id: 'publish',
      label: 'Publish',
      icon: 'publish',
      color: 'green',
      handler: (a) => this.togglePublish(a),
      visible: (a) => !a.isPublished,
    },
    {
      id: 'unpublish',
      label: 'Unpublish',
      icon: 'unpublished',
      color: 'amber',
      handler: (a) => this.togglePublish(a),
      visible: (a) => a.isPublished,
      divider: true,
    },
    {
      id: 'delete',
      label: 'Delete',
      icon: 'delete',
      color: 'red',
      handler: (a) => this.removeAssessment(a),
    },
  ];

  tableHeader: TableHeader = {
    title:         'Assessments List',
    subtitle:      '',
    icon:          'table_chart',
    iconGradient:  'bg-gradient-to-br from-indigo-500 via-violet-600 to-purple-700',
  };

  tableEmptyState: TableEmptyState = {
    icon:        'assignment_late',
    message:     'No assessments found',
    description: 'Adjust your filters or create a new assessment.',
    action: {
      label:   'Add',
      icon:    'add_circle',
      handler: () => this.openCreate(),
    },
  };

  // ── Filtered data ─────────────────────────────────────────────────────────
  get filteredData(): AssessmentDto[] {
    const q = this._filters.search.toLowerCase();
    return this.allData.filter(a =>
      (!q || a.title?.toLowerCase().includes(q) || a.teacherName?.toLowerCase().includes(q)
              || a.className?.toLowerCase().includes(q) || a.subjectName?.toLowerCase().includes(q)) &&
      (this._filters.assessmentType === 'all' || a.assessmentType === this._filters.assessmentType) &&
      (this._filters.published === 'all'
        || (this._filters.published === 'published' && a.isPublished)
        || (this._filters.published === 'draft'     && !a.isPublished)) &&
      (this._filters.schoolId === 'all' || a.schoolId === this._filters.schoolId)
    );
  }

  get paginatedData(): AssessmentDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit(): void {
    if (this.isSuperAdmin) {
      this._loadSchoolsThenAssessments();
    } else {
      this._buildFilterFields();
      this.loadAll();
    }
  }

  ngAfterViewInit(): void {
    this.cellTemplates = {
      title:        this.titleCellTemplate,
      type:         this.typeCellTemplate,
      classSubject: this.classSubjectCellTemplate,
      teacher:      this.teacherCellTemplate,
      school:       this.schoolCellTemplate,
      dateScore:    this.dateScoreCellTemplate,
      published:    this.publishedCellTemplate,
    };
  }

  ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }

  // ── Data loading ──────────────────────────────────────────────────────────
  private _loadSchoolsThenAssessments(): void {
    this._schoolSvc.getAll().pipe(takeUntil(this._unsubscribe)).subscribe({
      next: res => {
        this.schools = res?.data ?? [];
        this._buildFilterFields();
        this.loadAll();
      },
      error: () => {
        this._buildFilterFields();
        this.loadAll();
      },
    });
  }

  loadAll(schoolId?: string | null): void {
    this.isLoading = true;
    this._service.getAll(schoolId || undefined)
      .pipe(takeUntil(this._unsubscribe), finalize(() => (this.isLoading = false)))
      .subscribe({
        next: res => {
          if (res.success) {
            this.allData = res.data ?? [];
            this.tableHeader.subtitle = `${this.filteredData.length} assessments found`;
          }
        },
        error: err => this._alert.error(err?.error?.message || 'Failed to load assessments'),
      });
  }

  // ── Filter fields ─────────────────────────────────────────────────────────
  private _buildFilterFields(): void {
    this.filterFields = [
      {
        id: 'search', label: 'Search', type: 'text',
        placeholder: 'Title, teacher, class…', value: this._filters.search,
      },
    ];

    if (this.isSuperAdmin) {
      this.filterFields.push({
        id: 'schoolId', label: 'School', type: 'select',
        value: this._filters.schoolId,
        options: [
          { label: 'All Schools', value: 'all' },
          ...this.schools.map(s => ({ label: s.name, value: s.id })),
        ],
      });
    }

    this.filterFields.push(
      {
        id: 'assessmentType', label: 'Type', type: 'select',
        value: this._filters.assessmentType,
        options: [
          { label: 'All Types',   value: 'all'        },
          { label: 'Formative',   value: 'Formative'  },
          { label: 'Summative',   value: 'Summative'  },
          { label: 'Competency',  value: 'Competency' },
        ],
      },
      {
        id: 'published', label: 'Status', type: 'select',
        value: this._filters.published,
        options: [
          { label: 'All Statuses', value: 'all'       },
          { label: 'Published',    value: 'published'  },
          { label: 'Draft',        value: 'draft'      },
        ],
      }
    );
  }

  // ── Filter handlers ───────────────────────────────────────────────────────
  toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

  onFilterChange(event: FilterChangeEvent): void {
    (this._filters as any)[event.filterId] = event.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} assessments found`;

    if (event.filterId === 'schoolId' && this.isSuperAdmin) {
      this.loadAll(event.value === 'all' ? null : event.value);
    }
  }

  onClearFilters(): void {
    this._filters = { search: '', assessmentType: 'all', published: 'all', schoolId: 'all' };
    this.filterFields.forEach(f => (f.value = (this._filters as any)[f.id]));
    this.currentPage = 1;
    this.loadAll();
  }

  // ── Pagination ────────────────────────────────────────────────────────────
  onPageChange(page: number): void        { this.currentPage = page; }
  onItemsPerPageChange(n: number): void   { this.itemsPerPage = n; this.currentPage = 1; }

  // ── CRUD actions ──────────────────────────────────────────────────────────
  openCreate(): void {
    this._openDialog({ mode: 'create', schools: this.schools });
  }

  openEdit(assessment: AssessmentDto): void {
    this._openDialog({ mode: 'edit', assessment, schools: this.schools });
  }

  private _openDialog(data: CreateEditAssessmentDialogData): void {
    const ref = this._dialog.open(CreateEditAssessmentDialogComponent, {
      width: '860px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: true, autoFocus: 'input', data,
    });

    ref.afterClosed().pipe(takeUntil(this._unsubscribe)).subscribe(result => {
      if (result?.success) {
        this._alert.success(result.message || 'Saved successfully');
        this.loadAll();
      }
    });
  }

  togglePublish(assessment: AssessmentDto): void {
    const action   = assessment.isPublished ? 'unpublish' : 'publish';
    const newState = !assessment.isPublished;

    this._alert.confirm({
      title:       `${newState ? 'Publish' : 'Unpublish'} Assessment`,
      message:     `Are you sure you want to ${action} "${assessment.title}"?`,
      confirmText: newState ? 'Publish' : 'Unpublish',
      cancelText:  'Cancel',
      onConfirm: () => {
        this._service.updatePublishStatus(assessment.id, { isPublished: newState })
          .pipe(takeUntil(this._unsubscribe))
          .subscribe({
            next: res => {
              if (res.success) {
                this._alert.success(`Assessment ${action}ed successfully`);
                this.loadAll();
              }
            },
            error: err => this._alert.error(err?.error?.message || `Failed to ${action} assessment`),
          });
      },
      onCancel: () => {},
    });
  }

  removeAssessment(assessment: AssessmentDto): void {
    const ref = this._confirmation.open({
      title:   'Delete Assessment',
      message: `Delete "${assessment.title}"? This cannot be undone.`,
      icon:    { name: 'delete', color: 'warn' },
      actions: {
        confirm: { label: 'Delete', color: 'warn' },
        cancel:  { label: 'Cancel' },
      },
    });

    ref.afterClosed().pipe(takeUntil(this._unsubscribe)).subscribe(result => {
      if (result !== 'confirmed') return;
      this._service.delete(assessment.id)
        .pipe(takeUntil(this._unsubscribe))
        .subscribe({
          next: res => {
            if (res.success) {
              this._alert.success('Assessment deleted successfully');
              if (this.paginatedData.length === 1 && this.currentPage > 1) this.currentPage--;
              this.loadAll();
            }
          },
          error: err => this._alert.error(err?.error?.message || 'Failed to delete assessment'),
        });
    });
  }
}