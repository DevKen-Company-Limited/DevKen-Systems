// ═══════════════════════════════════════════════════════════════════
// payment-bulk-form.component.ts
// Universal Payment Entry — handles ALL payment scenarios:
//   • Single student  →  Add one row manually
//   • Multiple students  →  Add rows manually or import by class
//   • Whole class / school  →  Use "Import by Class" panel
// Student search: ngx-mat-select-search (Option 1)
// Invoice flexibility: A) auto-strategy  B) apply-to-all  C) manual per row
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
import { NgxMatSelectSearchModule } from 'ngx-mat-select-search';
import { Subject, forkJoin, of } from 'rxjs';
import { takeUntil, catchError, finalize } from 'rxjs/operators';

import { AlertService } from 'app/core/DevKenService/Alert/AlertService';
import { AuthService } from 'app/core/auth/auth.service';
import { PaymentService } from 'app/core/DevKenService/payments/payment.service';
import {
    BulkPaymentDto, BulkPaymentItemDto, BulkPaymentResultDto,
    PaymentMethod, PaymentStatus, PaymentMethodValue, PaymentStatusValue,
} from '../types/payments';

// ── Auto-invoice strategy ──────────────────────────────────────────
type InvoiceStrategy = 'none' | 'first' | 'highest' | 'lowest';

// ── Row model ─────────────────────────────────────────────────────
interface BulkRow {
    studentId: string;
    invoiceId: string;
    amount: number | null;
    mpesaCode?: string;
    phoneNumber?: string;
    transactionReference?: string;
    notes?: string;
    // UI-only
    _studentName: string;
    _studentSearchCtrl: FormControl;   // drives ngx-mat-select-search
    _filteredStudents: any[];
    _invoices: any[];
    _loadingInvoices: boolean;
    _error?: string;
}

// ── Importable student ─────────────────────────────────────────────
interface ImportStudent {
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
    imports: [
        CommonModule, FormsModule, ReactiveFormsModule,
        MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatTooltipModule,
        MatFormFieldModule, MatInputModule, MatSelectModule,
        MatCardModule, MatDividerModule, MatCheckboxModule,
        NgxMatSelectSearchModule,
    ],
    template: `
<!-- ── Loading overlay ─────────────────────────────────────────── -->
<div *ngIf="isLoadingLookups"
  class="fixed inset-0 z-50 flex flex-col items-center justify-center
         bg-white/80 dark:bg-gray-900/80 backdrop-blur-sm">
  <mat-progress-spinner mode="indeterminate" diameter="52" strokeWidth="4"></mat-progress-spinner>
  <p class="mt-4 text-sm font-medium text-gray-600 dark:text-gray-400">Loading students &amp; staff…</p>
</div>

<div class="w-full min-h-screen bg-gray-50 dark:bg-gray-900">

  <!-- ── Sticky top bar ───────────────────────────────────────── -->
  <header class="sticky top-0 z-20 flex items-center justify-between
                 px-4 sm:px-6 lg:px-8 py-3
                 bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 shadow-sm">

    <div class="flex items-center gap-3 min-w-0">
      <button mat-icon-button (click)="goBack()" matTooltip="Back to Payments">
        <mat-icon svgIcon="heroicons_outline:arrow-left"></mat-icon>
      </button>
      <div class="flex items-center gap-3 min-w-0">
        <div class="flex items-center justify-center w-9 h-9 rounded-xl shadow-md flex-shrink-0
                    bg-gradient-to-br from-teal-500 to-emerald-600">
          <mat-icon class="text-white text-base">payments</mat-icon>
        </div>
        <div class="min-w-0">
          <h1 class="text-base font-bold text-gray-900 dark:text-white leading-tight">
            Payment Entry
          </h1>
          <p class="text-xs text-gray-500 dark:text-gray-400 truncate">
            <ng-container *ngIf="rows.length === 0">Single student · Class · Whole school</ng-container>
            <ng-container *ngIf="rows.length > 0">
              {{ rows.length }} row{{ rows.length !== 1 ? 's' : '' }}
              · <span class="text-emerald-600 font-semibold">KES {{ formatCurrency(totalAmount) }}</span>
              · <span [class.text-green-600]="validRowCount === rows.length"
                      [class.text-amber-600]="validRowCount < rows.length">
                  {{ validRowCount }}/{{ rows.length }} valid
                </span>
            </ng-container>
          </p>
        </div>
      </div>
    </div>

    <div class="flex items-center gap-2 flex-shrink-0">
      <button mat-icon-button (click)="showHelp = !showHelp"
        [class.!bg-blue-50]="showHelp"
        matTooltip="{{ showHelp ? 'Hide help' : 'How to use this form' }}">
        <mat-icon class="text-blue-500">help_outline</mat-icon>
      </button>
      <button mat-stroked-button (click)="toggleClassPanel()"
        [disabled]="isSuperAdmin && !shared.schoolId"
        [matTooltip]="isSuperAdmin && !shared.schoolId ? 'Select a school first' : (showClassImport ? 'Hide import panel' : 'Import students by class')"
        [class.!bg-indigo-50]="showClassImport"
        [class.!border-indigo-400]="showClassImport"
        class="border-indigo-200 text-indigo-700 hover:bg-indigo-50">
        <mat-icon class="icon-size-5">groups</mat-icon>
        <span class="ml-1 hidden sm:inline">
          {{ showClassImport ? 'Hide Panel' : 'Import by Class' }}
        </span>
      </button>
      <button mat-stroked-button (click)="addRow()"
        [disabled]="isSuperAdmin && !shared.schoolId"
        [matTooltip]="isSuperAdmin && !shared.schoolId ? 'Select a school first' : 'Add a payment row'"
        class="border-emerald-200 text-emerald-700 hover:bg-emerald-50">
        <mat-icon class="icon-size-5">add</mat-icon>
        <span class="ml-1 hidden sm:inline">Add Row</span>
      </button>
      <button mat-flat-button [disabled]="!isValid || isSubmitting" (click)="submit()"
        class="!bg-emerald-600 hover:!bg-emerald-700 !text-white">
        <mat-progress-spinner *ngIf="isSubmitting" mode="indeterminate" diameter="16"
          strokeWidth="2" class="inline-block mr-1"></mat-progress-spinner>
        <mat-icon *ngIf="!isSubmitting" class="icon-size-5">send</mat-icon>
        <span class="ml-1">{{ isSubmitting ? 'Submitting…' : 'Submit' }}</span>
      </button>
    </div>
  </header>

  <div class="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-6 space-y-6">
    <!-- ════════════════════════════════════════════════
         HOW TO USE — collapsible help panel
         ════════════════════════════════════════════════ -->
    <div *ngIf="showHelp"
      class="rounded-2xl border-2 border-blue-200 dark:border-blue-800
             bg-blue-50 dark:bg-blue-900/20 overflow-hidden">

      <!-- Header -->
      <div class="flex items-center justify-between px-5 py-3
                  border-b border-blue-100 dark:border-blue-800
                  bg-blue-100/60 dark:bg-blue-900/40">
        <div class="flex items-center gap-2">
          <mat-icon class="text-blue-600 dark:text-blue-400">help_outline</mat-icon>
          <span class="text-sm font-bold text-blue-800 dark:text-blue-200">
            How to use Payment Entry
          </span>
          <span class="text-xs text-blue-500 font-normal ml-1">
            — works for 1 student up to your entire school
          </span>
        </div>
        <button mat-icon-button class="!w-7 !h-7" (click)="showHelp = false">
          <mat-icon class="text-blue-400 icon-size-4">close</mat-icon>
        </button>
      </div>

      <div class="px-5 py-5 space-y-5">

        <!-- Scenario cards -->
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">

          <!-- Single student -->
          <div class="rounded-xl border border-blue-200 dark:border-blue-700
                      bg-white dark:bg-gray-800 p-4 space-y-2">
            <div class="flex items-center gap-2">
              <div class="flex items-center justify-center w-8 h-8 rounded-full flex-shrink-0
                          bg-emerald-100 dark:bg-emerald-900/40 text-emerald-700">
                <mat-icon class="icon-size-5">person</mat-icon>
              </div>
              <span class="text-sm font-bold text-gray-800 dark:text-white">Single Student</span>
            </div>
            <ol class="text-xs text-gray-600 dark:text-gray-400 space-y-1.5 pl-0 list-none">
              <li class="flex gap-2">
                <span class="flex-shrink-0 w-4 h-4 rounded-full bg-emerald-500 text-white text-[10px]
                             font-bold flex items-center justify-center">1</span>
                Fill in <strong>Shared Settings</strong> (date, method).
              </li>
              <li class="flex gap-2">
                <span class="flex-shrink-0 w-4 h-4 rounded-full bg-emerald-500 text-white text-[10px]
                             font-bold flex items-center justify-center">2</span>
                Click <strong>Add Row</strong> → search and select the student.
              </li>
              <li class="flex gap-2">
                <span class="flex-shrink-0 w-4 h-4 rounded-full bg-emerald-500 text-white text-[10px]
                             font-bold flex items-center justify-center">3</span>
                Pick the invoice, enter amount → <strong>Submit</strong>.
              </li>
            </ol>
          </div>

          <!-- Multiple / select students -->
          <div class="rounded-xl border border-blue-200 dark:border-blue-700
                      bg-white dark:bg-gray-800 p-4 space-y-2">
            <div class="flex items-center gap-2">
              <div class="flex items-center justify-center w-8 h-8 rounded-full flex-shrink-0
                          bg-indigo-100 dark:bg-indigo-900/40 text-indigo-700">
                <mat-icon class="icon-size-5">group</mat-icon>
              </div>
              <span class="text-sm font-bold text-gray-800 dark:text-white">Multiple Students</span>
            </div>
            <ol class="text-xs text-gray-600 dark:text-gray-400 space-y-1.5 pl-0 list-none">
              <li class="flex gap-2">
                <span class="flex-shrink-0 w-4 h-4 rounded-full bg-indigo-500 text-white text-[10px]
                             font-bold flex items-center justify-center">1</span>
                Fill in <strong>Shared Settings</strong> and set an <strong>Invoice Strategy</strong>.
              </li>
              <li class="flex gap-2">
                <span class="flex-shrink-0 w-4 h-4 rounded-full bg-indigo-500 text-white text-[10px]
                             font-bold flex items-center justify-center">2</span>
                Click <strong>Import by Class</strong> → filter to the class → tick students → Import.
              </li>
              <li class="flex gap-2">
                <span class="flex-shrink-0 w-4 h-4 rounded-full bg-indigo-500 text-white text-[10px]
                             font-bold flex items-center justify-center">3</span>
                Invoices are auto-selected. Adjust amounts per row if needed → <strong>Submit</strong>.
              </li>
            </ol>
          </div>

          <!-- Whole school -->
          <div class="rounded-xl border border-blue-200 dark:border-blue-700
                      bg-white dark:bg-gray-800 p-4 space-y-2">
            <div class="flex items-center gap-2">
              <div class="flex items-center justify-center w-8 h-8 rounded-full flex-shrink-0
                          bg-violet-100 dark:bg-violet-900/40 text-violet-700">
                <mat-icon class="icon-size-5">corporate_fare</mat-icon>
              </div>
              <span class="text-sm font-bold text-gray-800 dark:text-white">Whole Class / School</span>
            </div>
            <ol class="text-xs text-gray-600 dark:text-gray-400 space-y-1.5 pl-0 list-none">
              <li class="flex gap-2">
                <span class="flex-shrink-0 w-4 h-4 rounded-full bg-violet-500 text-white text-[10px]
                             font-bold flex items-center justify-center">1</span>
                Set <strong>Invoice Strategy</strong> to "First unpaid" or "Highest balance".
              </li>
              <li class="flex gap-2">
                <span class="flex-shrink-0 w-4 h-4 rounded-full bg-violet-500 text-white text-[10px]
                             font-bold flex items-center justify-center">2</span>
                Open <strong>Import by Class</strong>. Leave filters blank to see all students.
                Set a <strong>Default Amount</strong>, then tick "Select all" → Import.
              </li>
              <li class="flex gap-2">
                <span class="flex-shrink-0 w-4 h-4 rounded-full bg-violet-500 text-white text-[10px]
                             font-bold flex items-center justify-center">3</span>
                Review rows, fix any flagged in amber → <strong>Submit all</strong> at once.
              </li>
            </ol>
          </div>

        </div>

        <!-- Feature tips -->
        <div class="rounded-xl border border-blue-100 dark:border-blue-800
                    bg-white/60 dark:bg-gray-800/60 p-4">
          <p class="text-xs font-bold text-blue-700 dark:text-blue-300 mb-3 flex items-center gap-1.5">
            <mat-icon class="icon-size-4">tips_and_updates</mat-icon>
            Useful features to know
          </p>
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-x-8 gap-y-2">

            <div class="flex gap-2 text-xs text-gray-600 dark:text-gray-400">
              <mat-icon class="icon-size-4 text-teal-500 flex-shrink-0 mt-0.5">auto_fix_high</mat-icon>
              <span><strong class="text-gray-800 dark:text-white">Invoice Strategy</strong> —
                Set "First unpaid", "Highest" or "Lowest balance" in Shared Settings. Invoices are
                auto-picked for every student on import. Change strategy and hit
                <em>Re-apply to all rows</em> to update existing rows instantly.
              </span>
            </div>

            <div class="flex gap-2 text-xs text-gray-600 dark:text-gray-400">
              <mat-icon class="icon-size-4 text-indigo-500 flex-shrink-0 mt-0.5">content_copy</mat-icon>
              <span><strong class="text-gray-800 dark:text-white">Copy invoice to all rows</strong> —
                Once a row has an invoice selected, click the
                <mat-icon class="icon-size-3 align-middle">content_copy</mat-icon> icon in its header
                to push that same invoice type to every other row.
              </span>
            </div>

            <div class="flex gap-2 text-xs text-gray-600 dark:text-gray-400">
              <mat-icon class="icon-size-4 text-emerald-500 flex-shrink-0 mt-0.5">search</mat-icon>
              <span><strong class="text-gray-800 dark:text-white">Student search</strong> —
                In each row's Student dropdown, just start typing a name or admission number.
                The list filters live — no need to scroll through hundreds of students.
              </span>
            </div>

            <div class="flex gap-2 text-xs text-gray-600 dark:text-gray-400">
              <mat-icon class="icon-size-4 text-amber-500 flex-shrink-0 mt-0.5">attach_money</mat-icon>
              <span><strong class="text-gray-800 dark:text-white">Default amount on import</strong> —
                In the Import panel set a "Default Amount (KES)" before importing. All rows
                will be pre-filled — you can still edit individual rows after.
              </span>
            </div>

            <div class="flex gap-2 text-xs text-gray-600 dark:text-gray-400">
              <mat-icon class="icon-size-4 text-red-400 flex-shrink-0 mt-0.5">refresh</mat-icon>
              <span><strong class="text-gray-800 dark:text-white">Retry failed payments</strong> —
                After submission, if some rows fail, click <em>Retry Failed</em> to reload only
                those rows and resubmit without re-entering the successful ones.
              </span>
            </div>

            <div class="flex gap-2 text-xs text-gray-600 dark:text-gray-400">
              <mat-icon class="icon-size-4 text-gray-400 flex-shrink-0 mt-0.5">circle</mat-icon>
              <span><strong class="text-gray-800 dark:text-white">Row status dots</strong> —
                Each row card shows a coloured dot:
                <span class="inline-block w-2 h-2 rounded-full bg-gray-300 align-middle"></span> not started,
                <span class="inline-block w-2 h-2 rounded-full bg-amber-400 align-middle"></span> in progress,
                <span class="inline-block w-2 h-2 rounded-full bg-green-500 align-middle"></span> complete,
                <span class="inline-block w-2 h-2 rounded-full bg-red-500 align-middle"></span> error.
              </span>
            </div>

          </div>
        </div>

      </div>
    </div>

    <!-- ════════════════════════════════════════════════
         STEP 1 — SHARED SETTINGS
         ════════════════════════════════════════════════ -->
    <mat-card class="shadow-sm !rounded-2xl border border-teal-200 dark:border-teal-800">
      <mat-card-header class="!px-5 !pt-4 !pb-3 border-b border-teal-100 dark:border-teal-800
                               bg-teal-50 dark:bg-teal-900/20 !rounded-t-2xl">
        <div class="flex items-center gap-2">
          <span class="flex items-center justify-center w-6 h-6 rounded-full
                       bg-teal-600 text-white text-xs font-bold flex-shrink-0">1</span>
          <mat-card-title class="!text-sm !font-semibold !m-0 text-teal-700 dark:text-teal-300">
            Shared Settings
            <span class="font-normal text-teal-500 ml-1">— applied to every row</span>
          </mat-card-title>
        </div>
      </mat-card-header>
      <mat-card-content class="!px-5 !pt-5 !pb-4">
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">

          <mat-form-field *ngIf="isSuperAdmin" appearance="outline" class="w-full lg:col-span-4">
            <mat-label>School <span class="text-red-500">*</span></mat-label>
            <mat-select [(ngModel)]="shared.schoolId" (ngModelChange)="onSchoolChange($event)">
              <mat-option value="">— Select school —</mat-option>
              <mat-option *ngFor="let s of schools" [value]="s.id">{{ s.name }}</mat-option>
            </mat-select>
            <mat-icon matPrefix class="text-gray-400">corporate_fare</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Payment Date <span class="text-red-500">*</span></mat-label>
            <input matInput type="date" [(ngModel)]="shared.paymentDate" />
            <mat-icon matPrefix class="text-gray-400">event</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Method <span class="text-red-500">*</span></mat-label>
            <mat-select [(ngModel)]="shared.paymentMethod" (ngModelChange)="onMethodChange()">
              <mat-option *ngFor="let m of paymentMethods" [value]="m.value">{{ m.label }}</mat-option>
            </mat-select>
            <mat-icon matPrefix class="text-gray-400">{{ getMethodIcon(shared.paymentMethod) }}</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Status</mat-label>
            <mat-select [(ngModel)]="shared.statusPayment">
              <mat-option value="Completed">Completed</mat-option>
              <mat-option value="Pending">Pending</mat-option>
            </mat-select>
            <mat-icon matPrefix class="text-gray-400">flag</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Received By</mat-label>
            <mat-select [(ngModel)]="shared.receivedBy">
              <mat-option value="">— Optional —</mat-option>
              <mat-option *ngFor="let s of staffList" [value]="s.id">
                {{ s.firstName }} {{ s.lastName }}
              </mat-option>
            </mat-select>
            <mat-icon matPrefix class="text-gray-400">badge</mat-icon>
          </mat-form-field>

          <ng-container *ngIf="shared.paymentMethod === 'BankTransfer' || shared.paymentMethod === 'Cheque'">
            <mat-form-field appearance="outline" class="w-full">
              <mat-label>Bank Name</mat-label>
              <input matInput [(ngModel)]="shared.bankName" placeholder="e.g. Equity Bank" />
              <mat-icon matPrefix class="text-gray-400">account_balance</mat-icon>
            </mat-form-field>
            <mat-form-field appearance="outline" class="w-full">
              <mat-label>Account Number</mat-label>
              <input matInput [(ngModel)]="shared.accountNumber" />
              <mat-icon matPrefix class="text-gray-400">credit_card</mat-icon>
            </mat-form-field>
          </ng-container>

          <mat-form-field appearance="outline" class="w-full sm:col-span-2 lg:col-span-4">
            <mat-label>Description</mat-label>
            <input matInput [(ngModel)]="shared.description" maxlength="500"
              placeholder="e.g. Term 2 fee payment batch" />
            <mat-icon matPrefix class="text-gray-400">short_text</mat-icon>
          </mat-form-field>

        </div>

        <!-- ── OPTION C: Auto-invoice strategy ────────────────── -->
        <div class="mt-4 pt-4 border-t border-teal-100 dark:border-teal-800">
          <p class="text-xs font-semibold text-teal-700 dark:text-teal-400 mb-3 flex items-center gap-1.5">
            <mat-icon class="icon-size-4">auto_fix_high</mat-icon>
            Invoice Strategy
            <span class="font-normal text-gray-500 ml-1">
              — how to auto-pick an invoice when a student has multiple
            </span>
          </p>
          <div class="flex flex-wrap items-end gap-4">
            <mat-form-field appearance="outline" class="w-full sm:w-56 !mb-0">
              <mat-label>Auto-select Invoice</mat-label>
              <mat-select [(ngModel)]="shared.invoiceStrategy" (ngModelChange)="onStrategyChange()">
                <mat-option value="none">Manual (pick per row)</mat-option>
                <mat-option value="first">First unpaid invoice</mat-option>
                <mat-option value="highest">Highest balance due</mat-option>
                <mat-option value="lowest">Lowest balance due</mat-option>
              </mat-select>
              <mat-icon matPrefix class="text-gray-400">receipt_long</mat-icon>
            </mat-form-field>

            <button *ngIf="rows.length > 0 && shared.invoiceStrategy !== 'none'"
              mat-stroked-button (click)="applyStrategyToAllRows()"
              matTooltip="Re-apply the selected strategy to every existing row"
              class="border-teal-200 text-teal-700 hover:bg-teal-50 !mb-0.5">
              <mat-icon class="icon-size-5">sync</mat-icon>
              <span class="ml-1">Re-apply to all rows</span>
            </button>

            <p class="text-xs text-gray-500 leading-snug self-center">{{ strategyHint }}</p>
          </div>
        </div>

      </mat-card-content>
    </mat-card>

    <!-- ════════════════════════════════════════════════
         SUPERADMIN — SCHOOL REQUIRED BANNER
         ════════════════════════════════════════════════ -->
    <div *ngIf="isSuperAdmin && !shared.schoolId"
      class="flex items-start gap-4 rounded-2xl border-2 border-amber-300 dark:border-amber-600
             bg-amber-50 dark:bg-amber-900/20 px-5 py-4">
      <div class="flex items-center justify-center w-10 h-10 rounded-full flex-shrink-0
                  bg-amber-100 dark:bg-amber-800/50 text-amber-600 dark:text-amber-400 mt-0.5">
        <mat-icon>corporate_fare</mat-icon>
      </div>
      <div class="flex-1 min-w-0">
        <p class="text-sm font-bold text-amber-800 dark:text-amber-200">
          Select a school to continue
        </p>
        <p class="text-xs text-amber-700 dark:text-amber-300 mt-0.5 leading-relaxed">
          As a Super Admin you can record payments for any school in the system.
          Please choose a school in <strong>Shared Settings → School</strong> above before adding
          rows, importing students, or submitting. All data will be scoped to the school you select.
        </p>
        <div class="flex flex-wrap items-center gap-2 mt-3">
          <span class="flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium
                       bg-amber-100 dark:bg-amber-800/40 text-amber-800 dark:text-amber-200
                       border border-amber-200 dark:border-amber-700">
            <mat-icon class="icon-size-3">looks_one</mat-icon>
            Pick a school above
          </span>
          <mat-icon class="text-amber-400 icon-size-4">arrow_forward</mat-icon>
          <span class="flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium
                       bg-amber-100 dark:bg-amber-800/40 text-amber-800 dark:text-amber-200
                       border border-amber-200 dark:border-amber-700">
            <mat-icon class="icon-size-3">looks_two</mat-icon>
            Add rows / Import class
          </span>
          <mat-icon class="text-amber-400 icon-size-4">arrow_forward</mat-icon>
          <span class="flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium
                       bg-amber-100 dark:bg-amber-800/40 text-amber-800 dark:text-amber-200
                       border border-amber-200 dark:border-amber-700">
            <mat-icon class="icon-size-3">looks_3</mat-icon>
            Submit payments
          </span>
        </div>
      </div>
    </div>

    <!-- ════════════════════════════════════════════════
         STEP 2 — CLASS IMPORT PANEL
         ════════════════════════════════════════════════ -->
    <mat-card *ngIf="showClassImport"
      class="shadow-sm !rounded-2xl border-2 border-indigo-300 dark:border-indigo-700">
      <mat-card-header class="!px-5 !pt-4 !pb-3 border-b border-indigo-100 dark:border-indigo-800
                               bg-indigo-50 dark:bg-indigo-900/20 !rounded-t-2xl">
        <div class="flex items-center justify-between w-full">
          <div class="flex items-center gap-2">
            <span class="flex items-center justify-center w-6 h-6 rounded-full
                         bg-indigo-600 text-white text-xs font-bold flex-shrink-0">2</span>
            <mat-card-title class="!text-sm !font-semibold !m-0 text-indigo-700 dark:text-indigo-300">
              Import Students by Class
            </mat-card-title>
          </div>
          <button mat-icon-button class="!w-7 !h-7" (click)="showClassImport = false">
            <mat-icon class="text-indigo-400 icon-size-4">close</mat-icon>
          </button>
        </div>
      </mat-card-header>

      <mat-card-content class="!px-5 !pt-5 !pb-4 space-y-4">

        <!-- Cascading filters -->
        <div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
          <mat-form-field appearance="outline" class="w-full">
            <mat-label>CBC Level</mat-label>
            <mat-select [(ngModel)]="classFilter.cbcLevel"
              (ngModelChange)="onClassFilterChange('cbcLevel')">
              <mat-option value="">All Levels</mat-option>
              <mat-option *ngFor="let l of cbcLevels" [value]="l">{{ l }}</mat-option>
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Grade</mat-label>
            <mat-select [(ngModel)]="classFilter.grade"
              (ngModelChange)="onClassFilterChange('grade')">
              <mat-option value="">All Grades</mat-option>
              <mat-option *ngFor="let g of availableGrades" [value]="g">{{ g }}</mat-option>
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Stream</mat-label>
            <mat-select [(ngModel)]="classFilter.stream"
              (ngModelChange)="onClassFilterChange('stream')">
              <mat-option value="">All Streams</mat-option>
              <mat-option *ngFor="let s of availableStreams" [value]="s">{{ s }}</mat-option>
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Search student</mat-label>
            <input matInput [(ngModel)]="classFilter.search"
              (ngModelChange)="onClassFilterChange('search')"
              placeholder="Name or Adm. No." />
            <mat-icon matSuffix class="text-gray-400">search</mat-icon>
          </mat-form-field>
        </div>

        <!-- Default amount -->
        <div class="flex flex-wrap items-center gap-4 p-3 rounded-xl
                    bg-gray-50 dark:bg-gray-800 border border-gray-200 dark:border-gray-700">
          <mat-icon class="text-emerald-500 flex-shrink-0">attach_money</mat-icon>
          <mat-form-field appearance="outline" class="!mb-0 w-48">
            <mat-label>Default Amount (KES)</mat-label>
            <input matInput type="number" [(ngModel)]="classImportAmount"
              min="0.01" step="0.01" placeholder="Optional" />
          </mat-form-field>
          <p class="text-xs text-gray-500 leading-snug">
            Pre-fills the amount for all imported rows.<br>You can still edit each row after import.
          </p>
        </div>

        <!-- Status bar -->
        <div class="flex items-center justify-between">
          <p class="text-sm text-gray-600 dark:text-gray-400">
            <span class="font-semibold text-indigo-700 dark:text-indigo-400">
              {{ filteredClassStudents.length }}
            </span>
            student{{ filteredClassStudents.length !== 1 ? 's' : '' }} match
            <ng-container *ngIf="newSelectionCount > 0">
              · <span class="font-semibold text-emerald-600">{{ newSelectionCount }} new selected</span>
            </ng-container>
            <ng-container *ngIf="alreadyAddedCount > 0">
              · <span class="text-amber-600">{{ alreadyAddedCount }} already added</span>
            </ng-container>
          </p>
          <button *ngIf="hasActiveClassFilters" mat-button class="text-xs !text-indigo-600 !min-h-0"
            (click)="clearClassFilters()">
            <mat-icon class="icon-size-4">filter_alt_off</mat-icon> Clear filters
          </button>
        </div>

        <!-- Select-all bar -->
        <div *ngIf="filteredClassStudents.length > 0"
          class="flex items-center gap-3 px-3 py-2 rounded-lg
                 bg-indigo-50 dark:bg-indigo-900/20 border border-indigo-100 dark:border-indigo-800">
          <mat-checkbox [checked]="allClassSelected" [indeterminate]="someClassSelected"
            (change)="toggleSelectAll($event.checked)">
          </mat-checkbox>
          <span class="text-sm text-indigo-700 dark:text-indigo-300">
            {{ allClassSelected ? 'Deselect all' : 'Select all ' + filteredClassStudents.length }}
          </span>
        </div>

        <!-- Empty -->
        <div *ngIf="filteredClassStudents.length === 0" class="flex flex-col items-center py-8">
          <mat-icon class="text-gray-300 dark:text-gray-600 !w-12 !h-12 !text-5xl">person_search</mat-icon>
          <p class="mt-2 text-sm text-gray-500">No students match these filters</p>
        </div>

        <!-- Scrollable list -->
        <div class="max-h-64 overflow-y-auto rounded-xl border
                    border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 divide-y
                    divide-gray-100 dark:divide-gray-800">
          <div *ngFor="let s of filteredClassStudents; trackBy: trackById"
            class="flex items-center gap-3 px-3 py-2.5 cursor-pointer select-none
                   transition-colors hover:bg-gray-50 dark:hover:bg-gray-800"
            [class.bg-indigo-50]="s.selected"
            (click)="toggleStudent(s)">

            <mat-checkbox [(ngModel)]="s.selected"
              (ngModelChange)="updateSelectAllState()"
              (click)="$event.stopPropagation()">
            </mat-checkbox>

            <div class="flex items-center justify-center w-8 h-8 rounded-full flex-shrink-0
                        text-xs font-bold text-white bg-gradient-to-br from-indigo-400 to-violet-500">
              {{ getInitials(s) }}
            </div>

            <div class="flex-1 min-w-0">
              <p class="text-sm font-semibold text-gray-900 dark:text-white truncate">
                {{ s.firstName }} {{ s.lastName }}
              </p>
              <p class="text-xs text-gray-500 truncate">
                {{ s.admissionNo || s.admissionNumber || 'No Adm. No.' }}
                <ng-container *ngIf="getStudentClass(s)">· {{ getStudentClass(s) }}</ng-container>
              </p>
            </div>

            <span *ngIf="isAlreadyAdded(s.id)"
              class="text-[10px] px-1.5 py-0.5 rounded-full font-semibold flex-shrink-0
                     bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400">
              Added
            </span>
          </div>
        </div>

        <!-- Import action -->
        <div class="flex items-center justify-between pt-2 border-t border-gray-200 dark:border-gray-700">
          <p class="text-sm text-gray-500">
            <ng-container *ngIf="newSelectionCount > 0; else noSel">
              <span class="font-semibold text-indigo-700 dark:text-indigo-300">{{ newSelectionCount }}</span>
              student{{ newSelectionCount !== 1 ? 's' : '' }} will be imported.
            </ng-container>
            <ng-template #noSel>Select students above to import.</ng-template>
          </p>
          <button mat-flat-button [disabled]="newSelectionCount === 0 || isImporting"
            (click)="importSelectedStudents()"
            class="!bg-indigo-600 hover:!bg-indigo-700 !text-white">
            <mat-progress-spinner *ngIf="isImporting" mode="indeterminate" diameter="16"
              strokeWidth="2" class="inline-block mr-1"></mat-progress-spinner>
            <mat-icon *ngIf="!isImporting" class="icon-size-5">group_add</mat-icon>
            <span class="ml-1">
              {{ isImporting ? 'Importing…' : 'Import ' + (newSelectionCount > 0 ? newSelectionCount : '') }}
            </span>
          </button>
        </div>

      </mat-card-content>
    </mat-card>

    <!-- ════════════════════════════════════════════════
         STEP 3 — PAYMENT ROWS
         ════════════════════════════════════════════════ -->
    <div class="space-y-3">

      <div class="flex items-center justify-between">
        <div class="flex items-center gap-2">
          <span class="flex items-center justify-center w-6 h-6 rounded-full
                       bg-emerald-600 text-white text-xs font-bold flex-shrink-0">
            {{ showClassImport ? '3' : '2' }}
          </span>
          <h2 class="text-sm font-bold text-gray-900 dark:text-white">Payment Rows</h2>
          <span *ngIf="rows.length > 0"
            class="px-2 py-0.5 rounded-full text-xs font-semibold
                   bg-teal-100 text-teal-700 dark:bg-teal-900/30 dark:text-teal-400">
            {{ rows.length }}
          </span>
          <span *ngIf="rows.length > 0 && validRowCount < rows.length"
            class="px-2 py-0.5 rounded-full text-xs font-semibold
                   bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400">
            {{ rows.length - validRowCount }} incomplete
          </span>
        </div>
        <div class="flex items-center gap-2">
          <button *ngIf="rows.length > 0" mat-button (click)="clearAll()"
            class="!text-red-500 !text-xs !min-h-0">
            <mat-icon class="icon-size-4">delete_sweep</mat-icon>
            <span class="ml-0.5 hidden sm:inline">Clear all</span>
          </button>
          <button mat-stroked-button (click)="addRow()"
            class="border-emerald-200 text-emerald-700 hover:bg-emerald-50 !text-xs">
            <mat-icon class="icon-size-4">add</mat-icon>
            <span class="ml-1">Add row</span>
          </button>
        </div>
      </div>

      <!-- Empty state -->
      <div *ngIf="rows.length === 0"
        class="flex flex-col items-center gap-4 py-12 border-2 border-dashed
               border-gray-300 dark:border-gray-700 rounded-2xl">
        <mat-icon class="text-gray-300 dark:text-gray-600 !w-14 !h-14 !text-5xl">payments</mat-icon>
        <div class="text-center">
          <p class="text-sm font-bold text-gray-700 dark:text-gray-200">Ready to record payments</p>
          <p class="text-xs text-gray-400 dark:text-gray-500 mt-1">
            Use for a single student, a whole class, or everyone in school
          </p>
        </div>
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-3 w-full max-w-xl px-4">
          <div class="flex flex-col items-center gap-1.5 p-3 rounded-xl
                      border border-emerald-200 dark:border-emerald-800
                      bg-emerald-50 dark:bg-emerald-900/20 text-center">
            <mat-icon class="text-emerald-600 icon-size-5">person</mat-icon>
            <span class="text-xs font-semibold text-emerald-700 dark:text-emerald-400">Single student</span>
            <span class="text-[10px] text-gray-500">Click Add Row below</span>
          </div>
          <div class="flex flex-col items-center gap-1.5 p-3 rounded-xl
                      border border-indigo-200 dark:border-indigo-800
                      bg-indigo-50 dark:bg-indigo-900/20 text-center">
            <mat-icon class="text-indigo-600 icon-size-5">group</mat-icon>
            <span class="text-xs font-semibold text-indigo-700 dark:text-indigo-400">Class / group</span>
            <span class="text-[10px] text-gray-500">Import by Class below</span>
          </div>
          <div class="flex flex-col items-center gap-1.5 p-3 rounded-xl
                      border border-violet-200 dark:border-violet-800
                      bg-violet-50 dark:bg-violet-900/20 text-center">
            <mat-icon class="text-violet-600 icon-size-5">corporate_fare</mat-icon>
            <span class="text-xs font-semibold text-violet-700 dark:text-violet-400">Whole school</span>
            <span class="text-[10px] text-gray-500">Import — no filters</span>
          </div>
        </div>
        <div class="flex gap-3 mt-1">
          <button mat-stroked-button (click)="toggleClassPanel()"
            [disabled]="isSuperAdmin && !shared.schoolId"
            [matTooltip]="isSuperAdmin && !shared.schoolId ? 'Select a school in Shared Settings first' : ''"
            class="border-indigo-200 text-indigo-700 hover:bg-indigo-50">
            <mat-icon>groups</mat-icon><span class="ml-1">Import by Class</span>
          </button>
          <button mat-flat-button (click)="addRow()"
            [disabled]="isSuperAdmin && !shared.schoolId"
            [matTooltip]="isSuperAdmin && !shared.schoolId ? 'Select a school in Shared Settings first' : ''"
            class="!bg-emerald-600 !text-white">
            <mat-icon>add</mat-icon><span class="ml-1">Add Row</span>
          </button>
          <button mat-stroked-button (click)="showHelp = true"
            class="border-blue-200 text-blue-600 hover:bg-blue-50">
            <mat-icon class="icon-size-5">help_outline</mat-icon><span class="ml-1">How to use</span>
          </button>
        </div>
      </div>

      <!-- Row cards -->
      <mat-card *ngFor="let row of rows; let i = index; trackBy: trackByIndex"
        class="!rounded-xl shadow-sm border transition-all"
        [class.border-red-300]="row._error"
        [class.dark:border-red-700]="row._error"
        [class.border-green-200]="!row._error && isRowComplete(row)"
        [class.dark:border-green-800]="!row._error && isRowComplete(row)"
        [class.border-gray-200]="!row._error && !isRowComplete(row)"
        [class.dark:border-gray-700]="!row._error && !isRowComplete(row)">

        <!-- Row header -->
        <div class="flex items-center justify-between px-4 py-2.5
                    border-b border-gray-100 dark:border-gray-700">
          <div class="flex items-center gap-2 min-w-0">
            <span class="w-2 h-2 rounded-full flex-shrink-0"
              [class.bg-green-500]="isRowComplete(row)"
              [class.bg-amber-400]="!isRowComplete(row) && !row._error && !!row.studentId"
              [class.bg-gray-300]="!row.studentId"
              [class.bg-red-500]="!!row._error">
            </span>
            <span class="text-xs font-bold text-gray-400 flex-shrink-0">#{{ i + 1 }}</span>
            <span class="text-sm font-semibold text-gray-800 dark:text-white truncate">
              {{ row._studentName || '— Select a student —' }}
            </span>
            <span *ngIf="row.invoiceId" class="text-xs text-gray-400 hidden sm:inline truncate">
              · {{ getInvoiceLabel(row) }}
            </span>
          </div>
          <div class="flex items-center gap-2 flex-shrink-0">
            <span *ngIf="row.amount && +row.amount > 0"
              class="text-sm font-bold text-emerald-600 dark:text-emerald-400">
              KES {{ formatCurrency(row.amount) }}
            </span>

            <!-- OPTION B: Apply this row's invoice to all rows -->
            <button *ngIf="row.invoiceId && rows.length > 1"
              mat-icon-button class="!w-7 !h-7"
              (click)="applyInvoiceToAll(row)"
              matTooltip="Apply this invoice selection to all rows">
              <mat-icon class="text-indigo-400 hover:text-indigo-600 icon-size-4">content_copy</mat-icon>
            </button>

            <mat-icon *ngIf="row._error" class="icon-size-4 text-red-500" [matTooltip]="row._error!">
              error
            </mat-icon>
            <button mat-icon-button class="!w-7 !h-7" (click)="removeRow(i)" matTooltip="Remove row">
              <mat-icon class="text-gray-400 hover:text-red-500 icon-size-4">close</mat-icon>
            </button>
          </div>
        </div>

        <!-- Row fields -->
        <div class="px-4 py-3">
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">

            <!-- ── Student — ngx-mat-select-search ─────────────────────
                 How it works:
                 • <ngx-mat-select-search> sits as the very first child of
                   <mat-select>. It renders a real <input> INSIDE the panel
                   overlay — not inside a disabled option — so keystrokes are
                   captured natively without any workarounds.
                 • [formControl]="row._studentSearchCtrl" drives the search;
                   we subscribe to valueChanges in _makeRow() to update
                   row._filteredStudents live.
                 • (openedChange) resets the search every time the panel
                   opens so the user always starts with a full list.
            ─────────────────────────────────────────────────────────── -->
            <mat-form-field appearance="outline" class="w-full">
              <mat-label>Student <span class="text-red-500">*</span></mat-label>
              <mat-icon matPrefix class="text-gray-400">person</mat-icon>

              <mat-select
                [(ngModel)]="row.studentId"
                (ngModelChange)="onRowStudentChange(row, $event)"
                (openedChange)="onStudentSelectOpened(row, $event)">

                <!-- ngx-mat-select-search — must be the first child -->
                <mat-option>
                  <ngx-mat-select-search
                    [formControl]="row._studentSearchCtrl"
                    placeholderLabel="Search name or adm. no…"
                    noEntriesFoundLabel="No students found"
                    [clearSearchInput]="true">
                  </ngx-mat-select-search>
                </mat-option>

                <mat-option value="">— Select student —</mat-option>

                <mat-option
                  *ngFor="let s of row._filteredStudents; trackBy: trackStudentById"
                  [value]="s.id">
                  <div class="flex items-center gap-2">
                    <span class="flex items-center justify-center w-6 h-6 rounded-full flex-shrink-0
                                 text-[10px] font-bold text-white
                                 bg-gradient-to-br from-teal-400 to-emerald-500">
                      {{ getInitials(s) }}
                    </span>
                    <span class="font-medium">{{ s.firstName }} {{ s.lastName }}</span>
                    <span class="text-xs text-gray-400 ml-auto">
                      {{ s.admissionNo || s.admissionNumber || '' }}
                    </span>
                  </div>
                </mat-option>

              </mat-select>
            </mat-form-field>

            <!-- Invoice -->
            <mat-form-field appearance="outline" class="w-full">
              <mat-label>Invoice <span class="text-red-500">*</span></mat-label>
              <mat-select [(ngModel)]="row.invoiceId"
                [disabled]="!row.studentId || row._loadingInvoices">
                <mat-option value="">
                  <ng-container [ngSwitch]="true">
                    <ng-container *ngSwitchCase="row._loadingInvoices">Loading…</ng-container>
                    <ng-container *ngSwitchCase="!row.studentId">Select student first</ng-container>
                    <ng-container *ngSwitchCase="!row._loadingInvoices && row._invoices.length === 0 && !!row.studentId">
                      No invoices found
                    </ng-container>
                    <ng-container *ngSwitchDefault>— Pick invoice —</ng-container>
                  </ng-container>
                </mat-option>
                <mat-option *ngFor="let inv of row._invoices" [value]="inv.id">
                  {{ inv.invoiceNumber }}
                  <span class="text-xs text-gray-400 ml-1">
                    KES {{ formatCurrency(inv.balanceDue ?? inv.totalAmount) }}
                  </span>
                </mat-option>
              </mat-select>
              <mat-icon matPrefix class="text-gray-400">receipt_long</mat-icon>
              <mat-progress-spinner *ngIf="row._loadingInvoices"
                matSuffix mode="indeterminate" diameter="16" strokeWidth="2">
              </mat-progress-spinner>
            </mat-form-field>

            <!-- Amount -->
            <mat-form-field appearance="outline" class="w-full">
              <mat-label>Amount (KES) <span class="text-red-500">*</span></mat-label>
              <input matInput type="number" [(ngModel)]="row.amount"
                min="0.01" step="0.01" placeholder="0.00" />
              <mat-icon matPrefix class="text-gray-400">attach_money</mat-icon>
            </mat-form-field>

            <!-- M-Pesa fields -->
            <ng-container *ngIf="shared.paymentMethod === 'Mpesa'">
              <mat-form-field appearance="outline" class="w-full">
                <mat-label>M-Pesa Code <span class="text-red-500">*</span></mat-label>
                <input matInput [(ngModel)]="row.mpesaCode" maxlength="20" placeholder="QA12BX…" />
                <mat-icon matPrefix class="text-gray-400">confirmation_number</mat-icon>
              </mat-form-field>
              <mat-form-field appearance="outline" class="w-full">
                <mat-label>Phone Number</mat-label>
                <input matInput [(ngModel)]="row.phoneNumber" maxlength="20" placeholder="07…" />
                <mat-icon matPrefix class="text-gray-400">phone</mat-icon>
              </mat-form-field>
            </ng-container>

            <!-- Notes / ref -->
            <mat-form-field appearance="outline" class="w-full sm:col-span-3">
              <mat-label>Notes / Reference</mat-label>
              <input matInput [(ngModel)]="row.notes" maxlength="500"
                placeholder="Optional: transaction reference or notes for this row" />
              <mat-icon matPrefix class="text-gray-400">notes</mat-icon>
            </mat-form-field>

          </div>
        </div>

      </mat-card>
    </div>

    <!-- ════════════════════════════════════════════════
         STICKY SUBMIT FOOTER
         ════════════════════════════════════════════════ -->
    <div *ngIf="rows.length > 0" class="sticky bottom-4 z-10">
      <div class="rounded-2xl shadow-lg border border-emerald-200 dark:border-emerald-800
                  bg-white dark:bg-gray-800 px-5 py-3
                  flex flex-wrap items-center justify-between gap-3">
        <div class="flex flex-wrap gap-5">
          <div>
            <p class="text-[10px] uppercase tracking-wide text-gray-400">Rows</p>
            <p class="text-lg font-bold text-gray-900 dark:text-white">{{ rows.length }}</p>
          </div>
          <div>
            <p class="text-[10px] uppercase tracking-wide text-gray-400">Valid</p>
            <p class="text-lg font-bold"
              [class.text-green-600]="validRowCount === rows.length"
              [class.text-amber-600]="validRowCount < rows.length">
              {{ validRowCount }}/{{ rows.length }}
            </p>
          </div>
          <div>
            <p class="text-[10px] uppercase tracking-wide text-gray-400">Total</p>
            <p class="text-lg font-bold text-emerald-600">KES {{ formatCurrency(totalAmount) }}</p>
          </div>
          <div>
            <p class="text-[10px] uppercase tracking-wide text-gray-400">Method</p>
            <p class="text-sm font-bold text-gray-700 dark:text-gray-300 mt-0.5">
              {{ getMethodLabel(shared.paymentMethod) }}
            </p>
          </div>
        </div>
        <div class="flex items-center gap-3">
          <p *ngIf="!isValid && rows.length > 0"
            class="text-xs text-amber-600 dark:text-amber-400 text-right max-w-xs">
            {{ validationHint }}
          </p>
          <button mat-flat-button [disabled]="!isValid || isSubmitting" (click)="submit()"
            class="!bg-emerald-600 hover:!bg-emerald-700 !text-white !px-6">
            <mat-progress-spinner *ngIf="isSubmitting" mode="indeterminate" diameter="18"
              strokeWidth="2" class="inline-block mr-2"></mat-progress-spinner>
            <mat-icon *ngIf="!isSubmitting">send</mat-icon>
            <span class="ml-2">
              {{ isSubmitting ? 'Processing…' : 'Submit ' + rows.length + ' Payments' }}
            </span>
          </button>
        </div>
      </div>
    </div>

    <!-- ════════════════════════════════════════════════
         RESULT PANEL
         ════════════════════════════════════════════════ -->
    <mat-card *ngIf="result" class="!rounded-2xl shadow-sm border-2"
      [class.border-green-300]="result.failed === 0"
      [class.border-amber-300]="result.failed > 0 && result.succeeded > 0"
      [class.border-red-300]="result.succeeded === 0">
      <mat-card-header class="!px-5 !pt-4 !pb-3">
        <mat-card-title class="!text-base !font-bold flex items-center gap-2"
          [ngClass]="{
            'text-green-700': result.failed === 0,
            'text-amber-700': result.failed > 0 && result.succeeded > 0,
            'text-red-700':   result.succeeded === 0
          }">
          <mat-icon>
            {{ result.failed === 0 ? 'check_circle' : result.succeeded > 0 ? 'warning' : 'cancel' }}
          </mat-icon>
          {{ result.failed === 0 ? 'All payments processed!'
             : result.succeeded > 0 ? 'Partial success' : 'All payments failed' }}
          <span class="font-normal text-sm ml-1">({{ result.succeeded }}/{{ result.totalRequested }})</span>
        </mat-card-title>
      </mat-card-header>
      <mat-card-content class="!px-5 !pb-5">
        <div class="grid grid-cols-2 sm:grid-cols-4 gap-3 mb-4">
          <div class="text-center p-3 rounded-xl bg-gray-50 dark:bg-gray-800">
            <p class="text-2xl font-bold text-gray-900 dark:text-white">{{ result.totalRequested }}</p>
            <p class="text-xs text-gray-500">Requested</p>
          </div>
          <div class="text-center p-3 rounded-xl bg-green-50 dark:bg-green-900/20">
            <p class="text-2xl font-bold text-green-700">{{ result.succeeded }}</p>
            <p class="text-xs text-gray-500">Succeeded</p>
          </div>
          <div class="text-center p-3 rounded-xl bg-red-50 dark:bg-red-900/20">
            <p class="text-2xl font-bold text-red-700">{{ result.failed }}</p>
            <p class="text-xs text-gray-500">Failed</p>
          </div>
          <div class="text-center p-3 rounded-xl bg-emerald-50 dark:bg-emerald-900/20">
            <p class="text-lg font-bold text-emerald-700">KES {{ formatCurrency(result.totalAmountPosted) }}</p>
            <p class="text-xs text-gray-500">Posted</p>
          </div>
        </div>
        <div *ngIf="result.errors?.length" class="space-y-2 mb-4">
          <p class="text-sm font-semibold text-red-700 dark:text-red-400">Failed rows:</p>
          <div *ngFor="let err of result.errors"
            class="flex items-start gap-2 p-3 rounded-lg bg-red-50 dark:bg-red-900/20 text-sm">
            <mat-icon class="text-red-500 icon-size-4 flex-shrink-0 mt-0.5">error</mat-icon>
            <span class="text-red-700 dark:text-red-400">{{ err.reason }}</span>
          </div>
        </div>
        <div class="flex gap-3">
          <button mat-flat-button class="!bg-emerald-600 !text-white" (click)="goBack()">
            <mat-icon>arrow_back</mat-icon><span class="ml-1">Back to Payments</span>
          </button>
          <button *ngIf="result.failed > 0" mat-stroked-button (click)="retryFailed()">
            <mat-icon>refresh</mat-icon><span class="ml-1">Retry {{ result.failed }} Failed</span>
          </button>
        </div>
      </mat-card-content>
    </mat-card>

    <div class="h-24"></div>

  </div>
</div>
  `,
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
    isImporting = false;
    allClassSelected = false;
    someClassSelected = false;
    classImportAmount: number | null = null;
    classFilter = { cbcLevel: '', grade: '', stream: '', search: '' };

    isLoadingLookups = false;
    isSubmitting = false;
    result: BulkPaymentResultDto | null = null;

    readonly paymentMethods = [
        { value: 'Cash', label: 'Cash', icon: 'payments' },
        { value: 'Mpesa', label: 'M-Pesa', icon: 'phone_android' },
        { value: 'BankTransfer', label: 'Bank Transfer', icon: 'account_balance' },
        { value: 'Cheque', label: 'Cheque', icon: 'description' },
        { value: 'Card', label: 'Card', icon: 'credit_card' },
        { value: 'Online', label: 'Online', icon: 'language' },
    ];

    constructor(
        private _service: PaymentService,
        private _router: Router,
        private _alertService: AlertService,
        private _authService: AuthService,
    ) { }

    get isSuperAdmin(): boolean { return this._authService.authUser?.isSuperAdmin ?? false; }

    // ── Computed ──────────────────────────────────────────────────
    get validRowCount(): number { return this.rows.filter(r => this.isRowComplete(r)).length; }
    get totalAmount(): number { return this.rows.reduce((s, r) => s + (r.amount ? +r.amount : 0), 0); }

    get isValid(): boolean {
        return !!this.shared.paymentDate && !!this.shared.paymentMethod &&
            this.rows.length > 0 && this.validRowCount === this.rows.length &&
            (!this.isSuperAdmin || !!this.shared.schoolId);
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
            case 'first': return 'Auto-selects the earliest unpaid invoice per student.';
            case 'highest': return 'Auto-selects the invoice with the highest balance due.';
            case 'lowest': return 'Auto-selects the invoice with the lowest balance due.';
            default: return "Each row's invoice must be chosen manually.";
        }
    }

    isRowComplete(row: BulkRow): boolean {
        const mpesaOk = this.shared.paymentMethod !== 'Mpesa' || !!row.mpesaCode?.trim();
        return !!row.studentId && !!row.invoiceId && !!row.amount && +row.amount > 0 && mpesaOk;
    }

    // ── Class filter derived lists ────────────────────────────────
    get cbcLevels(): string[] {
        return [...new Set(
            this.importStudents.map(s => s.cbcLevel || s.currentLevel).filter(Boolean) as string[]
        )].sort();
    }

    get availableGrades(): string[] {
        return [...new Set(
            this.importStudents
                .filter(s => !this.classFilter.cbcLevel ||
                    (s.cbcLevel || s.currentLevel) === this.classFilter.cbcLevel)
                .map(s => s.grade || s.currentGrade || s.gradeLevel)
                .filter(Boolean) as string[]
        )].sort();
    }

    get availableStreams(): string[] {
        return [...new Set(
            this.importStudents
                .filter(s =>
                    (!this.classFilter.cbcLevel || (s.cbcLevel || s.currentLevel) === this.classFilter.cbcLevel) &&
                    (!this.classFilter.grade || (s.grade || s.currentGrade || s.gradeLevel) === this.classFilter.grade)
                )
                .map(s => s.stream || s.className)
                .filter(Boolean) as string[]
        )].sort();
    }

    get filteredClassStudents(): ImportStudent[] {
        const q = this.classFilter.search.toLowerCase();
        return this.importStudents.filter(s => {
            const level = s.cbcLevel || s.currentLevel || '';
            const grade = s.grade || s.currentGrade || s.gradeLevel || '';
            const stream = s.stream || s.className || '';
            const name = `${s.firstName} ${s.lastName}`.toLowerCase();
            const adm = (s.admissionNo || s.admissionNumber || '').toLowerCase();
            return (
                (!this.classFilter.cbcLevel || level === this.classFilter.cbcLevel) &&
                (!this.classFilter.grade || grade === this.classFilter.grade) &&
                (!this.classFilter.stream || stream === this.classFilter.stream) &&
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
        return !!(this.classFilter.cbcLevel || this.classFilter.grade ||
            this.classFilter.stream || this.classFilter.search);
    }

    isAlreadyAdded(id: string): boolean { return this.rows.some(r => r.studentId === id); }

    // ── Lifecycle ─────────────────────────────────────────────────
    ngOnInit(): void {
        this.isLoadingLookups = true;
        forkJoin({
            students: this._service.getStudents().pipe(catchError(() => of([]))),
            staffList: this._service.getStaff().pipe(catchError(() => of([]))),
            schools: this._service.getSchools().pipe(catchError(() => of([]))),
        })
            .pipe(takeUntil(this._destroy$), finalize(() => this.isLoadingLookups = false))
            .subscribe(d => {
                this.importStudents = (d.students as any[]).map(s => ({ ...s, selected: false }));
                this.staffList = d.staffList;
                this.schools = d.schools;
                // No default row — empty state shown until user adds/imports
            });
    }

    ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

    // ── School change ─────────────────────────────────────────────
    onSchoolChange(schoolId: string): void {
        if (!schoolId) return;
        forkJoin({
            students: this._service.getStudents(schoolId).pipe(catchError(() => of([]))),
            staffList: this._service.getStaff(schoolId).pipe(catchError(() => of([]))),
        })
            .pipe(takeUntil(this._destroy$))
            .subscribe(d => {
                this.importStudents = (d.students as any[]).map(s => ({ ...s, selected: false }));
                this.staffList = d.staffList;
                // Invalidate all rows — school changed
                this.rows.forEach(r => {
                    r.studentId = '';
                    r._studentName = '';
                    r.invoiceId = '';
                    r._invoices = [];
                    r._filteredStudents = [...this.importStudents];
                    r._studentSearchCtrl.setValue('', { emitEvent: false });
                });
            });
    }

    onMethodChange(): void {
        if (this.shared.paymentMethod !== 'Mpesa') {
            this.rows.forEach(r => { r.mpesaCode = ''; r.phoneNumber = ''; });
        }
    }

    // ── OPTION C: Invoice strategy ────────────────────────────────
    onStrategyChange(): void {
        if (this.shared.invoiceStrategy === 'none' || this.rows.length === 0) return;
        this.applyStrategyToAllRows();
    }

    applyStrategyToAllRows(): void {
        let changed = 0;
        this.rows.forEach(row => {
            if (!row._invoices.length) return;
            const picked = this.pickInvoiceByStrategy(row._invoices);
            if (picked && picked !== row.invoiceId) { row.invoiceId = picked; changed++; }
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
                // Assumes list is date-ordered ascending from the API
                return invoices[0].id;
            case 'highest': {
                const best = invoices.reduce((a, b) =>
                    (b.balanceDue ?? b.totalAmount) > (a.balanceDue ?? a.totalAmount) ? b : a);
                return best.id;
            }
            case 'lowest': {
                const best = invoices.reduce((a, b) =>
                    (b.balanceDue ?? b.totalAmount) < (a.balanceDue ?? a.totalAmount) ? b : a);
                return best.id;
            }
            default:
                // 'none' — only auto-select when there is exactly one invoice
                return invoices.length === 1 ? invoices[0].id : '';
        }
    }

    // ── OPTION B: Apply one row's invoice to all ──────────────────
    applyInvoiceToAll(sourceRow: BulkRow): void {
        const sourceInv = sourceRow._invoices.find(x => x.id === sourceRow.invoiceId);
        if (!sourceInv) return;
        let changed = 0;
        this.rows.forEach(row => {
            if (row === sourceRow || !row._invoices.length) return;
            // Match by exact ID first, then fall back to matching invoice number
            const match =
                row._invoices.find((x: any) => x.id === sourceInv.id) ??
                row._invoices.find((x: any) => x.invoiceNumber === sourceInv.invoiceNumber);
            if (match && match.id !== row.invoiceId) { row.invoiceId = match.id; changed++; }
        });
        const msg = changed > 0
            ? `Invoice "${sourceInv.invoiceNumber}" applied to ${changed} other row${changed !== 1 ? 's' : ''}.`
            : `No other rows have a matching invoice for "${sourceInv.invoiceNumber}".`;
        changed > 0 ? this._alertService.success(msg) : this._alertService.info(msg);
    }

    // ── Class import ──────────────────────────────────────────────
    toggleClassPanel(): void { this.showClassImport = !this.showClassImport; }

    onClassFilterChange(changed: 'cbcLevel' | 'grade' | 'stream' | 'search'): void {
        if (changed === 'cbcLevel') { this.classFilter.grade = ''; this.classFilter.stream = ''; }
        else if (changed === 'grade') { this.classFilter.stream = ''; }
        const visibleIds = new Set(this.filteredClassStudents.map(s => s.id));
        this.importStudents.forEach(s => { if (!visibleIds.has(s.id)) s.selected = false; });
        this.updateSelectAllState();
    }

    clearClassFilters(): void {
        this.classFilter = { cbcLevel: '', grade: '', stream: '', search: '' };
        this.updateSelectAllState();
    }

    toggleStudent(s: ImportStudent): void { s.selected = !s.selected; this.updateSelectAllState(); }

    toggleSelectAll(checked: boolean): void {
        this.filteredClassStudents.forEach(s => s.selected = checked);
        this.updateSelectAllState();
    }

    updateSelectAllState(): void {
        const f = this.filteredClassStudents;
        const count = f.filter(s => s.selected).length;
        this.allClassSelected = f.length > 0 && count === f.length;
        this.someClassSelected = count > 0 && count < f.length;
    }

    importSelectedStudents(): void {
        const toImport = this.filteredClassStudents
            .filter(s => s.selected && !this.isAlreadyAdded(s.id));

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
            .pipe(takeUntil(this._destroy$), finalize(() => this.isImporting = false))
            .subscribe((invoicesPerStudent: any[]) => {
                toImport.forEach((student, idx) => {
                    const invoices = invoicesPerStudent[idx] as any[];
                    const autoInvoice = invoices.length === 0
                        ? ''
                        : invoices.length === 1
                            ? invoices[0].id
                            : this.pickInvoiceByStrategy(invoices);

                    this.rows.push(this._makeRow({
                        studentId: student.id,
                        invoiceId: autoInvoice,
                        amount: this.classImportAmount ?? null,
                        _studentName: `${student.firstName} ${student.lastName}`,
                        _invoices: invoices,
                    }));
                });

                toImport.forEach(s => s.selected = false);
                this.updateSelectAllState();
                this._alertService.success(
                    `${toImport.length} student${toImport.length !== 1 ? 's' : ''} imported successfully.`
                );
                this.showClassImport = false;
            });
    }

    // ── Row management ────────────────────────────────────────────

    /**
     * Factory — creates a BulkRow with a dedicated FormControl for the search
     * input and wires up valueChanges to filter _filteredStudents reactively.
     */
    private _makeRow(overrides: Partial<BulkRow> = {}): BulkRow {
        const ctrl = new FormControl<string>('');

        const row: BulkRow = {
            studentId: '',
            invoiceId: '',
            amount: null,
            mpesaCode: '',
            phoneNumber: '',
            transactionReference: '',
            notes: '',
            _studentName: '',
            _studentSearchCtrl: ctrl,
            _filteredStudents: [...this.importStudents],
            _invoices: [],
            _loadingInvoices: false,
            ...overrides,
        };

        // Live filtering — triggered every time the user types in ngx-mat-select-search
        ctrl.valueChanges
            .pipe(takeUntil(this._destroy$))
            .subscribe(q => {
                const term = (q ?? '').toLowerCase().trim();
                row._filteredStudents = term
                    ? this.importStudents.filter(s =>
                        `${s.firstName} ${s.lastName}`.toLowerCase().includes(term) ||
                        (s.admissionNo || s.admissionNumber || '').toLowerCase().includes(term))
                    : [...this.importStudents];
            });

        return row;
    }

    addRow(): void { this.rows.push(this._makeRow()); }

    removeRow(i: number): void { this.rows.splice(i, 1); }

    clearAll(): void {
        this._alertService.confirm({
            title: 'Clear All Rows',
            message: 'Remove all payment rows?',
            confirmText: 'Clear All',
            onConfirm: () => { this.rows = []; this.result = null; },
        });
    }

    trackByIndex(i: number): number { return i; }
    trackById(_: number, s: ImportStudent): string { return s.id; }
    trackStudentById(_: number, s: any): string { return s.id; }

    // ── Row handlers ──────────────────────────────────────────────

    /**
     * Resets the search input every time the student panel opens
     * so the user always starts with a full unfiltered list.
     */
    onStudentSelectOpened(row: BulkRow, isOpen: boolean): void {
        if (isOpen) {
            row._studentSearchCtrl.setValue('', { emitEvent: true });
        }
    }

    onRowStudentChange(row: BulkRow, studentId: string): void {
        row.invoiceId = '';
        row._invoices = [];
        row._error = undefined;
        row._studentName = '';
        if (!studentId) return;

        const found = this.importStudents.find(s => s.id === studentId);
        if (found) row._studentName = `${found.firstName} ${found.lastName}`;

        row._loadingInvoices = true;
        this._service
            .getInvoicesByStudent(studentId, this.shared.schoolId || undefined)
            .pipe(takeUntil(this._destroy$), finalize(() => row._loadingInvoices = false))
            .subscribe({
                next: inv => {
                    row._invoices = inv;
                    row.invoiceId = inv.length === 1
                        ? inv[0].id
                        : this.pickInvoiceByStrategy(inv);
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
        return [s.cbcLevel || s.currentLevel,
        s.grade || s.currentGrade || s.gradeLevel,
        s.stream || s.className]
            .filter(Boolean).join(' · ');
    }

    getInitials(s: ImportStudent): string {
        return `${(s.firstName || '')[0] ?? ''}${(s.lastName || '')[0] ?? ''}`.toUpperCase();
    }

    // ── Submit ────────────────────────────────────────────────────
    submit(): void {
        if (!this.isValid) return;
        this.isSubmitting = true;
        this.result = null;

        const dto: BulkPaymentDto = {
            tenantId: this.shared.schoolId || undefined,
            paymentDate: this.shared.paymentDate,
            paymentMethod: PaymentMethodValue[this.shared.paymentMethod],
            statusPayment: PaymentStatusValue[this.shared.statusPayment],
            receivedBy: this.shared.receivedBy || undefined,
            description: this.shared.description || undefined,
            bankName: this.shared.bankName || undefined,
            accountNumber: this.shared.accountNumber || undefined,
            payments: this.rows.map(r => ({
                studentId: r.studentId,
                invoiceId: r.invoiceId,
                amount: +r.amount!,
                mpesaCode: r.mpesaCode || undefined,
                phoneNumber: r.phoneNumber || undefined,
                transactionReference: r.transactionReference || undefined,
                notes: r.notes || undefined,
            } as BulkPaymentItemDto)),
        };

        this._service.bulkCreate(dto)
            .pipe(takeUntil(this._destroy$), finalize(() => this.isSubmitting = false))
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
        this.rows.forEach(r => r._error = undefined);
        this.result = null;
    }

    // ── Helpers ───────────────────────────────────────────────────
    getMethodIcon(m: string): string {
        return ({
            Cash: 'payments', Mpesa: 'phone_android', BankTransfer: 'account_balance',
            Cheque: 'description', Card: 'credit_card', Online: 'language'
        } as any)[m] ?? 'payments';
    }

    getMethodLabel(m: string): string {
        return ({
            Cash: 'Cash', Mpesa: 'M-Pesa', BankTransfer: 'Bank Transfer',
            Cheque: 'Cheque', Card: 'Card', Online: 'Online'
        } as any)[m] ?? m;
    }

    formatCurrency(v: number): string {
        return (v ?? 0).toLocaleString('en-KE', { minimumFractionDigits: 2 });
    }

    goBack(): void { this._router.navigate(['/finance/payments']); }
}