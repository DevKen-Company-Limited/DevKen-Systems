// ═══════════════════════════════════════════════════════════════════
// pesapal-callback.component.ts
// Handles PesaPal redirect after payment (callback_url page).
// PesaPal appends: ?OrderTrackingId=xxx&OrderMerchantReference=yyy&OrderNotificationType=IPNCHANGE
// ═══════════════════════════════════════════════════════════════════

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { switchMap, catchError } from 'rxjs/operators';
import { of } from 'rxjs';

import { PesaPalService } from '../../../core/DevKenService/payments/pesapal.service';
import { PesaPalStatusResponse } from '../pesapal.types';

@Component({
  selector: 'app-pesapal-callback',
  standalone: true,
  templateUrl: './pesapal-callback.component.html',
  imports: [CommonModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
})
export class PesaPalCallbackComponent implements OnInit {

  isLoading = true;
  status: PesaPalStatusResponse | null = null;
  error: string | null = null;

  constructor(
    private _route: ActivatedRoute,
    private _router: Router,
    private _service: PesaPalService,
  ) {}

  ngOnInit(): void {
    const trackingId = this._route.snapshot.queryParamMap.get('OrderTrackingId');

    if (!trackingId) {
      this.isLoading = false;
      this.error     = 'No tracking ID found in callback URL.';
      return;
    }

    this._service.getTransactionStatus(trackingId)
      .pipe(catchError(err => {
        this.error     = err?.message ?? 'Failed to verify payment status.';
        this.isLoading = false;
        return of(null);
      }))
      .subscribe(res => {
        this.status    = res;
        this.isLoading = false;
      });
  }

  get isSuccess(): boolean {
    return this.status?.payment_status_description === 'COMPLETED';
  }

  goToPayments(): void {
    this._router.navigate(['/finance/payments']);
  }
}