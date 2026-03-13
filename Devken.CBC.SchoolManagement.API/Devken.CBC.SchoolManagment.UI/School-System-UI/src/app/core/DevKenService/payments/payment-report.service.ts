// ═══════════════════════════════════════════════════════════════════
// payment-report.service.ts
// Mirrors AssessmentReportService pattern
// ═══════════════════════════════════════════════════════════════════

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { API_BASE_URL } from 'app/app.config';

interface ReportResult {
  success: boolean;
  message?: string;
  fileUrl?: string;
}

@Injectable({ providedIn: 'root' })
export class PaymentReportService {

  private readonly _apiBase = inject(API_BASE_URL);
  private readonly _http    = inject(HttpClient);
  private readonly _base    = `${this._apiBase}/api/finance/payments/reports`;

  // ── Download individual receipt PDF ───────────────────────────────
  downloadReceipt(paymentId: string): Observable<ReportResult> {
    return this._http
      .post<ReportResult>(`${this._base}/receipt`, { paymentId })
      .pipe(map(r => r));
  }

  // ── Download payments list PDF ────────────────────────────────────
  downloadPaymentsList(filters: {
    paymentMethod?: string | null;
    status?:        string | null;
    from?:          string | null;
    to?:            string | null;
    schoolId?:      string | null;
    studentId?:     string | null;
  }): Observable<ReportResult> {
    const body: any = {};
    if (filters.paymentMethod) body.paymentMethod = filters.paymentMethod;
    if (filters.status)        body.status        = filters.status;
    if (filters.from)          body.from          = filters.from;
    if (filters.to)            body.to            = filters.to;
    if (filters.schoolId)      body.schoolId      = filters.schoolId;
    if (filters.studentId)     body.studentId     = filters.studentId;

    return this._http
      .post<ReportResult>(`${this._base}/list`, body)
      .pipe(map(r => r));
  }

  // ── Download summary PDF ──────────────────────────────────────────
  downloadPaymentsSummary(filters: {
    from?:      string | null;
    to?:        string | null;
    schoolId?:  string | null;
    studentId?: string | null;
  }): Observable<ReportResult> {
    const body: any = {};
    if (filters.from)      body.from      = filters.from;
    if (filters.to)        body.to        = filters.to;
    if (filters.schoolId)  body.schoolId  = filters.schoolId;
    if (filters.studentId) body.studentId = filters.studentId;

    return this._http
      .post<ReportResult>(`${this._base}/summary`, body)
      .pipe(map(r => r));
  }
}