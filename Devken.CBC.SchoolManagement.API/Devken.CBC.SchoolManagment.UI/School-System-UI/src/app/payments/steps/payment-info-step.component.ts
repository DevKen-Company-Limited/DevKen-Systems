// ═══════════════════════════════════════════════════════════════════
// payment-info-step.component.ts
// Step 1: Student, Invoice, Amount, Date, Method, Status
// Mirrors assessment-info-step pattern
// ═══════════════════════════════════════════════════════════════════

import {
  Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges,
} from '@angular/core';
import { CommonModule }        from '@angular/common';
import { FormsModule }         from '@angular/forms';
import { MatFormFieldModule }  from '@angular/material/form-field';
import { MatInputModule }      from '@angular/material/input';
import { MatSelectModule }     from '@angular/material/select';
import { MatIconModule }       from '@angular/material/icon';
import { MatCardModule }       from '@angular/material/card';
import { MatDividerModule }    from '@angular/material/divider';
import { MatTooltipModule }    from '@angular/material/tooltip';

@Component({
  selector: 'app-payment-info-step',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatIconModule, MatCardModule, MatDividerModule, MatTooltipModule,
  ],
  template: `
<div class="max-w-3xl mx-auto space-y-8">

  <!-- Section header -->
  <div class="flex items-center gap-4">
    <div class="flex items-center justify-center w-12 h-12 rounded-xl shadow-lg
                bg-gradient-to-br from-emerald-500 to-teal-600">
      <mat-icon class="text-white">info</mat-icon>
    </div>
    <div>
      <h2 class="text-xl font-bold text-gray-900 dark:text-white">Payment Information</h2>
      <p class="text-sm text-gray-500 dark:text-gray-400">Student, invoice and payment basics</p>
    </div>
  </div>

  <!-- ── SuperAdmin school selector ──────────────────────────────── -->
  <mat-card *ngIf="isSuperAdmin" class="shadow-sm border border-violet-200 dark:border-violet-800">
    <mat-card-header class="bg-violet-50 dark:bg-violet-900/20 !rounded-t-xl !pb-3">
      <mat-card-title class="!text-sm !font-semibold text-violet-700 dark:text-violet-300 flex items-center gap-2">
        <mat-icon class="text-violet-600 icon-size-4">corporate_fare</mat-icon>
        School (SuperAdmin)
      </mat-card-title>
    </mat-card-header>
    <mat-card-content class="!pt-4">
      <mat-form-field appearance="outline" class="w-full">
        <mat-label>Select School <span class="text-red-500">*</span></mat-label>
        <mat-select [(ngModel)]="form.schoolId" (ngModelChange)="onSchoolSelect($event)">
          <mat-option value="">— Select school —</mat-option>
          <mat-option *ngFor="let s of schools" [value]="s.id">{{ s.name }}</mat-option>
        </mat-select>
        <mat-icon matPrefix class="text-gray-400">school</mat-icon>
      </mat-form-field>
    </mat-card-content>
  </mat-card>

  <!-- ── Student & Invoice ────────────────────────────────────────── -->
  <mat-card class="shadow-sm border border-emerald-200 dark:border-emerald-800">
    <mat-card-header class="bg-emerald-50 dark:bg-emerald-900/20 !rounded-t-xl !pb-3">
      <mat-card-title class="!text-sm !font-semibold text-emerald-700 dark:text-emerald-300 flex items-center gap-2">
        <mat-icon class="text-emerald-600 icon-size-4">person</mat-icon>
        Student &amp; Invoice
      </mat-card-title>
    </mat-card-header>
    <mat-card-content class="!pt-4">
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">

        <!-- Student -->
        <mat-form-field appearance="outline" class="w-full sm:col-span-2">
          <mat-label>Student <span class="text-red-500">*</span></mat-label>
          <mat-select [(ngModel)]="form.studentId" (ngModelChange)="onStudentSelect($event)">
            <mat-option disabled class="!h-auto !px-0 !py-0">
              <div class="px-3 py-2 sticky top-0 bg-white dark:bg-gray-800 z-10">
                <input
                  class="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm outline-none
                         focus:ring-2 focus:ring-emerald-500 bg-white dark:bg-gray-700
                         text-gray-900 dark:text-white"
                  placeholder="Search students…"
                  (keydown.Space)="$event.stopPropagation()"
                  [(ngModel)]="studentSearch"
                  (ngModelChange)="filterStudents()" />
              </div>
            </mat-option>
            <mat-option value="">— Select student —</mat-option>
            <mat-option *ngFor="let s of filteredStudents" [value]="s.id">
              {{ s.firstName }} {{ s.lastName }}
              <span class="text-xs text-gray-400 ml-2">({{ s.admissionNo }})</span>
            </mat-option>
          </mat-select>
          <mat-icon matPrefix class="text-gray-400">person_search</mat-icon>
          <mat-hint>Select a student first to load their invoices</mat-hint>
        </mat-form-field>

        <!-- Invoice -->
        <mat-form-field appearance="outline" class="w-full sm:col-span-2">
          <mat-label>Invoice <span class="text-red-500">*</span></mat-label>
          <mat-select [(ngModel)]="form.invoiceId" (ngModelChange)="onFieldChange()">
            <mat-option value="">— Select invoice —</mat-option>
            <mat-option *ngFor="let inv of invoices" [value]="inv.id">
              {{ inv.invoiceNumber }}
              <span class="text-xs text-gray-400 ml-2">
                — Balance: KES {{ formatCurrency(inv.balanceDue ?? inv.totalAmount) }}
              </span>
            </mat-option>
          </mat-select>
          <mat-icon matPrefix class="text-gray-400">receipt_long</mat-icon>
          <mat-hint *ngIf="!form.studentId">Select a student first</mat-hint>
          <mat-hint *ngIf="form.studentId && invoices.length === 0">No open invoices found</mat-hint>
        </mat-form-field>

        <!-- Received By -->
        <mat-form-field appearance="outline" class="w-full sm:col-span-2">
          <mat-label>Received By</mat-label>
          <mat-select [(ngModel)]="form.receivedBy" (ngModelChange)="onFieldChange()">
            <mat-option value="">— Optional —</mat-option>
            <mat-option *ngFor="let staff of staffList" [value]="staff.id">
              {{ staff.firstName }} {{ staff.lastName }}
            </mat-option>
          </mat-select>
          <mat-icon matPrefix class="text-gray-400">badge</mat-icon>
        </mat-form-field>

      </div>
    </mat-card-content>
  </mat-card>

  <!-- ── Amount, Method & Status ──────────────────────────────────── -->
  <mat-card class="shadow-sm border border-blue-200 dark:border-blue-800">
    <mat-card-header class="bg-blue-50 dark:bg-blue-900/20 !rounded-t-xl !pb-3">
      <mat-card-title class="!text-sm !font-semibold text-blue-700 dark:text-blue-300 flex items-center gap-2">
        <mat-icon class="text-blue-600 icon-size-4">payments</mat-icon>
        Amount &amp; Method
      </mat-card-title>
    </mat-card-header>
    <mat-card-content class="!pt-4">
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">

        <!-- Amount -->
        <mat-form-field appearance="outline" class="w-full sm:col-span-2">
          <mat-label>Amount (KES) <span class="text-red-500">*</span></mat-label>
          <input matInput type="number" [(ngModel)]="form.amount" min="0.01" step="0.01"
            placeholder="0.00" (ngModelChange)="onFieldChange()" />
          <mat-icon matPrefix class="text-gray-400">attach_money</mat-icon>
          <mat-hint>Enter the payment amount in Kenyan Shillings</mat-hint>
        </mat-form-field>

        <!-- Payment Method -->
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Payment Method <span class="text-red-500">*</span></mat-label>
          <mat-select [(ngModel)]="form.paymentMethod" (ngModelChange)="onMethodChange($event)">
            <mat-option *ngFor="let m of paymentMethods" [value]="m.value">
              <mat-icon class="icon-size-4 mr-2">{{ m.icon }}</mat-icon>
              {{ m.label }}
            </mat-option>
          </mat-select>
          <mat-icon matPrefix class="text-gray-400">{{ getMethodIcon(form.paymentMethod) }}</mat-icon>
        </mat-form-field>

        <!-- Status -->
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Status <span class="text-red-500">*</span></mat-label>
          <mat-select [(ngModel)]="form.statusPayment" (ngModelChange)="onFieldChange()">
            <mat-option *ngFor="let s of paymentStatuses" [value]="s.value">{{ s.label }}</mat-option>
          </mat-select>
          <mat-icon matPrefix class="text-gray-400">flag</mat-icon>
        </mat-form-field>

      </div>
    </mat-card-content>
  </mat-card>

  <!-- ── Dates & Notes ────────────────────────────────────────────── -->
  <mat-card class="shadow-sm border border-gray-200 dark:border-gray-700">
    <mat-card-header class="!pb-3">
      <mat-card-title class="!text-sm !font-semibold text-gray-700 dark:text-gray-300 flex items-center gap-2">
        <mat-icon class="text-gray-500 icon-size-4">calendar_today</mat-icon>
        Dates &amp; Notes
      </mat-card-title>
    </mat-card-header>
    <mat-card-content class="!pt-4">
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">

        <!-- Payment Date -->
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Payment Date <span class="text-red-500">*</span></mat-label>
          <input matInput type="date" [(ngModel)]="form.paymentDate" (ngModelChange)="onFieldChange()" />
          <mat-icon matPrefix class="text-gray-400">event</mat-icon>
        </mat-form-field>

        <!-- Received Date -->
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Received Date</mat-label>
          <input matInput type="date" [(ngModel)]="form.receivedDate" (ngModelChange)="onFieldChange()" />
          <mat-icon matPrefix class="text-gray-400">event_available</mat-icon>
          <mat-hint>Optional — date payment was received</mat-hint>
        </mat-form-field>

        <!-- Description -->
        <mat-form-field appearance="outline" class="w-full sm:col-span-2">
          <mat-label>Description</mat-label>
          <input matInput [(ngModel)]="form.description" maxlength="500"
            placeholder="e.g. Term 1 fees payment" (ngModelChange)="onFieldChange()" />
          <mat-icon matPrefix class="text-gray-400">short_text</mat-icon>
          <mat-hint align="end">{{ (form.description || '').length }} / 500</mat-hint>
        </mat-form-field>

        <!-- Notes -->
        <mat-form-field appearance="outline" class="w-full sm:col-span-2">
          <mat-label>Internal Notes</mat-label>
          <textarea matInput [(ngModel)]="form.notes" rows="3" maxlength="1000"
            placeholder="Any internal notes for this payment…"
            (ngModelChange)="onFieldChange()"></textarea>
          <mat-icon matPrefix class="text-gray-400">notes</mat-icon>
          <mat-hint align="end">{{ (form.notes || '').length }} / 1000</mat-hint>
        </mat-form-field>

      </div>
    </mat-card-content>
  </mat-card>

</div>
  `,
})
export class PaymentInfoStepComponent implements OnInit, OnChanges {

  @Input() formData:  any = {};
  @Input() students:  any[] = [];
  @Input() invoices:  any[] = [];
  @Input() staffList: any[] = [];
  @Input() schools:   any[] = [];
  @Input() isEditMode  = false;
  @Input() isSuperAdmin = false;

  @Output() formChanged    = new EventEmitter<any>();
  @Output() formValid      = new EventEmitter<boolean>();
  @Output() schoolChanged  = new EventEmitter<string>();
  @Output() studentChanged = new EventEmitter<string>();

  form: any = {
    studentId:     '',
    invoiceId:     '',
    receivedBy:    '',
    paymentDate:   new Date().toISOString().split('T')[0],
    receivedDate:  '',
    amount:        null,
    paymentMethod: 'Cash',
    statusPayment: 'Completed',
    description:   '',
    notes:         '',
    schoolId:      '',
  };

  studentSearch    = '';
  filteredStudents: any[] = [];

  readonly paymentMethods = [
    { value: 'Cash',         label: 'Cash',          icon: 'payments'         },
    { value: 'Mpesa',        label: 'M-Pesa',         icon: 'phone_android'    },
    { value: 'BankTransfer', label: 'Bank Transfer',  icon: 'account_balance'  },
    { value: 'Cheque',       label: 'Cheque',         icon: 'description'      },
    { value: 'Card',         label: 'Card',           icon: 'credit_card'      },
    { value: 'Online',       label: 'Online',         icon: 'language'         },
  ];

readonly paymentStatuses = [
  { value: 'Pending',   label: 'Pending'   },  // 0
  { value: 'Completed', label: 'Completed' },  // 1
  { value: 'Failed',    label: 'Failed'    },  // 2
  { value: 'Refunded',  label: 'Refunded'  },  // 3
  { value: 'Cancelled', label: 'Cancelled' },  // 4
  { value: 'Reversed',  label: 'Reversed'  },  // 5
];

  ngOnInit(): void {
    if (this.formData && Object.keys(this.formData).length) {
      this.form = { ...this.form, ...this.formData };
    }
    this.filteredStudents = [...this.students];
    this.validateAndEmit();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['students']) {
      this.filteredStudents = [...this.students];
    }
    if (changes['formData'] && this.formData) {
      this.form = { ...this.form, ...this.formData };
    }
  }

  filterStudents(): void {
    const q = this.studentSearch.toLowerCase();
    this.filteredStudents = q
      ? this.students.filter(s =>
          `${s.firstName} ${s.lastName}`.toLowerCase().includes(q) ||
          (s.admissionNo || '').toLowerCase().includes(q))
      : [...this.students];
  }

  onSchoolSelect(schoolId: string): void {
    this.onFieldChange();
    this.schoolChanged.emit(schoolId);
  }

  onStudentSelect(studentId: string): void {
    this.form.invoiceId = '';
    this.onFieldChange();
    this.studentChanged.emit(studentId);
  }

  onMethodChange(method: string): void {
    this.onFieldChange();
  }

  onFieldChange(): void {
    this.formChanged.emit({ ...this.form });
    this.validateAndEmit();
  }

  private validateAndEmit(): void {
    const valid =
      !!this.form.studentId &&
      !!this.form.invoiceId &&
      !!this.form.paymentDate &&
      (this.form.amount != null && +this.form.amount > 0) &&
      !!this.form.paymentMethod &&
      !!this.form.statusPayment &&
      (!this.isSuperAdmin || !!this.form.schoolId);

    this.formValid.emit(valid);
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

  formatCurrency(val: number): string {
    return (val ?? 0).toLocaleString('en-KE', { minimumFractionDigits: 2 });
  }
}