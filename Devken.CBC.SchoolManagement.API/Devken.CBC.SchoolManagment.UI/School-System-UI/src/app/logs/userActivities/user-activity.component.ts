import {
  Component, OnInit, OnDestroy, AfterViewInit,
  ViewChild, TemplateRef, inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, forkJoin, takeUntil, finalize } from 'rxjs';

import { AlertService } from 'app/core/DevKenService/Alert/AlertService';


import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';
import {
  DataTableComponent, TableColumn, TableHeader, TableEmptyState
} from 'app/shared/data-table/data-table.component';
import { ActivitySummaryDto, UserActivityDto, UserActivityService } from 'app/core/DevKenService/userActivity/UserActivityService';

// Activity type → badge colour + icon
const ACTIVITY_META: Record<string, { badge: string; icon: string }> = {
  Login:          { badge: 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-300',   icon: 'login'          },
  Logout:         { badge: 'bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300',           icon: 'logout'         },
  PasswordChange: { badge: 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-300',   icon: 'lock_reset'     },
  RoleAssigned:   { badge: 'bg-indigo-100 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-300', icon: 'shield'        },
  RoleRemoved:    { badge: 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-300',            icon: 'shield_outlined'},
  ProfileUpdate:  { badge: 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300',        icon: 'edit'           },
  AccountCreated: { badge: 'bg-violet-100 dark:bg-violet-900/30 text-violet-700 dark:text-violet-300', icon: 'person_add'   },
  AccountDeleted: { badge: 'bg-rose-100 dark:bg-rose-900/30 text-rose-700 dark:text-rose-300',        icon: 'person_remove'  },
};

const DEFAULT_META = { badge: 'bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300', icon: 'info' };

@Component({
  selector: 'app-user-activity',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatButtonModule, MatTooltipModule,
    PageHeaderComponent, FilterPanelComponent,
    PaginationComponent, StatsCardsComponent, DataTableComponent,
  ],
  templateUrl: './user-activity.component.html',
})
export class UserActivityComponent implements OnInit, AfterViewInit, OnDestroy {

  // ── Cell Templates ────────────────────────────────────────────────────────────
  @ViewChild('userCell')    userCellTemplate!:    TemplateRef<any>;
  @ViewChild('schoolCell') schoolCellTemplate!: TemplateRef<any>;

  @ViewChild('typeCell')    typeCellTemplate!:    TemplateRef<any>;
  @ViewChild('detailsCell') detailsCellTemplate!: TemplateRef<any>;
  @ViewChild('timeCell')    timeCellTemplate!:    TemplateRef<any>;

  private readonly _activitySvc  = inject(UserActivityService);
  private readonly _alert        = inject(AlertService);
  private readonly _unsubscribe$ = new Subject<void>();

  // ── Breadcrumbs ───────────────────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Dashboard',      url: '/dashboard' },
    { label: 'Administration', url: '/admin' },
    { label: 'User Activity' },
  ];

  // ── State ─────────────────────────────────────────────────────────────────────
  allData:      UserActivityDto[]  = [];
  summary:      ActivitySummaryDto = { totalActivities: 0, todayActivities: 0, loginCount: 0, uniqueUsers: 0 };
  isLoading     = false;
  showFilterPanel = false;
  cellTemplates:  { [key: string]: TemplateRef<any> } = {};

  // ── Filters ───────────────────────────────────────────────────────────────────
  private _filterValues = {
    search:       '',
    activityType: 'all',
    dateRange:    'all',
  };

  // ── Pagination ────────────────────────────────────────────────────────────────
  currentPage  = 1;
  itemsPerPage = 20;

  // ── Stats ─────────────────────────────────────────────────────────────────────
  get statsCards(): StatCard[] {
    return [
      { label: 'Total Activities', value: this.summary.totalActivities, icon: 'history',         iconColor: 'indigo' },
      { label: 'Today',            value: this.summary.todayActivities,  icon: 'today',           iconColor: 'green'  },
      { label: 'Logins',           value: this.summary.loginCount,       icon: 'login',           iconColor: 'blue'   },
      { label: 'Unique Users',     value: this.summary.uniqueUsers,      icon: 'group',           iconColor: 'violet' },
    ];
  }

  // ── Filtered + Paginated ──────────────────────────────────────────────────────
  get filteredData(): UserActivityDto[] {
    const q    = this._filterValues.search.toLowerCase();
    const type = this._filterValues.activityType;
    const range = this._filterValues.dateRange;
    const now  = new Date();

    return this.allData.filter(a => {
            const matchText =
            !q ||
            a.userFullName?.toLowerCase().includes(q) ||
            a.userEmail?.toLowerCase().includes(q)    ||
            a.schoolName?.toLowerCase().includes(q)   ||
            a.activityType?.toLowerCase().includes(q) ||
            a.activityDetails?.toLowerCase().includes(q);



      const matchType = type === 'all' || a.activityType === type;

      let matchDate = true;
      if (range !== 'all') {
        const created = new Date(a.createdOn);
        const diffMs  = now.getTime() - created.getTime();
        const diffDays = diffMs / 86_400_000;
        if (range === 'today')  matchDate = created.toDateString() === now.toDateString();
        if (range === '7days')  matchDate = diffDays <= 7;
        if (range === '30days') matchDate = diffDays <= 30;
      }

      return matchText && matchType && matchDate;
    });
  }

  get paginatedData(): UserActivityDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  // ── Table Config ──────────────────────────────────────────────────────────────
readonly tableColumns: TableColumn<UserActivityDto>[] = [
  { id: 'user',    label: 'User',        align: 'left', sortable: true },
  { id: 'school',  label: 'School',      align: 'left', sortable: true },  // ✅
  { id: 'type',    label: 'Activity',    align: 'left' },
  { id: 'details', label: 'Details',     align: 'left', hideOnMobile: true },
  { id: 'time',    label: 'Date & Time', align: 'left', sortable: true },
];

get tableHeaderWithSubtitle(): TableHeader {
  return { ...this.tableHeader, subtitle: this.tableSubtitle };
}


  tableHeader: TableHeader = {
    title:        'Activity Log',
    subtitle:     '',
    icon:         'manage_search',
    iconGradient: 'bg-gradient-to-br from-indigo-500 via-violet-600 to-purple-700',
  };

  tableEmptyState: TableEmptyState = {
    icon:        'history',
    message:     'No activities found',
    description: 'Try adjusting your filters or date range',
  };

  // ── Filter Fields ─────────────────────────────────────────────────────────────
  filterFields: FilterField[] = [
    {
      id:          'search',
      label:       'Search',
      type:        'text',
      placeholder: 'User name, email, or activity...',
      value:       '',
    },
    {
      id:      'activityType',
      label:   'Activity Type',
      type:    'select',
      value:   'all',
      options: [
        { label: 'All Types',       value: 'all'            },
        { label: 'Login',           value: 'Login'          },
        { label: 'Logout',          value: 'Logout'         },
        { label: 'Password Change', value: 'PasswordChange' },
        { label: 'Role Assigned',   value: 'RoleAssigned'   },
        { label: 'Role Removed',    value: 'RoleRemoved'    },
        { label: 'Profile Update',  value: 'ProfileUpdate'  },
        { label: 'Account Created', value: 'AccountCreated' },
        { label: 'Account Deleted', value: 'AccountDeleted' },
      ],
    },
    {
      id:      'dateRange',
      label:   'Date Range',
      type:    'select',
      value:   'all',
      options: [
        { label: 'All Time',    value: 'all'    },
        { label: 'Today',       value: 'today'  },
        { label: 'Last 7 Days', value: '7days'  },
        { label: 'Last 30 Days',value: '30days' },
      ],
    },
  ];

  // ── Lifecycle ─────────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.loadAll();
  }

  formatActivityType(type: string): string {
  return type?.replace('user.', '').replace('.', ' ').toUpperCase();
}

get tableSubtitle(): string {
  return `${this.filteredData.length} activities found`;
}

  ngAfterViewInit(): void {
    this.cellTemplates = {
        user:    this.userCellTemplate,
        school:  this.schoolCellTemplate,   // 👈 ADD THIS
        type:    this.typeCellTemplate,
        details: this.detailsCellTemplate,
        time:    this.timeCellTemplate,
        };

  }

  ngOnDestroy(): void {
    this._unsubscribe$.next();
    this._unsubscribe$.complete();
  }

  // ── Data Loading ──────────────────────────────────────────────────────────────
  loadAll(): void {
    this.isLoading = true;

    forkJoin({
      activities: this._activitySvc.getAll(1, 1000),
      summary:    this._activitySvc.getSummary(),
    })
    .pipe(takeUntil(this._unsubscribe$), finalize(() => (this.isLoading = false)))
    .subscribe({
      next: ({ activities, summary }) => {
        if (activities?.success && activities.data) {
          this.allData = activities.data.items ?? [];
        }
        if (summary?.success && summary.data) {
          this.summary = summary.data;
        }
        this.tableHeader.subtitle = `${this.filteredData.length} activities found`;
      },
      error: err => {
        this._alert.error(err?.error?.message || 'Failed to load activity log');
      }
    });
  }

  // ── Filter Handlers ───────────────────────────────────────────────────────────
  toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

  onFilterChange(event: FilterChangeEvent): void {
    (this._filterValues as any)[event.filterId] = event.value;
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} activities found`;
  }

  onClearFilters(): void {
    this._filterValues = { search: '', activityType: 'all', dateRange: 'all' };
    this.filterFields.forEach(f => { f.value = (this._filterValues as any)[f.id]; });
    this.currentPage = 1;
    this.tableHeader.subtitle = `${this.filteredData.length} activities found`;
  }

  // ── Pagination ────────────────────────────────────────────────────────────────
  onPageChange(page: number): void         { this.currentPage  = page; }
  onItemsPerPageChange(n: number): void    { this.itemsPerPage = n; this.currentPage = 1; }

  // ── Template Helpers ──────────────────────────────────────────────────────────
  getInitials(name: string): string {
    if (!name?.trim()) return '?';
    const parts = name.trim().split(' ');
    return parts.length >= 2
      ? (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
      : name.substring(0, 2).toUpperCase();
  }

  getTypeBadgeClass(type: string): string {
    return (ACTIVITY_META[type] ?? DEFAULT_META).badge;
  }

  getTypeIcon(type: string): string {
    return (ACTIVITY_META[type] ?? DEFAULT_META).icon;
  }

  formatDate(iso: string): string {
    if (!iso) return '—';
    const d   = new Date(iso);
    const now = new Date();
    const diffDays = Math.floor((now.getTime() - d.getTime()) / 86_400_000);
    if (diffDays === 0) return 'Today';
    if (diffDays === 1) return 'Yesterday';
    if (diffDays < 7)  return `${diffDays} days ago`;
    return d.toLocaleDateString('en-US', {
      month: 'short', day: 'numeric',
      year: d.getFullYear() !== now.getFullYear() ? 'numeric' : undefined
    });
  }

  formatTime(iso: string): string {
    if (!iso) return '';
    return new Date(iso).toLocaleTimeString('en-US', {
      hour: '2-digit', minute: '2-digit', hour12: true
    });
  }
}