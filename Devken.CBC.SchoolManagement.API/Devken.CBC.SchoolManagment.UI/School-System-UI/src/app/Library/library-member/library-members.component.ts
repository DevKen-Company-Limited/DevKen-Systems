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
import { DataTableComponent, TableColumn, TableAction, TableHeader, TableEmptyState } from 'app/shared/data-table/data-table.component';
import { BaseListComponent } from 'app/shared/Lists/BaseListComponent';
import { LibraryMemberDto, LibraryMemberType } from './Types/library-member.types';
import { LibraryMemberService } from 'app/core/DevKenService/Library/library-member.service';
import { CreateEditMemberDialogComponent } from 'app/dialog-modals/Library/library-member/create-edit-member-dialog.component';


@Component({
  selector: 'app-library-members',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatButtonModule, MatDialogModule, MatProgressSpinnerModule,
    PageHeaderComponent, FilterPanelComponent, PaginationComponent,
    StatsCardsComponent, DataTableComponent,
  ],
  templateUrl: './library-members.component.html',
})
export class LibraryMembersComponent
  extends BaseListComponent<LibraryMemberDto>
  implements OnInit, OnDestroy, AfterViewInit {

  private _destroy$ = new Subject<void>();

  private _alertService!: AlertService;
  private _memberService!: LibraryMemberService;
  private _schoolService!: SchoolService;

  @ViewChild('memberCell',  { static: true }) memberCell!:  TemplateRef<any>;
  @ViewChild('typeCell',    { static: true }) typeCell!:    TemplateRef<any>;
  @ViewChild('joinedCell',  { static: true }) joinedCell!:  TemplateRef<any>;
  @ViewChild('statusCell',  { static: true }) statusCell!:  TemplateRef<any>;
  @ViewChild('borrowsCell', { static: true }) borrowsCell!: TemplateRef<any>;
  @ViewChild('schoolCell',  { static: true }) schoolCell!:  TemplateRef<any>;

  cellTemplates!: Record<string, TemplateRef<any>>;

  ngAfterViewInit(): void {
    this.cellTemplates = {
      member:  this.memberCell,
      type:    this.typeCell,
      joined:  this.joinedCell,
      status:  this.statusCell,
      borrows: this.borrowsCell,
      school:  this.schoolCell,
    };
  }

  // ── State ──────────────────────────────────────────────────────────────────
  schools: SchoolDto[] = [];
  isDataLoading   = true;
  showFilterPanel = false;
  currentPage     = 1;
  itemsPerPage    = 10;

  filterValues = {
    search: '', memberType: 'all', statusFilter: 'all', schoolId: 'all',
  };

  readonly memberTypeOptions: { label: string; value: LibraryMemberType | 'all'; icon: string }[] = [
    { label: 'All Types', value: 'all',     icon: 'people'        },
    { label: 'Student',   value: 'Student', icon: 'school'        },
    { label: 'Teacher',   value: 'Teacher', icon: 'person'        },
    { label: 'Staff',     value: 'Staff',   icon: 'badge'         },
    { label: 'Other',     value: 'Other',   icon: 'person_outline'},
  ];

  // ── Breadcrumbs ────────────────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard', url: '/dashboard' },
    { label: 'Library',   url: '/library'   },
    { label: 'Members' },
  ];

  // ── Table config ───────────────────────────────────────────────────────────
  get isSuperAdmin(): boolean { return this._injectedAuth.authUser?.isSuperAdmin ?? false; }

  get tableColumns(): TableColumn<LibraryMemberDto>[] {
    const cols: TableColumn<LibraryMemberDto>[] = [
      { id: 'member',  label: 'Member',     align: 'left', sortable: true },
      { id: 'type',    label: 'Type',       align: 'left', hideOnMobile: true },
      { id: 'joined',  label: 'Joined On',  align: 'left', hideOnTablet: true },
      { id: 'borrows', label: 'Borrows',    align: 'center', hideOnMobile: true },
      { id: 'status',  label: 'Status',     align: 'center' },
    ];
    if (this.isSuperAdmin) {
      cols.push({ id: 'school', label: 'School', align: 'left', hideOnMobile: true });
    }
    return cols;
  }

  tableActions: TableAction<LibraryMemberDto>[] = [
    {
      id: 'edit', label: 'Edit', icon: 'edit', color: 'blue',
      handler: m => this._openEdit(m),
    },
    {
      id: 'delete', label: 'Delete', icon: 'delete', color: 'red',
      handler: m => this._confirmDelete(m),
      divider: true,
    },
  ];

  tableHeader: TableHeader = {
    title:        'Library Members',
    subtitle:     '',
    icon:         'table_chart',
    iconGradient: 'bg-gradient-to-br from-violet-500 via-purple-600 to-indigo-700',
  };

  tableEmptyState: TableEmptyState = {
    icon:        'person_add',
    message:     'No library members found',
    description: 'Try adjusting your filters or register a new member',
    action: { label: 'Add First Member', icon: 'add', handler: () => this._openCreate() },
  };

  filterFields: FilterField[] = [];

  // ── Stats ──────────────────────────────────────────────────────────────────
  get statsCards(): StatCard[] {
    const data = this.dataSource.data;
    const active   = data.filter(m =>  m.isActive).length;
    const inactive = data.filter(m => !m.isActive).length;
    const students = data.filter(m =>  m.memberType === 'Student').length;
    const teachers = data.filter(m =>  m.memberType === 'Teacher').length;

    const base: StatCard[] = [
      { label: 'Total Members', value: data.length, icon: 'people',       iconColor: 'indigo' },
      { label: 'Active',        value: active,       icon: 'check_circle', iconColor: 'green'  },
      { label: 'Students',      value: students,     icon: 'school',       iconColor: 'blue'   },
      { label: 'Teachers',      value: teachers,     icon: 'person',       iconColor: 'amber'  },
    ];
    if (this.isSuperAdmin) {
      base.push({ label: 'Schools', value: new Set(data.map(m => m.schoolId)).size, icon: 'school', iconColor: 'violet' });
    }
    return base;
  }

  // ── Filtered & paginated ───────────────────────────────────────────────────
  get filteredData(): LibraryMemberDto[] {
    const q = this.filterValues.search.toLowerCase();
    return this.dataSource.data.filter(m =>
      (!q ||
        m.memberNumber?.toLowerCase().includes(q) ||
        m.userFullName?.toLowerCase().includes(q) ||
        m.userEmail?.toLowerCase().includes(q)
      ) &&
      (this.filterValues.memberType   === 'all' || m.memberType === this.filterValues.memberType) &&
      (this.filterValues.statusFilter === 'all' ||
        (this.filterValues.statusFilter === 'active'   &&  m.isActive) ||
        (this.filterValues.statusFilter === 'inactive' && !m.isActive)
      ) &&
      (this.filterValues.schoolId === 'all' || m.schoolId === this.filterValues.schoolId)
    );
  }

  get paginatedData(): LibraryMemberDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  constructor(
    dialog: MatDialog,
    snackBar: MatSnackBar,
    memberService: LibraryMemberService,
    private readonly _injectedAuth: AuthService,
    alertService: AlertService,
    schoolService: SchoolService,
  ) {
    super(memberService, dialog, snackBar);
    this._alertService  = alertService;
    this._memberService = memberService;
    this._schoolService = schoolService;
  }

  // ── Lifecycle ──────────────────────────────────────────────────────────────
  ngOnInit(): void    { this._loadMeta(); }
  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  // ── Data loading ───────────────────────────────────────────────────────────
  protected override loadAll(): void {
    this.isLoading = true;
    const schoolId = this.filterValues.schoolId !== 'all' ? this.filterValues.schoolId : undefined;
    this._memberService.getAll(schoolId)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: res => {
          if (res.success) {
            this.dataSource.data = res.data;
            this.tableHeader.subtitle = `${this.filteredData.length} members found`;
          } else {
            this._alertService.error(res.message || 'Failed to load members');
          }
          this.isLoading = false;
        },
        error: err => {
          this._alertService.error(err.error?.message || 'Failed to load members');
          this.isLoading = false;
        },
      });
  }

  private _loadMeta(): void {
    this.isDataLoading = true;

    if (!this.isSuperAdmin) {
      this._initFilters();
      this.loadAll();
      this.isDataLoading = false;
      return;
    }

    this._schoolService.getAll().pipe(
      catchError(() => of({ success: false, data: [] })),
      takeUntil(this._destroy$),
      finalize(() => { this.isDataLoading = false; })
    ).subscribe({
      next: (res: any) => {
        this.schools = res?.data || [];
        this._initFilters();
        this.loadAll();
      },
      error: () => { this._initFilters(); this.loadAll(); },
    });
  }

  // ── Filters ────────────────────────────────────────────────────────────────
  private _initFilters(): void {
    this.filterFields = [
      {
        id: 'search', label: 'Search', type: 'text',
        placeholder: 'Member number, name or email...', value: '',
      },
      {
        id: 'memberType', label: 'Member Type', type: 'select', value: 'all',
        options: [
          { label: 'All Types', value: 'all'     },
          { label: 'Student',   value: 'Student' },
          { label: 'Teacher',   value: 'Teacher' },
          { label: 'Staff',     value: 'Staff'   },
          { label: 'Other',     value: 'Other'   },
        ],
      },
      {
        id: 'statusFilter', label: 'Status', type: 'select', value: 'all',
        options: [
          { label: 'All Statuses', value: 'all'      },
          { label: 'Active',       value: 'active'   },
          { label: 'Inactive',     value: 'inactive' },
        ],
      },
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
  }

  toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

  onFilterChange(event: FilterChangeEvent): void {
    (this.filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} members found`;
    if (event.filterId === 'schoolId' && this.isSuperAdmin) {
      this.loadAll();
    }
  }

  onClearFilters(): void {
    this.filterValues = { search: '', memberType: 'all', statusFilter: 'all', schoolId: 'all' };
    this.filterFields.forEach(f => { f.value = (this.filterValues as any)[f.id]; });
    this.currentPage = 1;
    this.loadAll();
  }

  onPageChange(page: number): void      { this.currentPage = page; }
  onItemsPerPageChange(n: number): void  { this.itemsPerPage = n; this.currentPage = 1; }

  // ── CRUD ──────────────────────────────────────────────────────────────────
  private _openCreate(): void {
    this.openDialog(CreateEditMemberDialogComponent, {
      panelClass: ['member-dialog', 'no-padding-dialog'],
      width: '680px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: true, autoFocus: 'mat-select',
      data: { mode: 'create' },
    });
  }

  private _openEdit(member: LibraryMemberDto): void {
    this.openDialog(CreateEditMemberDialogComponent, {
      panelClass: ['member-dialog', 'no-padding-dialog'],
      width: '680px', maxWidth: '95vw', maxHeight: '95vh',
      disableClose: true, autoFocus: 'input',
      data: { mode: 'edit', member },
    });
  }

  openCreate(): void { this._openCreate(); }

  private _confirmDelete(member: LibraryMemberDto): void {
    this._alertService.confirm({
      title:       'Delete Library Member',
      message:     `Delete member "${member.memberNumber}" (${member.userFullName || member.userEmail})? Members with borrow history cannot be deleted — deactivate them instead.`,
      confirmText: 'Delete',
      onConfirm:   () => this._doDelete(member),
    });
  }

  private _doDelete(member: LibraryMemberDto): void {
    this.isLoading = true;
    this._memberService.delete(member.id)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: res => {
          if (res.success) {
            this._alertService.success('Member deleted successfully');
            if (this.paginatedData.length === 1 && this.currentPage > 1) this.currentPage--;
            this.loadAll();
          } else {
            this._alertService.error(res.message || 'Failed to delete member');
          }
          this.isLoading = false;
        },
        error: err => {
          this._alertService.error(err.error?.message || 'Failed to delete member');
          this.isLoading = false;
        },
      });
  }

  // ── Display helpers ───────────────────────────────────────────────────────
  getMemberTypeColor(type: LibraryMemberType): string {
    const map: Record<LibraryMemberType, string> = {
      Student: 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300',
      Teacher: 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-300',
      Staff:   'bg-teal-100 dark:bg-teal-900/30 text-teal-700 dark:text-teal-300',
      Other:   'bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300',
    };
    return map[type] ?? map['Other'];
  }

  getMemberTypeIcon(type: LibraryMemberType): string {
    const map: Record<LibraryMemberType, string> = {
      Student: 'school', Teacher: 'person', Staff: 'badge', Other: 'person_outline',
    };
    return map[type] ?? 'person_outline';
  }
}