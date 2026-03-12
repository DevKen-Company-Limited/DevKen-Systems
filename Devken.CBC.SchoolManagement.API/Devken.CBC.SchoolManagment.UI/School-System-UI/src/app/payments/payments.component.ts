// ═══════════════════════════════════════════════════════════════════
// payments.component.ts  — fully server-side paged version
// ═══════════════════════════════════════════════════════════════════

import {
    Component, OnInit, OnDestroy, AfterViewInit,
    ViewChild, TemplateRef, inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { Subject } from 'rxjs';
import { takeUntil, finalize } from 'rxjs/operators';

import { PageHeaderComponent, Breadcrumb }
    from 'app/shared/Page-Header/page-header.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent }
    from 'app/shared/Filter/filter-panel.component';
import { PaginationComponent }
    from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard }
    from 'app/shared/stats-cards/stats-cards.component';
import { DataTableComponent, TableColumn, TableAction, TableHeader, TableEmptyState }
    from 'app/shared/data-table/data-table.component';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { AuthService } from 'app/core/auth/auth.service';
import { PaymentReportService } from 'app/core/DevKenService/payments/payment-report.service';
import { PaymentService } from 'app/core/DevKenService/payments/payment.service';
import { PaymentPagedResultDto, PaymentResponseDto, getPaymentMethodLabel } from './types/payments';


// ── Pure helpers (module-level, no class needed) ──────────────────────────────

export function getPaymentMethodIcon(method: string): string {
    switch (method) {
        case 'Cash':         return 'payments';
        case 'Mpesa':        return 'phone_android';
        case 'BankTransfer': return 'account_balance';
        case 'Cheque':       return 'receipt_long';
        case 'Card':         return 'credit_card';
        case 'Online':       return 'language';
        default:             return 'attach_money';
    }
}


@Component({
    selector: 'app-payments',
    standalone: true,
    imports: [
        CommonModule, FormsModule,
        MatIconModule, MatButtonModule, MatMenuModule,
        MatProgressSpinnerModule, MatTooltipModule, MatDividerModule,
        PageHeaderComponent, FilterPanelComponent, PaginationComponent,
        StatsCardsComponent, DataTableComponent,
    ],
    templateUrl: './payments.component.html',
})
export class PaymentsComponent implements OnInit, AfterViewInit, OnDestroy {

    // ── Template refs ─────────────────────────────────────────────────
    @ViewChild('referenceCell') referenceCellTpl!: TemplateRef<any>;
    @ViewChild('studentCell')   studentCellTpl!:   TemplateRef<any>;
    @ViewChild('invoiceCell')   invoiceCellTpl!:   TemplateRef<any>;
    @ViewChild('methodCell')    methodCellTpl!:    TemplateRef<any>;
    @ViewChild('amountCell')    amountCellTpl!:    TemplateRef<any>;
    @ViewChild('dateCell')      dateCellTpl!:      TemplateRef<any>;
    @ViewChild('statusCell')    statusCellTpl!:    TemplateRef<any>;

    private _destroy$      = new Subject<void>();
    private _authService   = inject(AuthService);
    private _router        = inject(Router);
    private _alertService  = inject(AlertService);
    private _reportService = inject(PaymentReportService);

    constructor(private _service: PaymentService) { }

    // ── Auth ──────────────────────────────────────────────────────────
    get isSuperAdmin(): boolean {
        return this._authService.authUser?.isSuperAdmin ?? false;
    }

    // ── Breadcrumbs ───────────────────────────────────────────────────
    breadcrumbs: Breadcrumb[] = [
        { label: 'Dashboard', url: '/dashboard' },
        { label: 'Finance',   url: '/finance' },
        { label: 'Payments' },
    ];

    // ── Server-side paged result ──────────────────────────────────────
    pagedResult: PaymentPagedResultDto | null = null;

    get tableData(): PaymentResponseDto[] {
        return this.pagedResult?.items ?? [];
    }

    get totalItems(): number {
        return this.pagedResult?.totalCount ?? 0;
    }

    // ── Stats (all figures come from the server) ───────────────────────
    get statsCards(): StatCard[] {
        const r   = this.pagedResult;
        const fmt = (n: number) =>
            `KES ${(n ?? 0).toLocaleString('en-KE', { minimumFractionDigits: 2 })}`;

        const cards: StatCard[] = [
            {
                label: 'Total Payments',
                value: r?.totalCount ?? 0,
                icon: 'payments', iconColor: 'indigo',
            },
            {
                label: 'Collected',
                value: fmt(r?.totalCollected ?? 0),
                icon: 'account_balance_wallet', iconColor: 'green',
            },
            {
                label: 'Reversed',
                value: fmt(r?.totalReversed ?? 0),
                icon: 'undo', iconColor: 'blue',
            },
            {
                label: 'Net Available',
                value: fmt(r?.netAvailable ?? 0),
                icon: 'savings',
                iconColor: (r?.netAvailable ?? 0) >= 0 ? 'blue' : 'red',
            },
            {
                label: 'Pending',
                value: r?.pendingCount ?? 0,
                icon: 'hourglass_empty', iconColor: 'amber',
            },
            {
                label: 'M-Pesa',
                value: r?.mpesaCount ?? 0,
                icon: 'phone_android', iconColor: 'green',
            },
        ];

        if (this.isSuperAdmin) {
            cards.unshift({
                label: 'All Schools',
                value: r?.schoolCount ?? '—',
                icon: 'corporate_fare', iconColor: 'violet',
            });
        }

        return cards;
    }

    // ── Table columns ─────────────────────────────────────────────────
    get tableColumns(): TableColumn<PaymentResponseDto>[] {
        return [
            { id: 'reference', label: 'Reference', align: 'left',  sortable: true },
            { id: 'student',   label: 'Student',   align: 'left',  sortable: true },
            { id: 'invoice',   label: 'Invoice',   align: 'left',  hideOnMobile: true },
            { id: 'method',    label: 'Method',    align: 'left',  hideOnMobile: true },
            { id: 'amount',    label: 'Amount',    align: 'right', sortable: true },
            { id: 'date',      label: 'Date',      align: 'left',  hideOnTablet: true },
            { id: 'status',    label: 'Status',    align: 'center' },
        ];
    }

    // ── Actions ───────────────────────────────────────────────────────
    tableActions: TableAction<PaymentResponseDto>[] = [
        {
            id: 'view', label: 'View Details', icon: 'visibility', color: 'blue',
            handler: p => this.viewPayment(p),
        },
        {
            id: 'receipt', label: 'Print Receipt', icon: 'receipt', color: 'indigo',
            handler: p => this.printReceipt(p),
        },
        {
            id: 'edit', label: 'Edit', icon: 'edit', color: 'teal',
            handler: p => this.editPayment(p),
            visible: p => !p.isReversal
                && p.statusPayment !== 'Completed'
                && p.statusPayment !== 'Reversed',
        },
        {
            id: 'reverse', label: 'Reverse', icon: 'undo', color: 'amber',
            divider: true,
            handler: p => this.reversePayment(p),
            visible: p => !p.isReversal && p.statusPayment === 'Completed',
        },
        {
            id: 'delete', label: 'Delete', icon: 'delete', color: 'red',
            handler: p => this.deletePayment(p),
            visible: p => !p.isReversal
                && p.statusPayment !== 'Completed'
                && p.statusPayment !== 'Reversed',
        },
    ];

    tableHeader: TableHeader = {
        title: 'Payments',
        subtitle: '',
        icon: 'payments',
        iconGradient: 'bg-gradient-to-br from-emerald-500 via-teal-600 to-green-700',
    };

    tableEmptyState: TableEmptyState = {
        icon: 'payments',
        message: 'No payments found',
        description: 'Record a new payment or adjust your filters',
        action: { label: 'Add Payment', icon: 'add', handler: () => this.createPayment() },
    };

    cellTemplates: { [key: string]: TemplateRef<any> } = {};

    // ── UI state ──────────────────────────────────────────────────────
    isLoading       = false;
    isDownloading   = false;
    showFilterPanel = false;
    currentPage     = 1;
    itemsPerPage    = 20;

    private _filters = {
        search:        '',
        paymentMethod: 'all',
        status:        'all',
        isReversal:    'all',
        from:          '',
        to:            '',
    };

    // ── Filter field definitions (static — built once) ────────────────
    filterFields: FilterField[] = [
        {
            id: 'search', label: 'Search', type: 'text',
            placeholder: 'Reference, student, M-Pesa code…',
            value: '',
        },
        {
            id: 'paymentMethod', label: 'Method', type: 'select',
            value: 'all',
            options: [
                { label: 'All Methods',    value: 'all' },
                { label: 'Cash',           value: 'Cash' },
                { label: 'M-Pesa',         value: 'Mpesa' },
                { label: 'Bank Transfer',  value: 'BankTransfer' },
                { label: 'Cheque',         value: 'Cheque' },
                { label: 'Card',           value: 'Card' },
                { label: 'Online',         value: 'Online' },
            ],
        },
        {
            id: 'status', label: 'Status', type: 'select',
            value: 'all',
            options: [
                { label: 'All Statuses', value: 'all' },
                { label: 'Completed',    value: 'Completed' },
                { label: 'Pending',      value: 'Pending' },
                { label: 'Failed',       value: 'Failed' },
                { label: 'Refunded',     value: 'Refunded' },
                { label: 'Cancelled',    value: 'Cancelled' },
                { label: 'Reversed',     value: 'Reversed' },
            ],
        },
        {
            id: 'isReversal', label: 'Type', type: 'select',
            value: 'all',
            options: [
                { label: 'All',               value: 'all' },
                { label: 'Reversals Only',    value: 'true' },
                { label: 'Exclude Reversals', value: 'false' },
            ],
        },
        { id: 'from', label: 'From Date', type: 'date', value: '' },
        { id: 'to',   label: 'To Date',   type: 'date', value: '' },
    ];

    // ── Lifecycle ─────────────────────────────────────────────────────
    ngOnInit(): void { this.loadData(); }

    ngAfterViewInit(): void {
        this.cellTemplates = {
            reference: this.referenceCellTpl,
            student:   this.studentCellTpl,
            invoice:   this.invoiceCellTpl,
            method:    this.methodCellTpl,
            amount:    this.amountCellTpl,
            date:      this.dateCellTpl,
            status:    this.statusCellTpl,
        };
    }

    ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

    // ── Data loading ──────────────────────────────────────────────────
    // Calls getPaged (not getAll) so the server correctly applies
    // Skip/Take after the in-memory paired sort and returns the right
    // page of items together with the full stats counts.
    private loadData(): void {
        this.isLoading = true;

        this._service.getPaged({
            search:     this._filters.search        || undefined,
            method:     this._filters.paymentMethod !== 'all'
                            ? this._filters.paymentMethod : undefined,
            status:     this._filters.status        !== 'all'
                            ? this._filters.status        : undefined,
            isReversal: this._filters.isReversal    !== 'all'
                            ? this._filters.isReversal === 'true' : undefined,
            from:       this._filters.from          || undefined,
            to:         this._filters.to            || undefined,
            page:       this.currentPage,
            pageSize:   this.itemsPerPage,
        })
        .pipe(takeUntil(this._destroy$), finalize(() => this.isLoading = false))
        .subscribe({
            next: data => {
                this.pagedResult = data;
                this.tableHeader.subtitle =
                    `${data.totalCount.toLocaleString()} payments found`;
            },
            error: err =>
                this._alertService.error(
                    err?.error?.message ?? 'Failed to load payments'),
        });
    }

    reload(): void { this.loadData(); }

    // ── Filter events ─────────────────────────────────────────────────
    toggleFilterPanel(): void { this.showFilterPanel = !this.showFilterPanel; }

    onFilterChange(e: FilterChangeEvent): void {
        (this._filters as any)[e.filterId] = e.value;
        // Reset to page 1 whenever a filter changes
        this.currentPage = 1;
        this.loadData();
    }

    onClearFilters(): void {
        this._filters = {
            search: '', paymentMethod: 'all', status: 'all',
            isReversal: 'all', from: '', to: '',
        };
        this.filterFields.forEach(f => {
            f.value = (this._filters as any)[f.id] ?? '';
        });
        this.currentPage = 1;
        this.loadData();
    }

    // ── Pagination ────────────────────────────────────────────────────
    onPageChange(p: number): void {
        this.currentPage = p;
        this.loadData();
    }

    onItemsPerPageChange(n: number): void {
        this.itemsPerPage = n;
        // Always reset to page 1 when page size changes so we never land
        // on a page that no longer exists with the new page size
        this.currentPage  = 1;
        this.loadData();
    }

    // ── Navigation ────────────────────────────────────────────────────
    createPayment(): void { this._router.navigate(['/finance/payments/create']); }
    createBulk():    void { this._router.navigate(['/finance/payments/bulk']); }

    viewPayment(p: PaymentResponseDto): void {
        this._router.navigate(['/finance/payments/details', p.id]);
    }

    editPayment(p: PaymentResponseDto): void {
        this._router.navigate(['/finance/payments/edit', p.id]);
    }

    // ── Row actions ───────────────────────────────────────────────────
    printReceipt(p: PaymentResponseDto): void {
        if (this.isDownloading) return;
        this.isDownloading = true;
        this._alertService.info('Generating receipt…');
        this._reportService.downloadReceipt(p.id)
            .pipe(takeUntil(this._destroy$), finalize(() => this.isDownloading = false))
            .subscribe({
                next:  r => r.success
                    ? this._alertService.success('Receipt downloaded')
                    : this._alertService.error(r.message ?? 'Error generating receipt'),
                error: err => this._alertService.error(err?.message ?? 'Download failed'),
            });
    }

    reversePayment(p: PaymentResponseDto): void {
        this._alertService.confirm({
            title:       'Reverse Payment',
            message:     `Reverse "${p.paymentReference}" of KES ${this.fmt(p.amount)}?`,
            confirmText: 'Reverse',
            onConfirm:   () => this._router.navigate(['/finance/payments/reverse', p.id]),
        });
    }

    deletePayment(p: PaymentResponseDto): void {
        this._alertService.confirm({
            title:       'Delete Payment',
            message:     `Delete "${p.paymentReference}"? This cannot be undone.`,
            confirmText: 'Delete',
            onConfirm:   () => {
                this._service.delete(p.id)
                    .pipe(takeUntil(this._destroy$))
                    .subscribe({
                        next: () => {
                            this._alertService.success('Payment deleted');
                            this.reload();
                        },
                        error: err =>
                            this._alertService.error(
                                err?.error?.message ?? 'Failed to delete'),
                    });
            },
        });
    }

    // ── Template helpers ──────────────────────────────────────────────
    getMethodLabel = (m: string): string => getPaymentMethodLabel(m);
    getMethodIcon  = (m: string): string => getPaymentMethodIcon(m);

    fmt(amount: number): string {
        return (amount ?? 0).toLocaleString('en-KE', { minimumFractionDigits: 2 });
    }

    // ── Reports ───────────────────────────────────────────────────────
    downloadListReport(): void {
        if (this.isDownloading) return;
        this.isDownloading = true;
        this._alertService.info('Generating report…');
        this._reportService.downloadPaymentsList({
            paymentMethod: this._filters.paymentMethod !== 'all'
                ? this._filters.paymentMethod : null,
            status: this._filters.status !== 'all'
                ? this._filters.status : null,
            from: this._filters.from || null,
            to:   this._filters.to   || null,
        })
        .pipe(takeUntil(this._destroy$), finalize(() => this.isDownloading = false))
        .subscribe({
            next:  r => r.success
                ? this._alertService.success('Report downloaded')
                : this._alertService.error(r.message ?? 'Error generating report'),
            error: err => this._alertService.error(err?.message ?? 'Download failed'),
        });
    }

    downloadSummaryReport(): void {
        if (this.isDownloading) return;
        this.isDownloading = true;
        this._alertService.info('Generating summary…');
        this._reportService.downloadPaymentsSummary({
            from: this._filters.from || null,
            to:   this._filters.to   || null,
        })
        .pipe(takeUntil(this._destroy$), finalize(() => this.isDownloading = false))
        .subscribe({
            next:  r => r.success
                ? this._alertService.success('Summary downloaded')
                : this._alertService.error(r.message ?? 'Error generating summary'),
            error: err => this._alertService.error(err?.message ?? 'Download failed'),
        });
    }
}