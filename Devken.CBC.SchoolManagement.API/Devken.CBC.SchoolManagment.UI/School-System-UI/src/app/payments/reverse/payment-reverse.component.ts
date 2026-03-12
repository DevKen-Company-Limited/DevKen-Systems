import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule }           from '@angular/common';
import { FormsModule }            from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatButtonModule }        from '@angular/material/button';
import { MatFormFieldModule }     from '@angular/material/form-field';
import { MatInputModule }         from '@angular/material/input';
import { MatIconModule }          from '@angular/material/icon';
import { Subject }                from 'rxjs';
import { takeUntil }              from 'rxjs/operators';
import { PaymentService }         from 'app/core/DevKenService/payments/payment.service';
import { AlertService }           from 'app/core/DevKenService/Alert/AlertService';
import { ReversePaymentDto }      from '../types/payments';

@Component({
  selector: 'app-payment-reverse',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatButtonModule, MatFormFieldModule, MatInputModule, MatIconModule,
  ],
  template: `
<div class="flex flex-col h-full w-full p-8 bg-white dark:bg-gray-900">

  <div class="flex items-center gap-4 mb-8">
    <div class="flex items-center justify-center w-12 h-12 rounded-xl
                bg-gradient-to-br from-rose-500 to-red-600 shadow-lg">
      <mat-icon class="text-white">undo</mat-icon>
    </div>
    <div>
      <h2 class="text-xl font-bold text-gray-900 dark:text-white">Reverse Payment</h2>
      <p class="text-sm text-gray-500 dark:text-gray-400">
        Payment ID: <span class="font-mono text-xs">{{ paymentId }}</span>
      </p>
    </div>
  </div>

  <div class="p-4 rounded-xl bg-rose-50 border border-rose-200
              dark:bg-rose-900/20 dark:border-rose-800 mb-6">
    <div class="flex items-start gap-3">
      <mat-icon class="text-rose-500 mt-0.5 flex-shrink-0">warning</mat-icon>
      <p class="text-sm text-rose-700 dark:text-rose-300">
        This will create a counter-entry reversal and mark the original payment
        as <strong>Reversed</strong>. This action cannot be undone.
      </p>
    </div>
  </div>

  <mat-form-field appearance="outline" class="w-full flex-1">
    <mat-label>Reversal Reason <span class="text-red-500">*</span></mat-label>
    <textarea matInput [(ngModel)]="reversalReason" rows="6" maxlength="500"
      class="h-full"
      placeholder="State the reason for reversing this payment…"></textarea>
    <mat-hint align="end">{{ reversalReason.length }} / 500</mat-hint>
  </mat-form-field>

  <div class="flex gap-3 mt-6">
    <button mat-stroked-button class="flex-1" (click)="goBack()" [disabled]="isSubmitting">
      Cancel
    </button>
    <button mat-flat-button color="warn" class="flex-1"
      [disabled]="!reversalReason.trim() || isSubmitting"
      (click)="submit()">
      <mat-icon *ngIf="!isSubmitting">undo</mat-icon>
      <mat-spinner *ngIf="isSubmitting" diameter="18" class="inline-block mr-2"></mat-spinner>
      {{ isSubmitting ? 'Reversing…' : 'Confirm Reversal' }}
    </button>
  </div>

</div>
  `,
})
export class PaymentReverseComponent implements OnInit, OnDestroy {

  paymentId    = '';
  reversalReason = '';
  isSubmitting = false;

  private _destroy$ = new Subject<void>();

  constructor(
    private _service:       PaymentService,
    private _router:        Router,
    private _route:         ActivatedRoute,
    private _alertService:  AlertService,
  ) {}

  ngOnInit(): void {
    this.paymentId = this._route.snapshot.paramMap.get('id') ?? '';
    if (!this.paymentId) {
      this._alertService.error('Invalid payment ID.');
      this.goBack();
    }
  }

  ngOnDestroy(): void { this._destroy$.next(); this._destroy$.complete(); }

  submit(): void {
    if (!this.reversalReason.trim() || this.isSubmitting) return;
    this.isSubmitting = true;

    const dto: ReversePaymentDto = { reversalReason: this.reversalReason.trim() };

    this._service.reverse(this.paymentId, dto)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: () => {
          this._alertService.success('Payment reversed successfully.');
          setTimeout(() => this._router.navigate(['/finance/payments']), 1200);
        },
        error: err => {
          this._alertService.error(err?.error?.message ?? 'Reversal failed.');
          this.isSubmitting = false;
        },
      });
  }

  goBack(): void { this._router.navigate(['/finance/payments']); }
}