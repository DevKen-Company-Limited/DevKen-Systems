// ═══════════════════════════════════════════════════════════════════
// payment-details-step.component.ts
// Step 2: Method-specific fields (M-Pesa / Bank / Cheque / etc.)
// Mirrors assessment-details-step pattern
// ═══════════════════════════════════════════════════════════════════

import {
  Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges,
} from '@angular/core';
import { CommonModule }        from '@angular/common';
import { FormsModule }         from '@angular/forms';
import { MatFormFieldModule }  from '@angular/material/form-field';
import { MatInputModule }      from '@angular/material/input';
import { MatIconModule }       from '@angular/material/icon';
import { MatCardModule }       from '@angular/material/card';
import { MatDividerModule }    from '@angular/material/divider';

@Component({
  selector: 'app-payment-details-step',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatFormFieldModule, MatInputModule,
    MatIconModule, MatCardModule, MatDividerModule,
  ],
  template: `
<div class="max-w-3xl mx-auto space-y-8">

  <!-- Section header -->
  <div class="flex items-center gap-4">
    <div class="flex items-center justify-center w-12 h-12 rounded-xl shadow-lg"
      [ngClass]="headerGradient">
      <mat-icon class="text-white">{{ headerIcon }}</mat-icon>
    </div>
    <div>
      <h2 class="text-xl font-bold text-gray-900 dark:text-white">Payment Details</h2>
      <p class="text-sm text-gray-500 dark:text-gray-400">{{ headerSubtitle }}</p>
    </div>
  </div>

  <!-- ── NO extra fields needed (Cash / Card / Online) ────────────── -->
  <ng-container *ngIf="isCash || isCard || isOnline">
    <mat-card class="shadow-sm border border-gray-200 dark:border-gray-700">
      <mat-card-content class="!py-6">
        <div class="flex flex-col items-center gap-4 text-center py-4">
          <div class="flex items-center justify-center w-16 h-16 rounded-2xl shadow-lg"
            [ngClass]="headerGradient">
            <mat-icon class="text-white text-4xl">{{ headerIcon }}</mat-icon>
          </div>
          <div>
            <p class="text-base font-semibold text-gray-900 dark:text-white">
              {{ paymentMethod }} payment selected
            </p>
            <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
              No additional details required for this payment method.
            </p>
          </div>
        </div>

        <!-- Transaction reference (all methods) -->
        <mat-form-field appearance="outline" class="w-full mt-4">
          <mat-label>Transaction Reference</mat-label>
          <input matInput [(ngModel)]="form.transactionReference" maxlength="100"
            placeholder="Optional reference / receipt number"
            (ngModelChange)="onFieldChange()" />
          <mat-icon matPrefix class="text-gray-400">tag</mat-icon>
          <mat-hint>Optional reference for this transaction</mat-hint>
        </mat-form-field>

      </mat-card-content>
    </mat-card>
  </ng-container>

  <!-- ── M-PESA Fields ─────────────────────────────────────────────── -->
  <ng-container *ngIf="isMpesa">
    <mat-card class="shadow-sm border border-green-200 dark:border-green-800">
      <mat-card-header class="bg-green-50 dark:bg-green-900/20 !rounded-t-xl !pb-3">
        <mat-card-title class="!text-sm !font-semibold text-green-700 dark:text-green-300 flex items-center gap-2">
          <mat-icon class="text-green-600 icon-size-4">phone_android</mat-icon>
          M-Pesa Details
        </mat-card-title>
      </mat-card-header>
      <mat-card-content class="!pt-4">
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>M-Pesa Code</mat-label>
            <input matInput [(ngModel)]="form.mpesaCode" maxlength="20"
              placeholder="e.g. QA12BX3456" (ngModelChange)="onFieldChange()" />
            <mat-icon matPrefix class="text-gray-400">confirmation_number</mat-icon>
            <mat-hint>M-Pesa transaction code</mat-hint>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Phone Number</mat-label>
            <input matInput [(ngModel)]="form.phoneNumber" maxlength="20"
              placeholder="e.g. 0712345678" (ngModelChange)="onFieldChange()" />
            <mat-icon matPrefix class="text-gray-400">phone</mat-icon>
            <mat-hint>Sender's phone number</mat-hint>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full sm:col-span-2">
            <mat-label>Transaction Reference</mat-label>
            <input matInput [(ngModel)]="form.transactionReference" maxlength="100"
              placeholder="Optional additional reference" (ngModelChange)="onFieldChange()" />
            <mat-icon matPrefix class="text-gray-400">tag</mat-icon>
          </mat-form-field>

        </div>
      </mat-card-content>
    </mat-card>
  </ng-container>

  <!-- ── BANK TRANSFER Fields ─────────────────────────────────────── -->
  <ng-container *ngIf="isBankTransfer">
    <mat-card class="shadow-sm border border-blue-200 dark:border-blue-800">
      <mat-card-header class="bg-blue-50 dark:bg-blue-900/20 !rounded-t-xl !pb-3">
        <mat-card-title class="!text-sm !font-semibold text-blue-700 dark:text-blue-300 flex items-center gap-2">
          <mat-icon class="text-blue-600 icon-size-4">account_balance</mat-icon>
          Bank Transfer Details
        </mat-card-title>
      </mat-card-header>
      <mat-card-content class="!pt-4">
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Bank Name</mat-label>
            <input matInput [(ngModel)]="form.bankName" maxlength="100"
              placeholder="e.g. Equity Bank" (ngModelChange)="onFieldChange()" />
            <mat-icon matPrefix class="text-gray-400">account_balance</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Account Number</mat-label>
            <input matInput [(ngModel)]="form.accountNumber" maxlength="50"
              placeholder="e.g. 0123456789" (ngModelChange)="onFieldChange()" />
            <mat-icon matPrefix class="text-gray-400">credit_card</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full sm:col-span-2">
            <mat-label>Transaction Reference</mat-label>
            <input matInput [(ngModel)]="form.transactionReference" maxlength="100"
              placeholder="Bank reference / slip number" (ngModelChange)="onFieldChange()" />
            <mat-icon matPrefix class="text-gray-400">tag</mat-icon>
          </mat-form-field>

        </div>
      </mat-card-content>
    </mat-card>
  </ng-container>

  <!-- ── CHEQUE Fields ─────────────────────────────────────────────── -->
  <ng-container *ngIf="isCheque">
    <mat-card class="shadow-sm border border-amber-200 dark:border-amber-800">
      <mat-card-header class="bg-amber-50 dark:bg-amber-900/20 !rounded-t-xl !pb-3">
        <mat-card-title class="!text-sm !font-semibold text-amber-700 dark:text-amber-300 flex items-center gap-2">
          <mat-icon class="text-amber-600 icon-size-4">description</mat-icon>
          Cheque Details
        </mat-card-title>
      </mat-card-header>
      <mat-card-content class="!pt-4">
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Cheque Number</mat-label>
            <input matInput [(ngModel)]="form.chequeNumber" maxlength="50"
              placeholder="e.g. CHQ-00123" (ngModelChange)="onFieldChange()" />
            <mat-icon matPrefix class="text-gray-400">confirmation_number</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Clearance Date</mat-label>
            <input matInput type="date" [(ngModel)]="form.chequeClearanceDate"
              (ngModelChange)="onFieldChange()" />
            <mat-icon matPrefix class="text-gray-400">event</mat-icon>
            <mat-hint>Expected cheque clearance date</mat-hint>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Bank Name</mat-label>
            <input matInput [(ngModel)]="form.bankName" maxlength="100"
              placeholder="Bank that issued the cheque" (ngModelChange)="onFieldChange()" />
            <mat-icon matPrefix class="text-gray-400">account_balance</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Account Number</mat-label>
            <input matInput [(ngModel)]="form.accountNumber" maxlength="50"
              placeholder="Account on the cheque" (ngModelChange)="onFieldChange()" />
            <mat-icon matPrefix class="text-gray-400">credit_card</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full sm:col-span-2">
            <mat-label>Transaction Reference</mat-label>
            <input matInput [(ngModel)]="form.transactionReference" maxlength="100"
              placeholder="Optional reference" (ngModelChange)="onFieldChange()" />
            <mat-icon matPrefix class="text-gray-400">tag</mat-icon>
          </mat-form-field>

        </div>
      </mat-card-content>
    </mat-card>
  </ng-container>

</div>
  `,
})
export class PaymentDetailsStepComponent implements OnInit, OnChanges {

  @Input() formData:      any    = {};
  @Input() paymentMethod: string = 'Cash';
  @Input() isEditMode     = false;

  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid   = new EventEmitter<boolean>();

  form: any = {
    mpesaCode:            '',
    phoneNumber:          '',
    bankName:             '',
    accountNumber:        '',
    chequeNumber:         '',
    chequeClearanceDate:  '',
    transactionReference: '',
  };

  // ── Method flags ──────────────────────────────────────────────
  get isMpesa():        boolean { return this.paymentMethod === 'Mpesa';        }
  get isBankTransfer(): boolean { return this.paymentMethod === 'BankTransfer'; }
  get isCheque():       boolean { return this.paymentMethod === 'Cheque';       }
  get isCash():         boolean { return this.paymentMethod === 'Cash';         }
  get isCard():         boolean { return this.paymentMethod === 'Card';         }
  get isOnline():       boolean { return this.paymentMethod === 'Online';       }

  // ── Header config per method ──────────────────────────────────
  get headerGradient(): string {
    const map: Record<string, string> = {
      Cash:         'bg-gradient-to-br from-emerald-500 to-emerald-700',
      Mpesa:        'bg-gradient-to-br from-green-500 to-green-700',
      BankTransfer: 'bg-gradient-to-br from-blue-500 to-blue-700',
      Cheque:       'bg-gradient-to-br from-amber-500 to-amber-700',
      Card:         'bg-gradient-to-br from-violet-500 to-violet-700',
      Online:       'bg-gradient-to-br from-indigo-500 to-indigo-700',
    };
    return map[this.paymentMethod] ?? 'bg-gradient-to-br from-gray-500 to-gray-700';
  }

  get headerIcon(): string {
    const map: Record<string, string> = {
      Cash:         'payments',
      Mpesa:        'phone_android',
      BankTransfer: 'account_balance',
      Cheque:       'description',
      Card:         'credit_card',
      Online:       'language',
    };
    return map[this.paymentMethod] ?? 'payments';
  }

  get headerSubtitle(): string {
    const map: Record<string, string> = {
      Cash:         'No extra details required for cash',
      Mpesa:        'Enter M-Pesa transaction code and phone number',
      BankTransfer: 'Enter bank name, account number and reference',
      Cheque:       'Enter cheque number, bank and clearance date',
      Card:         'No extra details required for card payment',
      Online:       'Enter any online payment reference',
    };
    return map[this.paymentMethod] ?? 'Enter payment details';
  }

  ngOnInit(): void {
    if (this.formData && Object.keys(this.formData).length) {
      this.form = { ...this.form, ...this.formData };
    }
    this.formValid.emit(true); // details step is always valid
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['formData'] && this.formData) {
      this.form = { ...this.form, ...this.formData };
    }
    if (changes['paymentMethod']) {
      // Reset method-specific fields when method changes
      this.form = {
        ...this.form,
        mpesaCode:            '',
        phoneNumber:          '',
        bankName:             '',
        accountNumber:        '',
        chequeNumber:         '',
        chequeClearanceDate:  '',
      };
    }
  }

  onFieldChange(): void {
    this.formChanged.emit({ ...this.form });
    this.formValid.emit(true);
  }
}