// ═══════════════════════════════════════════════════════════════════
// payment-details.component.ts
// Read-only details page for a single payment
// ═══════════════════════════════════════════════════════════════════

import {
  Component, OnInit, OnDestroy,
} from '@angular/core';
import { CommonModule }           from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { MatButtonModule }        from '@angular/material/button';
import { MatIconModule }          from '@angular/material/icon';
import { MatDividerModule }       from '@angular/material/divider';
import { MatTooltipModule }       from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Subject }                from 'rxjs';
import { takeUntil }              from 'rxjs/operators';

import { PaymentService }      from 'app/core/DevKenService/payments/payment.service';
import { AlertService }        from 'app/core/DevKenService/Alert/AlertService';
import { AuthService }         from 'app/core/auth/auth.service';
import { PaymentResponseDto }  from '../types/payments';

@Component({
  selector: 'app-payment-details',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule, MatIconModule, MatDividerModule,
    MatTooltipModule, MatProgressSpinnerModule,
  ],
  template: `
<!-- ── Loading ─────────────────────────────────────────────────────── -->
<div *ngIf="isLoading" class="flex items-center justify-center h-96">
  <mat-spinner diameter="48"></mat-spinner>
</div>

<!-- ── Not Found ───────────────────────────────────────────────────── -->
<div *ngIf="!isLoading && !payment"
  class="flex flex-col items-center justify-center h-96 gap-4">
  <div class="w-16 h-16 rounded-2xl bg-red-50 dark:bg-red-900/20
              flex items-center justify-center">
    <mat-icon class="text-red-400 !text-3xl">error_outline</mat-icon>
  </div>
  <p class="text-gray-500 dark:text-gray-400">Payment not found.</p>
  <button mat-stroked-button (click)="goBack()">Go Back</button>
</div>

<!-- ── Main ────────────────────────────────────────────────────────── -->
<div *ngIf="!isLoading && payment"
  class="absolute inset-0 flex min-w-0 flex-col overflow-y-auto">

  <!-- Header bar -->
  <div class="bg-gradient-to-r from-emerald-600 via-teal-600 to-green-700
              px-6 py-8 sm:px-10">
    <div class="max-w-5xl mx-auto">

      <!-- Back + actions -->
      <div class="flex items-center justify-between mb-6">
        <button mat-stroked-button
          class="!border-white/30 !text-white hover:!bg-white/10"
          (click)="goBack()">
          <mat-icon>arrow_back</mat-icon>
          <span class="ml-1">Back</span>
        </button>

        <div class="flex gap-2">
          <!-- Edit (only non-reversals that aren't Completed) -->
          <button *ngIf="!payment.isReversal && payment.statusPayment !== 'Completed'"
            mat-stroked-button
            class="!border-white/30 !text-white hover:!bg-white/10"
            (click)="editPayment()">
            <mat-icon>edit</mat-icon>
            <span class="ml-1">Edit</span>
          </button>

          <!-- Reverse (Completed, non-reversal) -->
          <button *ngIf="!payment.isReversal && payment.statusPayment === 'Completed'"
            mat-stroked-button
            class="!border-rose-300/60 !text-rose-100 hover:!bg-rose-500/20"
            (click)="reversePayment()">
            <mat-icon>undo</mat-icon>
            <span class="ml-1">Reverse</span>
          </button>

          <!-- Print receipt -->
          <button mat-flat-button
            class="!bg-white !text-emerald-700 hover:!bg-emerald-50 shadow-lg font-bold"
            (click)="printReceipt()"
            [disabled]="isPrinting">
            <mat-icon>receipt</mat-icon>
            <span class="ml-1">{{ isPrinting ? 'Printing…' : 'Print Receipt' }}</span>
          </button>
        </div>
      </div>

      <!-- Title row -->
      <div class="flex items-start gap-5">
        <div class="flex items-center justify-center w-16 h-16 rounded-2xl
                    bg-white/20 backdrop-blur-sm shadow-inner flex-shrink-0">
          <mat-icon class="text-white !text-3xl">payments</mat-icon>
        </div>
        <div>
          <div class="flex items-center gap-3 flex-wrap">
            <h1 class="text-2xl font-bold text-white">
              {{ payment.paymentReference }}
            </h1>
            <!-- Reversal badge -->
            <span *ngIf="payment.isReversal"
              class="px-2.5 py-0.5 rounded-lg text-xs font-bold
                     bg-rose-500/80 text-white border border-rose-300/40">
              REVERSAL
            </span>
            <!-- Status badge -->
            <span class="px-3 py-1 rounded-full text-xs font-bold border"
              [ngClass]="statusBadgeClass(payment.statusPayment)">
              {{ payment.statusPayment }}
            </span>
          </div>
          <p class="text-white/70 text-sm mt-1">
            {{ payment.receiptNumber ? 'Receipt: ' + payment.receiptNumber : 'No receipt number' }}
            &nbsp;·&nbsp;
            Created {{ payment.createdOn | date:'mediumDate' }}
          </p>
        </div>
      </div>

    </div>
  </div>

  <!-- Body -->
  <div class="bg-card -mt-4 rounded-t-2xl flex-auto px-6 py-8 sm:px-10">
    <div class="max-w-5xl mx-auto space-y-8">

      <!-- ── Amount hero card ───────────────────────────────────── -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">

        <div class="sm:col-span-1 flex flex-col items-center justify-center
                    rounded-2xl border-2 p-6 text-center"
          [ngClass]="payment.isReversal
            ? 'border-rose-200 bg-rose-50 dark:border-rose-800 dark:bg-rose-900/10'
            : 'border-emerald-200 bg-emerald-50 dark:border-emerald-800 dark:bg-emerald-900/10'">
          <p class="text-xs font-semibold uppercase tracking-widest mb-1"
            [ngClass]="payment.isReversal
              ? 'text-rose-500' : 'text-emerald-600 dark:text-emerald-400'">
            {{ payment.isReversal ? 'Reversal Amount' : 'Amount Paid' }}
          </p>
          <p class="text-3xl font-extrabold"
            [ngClass]="payment.isReversal
              ? 'text-rose-600 dark:text-rose-400'
              : 'text-emerald-700 dark:text-emerald-300'">
            KES {{ formatCurrency(payment.amount) }}
          </p>
          <div class="mt-3 flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold"
            [ngClass]="methodBadgeClass(payment.paymentMethod)">
            <mat-icon class="icon-size-3.5">{{ getMethodIcon(payment.paymentMethod) }}</mat-icon>
            {{ getMethodLabel(payment.paymentMethod) }}
          </div>
        </div>

        <div class="sm:col-span-2 grid grid-cols-2 gap-4">
          <div class="rounded-xl border border-gray-200 dark:border-gray-700
                      bg-gray-50 dark:bg-gray-800/50 p-4">
            <p class="text-xs text-gray-500 dark:text-gray-400 mb-1">Student</p>
            <p class="font-semibold text-gray-900 dark:text-white truncate">
              {{ payment.studentName || '—' }}
            </p>
            <p class="text-xs text-gray-400 dark:text-gray-500 truncate">
              {{ payment.admissionNumber || 'No Adm. No.' }}
            </p>
          </div>

          <div class="rounded-xl border border-gray-200 dark:border-gray-700
                      bg-gray-50 dark:bg-gray-800/50 p-4">
            <p class="text-xs text-gray-500 dark:text-gray-400 mb-1">Invoice</p>
            <p class="font-semibold text-gray-900 dark:text-white truncate">
              {{ payment.invoiceNumber || '—' }}
            </p>
            <p class="text-xs text-gray-400 dark:text-gray-500 truncate">
              ID: {{ payment.invoiceId | slice:0:8 }}…
            </p>
          </div>

          <div class="rounded-xl border border-gray-200 dark:border-gray-700
                      bg-gray-50 dark:bg-gray-800/50 p-4">
            <p class="text-xs text-gray-500 dark:text-gray-400 mb-1">Payment Date</p>
            <p class="font-semibold text-gray-900 dark:text-white">
              {{ payment.paymentDate | date:'mediumDate' }}
            </p>
            <p *ngIf="payment.receivedDate" class="text-xs text-gray-400 dark:text-gray-500">
              Received: {{ payment.receivedDate | date:'mediumDate' }}
            </p>
          </div>

          <div class="rounded-xl border border-gray-200 dark:border-gray-700
                      bg-gray-50 dark:bg-gray-800/50 p-4">
            <p class="text-xs text-gray-500 dark:text-gray-400 mb-1">Received By</p>
            <p class="font-semibold text-gray-900 dark:text-white truncate">
              {{ payment.receivedByName || '—' }}
            </p>
            <p class="text-xs text-gray-400 dark:text-gray-500">Staff</p>
          </div>
        </div>

      </div>

      <!-- ── Transaction Details ─────────────────────────────────── -->
      <div class="rounded-2xl border border-blue-200 dark:border-blue-800 overflow-hidden">
        <div class="bg-blue-50 dark:bg-blue-900/20 px-6 py-4 flex items-center gap-3">
          <mat-icon class="text-blue-600 dark:text-blue-400">receipt_long</mat-icon>
          <h3 class="font-semibold text-blue-700 dark:text-blue-300">Transaction Details</h3>
        </div>
        <div class="p-6 grid grid-cols-1 sm:grid-cols-2 gap-x-8 gap-y-4">

          <ng-container *ngIf="payment.transactionReference">
            <app-detail-row label="Transaction Reference"
              [value]="payment.transactionReference" icon="tag"></app-detail-row>
          </ng-container>

          <!-- M-Pesa specific -->
          <ng-container *ngIf="payment.isMpesa">
            <div class="sm:col-span-2">
              <mat-divider class="!my-2"></mat-divider>
              <p class="text-xs font-semibold text-green-600 dark:text-green-400
                         uppercase tracking-widest mb-3 mt-2">M-Pesa Details</p>
            </div>
            <ng-container *ngIf="payment.mpesaCode">
              <div class="flex flex-col gap-1">
                <p class="text-xs text-gray-500 dark:text-gray-400">M-Pesa Code</p>
                <p class="font-mono font-semibold text-gray-900 dark:text-white
                           bg-green-50 dark:bg-green-900/20 px-3 py-1.5 rounded-lg
                           border border-green-200 dark:border-green-800 text-sm inline-block">
                  {{ payment.mpesaCode }}
                </p>
              </div>
            </ng-container>
            <ng-container *ngIf="payment.phoneNumber">
              <div class="flex flex-col gap-1">
                <p class="text-xs text-gray-500 dark:text-gray-400">Phone Number</p>
                <p class="font-semibold text-gray-900 dark:text-white">{{ payment.phoneNumber }}</p>
              </div>
            </ng-container>
          </ng-container>

          <!-- Bank / Cheque specific -->
          <ng-container *ngIf="payment.paymentMethod === 'BankTransfer' || payment.paymentMethod === 'Cheque'">
            <div class="sm:col-span-2">
              <mat-divider class="!my-2"></mat-divider>
              <p class="text-xs font-semibold text-blue-600 dark:text-blue-400
                         uppercase tracking-widest mb-3 mt-2">Banking Details</p>
            </div>
            <ng-container *ngIf="payment.bankName">
              <div class="flex flex-col gap-1">
                <p class="text-xs text-gray-500 dark:text-gray-400">Bank Name</p>
                <p class="font-semibold text-gray-900 dark:text-white">{{ payment.bankName }}</p>
              </div>
            </ng-container>
            <ng-container *ngIf="payment.accountNumber">
              <div class="flex flex-col gap-1">
                <p class="text-xs text-gray-500 dark:text-gray-400">Account Number</p>
                <p class="font-mono font-semibold text-gray-900 dark:text-white">{{ payment.accountNumber }}</p>
              </div>
            </ng-container>
            <ng-container *ngIf="payment.chequeNumber">
              <div class="flex flex-col gap-1">
                <p class="text-xs text-gray-500 dark:text-gray-400">Cheque Number</p>
                <p class="font-mono font-semibold text-gray-900 dark:text-white">{{ payment.chequeNumber }}</p>
              </div>
            </ng-container>
            <ng-container *ngIf="payment.chequeClearanceDate">
              <div class="flex flex-col gap-1">
                <p class="text-xs text-gray-500 dark:text-gray-400">Clearance Date</p>
                <p class="font-semibold text-gray-900 dark:text-white">
                  {{ payment.chequeClearanceDate | date:'mediumDate' }}
                </p>
              </div>
            </ng-container>
          </ng-container>

        </div>
      </div>

      <!-- ── Reversal Info (if this is a reversal) ──────────────── -->
      <div *ngIf="payment.isReversal"
        class="rounded-2xl border border-rose-200 dark:border-rose-800 overflow-hidden">
        <div class="bg-rose-50 dark:bg-rose-900/20 px-6 py-4 flex items-center gap-3">
          <mat-icon class="text-rose-600 dark:text-rose-400">undo</mat-icon>
          <h3 class="font-semibold text-rose-700 dark:text-rose-300">Reversal Information</h3>
        </div>
        <div class="p-6 grid grid-cols-1 sm:grid-cols-2 gap-x-8 gap-y-4">
          <div *ngIf="payment.reversedFromPaymentId" class="flex flex-col gap-1">
            <p class="text-xs text-gray-500 dark:text-gray-400">Reversed From</p>
            <button class="text-left font-mono text-sm text-blue-600 dark:text-blue-400
                           hover:underline cursor-pointer"
              (click)="viewOriginalPayment(payment.reversedFromPaymentId!)">
              {{ payment.reversedFromPaymentId | slice:0:8 }}…
              <mat-icon class="icon-size-3.5 ml-0.5">open_in_new</mat-icon>
            </button>
          </div>
          <div *ngIf="payment.reversalReason" class="sm:col-span-2 flex flex-col gap-1">
            <p class="text-xs text-gray-500 dark:text-gray-400">Reversal Reason</p>
            <p class="text-gray-900 dark:text-white bg-rose-50 dark:bg-rose-900/20
                       px-4 py-3 rounded-xl border border-rose-200 dark:border-rose-800 text-sm">
              {{ payment.reversalReason }}
            </p>
          </div>
        </div>
      </div>

      <!-- ── Notes & Description ─────────────────────────────────── -->
      <div *ngIf="payment.description || payment.notes"
        class="rounded-2xl border border-gray-200 dark:border-gray-700 overflow-hidden">
        <div class="bg-gray-50 dark:bg-gray-800/50 px-6 py-4 flex items-center gap-3">
          <mat-icon class="text-gray-500 dark:text-gray-400">notes</mat-icon>
          <h3 class="font-semibold text-gray-700 dark:text-gray-300">Notes</h3>
        </div>
        <div class="p-6 space-y-4">
          <div *ngIf="payment.description">
            <p class="text-xs text-gray-500 dark:text-gray-400 mb-1">Description</p>
            <p class="text-gray-700 dark:text-gray-300 text-sm">{{ payment.description }}</p>
          </div>
          <mat-divider *ngIf="payment.description && payment.notes"></mat-divider>
          <div *ngIf="payment.notes">
            <p class="text-xs text-gray-500 dark:text-gray-400 mb-1">Internal Notes</p>
            <p class="text-gray-700 dark:text-gray-300 text-sm whitespace-pre-line">{{ payment.notes }}</p>
          </div>
        </div>
      </div>

      <!-- ── Audit trail ─────────────────────────────────────────── -->
      <div class="rounded-2xl border border-gray-200 dark:border-gray-700 overflow-hidden">
        <div class="bg-gray-50 dark:bg-gray-800/50 px-6 py-4 flex items-center gap-3">
          <mat-icon class="text-gray-500 dark:text-gray-400">history</mat-icon>
          <h3 class="font-semibold text-gray-700 dark:text-gray-300">Audit Trail</h3>
        </div>
        <div class="p-6 grid grid-cols-2 sm:grid-cols-4 gap-4 text-sm">
          <div>
            <p class="text-xs text-gray-400 dark:text-gray-500 mb-0.5">Created</p>
            <p class="font-medium text-gray-700 dark:text-gray-300">
              {{ payment.createdOn | date:'mediumDate' }}
            </p>
          </div>
          <div>
            <p class="text-xs text-gray-400 dark:text-gray-500 mb-0.5">Created By</p>
            <p class="font-medium text-gray-700 dark:text-gray-300 truncate">
              {{ payment.createdBy || '—' }}
            </p>
          </div>
          <div>
            <p class="text-xs text-gray-400 dark:text-gray-500 mb-0.5">Last Updated</p>
            <p class="font-medium text-gray-700 dark:text-gray-300">
              {{ payment.updatedOn | date:'mediumDate' }}
            </p>
          </div>
          <div>
            <p class="text-xs text-gray-400 dark:text-gray-500 mb-0.5">Updated By</p>
            <p class="font-medium text-gray-700 dark:text-gray-300 truncate">
              {{ payment.updatedBy || '—' }}
            </p>
          </div>
        </div>
      </div>

    </div>
  </div>
</div>
  `,
})
export class PaymentDetailsComponent implements OnInit, OnDestroy {

  payment:   PaymentResponseDto | null = null;
  isLoading  = true;
  isPrinting = false;

  private _destroy$ = new Subject<void>();

  constructor(
    private _service:      PaymentService,
    private _router:       Router,
    private _route:        ActivatedRoute,
    private _alertService: AlertService,
    private _authService:  AuthService,
  ) {}

  get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }

  ngOnInit(): void {
    const id = this._route.snapshot.paramMap.get('id');
    if (!id) { this._alertService.error('Invalid payment ID.'); this.goBack(); return; }

    this._service.getById(id)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next:  p    => { this.payment = p; this.isLoading = false; },
        error: err  => {
          this._alertService.error(err?.error?.message ?? 'Could not load payment.');
          this.isLoading = false;
        },
      });
  }

  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  // ── Navigation ──────────────────────────────────────────────────
  goBack(): void { this._router.navigate(['/finance/payments']); }

  editPayment(): void {
    if (this.payment) this._router.navigate(['/finance/payments/edit', this.payment.id]);
  }

  reversePayment(): void {
    if (this.payment) this._router.navigate(['/finance/payments/reverse', this.payment.id]);
  }

  viewOriginalPayment(id: string): void {
    this._router.navigate(['/finance/payments/details', id]);
  }

  printReceipt(): void {
    if (!this.payment || this.isPrinting) return;
    this._alertService.info('Receipt printing is not yet implemented.');
  }

  // ── Formatters ──────────────────────────────────────────────────
  formatCurrency(val: number): string {
    return (val ?? 0).toLocaleString('en-KE', { minimumFractionDigits: 2 });
  }

  getMethodLabel(method: string): string {
    const map: Record<string, string> = {
      Cash: 'Cash', Mpesa: 'M-Pesa', BankTransfer: 'Bank Transfer',
      Cheque: 'Cheque', Card: 'Card', Online: 'Online',
    };
    return map[method] ?? method ?? 'Unknown';
  }

  getMethodIcon(method: string): string {
    const map: Record<string, string> = {
      Cash: 'payments', Mpesa: 'phone_android', BankTransfer: 'account_balance',
      Cheque: 'description', Card: 'credit_card', Online: 'language',
    };
    return map[method] ?? 'payments';
  }

  statusBadgeClass(status: string): Record<string, boolean> {
    return {
      'bg-green-500/20 text-green-100 border-green-400/40':  status === 'Completed',
      'bg-amber-500/20 text-amber-100 border-amber-400/40':  status === 'Pending',
      'bg-red-500/20   text-red-100   border-red-400/40':    status === 'Failed',
      'bg-blue-500/20  text-blue-100  border-blue-400/40':   status === 'Refunded',
      'bg-gray-500/20  text-gray-100  border-gray-400/40':   status === 'Cancelled',
      'bg-rose-500/20  text-rose-100  border-rose-400/40':   status === 'Reversed',
    };
  }

  methodBadgeClass(method: string): Record<string, boolean> {
    return {
      'bg-emerald-100 text-emerald-700 border-emerald-200 dark:bg-emerald-900/30 dark:text-emerald-300': method === 'Cash',
      'bg-green-100   text-green-700   border-green-200   dark:bg-green-900/30   dark:text-green-300':   method === 'Mpesa',
      'bg-blue-100    text-blue-700    border-blue-200    dark:bg-blue-900/30    dark:text-blue-300':    method === 'BankTransfer',
      'bg-amber-100   text-amber-700   border-amber-200   dark:bg-amber-900/30   dark:text-amber-300':   method === 'Cheque',
      'bg-violet-100  text-violet-700  border-violet-200  dark:bg-violet-900/30  dark:text-violet-300':  method === 'Card',
      'bg-indigo-100  text-indigo-700  border-indigo-200  dark:bg-indigo-900/30  dark:text-indigo-300':  method === 'Online',
    };
  }
}