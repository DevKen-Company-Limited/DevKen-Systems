import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { take } from 'rxjs/operators';

import { InvoiceStatus, InvoiceDialogData } from '../Types/Invoice.types';
import { InvoiceService } from 'app/core/DevKenService/Finance/Invoice/Invoice.service ';
import { PaymentService } from 'app/core/DevKenService/payments/payment.service';
import {
  PaymentResponseDto,
  getPaymentMethodLabel,
  PAYMENT_METHOD_ICONS,
  PAYMENT_STATUS_COLORS,
} from 'app/payments/types/payments';

@Component({
  selector: 'app-invoice-view-dialog',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatDialogModule, MatIconModule, MatButtonModule, MatDividerModule, MatTooltipModule,
    CurrencyPipe, DatePipe,
  ],
  templateUrl: './invoice-view-dialog.component.html',
})
export class InvoiceViewDialogComponent implements OnInit {
  InvoiceStatus = InvoiceStatus;

  // ── Tabs ──────────────────────────────────────────────────────────
  activeTab: 'details' | 'payments' = 'details';

  // ── Discount form ─────────────────────────────────────────────────
  isApplyingDiscount = false;
  isRecalculating    = false;
  discountAmount     = 0;
  showDiscountForm   = false;

  // ── Payments ──────────────────────────────────────────────────────
  payments:         PaymentResponseDto[] = [];
  isLoadingPayments = false;

  get invoice() { return this.data.invoice!; }

  constructor(
    private dialogRef:      MatDialogRef<InvoiceViewDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: InvoiceDialogData,
    private invoiceService: InvoiceService,
    private paymentService: PaymentService,
    private snackBar:       MatSnackBar,
    private router:         Router,
  ) {}

  ngOnInit(): void {
    this.loadPayments();
  }

  close(reload = false): void { this.dialogRef.close(reload); }

  // ── Tabs ──────────────────────────────────────────────────────────
  switchTab(tab: 'details' | 'payments'): void {
    this.activeTab = tab;
  }

  // ── Payments ──────────────────────────────────────────────────────
  loadPayments(): void {
    this.isLoadingPayments = true;
    this.paymentService.getByInvoice(this.invoice.id)
      .pipe(take(1))
      .subscribe({
        next: p => {
          this.payments         = p;
          this.isLoadingPayments = false;
          // Silently recalculate so statusInvoice, amountPaid and balance
          // are always in sync with the actual payments — no snackbar here.
          this.recalculateInvoice({ silent: true });
        },
        error: () => { this.isLoadingPayments = false; },
      });
  }

  /**
   * Calls PATCH /api/finance/invoices/{id}/recalculate.
   *
   * @param options.silent  When true the status snackbar is suppressed.
   *                        Pass false (or omit) for user-initiated recalculates.
   */
  recalculateInvoice(options: { silent?: boolean } = {}): void {
    this.isRecalculating = true;
    this.invoiceService.recalculate(this.invoice.id)
      .pipe(take(1))
      .subscribe({
        next: res => {
          this.isRecalculating = false;
          if (res.success) {
            // Always update the in-memory invoice so the template reflects
            // the correct status, amountPaid, balance, etc.
            Object.assign(this.data.invoice!, res.data);

            // Only show the snackbar when a user explicitly triggered this
            // (e.g. after applying a discount) — not on every dialog open.
            if (!options.silent) {
              this.snackBar.open(
                `Status → ${res.data.statusDisplay}`, 'Close', { duration: 2000 });
            }
          }
        },
        error: () => { this.isRecalculating = false; },
      });
  }

  recordPayment(): void {
    this.dialogRef.close(true);
    this.router.navigate(['/finance/payments/create'], {
      queryParams: {
        invoiceId:  this.invoice.id,
        studentId:  this.invoice.studentId,
        amount:     this.invoice.balance,
      },
    });
  }

  getPaymentMethodIcon(method: string): string {
    return PAYMENT_METHOD_ICONS[method as keyof typeof PAYMENT_METHOD_ICONS] ?? 'payments';
  }

  getPaymentMethodLabel(method: string): string {
    return getPaymentMethodLabel(method);
  }

  getPaymentStatusBadge(status: string): string {
    const colorMap: Record<string, string> = {
      Completed: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400',
      Pending:   'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
      Failed:    'bg-red-100   text-red-700   dark:bg-red-900/30   dark:text-red-400',
      Reversed:  'bg-rose-100  text-rose-700  dark:bg-rose-900/30  dark:text-rose-400',
      Refunded:  'bg-blue-100  text-blue-700  dark:bg-blue-900/30  dark:text-blue-400',
      Cancelled: 'bg-gray-100  text-gray-500  dark:bg-gray-700     dark:text-gray-400',
    };
    return colorMap[status] ?? colorMap['Cancelled'];
  }

  // ── Status badge ──────────────────────────────────────────────────
  getStatusClass(status: InvoiceStatus): string {
    const map: Record<InvoiceStatus, string> = {
      [InvoiceStatus.Draft]:         'bg-gray-100 text-gray-500',
      [InvoiceStatus.Pending]:       'bg-blue-100 text-blue-700',
      [InvoiceStatus.PartiallyPaid]: 'bg-amber-100 text-amber-700',
      [InvoiceStatus.Paid]:          'bg-green-100 text-green-700',
      [InvoiceStatus.Overdue]:       'bg-red-100 text-red-700',
      [InvoiceStatus.Cancelled]:     'bg-gray-200 text-gray-500',
      [InvoiceStatus.Refunded]:      'bg-violet-100 text-violet-700',
    };
    return map[status] ?? 'bg-gray-100 text-gray-600';
  }

  // ── Discount ──────────────────────────────────────────────────────
  applyDiscount(): void {
    if (!this.discountAmount || this.discountAmount <= 0) return;
    this.isApplyingDiscount = true;
    this.invoiceService.applyDiscount(this.invoice.id, { discountAmount: this.discountAmount })
      .pipe(take(1))
      .subscribe({
        next: (res) => {
          this.isApplyingDiscount = false;
          if (res.success) {
            this.snackBar.open('Discount applied.', 'Close', { duration: 2500 });
            Object.assign(this.data.invoice!, res.data);
            this.showDiscountForm = false;
            this.discountAmount   = 0;
            // Status may have changed after discount — recalculate (show snackbar here).
            this.recalculateInvoice({ silent: false });
          }
        },
        error: () => { this.isApplyingDiscount = false; },
      });
  }

  // ── Helpers ───────────────────────────────────────────────────────
  get totalPaymentsCollected(): number {
    return this.payments
      .filter(p => p.statusPayment === 'Completed' && !p.isReversal)
      .reduce((s, p) => s + p.amount, 0);
  }

  get totalReversed(): number {
    return this.payments
      .filter(p => p.isReversal)
      .reduce((s, p) => s + p.amount, 0);
  }
}