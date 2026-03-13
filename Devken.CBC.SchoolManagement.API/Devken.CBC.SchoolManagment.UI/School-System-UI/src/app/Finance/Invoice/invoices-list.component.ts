import { Component, OnInit, TemplateRef, ViewChild, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTableDataSource } from '@angular/material/table';
import { Subject, takeUntil } from 'rxjs';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { DataTableComponent, TableHeader, TableColumn, TableAction, TableEmptyState } from 'app/shared/data-table/data-table.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';
import { InvoiceSummaryResponseDto, InvoiceStatus, InvoiceQueryDto } from './Types/Invoice.types';
import { InvoiceViewDialogComponent } from './View/invoice-view-dialog.component';
import { Router } from '@angular/router';
import { AuthService } from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { SchoolDto } from 'app/Tenant/types/school';
import { InvoiceService } from 'app/core/DevKenService/Finance/Invoice/Invoice.service ';
import { ClassService } from 'app/core/DevKenService/ClassService/ClassService';
import { ClassDto } from 'app/Classes/Types/Class';

@Component({
  selector: 'app-invoices-list',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatButtonModule,
    PageHeaderComponent,
    StatsCardsComponent,
    DataTableComponent,
    FilterPanelComponent,
    PaginationComponent,
  ],
  templateUrl: './invoices-list.component.html',
})
export class InvoicesListComponent implements OnInit, OnDestroy {
  @ViewChild('statusTpl',  { static: true }) statusTpl!:  TemplateRef<any>;
  @ViewChild('amountTpl',  { static: true }) amountTpl!:  TemplateRef<any>;
  @ViewChild('balanceTpl', { static: true }) balanceTpl!: TemplateRef<any>;
  @ViewChild('overdueTpl', { static: true }) overdueTpl!: TemplateRef<any>;

  private _router        = inject(Router);
  private _authService   = inject(AuthService);
  private _schoolService = inject(SchoolService);
  private _classService  = inject(ClassService);
  private destroy$       = new Subject<void>();

  dataSource = new MatTableDataSource<InvoiceSummaryResponseDto>([]);
  isLoading  = false;

  get isSuperAdmin(): boolean {
    return this._authService.authUser?.isSuperAdmin ?? false;
  }

  schools: SchoolDto[] = [];
  classes: ClassDto[]  = [];

  selectedSchoolId: string | null = null;
  selectedClassId:  string | null = null;
  searchQuery = '';
  isOverdueClientFilter: boolean | null = null;

  // ── Header ────────────────────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Home', url: '/dashboard' },
    { label: 'Finance' },
    { label: 'Invoices' },
  ];

  statsCards: StatCard[] = [];

  tableHeader: TableHeader = {
    title: 'Invoices',
    subtitle: 'All student fee invoices',
    icon: 'receipt_long',
    iconGradient: 'bg-gradient-to-br from-blue-500 via-indigo-600 to-violet-700',
  };

  columns: TableColumn<InvoiceSummaryResponseDto>[] = [
    { id: 'invoiceNumber', label: 'Invoice #',    sortable: true },
    { id: 'studentName',   label: 'Student',       sortable: true },
    { id: 'termName',      label: 'Term',          hideOnMobile: true },
    { id: 'invoiceDate',   label: 'Invoice Date',  hideOnMobile: true, hideOnTablet: true },
    { id: 'dueDate',       label: 'Due Date',      hideOnMobile: true },
    { id: 'totalAmount',   label: 'Total (KES)',   align: 'right' },
    { id: 'balance',       label: 'Balance (KES)', align: 'right' },
    { id: 'isOverdue',     label: 'Overdue',       align: 'center', hideOnMobile: true },
    { id: 'statusInvoice', label: 'Status',        align: 'center' },
  ];

  actions: TableAction<InvoiceSummaryResponseDto>[] = [
    {
      id: 'view', label: 'View Details', icon: 'visibility', color: 'blue',
      handler: (row) => this.openView(row),
    },
    {
      id: 'view-items', label: 'Line Items', icon: 'receipt_long', color: 'indigo',
      handler: (row) => this.openLineItems(row),
    },
    {
      id: 'edit', label: 'Edit', icon: 'edit', color: 'indigo',
      visible: (row) =>
        row.statusInvoice !== InvoiceStatus.Cancelled &&
        row.statusInvoice !== InvoiceStatus.Paid &&
        row.statusInvoice !== InvoiceStatus.Refunded,
      handler: (row) => this.openEdit(row),
    },
    {
      id: 'cancel', label: 'Cancel Invoice', icon: 'cancel', color: 'amber', divider: true,
      visible: (row) =>
        row.statusInvoice !== InvoiceStatus.Cancelled &&
        row.statusInvoice !== InvoiceStatus.Refunded,
      handler: (row) => this.onCancel(row),
    },
    {
      id: 'delete', label: 'Delete', icon: 'delete', color: 'red',
      handler: (row) => this.onDelete(row),
    },
  ];

  emptyState: TableEmptyState = {
    icon: 'receipt_long',
    message: 'No invoices found',
    description: 'Create a new invoice to get started.',
    action: { label: 'Create Invoice', icon: 'add', handler: () => this.openCreate() },
  };

  cellTemplates: { [key: string]: TemplateRef<any> } = {};

  // ── Filters ───────────────────────────────────────────────────────────────
  showFilters = false;

  // Base filter fields — school/class injected dynamically after data loads
  filterFields: FilterField[] = [
    {
      id: 'invoiceStatus', label: 'Status', type: 'select', value: 'all',
      options: [
        { label: 'All Statuses',   value: 'all'                       },
        { label: 'Draft',          value: InvoiceStatus.Draft         },
        { label: 'Pending',        value: InvoiceStatus.Pending       },
        { label: 'Partially Paid', value: InvoiceStatus.PartiallyPaid },
        { label: 'Paid',           value: InvoiceStatus.Paid          },
        { label: 'Overdue',        value: InvoiceStatus.Overdue       },
        { label: 'Cancelled',      value: InvoiceStatus.Cancelled     },
        { label: 'Refunded',       value: InvoiceStatus.Refunded      },
      ],
    },
    {
      id: 'isOverdue', label: 'Overdue', type: 'select', value: 'all',
      options: [
        { label: 'All',     value: 'all'   },
        { label: 'Overdue', value: 'true'  },
        { label: 'On Time', value: 'false' },
      ],
    },
    { id: 'dateFrom', label: 'Date From', type: 'date', value: '' },
    { id: 'dateTo',   label: 'Date To',   type: 'date', value: '' },
    { id: 'search',   label: 'Search',    type: 'text', value: '',
      placeholder: 'Invoice #, student name…' },
  ];

  activeFilters: InvoiceQueryDto = {};

  // ── Pagination ────────────────────────────────────────────────────────────
  currentPage  = 1;
  itemsPerPage = 10;

  get filteredData(): InvoiceSummaryResponseDto[] {
    let data = this.dataSource.data;

    // Search: invoice number and student name
    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      data = data.filter(r =>
        r.invoiceNumber?.toLowerCase().includes(q) ||
        r.studentName?.toLowerCase().includes(q)
      );
    }

    // isOverdue=false is handled client-side (API returns empty when false is sent)
    if (this.isOverdueClientFilter !== null) {
      data = data.filter(r => r.isOverdue === this.isOverdueClientFilter);
    }

    return data;
  }

  get paginatedData(): InvoiceSummaryResponseDto[] {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredData.slice(start, start + this.itemsPerPage);
  }

  constructor(
    private invoiceService: InvoiceService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private alertService: AlertService,
  ) {}

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.cellTemplates = {
      statusInvoice: this.statusTpl,
      totalAmount:   this.amountTpl,
      balance:       this.balanceTpl,
      isOverdue:     this.overdueTpl,
    };

    if (this.isSuperAdmin) {
      this.loadSchools();
    } else {
      this.loadClasses();
      this.loadAll();
    }
  }

  ngAfterViewInit(): void {
    this.cellTemplates = {
      statusInvoice: this.statusTpl,
      totalAmount:   this.amountTpl,
      balance:       this.balanceTpl,
      isOverdue:     this.overdueTpl,
    };
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Data ──────────────────────────────────────────────────────────────────

  loadAll(): void {
    if (this.isSuperAdmin && !this.selectedSchoolId) {
      this.isLoading = false;
      return;
    }

    this.isLoading = true;
    const schoolId = this.isSuperAdmin ? this.selectedSchoolId! : undefined;

    // Pass classId to the query if selected
    const query: InvoiceQueryDto = {
      ...this.activeFilters,
      ...(this.selectedClassId ? { classId: this.selectedClassId } : {}),
    };

    this.invoiceService.getAll(query, schoolId).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success) {
          this.dataSource.data = res.data;
          this.buildStats(res.data);
          this.currentPage = 1;
        }
      },
      error: () => { this.isLoading = false; },
    });
  }

  private loadSchools(): void {
    this._schoolService.getAll().pipe(takeUntil(this.destroy$)).subscribe({
      next: (res: any) => {
        this.schools = res.data ?? [];
        if (this.schools.length > 0) {
          this.selectedSchoolId = this.schools[0].id;
          this.injectSchoolFilter();
          this.loadClasses(this.selectedSchoolId);
          this.loadAll();
        } else {
          this.isLoading = false;
        }
      },
      error: () => {
        this.alertService.error('Could not load schools. Please refresh.');
        this.isLoading = false;
      },
    });
  }

  private loadClasses(schoolId?: string): void {
    const call = schoolId
      ? this._classService.getAll(schoolId)
      : this._classService.getAll();

    call.pipe(takeUntil(this.destroy$)).subscribe({
      next: (res: any) => {
        this.classes = (res?.data ?? []).filter((c: ClassDto) => c.isActive !== false);
        this.injectClassFilter();
      },
      error: () => { /* non-critical — filter just won't appear */ },
    });
  }

  // ── Dynamic filter injection ───────────────────────────────────────────────

  private injectSchoolFilter(): void {
    const schoolFilter: FilterField = {
      id: 'schoolId', label: 'School', type: 'select',
      value: this.selectedSchoolId,
      options: this.schools.map(s => ({ label: s.name, value: s.id })),
    };
    // Place school first, remove any existing school filter
    this.filterFields = [
      schoolFilter,
      ...this.filterFields.filter(f => f.id !== 'schoolId'),
    ];
  }

  private injectClassFilter(): void {
    const classFilter: FilterField = {
      id: 'classId', label: 'Class', type: 'select', value: 'all',
      options: [
        { label: 'All Classes', value: 'all' },
        ...this.classes.map(c => ({ label: c.name, value: c.id })),
      ],
    };

    // Insert class filter after school (or at start for non-superAdmin)
    const schoolIdx = this.filterFields.findIndex(f => f.id === 'schoolId');
    const insertAt  = schoolIdx >= 0 ? schoolIdx + 1 : 0;
    const without   = this.filterFields.filter(f => f.id !== 'classId');
    this.filterFields = [
      ...without.slice(0, insertAt),
      classFilter,
      ...without.slice(insertAt),
    ];
  }

  // ── Stats ─────────────────────────────────────────────────────────────────

  private buildStats(data: InvoiceSummaryResponseDto[]): void {
    const total       = data.reduce((s, i) => s + i.totalAmount, 0);
    const collected   = data.reduce((s, i) => s + i.amountPaid,  0);
    const outstanding = data.reduce((s, i) => s + i.balance,     0);
    const overdueCount = data.filter(i => i.isOverdue).length;

    this.statsCards = [
      { label: 'Total Invoiced',   value: this.formatCurrency(total),       icon: 'receipt_long',    iconColor: 'indigo' },
      { label: 'Amount Collected', value: this.formatCurrency(collected),   icon: 'payments',        iconColor: 'green'  },
      { label: 'Outstanding',      value: this.formatCurrency(outstanding), icon: 'pending_actions', iconColor: 'amber'  },
      { label: 'Overdue',          value: overdueCount,                     icon: 'warning',         iconColor: 'red'    },
    ];
  }

  // ── Filter events ─────────────────────────────────────────────────────────

  onFilterChange(event: FilterChangeEvent): void {
    switch (event.filterId) {
      case 'schoolId':
        this.selectedSchoolId = event.value;
        this.selectedClassId  = null;
        // Reload classes for the newly selected school, then refresh invoices
        this.loadClasses(event.value);
        break;
      case 'classId':
        this.selectedClassId = event.value === 'all' ? null : event.value;
        break;
      case 'invoiceStatus':
        this.activeFilters.invoiceStatus = event.value === 'all' ? undefined : Number(event.value);
        break;
      case 'isOverdue':
        // Only send isOverdue=true to the API — sending false returns empty results
        // because the backend treats it as an exact match, not a "not overdue" filter.
        // "On Time" is handled client-side instead.
        if (event.value === 'all') {
          this.activeFilters.isOverdue = undefined;
          this.isOverdueClientFilter = null;
        } else if (event.value === 'true') {
          this.activeFilters.isOverdue = true;
          this.isOverdueClientFilter = null;
        } else {
          // 'false' = On Time: clear API filter, filter client-side after load
          this.activeFilters.isOverdue = undefined;
          this.isOverdueClientFilter = false;
        }
        break;
      case 'dateFrom':
        this.activeFilters.dateFrom = event.value || undefined;
        break;
      case 'dateTo':
        this.activeFilters.dateTo = event.value || undefined;
        break;
      case 'search':
        this.searchQuery = event.value ?? '';
        this.currentPage = 1;
        return; // client-side only — no API call needed
    }
    this.loadAll();
  }

  onClearFilters(): void {
    this.activeFilters         = {};
    this.selectedClassId       = null;
    this.searchQuery           = '';
    this.isOverdueClientFilter = null;
    this.filterFields    = this.filterFields.map(f => ({
      ...f,
      value: f.type === 'select' ? (f.id === 'schoolId' ? this.selectedSchoolId : 'all') : '',
    }));
    this.loadAll();
  }

  // ── CRUD ──────────────────────────────────────────────────────────────────

  openCreate(): void { this._router.navigate(['/finance/invoices/create']); }
  openEdit(row: InvoiceSummaryResponseDto): void { this._router.navigate(['/finance/invoices/edit', row.id]); }
  openBulkCreate(): void { this._router.navigate(['/finance/invoices/bulk-create']); }

  openView(row: InvoiceSummaryResponseDto): void {
    this.invoiceService.getById(row.id).subscribe((res) => {
      if (res.success) {
        this.dialog.open(InvoiceViewDialogComponent, {
          width: '860px', maxHeight: '95vh',
          data: { mode: 'view', invoice: res.data },
        });
      }
    });
  }

  openLineItems(row: InvoiceSummaryResponseDto): void {
    this._router.navigate(['/finance/invoice-items'], { queryParams: { invoiceId: row.id } });
  }

  onCancel(row: InvoiceSummaryResponseDto): void {
    this.alertService.confirm({
      title: 'Cancel Invoice',
      message: `Are you sure you want to cancel invoice ${row.invoiceNumber}? This action cannot be undone.`,
      confirmText: 'Yes, Cancel', cancelText: 'Keep',
      onConfirm: () => {
        this.invoiceService.cancel(row.id).subscribe((res) => {
          if (res.success) {
            this.snackBar.open('Invoice cancelled.', 'Close', { duration: 3000 });
            this.loadAll();
          }
        });
      },
    });
  }

  onDelete(row: InvoiceSummaryResponseDto): void {
    this.alertService.confirm({
      title: 'Delete Invoice',
      message: `Delete invoice ${row.invoiceNumber}? This cannot be undone.`,
      confirmText: 'Delete', cancelText: 'Cancel',
      onConfirm: () => {
        this.invoiceService.delete(row.id).subscribe((res) => {
          if (res.success) {
            this.snackBar.open('Invoice deleted.', 'Close', { duration: 2500 });
            this.loadAll();
          }
        });
      },
    });
  }

  // ── Pagination ────────────────────────────────────────────────────────────
  onPageChange(page: number): void       { this.currentPage = page; }
  onItemsPerPageChange(n: number): void  { this.itemsPerPage = n; this.currentPage = 1; }

  // ── Helpers ───────────────────────────────────────────────────────────────
  formatCurrency(val: number): string {
    return new Intl.NumberFormat('en-KE', {
      style: 'currency', currency: 'KES', maximumFractionDigits: 0,
    }).format(val);
  }

  getStatusClass(status: InvoiceStatus): string {
    const map: Record<InvoiceStatus, string> = {
      [InvoiceStatus.Draft]:         'bg-gray-100 text-gray-500 dark:bg-gray-700 dark:text-gray-400',
      [InvoiceStatus.Pending]:       'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300',
      [InvoiceStatus.PartiallyPaid]: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
      [InvoiceStatus.Paid]:          'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400',
      [InvoiceStatus.Overdue]:       'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
      [InvoiceStatus.Cancelled]:     'bg-gray-200 text-gray-500 dark:bg-gray-800 dark:text-gray-500',
      [InvoiceStatus.Refunded]:      'bg-violet-100 text-violet-700 dark:bg-violet-900/30 dark:text-violet-400',
    };
    return map[status] ?? map[InvoiceStatus.Pending];
  }
}