// ═══════════════════════════════════════════════════════════════════
// pesapal.service.ts  (updated — calls .NET API proxy)
//
// All PesaPal credentials stay server-side.
// Angular only calls OUR OWN .NET endpoints:
//
//   POST /api/pesapal/order/submit   → { redirectUrl, orderTrackingId, merchantReference }
//   GET  /api/pesapal/order/status   → PesaPalStatusResponse
//   POST /api/pesapal/ipn/register   → { ipnId }
//   GET  /api/pesapal/ipn/list       → PesaPalIPNResponse[]
//
// JWT auth is handled transparently by the existing Angular interceptor.
// ═══════════════════════════════════════════════════════════════════

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { API_BASE_URL } from 'app/app.config';

import {
    SubmitOrderRequestDto,
    PesaPalOrderResponse,
    PesaPalStatusResponse,
    PesaPalIpnRequest,
} from '../../../payments/pesaPall/pesapal.types';

// ── API envelope ──────────────────────────────────────────────────
interface ApiResponse<T> {
    success: boolean;
    message: string;
    data:    T;
}

// ── Raw data shape returned by /order/submit ──────────────────────
interface SubmitOrderData {
    redirectUrl:       string;
    orderTrackingId:   string;
    merchantReference: string;
}

@Injectable({ providedIn: 'root' })
export class PesaPalService {

    private readonly _apiBase = inject(API_BASE_URL);
    private readonly _http    = inject(HttpClient);
    private readonly _base    = `${this._apiBase}/api/pesapal`;

    // ── IPN ────────────────────────────────────────────────────────

    /**
     * Registers the IPN URL on our .NET backend (idempotent — cached per process).
     * Call once at app startup or before the first order submission.
     */
    registerIPN(): Observable<string> {
        return this._http
            .post<ApiResponse<{ ipnId: string }>>(`${this._base}/ipn/register`, {})
            .pipe(map(r => r.data.ipnId));
    }

    /** List all IPN URLs registered under the current PesaPal credentials. */
    getRegisteredIPNs(): Observable<PesaPalIpnRequest[]> {
        return this._http
            .get<ApiResponse<PesaPalIpnRequest[]>>(`${this._base}/ipn/list`)
            .pipe(map(r => r.data ?? []));
    }

    // ── Order ──────────────────────────────────────────────────────

    /**
     * Submit a payment order via our .NET proxy.
     * Returns { redirect_url, order_tracking_id, merchant_reference }.
     * Open redirect_url in an iframe for the hosted checkout.
     */
    submitOrder(dto: SubmitOrderRequestDto): Observable<PesaPalOrderResponse> {
        return this._http
            .post<ApiResponse<SubmitOrderData>>(`${this._base}/order/submit`, dto)
            .pipe(
                map(r => ({
                    redirect_url:       r.data.redirectUrl,
                    order_tracking_id:  r.data.orderTrackingId,
                    merchant_reference: r.data.merchantReference,
                } as PesaPalOrderResponse)),
            );
    }

    // ── Status polling ─────────────────────────────────────────────

    /**
     * Query PesaPal transaction status via our .NET proxy.
     * Poll this with takeUntil after the user clicks "I've Paid".
     */
    getTransactionStatus(orderTrackingId: string): Observable<PesaPalStatusResponse> {
        const params = new HttpParams().set('orderTrackingId', orderTrackingId);
        return this._http
            .get<ApiResponse<PesaPalStatusResponse>>(`${this._base}/order/status`, { params })
            .pipe(map(r => r.data));
    }

    // ── Helpers ────────────────────────────────────────────────────

    /** Generate a unique merchant reference for a new order. */
    generateMerchantRef(prefix = 'PAY'): string {
        return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2, 7).toUpperCase()}`;
    }
}