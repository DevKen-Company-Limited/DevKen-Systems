// ═══════════════════════════════════════════════════════════════════
// payment-bulk-form.component.ts
// Universal Payment Entry — handles ALL payment scenarios:
//   • Single student  →  Add one row manually
//   • Multiple students  →  Add rows manually or import by class
//   • Whole class / school  →  Use "Import by Class" panel
// Student search: ngx-mat-select-search
//
// Install dependency once:
//   npm install ngx-mat-select-search
// ═══════════════════════════════════════════════════════════════════

import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormControl } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { NgxMatSelectSearchModule } from 'ngx-mat-select-search';
import { Subject, forkJoin, of } from 'rxjs';
import { takeUntil, catchError, finalize } from 'rxjs/operators';

import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { AuthService } from 'app/core/auth/auth.service';
import { PaymentService } from 'app/core/DevKenService/payments/payment.service';
import {
    BulkPaymentDto,
    BulkPaymentItemDto,
    BulkPaymentResultDto,
    PaymentMethod,
    PaymentStatus,
    PaymentMethodValue,
    PaymentStatusValue,
} from '../types/payments';
import { PesaPalDialogComponent } from '../pesaPall/dialog/pesapal-dialog.component';
import { PesaPalDialogData, PesaPalDialogResult } from '../pesaPall/pesapal.types';

// ── Auto-invoice strategy ──────────────────────────────────────────
export type InvoiceStrategy = 'none' | 'first' | 'highest' | 'lowest';

// ── Auto-invoice strategy ──────────────────────────────────────────
//type InvoiceStrategy = 'none' | 'first' | 'highest' | 'lowest';

// ── Row model ─────────────────────────────────────────────────────
export interface BulkRow {
    studentId: string;
    invoiceId: string;
    amount: number | null;
    mpesaCode?: string;
    phoneNumber?: string;
    transactionReference?: string;
    notes?: string;
    // UI-only
    _studentName: string;
    _studentSearchCtrl: FormControl;
    _filteredStudents: any[];
    _invoices: any[];
    _loadingInvoices: boolean;
    _error?: string;
}

// ── Importable student ─────────────────────────────────────────────
export interface ImportStudent {
    id: string;
    firstName: string;
    lastName: string;
    admissionNo?: string;
    admissionNumber?: string;
    cbcLevel?: string;
    currentLevel?: string;
    grade?: string;
    currentGrade?: string;
    gradeLevel?: string;
    stream?: string;
    className?: string;
    selected: boolean;
}

@Component({
    selector: 'app-payment-bulk-form',
    standalone: true,
    templateUrl: './payment-bulk-form.component.html',
    styleUrls: ['./payment-bulk-form.component.scss'],
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        MatButtonModule,
        MatIconModule,
        MatProgressSpinnerModule,
        MatTooltipModule,
        MatFormFieldModule,
        MatInputModule,
        MatSelectModule,
        MatCardModule,
        MatDividerModule,
        MatCheckboxModule,
        NgxMatSelectSearchModule,
        MatDialogModule,
    ],
})
export class PaymentBulkFormComponent implements OnInit, OnDestroy {

    private _destroy$ = new Subject<void>();

    // ── Shared settings ───────────────────────────────────────────
    shared = {
        schoolId: '',
        paymentDate: new Date().toISOString().split('T')[0],
        paymentMethod: 'Cash' as PaymentMethod,
        statusPayment: 'Completed' as PaymentStatus,
        receivedBy: '',
        description: '',
        bankName: '',
        accountNumber: '',
        invoiceStrategy: 'first' as InvoiceStrategy,
    };

    rows: BulkRow[] = [];
    importStudents: ImportStudent[] = [];
    staffList: any[] = [];
    schools: any[] = [];

    showHelp = false;
    showClassImport = false;
    sidebarCollapsed = false;
    isImporting = false;
    allClassSelected = false;
    someClassSelected = false;
    classImportAmount: number | null = null;
    classFilter = { cbcLevel: '', grade: '', stream: '', search: '' };

    isLoadingLookups = false;
    isSubmitting = false;
    result: BulkPaymentResultDto | null = null;

    readonly helpTips = [
        {
            icon: 'auto_fix_high', color: '#0d9488',
            title: 'Invoice Strategy',
            body: 'Set "First unpaid", "Highest" or "Lowest balance" in Shared Settings. Invoices are auto-picked for every student on import.',
        },
        {
            icon: 'content_copy', color: '#4f46e5',
            title: 'Copy invoice to all rows',
            body: 'Once a row has an invoice, click the copy icon in its header to push that invoice to every other row.',
        },
        {
            icon: 'search', color: '#059669',
            title: 'Student search',
            body: "In each row's Student dropdown, type a name or admission number — the list filters live.",
        },
        {
            icon: 'attach_money', color: '#d97706',
            title: 'Default amount on import',
            body: 'Set a Default Amount (KES) in the Import panel before importing. All rows will be pre-filled.',
        },
        {
            icon: 'refresh', color: '#dc2626',
            title: 'Retry failed payments',
            body: 'After submission, if some rows fail, click Retry Failed to reload only those rows.',
        },
        {
            icon: 'circle', color: '#94a3b8',
            title: 'Row status dots',
            body: 'Grey = not started, Amber = in progress, Green = complete, Red = error.',
        },
    ];

    readonly paymentMethods = [
        { value: 'Cash',         label: 'Cash',          icon: 'payments' },
        { value: 'Mpesa',        label: 'M-Pesa',        icon: 'phone_android' },
        { value: 'BankTransfer', label: 'Bank Transfer',  icon: 'account_balance' },
        { value: 'Cheque',       label: 'Cheque',         icon: 'description' },
        { value: 'Card',         label: 'Card',           icon: 'credit_card' },
        { value: 'Online',       label: 'Online',         icon: 'language' },
    ];

    constructor(
        private _service: PaymentService,
        private _router: Router,
        private _alertService: AlertService,
        private _authService: AuthService,
        private _dialog: MatDialog,
    ) {}

    // ── Auth ──────────────────────────────────────────────────────
    get isSuperAdmin(): boolean {
        return this._authService.authUser?.isSuperAdmin ?? false;
    }

    // ── Computed ──────────────────────────────────────────────────
    get validRowCount(): number {
        return this.rows.filter(r => this.isRowComplete(r)).length;
    }

    get totalAmount(): number {
        return this.rows.reduce((s, r) => s + (r.amount ? +r.amount : 0), 0);
    }

    get isValid(): boolean {
        return (
            !!this.shared.paymentDate &&
            !!this.shared.paymentMethod &&
            this.rows.length > 0 &&
            this.validRowCount === this.rows.length &&
            (!this.isSuperAdmin || !!this.shared.schoolId)
        );
    }

    get validationHint(): string {
        if (this.isSuperAdmin && !this.shared.schoolId) return 'Select a school first.';
        if (!this.shared.paymentDate) return 'Set a payment date.';
        const n = this.rows.length - this.validRowCount;
        if (n > 0) return `${n} row${n > 1 ? 's are' : ' is'} incomplete.`;
        return '';
    }

    get strategyHint(): string {
        switch (this.shared.invoiceStrategy) {
            case 'first':   return 'Auto-selects the earliest unpaid invoice per student.';
            case 'highest': return 'Auto-selects the invoice with the highest balance due.';
            case 'lowest':  return 'Auto-selects the invoice with the lowest balance due.';
            default:        return "Each row's invoice must be chosen manually.";
        }
    }

    isRowComplete(row: BulkRow): boolean {
        const mpesaOk = this.shared.paymentMethod !== 'Mpesa' || !!row.mpesaCode?.trim();
        return !!row.studentId && !!row.invoiceId && !!row.amount && +row.amount > 0 && mpesaOk;
    }

    // ── Class filter derived lists ────────────────────────────────
    get cbcLevels(): string[] {
        return [
            ...new Set(
                this.importStudents
                    .map(s => s.cbcLevel || s.currentLevel)
                    .filter(Boolean) as string[]
            ),
        ].sort();
    }

    get availableGrades(): string[] {
        return [
            ...new Set(
                this.importStudents
                    .filter(
                        s =>
                            !this.classFilter.cbcLevel ||
                            (s.cbcLevel || s.currentLevel) === this.classFilter.cbcLevel
                    )
                    .map(s => s.grade || s.currentGrade || s.gradeLevel)
                    .filter(Boolean) as string[]
            ),
        ].sort();
    }

    get availableStreams(): string[] {
        return [
            ...new Set(
                this.importStudents
                    .filter(
                        s =>
                            (!this.classFilter.cbcLevel ||
                                (s.cbcLevel || s.currentLevel) === this.classFilter.cbcLevel) &&
                            (!this.classFilter.grade ||
                                (s.grade || s.currentGrade || s.gradeLevel) === this.classFilter.grade)
                    )
                    .map(s => s.stream || s.className)
                    .filter(Boolean) as string[]
            ),
        ].sort();
    }

    get filteredClassStudents(): ImportStudent[] {
        const q = this.classFilter.search.toLowerCase();
        return this.importStudents.filter(s => {
            const level  = s.cbcLevel   || s.currentLevel                  || '';
            const grade  = s.grade      || s.currentGrade || s.gradeLevel  || '';
            const stream = s.stream     || s.className                     || '';
            const name   = `${s.firstName} ${s.lastName}`.toLowerCase();
            const adm    = (s.admissionNo || s.admissionNumber || '').toLowerCase();
            return (
                (!this.classFilter.cbcLevel || level  === this.classFilter.cbcLevel) &&
                (!this.classFilter.grade   || grade  === this.classFilter.grade) &&
                (!this.classFilter.stream  || stream === this.classFilter.stream) &&
                (!q || name.includes(q) || adm.includes(q))
            );
        });
    }

    get newSelectionCount(): number {
        return this.filteredClassStudents.filter(s => s.selected && !this.isAlreadyAdded(s.id)).length;
    }

    get alreadyAddedCount(): number {
        return this.filteredClassStudents.filter(s => s.selected && this.isAlreadyAdded(s.id)).length;
    }

    get hasActiveClassFilters(): boolean {
        return !!(
            this.classFilter.cbcLevel ||
            this.classFilter.grade ||
            this.classFilter.stream ||
            this.classFilter.search
        );
    }

    isAlreadyAdded(id: string): boolean {
        return this.rows.some(r => r.studentId === id);
    }

    // ── Lifecycle ─────────────────────────────────────────────────
    ngOnInit(): void {
        this.isLoadingLookups = true;
        forkJoin({
            students:  this._service.getStudents().pipe(catchError(() => of([]))),
            staffList: this._service.getStaff().pipe(catchError(() => of([]))),
            schools:   this._service.getSchools().pipe(catchError(() => of([]))),
        })
            .pipe(takeUntil(this._destroy$), finalize(() => (this.isLoadingLookups = false)))
            .subscribe(d => {
                this.importStudents = (d.students as any[]).map(s => ({ ...s, selected: false }));
                this.staffList      = d.staffList;
                this.schools        = d.schools;
            });
    }

    ngOnDestroy(): void {
        this._destroy$.next();
        this._destroy$.complete();
    }

    // ── School change ─────────────────────────────────────────────
    onSchoolChange(schoolId: string): void {
        if (!schoolId) return;
        forkJoin({
            students:  this._service.getStudents(schoolId).pipe(catchError(() => of([]))),
            staffList: this._service.getStaff(schoolId).pipe(catchError(() => of([]))),
        })
            .pipe(takeUntil(this._destroy$))
            .subscribe(d => {
                this.importStudents = (d.students as any[]).map(s => ({ ...s, selected: false }));
                this.staffList      = d.staffList;
                this.rows.forEach(r => {
                    r.studentId           = '';
                    r._studentName        = '';
                    r.invoiceId           = '';
                    r._invoices           = [];
                    r._filteredStudents   = [...this.importStudents];
                    r._studentSearchCtrl.setValue('', { emitEvent: false });
                });
            });
    }

    onMethodChange(): void {
        if (this.shared.paymentMethod !== 'Mpesa') {
            this.rows.forEach(r => {
                r.mpesaCode   = '';
                r.phoneNumber = '';
            });
        }
    }

    // ── Invoice strategy ──────────────────────────────────────────
    onStrategyChange(): void {
        if (this.shared.invoiceStrategy === 'none' || this.rows.length === 0) return;
        this.applyStrategyToAllRows();
    }

    applyStrategyToAllRows(): void {
        let changed = 0;
        this.rows.forEach(row => {
            if (!row._invoices.length) return;
            const picked = this.pickInvoiceByStrategy(row._invoices);
            if (picked && picked !== row.invoiceId) {
                row.invoiceId = picked;
                changed++;
            }
        });
        if (changed > 0) {
            this._alertService.success(
                `Invoice strategy applied — ${changed} row${changed !== 1 ? 's' : ''} updated.`
            );
        }
    }

    private pickInvoiceByStrategy(invoices: any[]): string {
        if (!invoices.length) return '';
        switch (this.shared.invoiceStrategy) {
            case 'first':
                return invoices[0].id;
            case 'highest': {
                const best = invoices.reduce((a, b) =>
                    (b.balanceDue ?? b.totalAmount) > (a.balanceDue ?? a.totalAmount) ? b : a
                );
                return best.id;
            }
            case 'lowest': {
                const best = invoices.reduce((a, b) =>
                    (b.balanceDue ?? b.totalAmount) < (a.balanceDue ?? a.totalAmount) ? b : a
                );
                return best.id;
            }
            default:
                return invoices.length === 1 ? invoices[0].id : '';
        }
    }

    // ── Apply one row's invoice to all ────────────────────────────
    applyInvoiceToAll(sourceRow: BulkRow): void {
        const sourceInv = sourceRow._invoices.find(x => x.id === sourceRow.invoiceId);
        if (!sourceInv) return;
        let changed = 0;
        this.rows.forEach(row => {
            if (row === sourceRow || !row._invoices.length) return;
            const match =
                row._invoices.find((x: any) => x.id === sourceInv.id) ??
                row._invoices.find((x: any) => x.invoiceNumber === sourceInv.invoiceNumber);
            if (match && match.id !== row.invoiceId) {
                row.invoiceId = match.id;
                changed++;
            }
        });
        const msg =
            changed > 0
                ? `Invoice "${sourceInv.invoiceNumber}" applied to ${changed} other row${changed !== 1 ? 's' : ''}.`
                : `No other rows have a matching invoice for "${sourceInv.invoiceNumber}".`;
        changed > 0 ? this._alertService.success(msg) : this._alertService.info(msg);
    }

    // ── Class import ──────────────────────────────────────────────
    toggleClassPanel(): void {
        this.showClassImport = !this.showClassImport;
    }

    onClassFilterChange(changed: 'cbcLevel' | 'grade' | 'stream' | 'search'): void {
        if (changed === 'cbcLevel') {
            this.classFilter.grade  = '';
            this.classFilter.stream = '';
        } else if (changed === 'grade') {
            this.classFilter.stream = '';
        }
        const visibleIds = new Set(this.filteredClassStudents.map(s => s.id));
        this.importStudents.forEach(s => {
            if (!visibleIds.has(s.id)) s.selected = false;
        });
        this.updateSelectAllState();
    }

    clearClassFilters(): void {
        this.classFilter = { cbcLevel: '', grade: '', stream: '', search: '' };
        this.updateSelectAllState();
    }

    toggleStudent(s: ImportStudent): void {
        s.selected = !s.selected;
        this.updateSelectAllState();
    }

    toggleSelectAll(checked: boolean): void {
        this.filteredClassStudents.forEach(s => (s.selected = checked));
        this.updateSelectAllState();
    }

    updateSelectAllState(): void {
        const f     = this.filteredClassStudents;
        const count = f.filter(s => s.selected).length;
        this.allClassSelected  = f.length > 0 && count === f.length;
        this.someClassSelected = count > 0 && count < f.length;
    }

    importSelectedStudents(): void {
        const toImport = this.filteredClassStudents.filter(
            s => s.selected && !this.isAlreadyAdded(s.id)
        );
        if (toImport.length === 0) {
            this._alertService.info('All selected students are already in the list.');
            return;
        }

        this.isImporting = true;
        forkJoin(
            toImport.map(s =>
                this._service
                    .getInvoicesByStudent(s.id, this.shared.schoolId || undefined)
                    .pipe(catchError(() => of([])))
            )
        )
            .pipe(takeUntil(this._destroy$), finalize(() => (this.isImporting = false)))
            .subscribe((invoicesPerStudent: any[]) => {
                toImport.forEach((student, idx) => {
                    const invoices    = invoicesPerStudent[idx] as any[];
                    const autoInvoice =
                        invoices.length === 0
                            ? ''
                            : invoices.length === 1
                            ? invoices[0].id
                            : this.pickInvoiceByStrategy(invoices);

                    this.rows.push(
                        this._makeRow({
                            studentId:    student.id,
                            invoiceId:    autoInvoice,
                            amount:       this.classImportAmount ?? null,
                            _studentName: `${student.firstName} ${student.lastName}`,
                            _invoices:    invoices,
                        })
                    );
                });

                toImport.forEach(s => (s.selected = false));
                this.updateSelectAllState();
                this._alertService.success(
                    `${toImport.length} student${toImport.length !== 1 ? 's' : ''} imported successfully.`
                );
                this.showClassImport = false;
            });
    }

    // ── Row management ────────────────────────────────────────────
    private _makeRow(overrides: Partial<BulkRow> = {}): BulkRow {
        const ctrl = new FormControl<string>('');

        const row: BulkRow = {
            studentId:           '',
            invoiceId:           '',
            amount:              null,
            mpesaCode:           '',
            phoneNumber:         '',
            transactionReference:'',
            notes:               '',
            _studentName:        '',
            _studentSearchCtrl:  ctrl,
            _filteredStudents:   [...this.importStudents],
            _invoices:           [],
            _loadingInvoices:    false,
            ...overrides,
        };

        ctrl.valueChanges.pipe(takeUntil(this._destroy$)).subscribe(q => {
            const term = (q ?? '').toLowerCase().trim();
            row._filteredStudents = term
                ? this.importStudents.filter(
                      s =>
                          `${s.firstName} ${s.lastName}`.toLowerCase().includes(term) ||
                          (s.admissionNo || s.admissionNumber || '').toLowerCase().includes(term)
                  )
                : [...this.importStudents];
        });

        return row;
    }

    addRow():            void { this.rows.push(this._makeRow()); }
    removeRow(i: number):void { this.rows.splice(i, 1); }

    clearAll(): void {
        this._alertService.confirm({
            title:       'Clear All Rows',
            message:     'Remove all payment rows?',
            confirmText: 'Clear All',
            onConfirm:   () => { this.rows = []; this.result = null; },
        });
    }

    trackByIndex(i: number):            number { return i; }
    trackById(_: number, s: ImportStudent): string { return s.id; }
    trackStudentById(_: number, s: any):    string { return s.id; }

    // ── Row handlers ──────────────────────────────────────────────
    onStudentSelectOpened(row: BulkRow, isOpen: boolean): void {
        if (isOpen) row._studentSearchCtrl.setValue('', { emitEvent: true });
    }

    onRowStudentChange(row: BulkRow, studentId: string): void {
        row.invoiceId    = '';
        row._invoices    = [];
        row._error       = undefined;
        row._studentName = '';
        if (!studentId) return;

        const found = this.importStudents.find(s => s.id === studentId);
        if (found) row._studentName = `${found.firstName} ${found.lastName}`;

        row._loadingInvoices = true;
        this._service
            .getInvoicesByStudent(studentId, this.shared.schoolId || undefined)
            .pipe(takeUntil(this._destroy$), finalize(() => (row._loadingInvoices = false)))
            .subscribe({
                next: inv => {
                    row._invoices = inv;
                    row.invoiceId =
                        inv.length === 1 ? inv[0].id : this.pickInvoiceByStrategy(inv);
                },
                error: () => { row._error = 'Failed to load invoices'; },
            });
    }

    getInvoiceLabel(row: BulkRow): string {
        if (!row.invoiceId) return '';
        const inv = row._invoices.find(x => x.id === row.invoiceId);
        return inv
            ? `${inv.invoiceNumber} — KES ${this.formatCurrency(inv.balanceDue ?? inv.totalAmount)}`
            : '';
    }

    getStudentClass(s: ImportStudent): string {
        return [
            s.cbcLevel  || s.currentLevel,
            s.grade     || s.currentGrade || s.gradeLevel,
            s.stream    || s.className,
        ]
            .filter(Boolean)
            .join(' · ');
    }

    getInitials(s: ImportStudent): string {
        return `${(s.firstName || '')[0] ?? ''}${(s.lastName || '')[0] ?? ''}`.toUpperCase();
    }

    // ── Submit ────────────────────────────────────────────────────
    submit(): void {
        if (!this.isValid) return;
        this.isSubmitting = true;
        this.result       = null;

        const dto: BulkPaymentDto = {
            tenantId:      this.shared.schoolId  || undefined,
            paymentDate:   this.shared.paymentDate,
            paymentMethod: PaymentMethodValue[this.shared.paymentMethod],
            statusPayment: PaymentStatusValue[this.shared.statusPayment],
            receivedBy:    this.shared.receivedBy    || undefined,
            description:   this.shared.description   || undefined,
            bankName:      this.shared.bankName       || undefined,
            accountNumber: this.shared.accountNumber  || undefined,
            payments: this.rows.map(
                r =>
                    ({
                        studentId:            r.studentId,
                        invoiceId:            r.invoiceId,
                        amount:               +r.amount!,
                        mpesaCode:            r.mpesaCode           || undefined,
                        phoneNumber:          r.phoneNumber          || undefined,
                        transactionReference: r.transactionReference || undefined,
                        notes:                r.notes               || undefined,
                    } as BulkPaymentItemDto)
            ),
        };

        this._service
            .bulkCreate(dto)
            .pipe(takeUntil(this._destroy$), finalize(() => (this.isSubmitting = false)))
            .subscribe({
                next: res => {
                    this.result = res;
                    res.failed === 0
                        ? this._alertService.success(`All ${res.succeeded} payments processed!`)
                        : this._alertService.error(`${res.succeeded} succeeded, ${res.failed} failed.`);
                },
                error: err =>
                    this._alertService.error(err?.error?.message ?? 'Bulk submission failed'),
            });
    }

    retryFailed(): void {
        if (!this.result?.errors?.length) return;
        const keys = new Set(this.result.errors.map(e => `${e.studentId}|${e.invoiceId}`));
        this.rows = this.rows.filter(r => keys.has(`${r.studentId}|${r.invoiceId}`));
        this.rows.forEach(r => (r._error = undefined));
        this.result = null;
    }

    // ── Helpers ───────────────────────────────────────────────────
    getMethodIcon(m: string): string {
        return (
            ({
                Cash:         'payments',
                Mpesa:        'phone_android',
                BankTransfer: 'account_balance',
                Cheque:       'description',
                Card:         'credit_card',
                Online:       'language',
            } as Record<string, string>)[m] ?? 'payments'
        );
    }

    getMethodLabel(m: string): string {
        return (
            ({
                Cash:         'Cash',
                Mpesa:        'M-Pesa',
                BankTransfer: 'Bank Transfer',
                Cheque:       'Cheque',
                Card:         'Card',
                Online:       'Online',
            } as Record<string, string>)[m] ?? m
        );
    }

    formatCurrency(v: number): string {
        return (v ?? 0).toLocaleString('en-KE', { minimumFractionDigits: 2 });
    }

    // ── PesaPal checkout ──────────────────────────────────────────
    openPesaPal(): void {
        // Build a description from rows, cap at 100 chars
        const rowSummary =
            this.rows
                .map(r => r._studentName || 'Student')
                .slice(0, 3)
                .join(', ') +
            (this.rows.length > 3 ? ` +${this.rows.length - 3} more` : '');

        // ── Pre-populate billing from rows ────────────────────────
        // For a single student payment, use that student's details.
        // For bulk, use the first row as the billing contact.
        const firstRow     = this.rows[0];
        const firstStudent = firstRow
            ? this.importStudents.find(s => s.id === firstRow.studentId)
            : null;

        // Derive first/last name from the ImportStudent record if available,
        // otherwise split the display name string.
        const nameParts = (firstRow?._studentName ?? '').trim().split(/\s+/);
        const firstName = firstStudent?.firstName ?? nameParts[0]             ?? '';
        const lastName  = firstStudent?.lastName  ?? nameParts.slice(1).join(' ') ?? '';

        // Pre-fill phone from the row's phoneNumber field (set when M-Pesa is selected)
        const phone = firstRow?.phoneNumber?.trim() ?? '';

        const data: PesaPalDialogData = {
            // Required fields
            amount:      this.totalAmount,
            description: `Payment Entry — ${rowSummary}`.substring(0, 100),
            reference:   `BULK-${Date.now()}`,

            // Pre-populated from first student row — user can edit inside the dialog
            firstName,
            lastName,
            email:  '',          // user fills in the dialog
            phone,

            // Optional context
            merchantReference: `BULK-${Date.now()}`,
            schoolId:          this.shared.schoolId || undefined,
        };

        const ref = this._dialog.open(PesaPalDialogComponent, {
            data,
            width:        '520px',
            maxWidth:     '96vw',
            maxHeight:    '92vh',
            panelClass:   'pesapal-dialog-panel',
            disableClose: false,
        });

        ref.afterClosed().subscribe((result: PesaPalDialogResult | undefined) => {
            if (!result) return;
            if (result.success) {
                this._alertService.success(
                    `PesaPal payment confirmed! Code: ${result.confirmationCode ?? result.orderTrackingId}`
                );
            } else if (result.error && result.error !== 'Cancelled') {
                this._alertService.error(`PesaPal: ${result.error}`);
            }
        });
    }

    goBack(): void {
        this._router.navigate(['/finance/payments']);
    }
}