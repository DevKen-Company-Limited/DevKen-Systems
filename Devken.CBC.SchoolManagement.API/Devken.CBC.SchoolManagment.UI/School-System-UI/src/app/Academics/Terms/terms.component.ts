import { Component, OnInit, OnDestroy, AfterViewInit, ViewChild, TemplateRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Subject, forkJoin, of } from 'rxjs';
import { catchError, takeUntil, finalize } from 'rxjs/operators';

import { AuthService } from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { AcademicYearService } from 'app/core/DevKenService/AcademicYearService/AcademicYearService';
import { TermService } from 'app/core/DevKenService/TermService/term.service';
import { SchoolDto } from 'app/Tenant/types/school';
import { AcademicYearDto } from '../AcademicYear/Types/AcademicYear';
import { TermDto, CreateTermRequest, UpdateTermRequest, CloseTermRequest } from './Types/types';
import { CreateEditTermDialogComponent, CreateEditTermDialogResult } from 'app/dialog-modals/Terms/create-edit-term-dialog.component';

import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';
import { DataTableComponent, TableColumn, TableAction, TableHeader, TableEmptyState } from 'app/shared/data-table/data-table.component';

@Component({
  selector: 'app-terms',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatButtonModule, MatDialogModule, MatProgressSpinnerModule,
    PageHeaderComponent, FilterPanelComponent, PaginationComponent,
    StatsCardsComponent, DataTableComponent,
  ],
  templateUrl: './terms.component.html',
})
export class TermsComponent implements OnInit, OnDestroy, AfterViewInit {
  private _destroy$ = new Subject<void>();
  private _service = inject(TermService);
  private _authService = inject(AuthService);
  private _schoolService = inject(SchoolService);
  private _academicYearService = inject(AcademicYearService);
  private _alertService = inject(AlertService);
  private _dialog = inject(MatDialog);

  @ViewChild('termCell',        { static: true }) termCell!: TemplateRef<any>;
  @ViewChild('academicYearCell',{ static: true }) academicYearCell!: TemplateRef<any>;
  @ViewChild('schoolCell',      { static: true }) schoolCell!: TemplateRef<any>;
  @ViewChild('datesCell',       { static: true }) datesCell!: TemplateRef<any>;
  @ViewChild('statusCell',      { static: true }) statusCell!: TemplateRef<any>;

  cellTemplates!: Record<string, TemplateRef<any>>;

  ngAfterViewInit(): void {
    this.cellTemplates = {
      term:         this.termCell,
      academicYear: this.academicYearCell,
      school:       this.schoolCell,
      dates:        this.datesCell,
      status:       this.statusCell,
    };
  }

  get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }

  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Academic',  url: '/academic'  },
    { label: 'Terms' },
  ];

  allData: TermDto[] = [];
  schools: SchoolDto[] = [];
  academicYears: AcademicYearDto[] = [];
  isLoading = false;
  isDataLoading = true;
  showFilterPanel = false;

  currentPage = 1;
  itemsPerPage = 10;

  private _filterValues = { search: '', status: 'all', academicYearId: 'all', schoolId: 'all', termNumber: 'all' };

  // ── Stats ─────────────────────────────────────────────────────────────────────
  get statsCards(): StatCard[] {
    const base: StatCard[] = [
      { label: 'Total Terms', value: this.allData.length,                              icon: 'event_note',            iconColor: 'indigo' },
      { label: 'Current',     value: this.allData.filter(t => t.isCurrent).length,    icon: 'radio_button_checked',  iconColor: 'blue'   },
      { label: 'Active',      value: this.allData.filter(t => t.isActive).length,     icon: 'check_circle',          iconColor: 'green'  },
      { label: 'Closed',      value: this.allData.filter(t => t.isClosed).length,     icon: 'lock',                  iconColor: 'red'    },
    ];
    if (this.isSuperAdmin) {
      base.push({ label: 'Schools', value: new Set(this.allData.map(t => t.schoolId)).size, icon: 'school', iconColor: 'violet' });
    }
    return base;
  }

  // ── Table columns (dynamic for superadmin) ────────────────────────────────────
  get tableColumns(): TableColumn<TermDto>[] {
    const cols: TableColumn<TermDto>[] = [
      { id: 'term',         label: 'Term',          align: 'left', sortable: true },
      { id: 'academicYear', label: 'Academic Year', align: 'left', hideOnMobile: true },
    ];
    if (this.isSuperAdmin) {
      cols.push({ id: 'school', label: 'School', align: 'left', hideOnMobile: true });
    }
    cols.push(
      { id: 'dates',  label: 'Dates',  align: 'left',   hideOnTablet: true },
      { id: 'status', label: 'Status', align: 'center' },
    );
    return cols;
  }

  tableActions: TableAction<TermDto>[] = [
    {
      id: 'edit', label: 'Edit', icon: 'edit', color: 'blue',
      handler: r => this.openEdit(r),
    },
    {
      id: 'setCurrent', label: 'Set as Current', icon: 'radio_button_checked', color: 'indigo',
      handler: r => this.setAsCurrent(r),
      visible: r => !r.isCurrent && !r.isClosed,
    },
    {
      id: 'close', label: 'Close Term', icon: 'lock', color: 'amber',
      handler: r => this.closeTerm(r),
      visible: r => !r.isClosed,
      divider: true,
    },
    {
      id: 'reopen', label: 'Reopen Term', icon: 'lock_open', color: 'green',
      handler: r => this.reopenTerm(r),
      visible: r => r.isClosed,
      divider: true,
    },
    {
      id: 'delete', label: 'Delete', icon: 'delete', color: 'red',
      handler: r => this.removeTerm(r),
    },
  ];

  tableHeader: TableHeader = {
    title: 'Terms List', subtitle: '',
    icon: 'table_chart',
    iconGradient: 'bg-gradient-to-br from-blue-500 via-indigo-600 to-violet-700',
  };

  tableEmptyState: TableEmptyState = {
    icon: 'event_note', message: 'No terms found',
    description: 'Try adjusting your filters or add a new term',
    action: { label: 'Add First Term', icon: 'add', handler: () => this.openCreate() },
  };

  filterFields: FilterField[] = [];

  // ── Filtering & Pagination ────────────────────────────────────────────────────
  get filteredData(): TermDto[] {
    const q = this._filterValues.search.toLowerCase();
    return this.allData.filter(t =>
      (!q || t.name?.toLowerCase().includes(q) || t.academicYearName?.toLowerCase().includes(q)) &&
      (this._filterValues.status === 'all' ||
        (this._filterValues.status === 'current' && t.isCurrent) ||
        (this._filterValues.status === 'active'  && t.isActive && !t.isClosed) ||
        (this._filterValues.status === 'closed'  && t.isClosed)) &&
      (this._filterValues.academicYearId === 'all' || t.academicYearId === this._filterValues.academicYearId) &&
      (this._filterValues.schoolId === 'all'       || t.schoolId === this._filterValues.schoolId) &&
      (this._filterValues.termNumber === 'all'     || t.termNumber === Number(this._filterValues.termNumber))
    );
  }

  get paginatedData(): TermDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  ngOnInit(): void { this.loadDataAndInit(); }
  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  // ── Bootstrap ─────────────────────────────────────────────────────────────────
  private loadDataAndInit(): void {
    this.isDataLoading = true;

    const requests: any = {
      academicYears: this._academicYearService.getAll().pipe(
        catchError(() => of({ success: false, data: [] }))
      ),
    };
    if (this.isSuperAdmin) {
      requests.schools = this._schoolService.getAll().pipe(
        catchError(() => of({ success: false, data: [] }))
      );
    }

    forkJoin(requests).pipe(
      takeUntil(this._destroy$),
      finalize(() => { this.isDataLoading = false; })
    ).subscribe({
      next: (results: any) => {
        this.academicYears = results.academicYears.data || [];
        if (results.schools) this.schools = results.schools.data || [];
        this.initFilterFields();
        this.loadAll();
      },
      error: () => { this.initFilterFields(); this.loadAll(); },
    });
  }

  // ── Filter ────────────────────────────────────────────────────────────────────
  private initFilterFields(): void {
    this.filterFields = [
      { id: 'search', label: 'Search', type: 'text', placeholder: 'Term name or academic year...', value: '' },
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

    this.filterFields.push(
      {
        id: 'status', label: 'Status', type: 'select', value: 'all',
        options: [
          { label: 'All Statuses', value: 'all'     },
          { label: 'Current',      value: 'current' },
          { label: 'Active',       value: 'active'  },
          { label: 'Closed',       value: 'closed'  },
        ],
      },
      {
        id: 'academicYearId', label: 'Academic Year', type: 'select', value: 'all',
        options: [
          { label: 'All Years', value: 'all' },
          ...this.academicYears.map(ay => ({ label: `${ay.name} (${ay.code})`, value: ay.id })),
        ],
      },
      {
        id: 'termNumber', label: 'Term Number', type: 'select', value: 'all',
        options: [
          { label: 'All Terms', value: 'all' },
          { label: 'Term 1',    value: '1'   },
          { label: 'Term 2',    value: '2'   },
          { label: 'Term 3',    value: '3'   },
        ],
      },
    );
  }

  toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

  onFilterChange(event: FilterChangeEvent): void {
    (this._filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} terms found`;
    if (event.filterId === 'schoolId' && this.isSuperAdmin) {
      this.loadAll(event.value === 'all' ? null : event.value);
    }
  }

  onClearFilters(): void {
    this._filterValues = { search: '', status: 'all', academicYearId: 'all', schoolId: 'all', termNumber: 'all' };
    this.filterFields.forEach(f => { f.value = (this._filterValues as any)[f.id]; });
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} terms found`;
    this.loadAll();
  }

  onPageChange(page: number): void { this.currentPage = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

  // ── Data ──────────────────────────────────────────────────────────────────────
  loadAll(schoolId?: string | null): void {
    this.isLoading = true;
    this._service.getAll(schoolId || undefined).pipe(takeUntil(this._destroy$)).subscribe({
      next: res => {
        if (res.success) {
          this.allData = res.data;
          this.tableHeader.subtitle = `${this.filteredData.length} terms found`;
        }
        this.isLoading = false;
      },
      error: err => {
        this._alertService.error(err.error?.message || 'Failed to load terms');
        this.isLoading = false;
      },
    });
  }

  // ── CRUD ──────────────────────────────────────────────────────────────────────
  openCreate(): void {
    const ref = this._dialog.open(CreateEditTermDialogComponent, {
      panelClass: ['term-dialog', 'no-padding-dialog'],
      width: '700px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: true, autoFocus: 'input',
      data: { mode: 'create' },
    });
    ref.afterClosed().pipe(takeUntil(this._destroy$)).subscribe((result: CreateEditTermDialogResult | null) => {
      if (!result) return;
      this._service.create(result.formData as CreateTermRequest).pipe(takeUntil(this._destroy$)).subscribe({
        next: res => { if (res.success) { this._alertService.success('Term created successfully'); this.loadAll(); } },
        error: err => this._alertService.error(err.error?.message || 'Failed to create term'),
      });
    });
  }

  openEdit(term: TermDto): void {
    const ref = this._dialog.open(CreateEditTermDialogComponent, {
      panelClass: ['term-dialog', 'no-padding-dialog'],
      width: '700px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: true, autoFocus: 'input',
      data: { mode: 'edit', term },
    });
    ref.afterClosed().pipe(takeUntil(this._destroy$)).subscribe((result: CreateEditTermDialogResult | null) => {
      if (!result) return;
      this._service.update(term.id, result.formData as UpdateTermRequest).pipe(takeUntil(this._destroy$)).subscribe({
        next: res => { if (res.success) { this._alertService.success('Term updated successfully'); this.loadAll(); } },
        error: err => this._alertService.error(err.error?.message || 'Failed to update term'),
      });
    });
  }

  setAsCurrent(term: TermDto): void {
    this._alertService.confirm({
      title: 'Set as Current Term',
      message: `Set "${term.name}" as the current term? This will unset any other current terms.`,
      confirmText: 'Set as Current',
      onConfirm: () => {
        this._service.setCurrent(term.id).pipe(takeUntil(this._destroy$)).subscribe({
          next: res => { if (res.success) { this._alertService.success('Term set as current successfully'); this.loadAll(); } },
          error: err => this._alertService.error(err.error?.message || 'Failed to set term as current'),
        });
      },
    });
  }

  closeTerm(term: TermDto): void {
    this._alertService.confirm({
      title: 'Close Term',
      message: `Close "${term.name}"? This will prevent further modifications.`,
      confirmText: 'Close Term',
      onConfirm: () => {
        const request: CloseTermRequest = { termId: term.id, remarks: 'Term closed by user' };
        this._service.close(term.id, request).pipe(takeUntil(this._destroy$)).subscribe({
          next: res => { if (res.success) { this._alertService.success('Term closed successfully'); this.loadAll(); } },
          error: err => this._alertService.error(err.error?.message || 'Failed to close term'),
        });
      },
    });
  }

  reopenTerm(term: TermDto): void {
    this._alertService.confirm({
      title: 'Reopen Term',
      message: `Reopen "${term.name}"? This will allow modifications again.`,
      confirmText: 'Reopen Term',
      onConfirm: () => {
        this._service.reopen(term.id).pipe(takeUntil(this._destroy$)).subscribe({
          next: res => { if (res.success) { this._alertService.success('Term reopened successfully'); this.loadAll(); } },
          error: err => this._alertService.error(err.error?.message || 'Failed to reopen term'),
        });
      },
    });
  }

  removeTerm(term: TermDto): void {
    this._alertService.confirm({
      title: 'Delete Term',
      message: `Delete "${term.name}"? This action cannot be undone.`,
      confirmText: 'Delete',
      onConfirm: () => {
        this._service.delete(term.id).pipe(takeUntil(this._destroy$)).subscribe({
          next: res => {
            if (res.success) {
              this._alertService.success('Term deleted successfully');
              if (this.paginatedData.length === 1 && this.currentPage > 1) this.currentPage--;
              this.loadAll();
            }
          },
          error: err => this._alertService.error(err.error?.message || 'Failed to delete term'),
        });
      },
    });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────────
  getStatusIcon(status: string): string {
    const map: Record<string, string> = {
      current: 'radio_button_checked', active: 'check_circle',
      closed: 'lock', upcoming: 'schedule', past: 'history',
    };
    return map[status?.toLowerCase()] ?? 'info';
  }

  formatDate(dateString: string): string {
    if (!dateString) return '—';
    return new Date(dateString).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }
}