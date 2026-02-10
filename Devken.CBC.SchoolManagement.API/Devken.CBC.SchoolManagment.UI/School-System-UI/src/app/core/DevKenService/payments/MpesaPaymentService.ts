import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, interval, of } from 'rxjs';
import { catchError, map, switchMap, take, takeWhile } from 'rxjs/operators';
import { API_BASE_URL } from 'app/app.config';

export interface MpesaPaymentRequest {
  phoneNumber: string;
  amount: number;
  accountReference: string;
  transactionDesc: string;
}

export interface MpesaPaymentResponse {
  success: boolean;
  message: string;
  checkoutRequestId?: string;
  merchantRequestId?: string;
  responseCode?: string;
  responseDescription?: string;
  customerMessage?: string;
}

export interface MpesaCallbackStatus {
  success: boolean;
  resultCode: number;
  resultDesc: string;
  checkoutRequestId: string;
  mpesaReceiptNumber?: string;
  transactionDate?: string;
  phoneNumber?: string;
  amount?: number;
}

@Injectable({
  providedIn: 'root'
})
export class MpesaPaymentService {
  private readonly _http = inject(HttpClient);
  private readonly _apiBase = inject(API_BASE_URL);
  private readonly _url = `${this._apiBase}/api/mpesa`;

  /**
   * Initiate M-Pesa STK Push
   */
  initiatePayment(request: MpesaPaymentRequest): Observable<MpesaPaymentResponse> {
    return this._http.post<any>(`${this._url}/initiate`, request).pipe(
      map(response => ({
        success: response.success || false,
        message: response.message || 'Payment initiated',
        checkoutRequestId: response.data?.checkoutRequestId,
        merchantRequestId: response.data?.merchantRequestId,
        responseCode: response.data?.responseCode,
        responseDescription: response.data?.responseDescription,
        customerMessage: response.data?.customerMessage
      })),
      catchError(error => {
        console.error('M-Pesa initiation error:', error);
        return of({
          success: false,
          message: error.error?.message || 'Failed to initiate payment',
          responseCode: error.error?.responseCode
        });
      })
    );
  }

  /**
   * Check payment status
   */
  checkPaymentStatus(checkoutRequestId: string): Observable<MpesaCallbackStatus> {
    return this._http.get<any>(`${this._url}/status/${checkoutRequestId}`).pipe(
      map(response => ({
        success: response.success || false,
        resultCode: response.data?.resultCode || -1,
        resultDesc: response.data?.resultDesc || 'Unknown status',
        checkoutRequestId: response.data?.checkoutRequestId || checkoutRequestId,
        mpesaReceiptNumber: response.data?.mpesaReceiptNumber,
        transactionDate: response.data?.transactionDate,
        phoneNumber: response.data?.phoneNumber,
        amount: response.data?.amount
      })),
      catchError(error => {
        console.error('Status check error:', error);
        return of({
          success: false,
          resultCode: -1,
          resultDesc: error.error?.message || 'Failed to check status',
          checkoutRequestId
        });
      })
    );
  }

  /**
   * Poll payment status until completion or timeout
   * @param checkoutRequestId The checkout request ID from STK push
   * @param maxAttempts Maximum number of polling attempts (default: 40 = 2 minutes)
   * @param intervalMs Polling interval in milliseconds (default: 3000 = 3 seconds)
   */
  pollPaymentStatus(
    checkoutRequestId: string,
    maxAttempts: number = 40,
    intervalMs: number = 3000
  ): Observable<MpesaCallbackStatus> {
    let attempts = 0;

    return interval(intervalMs).pipe(
      switchMap(() => this.checkPaymentStatus(checkoutRequestId)),
      takeWhile(status => {
        attempts++;
        // Continue polling if:
        // 1. Payment is still pending (resultCode === -1 or undefined)
        // 2. We haven't exceeded max attempts
        const isPending = !status.resultCode || status.resultCode === -1;
        const shouldContinue = isPending && attempts < maxAttempts;
        
        return shouldContinue;
      }, true), // Include the final emission
      take(maxAttempts),
      catchError(error => {
        console.error('Polling error:', error);
        return of({
          success: false,
          resultCode: -1,
          resultDesc: 'Polling failed',
          checkoutRequestId
        });
      })
    );
  }

  /**
   * Format phone number to M-Pesa format (254...)
   */
  formatPhoneNumber(phone: string): string {
    // Remove spaces, dashes, and plus signs
    let cleaned = phone.replace(/[\s\-+]/g, '');
    
    // If starts with 0, replace with 254
    if (cleaned.startsWith('0')) {
      cleaned = '254' + cleaned.substring(1);
    }
    
    // If doesn't start with 254, add it
    if (!cleaned.startsWith('254')) {
      cleaned = '254' + cleaned;
    }
    
    return cleaned;
  }

  /**
   * Validate phone number
   */
  isValidPhoneNumber(phone: string): boolean {
    const formatted = this.formatPhoneNumber(phone);
    // Kenyan phone numbers should be 12 digits starting with 254
    return /^254[17]\d{8}$/.test(formatted);
  }

  /**
   * Get result description for result code
   */
  getResultDescription(resultCode: number): string {
    switch (resultCode) {
      case 0:
        return 'Payment successful';
      case 1:
        return 'Insufficient funds';
      case 1032:
        return 'Transaction cancelled by user';
      case 1037:
        return 'Timeout - user did not enter PIN';
      case 2001:
        return 'Invalid initiator information';
      case 1:
        return 'Balance insufficient';
      default:
        return resultCode === -1 ? 'Payment pending' : 'Payment failed';
    }
  }
}