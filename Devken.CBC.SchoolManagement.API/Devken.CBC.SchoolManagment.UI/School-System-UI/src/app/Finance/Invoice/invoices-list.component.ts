import { Component, OnInit, OnDestroy, AfterViewInit, TemplateRef, ViewChild, inject } from '@angular/core';
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
import { StudentService } from 'app/core/DevKenService/administration/students/StudentService';

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
export class InvoicesListComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('statusTpl',  { static: true }) statusTpl!:  TemplateRef<any>;
  @ViewChild('amountTpl',  { static: true }) amountTpl!:  TemplateRef<any>;
  @ViewChild('balanceTpl', { static: true }) balanceTpl!: TemplateRef<any>;
  @ViewChild('overdueTpl', { static: true }) overdueTpl!: TemplateRef<any>;

  private _router          = inject(Router);
  private _authService     = inject(AuthService);
  private _schoolService   = inject(SchoolService);
  private _classService    = inject(ClassService);
  private _studentService  = inject(StudentService);
  private destroy$         = new Subject<void>();

  private classStudentIds: Set<string> | null = null;
  isLoadingClassStudents = false;

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
      id: 'payments', label: 'View Payments', icon: 'payments', color: 'green',
      handler: (row) => this.openPayments(row),
    },
    {
      id: 'add-payment', label: 'Record Payment', icon: 'add_card', color: 'teal',
      visible: (row) =>
        row.balance > 0 &&
        row.statusInvoice !== InvoiceStatus.Cancelled &&
        row.statusInvoice !== InvoiceStatus.Refunded,
      handler: (row) => this.recordPayment(row),
    },
    {
      id: 'recalculate', label: 'Refresh Status', icon: 'sync', color: 'violet',
      visible: (row) =>
        row.statusInvoice !== InvoiceStatus.Cancelled &&
        row.statusInvoice !== InvoiceStatus.Refunded,
      handler: (row) => this.recalculateStatus(row),
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
      visible: (row) =>
        row.statusInvoice !== InvoiceStatus.Paid &&
        row.statusInvoice !== InvoiceStatus.Cancelled,
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

    if (this.classStudentIds !== null) {
      data = data.filter(r => this.classStudentIds!.has(r.studentId));
    }

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      data = data.filter(r =>
        r.invoiceNumber?.toLowerCase().includes(q) ||
        r.studentName?.toLowerCase().includes(q)
      );
    }

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

    const query: InvoiceQueryDto = {
      ...this.activeFilters,
      ...(this.selectedClassId ? { classId: this.selectedClassId } : {}),
    };

    this.invoiceService.getAll(query, schoolId)
      .pipe(takeUntil(this.destroy$), finalize(() => this.isLoading = false))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.dataSource.data = res.data;
            this.buildStats(res.data);
            this.currentPage = 1;
            // ── Auto-fix any invoices whose status is stale after a payment ──
            this.recalculateStaleInvoices(res.data);
          }
        },
        error: () => this.alertService.error('Failed to load invoices.'),
      });
  }

  /**
   * Detects invoices whose persisted status contradicts the recorded
   * amountPaid / balance figures — which happens when a payment is saved
   * but the backend PaymentService did not trigger RecalculateAsync.
   *
   * Each stale invoice is silently recalculated and patched in-place so
   * the table reflects the correct status without a full reload.
   */
  private recalculateStaleInvoices(data: InvoiceSummaryResponseDto[]): void {
    const stale = data.filter(inv =>
      // Skip terminal states — they are never stale
      inv.statusInvoice !== InvoiceStatus.Cancelled &&
      inv.statusInvoice !== InvoiceStatus.Refunded  &&
      (
        // Has payments but still shows Pending
        (inv.amountPaid > 0 && inv.statusInvoice === InvoiceStatus.Pending) ||
        // Fully settled but not marked Paid
        (inv.balance <= 0 && inv.amountPaid > 0 && inv.statusInvoice !== InvoiceStatus.Paid)
      )
    );

    stale.forEach(inv => {
      this.invoiceService.recalculate(inv.id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: res => {
            if (res.success) {
              const idx = this.dataSource.data.findIndex(r => r.id === inv.id);
              if (idx !== -1) {
                const updated  = { ...this.dataSource.data[idx], ...res.data };
                const newData  = [...this.dataSource.data];
                newData[idx]   = updated;
                this.dataSource.data = newData;
                this.buildStats(this.dataSource.data);
              }
            }
          },
          // Swallow silently — stale recalculation is best-effort
          error: () => {},
        });
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
      error: () => {},
    });
  }

  private loadStudentsForClass(classId: string): void {
    this.isLoadingClassStudents = true;
    this.classStudentIds = null;

    const schoolId = this.isSuperAdmin ? this.selectedSchoolId! : undefined;

    this._studentService.getAll(schoolId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res: any) => {
          this.isLoadingClassStudents = false;
          const students: any[] = Array.isArray(res) ? res : (res?.data ?? []);
          const inClass = students.filter(s =>
            s.currentClassId === classId ||
            s.classId        === classId ||
            s.gradeId        === classId ||
            s.grade?.id      === classId ||
            s.class?.id      === classId
          );
          this.classStudentIds = new Set(
            (inClass.length > 0 ? inClass : []).map((s: any) => s.id as string)
          );
          this.currentPage = 1;
        },
        error: () => {
          this.isLoadingClassStudents = false;
          this.classStudentIds = new Set();
        },
      });
  }

  // ── Dynamic filter injection ──────────────────────────────────────────────

  private injectSchoolFilter(): void {
    const schoolFilter: FilterField = {
      id: 'schoolId', label: 'School', type: 'select',
      value: this.selectedSchoolId,
      options: this.schools.map(s => ({ label: s.name, value: s.id })),
    };
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
    const total        = data.reduce((s, i) => s + i.totalAmount, 0);
    const collected    = data.reduce((s, i) => s + i.amountPaid,  0);
    const outstanding  = data.reduce((s, i) => s + i.balance,     0);
    const overdueCount = data.filter(i => i.isOverdue).length;
    const paidCount    = data.filter(i => i.statusInvoice === InvoiceStatus.Paid).length;
    const partialCount = data.filter(i => i.statusInvoice === InvoiceStatus.PartiallyPaid).length;

    this.statsCards = [
      { label: 'Total Invoiced',   value: this.formatCurrency(total),       icon: 'receipt_long',    iconColor: 'indigo' },
      { label: 'Amount Collected', value: this.formatCurrency(collected),   icon: 'payments',        iconColor: 'green'  },
      { label: 'Outstanding',      value: this.formatCurrency(outstanding), icon: 'pending_actions', iconColor: 'amber'  },
      { label: 'Overdue',          value: overdueCount,                     icon: 'warning',         iconColor: 'red'    },
      { label: 'Paid',             value: paidCount,                        icon: 'check_circle',    iconColor: 'green'  },
      { label: 'Partially Paid',   value: partialCount,                     icon: 'timelapse',       iconColor: 'blue'   },
    ];

    this.tableHeader = {
      ...this.tableHeader,
      subtitle: `${data.length.toLocaleString()} invoice${data.length !== 1 ? 's' : ''}`,
    };
  }

  // ── Filter events ─────────────────────────────────────────────────────────

  onFilterChange(event: FilterChangeEvent): void {
    switch (event.filterId) {
      case 'schoolId':
        this.selectedSchoolId = event.value;
        this.selectedClassId  = null;
        this.classStudentIds  = null;
        this.loadClasses(event.value);
        break;
      case 'classId':
        this.selectedClassId = event.value === 'all' ? null : event.value;
        if (!this.selectedClassId) {
          this.classStudentIds = null;
          this.currentPage = 1;
          return;
        }
        this.loadStudentsForClass(this.selectedClassId);
        return;
      case 'invoiceStatus':
        this.activeFilters.invoiceStatus = event.value === 'all' ? undefined : Number(event.value);
        break;
      case 'isOverdue':
        if (event.value === 'all') {
          this.activeFilters.isOverdue = undefined;
          this.isOverdueClientFilter   = null;
        } else if (event.value === 'true') {
          this.activeFilters.isOverdue = true;
          this.isOverdueClientFilter   = null;
        } else {
          this.activeFilters.isOverdue = undefined;
          this.isOverdueClientFilter   = false;
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
        return;
    }
    this.loadAll();
  }

  onClearFilters(): void {
    this.activeFilters         = {};
    this.selectedClassId       = null;
    this.classStudentIds       = null;
    this.searchQuery           = '';
    this.isOverdueClientFilter = null;
    this.filterFields = this.filterFields.map(f => ({
      ...f,
      value: f.type === 'select' ? (f.id === 'schoolId' ? this.selectedSchoolId : 'all') : '',
    }));
    this.loadAll();
  }

  // ── CRUD ──────────────────────────────────────────────────────────────────

  openCreate(): void    { this._router.navigate(['/finance/invoices/create']); }
  openBulkCreate(): void { this._router.navigate(['/finance/invoices/bulk-create']); }

  openEdit(row: InvoiceSummaryResponseDto): void {
    this._router.navigate(['/finance/invoices/edit', row.id]);
  }

  openView(row: InvoiceSummaryResponseDto): void {
    this.isLoading = true;
    this.invoiceService.getById(row.id)
      .pipe(takeUntil(this.destroy$), finalize(() => this.isLoading = false))
      .subscribe({
        next: (res) => {
          if (res.success) {
            const ref = this.dialog.open(InvoiceViewDialogComponent, {
              width: '860px', maxHeight: '95vh',
              data: { mode: 'view', invoice: res.data },
            });
            // Reload the list when the dialog closes in case status changed
            ref.afterClosed()
              .pipe(takeUntil(this.destroy$))
              .subscribe(() => this.loadAll());
          }
        },
        error: () => this.alertService.error('Could not load invoice details.'),
      });
  }

  openLineItems(row: InvoiceSummaryResponseDto): void {
    this._router.navigate(['/finance/invoice-items'], { queryParams: { invoiceId: row.id } });
  }

  openPayments(row: InvoiceSummaryResponseDto): void {
    this._router.navigate(['/finance/payments'], { queryParams: { invoiceId: row.id } });
  }

  recordPayment(row: InvoiceSummaryResponseDto): void {
    this._router.navigate(['/finance/payments/create'], {
      queryParams: { invoiceId: row.id, studentId: row.studentId, amount: row.balance },
    });
  }

  // ── Recalculate status ────────────────────────────────────────────────────

  recalculateStatus(row: InvoiceSummaryResponseDto): void {
    this.invoiceService.recalculate(row.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.success) {
            // Patch just this row in-place so the table refreshes without a full reload
            const idx = this.dataSource.data.findIndex(r => r.id === row.id);
            if (idx !== -1) {
              const updated = { ...this.dataSource.data[idx], ...res.data };
              const newData  = [...this.dataSource.data];
              newData[idx]   = updated;
              this.dataSource.data = newData;
              this.buildStats(this.dataSource.data);
            }
            this.snackBar.open(
              `Status updated → ${res.data.statusDisplay}`, 'Close', { duration: 2500 });
          }
        },
        error: () => this.alertService.error('Failed to refresh invoice status.'),
      });
  }

  onCancel(row: InvoiceSummaryResponseDto): void {
    this.alertService.confirm({
      title: 'Cancel Invoice',
      message: `Cancel invoice ${row.invoiceNumber}? This cannot be undone.`,
      confirmText: 'Yes, Cancel', cancelText: 'Keep',
      onConfirm: () => {
        this.invoiceService.cancel(row.id)
          .pipe(takeUntil(this.destroy$))
          .subscribe({
            next: (res) => {
              if (res.success) {
                this.snackBar.open('Invoice cancelled.', 'Close', { duration: 3000 });
                this.loadAll();
              }
            },
            error: () => this.alertService.error('Failed to cancel invoice.'),
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
        this.invoiceService.delete(row.id)
          .pipe(takeUntil(this.destroy$))
          .subscribe({
            next: (res) => {
              if (res.success) {
                this.snackBar.open('Invoice deleted.', 'Close', { duration: 2500 });
                this.loadAll();
              }
            },
            error: () => this.alertService.error('Failed to delete invoice.'),
          });
      },
    });
  }

  // ── Pagination ────────────────────────────────────────────────────────────
  onPageChange(page: number): void      { this.currentPage = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

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