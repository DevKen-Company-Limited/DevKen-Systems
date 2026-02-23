// list/subjects.component.ts
import {
  Component, ElementRef, OnInit, OnDestroy,
  ViewChild, TemplateRef, inject,
} from '@angular/core';
import { CommonModule }           from '@angular/common';
import { FormsModule }            from '@angular/forms';
import { Router }                 from '@angular/router';
import { MatIconModule }          from '@angular/material/icon';
import { MatButtonModule }        from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatMenuModule }          from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule }       from '@angular/material/divider';
import { MatTooltipModule }       from '@angular/material/tooltip';
import { Observable, of, Subject, forkJoin } from 'rxjs';
import { catchError, takeUntil, finalize } from 'rxjs/operators';

import { AuthService }      from 'app/core/auth/auth.service';
import { SchoolService }    from 'app/core/DevKenService/Tenant/SchoolService';
import { SchoolDto }        from 'app/Tenant/types/school';
import { AlertService }     from 'app/core/DevKenService/Alert/AlertService';

import { PageHeaderComponent, Breadcrumb }            from 'app/shared/Page-Header/page-header.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PaginationComponent }                        from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard }              from 'app/shared/stats-cards/stats-cards.component';
import {
  DataTableComponent, TableColumn, TableAction,
  TableHeader, TableEmptyState,
} from 'app/shared/data-table/data-table.component';


import {
  CBCLevelOptions, SubjectTypeOptions,
  getCBCLevelLabel, getSubjectTypeLabel,
} from '../Types/SubjectEnums';
import { SubjectReportService } from 'app/core/DevKenService/SubjectService/SubjectReportService';
import { SubjectService } from 'app/core/DevKenService/SubjectService/SubjectService';
import { SubjectDto } from '../Types/subjectdto';

@Component({
  selector: 'app-subjects',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatButtonModule, MatDialogModule, MatSnackBarModule,
    MatMenuModule, MatProgressSpinnerModule, MatDividerModule, MatTooltipModule,
    PageHeaderComponent, FilterPanelComponent, PaginationComponent,
    StatsCardsComponent, DataTableComponent,
  ],
  templateUrl: './subjects.component.html',
})
export class SubjectsComponent implements OnInit, OnDestroy {

  @ViewChild('nameCell')       nameCellTemplate!:    TemplateRef<any>;
  @ViewChild('codeCell')       codeCellTemplate!:    TemplateRef<any>;
  @ViewChild('typeCell')       typeCellTemplate!:    TemplateRef<any>;
  @ViewChild('levelCell')      levelCellTemplate!:   TemplateRef<any>;
  @ViewChild('schoolCell')     schoolCellTemplate!:  TemplateRef<any>;
  @ViewChild('statusCell')     statusCellTemplate!:  TemplateRef<any>;

  isDownloadingReport = false;

  private _unsubscribe   = new Subject<void>();
  private _router        = inject(Router);
  private _authService   = inject(AuthService);
  private _schoolService = inject(SchoolService);
  private _alertService  = inject(AlertService);
  private _reportService = inject(SubjectReportService);

  // ─── Breadcrumbs ─────────────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Academic',  url: '/academic'  },
    { label: 'Subjects' },
  ];

  // ─── Auth ─────────────────────────────────────────────────────────────────
  get isSuperAdmin(): boolean {
    return this._authService.authUser?.isSuperAdmin ?? false;
  }

  // ─── Schools ──────────────────────────────────────────────────────────────
  schools: SchoolDto[] = [];

  get schoolsCount(): number {
    return new Set(this.allData.map(s => s.schoolId)).size;
  }

  // ─── Stats ────────────────────────────────────────────────────────────────
  get statsCards(): StatCard[] {
    const cards: StatCard[] = [
      { label: 'Total Subjects',    value: this.total,          icon: 'menu_book',    iconColor: 'indigo' },
      { label: 'Active',            value: this.activeCount,    icon: 'check_circle', iconColor: 'green'  },
      { label: 'Core Subjects',     value: this.coreCount,      icon: 'star',         iconColor: 'blue'   },
      { label: 'Optional Subjects', value: this.optionalCount,  icon: 'tune',         iconColor: 'violet' },
    ];
    if (this.isSuperAdmin) {
      cards.push({ label: 'Schools', value: this.schoolsCount, icon: 'school', iconColor: 'green' });
    }
    return cards;
  }

  // ─── Table Columns ────────────────────────────────────────────────────────
  get tableColumns(): TableColumn<SubjectDto>[] {
    const cols: TableColumn<SubjectDto>[] = [
      { id: 'name',  label: 'Subject',     align: 'left', sortable: true },
      { id: 'code',  label: 'Code',        align: 'left' },
    ];
    if (this.isSuperAdmin) {
      cols.push({ id: 'school', label: 'School', align: 'left', hideOnMobile: true });
    }
    cols.push(
      { id: 'type',   label: 'Type',      align: 'left',   hideOnMobile: true  },
      { id: 'level',  label: 'CBC Level', align: 'left',   hideOnTablet: true  },
      { id: 'status', label: 'Status',    align: 'center' },
    );
    return cols;
  }

  // ─── Table Actions ────────────────────────────────────────────────────────
  tableActions: TableAction<SubjectDto>[] = [
    { id: 'view',   label: 'View Details', icon: 'visibility',   color: 'blue',   handler: s => this.viewSubject(s)   },
    { id: 'edit',   label: 'Edit',         icon: 'edit',         color: 'indigo', handler: s => this.editSubject(s)   },
    {
      id: 'deactivate', label: 'Deactivate', icon: 'block', color: 'amber',
      handler: s => this.toggleActive(s),
      visible: s => s.isActive,
    },
    {
      id: 'activate', label: 'Activate', icon: 'check_circle', color: 'green', divider: true,
      handler: s => this.toggleActive(s),
      visible: s => !s.isActive,
    },
    { id: 'delete', label: 'Delete', icon: 'delete', color: 'red', handler: s => this.removeSubject(s) },
  ];

  tableHeader: TableHeader = {
    title:        'Subjects List',
    subtitle:     '',
    icon:         'menu_book',
    iconGradient: 'bg-gradient-to-br from-indigo-500 via-violet-600 to-purple-700',
  };

  tableEmptyState: TableEmptyState = {
    icon:        'auto_stories',
    message:     'No subjects found',
    description: 'Try adjusting your filters or create a new subject',
    action: { label: 'Create First Subject', icon: 'add', handler: () => this.createSubject() },
  };

  // ─── State ────────────────────────────────────────────────────────────────
  cellTemplates: { [key: string]: TemplateRef<any> } = {};
  filterFields:  FilterField[] = [];
  showFilterPanel = false;
  allData:   SubjectDto[] = [];
  isLoading  = false;

  private _filterValues = {
    search: '', status: 'all', subjectType: 'all', cbcLevel: 'all', schoolId: 'all',
  };

  currentPage  = 1;
  itemsPerPage = 10;

  // ─── Computed ─────────────────────────────────────────────────────────────
  get total():         number { return this.allData.length; }
  get activeCount():   number { return this.allData.filter(s => s.isActive).length; }
  get coreCount():     number { return this.allData.filter(s => Number(s.subjectType) === 1).length; }
  get optionalCount(): number { return this.allData.filter(s => Number(s.subjectType) === 2).length; }

  get filteredData(): SubjectDto[] {
    return this.allData.filter(s => {
      const q         = this._filterValues.search.toLowerCase();
      const typeName  = getSubjectTypeLabel(s.subjectType);
<<<<<<< HEAD
      const levelName = getCBCLevelLabel(s.level);
=======
      const levelName = getCBCLevelLabel(s.cbcLevel);
>>>>>>> upstream/main

      return (
        (!q || s.name?.toLowerCase().includes(q) ||
               s.code?.toLowerCase().includes(q) ||
               s.description?.toLowerCase().includes(q)) &&
        (this._filterValues.status === 'all' ||
          (this._filterValues.status === 'active'   &&  s.isActive) ||
          (this._filterValues.status === 'inactive' && !s.isActive)) &&
        (this._filterValues.subjectType === 'all' || typeName  === this._filterValues.subjectType) &&
        (this._filterValues.cbcLevel    === 'all' || levelName === this._filterValues.cbcLevel) &&
        (this._filterValues.schoolId    === 'all' || s.schoolId === this._filterValues.schoolId)
      );
    });
  }

  get paginatedData(): SubjectDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  // ─── Helpers ─────────────────────────────────────────────────────────────
  getSubjectTypeName  = getSubjectTypeLabel;
  getCBCLevelName     = getCBCLevelLabel;

  constructor(
    private _service: SubjectService,
    private _dialog:  MatDialog,
  ) {}

  // ─── Lifecycle ────────────────────────────────────────────────────────────
  ngOnInit(): void { this._loadInit(); }

  ngAfterViewInit(): void {
    this.cellTemplates = {
      name:   this.nameCellTemplate,
      code:   this.codeCellTemplate,
      school: this.schoolCellTemplate,
      type:   this.typeCellTemplate,
      level:  this.levelCellTemplate,
      status: this.statusCellTemplate,
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
        placeholder: 'Name, code or description...', value: this._filterValues.search,
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
        id: 'status', label: 'Status', type: 'select', value: this._filterValues.status,
        options: [
          { label: 'All Statuses', value: 'all' },
          { label: 'Active',       value: 'active'   },
          { label: 'Inactive',     value: 'inactive' },
        ],
      },
      {
        id: 'subjectType', label: 'Subject Type', type: 'select', value: this._filterValues.subjectType,
        options: [
          { label: 'All Types', value: 'all' },
          ...SubjectTypeOptions.map(o => ({ label: o.label, value: o.label })),
        ],
      },
      {
        id: 'cbcLevel', label: 'CBC Level', type: 'select', value: this._filterValues.cbcLevel,
        options: [
          { label: 'All Levels', value: 'all' },
          ...CBCLevelOptions.map(o => ({ label: o.label, value: o.label })),
        ],
      },
    );
  }

  // ─── Filter events ────────────────────────────────────────────────────────
  toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

  onFilterChange(event: FilterChangeEvent): void {
    (this._filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} subjects found`;
    if (event.filterId === 'schoolId' && this.isSuperAdmin) {
      this.loadAll(event.value === 'all' ? null : event.value);
    }
  }

  onClearFilters(): void {
    this._filterValues = { search: '', status: 'all', subjectType: 'all', cbcLevel: 'all', schoolId: 'all' };
    this.filterFields.forEach(f => { f.value = (this._filterValues as any)[f.id]; });
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} subjects found`;
    this.loadAll();
  }

  onPageChange(page: number):      void { this.currentPage  = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

  // ─── Data ─────────────────────────────────────────────────────────────────
  loadAll(schoolId?: string | null): void {
    this.isLoading = true;
    this._service.getAll(schoolId ?? undefined)
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next: res => {
          this.allData = Array.isArray(res) ? res : [];
          this.tableHeader.subtitle = `${this.filteredData.length} subjects found`;
          this.isLoading = false;
        },
        error: err => {
          this.isLoading = false;
          this._alertService.error(err.error?.message || 'Failed to load subjects');
        },
      });
  }

  // ─── Actions ─────────────────────────────────────────────────────────────
  createSubject(): void { this._router.navigate(['/academic/subjects/create']); }
  viewSubject(s: SubjectDto): void { this._router.navigate(['/academic/subjects/details', s.id]); }
  editSubject(s: SubjectDto): void { this._router.navigate(['/academic/subjects/edit', s.id]); }

  toggleActive(subject: SubjectDto): void {
    const newStatus = !subject.isActive;
    const action    = newStatus ? 'activate' : 'deactivate';

    this._alertService.confirm({
      title:       `${newStatus ? 'Activate' : 'Deactivate'} Subject`,
      message:     `Are you sure you want to ${action} "${subject.name}"?`,
      confirmText: newStatus ? 'Activate' : 'Deactivate',
      onConfirm:   () => {
        this._service.toggleActive(subject.id, newStatus).subscribe({
          next:  res => { this._alertService.success(res.message); this.loadAll(); },
          error: err => this._alertService.error(err.error?.message || `Failed to ${action} subject`),
        });
      },
    });
  }

  removeSubject(subject: SubjectDto): void {
    this._alertService.confirm({
      title:       'Delete Subject',
      confirmText: 'Delete',
      cancelText:  'Cancel',
      message:     `Are you sure you want to delete "${subject.name}" (${subject.code})? This action cannot be undone.`,
      onConfirm:   () => {
        this._service.delete(subject.id).subscribe({
          next:  () => {
            this._alertService.success('Subject deleted successfully');
            if (this.paginatedData.length === 0 && this.currentPage > 1) this.currentPage--;
            this.loadAll();
          },
          error: err => this._alertService.error(err.error?.message || 'Failed to delete subject'),
        });
      },
    });
  }

  downloadSubjectsReport(): void {
    if (this.isDownloadingReport) return;
    this.isDownloadingReport = true;

    const schoolId =
      this.isSuperAdmin && this._filterValues.schoolId !== 'all'
        ? this._filterValues.schoolId : null;

    this._alertService.info('Generating PDF report…');

    this._reportService.downloadSubjectsList(schoolId)
      .pipe(takeUntil(this._unsubscribe))
      .subscribe({
        next:  () => { this._alertService.success('Subjects report downloaded'); this.isDownloadingReport = false; },
        error: (err: Error) => { this._alertService.error(err.message || 'Failed to generate report'); this.isDownloadingReport = false; },
      });
  }
}