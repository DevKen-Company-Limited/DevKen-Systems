import {
    Component,
    OnInit,
    OnDestroy,
    TemplateRef,
    ViewChild,
    ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Subject } from 'rxjs';
import { take, takeUntil } from 'rxjs/operators';

import { DataTableComponent, TableHeader, TableColumn, TableAction, TableEmptyState } from 'app/shared/data-table/data-table.component';
import { FilterPanelComponent, FilterField, FilterChangeEvent } from 'app/shared/Filter/filter-panel.component';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { PaginationComponent } from 'app/shared/pagination/pagination.component';
import { StatsCardsComponent, StatCard } from 'app/shared/stats-cards/stats-cards.component';
import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { AuthService } from 'app/core/auth/auth.service';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { SchoolDto } from 'app/Tenant/types/school';
import { ClassService } from 'app/core/DevKenService/ClassService/ClassService';
import { ClassDto } from 'app/Classes/Types/Class';
import { InvoiceItemDialogData, InvoiceItemResponseDto } from './Types/invoice-items.types';
import { InvoiceItemDialogComponent } from 'app/dialog-modals/Finance/Invoice-item/invoice-item-dialog.component';
import { inject } from '@angular/core';
import { StudentService } from 'app/core/DevKenService/administration/students/StudentService';
import { InvoiceService } from 'app/core/DevKenService/Finance/Invoice/Invoice.service ';
import { InvoiceItemService } from 'app/core/DevKenService/Finance/InvoiceItem/invoice-item.service';

@Component({
    selector: 'app-invoice-items',
    standalone: true,
    imports: [
        CommonModule,
        MatButtonModule,
        MatIconModule,
        PageHeaderComponent,
        StatsCardsComponent,
        FilterPanelComponent,
        DataTableComponent,
        PaginationComponent,
    ],
    templateUrl: './invoice-items.component.html',
})
export class InvoiceItemsComponent implements OnInit, OnDestroy {
    private _destroy$ = new Subject<void>();
    private _authService   = inject(AuthService);
    private _schoolService = inject(SchoolService);
    private _classService    = inject(ClassService);
    private _studentService  = inject(StudentService);
    private _invoiceService  = inject(InvoiceService);

    invoiceId!: string;

    /** invoiceIds that belong to students in the selected class */
    private classInvoiceIds: Set<string> | null = null;
    isLoadingClassFilter = false;

    // ── Auth ──────────────────────────────────────────────────────────────────
    get isSuperAdmin(): boolean {
        return this._authService.authUser?.isSuperAdmin ?? false;
    }

    schools: SchoolDto[] = [];
    classes: ClassDto[]  = [];
    selectedSchoolId: string | null = null;
    selectedClassId:  string | null = null;

    // ── Cell Templates ────────────────────────────────────────────────────────
    @ViewChild('descriptionCell') descriptionCell!: TemplateRef<any>;
    @ViewChild('typeCell')        typeCell!:         TemplateRef<any>;
    @ViewChild('amountCell')      amountCell!:       TemplateRef<any>;
    @ViewChild('discountCell')    discountCell!:     TemplateRef<any>;
    @ViewChild('taxCell')         taxCell!:          TemplateRef<any>;
    @ViewChild('netCell')         netCell!:          TemplateRef<any>;

    // ── Data ──────────────────────────────────────────────────────────────────
    allData:      InvoiceItemResponseDto[] = [];
    filteredData: InvoiceItemResponseDto[] = [];
    isLoading = false;

    // ── Pagination ─────────────────────────────────────────────────────────────
    currentPage  = 1;
    itemsPerPage = 10;

    // ── Filters ────────────────────────────────────────────────────────────────
    showFilters = false;
    private activeFilters: Record<string, any> = {};

    // ── Stats ──────────────────────────────────────────────────────────────────
    statsCards: StatCard[] = [];

    // ── Page Header ────────────────────────────────────────────────────────────
    breadcrumbs: Breadcrumb[] = [
        { label: 'Finance' },
        { label: 'Invoice Items' },
    ];

    tableHeader: TableHeader = {
        title: 'Invoice Line Items',
        subtitle: 'All invoice line items across your school',
        icon: 'receipt_long',
        iconGradient: 'bg-gradient-to-br from-indigo-500 via-violet-600 to-purple-700',
    };

    tableEmptyState: TableEmptyState = {
        icon: 'receipt_long',
        message: 'No line items yet',
        description: 'Add items to this invoice using the button above.',
        action: {
            label: 'Add First Item',
            icon: 'add',
            handler: () => this.hasInvoiceContext ? this.openCreate() : null,
        },
    };

    tableColumns: TableColumn<InvoiceItemResponseDto>[] = [
        { id: 'description', label: 'Description', align: 'left',   sortable: true },
        { id: 'itemType',    label: 'Type',         align: 'center', hideOnMobile: true },
        { id: 'quantity',    label: 'Qty',          align: 'center' },
        { id: 'unitPrice',   label: 'Unit Price',   align: 'right',  hideOnMobile: true },
        { id: 'discount',    label: 'Discount',     align: 'right',  hideOnMobile: true },
        { id: 'taxAmount',   label: 'Tax',          align: 'right',  hideOnMobile: true },
        { id: 'netAmount',   label: 'Net Amount',   align: 'right' },
    ];

    tableActions: TableAction<InvoiceItemResponseDto>[] = [
        { id: 'edit',      label: 'Edit',      icon: 'edit',      color: 'blue',  handler: row => this.openEdit(row) },
        { id: 'recompute', label: 'Recompute', icon: 'calculate', color: 'amber', handler: row => this.recompute(row), divider: true },
        { id: 'delete',    label: 'Delete',    icon: 'delete',    color: 'red',   handler: row => this.confirmDelete(row) },
    ];

    // Base filters — school/class injected dynamically
    filterFields: FilterField[] = [
        {
            id: 'search', label: 'Search', type: 'text',
            placeholder: 'Description, type, GL code…', value: '',
        },
        {
            id: 'taxable', label: 'Taxable', type: 'select', value: 'all',
            options: [
                { label: 'All',         value: 'all'   },
                { label: 'Taxable',     value: 'true'  },
                { label: 'Non-taxable', value: 'false' },
            ],
        },
    ];

    cellTemplates: { [k: string]: TemplateRef<any> } = {};

    constructor(
        private route:        ActivatedRoute,
        private service:      InvoiceItemService,
        private dialog:       MatDialog,
        private alertService: AlertService,
        private cdr:          ChangeDetectorRef,
    ) {}

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    ngOnInit(): void {
        this.invoiceId = this.route.snapshot.paramMap.get('invoiceId')
            ?? this.route.snapshot.queryParamMap.get('invoiceId')
            ?? '';

        if (this.isSuperAdmin) {
            this.loadSchools();
        } else {
            this.loadClasses();
            this.loadAll();
        }
    }

    ngAfterViewInit(): void {
        this.cellTemplates = {
            description: this.descriptionCell,
            itemType:    this.typeCell,
            unitPrice:   this.amountCell,
            discount:    this.discountCell,
            taxAmount:   this.taxCell,
            netAmount:   this.netCell,
        };
        this.cdr.detectChanges();
    }

    ngOnDestroy(): void {
        this._destroy$.next();
        this._destroy$.complete();
    }

    // ── Data ──────────────────────────────────────────────────────────────────

    loadAll(): void {
        if (this.isSuperAdmin && !this.selectedSchoolId) {
            this.isLoading = false;
            return;
        }

        this.isLoading = true;
        const schoolId = this.isSuperAdmin ? this.selectedSchoolId! : undefined;

        this.service.getAll(schoolId, this.invoiceId || undefined)
            .pipe(take(1))
            .subscribe({
                next: res => {
                    this.isLoading = false;
                    if (res.success) {
                        this.allData = res.data ?? [];
                        this.applyFilters();
                        this.buildStats();
                    } else {
                        this.alertService.error('Error', res.message || 'Could not load invoice items.');
                    }
                },
                error: err => {
                    this.isLoading = false;
                    this.alertService.error('Error', err?.error?.message || 'Failed to load invoice items.');
                },
            });
    }

    private loadSchools(): void {
        this._schoolService.getAll().pipe(takeUntil(this._destroy$)).subscribe({
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
            error: () => { this.isLoading = false; },
        });
    }

    private loadClasses(schoolId?: string): void {
        const call = schoolId
            ? this._classService.getAll(schoolId)
            : this._classService.getAll();

        call.pipe(takeUntil(this._destroy$)).subscribe({
            next: (res: any) => {
                this.classes = (res?.data ?? []).filter((c: ClassDto) => c.isActive !== false);
                this.injectClassFilter();
            },
            error: () => { /* non-critical */ },
        });
    }

    // ── Class invoice-ID loader ───────────────────────────────────────────────

    private loadInvoiceIdsForClass(classId: string): void {
        this.isLoadingClassFilter = true;
        this.classInvoiceIds = null;

        const schoolId = this.isSuperAdmin ? this.selectedSchoolId! : undefined;

        // Step 1 — get students, keep only those in the selected class
        this._studentService.getAll(schoolId)
            .pipe(takeUntil(this._destroy$))
            .subscribe({
                next: (res: any) => {
                    const students: any[] = Array.isArray(res) ? res : (res?.data ?? []);

                    const inClass = students.filter(s =>
                        s.currentClassId === classId ||
                        s.classId        === classId ||
                        s.gradeId        === classId ||
                        s.grade?.id      === classId ||
                        s.class?.id      === classId
                    );

                    if (inClass.length === 0) {
                        this.isLoadingClassFilter = false;
                        this.classInvoiceIds = new Set<string>();
                        this.currentPage = 1;
                        this.applyFilters();
                        return;
                    }

                    const studentIdSet = new Set<string>(inClass.map((s: any) => s.id as string));

                    // Step 2 — fetch all invoices, keep only those belonging to class students,
                    // collect their IDs into classInvoiceIds for the applyFilters() matcher
                    this._invoiceService.getAll({}, schoolId)
                        .pipe(takeUntil(this._destroy$))
                        .subscribe({
                            next: (invRes: any) => {
                                this.isLoadingClassFilter = false;
                                const invoices: any[] = Array.isArray(invRes)
                                    ? invRes
                                    : (invRes?.data ?? []);

                                this.classInvoiceIds = new Set<string>(
                                    invoices
                                        .filter((inv: any) => studentIdSet.has(inv.studentId))
                                        .map((inv: any) => inv.id as string)
                                );
                                this.currentPage = 1;
                                this.applyFilters();
                            },
                            error: () => {
                                this.isLoadingClassFilter = false;
                                this.classInvoiceIds = new Set<string>();
                                this.applyFilters();
                            },
                        });
                },
                error: () => {
                    this.isLoadingClassFilter = false;
                    this.classInvoiceIds = new Set<string>();
                    this.applyFilters();
                },
            });
    }

    // ── Dynamic filter injection ───────────────────────────────────────────────

    private injectSchoolFilter(): void {
        const f: FilterField = {
            id: 'schoolId', label: 'School', type: 'select',
            value: this.selectedSchoolId,
            options: this.schools.map(s => ({ label: s.name, value: s.id })),
        };
        this.filterFields = [f, ...this.filterFields.filter(x => x.id !== 'schoolId')];
    }

    private injectClassFilter(): void {
        const f: FilterField = {
            id: 'classId', label: 'Class', type: 'select', value: 'all',
            options: [
                { label: 'All Classes', value: 'all' },
                ...this.classes.map(c => ({ label: c.name, value: c.id })),
            ],
        };
        const schoolIdx = this.filterFields.findIndex(x => x.id === 'schoolId');
        const insertAt  = schoolIdx >= 0 ? schoolIdx + 1 : 0;
        const without   = this.filterFields.filter(x => x.id !== 'classId');
        this.filterFields = [
            ...without.slice(0, insertAt),
            f,
            ...without.slice(insertAt),
        ];
        this.cdr.detectChanges();
    }

    // ── Computed ───────────────────────────────────────────────────────────────

    get paginatedData(): InvoiceItemResponseDto[] {
        const start = (this.currentPage - 1) * this.itemsPerPage;
        return this.filteredData.slice(start, start + this.itemsPerPage);
    }

    // ── Filters ────────────────────────────────────────────────────────────────

    onFilterChange(event: FilterChangeEvent): void {
        switch (event.filterId) {
            case 'schoolId':
                this.selectedSchoolId = event.value;
                this.selectedClassId  = null;
                this.classInvoiceIds        = null;
                this.loadClasses(event.value);
                this.loadAll();
                break;
            case 'classId':
                this.selectedClassId = event.value === 'all' ? null : event.value;
                if (!this.selectedClassId) {
                    this.classInvoiceIds = null;
                    this.currentPage = 1;
                    this.applyFilters();
                    return;
                }
                // Fetch student IDs for the class, then their invoice IDs, filter client-side
                this.loadInvoiceIdsForClass(this.selectedClassId);
                return;
            default:
                this.activeFilters[event.filterId] = event.value;
                this.currentPage = 1;
                this.applyFilters();
        }
    }

    onClearFilters(): void {
        this.activeFilters          = {};
        this.selectedClassId        = null;
        this.classInvoiceIds        = null;
        this.filterFields    = this.filterFields.map(f => ({
            ...f,
            value: f.type === 'select'
                ? (f.id === 'schoolId' ? this.selectedSchoolId : 'all')
                : '',
        }));
        this.currentPage = 1;
        this.applyFilters();
    }

    private applyFilters(): void {
        let data = [...this.allData];

        const search  = ((this.activeFilters['search']  ?? '') as string).toLowerCase().trim();
        const taxable =  (this.activeFilters['taxable'] ?? 'all') as string;
        const classId =  (this.activeFilters['classId'] ?? 'all') as string;

        if (search) {
            data = data.filter(r =>
                r.description.toLowerCase().includes(search) ||
                (r.itemType ?? '').toLowerCase().includes(search) ||
                (r.glCode   ?? '').toLowerCase().includes(search)
            );
        }

        if (taxable !== 'all') {
            data = data.filter(r => r.isTaxable === (taxable === 'true'));
        }

        // Class filter: match items whose invoiceId belongs to a student in the selected class.
        // classInvoiceIds is populated by loadInvoiceIdsForClass() when a class is chosen.
        if (this.classInvoiceIds !== null) {
            data = data.filter(r => this.classInvoiceIds!.has(r.invoiceId));
        }

        this.filteredData = data;
    }

    // ── Pagination ─────────────────────────────────────────────────────────────
    onPageChange(n: number): void         { this.currentPage = n; }
    onItemsPerPageChange(n: number): void { this.itemsPerPage = n; this.currentPage = 1; }

    // ── Stats ──────────────────────────────────────────────────────────────────
    private buildStats(): void {
        const totalNet  = this.allData.reduce((s, i) => s + (i.netAmount  ?? 0), 0);
        const totalTax  = this.allData.reduce((s, i) => s + (i.taxAmount  ?? 0), 0);
        const totalDisc = this.allData.reduce((s, i) => s + (i.discount   ?? 0), 0);

        this.statsCards = [
            { label: 'Line Items',      value: this.allData.length, icon: 'receipt_long',   iconColor: 'indigo' },
            { label: 'Total Net',       value: this.fmt(totalNet),  icon: 'payments',        iconColor: 'green'  },
            { label: 'Total Tax',       value: this.fmt(totalTax),  icon: 'account_balance', iconColor: 'amber'  },
            { label: 'Total Discounts', value: this.fmt(totalDisc), icon: 'discount',        iconColor: 'violet' },
        ];
    }

    // ── CRUD ───────────────────────────────────────────────────────────────────

    get hasInvoiceContext(): boolean { return !!this.invoiceId; }

    openCreate(): void {
        if (!this.hasInvoiceContext) {
            this.alertService.warning('No Invoice Selected', 'Please open this page from an invoice to add items.');
            return;
        }
        const data: InvoiceItemDialogData = { mode: 'create', invoiceId: this.invoiceId };
        this.dialog
            .open(InvoiceItemDialogComponent, { data, width: '720px', panelClass: 'rounded-2xl' })
            .afterClosed().pipe(take(1))
            .subscribe(result => { if (result?.success) this.loadAll(); });
    }

    openEdit(item: InvoiceItemResponseDto): void {
        const data: InvoiceItemDialogData = { mode: 'edit', invoiceId: this.invoiceId, item };
        this.dialog
            .open(InvoiceItemDialogComponent, { data, width: '720px', panelClass: 'rounded-2xl' })
            .afterClosed().pipe(take(1))
            .subscribe(result => { if (result?.success) this.loadAll(); });
    }

    private recompute(item: InvoiceItemResponseDto): void {
        this.service.recompute(item.id).pipe(take(1)).subscribe({
            next: res => {
                if (res.success) {
                    this.alertService.success('Recomputed', `Financials updated for "${item.description}".`);
                    this.loadAll();
                } else {
                    this.alertService.error('Failed', res.message || 'Recompute failed.');
                }
            },
            error: err => this.alertService.error('Error', err?.error?.message || 'Recompute failed.'),
        });
    }

    confirmDelete(item: InvoiceItemResponseDto): void {
        this.alertService.confirm({
            title: 'Delete Item',
            message: `Remove "${item.description}" from this invoice? This cannot be undone.`,
            confirmText: 'Delete',
            onConfirm: () => {
                this.service.delete(item.id).pipe(takeUntil(this._destroy$)).subscribe({
                    next: res => {
                        if (res.success) {
                            this.alertService.success('Deleted', 'Item removed from invoice.');
                            if (this.paginatedData.length === 1 && this.currentPage > 1) this.currentPage--;
                            this.loadAll();
                        } else {
                            this.alertService.error('Failed', res.message || 'Could not delete item.');
                        }
                    },
                    error: err => this.alertService.error('Error', err?.error?.message || 'Failed to delete item.'),
                });
            },
        });
    }

    // ── Helpers ────────────────────────────────────────────────────────────────
    fmt(amount: number): string {
        return new Intl.NumberFormat('en-KE', {
            style: 'currency', currency: 'KES', minimumFractionDigits: 2,
        }).format(amount);
    }
}