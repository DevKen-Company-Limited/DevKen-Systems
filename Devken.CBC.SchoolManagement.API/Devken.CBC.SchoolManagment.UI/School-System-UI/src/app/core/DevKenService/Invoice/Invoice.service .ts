import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import { ApiResponse } from 'app/Tenant/types/school';
import { InvoiceQueryDto, InvoiceSummaryResponseDto, InvoiceResponseDto, CreateInvoiceDto, UpdateInvoiceDto, ApplyDiscountDto } from 'app/Finance/Invoice/Types/Invoice.types';


@Injectable({ providedIn: 'root' })
export class InvoiceService {
  private baseUrl = `${inject(API_BASE_URL)}/api/finance/invoices`;
  private http = inject(HttpClient);

  /** GET /api/finance/invoices */
  getAll(query?: InvoiceQueryDto, schoolId?: string): Observable<ApiResponse<InvoiceSummaryResponseDto[]>> {
  let params = new HttpParams();
  if (schoolId) params = params.set('schoolId', schoolId);  // ← add first
  if (query) {
    if (query.studentId)             params = params.set('studentId', query.studentId);
    if (query.parentId)              params = params.set('parentId', query.parentId);
    if (query.academicYearId)        params = params.set('academicYearId', query.academicYearId);
    if (query.termId)                params = params.set('termId', query.termId);
    if (query.invoiceStatus != null) params = params.set('invoiceStatus', String(query.invoiceStatus));
    if (query.isOverdue != null)     params = params.set('isOverdue', String(query.isOverdue));
    if (query.dateFrom)              params = params.set('dateFrom', query.dateFrom);
    if (query.dateTo)                params = params.set('dateTo', query.dateTo);
    if (query.isActive != null)      params = params.set('isActive', String(query.isActive));
  }
  return this.http.get<ApiResponse<InvoiceSummaryResponseDto[]>>(this.baseUrl, { params });
}

  /** GET /api/finance/invoices/{id} */
  getById(id: string): Observable<ApiResponse<InvoiceResponseDto>> {
    return this.http.get<ApiResponse<InvoiceResponseDto>>(`${this.baseUrl}/${id}`);
  }

  /** GET /api/finance/invoices/by-student/{studentId} */
  getByStudent(studentId: string): Observable<ApiResponse<InvoiceSummaryResponseDto[]>> {
    return this.http.get<ApiResponse<InvoiceSummaryResponseDto[]>>(`${this.baseUrl}/by-student/${studentId}`);
  }

  /** GET /api/finance/invoices/by-parent/{parentId} */
  getByParent(parentId: string): Observable<ApiResponse<InvoiceSummaryResponseDto[]>> {
    return this.http.get<ApiResponse<InvoiceSummaryResponseDto[]>>(`${this.baseUrl}/by-parent/${parentId}`);
  }

  /** POST /api/finance/invoices */
  create(dto: CreateInvoiceDto): Observable<ApiResponse<InvoiceResponseDto>> {
    return this.http.post<ApiResponse<InvoiceResponseDto>>(this.baseUrl, dto);
  }

  /** PUT /api/finance/invoices/{id} */
  update(id: string, dto: UpdateInvoiceDto): Observable<ApiResponse<InvoiceResponseDto>> {
    return this.http.put<ApiResponse<InvoiceResponseDto>>(`${this.baseUrl}/${id}`, dto);
  }

  /** PATCH /api/finance/invoices/{id}/apply-discount */
  applyDiscount(id: string, dto: ApplyDiscountDto): Observable<ApiResponse<InvoiceResponseDto>> {
    return this.http.patch<ApiResponse<InvoiceResponseDto>>(`${this.baseUrl}/${id}/apply-discount`, dto);
  }

  /** PATCH /api/finance/invoices/{id}/cancel */
  cancel(id: string): Observable<ApiResponse<InvoiceResponseDto>> {
    return this.http.patch<ApiResponse<InvoiceResponseDto>>(`${this.baseUrl}/${id}/cancel`, {});
  }

  /** DELETE /api/finance/invoices/{id} */
  delete(id: string): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.baseUrl}/${id}`);
  }
}