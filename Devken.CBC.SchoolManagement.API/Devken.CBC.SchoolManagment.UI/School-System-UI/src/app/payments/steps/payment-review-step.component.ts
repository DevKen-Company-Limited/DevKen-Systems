// ═══════════════════════════════════════════════════════════════════
// payment-review-step.component.ts
// Step 3: Review & confirm all sections before submit
// Mirrors assessment-review-step pattern
// ═══════════════════════════════════════════════════════════════════

import { Component, Input, Output, EventEmitter, forwardRef } from '@angular/core';
import { CommonModule }     from '@angular/common';
import { MatIconModule }    from '@angular/material/icon';
import { MatButtonModule }  from '@angular/material/button';
import { MatCardModule }    from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule }   from '@angular/material/chips';

@Component({
  selector: 'app-payment-review-step',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule, MatButtonModule,
    MatCardModule, MatDividerModule, MatChipsModule,
    forwardRef(() => ReviewFieldComponent)
],
  template: `
<div class="max-w-3xl mx-auto space-y-8">

  <!-- Section header -->
  <div class="flex items-center gap-4">
    <div class="flex items-center justify-center w-12 h-12 rounded-xl shadow-lg
                bg-gradient-to-br from-emerald-500 to-teal-600">
      <mat-icon class="text-white">rate_review</mat-icon>
    </div>
    <div>
      <h2 class="text-xl font-bold text-gray-900 dark:text-white">Review &amp; Confirm</h2>
      <p class="text-sm text-gray-500 dark:text-gray-400">
        Verify all payment details before submitting
      </p>
    </div>
  </div>

  <!-- Steps completion badges -->
  <div class="flex flex-wrap gap-3">
    <span *ngFor="let step of steps; let i = index"
      class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-semibold border"
      [ngClass]="completedSteps.has(i)
        ? 'bg-green-100 text-green-700 border-green-200 dark:bg-green-900/30 dark:text-green-400 dark:border-green-800'
        : 'bg-gray-100 text-gray-500 border-gray-200 dark:bg-gray-800 dark:text-gray-400 dark:border-gray-700'">
      <mat-icon class="icon-size-3.5">{{ completedSteps.has(i) ? 'check_circle' : 'radio_button_unchecked' }}</mat-icon>
      {{ step.label }}
    </span>
  </div>

  <!-- ── Payment Info Review ──────────────────────────────────────── -->
  <mat-card class="shadow-sm border border-emerald-200 dark:border-emerald-800">
    <mat-card-header class="bg-emerald-50 dark:bg-emerald-900/20 !rounded-t-xl !pb-3">
      <div class="flex items-center justify-between w-full">
        <mat-card-title class="!text-sm !font-semibold text-emerald-700 dark:text-emerald-300 flex items-center gap-2">
          <mat-icon class="text-emerald-600 icon-size-4">info</mat-icon>
          Payment Information
        </mat-card-title>
        <button mat-icon-button class="!w-8 !h-8" (click)="editSection.emit(0)"
          matTooltip="Edit this section">
          <mat-icon class="text-emerald-600 icon-size-4">edit</mat-icon>
        </button>
      </div>
    </mat-card-header>
    <mat-card-content class="!pt-4">
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-x-6 gap-y-4">

        <ng-container *ngIf="info.schoolId && isSuperAdmin">
          <div class="sm:col-span-2">
            <app-review-field label="School" [value]="getSchoolName(info.schoolId)"></app-review-field>
          </div>
        </ng-container>

        <app-review-field label="Student"        [value]="getStudentName(info.studentId)"></app-review-field>
        <app-review-field label="Invoice"        [value]="getInvoiceNumber(info.invoiceId)"></app-review-field>
        <app-review-field label="Received By"    [value]="getStaffName(info.receivedBy)"></app-review-field>
        <app-review-field label="Payment Date"   [value]="formatDate(info.paymentDate)"></app-review-field>
        <app-review-field label="Received Date"  [value]="formatDate(info.receivedDate)"></app-review-field>
        <app-review-field label="Description"    [value]="info.description"></app-review-field>

        <div class="sm:col-span-2">
          <div class="grid grid-cols-3 gap-4">

            <!-- Amount -->
            <div class="col-span-3 sm:col-span-1 flex flex-col gap-1
                        p-4 rounded-xl bg-emerald-50 dark:bg-emerald-900/20
                        border border-emerald-200 dark:border-emerald-800">
              <p class="text-xs text-emerald-600 dark:text-emerald-400 font-medium uppercase tracking-wide">Amount</p>
              <p class="text-2xl font-bold text-emerald-700 dark:text-emerald-300">
                KES {{ formatCurrency(info.amount) }}
              </p>
            </div>

            <!-- Method -->
            <div class="flex flex-col gap-1 p-4 rounded-xl bg-gray-50 dark:bg-gray-800/50
                        border border-gray-200 dark:border-gray-700">
              <p class="text-xs text-gray-500 font-medium uppercase tracking-wide">Method</p>
              <span class="inline-flex items-center gap-1.5 mt-1">
                <mat-icon class="icon-size-4 text-gray-600">{{ getMethodIcon(info.paymentMethod) }}</mat-icon>
                <span class="text-sm font-semibold text-gray-800 dark:text-white">
                  {{ getMethodLabel(info.paymentMethod) }}
                </span>
              </span>
            </div>

            <!-- Status -->
            <div class="flex flex-col gap-1 p-4 rounded-xl bg-gray-50 dark:bg-gray-800/50
                        border border-gray-200 dark:border-gray-700">
              <p class="text-xs text-gray-500 font-medium uppercase tracking-wide">Status</p>
              <span class="inline-flex items-center mt-1 px-2.5 py-1 rounded-full text-xs font-bold"
                [ngClass]="getStatusClass(info.statusPayment)">
                {{ info.statusPayment }}
              </span>
            </div>

          </div>
        </div>

        <div *ngIf="info.notes" class="sm:col-span-2">
          <app-review-field label="Notes" [value]="info.notes"></app-review-field>
        </div>

      </div>
    </mat-card-content>
  </mat-card>

  <!-- ── Payment Details Review ──────────────────────────────────── -->
  <mat-card class="shadow-sm border border-blue-200 dark:border-blue-800"
    *ngIf="hasDetails">
    <mat-card-header class="bg-blue-50 dark:bg-blue-900/20 !rounded-t-xl !pb-3">
      <div class="flex items-center justify-between w-full">
        <mat-card-title class="!text-sm !font-semibold text-blue-700 dark:text-blue-300 flex items-center gap-2">
          <mat-icon class="text-blue-600 icon-size-4">tune</mat-icon>
          Payment Details ({{ getMethodLabel(info.paymentMethod) }})
        </mat-card-title>
        <button mat-icon-button class="!w-8 !h-8" (click)="editSection.emit(1)"
          matTooltip="Edit this section">
          <mat-icon class="text-blue-600 icon-size-4">edit</mat-icon>
        </button>
      </div>
    </mat-card-header>
    <mat-card-content class="!pt-4">
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-x-6 gap-y-4">

        <!-- M-Pesa -->
        <ng-container *ngIf="info.paymentMethod === 'Mpesa'">
          <app-review-field label="M-Pesa Code"   [value]="details.mpesaCode"></app-review-field>
          <app-review-field label="Phone Number"  [value]="details.phoneNumber"></app-review-field>
        </ng-container>

        <!-- Bank Transfer -->
        <ng-container *ngIf="info.paymentMethod === 'BankTransfer'">
          <app-review-field label="Bank Name"       [value]="details.bankName"></app-review-field>
          <app-review-field label="Account Number"  [value]="details.accountNumber"></app-review-field>
        </ng-container>

        <!-- Cheque -->
        <ng-container *ngIf="info.paymentMethod === 'Cheque'">
          <app-review-field label="Cheque Number"    [value]="details.chequeNumber"></app-review-field>
          <app-review-field label="Clearance Date"   [value]="formatDate(details.chequeClearanceDate)"></app-review-field>
          <app-review-field label="Bank Name"        [value]="details.bankName"></app-review-field>
          <app-review-field label="Account Number"   [value]="details.accountNumber"></app-review-field>
        </ng-container>

        <ng-container *ngIf="details.transactionReference">
          <div class="sm:col-span-2">
            <app-review-field label="Transaction Reference" [value]="details.transactionReference"></app-review-field>
          </div>
        </ng-container>

      </div>
    </mat-card-content>
  </mat-card>

  <!-- ── Confirmation notice ───────────────────────────────────────── -->
  <div class="flex items-start gap-3 p-4 rounded-xl bg-amber-50 dark:bg-amber-900/20
              border border-amber-200 dark:border-amber-800">
    <mat-icon class="text-amber-600 flex-shrink-0 mt-0.5">info</mat-icon>
    <div>
      <p class="text-sm font-semibold text-amber-800 dark:text-amber-300">Ready to submit?</p>
      <p class="text-xs text-amber-700 dark:text-amber-400 mt-1">
        Once submitted, this payment will be recorded against the selected invoice and
        a receipt will be generated automatically. Completed payments can only be
        reversed — not edited.
      </p>
    </div>
  </div>

</div>
  `,
})
export class PaymentReviewStepComponent {

  @Input() formSections:  Record<string, any> = {};
  @Input() students:      any[] = [];
  @Input() invoices:      any[] = [];
  @Input() staffList:     any[] = [];
  @Input() schools:       any[] = [];
  @Input() steps:         any[] = [];
  @Input() completedSteps = new Set<number>();

  @Output() editSection = new EventEmitter<number>();

  get info():    any { return this.formSections['info']    ?? {}; }
  get details(): any { return this.formSections['details'] ?? {}; }

  get isSuperAdmin(): boolean { return !!this.info.schoolId; }

  get hasDetails(): boolean {
    const m = this.info.paymentMethod;
    return ['Mpesa','BankTransfer','Cheque'].includes(m) ||
      !!this.details.transactionReference;
  }

  // ── Lookup helpers ────────────────────────────────────────────
  getStudentName(id: string): string {
    const s = this.students.find(x => x.id === id);
    return s ? `${s.firstName} ${s.lastName} (${s.admissionNo ?? ''})` : id || '—';
  }

  getInvoiceNumber(id: string): string {
    const inv = this.invoices.find(x => x.id === id);
    return inv?.invoiceNumber ?? id ?? '—';
  }

  getStaffName(id: string): string {
    if (!id) return '—';
    const s = this.staffList.find(x => x.id === id);
    return s ? `${s.firstName} ${s.lastName}` : id;
  }

  getSchoolName(id: string): string {
    if (!id) return '—';
    const s = this.schools.find(x => x.id === id);
    return s?.name ?? id;
  }

  // ── Format helpers ────────────────────────────────────────────
  formatDate(val: string): string {
    if (!val) return '—';
    return new Date(val).toLocaleDateString('en-KE', {
      year: 'numeric', month: 'long', day: 'numeric',
    });
  }

  formatCurrency(val: number): string {
    return (val ?? 0).toLocaleString('en-KE', { minimumFractionDigits: 2 });
  }

  getMethodLabel(method: string): string {
    const map: Record<string, string> = {
      Cash:         'Cash',
      Mpesa:        'M-Pesa',
      BankTransfer: 'Bank Transfer',
      Cheque:       'Cheque',
      Card:         'Card',
      Online:       'Online',
    };
    return map[method] ?? method ?? '—';
  }

  getMethodIcon(method: string): string {
    const map: Record<string, string> = {
      Cash:         'payments',
      Mpesa:        'phone_android',
      BankTransfer: 'account_balance',
      Cheque:       'description',
      Card:         'credit_card',
      Online:       'language',
    };
    return map[method] ?? 'payments';
  }

  getStatusClass(status: string): string {
    const map: Record<string, string> = {
      Completed: 'bg-green-100  text-green-700  dark:bg-green-900/30  dark:text-green-400',
      Pending:   'bg-amber-100  text-amber-700  dark:bg-amber-900/30  dark:text-amber-400',
      Failed:    'bg-red-100    text-red-700    dark:bg-red-900/30    dark:text-red-400',
      Reversed:  'bg-rose-100   text-rose-700   dark:bg-rose-900/30   dark:text-rose-400',
      Partial:   'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400',
    };
    return map[status] ?? 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400';
  }
}

// ── Tiny shared review-field component (inline, no extra file needed) ──

import { Component as NgComponent, Input as NgInput } from '@angular/core';

@NgComponent({
  selector: 'app-review-field',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="flex flex-col gap-0.5">
      <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">{{ label }}</p>
      <p class="text-sm text-gray-900 dark:text-white font-medium">{{ value || '—' }}</p>
    </div>
  `,
})
export class ReviewFieldComponent {
  @NgInput() label = '';
  @NgInput() value: any = '';
}