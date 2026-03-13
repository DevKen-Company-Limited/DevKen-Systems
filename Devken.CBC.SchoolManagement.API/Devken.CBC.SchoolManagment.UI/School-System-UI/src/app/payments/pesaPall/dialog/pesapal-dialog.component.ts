import {
    Component, OnInit, OnDestroy, Inject,
    ChangeDetectionStrategy, ChangeDetectorRef, NgZone,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { Subject, interval } from 'rxjs';
import { takeUntil, switchMap, take } from 'rxjs/operators';

import { PesaPalService } from '../../../core/DevKenService/payments/pesapal.service';
import {
    PesaPalDialogData,
    PesaPalDialogResult,
    SubmitOrderRequestDto,
    PesaPalPaymentStatus,
} from '../pesapal.types';

@Component({
    selector:        'app-pesapal-dialog',
    standalone:      true,
    templateUrl:     './pesapal-dialog.component.html',
    styleUrls:       ['./pesapal-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        MatDialogModule,
        MatButtonModule,
        MatIconModule,
        MatProgressSpinnerModule,
    ],
})
export class PesaPalDialogComponent implements OnInit, OnDestroy {

    private _destroy$ = new Subject<void>();

    // ── Checkout ─────────────────────────────────────────────
    step: 'checkout' | 'result' = 'checkout';
    checkoutUrl: SafeResourceUrl | null = null;
    orderTrackingId: string | null = null;
    isLoadingCheckout = false;
    iframeLoaded = false;

    // ── Result ─────────────────────────────────────────────
    result: PesaPalDialogResult | null = null;
    isPolling = false;
    pollAttempts = 0;
    readonly MAX_POLL = 20;
    errorMessage: string | null = null;

    constructor(
        public  dialogRef: MatDialogRef<PesaPalDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: PesaPalDialogData,
        private _service:   PesaPalService,
        private _sanitizer: DomSanitizer,
        private _cdr:       ChangeDetectorRef,
        private _zone:      NgZone,
    ) {}

    ngOnInit(): void {
        // Pre-warm IPN registration
        this._service.registerIPN().pipe(take(1)).subscribe();

        // Directly start checkout
        this.submitOrder();
    }

    ngOnDestroy(): void {
        this._destroy$.next();
        this._destroy$.complete();
    }

    // ── Checkout ─────────────────────────────────────────────
    submitOrder(): void {
        const dto: SubmitOrderRequestDto = {
            id: this.data.merchantReference || this._service.generateMerchantRef(),
            currency: 'KES',
            amount: this.data.amount,
            description: this.data.description,
            branch: "001",
            billing_address: {
                email_address: this.data.email ?? '',
                phone_number:  this.data.phone ?? '',
                country_code:  'KE',
                first_name:    this.data.firstName ?? '',
                last_name:     this.data.lastName ?? '',
            },
        };

        this.isLoadingCheckout = true;
        this._cdr.markForCheck();

        this._service.submitOrder(dto)
            .pipe(takeUntil(this._destroy$))
            .subscribe({
                next: res => {
                    this.orderTrackingId   = res.order_tracking_id;
                    this.checkoutUrl       = this._sanitizer.bypassSecurityTrustResourceUrl(res.redirect_url);
                    this.isLoadingCheckout = false;
                    this._cdr.markForCheck();
                },
                error: err => {
                    this.isLoadingCheckout = false;
                    this.errorMessage      = err?.error?.message ?? err?.message
                                            ?? 'Failed to initiate payment. Please try again.';
                    this.step = 'result';
                    this._cdr.markForCheck();
                },
            });
    }

    onIframeLoad(): void {
        this.iframeLoaded = true;
        this._cdr.markForCheck();
    }

    // ── Polling for payment status ─────────────────────────
    checkPaymentStatus(): void {
        if (!this.orderTrackingId) return;

        this.isPolling    = true;
        this.pollAttempts = 0;
        this._cdr.markForCheck();

        interval(3_000)
            .pipe(
                take(this.MAX_POLL),
                switchMap(() => this._service.getTransactionStatus(this.orderTrackingId!)),
                takeUntil(this._destroy$),
            )
            .subscribe({
                next: status => {
                    this.pollAttempts++;
                    const s = status.payment_status_description as PesaPalPaymentStatus;

                    if (['COMPLETED','FAILED','INVALID','REVERSED'].includes(s)) {
                        this.isPolling = false;
                        this.result = {
                            success:          s === 'COMPLETED',
                            orderTrackingId:  status.order_tracking_id,
                            confirmationCode: status.confirmation_code,
                            paymentMethod:    status.payment_method,
                            amount:           status.amount,
                            status:           s,
                        };
                        this.step = 'result';
                        this._cdr.markForCheck();
                    } else if (this.pollAttempts >= this.MAX_POLL) {
                        this.isPolling = false;
                        this.result = {
                            success: false,
                            status:  'PENDING',
                            error:   'Payment is still pending. Please check back later.',
                        };
                        this.step = 'result';
                        this._cdr.markForCheck();
                    }
                },
                error: () => {
                    this.isPolling = false;
                    this._cdr.markForCheck();
                },
            });
    }

    // ── Result / Close ─────────────────────────────────────
    close(): void {
        this.dialogRef.close(
            this.result ?? ({ success: false, error: 'Cancelled' } as PesaPalDialogResult));
    }

    retry(): void {
        this.result = null;
        this.checkoutUrl = null;
        this.orderTrackingId = null;
        this.iframeLoaded = false;
        this.errorMessage = null;
        this.isLoadingCheckout = false;

        this.submitOrder();
    }
}