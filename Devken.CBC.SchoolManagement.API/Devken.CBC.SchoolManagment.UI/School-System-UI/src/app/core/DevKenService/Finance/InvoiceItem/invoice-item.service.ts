import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import { ApiResponse } from 'app/Tenant/types/school';
import {
  CreateInvoiceItemDto,
  InvoiceItemResponseDto,
  UpdateInvoiceItemDto,
} from 'app/Finance/InvoiceItem/Types/invoice-item.types';

@Injectable({ providedIn: 'root' })
export class InvoiceItemService {
  private readonly apiBase = inject(API_BASE_URL);
  private readonly http    = inject(HttpClient);

  // /api/invoices/{invoiceId}/items
  private url(invoiceId: string, suffix = ''): string {
    return `${this.apiBase}/api/invoices/${invoiceId}/items${suffix}`;
  }

  // ── Query ─────────────────────────────────────────────────────────────────

  getByInvoice(invoiceId: string): Observable<ApiResponse<InvoiceItemResponseDto[]>> {
    return this.http.get<ApiResponse<InvoiceItemResponseDto[]>>(this.url(invoiceId));
  }

  getById(invoiceId: string, id: string): Observable<ApiResponse<InvoiceItemResponseDto>> {
    return this.http.get<ApiResponse<InvoiceItemResponseDto>>(this.url(invoiceId, `/${id}`));
  }

  // ── Mutations ─────────────────────────────────────────────────────────────

  create(payload: CreateInvoiceItemDto): Observable<ApiResponse<InvoiceItemResponseDto>> {
    return this.http.post<ApiResponse<InvoiceItemResponseDto>>(
      this.url(payload.invoiceId),
      payload,
    );
  }

  update(
    invoiceId: string,
    id: string,
    payload: UpdateInvoiceItemDto,
  ): Observable<ApiResponse<InvoiceItemResponseDto>> {
    return this.http.put<ApiResponse<InvoiceItemResponseDto>>(
      this.url(invoiceId, `/${id}`),
      payload,
    );
  }

  delete(invoiceId: string, id: string): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(this.url(invoiceId, `/${id}`));
  }

  recompute(
    invoiceId: string,
    id: string,
    discountOverride?: number,
  ): Observable<ApiResponse<InvoiceItemResponseDto>> {
    const qs = discountOverride != null ? `?discountOverride=${discountOverride}` : '';
    return this.http.patch<ApiResponse<InvoiceItemResponseDto>>(
      this.url(invoiceId, `/${id}/recompute${qs}`),
      {},
    );
  }
}