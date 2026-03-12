// ═══════════════════════════════════════════════════════════════════
// payment.service.ts
// ═══════════════════════════════════════════════════════════════════

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { API_BASE_URL } from 'app/app.config';
import {
    PaymentMethod, PaymentStatus,
    PaymentResponseDto, PaymentSummaryDto,
    CreatePaymentDto, UpdatePaymentDto,
    ReversePaymentDto, BulkPaymentDto, BulkPaymentResultDto,
    PaymentPagedResultDto,
} from 'app/payments/types/payments';

interface ApiResponse<T> {
    success: boolean;
    message: string;
    data: T;
}

// Shared filter shape used by both getAll and getPaged
interface PaymentFilters {
    schoolId?:   string;
    studentId?:  string;
    invoiceId?:  string;
    method?:     string;
    status?:     string;
    from?:       string;
    to?:         string;
    isReversal?: boolean;
    search?:     string;
    page?:       number;
    pageSize?:   number;
}

@Injectable({ providedIn: 'root' })
export class PaymentService {

    private readonly _apiBase = inject(API_BASE_URL);
    private readonly _http    = inject(HttpClient);
    private readonly _base    = `${this._apiBase}/api/finance/payments`;

    // ── Shared param builder ──────────────────────────────────────────
    private _buildParams(filters: PaymentFilters, includePaging = true): HttpParams {
        let params = new HttpParams();
        const f = filters;
        if (f.schoolId)           params = params.set('schoolId',   f.schoolId);
        if (f.studentId)          params = params.set('studentId',  f.studentId);
        if (f.invoiceId)          params = params.set('invoiceId',  f.invoiceId);
        if (f.method)             params = params.set('method',     f.method);
        if (f.status)             params = params.set('status',     f.status);
        if (f.from)               params = params.set('from',       f.from);
        if (f.to)                 params = params.set('to',         f.to);
        if (f.isReversal != null) params = params.set('isReversal', String(f.isReversal));
        if (f.search)             params = params.set('search',     f.search);

        if (includePaging) {
            params = params.set('page',     String(f.page     ?? 1));
            params = params.set('pageSize', String(f.pageSize ?? 20));
        }
        return params;
    }

    // ──────────────────────────────────────────────────────────────
    // QUERIES
    // ──────────────────────────────────────────────────────────────

    /**
     * GET /api/finance/payments?page=&pageSize=&...
     *
     * Primary list endpoint used by the payments list page.
     * Returns a paged + paired-sorted result with full stats.
     * Page and pageSize are always sent so the server applies the
     * correct Skip/Take after the in-memory paired sort.
     */
    getPaged(filters?: PaymentFilters): Observable<PaymentPagedResultDto> {
        const params = this._buildParams(filters ?? {}, true);
        return this._http
            .get<ApiResponse<PaymentPagedResultDto>>(this._base, { params })
            .pipe(map(r => r.data));
    }

    /**
     * GET /api/finance/payments?page=1&pageSize=9999&...
     *
     * Convenience wrapper that fetches all records in a single call.
     * Use only where the full list is needed (e.g. export, reports).
     * For the list page always use getPaged() instead.
     */
    getAll(filters?: Omit<PaymentFilters, 'page' | 'pageSize'>): Observable<PaymentPagedResultDto> {
        const params = this._buildParams({ ...filters, page: 1, pageSize: 9999 }, true);
        return this._http
            .get<ApiResponse<PaymentPagedResultDto>>(this._base, { params })
            .pipe(map(r => r.data));
    }

    getById(id: string): Observable<PaymentResponseDto> {
        return this._http
            .get<ApiResponse<PaymentResponseDto>>(`${this._base}/${id}`)
            .pipe(map(r => r.data));
    }

    getByReference(reference: string): Observable<PaymentResponseDto> {
        return this._http
            .get<ApiResponse<PaymentResponseDto>>(
                `${this._base}/by-reference/${encodeURIComponent(reference)}`)
            .pipe(map(r => r.data));
    }

    getByStudent(studentId: string): Observable<PaymentResponseDto[]> {
        return this._http
            .get<ApiResponse<PaymentResponseDto[]>>(`${this._base}/student/${studentId}`)
            .pipe(map(r => r.data ?? []));
    }

    getByInvoice(invoiceId: string): Observable<PaymentResponseDto[]> {
        return this._http
            .get<ApiResponse<PaymentResponseDto[]>>(`${this._base}/invoice/${invoiceId}`)
            .pipe(map(r => r.data ?? []));
    }

    getSummary(
        schoolId?:  string,
        studentId?: string,
        from?:      Date | string,
        to?:        Date | string,
    ): Observable<PaymentSummaryDto> {
        let params = new HttpParams();
        if (schoolId)  params = params.set('schoolId',  schoolId);
        if (studentId) params = params.set('studentId', studentId);
        if (from)      params = params.set('from',      new Date(from).toISOString());
        if (to)        params = params.set('to',        new Date(to).toISOString());
        return this._http
            .get<ApiResponse<PaymentSummaryDto>>(`${this._base}/summary`, { params })
            .pipe(map(r => r.data));
    }

    // ──────────────────────────────────────────────────────────────
    // COMMANDS
    // ──────────────────────────────────────────────────────────────

    create(dto: CreatePaymentDto): Observable<PaymentResponseDto> {
        return this._http
            .post<ApiResponse<PaymentResponseDto>>(this._base, dto)
            .pipe(map(r => r.data));
    }

    update(id: string, dto: UpdatePaymentDto): Observable<PaymentResponseDto> {
        return this._http
            .put<ApiResponse<PaymentResponseDto>>(`${this._base}/${id}`, dto)
            .pipe(map(r => r.data));
    }

    delete(id: string): Observable<{ success: boolean; message: string }> {
        return this._http
            .delete<{ success: boolean; message: string }>(`${this._base}/${id}`);
    }

    reverse(id: string, dto: ReversePaymentDto): Observable<PaymentResponseDto> {
        return this._http
            .post<ApiResponse<PaymentResponseDto>>(`${this._base}/${id}/reverse`, dto)
            .pipe(map(r => r.data));
    }

    bulkCreate(dto: BulkPaymentDto): Observable<BulkPaymentResultDto> {
        return this._http
            .post<ApiResponse<BulkPaymentResultDto>>(`${this._base}/bulk`, dto)
            .pipe(map(r => r.data));
    }

    // ──────────────────────────────────────────────────────────────
    // LOOKUPS — used by the payment form steps
    // ──────────────────────────────────────────────────────────────

    /** GET /api/academic/students[?schoolId=] */
    getStudents(schoolId?: string): Observable<any[]> {
        let params = new HttpParams();
        if (schoolId) params = params.set('schoolId', schoolId);
        return this._http
            .get<any>(`${this._apiBase}/api/academic/students`, { params })
            .pipe(map(r => r.data ?? r));
    }

    /**
     * GET /api/finance/invoices/by-student/{studentId}[?schoolId=]
     * Returns open invoices for a student so the form can populate
     * the invoice selector after a student is chosen.
     */
    getInvoicesByStudent(studentId: string, schoolId?: string): Observable<any[]> {
        let params = new HttpParams();
        if (schoolId) params = params.set('schoolId', schoolId);
        return this._http
            .get<any>(`${this._apiBase}/api/finance/invoices/by-student/${studentId}`, { params })
            .pipe(map(r => r.data ?? r));
    }

    /**
     * GET /api/academic/teachers[?schoolId=]
     * Staff list used to populate the "Received By" selector.
     */
    getStaff(schoolId?: string): Observable<any[]> {
        let params = new HttpParams();
        if (schoolId) params = params.set('schoolId', schoolId);
        return this._http
            .get<any>(`${this._apiBase}/api/academic/teachers`, { params })
            .pipe(map(r => r.data ?? r));
    }

    /** GET /api/schools  (SuperAdmin only) */
    getSchools(): Observable<any[]> {
        return this._http
            .get<any>(`${this._apiBase}/api/schools`)
            .pipe(map(r => r.data ?? r));
    }
}