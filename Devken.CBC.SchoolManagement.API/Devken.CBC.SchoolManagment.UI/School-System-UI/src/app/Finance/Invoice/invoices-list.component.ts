import { Component, OnInit, TemplateRef, ViewChild, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTableDataSource } from '@angular/material/table';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { InvoiceService } from 'app/core/DevKenService/Invoice/Invoice.service ';
import { InvoiceFormDialogComponent } from 'app/dialog-modals/Invoice/invoice-form-dialog.component';
import { DataTableComponent, TableHeader, TableColumn, TableAction, TableEmptyState } from 'app/shared/data-table/data-table.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';
import { InvoiceSummaryResponseDto, InvoiceStatus, InvoiceQueryDto } from './Types/Invoice.types';
import { InvoiceViewDialogComponent } from './View/invoice-view-dialog.component';
import { Router } from '@angular/router';


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
  @ViewChild('statusTpl', { static: true }) statusTpl!: TemplateRef<any>;
  @ViewChild('amountTpl', { static: true }) amountTpl!: TemplateRef<any>;
  @ViewChild('balanceTpl', { static: true }) balanceTpl!: TemplateRef<any>;
  @ViewChild('overdueTpl', { static: true }) overdueTpl!: TemplateRef<any>;

  private _router        = inject(Router);
  private destroy$ = new Subject<void>();
  dataSource = new MatTableDataSource<InvoiceSummaryResponseDto>([]);
  isLoading = false;

  // ── Header ──────────────────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Home', url: '/dashboard' },
    { label: 'Finance' },
    { label: 'Invoices' },
  ];

  // ── Stats ────────────────────────────────────────────────────────────────
  statsCards: StatCard[] = [];

  // ── Table ────────────────────────────────────────────────────────────────
  tableHeader: TableHeader = {
    title: 'Invoices',
    subtitle: 'All student fee invoices',
    icon: 'receipt_long',
    iconGradient: 'bg-gradient-to-br from-blue-500 via-indigo-600 to-violet-700',
  };

  columns: TableColumn<InvoiceSummaryResponseDto>[] = [
    { id: 'invoiceNumber', label: 'Invoice #',     sortable: true },
    { id: 'studentName',   label: 'Student',        sortable: true },
    { id: 'termName',      label: 'Term',           hideOnMobile: true },
    { id: 'invoiceDate',   label: 'Invoice Date',   hideOnMobile: true, hideOnTablet: true },
    { id: 'dueDate',       label: 'Due Date',       hideOnMobile: true },
    { id: 'totalAmount',   label: 'Total (KES)',    align: 'right' },
    { id: 'balance',       label: 'Balance (KES)',  align: 'right' },
    { id: 'isOverdue',     label: 'Overdue',        align: 'center', hideOnMobile: true },
    { id: 'statusInvoice', label: 'Status',         align: 'center' },
  ];

  actions: TableAction<InvoiceSummaryResponseDto>[] = [
    {
      id: 'view',
      label: 'View Details',
      icon: 'visibility',
      color: 'blue',
      handler: (row) => this.openView(row),
    },
    {
      id: 'edit',
      label: 'Edit',
      icon: 'edit',
      color: 'indigo',
      visible: (row) => row.statusInvoice !== InvoiceStatus.Cancelled && row.statusInvoice !== InvoiceStatus.Paid,
      handler: (row) => this.openEdit(row),
    },
    {
      id: 'cancel',
      label: 'Cancel Invoice',
      icon: 'cancel',
      color: 'amber',
      divider: true,
      visible: (row) => row.statusInvoice !== InvoiceStatus.Cancelled,
      handler: (row) => this.onCancel(row),
    },
    {
      id: 'delete',
      label: 'Delete',
      icon: 'delete',
      color: 'red',
      handler: (row) => this.onDelete(row),
    },
  ];

  emptyState: TableEmptyState = {
    icon: 'receipt_long',
    message: 'No invoices found',
    description: 'Create a new invoice to get started.',
    action: {
      label: 'Create Invoice',
      icon: 'add',
      handler: () => this.openCreate(),
    },
  };

  cellTemplates: { [key: string]: TemplateRef<any> } = {};

  // ── Filters ───────────────────────────────────────────────────────────────
  showFilters = false;
  filterFields: FilterField[] = [
    {
      id: 'invoiceStatus',
      label: 'Status',
      type: 'select',
      value: 'all',
      options: [
        { label: 'All Statuses', value: 'all' },
        { label: 'Draft',           value: InvoiceStatus.Draft },
        { label: 'Issued',          value: InvoiceStatus.Issued },
        { label: 'Partially Paid',  value: InvoiceStatus.PartiallyPaid },
        { label: 'Paid',            value: InvoiceStatus.Paid },
        { label: 'Overdue',         value: InvoiceStatus.Overdue },
        { label: 'Cancelled',       value: InvoiceStatus.Cancelled },
      ],
    },
    {
      id: 'isOverdue',
      label: 'Overdue',
      type: 'select',
      value: 'all',
      options: [
        { label: 'All',       value: 'all' },
        { label: 'Overdue',   value: 'true' },
        { label: 'On Time',   value: 'false' },
      ],
    },
    {
      id: 'dateFrom',
      label: 'Date From',
      type: 'date',
      value: '',
    },
    {
      id: 'dateTo',
      label: 'Date To',
      type: 'date',
      value: '',
    },
  ];

  activeFilters: InvoiceQueryDto = {};

  // ── Pagination ────────────────────────────────────────────────────────────
  currentPage = 1;
  itemsPerPage = 10;

  get filteredData(): InvoiceSummaryResponseDto[] {
    return this.dataSource.data;
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

  ngOnInit(): void {
    this.loadAll();
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

  loadAll(): void {
    this.isLoading = true;
    this.invoiceService.getAll(this.activeFilters).subscribe({
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

  private buildStats(data: InvoiceSummaryResponseDto[]): void {
    const total        = data.reduce((s, i) => s + i.totalAmount, 0);
    const collected    = data.reduce((s, i) => s + i.amountPaid, 0);
    const outstanding  = data.reduce((s, i) => s + i.balance, 0);
    const overdueCount = data.filter(i => i.isOverdue).length;

    this.statsCards = [
      { label: 'Total Invoiced',  value: this.formatCurrency(total),       icon: 'receipt_long',    iconColor: 'indigo' },
      { label: 'Amount Collected', value: this.formatCurrency(collected),   icon: 'payments',        iconColor: 'green'  },
      { label: 'Outstanding',      value: this.formatCurrency(outstanding), icon: 'pending_actions', iconColor: 'amber'  },
      { label: 'Overdue',          value: overdueCount,                     icon: 'warning',         iconColor: 'red'    },
    ];
  }

  formatCurrency(val: number): string {
    return new Intl.NumberFormat('en-KE', { style: 'currency', currency: 'KES', maximumFractionDigits: 0 }).format(val);
  }

  // ── Filters ───────────────────────────────────────────────────────────────
  onFilterChange(event: FilterChangeEvent): void {
    if (event.filterId === 'invoiceStatus') {
      this.activeFilters.invoiceStatus = event.value === 'all' ? undefined : Number(event.value);
    } else if (event.filterId === 'isOverdue') {
      this.activeFilters.isOverdue = event.value === 'all' ? undefined : event.value === 'true';
    } else if (event.filterId === 'dateFrom') {
      this.activeFilters.dateFrom = event.value || undefined;
    } else if (event.filterId === 'dateTo') {
      this.activeFilters.dateTo = event.value || undefined;
    }
    this.loadAll();
  }

  onClearFilters(): void {
    this.activeFilters = {};
    this.loadAll();
  }

  // ── CRUD ──────────────────────────────────────────────────────────────────
  openCreate(): void {
  this._router.navigate(['/finance/invoices/create']);
}

openEdit(row: InvoiceSummaryResponseDto): void {
  this._router.navigate(['/finance/invoices/edit', row.id]);
}

  openView(row: InvoiceSummaryResponseDto): void {
    this.invoiceService.getById(row.id).subscribe((res) => {
      if (res.success) {
        this.dialog.open(InvoiceViewDialogComponent, {
          width: '860px',
          maxHeight: '95vh',
          data: { mode: 'view', invoice: res.data },
        });
      }
    });
  }

  onCancel(row: InvoiceSummaryResponseDto): void {
    this.alertService.confirm({
      title: 'Cancel Invoice',
      message: `Are you sure you want to cancel invoice ${row.invoiceNumber}? This action cannot be undone.`,
      confirmText: 'Yes, Cancel',
      cancelText: 'Keep',
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
      confirmText: 'Delete',
      cancelText: 'Cancel',
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
  onPageChange(page: number): void { this.currentPage = page; }
  onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

  // ── Helpers ───────────────────────────────────────────────────────────────
  getStatusClass(status: InvoiceStatus): string {
    const map: Record<InvoiceStatus, string> = {
      [InvoiceStatus.Draft]:         'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300',
      [InvoiceStatus.Issued]:        'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
      [InvoiceStatus.PartiallyPaid]: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
      [InvoiceStatus.Paid]:          'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400',
      [InvoiceStatus.Overdue]:       'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
      [InvoiceStatus.Cancelled]:     'bg-gray-200 text-gray-500 dark:bg-gray-800 dark:text-gray-500',
    };
    return map[status] ?? map[InvoiceStatus.Draft];
  }
}